// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor;

class TypeHandleInfos
{
    readonly Dictionary<string, string> m_CustomUssNames = new Dictionary<string, string>();

    /// <summary>
    /// Create a new instance of the <see cref="TypeHandleInfos"/> class.
    /// </summary>
    public TypeHandleInfos()
    {
        // Register usual types that would be badly converted by ToKebabCase_Internal()
        RegisterCustomTypeHandleUss(TypeHandle.Texture2D, "texture2d");
        RegisterCustomTypeHandleUss(TypeHandle.Texture2DArray, "texture2d-array");
        RegisterCustomTypeHandleUss(TypeHandle.Texture3D, "texture3d");
    }

    /// <summary>
    /// Register a custom uss name for a given <see cref="TypeHandle"/>.
    /// </summary>
    /// <param name="typeHandle">The type handle.</param>
    /// <param name="ussName">The uss name.</param>
    public void RegisterCustomTypeHandleUss(TypeHandle typeHandle, string ussName)
    {
        m_CustomUssNames[typeHandle.Identification] = ussName;
    }

    /// <summary>
    /// The name of the <see cref="TypeHandle"/> for use in uss.
    /// </summary>
    /// <param name="typeHandle">The type handle.</param>
    /// <returns>The uss name to use for this <see cref="TypeHandle"/>.</returns>
    public string GetUssName(TypeHandle typeHandle)
    {
        m_CustomUssNames.TryGetValue(typeHandle.Identification, out string ussName);
        return ussName ?? typeHandle.Name.ToKebabCase_Internal();
    }

    /// <summary>
    /// The optional additional uss name of the <see cref="TypeHandle"/> or null.
    /// </summary>
    /// <param name="typeHandle">The type handle.</param>
    /// <returns>The additional uss name to use for this <see cref="TypeHandle"/> or null.</returns>
    public static string GetAdditionalUssName(TypeHandle typeHandle)
    {
        var type = typeHandle.Resolve();
        if (type.IsSubclassOf(typeof(Component)) && type != typeof(Component))
            return "component";
        if (type.IsSubclassOf(typeof(Texture)) && type != typeof(Texture))
            return "texture";
        if (typeof(IList).IsAssignableFrom(type))
            return "list";
        if (typeof(IDictionary).IsAssignableFrom(type))
            return "dictionary";
        if (type.IsEnum)
            return "enum";

        return null;
    }

    /// <summary>
    /// Remove all uss classes for this prefix and typeHandle on the element.
    /// </summary>
    /// <param name="prefix">The uss class prefix.</param>
    /// <param name="element">The element.</param>
    /// <param name="typeHandle">The type handle.</param>
    public void RemoveUssClasses(string prefix, VisualElement element, TypeHandle typeHandle)
    {
        if (!typeHandle.IsValid)
            return;
        element.RemoveFromClassList(prefix + GetUssName(typeHandle));

        var additionalTypeUssName = TypeHandleInfos.GetAdditionalUssName(typeHandle);
        if (additionalTypeUssName != null)
        {
            element.RemoveFromClassList(prefix + additionalTypeUssName);
        }
    }

    /// <summary>
    /// Add all uss classes for this prefix and typeHandle on the element.
    /// </summary>
    /// <param name="prefix">The uss class prefix.</param>
    /// <param name="element">The element.</param>
    /// <param name="typeHandle">The type handle.</param>
    public void AddUssClasses(string prefix, VisualElement element, TypeHandle typeHandle)
    {
        if (!typeHandle.IsValid)
            return;
        element.AddToClassList(prefix + GetUssName(typeHandle));

        var additionalTypeUssName = TypeHandleInfos.GetAdditionalUssName(typeHandle);
        if (additionalTypeUssName != null)
        {
            element.AddToClassList(prefix + additionalTypeUssName);
        }
    }
}
