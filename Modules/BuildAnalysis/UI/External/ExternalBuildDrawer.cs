// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    /// <summary>Draws the report for builds made outside the Unity build pipeline, in the <b>Build Analysis</b> window.</summary>
    /// <remarks>Exists only for the Addressables package, which has access through InternalsVisibleTo.
    /// Mark an implementation with <see cref="ExternalBuildDrawerAttribute"/> to declare which package's
    /// builds it draws. The window gives the drawer its whole content area, so the drawer supplies its own tabs
    /// and toolbar.</remarks>
    /// <seealso cref="BuildHistory.RegisterExternalBuild"/>
    internal abstract class ExternalBuildDrawer
    {
        /// <summary>Creates the content that fills the window while this drawer's builds are selected.</summary>
        /// <returns>The root of the content. Must not be `null`.</returns>
        public abstract VisualElement CreateContent();

        /// <summary>Called when the user selects a build this drawer draws.</summary>
        /// <param name="summary">The summary of the selected build.</param>
        /// <remarks>The window shows no report of its own for these builds, so report anything that prevents
        /// drawing one, such as a missing report file, in the drawer's own content.</remarks>
        public virtual void OnBuildSelected(BuildReportSummary summary)
        {
        }

        /// <summary>Loads a selected build's report without blocking the editor.</summary>
        /// <param name="summary">The summary of the selected build.</param>
        /// <param name="token">Cancelled when the window closes or scripts reload.</param>
        /// <returns>The report, passed to <see cref="ApplyBuild"/>.</returns>
        /// <remarks>Called on the main thread; do the slow work in `Task.Run`. Show a loading state here and hide
        /// it in <see cref="ApplyBuild"/> and <see cref="OnBuildDeselected"/>.</remarks>
        public virtual Task<object> LoadBuildAsync(BuildReportSummary summary, CancellationToken token)
        {
            return Task.FromResult<object>(null);
        }

        /// <summary>Shows a loaded report, while its build is still selected.</summary>
        /// <param name="summary">The summary of the selected build.</param>
        /// <param name="loaded">The result of <see cref="LoadBuildAsync"/>.</param>
        /// <returns>Whether a report is shown. If not, selecting the build again retries, as for other builds in
        /// the window that have no report.</returns>
        public virtual bool ApplyBuild(BuildReportSummary summary, object loaded)
        {
            OnBuildSelected(summary);
            return true;
        }

        /// <summary>Called when the selection moves to a build this drawer doesn't draw.</summary>
        public virtual void OnBuildDeselected()
        {
        }

        /// <summary>Called when the build history changes while one of this drawer's builds is selected.</summary>
        public virtual void OnBuildListRefreshed()
        {
        }
    }

    /// <summary>Declares the package whose builds an <see cref="ExternalBuildDrawer"/> draws.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal sealed class ExternalBuildDrawerAttribute : Attribute
    {
        /// <param name="producerPackage">The package name, for example `com.unity.addressables`. The window draws a
        /// build with this drawer when the build's <see cref="BuildReportSummary.ProducerPackage"/> matches.</param>
        public ExternalBuildDrawerAttribute(string producerPackage)
        {
            ProducerPackage = producerPackage;
        }

        /// <summary>The package whose builds the drawer draws.</summary>
        public string ProducerPackage { get; }
    }
}
