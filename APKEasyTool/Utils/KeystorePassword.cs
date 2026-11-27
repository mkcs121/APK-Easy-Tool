using System;
using System.Security.Cryptography;
using System.Text;

namespace APKEasyTool
{
    /// <summary>Keystore password at rest: DPAPI, readable only by the current Windows user.</summary>
    public static class KeystorePassword
    {
        static readonly byte[] Entropy = Encoding.UTF8.GetBytes("APK Easy Tool keystore");

        public static string Protect(string password)
        {
            if (String.IsNullOrEmpty(password))
                return "";
            return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(password), Entropy, DataProtectionScope.CurrentUser));
        }

        public static string Unprotect(string stored)
        {
            if (String.IsNullOrEmpty(stored))
                return "";
            try
            {
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(stored), Entropy, DataProtectionScope.CurrentUser));
            }
            catch
            {
                return ""; // other user / machine: ask again
            }
        }
    }
}
