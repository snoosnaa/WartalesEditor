using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using WartalesEditor.Helpers;
using WartalesEditor.Models;

namespace WartalesEditor.ViewModels;

public sealed class ChangeSummaryViewModel :
    ObservableObject
{
    private readonly Action<ChangeSummaryItemModel>
        navigateToChange;

    private ChangeSummaryItemModel? selectedItem;

    public ChangeSummaryViewModel(
        IReadOnlyList<ChangeSummaryItemModel> items,
        Action<ChangeSummaryItemModel> navigateToChange)
    {
        ArgumentNullException.ThrowIfNull(items);

        this.navigateToChange =
            navigateToChange
            ?? throw new ArgumentNullException(
                nameof(navigateToChange));

        Items =
            new ObservableCollection<ChangeSummaryItemModel>();

        GroupedItems =
            CollectionViewSource.GetDefaultView(
                Items);

        GroupedItems.GroupDescriptions.Add(
            new PropertyGroupDescription(
                nameof(
                    ChangeSummaryItemModel
                        .CategoryName)));

        GroupedItems.GroupDescriptions.Add(
            new PropertyGroupDescription(
                nameof(
                    ChangeSummaryItemModel
                        .SettingName)));

        NavigateCommand =
            new RelayCommand(
                _ => NavigateToSelectedItem(),
                _ => SelectedItem != null);

        Refresh(items);
    }

    public ObservableCollection<ChangeSummaryItemModel>
        Items
    {
        get;
    }

    public ICollectionView GroupedItems
    {
        get;
    }

    public ChangeSummaryItemModel? SelectedItem
    {
        get => selectedItem;
        set
        {
            if (!SetProperty(
                    ref selectedItem,
                    value))
            {
                return;
            }

            NavigateCommand
                .NotifyCanExecuteChanged();
        }
    }

    public string Header =>
        Items.Count == 1
            ? "1 Modified Property"
            : $"{Items.Count:N0} Modified Properties";

    public bool HasChanges =>
        Items.Count > 0;

    public RelayCommand NavigateCommand
    {
        get;
    }

    public void Refresh(
        IReadOnlyList<ChangeSummaryItemModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        ChangeSummaryItemModel? previousSelection =
            SelectedItem;

        Items.Clear();

        foreach (ChangeSummaryItemModel item in items)
        {
            Items.Add(item);
        }

        SelectedItem =
            previousSelection == null
                ? null
                : FindMatchingItem(
                    previousSelection);

        OnPropertyChanged(nameof(Header));
        OnPropertyChanged(nameof(HasChanges));

        NavigateCommand
            .NotifyCanExecuteChanged();
    }

    private void NavigateToSelectedItem()
    {
        if (SelectedItem == null)
            return;

        navigateToChange(
            SelectedItem);
    }

    private ChangeSummaryItemModel? FindMatchingItem(
        ChangeSummaryItemModel previousSelection)
    {
        foreach (ChangeSummaryItemModel item in Items)
        {
            if (ReferenceEquals(
                    item.Property,
                    previousSelection.Property))
            {
                return item;
            }
        }

        return null;
    }
}