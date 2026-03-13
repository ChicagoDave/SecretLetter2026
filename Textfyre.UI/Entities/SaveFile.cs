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
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Windows.Threading;

namespace Textfyre.UI.Entities
{
    public class SaveFile
    {
        #region :: Title ::
        private string _title;
        public string Title {
            get {
                return _title;
            }
            set {
                _title = value;
            }
        }
        #endregion

        #region :: Description ::
        private string _description;
        public string Description {
            get {
                return _description;
            }
            set {
                _description = value;
            }
        }
        #endregion

        #region :: SaveTime ::
        public DateTime _saveTime;
		public DateTime SaveTime {
			get {
                return _saveTime;
			}
			set {
                _saveTime = value;
			}
        }
        #endregion

        #region :: Filename ::
        private string _filename = string.Empty;
        public string Filename
        {
            get
            {
                return _filename;
            }
            set
            {
                _filename = value;
            }
        }
        #endregion

        #region :: GameFileVersion ::
        private string _gameFileVersion = String.Empty;
        public string GameFileVersion
        {
            get
            {
                return _gameFileVersion;
            }
            set
            {
                _gameFileVersion = value;
            }
        }
        #endregion

        #region :: FyreXml ::
        private string _fyreXml = String.Empty;
        public string FyreXml
        {
            get
            {
                return _fyreXml;
            }
            set
            {
                _fyreXml = value;
            }
        }
        #endregion

        #region :: Transcript ::
        private string _transcript = String.Empty;
        public string Transcript
        {
            get
            {
                return _transcript;
            }
            set
            {
                _transcript = value;
            }
        }
        #endregion

        #region :: Chapter ::
        private string _chapter = String.Empty;
        public string Chapter
        {
            get
            {
                return _chapter;
            }
            set
            {
                _chapter = value;
            }
        }
        #endregion

        #region :: Theme ::
        private string _theme = String.Empty;
        public string Theme
        {
            get
            {
                return _theme;
            }
            set
            {
                _theme = value;
            }
        }
        #endregion

        #region :: StoryTitle ::
        private string _storyTitle = String.Empty;
        public string StoryTitle
        {
            get
            {
                return _storyTitle;
            }
            set
            {
                _storyTitle = value;
            }
        }
        #endregion

        #region :: Hints ::
        private string _hints = String.Empty;
        public string Hints
        {
            get
            {
                return _hints;
            }
            set
            {
                _hints = value;
            }
        }
        #endregion

        public void Delete()
        {
            if (_filename.Length == 0 || !IsStorageAvailable)
                return;

            string filepath = _dir + @"\" + _filename;
            Storage.StorageHandler.DeleteFile(filepath + ".fvt");
            Storage.StorageHandler.DeleteFile(filepath + ".fvq");
        }

        /// <summary>
        /// Saves metadata to storage. Returns filepath for binary story file.
        /// </summary>
        public string Save()
        {
            if (!IsStorageAvailable)
                return string.Empty;

            if (_filename.Length == 0)
            {
                _filename = FindFilename();
            }

            string filepath = _dir + @"\" + _filename;
            string serialized = Serialize(this);
            Storage.StorageHandler.WriteTextFile(filepath + ".fvt", serialized);

            return filepath + ".fvq";
        }

        private string FindFilename()
        {
            List<SaveFile> sfs = SaveFiles;

            foreach (SaveFile sf in sfs)
            {
                if (sf.Title == _title && sf.Filename.Length > 0)
                    return sf.Filename;
            }

            return Guid.NewGuid().ToString() + Current.Game.GameFileName;
        }

        public string BinaryStoryFilePath
        {
            get
            {
                return _dir + @"\" + _filename + ".fvq";
            }
        }

        public static int SaveFilesCount
        {
            get
            {
                if (!IsStorageAvailable) return 0;
                var files = Storage.StorageHandler.GetFileNames(_dir, "*" + Current.Game.GameFileName + ".fvt");
                return files.Count;
            }
        }

        public static void DeleteOldSaveFiles()
        {
            string currentGameFileName = Current.Game.GameFileName;
            List<SaveFile> files = SaveFiles;
            foreach (SaveFile file in files)
            {
                if (file.GameFileVersion != currentGameFileName)
                {
                    file.Delete();
                }
            }
        }

        public static List<SaveFile> SaveFiles
        {
            get
            {
                List<SaveFile> saveFiles = new List<SaveFile>();
                if (!IsStorageAvailable) return saveFiles;

                var files = Storage.StorageHandler.GetFileNames(_dir, "*" + Current.Game.GameFileName + ".fvt");
                foreach (string file in files)
                {
                    string data = Storage.StorageHandler.ReadTextFile(_dir + @"\" + file);
                    if (!string.IsNullOrEmpty(data))
                    {
                        SaveFile sf = SaveFile.Deserialize(data);
                        saveFiles.Add(sf);
                    }
                }

                return saveFiles;
            }
        }

        public static string Serialize( SaveFile saveFile)
        {
            System.Text.StringBuilder sb = new StringBuilder();
            sb.Append(ToParam(saveFile.Filename) + "|");
            sb.Append(ToParam(saveFile.Title) + "|");
            sb.Append(ToParam(saveFile.Description) + "|");
            sb.Append(ToParam(saveFile.SaveTime.ToString()) + "|");
            sb.Append(ToParam(saveFile.GameFileVersion.ToString()) + "|");
            sb.Append(ToParam(saveFile.FyreXml) + "|");
            sb.Append(ToParam(saveFile.Transcript) + "|");
            sb.Append(ToParam(saveFile.StoryTitle) + "|");
            sb.Append(ToParam(saveFile.Chapter) + "|");
            sb.Append(ToParam(saveFile.Theme) + "|");
            sb.Append(ToParam(saveFile.Hints) + "");

            return sb.ToString();
        }

        public static SaveFile Deserialize(string json)
        {
            SaveFile saveFile = new SaveFile();
            string[] parts = json.Split('|');
            if (parts.Length >= 4)
            {
                saveFile.Filename = FromParam(parts[0]);
                saveFile.Title = FromParam(parts[1]);
                saveFile.Description = FromParam(parts[2]);
                try
                {
                    saveFile.SaveTime = DateTime.Parse(FromParam(parts[3]));
                }
                catch
                {
                    saveFile.SaveTime = DateTime.Now;
                }
            }

            if (parts.Length >= 5)
            {
                saveFile.GameFileVersion = FromParam(parts[4]);
            }

            if (parts.Length >= 6)
            {
                saveFile.FyreXml = FromParam(parts[5]);
            }

            if (parts.Length >= 7)
            {
                saveFile.Transcript = FromParam(parts[6]);
            }

            if (parts.Length >= 8)
            {
                saveFile.StoryTitle = FromParam(parts[7]);
            }

            if (parts.Length >= 9)
            {
                saveFile.Chapter = FromParam(parts[8]);
            }

            if (parts.Length >= 10)
            {
                saveFile.Theme = FromParam(parts[9]);
            }

            if (parts.Length >= 11)
            {
                saveFile.Hints = FromParam(parts[10]);
            }

            return saveFile;
        }

        private static string ToParam(string value)
        {
            return value.Replace("|", "&#124;");
        }

        private static string FromParam(string value)
        {
            return value.Replace("&#124;", "|");
        }

        private static string _dir = Settings.SaveGameDirectory;

        private static bool _storageChecked;
        private static bool _storageAvailable;

        public static bool IsStorageAvailable
        {
            get
            {
                if (!_storageChecked)
                {
                    _storageChecked = true;
                    _storageAvailable = Storage.StorageHandler.IsAvailable;
                    if (_storageAvailable)
                        System.Console.WriteLine("[SL] localStorage available for save/load");
                    else
                        System.Console.WriteLine("[SL] localStorage not available — save/load disabled");
                }
                return _storageAvailable;
            }
        }

        public static long FreeSpace
        {
            get
            {
                // localStorage typically has 5-10MB; report a reasonable estimate
                return 5 * 1024 * 1024;
            }
        }

        public static long TotalSpace
        {
            get
            {
                return 5 * 1024 * 1024;
            }
        }

        public static void IncreaseStorageSpace()
        {
            // No-op for localStorage (fixed quota managed by browser)
        }

    }
}
