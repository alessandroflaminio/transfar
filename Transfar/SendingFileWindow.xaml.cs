﻿using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Transfar
{
    /// <summary>
    /// Logica di interazione per SendingFileWindow.xaml
    /// </summary>
    public partial class SendingFileWindow : Window
    {
        private Server server;
        private NamedIPEndPoint selectedClient;
        private string filePath;
        private long originalLength;
        private FileTransferData fileTransferData;

        private CancellationTokenSource cts;


        public SendingFileWindow(Server server, NamedIPEndPoint selectedClient, string filePath)
        {
            this.filePath = filePath;
            this.server = server;
            this.selectedClient = selectedClient;
            InitializeComponent();

            StartSending();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
        }

        private void ReportProgress(double value)
        {
            progressBar.Value = value;
        }

        private async void StartSending()
        {
            cts = new CancellationTokenSource();
            var progressIndicator = new Progress<double>(ReportProgress);

            try
            {
                fileTransferData = server.StartSending(filePath, selectedClient);
            }
            catch (SocketException)
            {
                MessageBox.Show("The selected host is unavailable.", "Transfar", MessageBoxButton.OK,
                    MessageBoxImage.Stop, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                this.Close();
                return;
            }
            originalLength = fileTransferData.Length;

            // If there are no issues with the transfer I can show the window
            fileInfo.Text = "Sending file " + fileTransferData.Name + " to " + fileTransferData.HostName + "...";
            this.Show();
            this.Activate();

            try
            {
                await SendFileAsync(progressIndicator, cts.Token);
            }
            catch (Exception)
            {
                Console.WriteLine("Cancellation requested!");
            }
            
            this.Close();
        }


        private async Task SendFileAsync(IProgress<double> progressIndicator, CancellationToken token)
        {
            await Task.Run(async () => // async put so that the exception is thrown to the caller
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    while (fileTransferData.Length > 0)
                    {
                        try
                        {
                            server.Send(fileTransferData);
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("The selected host is unavailable.", "Transfar", MessageBoxButton.OK,
                                MessageBoxImage.Stop, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                            server.CancelSending(fileTransferData);
                            return;
                        }

                        token.ThrowIfCancellationRequested();

                        //Thread.Sleep(500); // Waiting for testing purposes

                        progressIndicator.Report(100 - ((float)fileTransferData.Length / originalLength * 100));
                    }

                    server.EndSending(fileTransferData);
                }
                catch (OperationCanceledException)
                {
                    server.CancelSending(fileTransferData);
                    throw;
                }
            }, token);
        }
    }
}