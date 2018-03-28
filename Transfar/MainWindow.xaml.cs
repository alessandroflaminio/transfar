using System;
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

        public MainWindow()
        {
            CheckInstance();
            client = new Client();
            
            InitializeComponent();


            // TODO: for testing purposes
            try
            {
                testLabel.Content = Environment.GetCommandLineArgs().GetValue(1) ?? "";
                testLabel2.Content = Environment.GetCommandLineArgs().GetValue(2) ?? "";
            }
            catch (Exception)
            {
                
            }

            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("C:\\Users\\Alessandro\\source\\repos\\Transfar\\Transfar\\Icon.ico");
            ni.Visible = true;
        }

        private void CheckInstance()
        {
            // If Transfar is already running
            if (System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1)
            {
                IPCClient.Client(Environment.GetCommandLineArgs()); // Sends the data to the already running instance
                Application.Current.Shutdown(); // Exits the current process
            }
            else
            {
                ListenInstancesAsync();
            }
        }

        private async void ListenInstancesAsync()
        {
            await Task.Run(async () => // async put so that the exception is thrown to the caller
            {
                do
                {
                    IPCServer ipcServer = new IPCServer();
                    ipcServer.Server(); // The thread blocks here until an IPC Client connects to the server
                    // TODO: you should check that the received params are valid
                    // Operations pertaining the UI
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                     {
                         //this.Hide(); // Hide the MainWindow
                         ClientDiscoveryWindow clientDiscoveryWindow = new ClientDiscoveryWindow();
                         clientDiscoveryWindow.Show();
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
                await Task.WhenAll(AnnounceAsync(cts.Token), ListenRequestsAsync(reportIndicator, cts.Token));
            }
            catch (Exception)
            {
                Console.WriteLine("Cancellation requested!");
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

                    token.ThrowIfCancellationRequested();
                }
            }, token);
        }

        private async Task ListenRequestsAsync(IProgress<TcpClient> reportIndicator, CancellationToken token)
        {
            await Task.Run(async () => // async put so that the exception is thrown to the caller
            {
                client.StartListening(); // adesso si inizia ad ascoltare realmente

                while (true)
                {
                    reportIndicator.Report(client.ListenRequests());

                    token.ThrowIfCancellationRequested();
                }
            }, token);
        }

        private void Settings_Button_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow(this);
            settingsWindow.Show();
            settingsWindow.Activate();
            this.Hide();
            //using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            //{
            //    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            //    System.Console.WriteLine(dialog.SelectedPath);
            //}
        }
    }
}
