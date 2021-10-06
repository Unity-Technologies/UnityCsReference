// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    [Serializable]
    public class GizmoInfo : IComparable
    {
        [SerializeField]
        bool m_IconEnabled;

        [SerializeField]
        bool m_GizmoEnabled;

        [SerializeField]
        int m_ClassID;

        [SerializeField]
        string m_ScriptClass;

        [SerializeField]
        string m_Name;

        [SerializeField]
        int m_Flags;

        [NonSerialized]
        UnityObject m_Script;

        [NonSerialized]
        Texture2D m_Thumb;

        internal GizmoInfo()
        {
            m_IconEnabled = false;
            m_GizmoEnabled = false;
            m_ClassID = -1;
            m_ScriptClass = null;
            m_Name = "";
            m_Flags = 0;
        }

        internal GizmoInfo(Annotation annotation)
        {
            m_GizmoEnabled = annotation.gizmoEnabled > 0;
            m_IconEnabled = annotation.iconEnabled > 0;
            m_ClassID = annotation.classID;
            m_ScriptClass = annotation.scriptClass;
            m_Flags = annotation.flags;
            m_Name = string.IsNullOrEmpty(m_ScriptClass)
                ? UnityType.FindTypeByPersistentTypeID(m_ClassID).name
                : m_Name = m_ScriptClass;
        }

        internal int classID => m_ClassID;

        internal string scriptClass => m_ScriptClass;

        public string name => m_Name;

        public bool hasGizmo => (m_Flags & (int)AnnotationUtility.Flags.kHasGizmo) > 0;

        public bool gizmoEnabled
        {
            get => m_GizmoEnabled;
            set => m_GizmoEnabled = value;
        }

        public bool hasIcon => (m_Flags & (int)AnnotationUtility.Flags.kHasIcon) > 0;

        public bool iconEnabled
        {
            get => m_IconEnabled;
            set => m_IconEnabled = value;
        }

        internal bool disabled => (m_Flags & (int)AnnotationUtility.Flags.kIsDisabled) > 0;

        public UnityObject script
        {
            get
            {
                if (m_Script == null && m_ScriptClass != "")
                    m_Script = EditorGUIUtility.GetScript(m_ScriptClass);
                return m_Script;
            }
        }

        public Texture2D thumb
        {
            get
            {
                if (m_Thumb == null)
                {
                    // Icon for scripts
                    if (script != null)
                        m_Thumb = EditorGUIUtility.GetIconForObject(m_Script);
                    // Icon for builtin components
                    else if (hasIcon)
                        m_Thumb = AssetPreview.GetMiniTypeThumbnailFromClassID(m_ClassID);
                }

                return m_Thumb;
            }
    }

        public int CompareTo(object obj)
        {
            if(obj is GizmoInfo other)
                return m_Name.CompareTo(other.m_Name);
            return 1;
        }
    }
}
