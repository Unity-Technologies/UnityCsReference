// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine.Scripting;
using uei = UnityEngine.Internal;

namespace UnityEditor
{
    public sealed partial class AssetDatabase
    {
        // Delegate to be called from [[AssetDatabase.ImportPackage]] callbacks
        public delegate void ImportPackageCallback(string packageName);

        // Delegate to be called from [[AssetDatabase.ImportPackage]] callbacks in the event of failure
        public delegate void ImportPackageFailedCallback(string packageName, string errorMessage);

        // Delegate to be called when package import begins
        public static event ImportPackageCallback importPackageStarted
        {
            add => m_importPackageStartedEvent.Add(value);
            remove => m_importPackageStartedEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<ImportPackageCallback> m_importPackageStartedEvent = new EventWithPerformanceTracker<ImportPackageCallback>($"{nameof(AssetDatabase)}.{nameof(importPackageStarted)}");

        // Delegate to be called when package import completes
        public static event ImportPackageCallback importPackageCompleted
        {
            add => m_importPackageCompletedEvent.Add(value);
            remove => m_importPackageCompletedEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<ImportPackageCallback> m_importPackageCompletedEvent = new EventWithPerformanceTracker<ImportPackageCallback>($"{nameof(AssetDatabase)}.{nameof(importPackageCompleted)}");

        // Called when package import completes, listing the selected items
        public static Action<string[]> onImportPackageItemsCompleted;
        private static DelegateWithPerformanceTracker<Action<string[]>> m_onImportPackageItemsCompleted = new DelegateWithPerformanceTracker<Action<string[]>>($"{nameof(AssetDatabase)}.{nameof(onImportPackageItemsCompleted)}");

        // Delegate to be called when package import is cancelled
        public static event ImportPackageCallback importPackageCancelled
        {
            add => m_importPackageCancelledEvent.Add(value);
            remove => m_importPackageCancelledEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<ImportPackageCallback> m_importPackageCancelledEvent = new EventWithPerformanceTracker<ImportPackageCallback>($"{nameof(AssetDatabase)}.{nameof(importPackageCancelled)}");

        // Delegate to be called when package import fails
        public static event ImportPackageFailedCallback importPackageFailed
        {
            add => m_importPackageFailedEvent.Add(value);
            remove => m_importPackageFailedEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<ImportPackageFailedCallback> m_importPackageFailedEvent = new EventWithPerformanceTracker<ImportPackageFailedCallback>($"{nameof(AssetDatabase)}.{nameof(importPackageFailed)}");

        [RequiredByNativeCode]
        private static void Internal_CallImportPackageStarted(string packageName)
        {
            foreach (var evt in m_importPackageStartedEvent)
                evt(packageName);
        }

        [RequiredByNativeCode]
        private static void Internal_CallImportPackageCompleted(string packageName)
        {
            foreach (var evt in m_importPackageCompletedEvent)
                evt(packageName);
        }

        [RequiredByNativeCode]
        private static void Internal_CallOnImportPackageItemsCompleted(string[] items)
        {
            foreach (var evt in m_onImportPackageItemsCompleted.UpdateAndInvoke(onImportPackageItemsCompleted))
                evt(items);
        }

        [RequiredByNativeCode]
        private static void Internal_CallImportPackageCancelled(string packageName)
        {
            foreach (var evt in m_importPackageCancelledEvent)
                evt(packageName);
        }

        [RequiredByNativeCode]
        private static void Internal_CallImportPackageFailed(string packageName, string errorMessage)
        {
            foreach (var evt in m_importPackageFailedEvent)
                evt(packageName, errorMessage);
        }

        [RequiredByNativeCode]
        private static bool Internal_IsOpenForEdit(string assetOrMetaFilePath)
        {
            return IsOpenForEdit(assetOrMetaFilePath);
        }

        public static void CanOpenForEdit(string[] assetOrMetaFilePaths, List<string> outNotEditablePaths, [uei.DefaultValue("StatusQueryOptions.UseCachedIfPossible")] StatusQueryOptions statusQueryOptions = StatusQueryOptions.UseCachedIfPossible)
        {
            if (assetOrMetaFilePaths == null)
                throw new ArgumentNullException(nameof(assetOrMetaFilePaths));
            if (outNotEditablePaths == null)
                throw new ArgumentNullException(nameof(outNotEditablePaths));
            UnityEngine.Profiling.Profiler.BeginSample("AssetDatabase.CanOpenForEdit");
            AssetModificationProcessorInternal.CanOpenForEdit(assetOrMetaFilePaths, outNotEditablePaths, statusQueryOptions);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public static void IsOpenForEdit(string[] assetOrMetaFilePaths, List<string> outNotEditablePaths, [uei.DefaultValue("StatusQueryOptions.UseCachedIfPossible")] StatusQueryOptions statusQueryOptions = StatusQueryOptions.UseCachedIfPossible)
        {
            if (assetOrMetaFilePaths == null)
                throw new ArgumentNullException(nameof(assetOrMetaFilePaths));
            if (outNotEditablePaths == null)
                throw new ArgumentNullException(nameof(outNotEditablePaths));
            UnityEngine.Profiling.Profiler.BeginSample("AssetDatabase.IsOpenForEdit");
            AssetModificationProcessorInternal.IsOpenForEdit(assetOrMetaFilePaths, outNotEditablePaths, statusQueryOptions);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        [RequiredByNativeCode]
        private static bool Internal_MakeEditable(string path)
        {
            return MakeEditable(path);
        }

        public static bool MakeEditable(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            return MakeEditable(new[] {path});
        }

        [RequiredByNativeCode]
        private static bool Internal_MakeEditable2(string[] paths, string prompt = null, List<string> outNotEditablePaths = null)
        {
            return MakeEditable(paths, prompt, outNotEditablePaths);
        }

        public static bool MakeEditable(string[] paths, string prompt = null, List<string> outNotEditablePaths = null)
        {
            if (paths == null)
                throw new ArgumentNullException(nameof(paths));
            UnityEngine.Profiling.Profiler.BeginSample("AssetDatabase.MakeEditable");
            ChangeSet changeSet = null;
            var result = Provider.HandlePreCheckoutCallback(ref paths, ref changeSet);
            if (result && !AssetModificationProcessorInternal.MakeEditable(paths, prompt, outNotEditablePaths))
                result = false;
            if (result && !Provider.MakeEditableImpl(paths, prompt, changeSet, outNotEditablePaths))
                result = false;
            UnityEngine.Profiling.Profiler.EndSample();
            return result;
        }
    }
}
