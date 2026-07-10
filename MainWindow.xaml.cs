using System.Windows;
using WartalesEditor.ViewModels;

namespace WartalesEditor;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new();

    public MainWindow()
    {
        InitializeComponent();

        DataContext = ViewModel;
    }
}