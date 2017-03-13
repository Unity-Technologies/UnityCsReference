// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [System.Serializable]
    internal class CurveEditorSelection : ScriptableObject
    {
        [SerializeField] private List<CurveSelection> m_SelectedCurves;

        public List<CurveSelection> selectedCurves
        {
            get { return m_SelectedCurves ?? (m_SelectedCurves = new List<CurveSelection>()); }
            set { m_SelectedCurves = value; }
        }
    }

    [System.Serializable]
    internal class CurveSelection : System.IComparable
    {
        internal enum SelectionType
        {
            Key = 0,
            InTangent = 1,
            OutTangent = 2,
            Count = 3,
        }

        [SerializeField] public int curveID = 0;
        [SerializeField] public int key = -1;
        [SerializeField] public bool semiSelected = false;
        [SerializeField] public SelectionType type;

        internal CurveSelection(int curveID, int key)
        {
            this.curveID = curveID;
            this.key = key;
            this.type = SelectionType.Key;
        }

        internal CurveSelection(int curveID, int key, SelectionType type)
        {
            this.curveID = curveID;
            this.key = key;
            this.type = type;
        }

        public int CompareTo(object _other)
        {
            CurveSelection other = (CurveSelection)_other;
            int cmp = curveID - other.curveID;
            if (cmp != 0)
                return cmp;

            cmp = key - other.key;
            if (cmp != 0)
                return cmp;

            return (int)type - (int)other.type;
        }

        public override bool Equals(object _other)
        {
            CurveSelection other = (CurveSelection)_other;
            return other.curveID == curveID && other.key == key && other.type == type;
        }

        public override int GetHashCode()
        {
            return curveID * 729 + key * 27 + (int)type;
        }
    }
}
