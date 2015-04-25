using HaloChat.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Guacamole.Game;
using Guacamole.Communication;

namespace HaloChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private ElDorado _gameProcess;
        ChatHelper _co = new ChatHelper();
        KeyboardHook _listener;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _co;
            Loaded += MainWindow_Loaded;
        }

        #region Window Related Stuff (moving, resizing, Keyhook etc)
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _listener = new KeyboardHook(KeyboardHook.Parameters.PassAllKeysToNextApp);
            _listener.KeyIntercepted += _listener_KeyIntercepted;
            MainGrid.RowDefinitions[0].Height = new GridLength(0);
            StartSystem();
        }

        void _listener_KeyIntercepted(KeyboardHook.KeyboardHookEventArgs e)
        {
            if (e.KeyName == "T")
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    _listener.KeyIntercepted -= _listener_KeyIntercepted;
                    this.Activate();
                    InputBlock.Focus();
                });
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (_listener != null)
            this.Topmost = true;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Topmost = true;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _gameProcess.StopMonitors();
            if (!_gameProcess.UseIRC) _gameProcess.KillClientsAndServer();
            else _gameProcess.DisconnectFromIrcChannel();
            _listener.Dispose();
            Environment.Exit(0);
        }

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void window_MouseEnter(object sender, MouseEventArgs e)
        {
            MainGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
        }

        private void window_MouseLeave(object sender, MouseEventArgs e)
        {
            MainGrid.RowDefinitions[0].Height = new GridLength(0);
        }

        double resizeStep = 50;
        private void ResizeMinus_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ChatBox.ItemsSource = null;
            window.Height -= resizeStep;
            window.Width -= resizeStep;
            _co.ChatSize-=2;
            ChatBox.ItemsSource = _co.ChatMessages;
        }

        private void ResizePlus_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ChatBox.ItemsSource = null;
            window.Height += resizeStep;
            window.Width += resizeStep;
            _co.ChatSize += 2;
            ChatBox.ItemsSource = _co.ChatMessages;
        }
        #endregion

        #region Chat Related

        private void StartSystem()
        {
            _gameProcess = new ElDorado() { UseIRC = true, IrcServer = "teamitf.co.uk", IrcPort = 6667 };
            _gameProcess.MonitorProcesses();
            _gameProcess.AllMessages.CollectionChanged += AllMessages_CollectionChanged;
        }

        void AllMessages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                   {
                       ChatBox.ItemsSource = null;
                       _co.ChatMessages.Clear();
                       ChatBox.ItemsSource = _co.ChatMessages;
                   });
            }

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (string item in e.NewItems)
                {
                    if (!_co.ChatMessages.Contains(item))
                        _co.ChatMessages.Add(item);
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Scroller.ScrollToBottom();
                    });
                }
            }
        }

        private void InputBlock_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (string.IsNullOrWhiteSpace(InputBlock.Text)) return;
                _listener.KeyIntercepted += _listener_KeyIntercepted;
                if (!_gameProcess.UseIRC)
                    {
                        if (_gameProcess._chatClient != null && _gameProcess._chatClient._isConnected)
                            _gameProcess._chatClient.SendMessage(InputBlock.Text);
                        else 
                            _co.ChatMessages.Add("Error: Your not connected to a game lobby!");
                    }
                    else
                    {
                        if (_gameProcess._ircClient != null && _gameProcess._ircClient.IsConnected)
                            _gameProcess.SendIrcMessage(InputBlock.Text);
                        else
                            _co.ChatMessages.Add("Error: Your not connected to a game lobby!");
                    }   
                _co.ChatInput = InputBlock.Text;
                _co.ProcessText();
                Scroller.ScrollToBottom();
                Keyboard.ClearFocus();
            }
        }
        #endregion

    }
}