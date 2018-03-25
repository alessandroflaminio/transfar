using System;
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
        private FileTransferData fileTransferData;

        private CancellationTokenSource cts;


        // TODO to process here the startsending
        public SendingFileWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
        }

        private void filePickerButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowser fb = new FolderBrowser();
            fb.Description = "Please select a file or folder below:";
            fb.IncludeFiles = true;
            //fb.InitialDirectory = @"C:\";
            if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string filePath = fb.SelectedPath;
                Console.WriteLine("Selected path:" + filePath);

                filePickerButton.Visibility = Visibility.Hidden;
                progressBar.Visibility = Visibility.Visible;
                cancelButton.Visibility = Visibility.Visible;

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
                        client.Receive(fileTransferData);
                        token.ThrowIfCancellationRequested();

                        //Thread.Sleep(500); // Waiting for testing purposes

                        progressIndicator.Report(100 - ((float)fileTransferData.Length / originalLength * 100));
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
