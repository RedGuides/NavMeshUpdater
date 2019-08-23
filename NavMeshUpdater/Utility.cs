using System;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;

namespace NavMeshUpdater
{
    class Utility
    {
        // For generating file hashes only. Not secure for passwords or other security.
        public static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        // For opening a web browser.
        public static void OpenURL(string url)
        {
            Process.Start(url);
        }
    }
}
