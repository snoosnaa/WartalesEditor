using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class ProfileManagerWindow : Window
{
    private ProfileManagerViewModel? subscribedViewModel;

    public ProfileManagerWindow()
    {
        InitializeComponent();

        DataContextChanged +=
            OnDataContextChanged;

        Loaded +=
            OnWindowLoaded;

        Closed +=
            OnWindowClosed;
    }

    private void OnDataContextChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        UnsubscribeFromViewModel();

        subscribedViewModel =
            e.NewValue as ProfileManagerViewModel;

        if (subscribedViewModel != null)
        {
            subscribedViewModel.PropertyChanged +=
                OnViewModelPropertyChanged;
        }

        SynchronizeSelectedProfile();
    }

    private void OnWindowLoaded(
        object sender,
        RoutedEventArgs e)
    {
        SynchronizeSelectedProfile();
    }

    private void OnWindowClosed(
        object? sender,
        EventArgs e)
    {
        UnsubscribeFromViewModel();

        DataContextChanged -=
            OnDataContextChanged;

        Loaded -=
            OnWindowLoaded;

        Closed -=
            OnWindowClosed;
    }

    private void OnViewModelPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
            nameof(ProfileManagerViewModel.SelectedProfile))
        {
            SynchronizeSelectedProfile();
        }
    }

    private void SynchronizeSelectedProfile()
    {
        Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            new Action(() =>
            {
                if (subscribedViewModel?.SelectedProfile == null)
                {
                    ProfileGrid.SelectedItem =
                        null;

                    return;
                }

                ProfileGrid.SelectedItem =
                    subscribedViewModel.SelectedProfile;

                ProfileGrid.ScrollIntoView(
                    subscribedViewModel.SelectedProfile);
            }));
    }

    private void UnsubscribeFromViewModel()
    {
        if (subscribedViewModel != null)
        {
            subscribedViewModel.PropertyChanged -=
                OnViewModelPropertyChanged;
        }

        subscribedViewModel =
            null;
    }

    private void OnProfileGridMouseDoubleClick(
        object sender,
        MouseButtonEventArgs e)
    {
        ExecuteApplyCommand();
    }

    private void OnProfileGridPreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        if (ExecuteApplyCommand())
        {
            e.Handled = true;
        }
    }

    private bool ExecuteApplyCommand()
    {
        if (DataContext
            is not ProfileManagerViewModel viewModel)
        {
            return false;
        }

        if (!viewModel.ApplyCommand.CanExecute(null))
        {
            return false;
        }

        viewModel.ApplyCommand.Execute(null);

        return true;
    }

    private void OnCloseButtonClick(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}