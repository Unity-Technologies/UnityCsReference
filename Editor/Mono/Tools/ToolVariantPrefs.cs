// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.EditorTools
{
    [Serializable]
    class ToolVariantPrefs : ISerializationCallbackReceiver
    {
        Dictionary<Type, Type> m_VariantGroupToActiveTool = new Dictionary<Type,Type>();

        [SerializeField]
        string[] m_Keys, m_Values;

        public Type GetPreferredVariant(Type variantGroup) =>
            m_VariantGroupToActiveTool.TryGetValue(variantGroup, out var tool)
            ? tool
            : null;

        public void SetPreferredVariant(Type variantGroup, Type tool) =>
            m_VariantGroupToActiveTool[variantGroup] = tool;

        public void OnBeforeSerialize()
        {
            m_Keys = m_VariantGroupToActiveTool.Keys.Select(x => x.AssemblyQualifiedName).ToArray();
            m_Values = m_VariantGroupToActiveTool.Values.Select(x => x.AssemblyQualifiedName).ToArray();
        }

        public void OnAfterDeserialize()
        {
            for (int i = 0, c = Math.Min(m_Keys.Length, m_Values.Length); i < c; ++i)
            {
                var key = Type.GetType(m_Keys[i]);
                var val = Type.GetType(m_Values[i]);
                if (key != null && val != null)
                    m_VariantGroupToActiveTool.Add(key, val);
            }
        }
    }
}
