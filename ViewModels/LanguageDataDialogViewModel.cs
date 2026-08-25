using System;
using WartalesEditor.Helpers;
using WartalesEditor.Models;

namespace WartalesEditor.ViewModels;

public sealed class LanguageDataDialogViewModel :
    ObservableObject
{
    private LanguageDataState state;

    public LanguageDataDialogViewModel(
        LanguageDataState state)
    {
        this.state =
            state
            ?? throw new ArgumentNullException(
                nameof(state));
    }

    public bool IsAvailable =>
        state.IsAvailable;

    public bool IsUnavailable =>
        !state.IsAvailable;

    public string Heading =>
        state.Availability switch
        {
            LanguageDataAvailability.Available =>
                "Wartales language data is ready",

            LanguageDataAvailability.Invalid =>
                "Stored language data could not be used",

            _ =>
                "Wartales language data is not set up"
        };

    public string Explanation =>
        state.IsAvailable
            ? "Localized Wartales names are loaded automatically whenever the editor starts."
            : "Select a Wartales export language file to add localized names. Internal IDs remain available without it.";

    public string ActionText =>
        state.IsAvailable
            ? "Replace Language Data..."
            : "Select Language Data...";

    public string LanguageCode =>
        state.Metadata?.LanguageCode
        ?? string.Empty;

    public string MappingCount =>
        state.IsAvailable
            ? $"{state.MappingCount:N0} localized names"
            : string.Empty;

    public string Version =>
        state.Metadata?.Version
        ?? string.Empty;

    public string Revision =>
        state.Metadata?.Revision
        ?? string.Empty;

    public string Date =>
        state.Metadata?.Date
        ?? string.Empty;

    public string DiagnosticSummary
    {
        get
        {
            if (!state.IsAvailable)
            {
                return state.FailureMessage;
            }

            string[] details =
            {
                string.IsNullOrWhiteSpace(Version)
                    ? string.Empty
                    : $"Version {Version}",

                string.IsNullOrWhiteSpace(Revision)
                    ? string.Empty
                    : $"Revision {Revision}",

                Date
            };

            return string.Join(
                " · ",
                Array.FindAll(
                    details,
                    detail =>
                        !string.IsNullOrWhiteSpace(
                            detail)));
        }
    }

    public void Refresh(
        LanguageDataState newState)
    {
        state =
            newState
            ?? throw new ArgumentNullException(
                nameof(newState));

        OnPropertyChanged(string.Empty);
    }
}
