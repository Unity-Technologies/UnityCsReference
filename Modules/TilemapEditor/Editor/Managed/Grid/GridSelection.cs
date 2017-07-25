// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public class GridSelection : ScriptableObject
    {
        public static event Action gridSelectionChanged;
        private BoundsInt m_Position;
        private GameObject m_Target;
        [SerializeField] private Object m_PreviousSelection;

        public static bool active { get { return Selection.activeObject is GridSelection && selection.m_Target != null; } }

        private static GridSelection selection { get { return Selection.activeObject as GridSelection; } }
        public static BoundsInt position
        {
            get { return selection != null ? selection.m_Position : new BoundsInt(); }
            set
            {
                if (selection != null && selection.m_Position != value)
                {
                    selection.m_Position = value;
                    if (gridSelectionChanged != null)
                        gridSelectionChanged();
                }
            }
        }
        public static GameObject target { get { return selection != null ? selection.m_Target : null; } }
        public static Grid grid { get { return selection != null && selection.m_Target != null ? selection.m_Target.GetComponentInParent<Grid>() : null; } }

        public static void Select(Object target, BoundsInt bounds)
        {
            GridSelection newSelection = CreateInstance<GridSelection>();
            newSelection.m_PreviousSelection = Selection.activeObject;
            newSelection.m_Target = target as GameObject;
            newSelection.m_Position = bounds;
            Selection.activeObject = newSelection;
            if (gridSelectionChanged != null)
                gridSelectionChanged();
        }

        public static void Clear()
        {
            if (active)
            {
                selection.m_Position = new BoundsInt();
                Selection.activeObject = selection.m_PreviousSelection;
                if (gridSelectionChanged != null)
                    gridSelectionChanged();
            }
        }
    }
}
