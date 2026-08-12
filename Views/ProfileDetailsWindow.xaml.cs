using System;
using System.Windows;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class ProfileDetailsWindow :
    Window
{
    public ProfileDetailsWindow(
        ProfileDetailsViewModel viewModel)
    {
        ViewModel =
            viewModel
            ?? throw new ArgumentNullException(
                nameof(viewModel));

        InitializeComponent();

        DataContext =
            ViewModel;

        Loaded +=
            ProfileDetailsWindow_Loaded;
    }

    public ProfileDetailsViewModel ViewModel
    {
        get;
    }

    private void ProfileDetailsWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        ProfileNameTextBox.Focus();

        ProfileNameTextBox.SelectAll();
    }

    private void ConfirmButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!ViewModel.CanConfirm)
        {
            return;
        }

        DialogResult =
            true;
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult =
            false;
    }

    protected override void OnClosed(
        EventArgs e)
    {
        Loaded -=
            ProfileDetailsWindow_Loaded;

        base.OnClosed(e);
    }
}
