// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEditor;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Provides extension methods for SerializedProperty.
/// </summary>
static class SerializedPropertyExtensions
{
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
}
