// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

/// <summary>
/// Associates a <see cref="ResourceEntryDrawer"/> with the entry type it renders.
/// </summary>
/// <remarks>
/// Apply this attribute to a <see cref="ResourceEntryDrawer"/> subclass to register it for a concrete
/// <see cref="IResourceEntry"/> type. The Resource Tables window discovers the drawer through
/// <see cref="UnityEditor.TypeCache"/>, so a custom entry kind renders its value cell with no manual registration. The
/// drawer for the nearest base type is used when no exact match exists.
/// </remarks>
/// <seealso cref="ResourceEntryDrawer"/>
/// <seealso cref="IResourceEntry"/>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ResourceEntryDrawerAttribute : Attribute
{
    /// <summary>
    /// The entry type this drawer renders.
    /// </summary>
    public Type EntryType { get; }

    /// <summary>
    /// Associates the drawer with an entry type.
    /// </summary>
    /// <param name="entryType">The concrete entry type the drawer renders.</param>
    public ResourceEntryDrawerAttribute(Type entryType) => EntryType = entryType;
}

/// <summary>
/// Carries the state and callbacks a <see cref="ResourceEntryDrawer"/> needs while it builds a value cell.
/// </summary>
/// <remarks>
/// The window creates one of these per cell and passes it to <see cref="ResourceEntryDrawer.CreateCell"/>. Bind a field
/// to <see cref="EntryProperty"/> for free undo and redo when it is set; otherwise edit the entry directly and call
/// <see cref="MarkDirty"/>. Call <see cref="Rebuild"/> after a change that alters the row's structure.
/// </remarks>
/// <seealso cref="ResourceEntryDrawer"/>
public sealed class ResourceEntryContext
{
    /// <summary>
    /// The table whose locale value this cell edits.
    /// </summary>
    public ResourceTable Table { get; internal set; }

    /// <summary>
    /// The serialized array element for this entry, for binding, or <see langword="null"/> when it is unavailable.
    /// </summary>
    public SerializedProperty EntryProperty { get; internal set; }

    /// <summary>
    /// Marks the table dirty after a value change made outside a serialized-property binding.
    /// </summary>
    public Action MarkDirty { get; internal set; }

    /// <summary>
    /// Rebuilds the tree after a change to the entry's structure.
    /// </summary>
    public Action Rebuild { get; internal set; }
}

/// <summary>
/// Renders the value cell for one <see cref="IResourceEntry"/> kind within a locale column.
/// </summary>
/// <remarks>
/// Subclass this and mark the subclass with <see cref="ResourceEntryDrawerAttribute"/> to add a custom entry kind to
/// the table editor. The drawer builds only the value editor; the window owns the tree structure, the variant child
/// rows, and the per-row action menu. Read <see cref="ResourceEntryContext.EntryProperty"/> to bind a field to the
/// serialized value, or edit the entry directly and call <see cref="ResourceEntryContext.MarkDirty"/>.
/// </remarks>
/// <seealso cref="ResourceEntryDrawerAttribute"/>
/// <seealso cref="ResourceEntryContext"/>
/// <seealso cref="IResourceEntry"/>
public abstract partial class ResourceEntryDrawer
{
    /// <summary>
    /// Builds the visual element that edits the entry's value.
    /// </summary>
    /// <param name="entry">The entry to edit.</param>
    /// <param name="context">The table, serialized property, and callbacks for this cell.</param>
    /// <returns>The visual element that edits the value.</returns>
    public abstract VisualElement CreateCell(IResourceEntry entry, ResourceEntryContext context);

    [AutoStaticsCleanup] // keyed by Type and holds drawer instances; both go stale on reload
    static Dictionary<Type, ResourceEntryDrawer> s_Drawers;
    [NoAutoStaticsCleanup] // stateless fallback; a readonly field cannot be reassigned anyway
    static readonly ResourceEntryDrawer s_Fallback = new FallbackEntryDrawer();
    // Resolved base-type walks, cached because Get runs per cell while the table scrolls.
    [AutoStaticsCleanup] // caches Type handles a reload invalidates
    static readonly Dictionary<Type, ResourceEntryDrawer> s_Resolved = new();

    internal static ResourceEntryDrawer Get(Type entryType)
    {
        if (s_Resolved.TryGetValue(entryType, out var cached))
            return cached;
        s_Drawers ??= BuildRegistry();
        var type = entryType;
        while (type != null && type != typeof(object))
        {
            if (s_Drawers.TryGetValue(type, out var drawer))
                return s_Resolved[entryType] = drawer;
            type = type.BaseType;
        }
        return s_Resolved[entryType] = s_Fallback;
    }

    static Dictionary<Type, ResourceEntryDrawer> BuildRegistry()
    {
        var map = new Dictionary<Type, ResourceEntryDrawer>();
        foreach (var type in TypeCache.GetTypesWithAttribute<ResourceEntryDrawerAttribute>())
        {
            // Skip a custom drawer with no parameterless constructor rather than throw during discovery.
            if (type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)
                continue;
            var attribute = (ResourceEntryDrawerAttribute)Attribute.GetCustomAttribute(type, typeof(ResourceEntryDrawerAttribute));
            if (attribute?.EntryType != null)
                map[attribute.EntryType] = (ResourceEntryDrawer)Activator.CreateInstance(type);
        }
        return map;
    }

    sealed class FallbackEntryDrawer : ResourceEntryDrawer
    {
        public override VisualElement CreateCell(IResourceEntry entry, ResourceEntryContext context)
        {
            var label = new Label(entry?.GetType().Name ?? "<null>");
            label.AddToClassList(LocClasses.LocTextSecondary);
            return label;
        }
    }
}
