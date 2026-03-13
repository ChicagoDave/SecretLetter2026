using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Data;
using System.Windows.Markup;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using Textfyre.VM;
using System.Windows.Media;

namespace Textfyre.UI
{
    public partial class Page : UserControl
    {
        public Page()
        {
            InitializeComponent();

            try
            {
                Settings.Init();
                Console.WriteLine($"[SL] Settings.Init OK — BookWidth={Settings.BookWidth}, BookPageHeight={Settings.BookPageHeight}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SL] Settings.Init FAILED: {ex}");
            }

            try
            {
                SpotArt.Init();
                Console.WriteLine("[SL] SpotArt.Init OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SL] SpotArt.Init FAILED: {ex}");
            }

            if (!string.IsNullOrEmpty(Settings.BackgroundColor))
            {
                this.Background = Helpers.Color.SolidColorBrush(Settings.BackgroundColor);
                LayoutRoot.Background = Helpers.Color.SolidColorBrush(Settings.BackgroundColor);
            }

            this.SizeChanged += (s, e) => Resize();
            this.Loaded += (s, e) => Resize();
        }

        private void Resize()
        {
            double currentWidth = Application.Current.Host.Content.ActualWidth;
            double currentHeight = Application.Current.Host.Content.ActualHeight;

            // Fallback if Host.Content dimensions are 0 (OpenSilver)
            if (currentWidth <= 0 || currentHeight <= 0)
            {
                currentWidth = this.ActualWidth > 0 ? this.ActualWidth : 1024;
                currentHeight = this.ActualHeight > 0 ? this.ActualHeight : 768;
            }

            Console.WriteLine($"[SL] Page.Resize: {currentWidth}x{currentHeight} (Host={Application.Current.Host.Content.ActualWidth}x{Application.Current.Host.Content.ActualHeight})");

            this.Width = currentWidth;
            this.Height = currentHeight;
            LayoutRoot.Width = currentWidth;
            LayoutRoot.Height = currentHeight;

            MasterPage.SetSize();
            MasterPage.Resize();
        }

        public void LoadStory(byte[] memorystream, string gameFileName)
        {
            LoadStory(memorystream, gameFileName, new StoryHandle());
        }

        public void LoadStory(byte[] memorystream, string gameFileName, StoryHandle storyHandle)
        {
            if (storyHandle == null)
                Current.Game.StoryHandle = new StoryHandle();
            else
                Current.Game.StoryHandle = storyHandle;

            Current.Game.StoryHandle.UserSettingsInit();

            MasterPage.LoadStory(memorystream, gameFileName);
        }
    }
}
