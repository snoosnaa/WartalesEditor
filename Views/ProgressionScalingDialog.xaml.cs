using System;
using System.Diagnostics;
using System.Windows;
using WartalesEditor.Models;
using WartalesEditor.ViewModels;

namespace WartalesEditor.Views;

public sealed class ProgressionApplyRequestedEventArgs :
    EventArgs
{
    public ProgressionApplyRequestedEventArgs(
        ProgressionType progressionType,
        int percentage)
    {
        ProgressionType = progressionType;
        Percentage = percentage;
    }

    public ProgressionType ProgressionType { get; }

    public int Percentage { get; }
}

public sealed class ProgressionBaselineAdoptionRequestedEventArgs :
    EventArgs
{
    public ProgressionBaselineAdoptionRequestedEventArgs(
        ProgressionType progressionType)
    {
        ProgressionType = progressionType;
    }

    public ProgressionType ProgressionType { get; }
}

public partial class ProgressionScalingDialog : Window
{
    public event EventHandler<ProgressionApplyRequestedEventArgs>?
        ApplyRequested;

    public event Action<Exception>?
        DisplayFailed;

    public event EventHandler<
        ProgressionBaselineAdoptionRequestedEventArgs>?
        BaselineAdoptionRequested;

    public ProgressionScalingDialog()
    {
        Trace.WriteLine(
            "XP Progression: before InitializeComponent.");

        InitializeComponent();

        Loaded += OnDialogLoaded;
        ContentRendered += OnDialogContentRendered;
        Closed += OnDialogClosed;

        Trace.WriteLine(
            "XP Progression: after InitializeComponent.");
    }

    private void ApplyCharacterButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is
                ProgressionScalingDialogViewModel viewModel &&
            viewModel.CanApplyCharacter)
        {
            ApplyRequested?.Invoke(
                this,
                new ProgressionApplyRequestedEventArgs(
                    ProgressionType.Character,
                    viewModel.CharacterPercentage));
        }
    }

    private void ApplyProfessionButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is
                ProgressionScalingDialogViewModel viewModel &&
            viewModel.CanApplyProfession)
        {
            ApplyRequested?.Invoke(
                this,
                new ProgressionApplyRequestedEventArgs(
                    ProgressionType.Profession,
                    viewModel.ProfessionPercentage));
        }
    }

    private void AdoptCharacterButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        BaselineAdoptionRequested?.Invoke(
            this,
            new ProgressionBaselineAdoptionRequestedEventArgs(
                ProgressionType.Character));
    }

    private void AdoptProfessionButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        BaselineAdoptionRequested?.Invoke(
            this,
            new ProgressionBaselineAdoptionRequestedEventArgs(
                ProgressionType.Profession));
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }

    private void OnDialogLoaded(
        object sender,
        RoutedEventArgs e)
    {
        Trace.WriteLine(
            $"XP Progression: Loaded at ({Left}, {Top}), " +
            $"size {ActualWidth} x {ActualHeight}.");
    }

    private void OnDialogContentRendered(
        object? sender,
        EventArgs e)
    {
        try
        {
            Trace.WriteLine(
                "XP Progression: ContentRendered.");

            Activate();
            Focus();
        }
        catch (Exception exception)
        {
            Trace.WriteLine(
                "XP Progression: ContentRendered failed: " +
                exception);

            DisplayFailed?.Invoke(
                exception);

            Close();
        }
    }

    private void OnDialogClosed(
        object? sender,
        EventArgs e)
    {
        Loaded -= OnDialogLoaded;
        ContentRendered -= OnDialogContentRendered;
        Closed -= OnDialogClosed;

        Trace.WriteLine(
            "XP Progression: Closed.");
    }
}
