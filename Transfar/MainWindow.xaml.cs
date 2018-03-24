﻿using System;
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
            client = new Client();
            InitializeComponent();
        }

        // devo fare in modo che quando premo la checkbox entrambe le operazioni asincrone vengano avviate ed aspettate (WaitAll)

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
