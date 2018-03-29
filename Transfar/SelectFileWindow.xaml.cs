using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Transfar
{
    /// <summary>
    /// Logica di interazione per SelectFileToSendWindow.xaml
    /// </summary>
    public partial class SelectFileWindow : Window
    {
        // TODO: you should check what happens when a transfer is cancelled to the other party
        private Server server;
        private IPEndPoint selectedClient;

        private CancellationTokenSource cts;


        public SelectFileWindow(Server server, IPEndPoint selectedClient)
        {
            this.server = server;
            this.selectedClient = selectedClient;

            InitializeComponent();
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

                SendingFileWindow sendingFileWindow = new SendingFileWindow(server, selectedClient, filePath);

                this.Close();
            }
        }
    }
}
