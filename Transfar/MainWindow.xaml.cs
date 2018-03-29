using System;
using System.ComponentModel;
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
        private System.Windows.Forms.NotifyIcon ni;

        public MainWindow()
        {
            CheckInstance();
            client = new Client();
            ConfigureTrayIcon();

            InitializeComponent();
        }

        private void ConfigureTrayIcon()
        {
            ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("C:\\Users\\Alessandro\\source\\repos\\Transfar\\Transfar\\Icon.ico");
            ni.Visible = true;
            ni.Click += ShowTransfarClick;
            ni.ContextMenu = new System.Windows.Forms.ContextMenu();
            ni.ContextMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Exit Transfar", ExitTransfarClick));
        }

        private void ExitTransfarClick(object sender, EventArgs e) => Application.Current.Shutdown();

        private void ShowTransfarClick(object sender, EventArgs e) => this.Show();

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true; // setting cancel to true will cancel the close request so the application is not closed

            this.Hide();

            base.OnClosing(e);
        }

        private void CheckInstance()
        {
            // If Transfar is already running
            if (System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1)
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
                if (args.Count() > 1) // If launched from contextual menu
                {
                    ClientDiscoveryWindow clientDiscoveryWindow = new ClientDiscoveryWindow(args[1]); // Passing the path (second argument)
                    clientDiscoveryWindow.Show();
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
                         try
                         {
                             ClientDiscoveryWindow clientDiscoveryWindow = new ClientDiscoveryWindow(filePath); // Passing the path
                             clientDiscoveryWindow.Show();
                         }
                         catch (SocketException) // That means that I'm trying to open 2 ClientDiscoveryWindow (two servers)
                         {
                             MessageBox.Show("To start sending another file, close the open Transfar Windows.", "Transfar", MessageBoxButton.OK,
                                 MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                         }
                     }));

                } while (true);
            });

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ClientDiscoveryWindow clientDiscoveryWindow = new ClientDiscoveryWindow();
            clientDiscoveryWindow.Show();
        }

        private async void AvailabilityCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            cts = new CancellationTokenSource();
            var reportIndicator = new Progress<TcpClient>(ReportNewFile);

            try
            {
                await Task.WhenAll(AnnounceAsync(cts.Token), ListenRequestsAsync(reportIndicator));
            }
            //catch (OperationCanceledException)
            //{
            //    Console.WriteLine("Cancellation requested!");
            //}
            catch (SocketException)
            {
                Console.WriteLine("The AcceptTcpClient was effectively blocked");
            }
        }

        private void AvailabilityCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
        }

        private void ReportNewFile(TcpClient tcpClient)
        {
            ReceivingFileWindow receivingFileWindow = new ReceivingFileWindow(client, tcpClient);
            receivingFileWindow.Show();
            receivingFileWindow.Activate();
        }

        private async Task AnnounceAsync(CancellationToken token)
        {
            await Task.Run(async () => // async put so that the exception is thrown to the caller
            {
                while (true)
                {
                    client.Announce();

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
