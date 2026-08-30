using System.Windows;

namespace WartalesEditor.Views;

public partial class GoldenCdbWindow : Window
{
    public event EventHandler? SetCurrentRequested;
    public event EventHandler? SelectRequested;
    public event EventHandler? ImportCurrentWartalesRequested;
    public event EventHandler? LoadRequested;
    public event EventHandler? CompareRequested;
    public event EventHandler? RemoveRequested;

    public GoldenCdbWindow()
    {
        InitializeComponent();
    }

    private void SetCurrentButton_Click(object sender, RoutedEventArgs e) =>
        SetCurrentRequested?.Invoke(this, EventArgs.Empty);

    private void SelectButton_Click(object sender, RoutedEventArgs e) =>
        SelectRequested?.Invoke(this, EventArgs.Empty);

    private void ImportCurrentWartalesButton_Click(
        object sender,
        RoutedEventArgs e) =>
        ImportCurrentWartalesRequested?.Invoke(
            this,
            EventArgs.Empty);

    private void LoadButton_Click(object sender, RoutedEventArgs e) =>
        LoadRequested?.Invoke(this, EventArgs.Empty);

    private void CompareButton_Click(object sender, RoutedEventArgs e) =>
        CompareRequested?.Invoke(this, EventArgs.Empty);

    private void RemoveButton_Click(object sender, RoutedEventArgs e) =>
        RemoveRequested?.Invoke(this, EventArgs.Empty);

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
