﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Transfar
{
    /// <summary>
    /// Logica di interazione per ReceivingFileWindow.xaml
    /// </summary>
    public partial class ReceivingFileWindow : Window
    {
        // TODO: you should check what happens when a transfer is cancelled to the other party
        private Client client;
        private TcpClient tcpClient;
        private long originalLength;
        private FileTransferData fileTransferData;

        private CancellationTokenSource cts;


        public ReceivingFileWindow(Client client, TcpClient tcpClient)
        {
            this.client = client;
            this.tcpClient = tcpClient;
            fileTransferData = client.StartReceiving(tcpClient);

            InitializeComponent();
            fileInfo.Text = fileInfo.Text.Replace("CLIENT_NAME", fileTransferData.HostName).Replace("FILE_NAME", fileTransferData.Name);
        }

        private async void Yes_Button_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(fileTransferData.Path) && !Properties.Settings.Default.AutoReplace) // if autoreplace is disabled
            {
                if (MessageBox.Show(fileTransferData.Name + " already exists. Do you want to replace it?", "Transfar",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                { // no substitution
                    string appendedTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string oldName = fileTransferData.Name;
                    string oldNameWithoutExtension = Path.GetFileNameWithoutExtension(oldName);
                    fileTransferData.Name = oldName.Replace(oldNameWithoutExtension, oldNameWithoutExtension + appendedTimestamp);
                    fileTransferData.Path = fileTransferData.Path.Replace(oldName, fileTransferData.Name);

                    Debug.WriteLine("Name: " + fileTransferData.Name + " Path: " + fileTransferData.Path);
                }
            }

            fileTransferData.FileStream = File.Create(fileTransferData.Path);
            originalLength = fileTransferData.Length;

            progressBar.Visibility = Visibility.Visible;
            cancelButton.Visibility = Visibility.Visible;
            yesButton.Visibility = Visibility.Hidden;
            noButton.Visibility = Visibility.Hidden;

            cts = new CancellationTokenSource();
            var progressIndicator = new Progress<double>(ReportProgress);

            try
            {
                await ReceiveFileAsync(progressIndicator, cts.Token);
            }
            catch (Exception)
            {
                Console.WriteLine("Cancellation requested!");
            }

            this.Close();
        }

        private void No_Button_Click(object sender, RoutedEventArgs e)
        {
            client.CancelReceiving(fileTransferData);
            tcpClient.Dispose();
            this.Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
        }

        private void ReportProgress(double value)
        {
            progressBar.Value = value;
        }

        private async Task ReceiveFileAsync(IProgress<double> progressIndicator, CancellationToken token)
        {
            await Task.Run(async () => // async put so that the exception is thrown to the caller
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    while (fileTransferData.Length > 0)
                    {
                        client.Receive(fileTransferData);
                        token.ThrowIfCancellationRequested();

                        Thread.Sleep(500); // TODO: Waiting for testing purposes

                        progressIndicator.Report(100 - ((float) fileTransferData.Length / originalLength * 100));
                    }

                    client.EndReceiving(fileTransferData);
                }
                catch (OperationCanceledException)
                {
                    client.CancelReceiving(fileTransferData);
                    throw;
                }
            }, token);
        }


    }
}
