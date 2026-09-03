// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Core.Parsing;
using Unity.SmartStrings.Core.Settings;
using Unity.Scripting.LifecycleManagement;
using Unity.SmartStrings.Pooling.SmartPools;
namespace Unity.SmartStrings.Extensions;

/// <summary>
/// Formats each item individually when the source value is an array (or supports ICollection).
/// Syntax:
/// #1: "format|spacer"
/// #2: "format|spacer|last spacer"
/// #3: "format|spacer|last spacer|two spacer"
/// The format will be used for each item in the collection, the spacer will be between all items, and the last spacer
/// will replace the spacer for the last item only.
/// Example:
/// CustomFormat("{Dates:D|; |; and }", {#1/1/2000#, #12/31/2999#, #9/9/9999#}) = "January 1, 2000; December 31, 2999;
/// and September 9, 9999"
/// In this example, format = "D", spacer = "; ", and last spacer = "; and "
/// Advanced:
/// Composite Formatting is allowed in the format by using nested braces.
/// If a nested item is detected, Composite formatting will be used.
/// Example:
/// CustomFormat("{Sizes:{Width}x{Height}|, }", {new Size(4,3), new Size(16,9)}) = "4x3, 16x9"
/// In this example, format = "{Width}x{Height}".  Notice the nested braces.
/// </summary>
/// <remarks>
/// The <see cref="ListFormatter"/> PluralLocalizationExtension and ConditionalExtension
/// </remarks>
[Serializable]
public class ListFormatter : FormatterBase, ISource, IInitializer, IFormatterLiteralExtractor
{
    SmartSettings m_SmartSettings;

    [Tooltip("The character used to split the option text literals. Valid characters are: | (pipe) , (comma)  ~ (tilde)")]
    [SerializeField] char m_SplitChar = '|';

    /// <inheritdoc/>
    public override string DefaultName => "list";

    /// <summary>
    /// The character used to split the option text literals.
    /// Valid characters are: | (pipe) , (comma)  ~ (tilde)
    /// </summary>
    public char SplitChar
    {
        get => m_SplitChar;
        set => m_SplitChar = Utilities.Validation.GetValidSplitCharOrThrow(value);
    }

    /// <summary>
    /// Creates a new instance of the formatter.
    /// </summary>
    public ListFormatter()
    {
        CanAutoDetect = true;
    }

    /// <summary>
    /// Allows an integer to be used as a selector to index an array (or list).
    /// This is better described using an example:
    /// CustomFormat("{Dates.2.Year}", {#1/1/2000#, #12/31/2999#, #9/9/9999#}) = "9999"
    /// The ".2" selector is used to reference Dates[2].
    /// </summary>
    public bool TryEvaluateSelector(ISelectorInfo selectorInfo)
    {
        var current = selectorInfo.CurrentValue;
        var selector = selectorInfo.SelectorText;

        if (current is not IList currentList) return false;

        // See if we're trying to access a specific index:
        var isAbsolute = selectorInfo.SelectorIndex == 0 && selectorInfo.SelectorOperator.Length == 0;
        if (!isAbsolute && int.TryParse(selector, out var itemIndex) &&
            itemIndex < currentList.Count)
        {
            // The current is a List, and the selector is a number
            // let's return the List item:
            // Example: {People[2].Name}
            //           ^List  ^itemIndex
            selectorInfo.Result = currentList[itemIndex];
            return true;
        }

        // We want to see if there is an "Index" property that was supplied.
        if (selector.Equals("index", StringComparison.OrdinalIgnoreCase))
        {
            // Looking for "{Index}"
            if (selectorInfo.SelectorIndex == 0)
            {
                selectorInfo.Result = CollectionIndex;
                return true;
            }

            // Looking for 2 lists to sync: "{List1: {List2[Index]} }"
            if (0 <= CollectionIndex && CollectionIndex < currentList.Count)
            {
                selectorInfo.Result = currentList[CollectionIndex];
                return true;
            }
        }

        return false;
    }

    // Note: CollectionIndex must be initialized ONLY ONCE, NOT once per thread (aka ThreadStatic).
    // Note: Multi threading support generates some garbage
    [NoAutoStaticsCleanup] // must be initialized only once (see note above); holds only ints
    static readonly AsyncLocal<int> s_CollectionIndexThreadSafe = new();
    [NoAutoStaticsCleanup] // per-format scratch index; plain value
    static int s_CollectionIndexSingleThread = -1;

    /// <summary>
    /// Gets, whether the <see cref="ListFormatter"/> is in thread safe mode.
    /// </summary>
    [NoAutoStaticsCleanup] // plain-value mode flag
    internal static bool IsThreadSafeMode { get; set; } = SmartSettings.IsThreadSafeMode;

    /// <summary>
    /// Gets or sets the collection index.
    /// </summary>
    /// <remarks>
    /// In thread safe mode, <see cref="CollectionIndex"/> wraps an <see cref="AsyncLocal{T}"/>,
    /// while in single thread mode, a static <see langword="int"/> ist wrapped.
    /// </remarks>
    static int CollectionIndex
    {
        get => IsThreadSafeMode
        ? s_CollectionIndexThreadSafe.Value
        : s_CollectionIndexSingleThread;

        set
        {
            if (IsThreadSafeMode)
                s_CollectionIndexThreadSafe.Value = value;
            else s_CollectionIndexSingleThread = value;
        }
    }

    /// <summary>
    /// Writes the given <see cref="IFormattingInfo"/> to the <see cref="Core.Output.IOutput"/>
    /// if it can be processed by the formatter.
    /// </summary>
    /// <param name="formattingInfo"><see cref="IFormattingInfo"/> to process.</param>
    /// <returns>Returns <see langword="true"/> if processing was possible, else <see langword="false"/>.</returns>
    public override bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        var format = formattingInfo.Format;
        var current = formattingInfo.CurrentValue;

        // If the ListFormatter is called explicitly, null with nullable must be handled here
        if (current is null && HasNullableOperator(formattingInfo))
        {
            formattingInfo.Write(string.Empty);
            return true;
        }

        // See if the format string contains un-nested "|":
        using var splitListPooledObject = SplitListPool.Pool.Get(out var splitList);
        var parameters = (SplitList)(format is not null ? format.Split(SplitChar, 4) : splitList);

        // Check whether arguments can be handled by this formatter
        if (format is null || parameters.Count < 2 || current is not IEnumerable currentAsEnumerable
            || currentAsEnumerable is string or IFormattable)
        {
            // Auto detection calls just return a failure to evaluate
            if (string.IsNullOrEmpty(formattingInfo.Placeholder?.FormatterName))
                return false;

            // throw, if the formatter has been called explicitly
            throw new FormatException($"Formatter named '{formattingInfo.Placeholder?.FormatterName}' requires an IEnumerable argument and at least 2 format parameters.");
        }

        // Grab all formatting options:
        // They must be in one of these formats:
        // itemFormat|spacer
        // itemFormat|spacer|lastSpacer
        // itemFormat|spacer|lastSpacer|twoSpacer
        var itemFormat = parameters[0];
        var spacer = parameters[1];
        var lastSpacer = parameters.Count >= 3 ? parameters[2] : spacer;
        var twoSpacer = parameters.Count >= 4 ? parameters[3] : lastSpacer;

        // A non-nested item format is wrapped in a pooled placeholder that we own and release at the end.
        Format wrappedItemFormat = null;
        Placeholder wrappedPlaceholder = null;

        if (!itemFormat.HasNested)
        {
            // The format is not nested,
            // so we will treat it as an ItemFormat:
            var newItemFormat = FormatPool.Pool.Get().Initialize(m_SmartSettings, itemFormat.BaseString,
                itemFormat.StartIndex, itemFormat.EndIndex, true);

            var newPlaceholder = PlaceholderPool.Pool.Get().Initialize(newItemFormat, itemFormat.StartIndex, 0);
            newPlaceholder.Format = itemFormat;
            newPlaceholder.EndIndex = itemFormat.EndIndex;
            // inherit alignment
            newPlaceholder.Alignment = formattingInfo.Alignment;

            newItemFormat.Items.Add(newPlaceholder);
            itemFormat = newItemFormat;
            wrappedItemFormat = newItemFormat;
            wrappedPlaceholder = newPlaceholder;
        }

        // Let's buffer all items from the enumerable (to ensure the Count without double-enumeration):
        List<object> itemsAsList = null;
        if (currentAsEnumerable is not ICollection items)
        {
            itemsAsList = UnityEngine.Pool.ListPool<object>.Get();
            foreach (var item in currentAsEnumerable)
                itemsAsList.Add(item);

            items = itemsAsList;
        }

        // In case we have nested arrays, we might need to restore the CollectionIndex
        var savedCollectionIndex = CollectionIndex;
        CollectionIndex = -1;

        // Do not inherit alignment for the spacers - use new FormattingInfo
        var spacerFormattingInfo = FormattingInfoPool.Pool.Get()
            .Initialize(null, formattingInfo.FormatDetails, format, null);
        spacerFormattingInfo.Alignment = 0;

        // Note:
        // Give spacers the data context of the root parent.
        // formattingInfo.CurrentValue from the argument to
        // TryEvaluateFormat(IFormattingInfo formattingInfo) only contains the list elements.
        var rootParent = (formattingInfo as Core.Formatting.FormattingInfo);
        do
        {
            rootParent = rootParent?.Parent;
        }
        while (rootParent?.Parent != null);

        var rootParentValue = rootParent?.CurrentValue;

        foreach (var item in items)
        {
            CollectionIndex += 1; // Keep track of the index

            // Determine which spacer to write:
            if (spacer == null || CollectionIndex == 0)
            {
                // Don't write the spacer.
            }
            else if (CollectionIndex < items.Count - 1)
            {
                WriteSpacer(spacerFormattingInfo, spacer, rootParentValue);
            }
            else if (CollectionIndex == 1)
            {
                WriteSpacer(spacerFormattingInfo, twoSpacer, rootParentValue);
            }
            else
            {
                WriteSpacer(spacerFormattingInfo, lastSpacer, rootParentValue);
            }

            // Output the nested format for this item:
            formattingInfo.FormatAsChild(itemFormat, item);
        }

        CollectionIndex = savedCollectionIndex; // Restore the CollectionIndex

        FormattingInfoPool.Pool.Release(spacerFormattingInfo);

        if (itemsAsList != null)
            UnityEngine.Pool.ListPool<object>.Release(itemsAsList);

        // The split segments are owned by the format's split list and released on teardown; only release the wrapper we created.
        if (wrappedItemFormat != null)
        {
            wrappedPlaceholder.Format = null; // detach the borrowed segment so it is not returned to the pool twice
            FormatPool.Pool.Release(wrappedItemFormat);
        }

        return true;
    }

    static void WriteSpacer(IFormattingInfo formattingInfo, Format spacer, object value)
    {
        if (spacer.HasNested)
            formattingInfo.FormatAsChild(spacer, value);
        else
            formattingInfo.Write(spacer.GetLiteralText());
    }

    /// <summary>
    /// Checks if any of the <see cref="Placeholder"/>'s <see cref="Placeholder.Selectors"/> has nullable <c>?</c> as their first operator.
    /// </summary>
    /// <param name="formattingInfo"></param>
    /// <returns>
    /// <see langword="true"/>, any of the <see cref="Placeholder"/>'s <see cref="Placeholder.Selectors"/> has nullable <c>?</c> as their first operator.
    /// </returns>
    /// <remarks>
    /// The nullable operator '?' can be followed by a dot (like '?.') or a square brace (like '?[')
    /// </remarks>
    bool HasNullableOperator(IFormattingInfo formattingInfo)
    {
        if (formattingInfo.Placeholder != null)
        {
            foreach (var s in formattingInfo.Placeholder.Selectors)
            {
                if (s.OperatorLength > 0 && s.BaseString[s.OperatorStartIndex] == m_SmartSettings.Parser.NullableOperator)
                    return true;
            }
        }
        return false;
    }

    ///<inheritdoc />
    public void Initialize(SmartFormatter smartFormatter)
    {
        m_SmartSettings = smartFormatter.Settings;
    }

    void IFormatterLiteralExtractor.WriteAllLiterals(IFormattingInfo formattingInfo)
    {
        var format = formattingInfo.Format;

        // See if the format string contains un-nested "|":
        using var splitListPooledObject = SplitListPool.Pool.Get(out var splitList);
        var parameters = (SplitList)(format != null ? format.Split(SplitChar, 4) : splitList);

        if (format == null || parameters.Count < 2)
            return;

        // Write all formatting options
        for (int i = 0; i < parameters.Count; i++)
        {
            formattingInfo.FormatAsChild(parameters[i], formattingInfo.CurrentValue);
        }
    }
}
