using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Transfar
{
    /// <summary>
    /// Logica di interazione per ClientDiscoveryWindow.xaml
    /// </summary>
    public partial class ClientDiscoveryWindow : Window
    {
        private Server server;
        private CancellationTokenSource cts;


        public ClientDiscoveryWindow()
        {
            server = new Server();
            InitializeComponent();
        }

        private async void startButton_Click(object sender, RoutedEventArgs e)
        {
            startButton.IsEnabled = false;
            stopButton.IsEnabled = true;
            stopButton.IsDefault = true;

            cts = new CancellationTokenSource();
            var progressIndicator = new Progress<int>(ReportProgress);
            var reportIndicator = new Progress<IPEndPoint>(ReportAddition);

            clientsListBox.Items.Clear();

            try
            {
                await ClientDiscoveryAsync(reportIndicator, progressIndicator, cts.Token);
            }
            catch (Exception)
            {
                Console.WriteLine("Cancellation requested!");
            }

            stopButton.IsDefault = false;
            stopButton.IsEnabled = false;
            startButton.IsEnabled = true;
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            stopButton.IsDefault = false;
            stopButton.IsEnabled = false;
            startButton.IsEnabled = true;

            cts.Cancel();
            progressBar.Value = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            server.Dispose();
        }

        private void ReportProgress(int value)
        {
            progressBar.Value = value; // Attualmente il primo progresso è 0, controllare come viene assegnato il Report nella Async
        }

        private void ReportAddition(IPEndPoint client)
        {
            if (client != null && !clientsListBox.Items.Contains(client))
                clientsListBox.Items.Add(client);
        }

        private async Task ClientDiscoveryAsync(IProgress<IPEndPoint> reportIndicator, IProgress<int> progressIndicator, CancellationToken token)
        {
            await Task.Run(async () => // async put so that the exception is thrown to the caller
            {
                for (int i = 0; i < 100; i++)
                {
                    reportIndicator.Report(server.ClientDiscovery());

                    Thread.Sleep(500);

                    token.ThrowIfCancellationRequested();

                    progressIndicator.Report(i + 1);
                }
            }, token);
        }

        private void clientsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (clientsListBox.SelectedItem != null)
            {
                sendButton.IsEnabled = true;
            }
            else
            {
                sendButton.IsEnabled = false;
            }
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(clientsListBox.SelectedItem);

            SelectFileWindow selectFileWindow = new SelectFileWindow(server, (IPEndPoint) clientsListBox.SelectedItem);
            selectFileWindow.Show();
            selectFileWindow.Activate();
        }
    }
}
