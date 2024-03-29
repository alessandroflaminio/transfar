﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Transfar
{
    /// <summary>
    /// Logica di interazione per ClientDiscoveryWindow.xaml
    /// </summary>
    public partial class ClientDiscoveryWindow : Window
    {
        private Server server;
        private CancellationTokenSource cts;
        private string filePath;

        public ClientDiscoveryWindow()
        {
                server = new Server();

                InitializeComponent();

                startButton_Click(null, null); // HACK: Added so that it's not necessary to press the start button
        }

        public ClientDiscoveryWindow(string filePath) // Constructor when you don't need to open the file picker
        {
            this.filePath = filePath;
            
            server = new Server();

            InitializeComponent();

            infoLabel.Content = "Please select an host to which send the selected file:";
            sendButton.Content = "Send file";
            sendButton.Click -= sendButton_Click;
            sendButton.Click += sendButtonContextual_Click;

            startButton_Click(null, null); // HACK: Added so that it's not necessary to press the start button
        }

        private async void startButton_Click(object sender, RoutedEventArgs e)
        {
            startButton.IsEnabled = false;
            stopButton.IsEnabled = true;
            stopButton.IsDefault = true;

            cts = new CancellationTokenSource();
            var progressIndicator = new Progress<int>(ReportProgress);
            var reportIndicator = new Progress<NamedIPEndPoint>(ReportAddition);

            clientsListView.Items.Clear();

            try
            {
                await ClientDiscoveryAsync(reportIndicator, progressIndicator, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Cancellation requested!");
                server.ResetAvailableClients(); // HACK: Resets the list of the available clients in the server object
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
            if(stopButton.IsEnabled) // Check if there is something to interrupt
                stopButton_Click(this, new RoutedEventArgs()); // HACK: to dispose the processes that are executing
            server.Dispose();
        }

        private void ReportProgress(int value)
        {
            progressBar.Value = value;
        }

        private void ReportAddition(NamedIPEndPoint client)
        {
            if (client != null && !clientsListView.Items.Contains(client))
                clientsListView.Items.Add(client);
        }

        private async Task ClientDiscoveryAsync(IProgress<NamedIPEndPoint> reportIndicator, IProgress<int> progressIndicator, CancellationToken token)
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

        private void clientsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (clientsListView.SelectedItem != null)
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
            Debug.WriteLine(clientsListView.SelectedItem);

            FolderBrowser fb = new FolderBrowser();
            fb.Description = "Please select a file or a folder below:";
            fb.IncludeFiles = true;
            fb.NewStyle = false;
            //fb.InitialDirectory = @"C:\";
            if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = fb.SelectedPath;
                Debug.WriteLine("Selected path:" + filePath);

                for (int i = 0; i < clientsListView.SelectedItems.Count; i++)
                {
                    SendingFileWindow sendingFileWindow = new SendingFileWindow(server, (NamedIPEndPoint)clientsListView.SelectedItems[i], filePath);
                }

                this.Close();
            }
        }

        private void sendButtonContextual_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < clientsListView.SelectedItems.Count; i++)
            {
                SendingFileWindow sendingFileWindow = new SendingFileWindow(server, (NamedIPEndPoint)clientsListView.SelectedItems[i], filePath);
            }

            this.Close();
        }
    }
}
