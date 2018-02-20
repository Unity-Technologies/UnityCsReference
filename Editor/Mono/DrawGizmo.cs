// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    // Determines how a gizmo is drawn or picked in the Unity editor.
    public enum GizmoType
    {
        // The gizmo can be picked in the editor.
        Pickable = 1,

        // Draw the gizmo if it is not selected or child of the selection.
        NotInSelectionHierarchy = 2,

        // Draw the gizmo if it is not selected.
        NonSelected = 32,

        // Draw the gizmo if it is selected.
        Selected = 4,

        // Draw the gizmo if it is active (shown in the inspector).
        Active = 8,

        // Draw the gizmo if it is selected or a child of the selection.
        InSelectionHierarchy = 16,

        [Obsolete("Use NotInSelectionHierarchy instead (UnityUpgradable) -> NotInSelectionHierarchy")]
        NotSelected = -127,

        [Obsolete("Use InSelectionHierarchy instead (UnityUpgradable) -> InSelectionHierarchy")]
        SelectedOrChild = -127,
    }

    // The DrawGizmo attribute allows you to supply a gizmo renderer for any [[Component]].
    public sealed class DrawGizmo : Attribute
    {
        // Defines when the gizmo should be invoked for drawing.
        public DrawGizmo(GizmoType gizmo)
        {
            drawOptions = gizmo;
        }

        // Same as above. /drawnGizmoType/ determines of what type the object we are drawing the gizmo of has to be.
        public DrawGizmo(GizmoType gizmo, Type drawnGizmoType)
        {
            drawnType = drawnGizmoType;
            drawOptions = gizmo;
        }

        //*undocumented
        public Type drawnType;

        //*undocumented
        public GizmoType drawOptions;
    }
}
