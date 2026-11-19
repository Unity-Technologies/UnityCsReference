// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.VersionControl;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Answers about one set of inspected assets, each computed on first use: recomputing them on every layout and repaint
    // event costs hundreds of milliseconds with thousands of selected assets (UUM-148632). The answers are stale once a
    // source they read reports a change after they were created.
    internal sealed partial class InspectedAssets
    {
        // The queries behind the answers; tests substitute them to count calls and change answers without the AssetDatabase.
        internal interface IQueries
        {
            bool CanEditLabels(Object asset);
            bool CanEditAssetBundleName(Object asset);
            HashSet<string> GetAssetBundles(Object[] assets, bool isVariant, out bool isMixed);
        }

        sealed class EditorQueries : IQueries
        {
            public bool CanEditLabels(Object asset) => PropertyEditor.IsOpenForEdit(asset) && Editor.IsAppropriateFileOpenForEdit(asset);
            public bool CanEditAssetBundleName(Object asset) => PropertyEditor.IsOpenForEdit(asset);
            public HashSet<string> GetAssetBundles(Object[] assets, bool isVariant, out bool isMixed) =>
                AssetBundleNameGUI.GetAssetBundlesFromAssets(assets, isVariant, out isMixed);
        }

        [NoAutoStaticsCleanup] // only compared with each instance's own snapshot, and no instance outlives a code reload
        static int s_AssetsVersion;

        [NoAutoStaticsCleanup] // reassigned on every code load
        static DiagnosticSwitch s_ValidateSwitch;

        readonly IQueries m_Queries;
        readonly Func<(int, uint, int)> m_ReadVersions;
        readonly (int, uint, int) m_VersionsAtCreation;

        bool? m_LabelsDisabled;
        bool? m_AssetBundleNameDisabled;
        // Indexed by isVariant.
        readonly HashSet<string>[] m_AssetBundles = new HashSet<string>[2];
        readonly bool[] m_AssetBundlesMixed = new bool[2];

        public Object[] assets { get; }

        public InspectedAssets(Object[] assets)
            : this(assets, null, null)
        {
        }

        internal InspectedAssets(Object[] assets, IQueries queries, Func<(int, uint, int)> readVersions)
        {
            this.assets = assets;
            m_Queries = queries ?? new EditorQueries();
            m_ReadVersions = readVersions ?? CurrentVersions;
            m_VersionsAtCreation = m_ReadVersions();
        }

        // One version per source the answers read: version control status, in-memory importer settings, and
        // imported assets or the project.
        internal static (int statusVersion, uint settingsVersion, int assetsVersion) CurrentVersions() =>
            (VersionControlManager.statusVersion, AssetImporter.settingsVersion, s_AssetsVersion);

        // projectChanged is cleared on code reload, so subscribe on every load.
        [OnCodeLoaded]
        static void Initialize()
        {
            s_ValidateSwitch = Debug.GetDiagnosticSwitch("ValidateInspectedAssetsCache");
            EditorApplication.projectChanged += IncrementAssetsVersion;
        }

        [OnCodeUnloading]
        static void Shutdown() => EditorApplication.projectChanged -= IncrementAssetsVersion;

        // Also called after every import: a reimport that only changes import settings raises no projectChanged.
        internal static void IncrementAssetsVersion() => s_AssetsVersion++;

        public bool IsFor(Object[] assets)
        {
            if (m_ReadVersions() != m_VersionsAtCreation || !HasSameAssets(assets))
                return false;

            // A development aid for finding sources that change without a version: recomputes every cached answer on each use.
            if (isValidating && FindStaleAnswer() is { } staleAnswer)
            {
                Debug.LogWarning($"The Inspector footer's cached {staleAnswer} no longer match the inspected assets: a source changed without moving a version in InspectedAssets.CurrentVersions.");
                return false;
            }

            return true;
        }

        public bool labelsDisabled => m_LabelsDisabled ??= !Array.TrueForAll(assets, m_Queries.CanEditLabels);

        public bool assetBundleNameDisabled => m_AssetBundleNameDisabled ??= !Array.TrueForAll(assets, m_Queries.CanEditAssetBundleName);

        public HashSet<string> GetAssetBundles(bool isVariant, out bool isMixed)
        {
            var slot = isVariant ? 1 : 0;
            m_AssetBundles[slot] ??= m_Queries.GetAssetBundles(assets, isVariant, out m_AssetBundlesMixed[slot]);
            isMixed = m_AssetBundlesMixed[slot];
            return m_AssetBundles[slot];
        }

        internal static bool isValidating
        {
            get => s_ValidateSwitch != null && (bool)s_ValidateSwitch.value;
            set => s_ValidateSwitch.value = value;
        }

        string FindStaleAnswer()
        {
            var actual = new InspectedAssets(assets, m_Queries, m_ReadVersions);
            if (m_LabelsDisabled.HasValue && m_LabelsDisabled != actual.labelsDisabled)
                return "label editability";
            if (m_AssetBundleNameDisabled.HasValue && m_AssetBundleNameDisabled != actual.assetBundleNameDisabled)
                return "AssetBundle editability";
            for (var slot = 0; slot < m_AssetBundles.Length; slot++)
            {
                if (m_AssetBundles[slot] == null)
                    continue;
                var actualBundles = actual.GetAssetBundles(slot == 1, out var actualMixed);
                if (!m_AssetBundles[slot].SetEquals(actualBundles) || m_AssetBundlesMixed[slot] != actualMixed)
                    return slot == 1 ? "AssetBundle variants" : "AssetBundle names";
            }
            return null;
        }

        bool HasSameAssets(Object[] other)
        {
            if (other.Length != assets.Length)
                return false;
            for (var i = 0; i < other.Length; i++)
            {
                if (!ReferenceEquals(other[i], assets[i]))
                    return false;
            }
            return true;
        }
    }
}
