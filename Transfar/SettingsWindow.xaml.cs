using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Transfar
{
    /// <summary>
    /// Logica di interazione per SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        private MainWindow mainWindow;
        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads\\Transfar";

        string _directoryPath;
        public string DirectoryPath
        {
            get
            {
                return this._directoryPath;
            }
            set
            {
                this._directoryPath = value;

                applyButton.IsEnabled = true;
                if (value != defaultPath)
                {
                    resetPathButton.IsEnabled = true;
                    Properties.Settings.Default.Path = value;
                }
                else
                {
                    resetPathButton.IsEnabled = false;
                    Properties.Settings.Default.Path = "";
                }

                OnPropertyChanged("DirectoryPath");
            }
        }

        public SettingsWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            if (Properties.Settings.Default.Path != "")
            {
                _directoryPath = Properties.Settings.Default.Path;
            }
            else _directoryPath = defaultPath;

            InitializeComponent();
            this.DataContext = this; // This is for the binding of the TextBox (Binding Path=DirectoryPath)

            if (DirectoryPath != defaultPath)
            {
                resetPathButton.IsEnabled = true;
            }
            else resetPathButton.IsEnabled = false;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); // Richiama l'evento PropertyChanged dicendo che la proprietà propertyName è stata modificata
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWindow.Show();
        }

        private void filePickerButton_Click(object sender, RoutedEventArgs e)
        {
            // Create the OpenFIleDialog object
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                DirectoryPath = dialog.SelectedPath;
                System.Console.WriteLine(dialog.SelectedPath);
            }
            //Microsoft.Win32.FolderBrowserDialog openPicker = new Microsoft.Win32.OpenFileDialog();

            //// Add file filters
            //// We are using excel files in this example
            //openPicker.DefaultExt = ".xslt";
            //openPicker.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm";

            //// Display the OpenFileDialog by calling ShowDialog method
            //Nullable<bool> result = openPicker.ShowDialog();

            //// Check to see if we have a result 
            //if (result == true)
            //{
            //    // Application now has read/write access to the picked file
            //    // I am saving the file path to a textbox in the UI to display to the user
            //    // as well as a fileDirectory variable to pass to a method
            //    filePathTextBox.Text = openPicker.FileName.ToString();
            //    fileDirectory = openPicker.FileName.ToString();
            //}
            //else
            //{
            //    // Display to the user that the selection process was cancelled
            //    // Not necessary, but helpful when I was debugging the code
            //    OutputTextBlock.Text = "Operation cancelled.";
            //}
        }

        private void applyButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            applyButton.IsEnabled = false;
        }

        private void resetPathButton_Click(object sender, RoutedEventArgs e)
        {
            DirectoryPath = defaultPath;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            applyButton.IsEnabled = false;
            this.Close();
        }

        // TODO: Aggiungere checkbox per l'accettazione automatica dei trasferimenti ed aggiungere checkbox per la sostituzione automatica dei file in caso di nome identico
    }
}
