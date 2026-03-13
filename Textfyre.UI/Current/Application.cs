using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Windows.Markup;
using System.Windows.Resources;
using System.IO;
using System.Xml.Linq;

namespace Textfyre.UI.Current
{
    public static class Application
    {
        /// <summary>
        /// Load an XML resource file from the game assembly's embedded resources.
        /// The relativePath should be like "GameFiles/Settings.xml" — it gets
        /// converted to a manifest resource name like "SecretLetter.GameFiles.Settings.xml".
        /// </summary>
        public static XDocument LoadResourceXml(string relativePath)
        {
            // Try loading from the game assembly's embedded resources
            var assembly = GameAssembly ?? System.Windows.Application.Current.GetType().Assembly;
            string resourceName = assembly.GetName().Name + "." + relativePath.Replace("/", ".").Replace("\\", ".");

            var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using (var reader = new StreamReader(stream))
                {
                    return XDocument.Parse(reader.ReadToEnd());
                }
            }

            // Fallback: search for partial match in case naming differs
            var names = assembly.GetManifestResourceNames();
            string searchSuffix = "." + relativePath.Replace("/", ".").Replace("\\", ".");
            foreach (var name in names)
            {
                if (name.EndsWith(searchSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    stream = assembly.GetManifestResourceStream(name);
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            return XDocument.Parse(reader.ReadToEnd());
                        }
                    }
                }
            }

            throw new FileNotFoundException($"Embedded resource not found: {relativePath} (tried {resourceName}). Available: {string.Join(", ", names)}");
        }

        #region :: GameAssembly ::
        private static Assembly _gameAssembly;
        public static Assembly GameAssembly
        {
            get
            {
                return _gameAssembly;
            }
            set
            {
                _gameAssembly = value;
            }
        }
        #endregion

        #region :: AppResources ::
        private static ResourceDictionary _appResources;
        public static ResourceDictionary AppResources
        {
            get
            {
                return _appResources;
            }
            set
            {
                _appResources = value;
            }
        }
        #endregion

        #region :: Platform ::
        private static Current.Platform _platform = Current.Platform.NA;
        public static Current.Platform Platform
        {
            get
            {
                if (_platform == Platform.NA)
                {
                    string pfstr = System.Windows.Browser.HtmlPage.BrowserInformation.Platform;

                    _platform = Platform.Windows;

                    if (pfstr.Contains("Mac"))
                        _platform = Platform.Mac;
                    else if (pfstr.Contains("Linux"))
                        _platform = Platform.Linux;
                    else if (pfstr.Contains("Unix"))
                        _platform = Platform.Linux;
                }

                return _platform;
            }

        }
        #endregion
        #region :: SessionID ::
        private static string _sessionID = String.Empty;
        public static string SessionID
        {
            get
            {
                if (_sessionID.Length == 0)
                    _sessionID = System.Guid.NewGuid().ToString();

                return _sessionID;
            }
        }
        #endregion

        #region :: EntryPointAssemblyName ::
        public static string EntryPointAssemblyName
        {
            get
            {
                return System.Windows.Deployment.Current.EntryPointAssembly;
            }
        }
        #endregion

        #region :: ResBasePath ::
        private static string _resBasePath = String.Empty;
        public static string ResBasePath
        {
            get
            {
                if (_resBasePath.Length == 0)
                {


                    _resBasePath = "/" + EntryPointAssemblyName + ";component/";
                    
                }

                return _resBasePath;
            }
        }
        #endregion

        #region :: GetResPath, GetResUri ::
        public static string GetResPath(string relativePath)
        {
            string path = relativePath;
            if (relativePath.StartsWith("/"))
                path = path.Substring(1);

            return ResBasePath + path;
        }

        public static Uri GetResUri( string relativePath )
        {
            return new Uri(GetResPath(relativePath), UriKind.RelativeOrAbsolute);
        }
        #endregion

        #region :: GetImageBitmap && GetImageXaml ::
        public static BitmapImage GetImageBitmap( string relativePath )
        {
            return new BitmapImage(Current.Application.GetResUri(relativePath));
        }

        public static async System.Threading.Tasks.Task<UIElement> GetImageXamlAsync(string relativePath)
        {
            StreamResourceInfo sri = await System.Windows.Application.GetResourceStream(Current.Application.GetResUri(relativePath));
            string xaml = string.Empty;
            using (StreamReader reader = new StreamReader(sri.Stream) )
            {
                xaml = reader.ReadToEnd();
            }
            return XamlReader.Load(xaml) as UIElement;
        }
        #endregion
    }
}
