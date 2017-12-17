using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
    /// Logica di interazione per ReceivingFileWindow.xaml
    /// </summary>
    public partial class ReceivingFileWindow : Window
    {
        private TcpClient tcpClient;
        private long originalLength;
        private FileTransferData fileTransferData;

        private CancellationTokenSource cts;

        public ReceivingFileWindow()
        {
            InitializeComponent();
        }

        public ReceivingFileWindow(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
            fileTransferData = Client.StartReceiving(tcpClient);
            originalLength = fileTransferData.Length;

            InitializeComponent();
            fileInfo.Text = fileInfo.Text.Replace("CLIENT_NAME", tcpClient.Client.RemoteEndPoint.ToString()).Replace("FILE_NAME", fileTransferData.Name);
        }

        private async void Yes_Button_Click(object sender, RoutedEventArgs e)
        {
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
            Client.CancelReceiving(fileTransferData);
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
                        Client.Receive(fileTransferData);
                        token.ThrowIfCancellationRequested();

                        Thread.Sleep(500); // Waiting for testing purposes

                        progressIndicator.Report(100 - ((float) fileTransferData.Length / originalLength * 100));
                    }

                    Client.EndReceiving(fileTransferData);
                }
                catch (OperationCanceledException)
                {
                    Client.CancelReceiving(fileTransferData);
                    throw;
                }
            }, token);
        }


    }
}
