// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using UnityEditor.EditorTools;
using UnityEditorInternal;

namespace UnityEditor
{
    public abstract class CustomPivotMode : ScriptableObject
    {
        public abstract Vector3 position { get; }
    }
    
    [CustomPivot(k_DisplayName, tooltip = k_Tooltip, priority = CustomPivotAttribute.defaultPriority)]
    [Icon(k_Icon)]
    class CenterPivotMode : CustomPivotMode
    {
        const string k_DisplayName = "Center";
        const string k_Icon = "ToolHandleCenter";
        const string k_Tooltip = "The tool handle is placed at the center of the selection.";
        
        public override  Vector3 position
        {
            get
            {
                if (Tools.current == Tool.Rect)
                    return Tools.handleRotation * InternalEditorUtility
                        .CalculateSelectionBoundsInSpace(Vector3.zero, Tools.handleRotation, Tools.rectBlueprintMode)
                        .center;
                
                return InternalEditorUtility.CalculateSelectionBounds(true).center;
            }
        }
    }
    
    [CustomPivot(k_DisplayName, tooltip = k_Tooltip, priority = CustomPivotAttribute.defaultPriority + 1)]
    [Icon(k_Icon)]
    class PivotPointPivotMode : CustomPivotMode
    {
        const string k_DisplayName = "Pivot";
        const string k_Icon = "ToolHandlePivot";
        const string k_Tooltip = "The tool handle is placed at the active object's pivot point.";
        
        public override Vector3 position
        {
            get
            {
                Transform t = Selection.activeTransform;
                if (Tools.current == Tool.Rect && Tools.rectBlueprintMode && InternalEditorUtility.SupportsRectLayout(t))
                    return t.parent.TransformPoint(new Vector3(t.localPosition.x, t.localPosition.y, 0));
                
                return t.position;
            }
        }
    }
    
    public abstract class CustomPivotRotation : ScriptableObject
    {
        public abstract Quaternion rotation { get; }
    }
    
    [CustomPivot(k_DisplayName, tooltip = k_Tooltip, priority = CustomPivotAttribute.defaultPriority)]
    [Icon(k_Icon)]
    class GlobalPivotRotation : CustomPivotRotation
    {
        const string k_DisplayName = "Global";
        const string k_Icon = "ToolHandleGlobal";
        const string k_Tooltip = "Tool handles are in global rotation.";
        
        public override Quaternion rotation => Tools.globalHandleRotation;
    }
    
    [CustomPivot(k_DisplayName, tooltip = k_Tooltip, priority = CustomPivotAttribute.defaultPriority + 1)]
    [Icon(k_Icon)]
    class LocalPivotRotation : CustomPivotRotation
    {
        const string k_DisplayName = "Local";
        const string k_Icon = "ToolHandleLocal";
        const string k_Tooltip = "Tool handles are in the active object's rotation.";
        public override Quaternion rotation => RetrieveLocalRotation();

        internal static Quaternion RetrieveLocalRotation()
        {
            Transform t = Selection.activeTransform;
            if (!t)
                return Quaternion.identity;
            if (Tools.rectBlueprintMode && InternalEditorUtility.SupportsRectLayout(t))
                return t.parent.rotation;
            return t.rotation;
        }
    }

    public static class PivotManager
    {
        public static Type defaultPivotModeType => typeof(CenterPivotMode);
        public static Type defaultPivotRotationType => typeof(LocalPivotRotation);

        public static CustomPivotMode GetActivePivotMode() => EditorPivotManager.activePivotMode;
        public static CustomPivotRotation GetActivePivotRotation() => EditorPivotManager.activePivotRotation;
        
        public static void SetActivePivotMode<T>() where T : CustomPivotMode
        {
            SetActivePivotMode(typeof(T));
        }
        
        public static void SetActivePivotMode(Type pivotModeType)
        {
            EditorPivotManager.SetActivePivotMode(pivotModeType);
            
            Tools.InvalidateHandlePosition();
            activePivotModeChanged?.Invoke();
        }
        
        public static void SetActivePivotRotation<T>() where T : CustomPivotRotation
        {
            SetActivePivotRotation(typeof(T));
        }
        
        public static void SetActivePivotRotation(Type pivotRotationType)
        {
            EditorPivotManager.SetActivePivotRotation(pivotRotationType);
            
            activePivotRotationChanged?.Invoke();
        }
        
        public static event Action activePivotModeChanged;
        public static event Action activePivotRotationChanged;
    }
}
