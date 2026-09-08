using System;
using System.Windows;
using System.Windows.Controls;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public partial class PathXpRewardsDialog : Window
{
    public event EventHandler<PathXpRewardsApplyEventArgs>? ApplyRequested;
    public event Action<Exception>? DisplayFailed;

    public PathXpRewardsDialog()
    {
        InitializeComponent();
        ContentRendered += OnContentRendered;
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: PathXpRewardsSectionViewModel section } &&
            section.CanApply && section.SelectedOption != null)
            ApplyRequested?.Invoke(this, new PathXpRewardsApplyEventArgs(
                section.PathId, section.SelectedOption.Multiplier));
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: PathXpRewardsSectionViewModel section } &&
            section.CanRestorePreviousValues)
            ApplyRequested?.Invoke(this, new PathXpRewardsApplyEventArgs(
                section.PathId, 1, true));
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        ContentRendered -= OnContentRendered;
        base.OnClosed(e);
    }

    private void OnContentRendered(object? sender, EventArgs e)
    {
        try { Activate(); Focus(); }
        catch (Exception exception) { DisplayFailed?.Invoke(exception); Close(); }
    }
}

public sealed class PathXpRewardsApplyEventArgs : EventArgs
{
    public PathXpRewardsApplyEventArgs(
        string pathId,
        int multiplier,
        bool restorePreviousValues = false)
    {
        PathId = pathId;
        Multiplier = multiplier;
        RestorePreviousValues = restorePreviousValues;
    }
    public string PathId { get; }
    public int Multiplier { get; }
    public bool RestorePreviousValues { get; }
}
