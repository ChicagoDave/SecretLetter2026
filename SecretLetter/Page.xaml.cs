using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SecretLetter
{
    public partial class Page : UserControl
    {
        public Page()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(Page_Loaded);
        }

        void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Textfyre.UI.Current.Application.GameAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                Textfyre.UI.Current.Application.AppResources = App.Current.Resources;

                Console.WriteLine("[SL] Page_Loaded: calling LoadStory...");
                StoryPage.LoadStory(GameFiles.GameFile.sl_v1_07e, "sl_v1_07e", new StoryHandle());
                Console.WriteLine("[SL] Page_Loaded: LoadStory complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SL] Page_Loaded FAILED: {ex}");
            }
        }
    }
}
