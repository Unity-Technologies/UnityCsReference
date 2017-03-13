// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditorInternal
{
    internal class DopeLine
    {
        private int m_HierarchyNodeID;
        private AnimationWindowCurve[] m_Curves;
        private List<AnimationWindowKeyframe> m_Keys;

        public static GUIStyle dopekeyStyle = "Dopesheetkeyframe";

        public Rect position;
        public System.Type objectType;
        public bool tallMode;
        public bool hasChildren;
        public bool isMasterDopeline;

        public System.Type valueType
        {
            get
            {
                if (m_Curves.Length > 0)
                {
                    System.Type type = m_Curves[0].valueType;
                    for (int i = 1; i < m_Curves.Length; i++)
                    {
                        if (m_Curves[i].valueType != type)
                            return null;
                    }
                    return type;
                }

                return null;
            }
        }

        public bool isPptrDopeline
        {
            get
            {
                if (m_Curves.Length > 0)
                {
                    for (int i = 0; i < m_Curves.Length; i++)
                    {
                        if (!m_Curves[i].isPPtrCurve)
                            return false;
                    }
                    return true;
                }
                return false;
            }
        }

        public bool isEditable
        {
            get
            {
                if (m_Curves.Length > 0)
                {
                    bool isReadOnly = Array.Exists(m_Curves, curve => !curve.animationIsEditable);
                    return !isReadOnly;
                }

                return false;
            }
        }

        public int hierarchyNodeID
        {
            get
            {
                return m_HierarchyNodeID;
            }
        }

        public AnimationWindowCurve[] curves
        {
            get
            {
                return m_Curves;
            }
        }

        public List<AnimationWindowKeyframe> keys
        {
            get
            {
                if (m_Keys == null)
                {
                    m_Keys = new List<AnimationWindowKeyframe>();
                    foreach (AnimationWindowCurve curve in m_Curves)
                        foreach (AnimationWindowKeyframe key in curve.m_Keyframes)
                            m_Keys.Add(key);

                    m_Keys.Sort((a, b) => a.time.CompareTo(b.time));
                }

                return m_Keys;
            }
        }

        public void InvalidateKeyframes()
        {
            m_Keys = null;
        }

        public DopeLine(int hierarchyNodeID, AnimationWindowCurve[] curves)
        {
            m_HierarchyNodeID = hierarchyNodeID;
            m_Curves = curves;
        }
    }
}
