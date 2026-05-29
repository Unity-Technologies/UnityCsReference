// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Contains predefined command sources and categories for the UI Toolkit authoring system.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
static class CommandSources
{
    /// <summary>
    /// Represents the source of a command. Used to track where commands originate from.
    /// </summary>
    public class CommandSource
    {
    }

    /// <summary>
    /// Command source representing the Inspector window.
    /// </summary>
    public static readonly CommandSource Inspector = new();

    /// <summary>
    /// Command source representing the Hierarchy window.
    /// </summary>
    public static readonly CommandSource Hierarchy = new();

    /// <summary>
    /// Command source representing the StyleSheets window.
    /// </summary>
    public static readonly CommandSource StyleSheets = new();

    /// <summary>
    /// Command source representing the Viewport window.
    /// </summary>
    public static readonly CommandSource Viewport = new();

    /// <summary>
    /// Command source representing the Scene window.
    /// </summary>
    public static readonly CommandSource Scene = new();

    /// <summary>
    /// Command source representing the Uxml Preview window.
    /// </summary>
    public static readonly CommandSource UxmlPreview = new();

    /// <summary>
    /// Command source representing the Uss Preview window.
    /// </summary>
    public static readonly CommandSource UssPreview = new();
}

/// <summary>
/// Represents categories of commands. Commands can override the Category property
/// to be associated with one or more categories, allowing handlers to register on categories
/// instead of specific types. This avoids reflection overhead from type hierarchy walking.
/// Multiple categories can be combined using bitwise OR (e.g., InlineRule | StyleSheetRule).
/// </summary>
[Flags]
internal enum CommandCategory
{
    /// <summary>
    /// No category assigned.
    /// </summary>
    None = 0,

    /// <summary>
    /// Command category for inline style rule-related commands.
    /// Commands in this category modify inline styles on visual elements.
    /// </summary>
    InlineRule = 1 << 0,

    /// <summary>
    /// Command category for style sheet rule-related commands.
    /// Commands in this category modify style sheet rules.
    /// </summary>
    StyleSheetRule = 1 << 1,

    /// <summary>
    /// Command category for styling context related commands.
    /// Commands in this category modify data that would change the set of selectors matching for a given element. For
    /// example, adding or removing a style sheet, change the element's name, adding or removing a selector, etc.
    /// </summary>
    StylingContext = 1 << 2,

    /// <summary>
    /// Command category for highlight related commands.
    /// Commands in this category either request or process highlight items.
    /// </summary>
    Highlight = 1 << 3,

    /// <summary>
    /// Command category for selection related commands.
    /// Commands in this category either request or process selection.
    /// </summary>
    Selection = 1 << 4,
}
