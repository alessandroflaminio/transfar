using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Transfar
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Client client;
        private CancellationTokenSource cts;
        public System.Windows.Forms.NotifyIcon Ni { get; set; }

        private ClientDiscoveryWindow cdw;


        public MainWindow()
        {
            CheckInstance();
            client = new Client();
            ConfigureTrayIcon();

            InitializeComponent();
        }


        private void ConfigureTrayIcon()
        {
            Ni = new System.Windows.Forms.NotifyIcon();
            Ni.Icon = Properties.Resources.Icon;
            Ni.Visible = true;
            Ni.Click += ShowTransfarClick;
            Ni.ContextMenu = new System.Windows.Forms.ContextMenu();
            Ni.ContextMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Exit Transfar", ExitTransfarClick));
        }


        private void ExitTransfarClick(object sender, EventArgs e) => Application.Current.Shutdown(); // TODO: tray icon visible even after closing the app


        private void ShowTransfarClick(object sender, EventArgs e) => this.Show();


        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true; // setting cancel to true will cancel the close request so that the application is not closed

            this.Hide();

            base.OnClosing(e);
        }


        private void CheckInstance()
        {
            // If Transfar is already running
            if (Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1)
            {
                string[] args = Environment.GetCommandLineArgs();
                if (args.Count() > 1) // If launched from contextual menu
                {
                    IPCClient.Client(args[1]); // Sends the data to the already running instance
                }
                else // If launched from .exe
                {
                    MessageBox.Show("Transfar is already running.", "Transfar", MessageBoxButton.OK,
                        MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
                Application.Current.Shutdown(); // Exits the current process
            }
            else // If there isn't a running instance of Transfar
            {
                string[] args = Environment.GetCommandLineArgs();
                if (args.Count() > 1) // If launched from contextual menu (that means that I will have some args passed to the .exe)
                {
                    InstantiateClientDiscoveryWindow(args[1]); // Passing the path (second argument)
                }

                ListenInstancesAsync(); // Listens for other instances of Transfar launched
            }
        }


        private async void ListenInstancesAsync()
        {
            await Task.Run(async () => // async put so that the exception is thrown to the caller
            {
                do
                {
                    IPCServer ipcServer = new IPCServer();
                    string filePath = ipcServer.Server(); // The thread blocks here until an IPC Client connects to the server
                    // TODO: you should check that the received params are valid
                    // Operations pertaining the UI
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                     {
                         //this.Hide(); // Hide the MainWindow
                         InstantiateClientDiscoveryWindow(filePath);
                     }));

                } while (true);
            });

        }


        private void DiscoveryButton_Click(object sender, RoutedEventArgs e)
        {
            InstantiateClientDiscoveryWindow();
        }

        private void InstantiateClientDiscoveryWindow()
        {
            if ((cdw == null) || (cdw.IsLoaded == false)) // HACK: the first time cdw is null
            {
                cdw = new ClientDiscoveryWindow();
                cdw.Show();
            }
            else
            {
                cdw.Activate();
            }
        }


        private void InstantiateClientDiscoveryWindow(string filePath)
        {
            if ((cdw == null) || (cdw.IsLoaded == false)) // HACK: the first time cdw is null, so the second condition will not be evaluated
            {
                cdw = new ClientDiscoveryWindow(filePath);
                cdw.Show();
            }
            else // That means that I'm trying to open 2 ClientDiscoveryWindow (two servers)
            {
                MessageBox.Show("To start sending another file, close the open Transfar Windows.", "Transfar", MessageBoxButton.OK,
                    MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            }
        }


        private async void AvailabilityCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            cts = new CancellationTokenSource();
            var reportIndicator = new Progress<TcpClient>(ReportNewFile);

            try
            {
                await Task.WhenAll(AnnounceAsync(cts.Token), ListenRequestsAsync(reportIndicator));
            }
            catch (SocketException)
            {
                Debug.WriteLine("The AcceptTcpClient was effectively blocked");
            }
        }


        private void AvailabilityCheckbox_Unchecked(object sender, RoutedEventArgs e) => cts.Cancel();


        private void ReportNewFile(TcpClient tcpClient) => new ReceivingFileWindow(this, client, tcpClient);


        private async Task AnnounceAsync(CancellationToken token)
        {
            await Task.Run(async () => // async put so that the exception is thrown to the caller
            {
                while (true)
                {
                    try
                    {
                        client.Announce();
                    }
                    catch (SocketException) // Probably there is no network connection
                    {
                        MessageBox.Show("Probably there is no network connection. Transfar will be closed.", "Transfar", MessageBoxButton.OK,
                                 MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

                        await Application.Current.Dispatcher.BeginInvoke(new Action(() => // GUI thread
                        {
                            Application.Current.Shutdown();
                        }));
                    }

                    Thread.Sleep(1000);

                    if (token.IsCancellationRequested)
                    {
                        client.StopListening(); // If I don't want to be available, stop listening for connections
                    }
                    token.ThrowIfCancellationRequested();
                }
            }, token);
        }


        private async Task ListenRequestsAsync(IProgress<TcpClient> reportIndicator)
        {
            await Task.Run(async () => // async put so that the exception is thrown to the caller
            {
                client.StartListening(); // adesso si inizia ad ascoltare realmente

                while (true)
                {
                    reportIndicator.Report(client.ListenRequests());
                }
            });
        }


        private void Settings_Button_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow(this);
            settingsWindow.Show();
            settingsWindow.Activate();
            this.Hide();
        }
    }
}
