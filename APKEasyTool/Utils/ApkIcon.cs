using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace APKEasyTool
{
    /// <summary>
    /// Finds and decodes an app's launcher icon. Handles what the old code missed:
    /// adaptive icons (res/mipmap-anydpi-v26/*.xml) whose bitmaps are WebP, not PNG.
    /// </summary>
    public static class ApkIcon
    {
        static readonly string[] DensityOrder = { "xxxhdpi", "xxhdpi", "xhdpi", "hdpi", "mdpi", "ldpi", "" };
        static readonly string[] Exts = { ".png", ".webp", ".jpg", ".jpeg" };

        /// <summary>PNG/JPG via GDI+, WebP (and anything else Windows has a codec for) via WIC.</summary>
        public static Image Decode(byte[] data)
        {
            try
            {
                using (var ms = new MemoryStream(data))
                using (var img = Image.FromStream(ms))
                    return new Bitmap(img);
            }
            catch (ArgumentException) { }

            using (var ms = new MemoryStream(data))
            {
                var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(ms,
                    System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(decoder.Frames[0]);
                using (var png = new MemoryStream())
                {
                    encoder.Save(png);
                    png.Position = 0;
                    using (var img = Image.FromStream(png))
                        return new Bitmap(img);
                }
            }
        }

        public static Image FromFile(string file)
        {
            return Decode(File.ReadAllBytes(file));
        }

        /// <summary>
        /// Icon of an APK. iconPaths are aapt's application-icon-* values; for an adaptive icon (.xml)
        /// the bitmap with the same name in the density folders is used, then its foreground layer.
        /// Returns null when nothing usable is found.
        /// </summary>
        public static Image FromApk(string apkPath, IEnumerable<string> iconPaths)
        {
            using (ZipArchive zip = ZipFile.OpenRead(apkPath))
            {
                var entries = zip.Entries.Where(e => e.FullName.StartsWith("res/")).ToList();
                var tried = new List<string>();
                foreach (string icon in iconPaths.Where(i => !String.IsNullOrEmpty(i)))
                {
                    if (!icon.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        tried.Add(icon);
                    string name = Path.GetFileNameWithoutExtension(icon);
                    foreach (string n in new[] { name, name + "_foreground" })
                        foreach (string d in DensityOrder)
                            foreach (var e in entries)
                            {
                                string dir = Path.GetDirectoryName(e.FullName).Replace('\\', '/');
                                if (Path.GetFileNameWithoutExtension(e.Name) == n && Exts.Contains(Path.GetExtension(e.Name).ToLowerInvariant())
                                    && Regex.IsMatch(dir, @"^res/(mipmap|drawable)") && (d == "" || dir.Contains(d)))
                                    tried.Add(e.FullName);
                            }
                }
                foreach (string path in tried.Distinct())
                {
                    ZipArchiveEntry entry = zip.GetEntry(path);
                    if (entry == null)
                        continue;
                    try
                    {
                        using (var s = entry.Open())
                        using (var ms = new MemoryStream())
                        {
                            s.CopyTo(ms);
                            return Decode(ms.ToArray());
                        }
                    }
                    catch { } // try the next candidate
                }
            }
            return null;
        }
    }
}
