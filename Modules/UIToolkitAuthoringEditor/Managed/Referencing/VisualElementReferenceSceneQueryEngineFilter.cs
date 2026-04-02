// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEditor;
using UnityEditor.Search.Providers;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements;

/// <summary>
/// Scene search filters for visual element references.
/// </summary>
/// <remarks>
/// The following are the supported filters:
///- veRef:any - matches any reference.
///- veRef:empty - matches references with an empty path and no panel renderer.
///- veRef:missing - matches references where the path doesn't resolve to an element or the PanelRenderer/VTA is missing.
///- veRef:[1/2] - matches references with a partial path match.
///- veRef=[1/2/3] - matches references with an exact path match.
/// </remarks>
static class VisualElementReferenceSceneQueryEngineFilter
{
    // We cache the parsed result to avoid parsing it for every item.
    public const string FilterId = "veRef";
    public const char PathSeperatorToken = '/';

    const string k_EqualsToken = "=";
    const string k_PartialToken = ":";
    const string k_AnyFilter = "any";
    const string k_EmptyFilter = "empty";
    const string k_MissingFilter = "missing";

    const string k_ReferenceTypeName = nameof(VisualElementReference);
    static readonly string k_ReferenceTypeGenericName = typeof(VisualElementReference<>).Name;
    static readonly int k_MinTypeLen = k_ReferenceTypeName.Length;
    static readonly int k_MaxTypeLen = k_ReferenceTypeGenericName.Length;

    static string s_PreviousQuery;
    static int[] s_CurrentValue;

    [System.ComponentModel.Description("Find visual element references")]
    [SceneQueryEngineFilter(FilterId, [k_EqualsToken, k_PartialToken], "veRef=[1/2/3]")]
    public static bool CompareVisualElementReference(GameObject go, string op, string value)
    {
        Func<SerializedProperty, bool> compareFunc = null;

        switch (op, value)
        {
            case (k_PartialToken, k_AnyFilter):
                compareFunc = CompareFilterAny;
                break;
            case (k_PartialToken, k_EmptyFilter):
                compareFunc = CompareFilterEmpty;
                break;
            case (k_PartialToken, k_MissingFilter):
                compareFunc = CompareFilterMissing;
                break;
            default:
                var authoringIdPath = ParsePathValue(value);
                if (authoringIdPath == null || authoringIdPath.Length == 0)
                    return false;
                if (op == k_EqualsToken)
                    compareFunc = CompareFilterPathEquals;
                else if (op == k_PartialToken)
                    compareFunc = CompareFilterPathPartial;
                else
                    return false;
                break;
        }

        using (var pool = ListPool<MonoBehaviour>.Get(out var monoBehaviours))
        {
            go.GetComponents(monoBehaviours);
            foreach (var monoBehaviour in monoBehaviours)
            {
                using var so = new SerializedObject(monoBehaviour);
                var itr = so.GetIterator();

                var enterChildren = true;
                while (itr.Next(enterChildren))
                {
                    enterChildren = true;

                    if (itr.propertyType == SerializedPropertyType.Generic &&
                        itr.type.Length >= k_MinTypeLen &&
                        itr.type.Length <= k_MaxTypeLen &&
                        (itr.type == k_ReferenceTypeName || itr.type == k_ReferenceTypeGenericName))
                    {
                        // If we get a match then we return true otherwise we keep searching as there may be more fields we can check.
                        if (compareFunc(itr))
                            return true;

                        // Skip child fields
                        enterChildren = false;
                    }
                }
            }
        }

        return false;
    }

    static bool CompareFilterAny(SerializedProperty serializedProperty)
    {
        // If we found a VisualElementReference field, return true (any reference exists)
        return true;
    }

    static bool CompareFilterEmpty(SerializedProperty serializedProperty)
    {
        var panelRendererProp = serializedProperty.FindPropertyRelative("m_PanelRenderer");
        if (panelRendererProp == null)
            return false;

        var pathProp = serializedProperty.FindPropertyRelative("m_AuthoringPath.m_PathIds");
        if (pathProp == null)
            return false;

        return panelRendererProp.objectReferenceValue is not PanelRenderer panelRenderer && pathProp.arraySize == 0;
    }

    static bool CompareFilterMissing(SerializedProperty serializedProperty)
    {
        var pathProp = serializedProperty.FindPropertyRelative("m_AuthoringPath.m_PathIds");
        if (pathProp == null || pathProp.arraySize == 0)
            return false; // Empty path, not missing

        var panelRendererProp = serializedProperty.FindPropertyRelative("m_PanelRenderer");
        if (panelRendererProp == null)
            return false;

        // Missing if PanelRenderer is null and we have a path (since it can't resolve to anything)
        if (panelRendererProp.objectReferenceValue is not PanelRenderer panelRenderer)
            return true;

        // Missing if PanelRenderer has no VisualTreeAsset and we have a path (since it can't resolve to anything)
        var vta = panelRenderer.visualTreeAsset;
        if (vta == null)
            return true;

        Span<int> pathBuffer = stackalloc int[pathProp.arraySize];

        for (int i = 0; i < pathProp.arraySize; i++)
            pathBuffer[i] = pathProp.GetArrayElementAtIndex(i).intValue;

        var foundElement = vta.FindElementByPath(pathBuffer);
        return foundElement == null;
    }

    static bool CompareFilterPathPartial(SerializedProperty serializedProperty)
    {
        var pathProp = serializedProperty.FindPropertyRelative("m_AuthoringPath.m_PathIds");
        if (pathProp == null)
            return false;

        var authoringIdPath = s_CurrentValue;
        if (authoringIdPath == null || authoringIdPath.Length == 0 || authoringIdPath.Length > pathProp.arraySize)
            return false;

        // Try to find a contiguous match starting at each position
        for (int startIndex = 0; startIndex <= pathProp.arraySize - authoringIdPath.Length; startIndex++)
        {
            bool foundMatch = true;
            for (int i = 0; i < authoringIdPath.Length; i++)
            {
                if (authoringIdPath[i] != pathProp.GetArrayElementAtIndex(startIndex + i).intValue)
                {
                    foundMatch = false;
                    break;
                }
            }

            if (foundMatch)
                return true;
        }

        return false;
    }

    static bool CompareFilterPathEquals(SerializedProperty serializedProperty)
    {
        var pathProp = serializedProperty.FindPropertyRelative("m_AuthoringPath.m_PathIds");
        if (pathProp == null)
            return false;

        var authoringIdPath = s_CurrentValue;
        if (authoringIdPath == null || authoringIdPath.Length == 0 || authoringIdPath.Length != pathProp.arraySize)
            return false;

        for (int i = 0; i < authoringIdPath.Length; ++i)
        {
            if (authoringIdPath[i] != pathProp.GetArrayElementAtIndex(i).intValue)
                return false;
        }

        return true;
    }

    static int[] ParsePathValue(string value)
    {
        // We cache the result of parsing the path value since it can be expensive and we don't want to do it for every item.
        if (s_PreviousQuery == value)
            return s_CurrentValue;
        s_PreviousQuery = value;
        s_CurrentValue = default;

        var start = value.IndexOf('[');
        var end = value.LastIndexOf("]");

        if (start != -1 && end != -1)
            value = value.Substring(start + 1, end - start - 1);

        // Using comma (,) as a separator for the path elements causes the search to split it into multiple searches, so we use forward slash (/) which does not have that issue.
        var split = value.Split(PathSeperatorToken, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length > 0)
        {
            var path = new int[split.Length];
            for (int i = 0; i < split.Length; i++)
            {
                if (int.TryParse(split[i], NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                {
                    path[i] = parsed;
                }
                else
                {
                    return s_CurrentValue;
                }
            }

            s_CurrentValue = path;
            return s_CurrentValue;
        }

        return s_CurrentValue;
    }
}
