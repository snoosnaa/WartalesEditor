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

    public event EventHandler? HistoryChanged;

    public bool IsApplyingHistory { get; private set; }

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

        if (IsApplyingHistory)
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
            return false;

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
            return false;

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
}