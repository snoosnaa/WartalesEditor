using System;
using System.Runtime.CompilerServices;
using WartalesEditor.Models;

namespace WartalesEditor.Services;

internal static class ProjectObservationSuppressionService
{
    private static readonly ConditionalWeakTable<ProjectModel, ScopeState>
        states = new();

    public static bool IsSuppressed(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        lock (states)
        {
            return states.TryGetValue(project, out ScopeState? state) &&
                   state.Depth > 0;
        }
    }

    public static IDisposable Suppress(ProjectModel project)
    {
        ArgumentNullException.ThrowIfNull(project);
        lock (states)
        {
            ScopeState state = states.GetOrCreateValue(project);
            state.Depth++;
        }

        return new Scope(project);
    }

    private static void Release(ProjectModel project)
    {
        lock (states)
        {
            if (!states.TryGetValue(project, out ScopeState? state) ||
                state.Depth <= 0)
            {
                throw new InvalidOperationException(
                    "Project observation suppression is not active.");
            }

            state.Depth--;
            if (state.Depth == 0)
            {
                states.Remove(project);
            }
        }
    }

    private sealed class ScopeState
    {
        public int Depth { get; set; }
    }

    private sealed class Scope : IDisposable
    {
        private ProjectModel? project;

        public Scope(ProjectModel project)
        {
            this.project = project;
        }

        public void Dispose()
        {
            ProjectModel? current = project;
            if (current == null)
            {
                return;
            }

            project = null;
            Release(current);
        }
    }
}
