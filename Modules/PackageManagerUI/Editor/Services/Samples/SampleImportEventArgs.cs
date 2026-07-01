// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// Describes a single sample that was imported when the <see cref="Sample.OnBeforeImportFinish"/> event is raised.
    /// </summary>
    /// <remarks>
    /// An array of <see cref="SampleImportEventData"/> is passed to subscribers of
    /// <see cref="Sample.OnBeforeImportFinish"/>, with one entry per sample imported in the originating
    /// call to <see cref="Sample.Import"/>. Use the data to react to imports — for example, by logging
    /// the destination path or chaining additional setup work before the Asset Database refresh runs.
    /// </remarks>
    /// <example>
    /// <code>
    /// using UnityEditor;
    /// using UnityEditor.PackageManager.UI;
    /// using UnityEngine;
    ///
    /// public static class SampleImportLogger
    /// {
    ///     [InitializeOnLoadMethod]
    ///     static void Subscribe()
    ///     {
    ///         Sample.OnBeforeImportFinish += data =>
    ///         {
    ///             foreach (var entry in data)
    ///                 Debug.Log($"{entry.packageTechnicalName}: {entry.sampleDisplayName} -> {entry.newImportPath}");
    ///         };
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="Sample.OnBeforeImportFinish"/>
    /// <seealso cref="Sample"/>
    public struct SampleImportEventData
    {
        /// <summary>
        /// The display name of the package sample.
        /// </summary>
        public string sampleDisplayName { get; }
        /// <summary>
        /// The package technical name of the parent package.
        /// </summary>
        public string packageTechnicalName { get; }
        /// <summary>
        /// The full path of where the sample is imported on disk.
        /// </summary>
        public string newImportPath { get; }
        /// <summary>
        /// The full path of a sample on disk before it was updated.
        /// If empty or null, the sample was newly imported rather than updated from a previous import.
        /// </summary>
        public string oldImportPath { get; }

        internal SampleImportEventData(string sampleDisplayName, string packageTechnicalName, string newImportPath, string oldImportPath)
        {
            this.sampleDisplayName = sampleDisplayName;
            this.packageTechnicalName = packageTechnicalName;
            this.newImportPath = newImportPath;
            this.oldImportPath = oldImportPath;
        }
    }
}
