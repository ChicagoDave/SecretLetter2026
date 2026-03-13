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

namespace Textfyre.UI.Current
{
    public static class User
    {
        private static int _age = 0;
        public static int Age
        {
            get
            {
                return _age;
            }
            set
            {
                _age = value;
            }
        }

        private static string _gender = String.Empty;
        public static string Gender
        {
            get
            {
                return _gender;
            }
            set
            {
                _gender = value;
            }
        }

        private static string _city = String.Empty;
        public static string City
        {
            get
            {
                return _city;
            }
            set
            {
                _city = value.Replace("|", String.Empty);
            }
        }

        private static string _state = String.Empty;
        public static string State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value.Replace("|", String.Empty);
            }
        }

        private static string _school = String.Empty;
        public static string School
        {
            get
            {
                return _school;
            }
            set
            {
                _school = value.Replace("|",String.Empty);
            }
        }

        public static bool IsStored
        {
            get
            {
                string dg = Storage.Settings.Get("Demographic");

                if( dg.Length == 0 )
                    return false;

                return dg.Split('|').Length == 5;
            }
        }

        public static void Load()
        {
            if( IsStored )
            {
                string dg = Storage.Settings.Get("Demographic");
                string[] items = dg.Split('|');

                _gender = items[0];
                _age = Convert.ToInt32(items[1]);
                _city = items[2];
                _state = items[3];
                _school = items[4];
            }
        }

        public static void Save()
        {
            string dg = _gender + "|" + _age.ToString() + "|" +
                _city + "|" + _state + "|" + _school;
            Storage.Settings.Set("Demographic",dg);
        }

        public static void LogCommand(string responseText)
        {
            // FyreService web service no longer exists — logging disabled
        }

        public static void LogNotes(string notesBy, string notes)
        {
            // FyreService web service no longer exists — logging disabled
        }

        //static void proxy_LogCommandCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        //{
        //    if (e.Cancelled == false && e.Error == null)
        //    {
        //        // Logging was successful.
        //    }
        //}
    }
}
