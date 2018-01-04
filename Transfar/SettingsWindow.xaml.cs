﻿using System;
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

            // The event handlers are detached and reattached when setting the values from the settings xml
            autoAcceptCheckBox.Checked -= autoAcceptCheckBox_Checked;
            autoReplaceCheckBox.Checked -= autoReplaceCheckBox_Checked;
            autoAcceptCheckBox.Checked -= autoAcceptCheckBox_Unchecked;
            autoReplaceCheckBox.Checked -= autoReplaceCheckBox_Unchecked;
            autoAcceptCheckBox.IsChecked = Properties.Settings.Default.AutoAccept;
            autoReplaceCheckBox.IsChecked = Properties.Settings.Default.AutoReplace;
            autoAcceptCheckBox.Checked += autoAcceptCheckBox_Checked;
            autoReplaceCheckBox.Checked += autoReplaceCheckBox_Checked;
            autoAcceptCheckBox.Checked += autoAcceptCheckBox_Unchecked;
            autoReplaceCheckBox.Checked += autoReplaceCheckBox_Unchecked;
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
            // Create the OpenFileDialog object
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                DirectoryPath = dialog.SelectedPath;
                System.Console.WriteLine(dialog.SelectedPath);
            }
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
       
        private void autoAcceptCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoAccept = true;
            applyButton.IsEnabled = true;
        }

        private void autoReplaceCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoReplace = true;
            applyButton.IsEnabled = true;
        }

        private void autoAcceptCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoAccept = false;
            applyButton.IsEnabled = true;
        }

        private void autoReplaceCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoReplace = false;
            applyButton.IsEnabled = true;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // TODO: Aggiungere checkbox per l'accettazione automatica dei trasferimenti ed aggiungere checkbox per la sostituzione automatica dei file in caso di nome identico
    }
}