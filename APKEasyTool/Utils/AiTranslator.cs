using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace APKEasyTool
{
    /// <summary>Settings for an OpenAI-compatible chat completions endpoint.</summary>
    public class AiSettings
    {
        public string BaseUrl = "https://api.openai.com/v1";
        public string Model = "gpt-4o-mini";
        public string ProtectedApiKey = ""; // DPAPI, current Windows user only
        public int BatchSize = 40;
        public int Parallel = 2;
        public int DelayMs = 0;

        [XmlIgnore]
        public string ApiKey
        {
            get
            {
                if (String.IsNullOrEmpty(ProtectedApiKey))
                    return "";
                try
                {
                    byte[] data = ProtectedData.Unprotect(Convert.FromBase64String(ProtectedApiKey), null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(data);
                }
                catch
                {
                    return "";
                }
            }
            set
            {
                ProtectedApiKey = String.IsNullOrEmpty(value) ? "" :
                    Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser));
            }
        }

        public static AiSettings Load(string file)
        {
            try
            {
                if (File.Exists(file))
                    using (var fs = File.OpenRead(file))
                        return (AiSettings)new XmlSerializer(typeof(AiSettings)).Deserialize(fs);
            }
            catch { }
            return new AiSettings();
        }

        public void Save(string file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            using (var fs = File.Create(file))
                new XmlSerializer(typeof(AiSettings)).Serialize(fs, this);
        }

        public string Endpoint
        {
            get
            {
                string u = (BaseUrl ?? "").Trim().TrimEnd('/');
                if (u.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                    return u;
                return u + "/chat/completions";
            }
        }
    }

    public class AiProgress
    {
        public Dictionary<string, string> Translated;
        public int Done, Total, Failed;
        public string Status;
    }

    /// <summary>
    /// Translates many short strings through an OpenAI-compatible API.
    /// Sends batches as a JSON object, retries 429/5xx/timeouts with backoff (honouring Retry-After),
    /// pauses every worker after a rate limit, splits batches that are too large, and re-asks for
    /// items that came back missing or with broken placeholders.
    /// </summary>
    public class AiTranslator
    {
        static readonly Regex FormatSpec = new Regex(@"%(\d+\$)?[-#+ 0,(]*\d*(\.\d+)?[sdfxXoeEgGcbBhHaAn%]");
        const int MaxAttempts = 8;

        readonly AiSettings settings;
        readonly HttpClient http;
        readonly object pauseLock = new object();
        DateTime pausedUntil = DateTime.MinValue;

        public AiTranslator(AiSettings settings)
        {
            this.settings = settings;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            http = new HttpClient { Timeout = TimeSpan.FromSeconds(180) };
        }

        /// <param name="items">key -> source text</param>
        /// <param name="targetLanguage">language code, e.g. "fa"</param>
        public async Task<Dictionary<string, string>> TranslateAll(Dictionary<string, string> items, string targetLanguage,
            IProgress<AiProgress> progress, CancellationToken ct)
        {
            var result = new Dictionary<string, string>();
            var pending = new Queue<List<KeyValuePair<string, string>>>(
                Chunk(items.ToList(), Math.Max(1, settings.BatchSize)));
            int total = items.Count, failed = 0;
            var running = new List<Task>();
            var failedKeys = new List<string>();

            while (pending.Count > 0 || running.Count > 0)
            {
                ct.ThrowIfCancellationRequested();
                while (pending.Count > 0 && running.Count < Math.Max(1, settings.Parallel))
                {
                    var batch = pending.Dequeue();
                    running.Add(RunBatch(batch, targetLanguage, ct, 0).ContinueWith(t =>
                    {
                        lock (result)
                        {
                            if (t.IsFaulted)
                            {
                                Exception ex = t.Exception.GetBaseException();
                                if (ex is AiFatalException)
                                    throw ex;
                                failed += batch.Count;
                                failedKeys.AddRange(batch.Select(b => b.Key));
                                Report(progress, null, result.Count, total, failed, "Batch failed: " + ex.Message);
                                return;
                            }
                            if (t.IsCanceled)
                                return;
                            var got = t.Result;
                            foreach (var kv in got)
                                result[kv.Key] = kv.Value;
                            int missing = batch.Count - got.Count;
                            failed += missing;
                            Report(progress, got, result.Count, total, failed, null);
                        }
                    }, TaskScheduler.Default));
                }
                Task finished = await Task.WhenAny(running);
                running.Remove(finished);
                await finished; // rethrows fatal errors / cancellation
            }
            return result;
        }

        static void Report(IProgress<AiProgress> progress, Dictionary<string, string> got, int done, int total, int failed, string status)
        {
            if (progress != null)
                progress.Report(new AiProgress { Translated = got, Done = done, Total = total, Failed = failed, Status = status });
        }

        // Translates one batch; items that come back missing or invalid are retried in smaller batches.
        async Task<Dictionary<string, string>> RunBatch(List<KeyValuePair<string, string>> batch, string lang, CancellationToken ct, int depth)
        {
            var output = new Dictionary<string, string>();
            Dictionary<string, string> answer = null;
            bool tooLarge = false;
            try
            {
                answer = await Request(batch, lang, ct);
            }
            catch (AiTooLargeException)
            {
                tooLarge = true;
            }
            if (tooLarge)
            {
                // Request too big for the model: split in half.
                if (batch.Count == 1)
                    return output;
                foreach (var half in Chunk(batch, (batch.Count + 1) / 2))
                    foreach (var kv in await RunBatch(half, lang, ct, depth + 1))
                        output[kv.Key] = kv.Value;
                return output;
            }

            var retry = new List<KeyValuePair<string, string>>();
            foreach (var item in batch)
            {
                string tr;
                if (answer.TryGetValue(item.Key, out tr) && !String.IsNullOrWhiteSpace(tr) && SamePlaceholders(item.Value, tr))
                    output[item.Key] = tr;
                else
                    retry.Add(item);
            }
            if (retry.Count > 0 && depth < 2)
            {
                int size = Math.Max(1, Math.Min(retry.Count, settings.BatchSize / 4));
                foreach (var part in Chunk(retry, size))
                    foreach (var kv in await RunBatch(part, lang, ct, depth + 1))
                        output[kv.Key] = kv.Value;
            }
            return output;
        }

        async Task<Dictionary<string, string>> Request(List<KeyValuePair<string, string>> batch, string lang, CancellationToken ct)
        {
            var ser = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var input = new Dictionary<string, string>();
            foreach (var kv in batch)
                input[kv.Key] = kv.Value;

            string system =
                "You translate Android app UI strings. Translate every value into the language with code \"" + lang + "\". " +
                "Return ONLY a JSON object with exactly the same keys and the translated values. " +
                "Keep placeholders like %s %d %1$s %2$d, escape sequences like \\n \\' \\\", HTML tags and URLs exactly as they are. " +
                "Keep brand and product names untranslated. Keep it short and natural for an app interface. " +
                "If a value is already in the target language, return it unchanged.";
            var body = new Dictionary<string, object>
            {
                { "model", settings.Model },
                { "temperature", 0.2 },
                { "messages", new object[] {
                    new Dictionary<string, string> { { "role", "system" }, { "content", system } },
                    new Dictionary<string, string> { { "role", "user" }, { "content", ser.Serialize(input) } } } }
            };
            string json = ser.Serialize(body);

            for (int attempt = 1; ; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                await WaitWhilePaused(ct);
                if (settings.DelayMs > 0)
                    await Task.Delay(settings.DelayMs, ct);

                HttpResponseMessage resp = null;
                string text = null;
                Exception netError = null;
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, settings.Endpoint);
                    req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + settings.ApiKey);
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    resp = await http.SendAsync(req, ct);
                    text = await resp.Content.ReadAsStringAsync();
                }
                catch (OperationCanceledException)
                {
                    if (ct.IsCancellationRequested)
                        throw;
                    netError = new TimeoutException("Request timed out");
                }
                catch (HttpRequestException ex)
                {
                    netError = ex;
                }

                if (netError == null)
                {
                    int code = (int)resp.StatusCode;
                    if (resp.IsSuccessStatusCode)
                    {
                        Dictionary<string, string> parsed = ParseAnswer(ser, text);
                        if (parsed != null)
                            return parsed;
                        if (attempt >= 3)
                            return new Dictionary<string, string>(); // unusable answer: items get retried smaller
                        continue;
                    }
                    if (code == 401 || code == 403)
                        throw new AiFatalException("The API key was rejected (" + code + "). Check AI settings.\n" + Short(text));
                    if (code == 404)
                        throw new AiFatalException("Endpoint or model not found (404). Check the URL and model ID in AI settings.\n" + Short(text));
                    if (code == 400 || code == 413)
                    {
                        string t = (text ?? "").ToLowerInvariant();
                        if (code == 413 || t.Contains("context") || t.Contains("too long") || t.Contains("maximum") || t.Contains("token"))
                            throw new AiTooLargeException();
                        throw new AiFatalException("The service refused the request (400):\n" + Short(text));
                    }
                    if (attempt >= MaxAttempts)
                        throw new Exception("HTTP " + code + " after " + attempt + " attempts: " + Short(text));
                    TimeSpan wait = Backoff(attempt, resp);
                    if (code == 429)
                        PauseAll(wait);
                    await Task.Delay(wait, ct);
                    continue;
                }

                if (attempt >= MaxAttempts)
                    throw new Exception(netError.Message);
                await Task.Delay(Backoff(attempt, null), ct);
            }
        }

        static Dictionary<string, string> ParseAnswer(JavaScriptSerializer ser, string responseJson)
        {
            try
            {
                var root = ser.Deserialize<Dictionary<string, object>>(responseJson);
                var choices = (System.Collections.IList)root["choices"];
                var message = (Dictionary<string, object>)((Dictionary<string, object>)choices[0])["message"];
                string content = Convert.ToString(message["content"]);
                int start = content.IndexOf('{'), end = content.LastIndexOf('}');
                if (start < 0 || end <= start)
                    return null;
                var obj = ser.Deserialize<Dictionary<string, object>>(content.Substring(start, end - start + 1));
                var result = new Dictionary<string, string>();
                foreach (var kv in obj)
                    if (kv.Value != null)
                        result[kv.Key] = Convert.ToString(kv.Value);
                return result;
            }
            catch
            {
                return null;
            }
        }

        TimeSpan Backoff(int attempt, HttpResponseMessage resp)
        {
            if (resp != null && resp.Headers.RetryAfter != null)
            {
                if (resp.Headers.RetryAfter.Delta.HasValue)
                    return Clamp(resp.Headers.RetryAfter.Delta.Value);
                if (resp.Headers.RetryAfter.Date.HasValue)
                    return Clamp(resp.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow);
            }
            double seconds = Math.Min(60, Math.Pow(2, attempt)) + new Random().NextDouble() * 2;
            return TimeSpan.FromSeconds(seconds);
        }

        static TimeSpan Clamp(TimeSpan t)
        {
            if (t < TimeSpan.FromSeconds(1)) return TimeSpan.FromSeconds(1);
            if (t > TimeSpan.FromSeconds(120)) return TimeSpan.FromSeconds(120);
            return t;
        }

        void PauseAll(TimeSpan wait)
        {
            lock (pauseLock)
            {
                DateTime until = DateTime.UtcNow + wait;
                if (until > pausedUntil)
                    pausedUntil = until;
            }
        }

        async Task WaitWhilePaused(CancellationToken ct)
        {
            while (true)
            {
                TimeSpan left;
                lock (pauseLock)
                    left = pausedUntil - DateTime.UtcNow;
                if (left <= TimeSpan.Zero)
                    return;
                await Task.Delay(left, ct);
            }
        }

        public static bool SamePlaceholders(string a, string b)
        {
            Func<string, string> key = s => String.Join(",", FormatSpec.Matches(s ?? "").Cast<Match>().Select(m => m.Value).OrderBy(x => x, StringComparer.Ordinal));
            return key(a) == key(b);
        }

        static IEnumerable<List<T>> Chunk<T>(List<T> list, int size)
        {
            for (int i = 0; i < list.Count; i += size)
                yield return list.GetRange(i, Math.Min(size, list.Count - i));
        }

        static string Short(string s)
        {
            s = s ?? "";
            return s.Length > 400 ? s.Substring(0, 400) + "..." : s;
        }

        /// <summary>Quick connection test with a single string.</summary>
        public async Task<string> Test(CancellationToken ct)
        {
            var one = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("hello", "Hello") };
            var r = await Request(one, "fa", ct);
            string v;
            return r.TryGetValue("hello", out v) ? v : null;
        }
    }

    public class AiFatalException : Exception
    {
        public AiFatalException(string message) : base(message) { }
    }

    class AiTooLargeException : Exception { }
}
