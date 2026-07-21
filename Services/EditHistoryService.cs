using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

public sealed class EditHistoryService
{
    private readonly Stack<IEditAction> undoStack =
        new();

    private readonly Stack<IEditAction> redoStack =
        new();

    private int recordingSuppressionDepth;

    public event EventHandler? HistoryChanged;

    public bool IsApplyingHistory { get; private set; }

    public bool IsRecordingSuppressed =>
        recordingSuppressionDepth > 0;

    public bool CanUndo =>
        undoStack.Count > 0;

    public bool CanRedo =>
        redoStack.Count > 0;

    public string? UndoDescription =>
        CanUndo
            ? undoStack.Peek().Description
            : null;

    public string? RedoDescription =>
        CanRedo
            ? redoStack.Peek().Description
            : null;

    public IDisposable SuppressRecording()
    {
        recordingSuppressionDepth++;

        return new HistoryRecordingSuppressionScope(
            this);
    }

    public void Record(
        PropertyModel property,
        JToken previousValue,
        JToken newValue)
    {
        ArgumentNullException.ThrowIfNull(
            property);

        ArgumentNullException.ThrowIfNull(
            previousValue);

        ArgumentNullException.ThrowIfNull(
            newValue);

        if (IsApplyingHistory ||
            IsRecordingSuppressed ||
            JToken.DeepEquals(
                previousValue,
                newValue))
        {
            return;
        }

        Record(
            new PropertyEditAction(
                property,
                previousValue,
                newValue));
    }

    public void Record(
        IEditAction action)
    {
        ArgumentNullException.ThrowIfNull(
            action);

        if (IsApplyingHistory ||
            IsRecordingSuppressed)
        {
            return;
        }

        undoStack.Push(
            action);

        redoStack.Clear();

        OnHistoryChanged();
    }

    public bool Undo()
    {
        if (!CanUndo)
        {
            return false;
        }

        IEditAction action =
            undoStack.Pop();

        ExecuteHistoryAction(
            action.Undo);

        redoStack.Push(
            action);

        OnHistoryChanged();

        return true;
    }

    public bool Redo()
    {
        if (!CanRedo)
        {
            return false;
        }

        IEditAction action =
            redoStack.Pop();

        ExecuteHistoryAction(
            action.Redo);

        undoStack.Push(
            action);

        OnHistoryChanged();

        return true;
    }

    public void Clear()
    {
        if (!CanUndo &&
            !CanRedo)
        {
            return;
        }

        undoStack.Clear();
        redoStack.Clear();

        OnHistoryChanged();
    }

    private void EndRecordingSuppression()
    {
        if (recordingSuppressionDepth <= 0)
        {
            throw new InvalidOperationException(
                "Edit history recording suppression " +
                "ended without a matching suppression scope.");
        }

        recordingSuppressionDepth--;
    }

    private void ExecuteHistoryAction(
        Action action)
    {
        IsApplyingHistory =
            true;

        try
        {
            action();
        }
        finally
        {
            IsApplyingHistory =
                false;
        }
    }

    private void OnHistoryChanged()
    {
        HistoryChanged?.Invoke(
            this,
            EventArgs.Empty);
    }

    private sealed class
        HistoryRecordingSuppressionScope :
            IDisposable
    {
        private EditHistoryService? historyService;

        public HistoryRecordingSuppressionScope(
            EditHistoryService historyService)
        {
            this.historyService =
                historyService
                ?? throw new ArgumentNullException(
                    nameof(historyService));
        }

        public void Dispose()
        {
            EditHistoryService? service =
                historyService;

            if (service == null)
            {
                return;
            }

            historyService = null;

            service.EndRecordingSuppression();
        }
    }
}