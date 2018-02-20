// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm = System.ComponentModel;
using uei = UnityEngine.Internal;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Linq;

namespace UnityEditor
{
    public class CustomEditor : System.Attribute
    {
        public CustomEditor(System.Type inspectedType)
        {
            if (inspectedType == null)
                Debug.LogError("Failed to load CustomEditor inspected type");
            m_InspectedType = inspectedType;
            m_EditorForChildClasses = false;
        }

        public CustomEditor(System.Type inspectedType, bool editorForChildClasses)
        {
            if (inspectedType == null)
                Debug.LogError("Failed to load CustomEditor inspected type");
            m_InspectedType = inspectedType;
            m_EditorForChildClasses = editorForChildClasses;
        }

        internal Type m_InspectedType;
        internal bool m_EditorForChildClasses;

        public bool isFallback { get; set; }
    }
}
