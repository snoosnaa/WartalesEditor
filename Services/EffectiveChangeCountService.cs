using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WartalesEditor.Models;
using WartalesEditor.Models.Profiles;
using WartalesEditor.Models.Snapshots;
using WartalesEditor.Services.Operations;

namespace WartalesEditor.Services;

public sealed class EffectiveChangeCountService
{
    public EffectiveChangeCountService()
        : this(new CampFacilityJsonBuilder())
    {
    }

    public EffectiveChangeCountService(
        CampFacilityJsonBuilder campBuilder)
    {
        ArgumentNullException.ThrowIfNull(campBuilder);
    }

    public int Calculate(ModProfileModel profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return profile.Snapshot.Categories
                .SelectMany(category =>
                    category.Settings.SelectMany(setting =>
                        setting.Properties.Select(property =>
                            CreateIdentity(
                                category.Name,
                                setting.Id,
                                GetPropertyIdentity(property)))))
                .Distinct(StringComparer.Ordinal)
                .Count();
    }

    public int Calculate(
        ProjectModel targetProject,
        ModProfileModel profile,
        out bool isExact)
    {
        ArgumentNullException.ThrowIfNull(targetProject);
        ArgumentNullException.ThrowIfNull(profile);

        GameplayStateObservation[] stateObservations =
            CaptureStateObservations(targetProject.GameplayOperationStates);
        ModificationSnapshotImportResultModel? result = null;
        using IDisposable observationScope =
            ProjectObservationSuppressionService.Suppress(targetProject);
        try
        {
            result = new ModProfileWorkflowService().ApplyProfile(
                targetProject,
                profile);
            isExact = true;
            return Calculate(result.MutationResult);
        }
        catch (ProjectRollbackIntegrityException)
        {
            throw;
        }
        catch
        {
            isExact = false;
            return Calculate(profile);
        }
        finally
        {
            ProjectRollbackIntegrityException? integrityFailure = null;
            if (result?.MutationResult.WasModified == true)
            {
                try
                {
                    new ProjectOperationTransactionService().Rollback(
                        result.MutationResult);
                }
                catch (Exception rollbackException)
                {
                    integrityFailure =
                        new ProjectRollbackIntegrityException(
                            "Profile count evaluation completed, but its " +
                            "temporary changes could not be fully rolled back.",
                            new InvalidOperationException(
                                "The profile evaluation completed before cleanup failed."),
                            rollbackException);
                }
            }

            try
            {
                RestoreStateObservations(
                    targetProject.GameplayOperationStates,
                    stateObservations);
            }
            catch (Exception restorationException)
            {
                integrityFailure = integrityFailure == null
                    ? new ProjectRollbackIntegrityException(
                        "Profile count evaluation could not restore its " +
                        "gameplay-state observations.",
                        new InvalidOperationException(
                            "Profile evaluation cleanup was incomplete."),
                        restorationException)
                    : new ProjectRollbackIntegrityException(
                        "Profile count evaluation encountered more than one " +
                        "cleanup-integrity failure.",
                        integrityFailure,
                        restorationException);
            }

            if (integrityFailure != null)
            {
                throw integrityFailure;
            }
        }
    }

    public int Calculate(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);

        HashSet<string> effectiveChanges = project.Sheets
            .SelectMany(sheet => sheet.Entries.SelectMany(entry =>
                entry.Properties
                    .Where(property => property.IsModified)
                    .Select(property => CreateIdentity(
                        sheet.Name,
                        entry.Id,
                        property.EffectivePropertyPath))))
            .ToHashSet(StringComparer.Ordinal);

        AddCurrentRandomTraitExclusionChanges(
            project,
            effectiveChanges);

        return effectiveChanges.Count;
    }

    public int Calculate(ProjectMutationResult mutationResult)
    {
        ArgumentNullException.ThrowIfNull(mutationResult);

        return mutationResult.CreatedProperties
            .Concat(mutationResult.UpdatedProperties)
            .Concat(mutationResult.RemovedProperties)
            .Distinct()
            .Count();
    }

    public bool HasUnrepresentedRandomTraitExclusionChange(
        ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);

        GameplayOperationStateService stateService = new();
        if (!stateService.IsStateModified(
                project,
                ProgressionType.RandomTraitExclusions))
        {
            return false;
        }

        GameplayOperationStateModel? state =
            project.GameplayOperationStates.FirstOrDefault(candidate =>
                candidate.OperationType ==
                ProgressionType.RandomTraitExclusions);
        if (state == null)
        {
            return false;
        }

        HashSet<string> changed =
            RandomTraitExclusionsService.GetChangedTraitIds(
                project,
                state);
        if (changed.Count == 0)
        {
            return false;
        }

        SheetModel? traitSheet = project.Sheets.FirstOrDefault(sheet =>
            string.Equals(sheet.Name, "trait", StringComparison.Ordinal));

        if (traitSheet == null)
        {
            return true;
        }

        HashSet<string> represented = traitSheet.Entries
            .Where(entry => changed.Contains(entry.Id))
            .Where(entry => entry.Properties.Any(property =>
                string.Equals(
                    property.EffectivePropertyPath,
                    "done",
                    StringComparison.Ordinal) &&
                property.IsModified))
            .Select(entry => entry.Id)
            .ToHashSet(StringComparer.Ordinal);

        return changed.Except(represented).Any();
    }

    private static void AddCurrentRandomTraitExclusionChanges(
        ProjectModel project,
        ISet<string> effectiveChanges)
    {
        GameplayOperationStateService stateService = new();
        if (!stateService.IsStateModified(
                project,
                ProgressionType.RandomTraitExclusions))
        {
            return;
        }

        GameplayOperationStateModel? state =
            project.GameplayOperationStates.FirstOrDefault(candidate =>
                candidate.OperationType ==
                ProgressionType.RandomTraitExclusions);
        if (state == null)
        {
            return;
        }

        foreach (string id in
                 RandomTraitExclusionsService.GetChangedTraitIds(
                     project,
                     state))
        {
            effectiveChanges.Add(
                CreateIdentity("trait", id, "done"));
        }
    }

    private static string GetPropertyIdentity(
        ModificationSnapshotPropertyModel property) =>
        string.IsNullOrWhiteSpace(property.PropertyPath)
            ? property.Name
            : property.PropertyPath;

    private static string CreateIdentity(
        string categoryName,
        string settingId,
        string propertyPath) =>
        $"{categoryName}\u001f{settingId}\u001f{propertyPath}";

    private static GameplayStateObservation[] CaptureStateObservations(
        IEnumerable<GameplayOperationStateModel> states) =>
        states.Select(state => new GameplayStateObservation(
                state.OperationType,
                state.IsCompatible,
                state.CompatibilityMessage,
                state.PersistedStateFingerprint))
            .ToArray();

    private static void RestoreStateObservations(
        System.Collections.ObjectModel.ObservableCollection<
            GameplayOperationStateModel> states,
        IReadOnlyList<GameplayStateObservation> observations)
    {
        if (states.Count != observations.Count)
        {
            throw new InvalidOperationException(
                "Profile count evaluation did not restore gameplay state.");
        }

        for (int targetIndex = 0;
             targetIndex < observations.Count;
             targetIndex++)
        {
            GameplayStateObservation observation = observations[targetIndex];
            int currentIndex = states
                .Select((state, index) => (state, index))
                .Single(pair =>
                    pair.state.OperationType == observation.OperationType)
                .index;
            if (currentIndex != targetIndex)
            {
                states.Move(currentIndex, targetIndex);
            }

            GameplayOperationStateModel state = states[targetIndex];
            state.IsCompatible = observation.IsCompatible;
            state.CompatibilityMessage = observation.CompatibilityMessage;
            state.PersistedStateFingerprint =
                observation.PersistedStateFingerprint;
        }
    }

    private sealed record GameplayStateObservation(
        ProgressionType OperationType,
        bool IsCompatible,
        string CompatibilityMessage,
        string PersistedStateFingerprint);

}
