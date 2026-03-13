using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Textfyre.VM;

namespace Textfyre.UI.Pages
{
    /// <summary>
    /// A MemoryStream that persists to localStorage when the engine closes it.
    /// </summary>
    internal class PersistingStream : MemoryStream
    {
        private readonly string _path;

        public PersistingStream(string path) : base()
        {
            _path = path;
        }

        public override void Close()
        {
            byte[] data = this.ToArray();
            Storage.StorageHandler.WriteBinaryFile(_path, data);
            System.Console.WriteLine($"[SL] Saved {data.Length} bytes to {_path}");
            base.Close();
        }
    }

    public partial class Story : UserControl
    {
        private Engine _engine;
        private string _inputLine = null;
        private TaskCompletionSource<string> _inputTcs;
        private TaskCompletionSource<object> _saveTcs;

        private DateTime _commandStartTime;
        private TimeSpan _sendToVM;
        private TimeSpan _returnFromVM;

        public Story(byte[] memorystream, string gameFileName)
        {
            InitializeComponent();

            BookGrid.Width = Settings.BookWidth;

            LayoutRoot.Background = Helpers.Color.SolidColorBrush(Settings.BackgroundColor);

            this.Loaded += new RoutedEventHandler(Story_Loaded);

            _tbVersion.Text = string.Concat(Settings.VersionText, " (OpenSilver)");

            TextfyreBook.TextfyreDocument.InputEntered += new EventHandler<Textfyre.UI.Controls.Input.InputEventArgs>(TextfyreDocument_InputEntered);
            TextfyreBook.SaveGameDialog.SaveRequest += new EventHandler<Textfyre.UI.Controls.IODialog.Save.SaveEventArgs>(SaveGameDialog_SaveRequest);
            TextfyreBook.RestoreGameDialog.RestoreRequest += new EventHandler<Textfyre.UI.Controls.IODialog.Restore.RestoreEventArgs>(RestoreGameDialog_RestoreRequest);

            Keyboard.KeyPress += new EventHandler<KeyEventArgs>(Keyboard_KeyPress);
            Keyboard.KeyDown += new EventHandler<KeyEventArgs>(Keyboard_KeyDown);
            Keyboard.KeyUp += new EventHandler<KeyEventArgs>(Keyboard_KeyUp);

            LoadGame(memorystream, gameFileName);
        }

        bool _keyAltPressed = false;
        bool _keyShiftPressed = false;

        void Keyboard_KeyUp(object sender, KeyEventArgs e)
        {

            switch (e.Key)
            {
                case Key.Ctrl:
                    TextfyreBook.TextfyreDocument.HideWordDefs();
                    break;
                case Key.Alt:
                    _keyAltPressed = false;
                    break;
                case Key.Shift:
                    _keyShiftPressed = false;
                    break;
            }

        }

        void Keyboard_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Ctrl:
                    TextfyreBook.TextfyreDocument.ShowWordDefs();
                    break;
                case Key.Alt:
                    _keyAltPressed = true;
                    break;
                case Key.Shift:
                    _keyShiftPressed = true;
                    break;
                case Key.N:
                    if (_keyAltPressed && _keyShiftPressed)
                    {
                        TextfyreBook.TextfyreDocument.Input("*");
                    }
                    break;
            }
        }

        void Keyboard_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.PageDown)
            {
                TextfyreBook.FlipForward();
            }

            if (e.Key == Key.PageUp)
            {
                TextfyreBook.FlipBack();
            }
        }

        private void TextfyreDocument_InputEntered(object sender, Textfyre.UI.Controls.Input.InputEventArgs e)
        {
            _commandStartTime = DateTime.Now;
            _inputLine = e.TextEntered;
            _inputTcs?.TrySetResult(e.TextEntered);
        }


        private void Story_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void LoadGame(byte[] memorystream, string gameFileName)
        {
            Current.Game.GameFileName = gameFileName;
            MemoryStream mstr = new MemoryStream(memorystream);
            StartGame(mstr);
        }

        List<Grid> _textblocktests = new List<Grid>();

        private async void StartGame(Stream mstr)
        {
            _engine = new Engine(mstr);

            _engine.OutputReady += new OutputReadyEventHandler(engine_OutputReady);

            _engine.LineWantedAsync = async (sender, e) =>
            {
                Current.Game.IsEngineRunning = false;
                Dispatcher.BeginInvoke(() =>
                {
                    if (Current.Game.GameMode == GameModes.Restart)
                    {
                        TextfyreBook.TextfyreDocument.Input("yes", String.Empty);
                        Current.Game.IsStoryChanged = false;
                    }
                });

                _inputTcs = new TaskCompletionSource<string>();
                string input = await _inputTcs.Task;

                Current.Game.IsEngineRunning = true;
                _inputLineForTranscript = input;

                Dispatcher.BeginInvoke(() =>
                {
                    TextfyreBook.Wait.Show();
                    Current.Game.IsScrollLimitEnabled = true;
                    Current.Game.GameState.FyreXmlAdd(
                        String.Concat(Resource.FyreXML_Paragraph_Begin, "&gt;", input, Resource.FyreXML_Paragraph_End)
                    );
                });

                e.Line = input;
                _inputLine = null;
                _sendToVM = DateTime.Now.Subtract(_commandStartTime);
            };

            _engine.KeyWantedAsync = async (sender, e) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    TextfyreBook.Wait.Hide();
                    TextfyreBook.TextfyreDocument.AddInputSingleChar();
                    TextfyreBook.TextfyreDocument.AddInputHandler();
                });

                Current.Game.IsEngineRunning = false;

                _inputTcs = new TaskCompletionSource<string>();
                string input = await _inputTcs.Task;

                Current.Game.IsScrollLimitEnabled = true;
                Current.Game.IsEngineRunning = true;
                char key = (input != null && input.Length > 0) ? input[0] : ' ';
                e.Char = key;
                _inputLine = null;
            };

            _engine.SaveRequestedAsync = async (sender, e) =>
            {
                if (!Entities.SaveFile.IsStorageAvailable)
                    return;

                Dispatcher.BeginInvoke(() => TextfyreBook.Wait.Hide());

                try
                {
                    // Auto-save with location, turn, and timestamp
                    var sf = new Entities.SaveFile();
                    sf.Title = Current.Game.Location ?? "Unknown";
                    sf.Description = $"Turn {Current.Game.Turn} — {DateTime.Now:g}";
                    sf.SaveTime = DateTime.Now;
                    sf.GameFileVersion = Current.Game.GameFileName;
                    sf.FyreXml = Current.Game.GameState.FyreXml;
                    sf.StoryTitle = Current.Game.StoryTitle;
                    sf.Chapter = Current.Game.Chapter;
                    sf.Theme = Current.Game.ThemeID;
                    sf.Hints = Current.Game.Hints;

                    string binaryPath = sf.Save();
                    e.Stream = new PersistingStream(binaryPath);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[SL] Save failed: {ex.Message}");
                }

                Dispatcher.BeginInvoke(() => TextfyreBook.Wait.Hide());
            };

            _engine.LoadRequestedAsync = async (sender, e) =>
            {
                if (!Entities.SaveFile.IsStorageAvailable)
                    return;

                // Cancel any pending wait spinner, then show restore dialog
                Dispatcher.BeginInvoke(() =>
                {
                    TextfyreBook.Wait.Hide();
                    TextfyreBook.RestoreGameDialog.Show();
                });

                _saveTcs = new TaskCompletionSource<object>();
                await _saveTcs.Task;

                if (_saveFile != null)
                {
                    try
                    {
                        string binaryPath = _saveFile.BinaryStoryFilePath;
                        byte[] data = Storage.StorageHandler.ReadBinaryFile(binaryPath);

                        if (data != null && data.Length > 0)
                        {
                            var ms = new MemoryStream(data);
                            e.Stream = ms;

                            // Restore UI state from save metadata
                            Dispatcher.BeginInvoke(() =>
                            {
                                try
                                {
                                    if (!string.IsNullOrEmpty(_saveFile.FyreXml))
                                    {
                                        Current.Game.GameState.FyreXmlClear();
                                        Current.Game.GameState.FyreXmlAdd(_saveFile.FyreXml);
                                        Current.Game.GameState.ActivateGameState();
                                    }
                                    if (!string.IsNullOrEmpty(_saveFile.Theme))
                                        Current.Game.ThemeID = _saveFile.Theme;
                                    if (!string.IsNullOrEmpty(_saveFile.Chapter))
                                        Current.Game.Chapter = _saveFile.Chapter;
                                    if (!string.IsNullOrEmpty(_saveFile.Hints))
                                        Current.Game.Hints = _saveFile.Hints;
                                }
                                catch (Exception ex2)
                                {
                                    System.Console.WriteLine($"[SL] Restore UI state failed: {ex2.Message}");
                                }
                            });
                        }
                        else
                        {
                            System.Console.WriteLine("[SL] Restore: no binary data found");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"[SL] Restore failed: {ex.Message}");
                    }
                }

                _saveFile = null;
                Dispatcher.BeginInvoke(() => TextfyreBook.Wait.Hide());
            };

            try
            {
                await _engine.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SL] Engine.RunAsync failed: {ex}");
            }
        }

        private void engine_OutputReady(object sender, OutputReadyEventArgs e)
        {
            _returnFromVM = DateTime.Now.Subtract(_commandStartTime);
            _returnFromVM = _returnFromVM.Subtract(_sendToVM);
            if (e.Package.Count == 0)
                return;

            if (e.Package.ContainsKey(OutputChannel.Time))
            {
                int turn = -1;
                int.TryParse(e.Package[OutputChannel.Time], out turn);
                if (turn > -1)
                    Current.Game.Turn = turn;

            }

            Dispatcher.BeginInvoke(new OutputDelegate(UpdateScreen), e.Package);
        }

        private delegate void OutputDelegate(IDictionary<OutputChannel, string> output);

        private void UpdateScreen(IDictionary<OutputChannel, string> output)
        {
            Dispatcher.BeginInvoke(() =>
            {
                TextfyreBook.Wait.Hide();

                if (Current.Game.GameMode == GameModes.ExecuteRestart)
                {
                    Current.Game.TextfyreBook.RestartGame();
                }
            
            });

            TimeSpan elapsed = DateTime.Now.Subtract(_commandStartTime);

            if (Settings.Profile)
                TextfyreBook.TextfyreDocument.AddFyreXml(String.Concat(Resource.FyreXML_Paragraph_Begin, "Elapsed VM Time: ", _returnFromVM.ToString(), Resource.FyreXML_Paragraph_End));

            if (AnyOutput(output, OutputChannel.Title))
            {
                Current.Game.StoryTitle = output[OutputChannel.Title];
            }

            string location = String.Empty;
            bool updateLocAndChap = false;

            #region :: Location && Chapter ::
            if (AnyOutput(output, OutputChannel.Location))
            {
                location = output[OutputChannel.Location];
                Current.Game.Location = location;
                updateLocAndChap = true;
            }

            if (AnyOutput(output, OutputChannel.Chapter))
            {
                string chapter = output[OutputChannel.Chapter];
                Current.Game.Chapter = chapter;
                updateLocAndChap = true;
            }

            if (updateLocAndChap)
                TextfyreBook.SetLocationAndChapter();

            #endregion


            #region :: Credits ::
            if (AnyOutput(output, OutputChannel.Credits))
            {
                string credits = string.Concat("", output[OutputChannel.Credits], "");
                credits = credits.Replace("&#169;", "©").Replace(Environment.NewLine + " ", Environment.NewLine).Replace("\n ", "\n");

                // Rewrite credits for 2026 edition
                credits = credits.Replace("Jesse McGrew", "Tara McGrew");
                credits = credits.Replace("Graeme Jefferis", "David Cornelson");
                credits = credits.Replace("Copyright © 2009 by Textfyre, Inc", "Copyright © 2026 David Cornelson");
                credits = credits.Replace("\nof Tenteo (www.tenteo.com)", "");

                Current.Game.TextfyreBook.SetCreditsText(credits);
            }
            #endregion

            #region :: Prologue ::
            if (AnyOutput(output, OutputChannel.Prologue))
            {
                Current.Game.GameMode = GameModes.Story;

                if (Current.Game.IsStoryReady == false)
                {
                    string prologue = output[OutputChannel.Prologue];

                    if (!prologue.StartsWith(Resource.FyreXML_Paragraph_Partial_Begin))
                    {
                        prologue = String.Concat(Resource.FyreXML_Paragraph_Begin, prologue, Resource.FyreXML_Paragraph_End);
                    }

                    prologue = SpotArt.InsertSpotArt(prologue);

                    System.Text.StringBuilder leadingNewlines = new StringBuilder("");

                    if (Settings.PagingMechanism == Settings.PagingMechanismType.StaticPageCreateBackPages)
                        prologue = String.Concat(Resource.FyreXML_PrologueMode, leadingNewlines.ToString(), prologue, Resource.FyreXML_PrologueContents);
                    else if (Settings.PagingMechanism == Settings.PagingMechanismType.CreateNewPages)
                        prologue = String.Concat(Resource.FyreXML_PrologueMode, prologue, Resource.FyreXML_StoryMode);

                    prologue = DocSystem.WordDef.ParseTextForWordDefs(prologue);

                    Current.Game.GameState.FyreXmlAdd(prologue);
                    TextfyreBook.TextfyreDocument.AddFyreXml(prologue);
                }
            }
            #endregion

            #region :: Time ::
            if (AnyOutput(output, OutputChannel.Time))
            {
                TextfyreBook.SetTime(output[OutputChannel.Time]);
            }
            #endregion

            #region :: Conversation ::
            if (AnyOutput(output, OutputChannel.Conversation))
            {
                string conversation = output[OutputChannel.Conversation].Replace("\n", String.Empty);

                Current.Game.GameState.FyreXmlAdd(conversation);
                TextfyreBook.TextfyreDocument.AddFyreXml(conversation);
            }
            #endregion

            #region :: Hints ::
            if (AnyOutput(output, OutputChannel.Hints))
            {
                string hints = output[OutputChannel.Hints].Replace("\n", String.Empty);
                Current.Game.Hints = hints;
            }
            #endregion

            #region :: Main ::
            if (AnyOutput(output, OutputChannel.Main))
            {
                elapsed = DateTime.Now.Subtract(_commandStartTime);

                if (Settings.Profile)
                    TextfyreBook.TextfyreDocument.AddFyreXml(String.Concat(Resource.FyreXML_Paragraph_Begin, "Elapsed Time: ", elapsed.ToString(), Resource.FyreXML_Paragraph_End));

                string[] tbs = output[OutputChannel.Main].Split('~');

                System.Text.StringBuilder sbMain = new System.Text.StringBuilder("");
                foreach (string tb in tbs)
                {
                    if (tb != "")
                    {
                        string txt = SpotArt.InsertSpotArt(tb);

                        txt = Regex.Replace(txt, Resource.FyreXML_LocationName_Regex_Search, Resource.FyreXML_LocationName_Replacement);

                        txt = DocSystem.WordDef.ParseTextForWordDefs(txt);

                        sbMain.Append(txt);

                        Current.Game.GameState.FyreXmlAdd(txt);
                        TextfyreBook.TextfyreDocument.AddFyreXml(txt);
                    }
                }
                Current.User.LogCommand(sbMain.ToString());
            }
            #endregion

            #region :: Theme ::
            if (AnyOutput(output, OutputChannel.Theme))
            {
                string themeID = output[OutputChannel.Theme];

                Current.Game.ThemeID = themeID;
            }
            #endregion

            #region :: Death ::
            if (AnyOutput(output, OutputChannel.Death))
            {
                string death = output[OutputChannel.Death];
                Current.Game.GameState.FyreXmlAdd(death);

                if (!death.StartsWith(Resource.FyreXML_Paragraph_Partial_Begin))
                {
                    death = String.Concat(Resource.FyreXML_Paragraph_Begin, death, Resource.FyreXML_Paragraph_End);
                }

                TextfyreBook.TextfyreDocument.AddFyreXml(death);
            }
            #endregion

            #region :: Prompt ::
            if (AnyOutput(output, OutputChannel.Prompt))
            {
                bool promptOnly = output.Count == 1;
                string prompt = output[OutputChannel.Prompt];

                if (Current.Game.GameMode == GameModes.Story)
                {
                    elapsed = DateTime.Now.Subtract(_commandStartTime);

                    if (Settings.Profile)
                        TextfyreBook.TextfyreDocument.AddFyreXml(String.Concat(Resource.FyreXML_Paragraph_Begin, "Elapsed Time: ", elapsed.ToString(), Resource.FyreXML_Paragraph_End));

                    prompt = String.Concat(Resource.FyreXML_Prompt_Start, prompt.Replace(">", "&gt;"), Resource.FyreXML_Prompt_End);
                    TextfyreBook.TextfyreDocument.AddFyreXml(prompt);
                }
            }
            #endregion

            Current.Game.IsStoryReady = true;
        }

        private bool AnyOutput(IDictionary<OutputChannel, string> output, OutputChannel outputChannel)
        {
            if (output.ContainsKey(outputChannel) == false)
                return false;

            return output[outputChannel] != "";
        }

        private string _inputLineForTranscript;

        private Entities.SaveFile _saveFile;

        void SaveGameDialog_SaveRequest(object sender, Textfyre.UI.Controls.IODialog.Save.SaveEventArgs e)
        {
            _saveFile = e.SaveFile;
            _saveTcs?.TrySetResult(null);
        }

        void RestoreGameDialog_RestoreRequest(object sender, Textfyre.UI.Controls.IODialog.Restore.RestoreEventArgs e)
        {
            TextfyreBook.RestoreGameDialog.Hide();
            _saveFile = e.SaveFile;
            _saveTcs?.TrySetResult(null);
        }
    }
}
