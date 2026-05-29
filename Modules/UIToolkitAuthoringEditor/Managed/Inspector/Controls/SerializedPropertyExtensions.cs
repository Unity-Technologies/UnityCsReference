// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;

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

        var copy = property.Copy();
        return copy.Parent() ? copy : null;
    }

    // managedReferenceFullTypename returns the UxmlSerializedData nested type (e.g.
    // "UnityEngine.UIElements UnityEngine.UIElements.Columns/UxmlSerializedData"), but
    // GetDescription expects the declaring element type name (e.g. "UnityEngine.UIElements.Columns").
    // We resolve the Type and use DeclaringType.FullName to get the correct key.
    static UxmlSerializedDataDescription GetDescriptionFromManagedReferenceTypename(string managedReferenceFullTypename)
    {
        if (string.IsNullOrEmpty(managedReferenceFullTypename))
            return null;

        // Format: "AssemblyName TypeName" — split into assembly and type name.
        var spaceIndex = managedReferenceFullTypename.IndexOf(' ');
        if (spaceIndex < 0)
            return null;

        var assemblyName = managedReferenceFullTypename.Substring(0, spaceIndex);
        var typeName = managedReferenceFullTypename.Substring(spaceIndex + 1).Replace('/', '+');

        // Use Type.GetType with assembly-qualified name to avoid Assembly.Load throwing
        // FileNotFoundException if the assembly is not found.
        var type = Type.GetType($"{typeName}, {assemblyName}");
        var declaringType = type?.DeclaringType ?? type;
        if (declaringType == null)
            return null;

        return UxmlSerializedDataRegistry.GetDescription(declaringType.FullName);
    }

    // Sentinel stored in the segments list to represent an array element index boundary.
    // When encountered during forward resolution, it signals that the next description
    // must be resolved from the managed reference type captured at that boundary.
    const string k_ArrayElementSentinel = "\x00array_element";

    /// <summary>
    /// Builds the full data binding path for a serialized property by walking the
    /// <see cref="UxmlSerializedDataDescription"/> tree and reading each attribute's
    /// <see cref="UxmlSerializedAttributeDescription.bindingPath"/>.
    /// This correctly handles fields annotated with <see cref="UxmlAttributeBindingPathAttribute"/>
    /// where the serialized field name differs from the binding path.
    /// The root <see cref="UxmlSerializedDataDescription"/> is resolved automatically by walking
    /// up to the <c>m_SerializedData</c> root using <see cref="SerializedProperty.Parent()"/>,
    /// avoiding string parsing and <c>FindProperty</c> calls.
    /// </summary>
    /// <param name="property">
    /// The serialized property to resolve. Its <c>propertyPath</c> must be rooted under
    /// <c>m_VisualTree.m_Children.Array.data[i].m_SerializedData</c>.
    /// </param>
    /// <returns>The full binding path, or an empty string if it cannot be resolved.</returns>
    public static BindingId GetFullBindingPath(this SerializedProperty property)
    {
        if (property == null)
            return string.Empty;

        // Walk up from the property to the m_SerializedData root, collecting path segments
        // and managed reference typenames at each UxmlObjectReference boundary.
        // Each entry is either:
        //   - a plain field name (e.g. "columns", "name")
        //   - k_ArrayElementSentinel + "[n]" (e.g. "\x00array_element[0]") for array elements
        // We also capture the managed reference typename at each boundary as we pass through it.
        using var segmentsPool = ListPool<string>.Get(out var segments);
        using var typenamesPool = DictionaryPool<int, string>.Get(out var managedRefTypenames);

        var cursor = property.Copy();

        // Walk up until we reach "m_SerializedData" or exhaust the hierarchy.
        while (true)
        {
            var segmentName = GetLastPathSegment(cursor.propertyPath);

            // Reached the m_SerializedData field — stop here; this is our root.
            if (segmentName == "m_SerializedData")
                break;

            // Skip "Array" and "data[n]" bookkeeping nodes that Unity inserts for arrays,
            // but record the array index so we can emit "[n]" in the binding path.
            if (segmentName == "Array")
            {
                if (!cursor.Parent())
                    return string.Empty;
                continue;
            }

            if (segmentName.StartsWith("data[", StringComparison.Ordinal))
            {
                var bracketIndex = segmentName.IndexOf('[');
                var indexToken = k_ArrayElementSentinel + segmentName.Substring(bracketIndex);

                // If this data[n] property is a managed reference, capture its typename so we
                // can resolve the concrete description for the element on the forward pass.
                if (cursor.propertyType == SerializedPropertyType.ManagedReference &&
                    !string.IsNullOrEmpty(cursor.managedReferenceFullTypename))
                {
                    managedRefTypenames[segments.Count] = cursor.managedReferenceFullTypename;
                }

                segments.Add(indexToken);
                if (!cursor.Parent())
                    return string.Empty;
                continue;
            }

            // For a regular field that is itself a managed reference (non-list UxmlObjectReference),
            // capture the typename before moving up so we can resolve the concrete description.
            if (cursor.propertyType == SerializedPropertyType.ManagedReference &&
                !string.IsNullOrEmpty(cursor.managedReferenceFullTypename))
            {
                managedRefTypenames[segments.Count] = cursor.managedReferenceFullTypename;
            }

            segments.Add(segmentName);

            if (!cursor.Parent())
                return string.Empty;
        }

        // Validate the serialized object is a VTA.
        if (property.serializedObject.targetObject is not VisualTreeAsset)
            return string.Empty;

        // cursor is now at m_SerializedData, which is a [SerializeReference] field whose concrete type
        // identifies the element (e.g. "MultiColumnListView+UxmlSerializedData"). Capture the typename
        // before walking further up, as it is the typename we need to resolve the root description.
        if (cursor.propertyType != SerializedPropertyType.ManagedReference ||
            string.IsNullOrEmpty(cursor.managedReferenceFullTypename))
            return string.Empty;

        var rootDescription = GetDescriptionFromManagedReferenceTypename(cursor.managedReferenceFullTypename);
        if (rootDescription == null)
            return string.Empty;

        // Segments were collected bottom-up; reverse for top-down forward resolution.
        segments.Reverse();
        // Remap managed reference indices to the reversed positions.
        using var reversedTypenamesPool = DictionaryPool<int, string>.Get(out var reversedTypenames);
        var lastIdx = segments.Count - 1;
        foreach (var kv in managedRefTypenames)
            reversedTypenames[lastIdx - kv.Key] = kv.Value;

        // Forward pass: resolve binding path segments using the description tree.
        using var sbPool = StringBuilderPool.Get(out var sb);
        var currentDescription = rootDescription;

        for (var i = 0; i < segments.Count && currentDescription != null; i++)
        {
            var segment = segments[i];

            // Array element boundary — emit "[n]" and resolve concrete description if available.
            if (segment.StartsWith(k_ArrayElementSentinel, StringComparison.Ordinal))
            {
                sb.Append(segment, k_ArrayElementSentinel.Length, segment.Length - k_ArrayElementSentinel.Length);
                if (reversedTypenames.TryGetValue(i, out var typename))
                {
                    var desc = GetDescriptionFromManagedReferenceTypename(typename);
                    if (desc != null)
                        currentDescription = desc;
                }
                continue;
            }

            var attribute = currentDescription.FindAttributeWithPropertyName(segment);
            if (attribute == null)
                break;

            if (sb.Length > 0)
                sb.Append('.');
            sb.Append(attribute.bindingPath);

            if (attribute.isUxmlObject && !attribute.isList)
            {
                // Non-list UxmlObjectReference: use captured managed reference type for the next description.
                var desc = reversedTypenames.TryGetValue(i, out var typename)
                    ? GetDescriptionFromManagedReferenceTypename(typename)
                    : null;
                // Fall back to the declared type if the reference is null or type cannot be resolved.
                currentDescription = desc ?? attribute.dataDescription;
            }
            else if (attribute.isUxmlObject) // isList — description resolved per element at the data[n] boundary
            {
                currentDescription = attribute.dataDescription;
            }
            else
            {
                currentDescription = null;
            }
        }

        return sb.ToString();
    }

    static string GetLastPathSegment(string path)
    {
        var lastDot = path.LastIndexOf('.');
        return lastDot < 0 ? path : path.Substring(lastDot + 1);
    }

    public static SerializedProperty GetUxmlAttributeFlags(this SerializedProperty property)
    {
        return property?.serializedObject?.FindProperty(property.propertyPath + UxmlSerializedData.AttributeFlagSuffix);
    }

}
