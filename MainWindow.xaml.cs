using Microsoft.Win32;
using System.Windows;

namespace WartalesEditor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "JSON (*.json;*.cdb)|*.json;*.cdb|All Files (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                StatusText.Text = $"Loaded: {dialog.FileName}";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Save functionality coming in Phase 1.");
        }
    }
}