// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEngine
{
[Flags]
public enum DrivenTransformProperties
{
    None                = 0,
    All                 = ~None,
    AnchoredPositionX   = 1 << 1,
    AnchoredPositionY   = 1 << 2,
    
    
    
    AnchoredPositionZ   = 1 << 3,
    Rotation            = 1 << 4,
    ScaleX              = 1 << 5,
    ScaleY              = 1 << 6,
    ScaleZ              = 1 << 7,
    AnchorMinX          = 1 << 8,
    AnchorMinY          = 1 << 9,
    AnchorMaxX          = 1 << 10,
    AnchorMaxY          = 1 << 11,
    SizeDeltaX          = 1 << 12,
    SizeDeltaY          = 1 << 13,
    PivotX              = 1 << 14,
    PivotY              = 1 << 15,
    
    AnchoredPosition    = AnchoredPositionX | AnchoredPositionY,
    
    AnchoredPosition3D  = AnchoredPositionX | AnchoredPositionY | AnchoredPositionZ,
    Scale               = ScaleX | ScaleY | ScaleZ,
    AnchorMin           = AnchorMinX | AnchorMinY,
    AnchorMax           = AnchorMaxX | AnchorMaxY,
    Anchors             = AnchorMin | AnchorMax,
    SizeDelta           = SizeDeltaX | SizeDeltaY,
    Pivot               = PivotX | PivotY
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct DrivenRectTransformTracker
{
    
            private List<RectTransform> m_Tracked;
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool CanRecordModifications () ;

    public void Add(Object driver, RectTransform rectTransform, DrivenTransformProperties drivenProperties)
        {
            if (m_Tracked == null)
                m_Tracked = new List<RectTransform>();

            if (!Application.isPlaying && CanRecordModifications())
                RuntimeUndo.RecordObject(rectTransform, "Driving RectTransform");

            rectTransform.drivenByObject = driver;
            rectTransform.drivenProperties = rectTransform.drivenProperties | drivenProperties;

            m_Tracked.Add(rectTransform);
        }
    
    
    [Obsolete("revertValues parameter is ignored. Please use Clear() instead.")]
    public void Clear(bool revertValues)
        {
            Clear();
        }
    
    
    public void Clear()
        {
            if (m_Tracked != null)
            {
                for (int i = 0; i < m_Tracked.Count; i++)
                {
                    if (m_Tracked[i] != null)
                    {
                        if (!Application.isPlaying && CanRecordModifications())
                            RuntimeUndo.RecordObject(m_Tracked[i], "Driving RectTransform");

                        m_Tracked[i].drivenByObject = null;
                    }
                }
                m_Tracked.Clear();
            }
        }
    
    
}

[NativeClass("UI::RectTransform")]
public sealed partial class RectTransform : Transform
{
    public Rect rect
    {
        get { Rect tmp; INTERNAL_get_rect(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_rect (out Rect value) ;


    public Vector2 anchorMin
    {
        get { Vector2 tmp; INTERNAL_get_anchorMin(out tmp); return tmp;  }
        set { INTERNAL_set_anchorMin(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_anchorMin (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_anchorMin (ref Vector2 value) ;

    public Vector2 anchorMax
    {
        get { Vector2 tmp; INTERNAL_get_anchorMax(out tmp); return tmp;  }
        set { INTERNAL_set_anchorMax(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_anchorMax (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_anchorMax (ref Vector2 value) ;

    public Vector3 anchoredPosition3D
        {
            get
            {
                Vector2 pos2 = anchoredPosition;
                return new Vector3(pos2.x, pos2.y, localPosition.z);
            }
            set
            {
                anchoredPosition = new Vector2(value.x, value.y);
                Vector3 pos3 = localPosition;
                pos3.z = value.z;
                localPosition = pos3;
            }
        }
    public Vector2 anchoredPosition
    {
        get { Vector2 tmp; INTERNAL_get_anchoredPosition(out tmp); return tmp;  }
        set { INTERNAL_set_anchoredPosition(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_anchoredPosition (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_anchoredPosition (ref Vector2 value) ;

    public Vector2 sizeDelta
    {
        get { Vector2 tmp; INTERNAL_get_sizeDelta(out tmp); return tmp;  }
        set { INTERNAL_set_sizeDelta(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_sizeDelta (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_sizeDelta (ref Vector2 value) ;

    public Vector2 pivot
    {
        get { Vector2 tmp; INTERNAL_get_pivot(out tmp); return tmp;  }
        set { INTERNAL_set_pivot(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_pivot (out Vector2 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_pivot (ref Vector2 value) ;

    internal extern  Object drivenByObject
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    internal extern  DrivenTransformProperties drivenProperties
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public delegate void ReapplyDrivenProperties(RectTransform driven);
    public static event ReapplyDrivenProperties reapplyDrivenProperties;
    [RequiredByNativeCode]
    internal static void SendReapplyDrivenProperties(RectTransform driven) { if (reapplyDrivenProperties != null) reapplyDrivenProperties(driven); }
    
    
    public void GetLocalCorners(Vector3[] fourCornersArray)
        {
            if (fourCornersArray == null || fourCornersArray.Length < 4)
            {
                Debug.LogError("Calling GetLocalCorners with an array that is null or has less than 4 elements.");
                return;
            }

            Rect tmpRect = rect;
            float x0 = tmpRect.x;
            float y0 = tmpRect.y;
            float x1 = tmpRect.xMax;
            float y1 = tmpRect.yMax;

            fourCornersArray[0] = new Vector3(x0, y0, 0f);
            fourCornersArray[1] = new Vector3(x0, y1, 0f);
            fourCornersArray[2] = new Vector3(x1, y1, 0f);
            fourCornersArray[3] = new Vector3(x1, y0, 0f);
        }
    
    
    public void GetWorldCorners(Vector3[] fourCornersArray)
        {
            if (fourCornersArray == null || fourCornersArray.Length < 4)
            {
                Debug.LogError("Calling GetWorldCorners with an array that is null or has less than 4 elements.");
                return;
            }

            GetLocalCorners(fourCornersArray);

            Matrix4x4 mat = transform.localToWorldMatrix;
            for (int i = 0; i < 4; i++)
                fourCornersArray[i] = mat.MultiplyPoint(fourCornersArray[i]);
        }
    
    
    internal Rect GetRectInParentSpace()
        {
            Rect rect = this.rect;
            Vector2 offset = offsetMin + Vector2.Scale(pivot, rect.size);
            Transform parent = transform.parent;
            if (parent)
            {
                RectTransform parentRect = parent.GetComponent<RectTransform>();
                if (parentRect)
                    offset += Vector2.Scale(anchorMin, parentRect.rect.size);
            }

            rect.x += offset.x;
            rect.y += offset.y;
            return rect;
        }
    
    
    public Vector2 offsetMin
        {
            get
            {
                return anchoredPosition - Vector2.Scale(sizeDelta, pivot);
            }
            set
            {
                Vector2 offset = value - (anchoredPosition - Vector2.Scale(sizeDelta, pivot));
                sizeDelta -= offset;
                anchoredPosition += Vector2.Scale(offset, Vector2.one - pivot);
            }
        }
    
    
    public Vector2 offsetMax
        {
            get
            {
                return anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot);
            }
            set
            {
                Vector2 offset = value - (anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot));
                sizeDelta += offset;
                anchoredPosition += Vector2.Scale(offset, pivot);
            }
        }
    
    
    public enum Edge { Left = 0, Right = 1, Top = 2, Bottom = 3 }
    
    
    public void SetInsetAndSizeFromParentEdge(Edge edge, float inset, float size)
        {
            int axis = (edge == Edge.Top || edge == Edge.Bottom) ? 1 : 0;
            bool end = (edge == Edge.Top || edge == Edge.Right);

            float anchorValue = end ? 1 : 0;
            Vector2 anchor = anchorMin;
            anchor[axis] = anchorValue;
            anchorMin = anchor;
            anchor = anchorMax;
            anchor[axis] = anchorValue;
            anchorMax = anchor;

            Vector2 sizeD = sizeDelta;
            sizeD[axis] = size;
            sizeDelta = sizeD;

            Vector2 position = anchoredPosition;
            position[axis] = end ? -inset - size * (1 - pivot[axis]) : inset + size * pivot[axis];
            anchoredPosition = position;
        }
    
    
    public enum Axis { Horizontal = 0, Vertical = 1 }
    
    
    public void SetSizeWithCurrentAnchors(Axis axis, float size)
        {
            int i = (int)axis;
            Vector2 sizeD = sizeDelta;
            sizeD[i] = size - GetParentSize()[i] * (anchorMax[i] - anchorMin[i]);
            sizeDelta = sizeD;
        }
    
    
    private Vector2 GetParentSize()
        {
            RectTransform parentRect = parent as RectTransform;
            if (!parentRect)
                return Vector2.zero;
            return parentRect.rect.size;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void ForceUpdateRectTransforms () ;

}


}
