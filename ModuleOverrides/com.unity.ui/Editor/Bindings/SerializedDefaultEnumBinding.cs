// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

// specific enum version that binds on the index property of the PopupField<string>
class SerializedDefaultEnumBinding : SerializedObjectBindingToBaseField<string, PopupField<string>>
{
    private const int kDefaultValueIndex = -1;

    //we need to keep a copy of the last value since some fields will allocate when getting the value
    private int lastFieldValueIndex;

    private List<string> originalChoices;
    private List<int> displayIndexToEnumIndex;
    private List<int> enumIndexToDisplayIndex;
    private int originalIndex;

    public static void CreateBind(PopupField<string> field,  SerializedObjectBindingContext context,
        SerializedProperty property)
    {
        var newBinding = new SerializedDefaultEnumBinding();
        newBinding.isReleased = false;
        newBinding.SetBinding(field, context, property);
    }

    private void SetBinding(PopupField<string> c, SerializedObjectBindingContext context,
        SerializedProperty property)
    {
        property.unsafeMode = true;
        SetContext(context, property);

        this.originalChoices = c.choices;
        this.originalIndex = c.index;

        // We need to keep bidirectional lists of indices to translate between Popup choice index and
        // SerializedProperty enumValueIndex because the Popup choices might be displayed in another language
        // (using property.enumLocalizedDisplayNames), or in another display order (using enumData.displayNames).
        // We need to build the bidirectional lists when we assign field.choices, using any of the above options.

        if (displayIndexToEnumIndex == null)
            displayIndexToEnumIndex = new List<int>();
        else
            displayIndexToEnumIndex.Clear();

        if (enumIndexToDisplayIndex == null)
            enumIndexToDisplayIndex = new List<int>();
        else
            enumIndexToDisplayIndex.Clear();

        ScriptAttributeUtility.GetFieldInfoFromProperty(property, out var enumType);
        if (enumType != null)
        {
            var enumData = EnumDataUtility.GetCachedEnumData(enumType, UnityEngine.EnumDataUtility.CachedType.ExcludeObsolete);
            var enumDataOld = EnumDataUtility.GetCachedEnumData(enumType, UnityEngine.EnumDataUtility.CachedType.IncludeAllObsolete);
            c.choices = new List<string>(enumData.displayNames);

            var sortedEnumNames = EditorGUI.EnumNamesCache.GetEnumNames(property);
            // Build a name to value lookup. We need this to check for duplicate values.
            var nameValueDict = UnityEngine.Pool.DictionaryPool<string, int>.Get();
            for (int i = 0; i < enumDataOld.names.Length; ++i)
            {
                nameValueDict[enumDataOld.names[i]] = enumDataOld.flagValues[i];
            }

            foreach (var enumName in enumData.names)
                displayIndexToEnumIndex.Add(Array.IndexOf(sortedEnumNames, enumName));
            foreach (var sortedEnumName in sortedEnumNames)
                enumIndexToDisplayIndex.Add(Array.IndexOf(enumData.names, sortedEnumName));

            // We need to map the display index to the first occurrence of the value in the serialized property enum names.
            // The serialized property lacks information about obsolete enum values, so it always maps to the first occurrence of the value,
            // regardless of its obsolescence and visibility.
            // Additionally, we must handle obsolete values that are not displayed, as the enumValueIndex encompasses all values,
            // including those marked as obsolete but not visible. (UUM-36836, UUM-31162)
            var firstOccurrenceIndexToValueDict = UnityEngine.Pool.DictionaryPool<int, int>.Get();
            for (int i = 0; i < sortedEnumNames.Length; ++i)
            {
                var value = nameValueDict[sortedEnumNames[i]];

                var displayIndex = Array.IndexOf(enumData.names, sortedEnumNames[i]);
                if (displayIndex != -1)
                {
                    // If we have already encountered this value then we need to use the first index as the serialized property will always map to this one.
                    if (firstOccurrenceIndexToValueDict.TryGetValue(value, out var firstEnumIndex))
                    {
                        displayIndexToEnumIndex[displayIndex] = firstEnumIndex;
                        enumIndexToDisplayIndex[firstEnumIndex] = displayIndex;
                    }
                    else
                    {
                        firstOccurrenceIndexToValueDict[value] = i;
                        displayIndexToEnumIndex[displayIndex] = i;
                    }

                    enumIndexToDisplayIndex[i] = displayIndex;
                }
                else
                {
                    firstOccurrenceIndexToValueDict[value] = i;
                }
            }

            UnityEngine.Pool.DictionaryPool<string, int>.Release(nameValueDict);
            UnityEngine.Pool.DictionaryPool<int, int>.Release(firstOccurrenceIndexToValueDict);
        }
        else
        {
            c.choices = new List<string>(property.enumLocalizedDisplayNames);

            for (int i = 0; i < c.choices.Count; i++)
            {
                displayIndexToEnumIndex.Add(i);
                enumIndexToDisplayIndex.Add(i);
            }
        }


        this.lastFieldValueIndex = c.index;

        BindingsStyleHelpers.RegisterRightClickMenu(c, property);

        this.field = c;
    }

    protected override void SyncPropertyToField(PopupField<string> c, SerializedProperty p)
    {
        if (p == null)
        {
            throw new ArgumentNullException(nameof(p));
        }
        if (c == null)
        {
            throw new ArgumentNullException(nameof(c));
        }

        int propValueIndex = p.enumValueIndex;
        int newIndex;
        if (propValueIndex >= 0 && propValueIndex < enumIndexToDisplayIndex.Count)
            newIndex = lastFieldValueIndex = enumIndexToDisplayIndex[propValueIndex];
        else
            newIndex = lastFieldValueIndex = kDefaultValueIndex;

        // We dont want to trigger a change event as this will cause the value to be applied to all targets.
        if (p.hasMultipleDifferentValues)
            c.SetIndexWithoutNotify(newIndex);
        else
            c.index = newIndex;
    }

    protected override void UpdateLastFieldValue()
    {
        if (field == null)
        {
            lastFieldValueIndex  = Int32.MinValue;
        }
        else
        {
            lastFieldValueIndex = field.index;
        }
    }

    protected override bool SyncFieldValueToProperty()
    {
        var validIndex = lastFieldValueIndex >= 0 && lastFieldValueIndex < displayIndexToEnumIndex.Count;
        if (validIndex && (boundProperty.hasMultipleDifferentValues || boundProperty.enumValueIndex != displayIndexToEnumIndex[lastFieldValueIndex]))
        {
            boundProperty.enumValueIndex = displayIndexToEnumIndex[lastFieldValueIndex];
            boundProperty.m_SerializedObject.ApplyModifiedProperties();
            return true;
        }
        return false;
    }

    public override void Release()
    {
        if (isReleased)
            return;

        if (FieldBinding == this)
        {
            //we set the popup values to the original ones
            try
            {
                var previousField = field;
                BindingsStyleHelpers.UnregisterRightClickMenu(previousField);

                field = null;
                previousField.choices = originalChoices;
                previousField.index = originalIndex;
            }
            catch (ArgumentException)
            {
                //we did our best
            }

            FieldBinding = null;
        }


        ResetContext();
        field = null;
        lastFieldValueIndex = kDefaultValueIndex;
        isReleased = true;

        ResetCachedValues();
    }
}
