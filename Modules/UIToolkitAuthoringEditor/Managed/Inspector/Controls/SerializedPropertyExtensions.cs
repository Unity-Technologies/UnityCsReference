// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Provides extension methods for SerializedProperty.
/// </summary>
static class SerializedPropertyExtensions
{
    static readonly string k_ArrayDataSuffixName = ".Array.data";
    static readonly string k_UxmlSerializedDataFieldName = "serializedData.";

    /// <summary>
    /// Retrieves the parent SerializedProperty of the given property.
    /// </summary>
    /// <param name="property">The target property</param>
    /// <returns>The parent property if found; returns null otherwise</returns>
    public static SerializedProperty GetParentProperty(this SerializedProperty property)
    {
        if (property == null)
            return null;

        string path = property.propertyPath;
        int lastDotIndex = path.LastIndexOf('.');

        if (lastDotIndex == -1)
            return null;

        string parentPath = path.Substring(0, lastDotIndex);
        return property.serializedObject.FindProperty(parentPath);
    }

    public static BindingId GetBindingPath(this SerializedProperty boundProperty)
    {
        var bindingPath = boundProperty.name;
        if (boundProperty.propertyPath.Contains(k_ArrayDataSuffixName, StringComparison.Ordinal))
        {
            bindingPath = TrimSerializedDataSuffix(boundProperty.propertyPath);
        }

        return bindingPath;
    }

    internal static string TrimSerializedDataSuffix(string path)
    {
        if (path == null)
            return string.Empty;

        ReadOnlySpan<char> span = path.AsSpan();
        int sIndex = span.IndexOf(k_UxmlSerializedDataFieldName, StringComparison.OrdinalIgnoreCase);

        if (sIndex >= 0)
            span = span.Slice(sIndex + k_UxmlSerializedDataFieldName.Length);

        string trimmed = span.ToString();

        int arrayDataIndex = trimmed.IndexOf(k_ArrayDataSuffixName, StringComparison.Ordinal);
        if (arrayDataIndex >= 0)
        {
            using var sbPool = StringBuilderPool.Get(out var sb);
            sb.Append(trimmed, 0, arrayDataIndex);
            sb.Append(trimmed, arrayDataIndex + k_ArrayDataSuffixName.Length, trimmed.Length - (arrayDataIndex + k_ArrayDataSuffixName.Length));
            return sb.ToString();
        }

        return trimmed;
    }
}
