// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace UnityEditor
{
    public sealed partial class Selection
    {
        static readonly EntityId[] k_SingleSelectionCache = new EntityId[1];

        public static System.Action selectionChanged;
        private static DelegateWithPerformanceTracker<System.Action> m_SelectionChangedEvent = new DelegateWithPerformanceTracker<System.Action>($"{nameof(Selection)}.{nameof(selectionChanged)}");
        internal static event System.Action<EntityId> selectedObjectWasDestroyed;
        internal static event System.Action<EntityId> nonSelectedObjectWasDestroyed;

        [PublicAPI] // Used by packages with internal access. Not actually intended for users.
        internal static event System.Action postProcessSelectionMetadata;

        [UsedImplicitly, RequiredByNativeCode]
        private static void Internal_SelectedObjectWasDestroyed(EntityId entityId)
        {
            if (selectedObjectWasDestroyed != null)
                selectedObjectWasDestroyed(entityId);
        }

        [UsedImplicitly, RequiredByNativeCode]
        private static void Internal_NonSelectedObjectWasDestroyed(EntityId entityId)
        {
            if (nonSelectedObjectWasDestroyed != null)
                nonSelectedObjectWasDestroyed(entityId);
        }

        [UsedImplicitly, RequiredByNativeCode]
        private static void Internal_PostProcessSelectionMetadata()
        {
            postProcessSelectionMetadata?.Invoke();
        }

        [UsedImplicitly, RequiredByNativeCode]
        private static void Internal_CallSelectionChanged()
        {
            foreach (var evt in m_SelectionChangedEvent.UpdateAndInvoke(selectionChanged))
                evt();
        }

        internal static void SetSelection(Object[] newSelection, Object newActiveObject = null, Object newActiveContext = null, DataMode newDataModeHint = default)
        {
            SetFullSelection(newSelection, newActiveObject, newActiveContext, newDataModeHint);
        }

        internal static void SetSelection(Object newActiveObject, Object newActiveContext = null, DataMode newDataModeHint = default)
        {
            var activeObjectID = newActiveObject != null ? newActiveObject.GetEntityId() : EntityId.None;
            var activeContextID = newActiveContext != null ? newActiveContext.GetEntityId() : EntityId.None;
            k_SingleSelectionCache[0] = activeObjectID;
            SetFullSelectionByID(k_SingleSelectionCache, activeObjectID, activeContextID, newDataModeHint);
        }

        public static bool Contains(Object obj) { return Contains(obj.GetEntityId()); }

        internal static void Add(EntityId entityId)
        {
            var ids = new List<EntityId>(Selection.entityIds);
            if (ids.IndexOf(entityId) < 0)
            {
                ids.Add(entityId);
                Selection.entityIds = ids.ToArray();
            }
        }

        internal static void Add(Object obj)
        {
            if (obj != null)
                Add(obj.GetEntityId());
        }

        internal static void Remove(EntityId entityId)
        {
            var ids = new List<EntityId>(Selection.entityIds);
            ids.Remove(entityId);
            Selection.entityIds = ids.ToArray();
        }

        internal static void Remove(Object obj)
        {
            if (obj != null)
                Remove(obj.GetEntityId());
        }

        private static IEnumerable GetFilteredInternal(System.Type type, SelectionMode mode)
        {
            if (typeof(Component).IsAssignableFrom(type) || type.IsInterface)
            {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return GetTransforms(mode).Select(t =>
#pragma warning restore RS0030
                {
                    t.TryGetComponent(type, out var component);
                    return component;
                }).Where(c => !ReferenceEquals(c, null));
            }
            else if (typeof(GameObject).IsAssignableFrom(type))
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return GetTransforms(mode).Select(t => t.gameObject);
#pragma warning restore RS0030
            else
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return GetObjectsMode(mode).Where(o => o != null && type.IsAssignableFrom(o.GetType()));
#pragma warning restore RS0030
        }

        public static T[] GetFiltered<T>(SelectionMode mode) // no generic constraint because we also want to allow interfaces
        {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return GetFilteredInternal(typeof(T), mode).Cast<T>().ToArray();
#pragma warning restore RS0030
        }

        public static Object[] GetFiltered(System.Type type, SelectionMode mode)
        {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return GetFilteredInternal(type, mode).Cast<Object>().ToArray();
#pragma warning restore RS0030
        }
    }
}
