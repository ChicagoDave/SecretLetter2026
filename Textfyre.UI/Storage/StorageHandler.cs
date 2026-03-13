using System;
using System.Collections.Generic;
using System.IO;

namespace Textfyre.UI.Storage
{
    public static class StorageHandler
    {
        private const string KeyPrefix = "SL_";

        #region :: Text Storage ::
        public static void WriteTextFile(string filename, string data)
        {
            try
            {
                string key = KeyPrefix + filename;
                OpenSilver.Interop.ExecuteJavaScript(
                    "localStorage.setItem($0, $1)", key, data);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[SL] StorageHandler.WriteTextFile failed: {ex.Message}");
            }
        }

        public static string ReadTextFile(string filename)
        {
            try
            {
                string key = KeyPrefix + filename;
                var result = OpenSilver.Interop.ExecuteJavaScript(
                    "localStorage.getItem($0)", key);
                string value = result?.ToString();
                if (value == "null" || value == null)
                    return string.Empty;
                return value;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[SL] StorageHandler.ReadTextFile failed: {ex.Message}");
                return string.Empty;
            }
        }

        public static void DeleteFile(string filename)
        {
            try
            {
                string key = KeyPrefix + filename;
                OpenSilver.Interop.ExecuteJavaScript(
                    "localStorage.removeItem($0)", key);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[SL] StorageHandler.DeleteFile failed: {ex.Message}");
            }
        }
        #endregion

        #region :: Binary Storage (for Quetzal save data) ::
        public static void WriteBinaryFile(string filename, byte[] data)
        {
            try
            {
                string key = KeyPrefix + filename;
                string base64 = Convert.ToBase64String(data);
                OpenSilver.Interop.ExecuteJavaScript(
                    "localStorage.setItem($0, $1)", key, base64);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[SL] StorageHandler.WriteBinaryFile failed: {ex.Message}");
            }
        }

        public static byte[] ReadBinaryFile(string filename)
        {
            try
            {
                string key = KeyPrefix + filename;
                var result = OpenSilver.Interop.ExecuteJavaScript(
                    "localStorage.getItem($0)", key);
                string value = result?.ToString();
                if (value == "null" || value == null || value.Length == 0)
                    return null;
                return Convert.FromBase64String(value);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[SL] StorageHandler.ReadBinaryFile failed: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region :: File Listing ::
        /// <summary>
        /// Returns all keys matching the given prefix (after the global KeyPrefix).
        /// </summary>
        public static List<string> GetFileNames(string directory, string pattern)
        {
            var files = new List<string>();
            try
            {
                // Get all localStorage keys
                var countObj = OpenSilver.Interop.ExecuteJavaScript("localStorage.length");
                int count = Convert.ToInt32(countObj.ToString());

                string searchPrefix = KeyPrefix + directory + "\\";

                for (int i = 0; i < count; i++)
                {
                    var keyObj = OpenSilver.Interop.ExecuteJavaScript(
                        "localStorage.key($0)", i);
                    string key = keyObj?.ToString();
                    if (key != null && key.StartsWith(searchPrefix))
                    {
                        string filename = key.Substring(searchPrefix.Length);
                        // Match against pattern suffix (e.g., "*.fvt" matches files ending in ".fvt")
                        if (pattern == null || pattern == "*" || filename.EndsWith(pattern.TrimStart('*')))
                        {
                            files.Add(filename);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[SL] StorageHandler.GetFileNames failed: {ex.Message}");
            }
            return files;
        }
        #endregion

        #region :: Storage Info ::
        public static bool IsAvailable
        {
            get
            {
                try
                {
                    OpenSilver.Interop.ExecuteJavaScript(
                        "localStorage.setItem('__test__', '1')");
                    OpenSilver.Interop.ExecuteJavaScript(
                        "localStorage.removeItem('__test__')");
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
        #endregion
    }
}
