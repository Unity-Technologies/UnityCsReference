// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using UnityEngineInternal;
using UnityEditorInternal;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor
{



[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct DrawGridParameters
{
    public Vector3 pivot;
    public Color   color;
    public float   size;
    public float   alphaX;
    public float   alphaY;
    public float   alphaZ;
}

public sealed partial class Handles
{
    internal static PrefColor s_XAxisColor = new PrefColor("Scene/X Axis", 219f / 255, 62f / 255, 29f / 255, .93f);
    public static Color xAxisColor { get { return s_XAxisColor; } }
    internal static PrefColor s_YAxisColor = new PrefColor("Scene/Y Axis", 154f / 255, 243f / 255, 72f / 255, .93f);
    public static Color yAxisColor { get { return s_YAxisColor; } }
    internal static PrefColor s_ZAxisColor = new PrefColor("Scene/Z Axis", 58f / 255, 122f / 255, 248f / 255, .93f);
    public static Color zAxisColor { get { return s_ZAxisColor; } }
    internal static PrefColor s_CenterColor = new PrefColor("Scene/Center Axis", .8f, .8f, .8f, .93f);
    public static Color centerColor { get { return s_CenterColor; } }
    internal static PrefColor s_SelectedColor = new PrefColor("Scene/Selected Axis", 246f / 255, 242f / 255, 50f / 255, .89f);
    public static Color selectedColor { get { return s_SelectedColor; } }
    internal static PrefColor s_PreselectionColor = new PrefColor("Scene/Preselection Highlight", 201f / 255, 200f / 255, 144f / 255, 0.89f);
    public static Color preselectionColor { get { return s_PreselectionColor; } }
    internal static PrefColor s_SecondaryColor = new PrefColor("Scene/Guide Line", .5f, .5f, .5f, .2f);
    public static Color secondaryColor { get { return s_SecondaryColor; } }
    internal static Color staticColor = new Color(.5f, .5f, .5f, 0f);
    internal static float staticBlend = 0.6f;
    
    
    internal static float backfaceAlphaMultiplier = 0.2f;
    internal static Color s_ColliderHandleColor = new Color(145f, 244f, 139f, 210f) / 255;
    internal static Color s_ColliderHandleColorDisabled = new Color(84, 200f, 77f, 140f) / 255;
    internal static Color s_BoundingBoxHandleColor = new Color(255, 255, 255, 150) / 255;
    
    
    private const int kMaxDottedLineVertices = 1000;
    
            internal static int s_SliderHash = "SliderHash".GetHashCode();
            internal static int s_Slider2DHash = "Slider2DHash".GetHashCode();
            internal static int s_FreeRotateHandleHash = "FreeRotateHandleHash".GetHashCode();
            internal static int s_RadiusHandleHash = "RadiusHandleHash".GetHashCode();
            internal static int s_xAxisMoveHandleHash  = "xAxisFreeMoveHandleHash".GetHashCode();
            internal static int s_yAxisMoveHandleHash  = "yAxisFreeMoveHandleHash".GetHashCode();
            internal static int s_zAxisMoveHandleHash  = "zAxisFreeMoveHandleHash".GetHashCode();
            internal static int s_FreeMoveHandleHash  = "FreeMoveHandleHash".GetHashCode();
            internal static int s_xzAxisMoveHandleHash = "xzAxisFreeMoveHandleHash".GetHashCode();
            internal static int s_xyAxisMoveHandleHash = "xyAxisFreeMoveHandleHash".GetHashCode();
            internal static int s_yzAxisMoveHandleHash = "yzAxisFreeMoveHandleHash".GetHashCode();
            internal static int s_xAxisScaleHandleHash = "xAxisScaleHandleHash".GetHashCode();
            internal static int s_yAxisScaleHandleHash = "yAxisScaleHandleHash".GetHashCode();
            internal static int s_zAxisScaleHandleHash = "zAxisScaleHandleHash".GetHashCode();
            internal static int s_ScaleSliderHash = "ScaleSliderHash".GetHashCode();
            internal static int s_ScaleValueHandleHash = "ScaleValueHandleHash".GetHashCode();
            internal static int s_DiscHash = "DiscHash".GetHashCode();
            internal static int s_ButtonHash = "ButtonHash".GetHashCode();
    
    
    public extern static bool lighting
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public static Color color
    {
        get { Color tmp; INTERNAL_get_color(out tmp); return tmp;  }
        set { INTERNAL_set_color(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_color (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_color (ref Color value) ;

    public extern static CompareFunction zTest
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public static Matrix4x4 matrix
    {
        get { Matrix4x4 tmp; INTERNAL_get_matrix(out tmp); return tmp;  }
        set { INTERNAL_set_matrix(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_matrix (out Matrix4x4 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_set_matrix (ref Matrix4x4 value) ;

    public static Matrix4x4 inverseMatrix
    {
        get { Matrix4x4 tmp; INTERNAL_get_inverseMatrix(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static void INTERNAL_get_inverseMatrix (out Matrix4x4 value) ;


    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void ClearHandles () ;

    public static Vector3 PositionHandle(Vector3 position, Quaternion rotation)
        {
            return DoPositionHandle(position, rotation);
        }
    
    public static Quaternion RotationHandle(Quaternion rotation, Vector3 position)
        {
            return DoRotationHandle(rotation, position);
        }
    
    public static Vector3 ScaleHandle(Vector3 scale, Vector3 position, Quaternion rotation, float size)
        {
            return DoScaleHandle(scale, position, rotation, size);
        }
    
    
    public static float RadiusHandle(Quaternion rotation, Vector3 position, float radius, bool handlesOnly)
        {
            return DoRadiusHandle(rotation, position, radius, handlesOnly);
        }
    
    public static float RadiusHandle(Quaternion rotation, Vector3 position, float radius)
        {
            return DoRadiusHandle(rotation, position, radius, false);
        }
    
    internal static Vector2 ConeHandle(Quaternion rotation, Vector3 position, Vector2 angleAndRange, float angleScale, float rangeScale, bool handlesOnly)
        {
            return DoConeHandle(rotation, position, angleAndRange, angleScale, rangeScale, handlesOnly);
        }
    
    internal static Vector3 ConeFrustrumHandle(Quaternion rotation, Vector3 position, Vector3 radiusAngleRange)
        {
            return DoConeFrustrumHandle(rotation, position, radiusAngleRange);
        }
    
    
    [uei.ExcludeFromDocs]
public static Vector3 Slider2D (int id, Vector3 handlePos, Vector3 offset, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, Vector2 snap) {
    bool drawHelper = false;
    return Slider2D ( id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, drawHelper );
}

public static Vector3 Slider2D(int id, Vector3 handlePos, Vector3 offset, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, Vector2 snap, [uei.DefaultValue("false")]  bool drawHelper )
        {
            return UnityEditorInternal.Slider2D.Do(id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, drawHelper);
        }

    
    
    [System.Obsolete ("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
[uei.ExcludeFromDocs]
public static Vector3 Slider2D (int id, Vector3 handlePos, Vector3 offset, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, DrawCapFunction drawFunc, Vector2 snap) {
    bool drawHelper = false;
    return Slider2D ( id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize, drawFunc, snap, drawHelper );
}

[System.Obsolete ("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
public static Vector3 Slider2D(int id, Vector3 handlePos, Vector3 offset, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, DrawCapFunction drawFunc, Vector2 snap, [uei.DefaultValue("false")]  bool drawHelper )
        {
            return UnityEditorInternal.Slider2D.Do(id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize, drawFunc, snap, drawHelper);
        }

    
    
    [uei.ExcludeFromDocs]
public static Vector3 Slider2D (Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, Vector2 snap) {
    bool drawHelper = false;
    return Slider2D ( handlePos, handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, drawHelper );
}

public static Vector3 Slider2D(Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, Vector2 snap, [uei.DefaultValue("false")]  bool drawHelper )
        {
            int id = GUIUtility.GetControlID(s_Slider2DHash, FocusType.Passive);
            return UnityEditorInternal.Slider2D.Do(id, handlePos, new Vector3(0, 0, 0), handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, drawHelper);
        }

    
    
    [System.Obsolete ("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
[uei.ExcludeFromDocs]
public static Vector3 Slider2D (Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, DrawCapFunction drawFunc, Vector2 snap) {
    bool drawHelper = false;
    return Slider2D ( handlePos, handleDir, slideDir1, slideDir2, handleSize, drawFunc, snap, drawHelper );
}

[System.Obsolete ("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
public static Vector3 Slider2D(Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, DrawCapFunction drawFunc, Vector2 snap, [uei.DefaultValue("false")]  bool drawHelper )
        {
            int id = GUIUtility.GetControlID(s_Slider2DHash, FocusType.Passive);
            return UnityEditorInternal.Slider2D.Do(id, handlePos, new Vector3(0, 0, 0), handleDir, slideDir1, slideDir2, handleSize, drawFunc, snap, drawHelper);
        }

    
    
    [uei.ExcludeFromDocs]
public static Vector3 Slider2D (int id, Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, Vector2 snap) {
    bool drawHelper = false;
    return Slider2D ( id, handlePos, handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, drawHelper );
}

public static Vector3 Slider2D(int id, Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, Vector2 snap, [uei.DefaultValue("false")]  bool drawHelper )
        {
            return UnityEditorInternal.Slider2D.Do(id, handlePos, new Vector3(0, 0, 0), handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, drawHelper);
        }

    
    
    [System.Obsolete ("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
[uei.ExcludeFromDocs]
public static Vector3 Slider2D (int id, Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, DrawCapFunction drawFunc, Vector2 snap) {
    bool drawHelper = false;
    return Slider2D ( id, handlePos, handleDir, slideDir1, slideDir2, handleSize, drawFunc, snap, drawHelper );
}

[System.Obsolete ("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
public static Vector3 Slider2D(int id, Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, DrawCapFunction drawFunc, Vector2 snap, [uei.DefaultValue("false")]  bool drawHelper )
        {
            return UnityEditorInternal.Slider2D.Do(id, handlePos, new Vector3(0, 0, 0), handleDir, slideDir1, slideDir2, handleSize, drawFunc, snap, drawHelper);
        }

    
    
    [uei.ExcludeFromDocs]
public static Vector3 Slider2D (Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, float snap) {
    bool drawHelper = false;
    return Slider2D ( handlePos, handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, drawHelper );
}

public static Vector3 Slider2D(Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, float snap, [uei.DefaultValue("false")]  bool drawHelper )
        {
            int id = GUIUtility.GetControlID(s_Slider2DHash, FocusType.Passive);
            return Slider2D(id, handlePos, new Vector3(0, 0, 0), handleDir, slideDir1, slideDir2, handleSize, capFunction, new Vector2(snap, snap), drawHelper);
        }

    
    
    [System.Obsolete ("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
[uei.ExcludeFromDocs]
public static Vector3 Slider2D (Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, DrawCapFunction drawFunc, float snap) {
    bool drawHelper = false;
    return Slider2D ( handlePos, handleDir, slideDir1, slideDir2, handleSize, drawFunc, snap, drawHelper );
}

[System.Obsolete ("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
public static Vector3 Slider2D(Vector3 handlePos, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, DrawCapFunction drawFunc, float snap, [uei.DefaultValue("false")]  bool drawHelper )
        {
            int id = GUIUtility.GetControlID(s_Slider2DHash, FocusType.Passive);
            return Slider2D(id, handlePos, new Vector3(0, 0, 0), handleDir, slideDir1, slideDir2, handleSize, drawFunc, new Vector2(snap, snap), drawHelper);
        }

    
    
    public static Quaternion FreeRotateHandle(Quaternion rotation, Vector3 position, float size)
        {
            int id = GUIUtility.GetControlID(s_FreeRotateHandleHash, FocusType.Passive);
            return UnityEditorInternal.FreeRotate.Do(id, rotation, position, size);
        }
    
    
    public static float ScaleSlider(float scale, Vector3 position, Vector3 direction, Quaternion rotation, float size, float snap)
        {
            int id = GUIUtility.GetControlID(s_ScaleSliderHash, FocusType.Passive);
            return UnityEditorInternal.SliderScale.DoAxis(id, scale, position, direction, rotation, size, snap);
        }
    
    
    public static Quaternion Disc(Quaternion rotation, Vector3 position, Vector3 axis, float size, bool cutoffPlane, float snap)
        {
            int id = GUIUtility.GetControlID(s_DiscHash, FocusType.Passive);
            return UnityEditorInternal.Disc.Do(id, rotation, position, axis, size, cutoffPlane, snap);
        }
    
    
    internal static void SetupIgnoreRaySnapObjects()
        {
            HandleUtility.ignoreRaySnapObjects = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.Deep);
        }
    
    
    public static float SnapValue(float val, float snap)
        {
            if (EditorGUI.actionKey && snap > 0)
            {
                return Mathf.Round(val / snap) * snap;
            }
            return val;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool Internal_IsCameraDrawModeEnabled (Camera camera, int drawMode) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_DrawCameraWithGrid (Camera cam, int renderMode, ref DrawGridParameters gridParam) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_DrawCamera (Camera cam, int renderMode) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_FinishDrawingCamera (Camera cam, [uei.DefaultValue("true")]  bool drawGizmos ) ;

    [uei.ExcludeFromDocs]
    private static void Internal_FinishDrawingCamera (Camera cam) {
        bool drawGizmos = true;
        Internal_FinishDrawingCamera ( cam, drawGizmos );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_ClearCamera (Camera cam) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void Internal_SetCurrentCamera (Camera cam) ;

    internal static void SetSceneViewColors (Color wire, Color wireOverlay, Color selectedOutline, Color selectedWire) {
        INTERNAL_CALL_SetSceneViewColors ( ref wire, ref wireOverlay, ref selectedOutline, ref selectedWire );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetSceneViewColors (ref Color wire, ref Color wireOverlay, ref Color selectedOutline, ref Color selectedWire);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void EnableCameraFx (Camera cam, bool fx) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void EnableCameraFlares (Camera cam, bool flares) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void EnableCameraSkybox (Camera cam, bool skybox) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetCameraOnlyDrawMesh (Camera cam) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetupCamera (Camera cam) ;

    public Camera currentCamera { get { return Camera.current; } set { Internal_SetCurrentCamera(value); } }
    
    
    
    internal static Color realHandleColor { get { return color * new Color(1, 1, 1, .5f) + (lighting ? new Color(0, 0, 0, .5f) : new Color(0, 0, 0, 0)); } }
    
    
    internal static void DrawTwoShadedWireDisc(Vector3 position, Vector3 axis, float radius)
        {
            Color col = Handles.color;
            Color origCol = col;
            col.a *= backfaceAlphaMultiplier;
            Handles.color = col;
            Handles.DrawWireDisc(position, axis, radius);
            Handles.color = origCol;
        }
    
    internal static void DrawTwoShadedWireDisc(Vector3 position, Vector3 axis, Vector3 from, float degrees, float radius)
        {
            Handles.DrawWireArc(position, axis, from, degrees, radius);
            Color col = Handles.color;
            Color origCol = col;
            col.a *= backfaceAlphaMultiplier;
            Handles.color = col;
            Handles.DrawWireArc(position, axis, from, degrees - 360, radius);
            Handles.color = origCol;
        }
    
    
    internal static Matrix4x4 StartCapDraw(Vector3 position, Quaternion rotation, float size)
        {
            Shader.SetGlobalColor("_HandleColor", realHandleColor);
            Shader.SetGlobalFloat("_HandleSize", size);
            Matrix4x4 mat = matrix * Matrix4x4.TRS(position, rotation, Vector3.one);
            Shader.SetGlobalMatrix("_ObjectToWorld", mat);
            HandleUtility.handleMaterial.SetInt("_HandleZTest", (int)zTest);
            HandleUtility.handleMaterial.SetPass(0);
            return mat;
        }
    
    
    [System.Obsolete ("Use CubeHandleCap instead")]
public static void CubeCap(int controlID, Vector3 position, Quaternion rotation, float size)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Graphics.DrawMeshNow(cubeMesh, StartCapDraw(position, rotation, size));
        }
    
    
    [System.Obsolete ("Use SphereHandleCap instead")]
public static void SphereCap(int controlID, Vector3 position, Quaternion rotation, float size)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Graphics.DrawMeshNow(sphereMesh, StartCapDraw(position, rotation, size));
        }
    
    
    [System.Obsolete ("Use ConeHandleCap instead")]
public static void ConeCap(int controlID, Vector3 position, Quaternion rotation, float size)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Graphics.DrawMeshNow(coneMesh, StartCapDraw(position, rotation, size));
        }
    
    
    [System.Obsolete ("Use CylinderHandleCap instead")]
public static void CylinderCap(int controlID, Vector3 position, Quaternion rotation, float size)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Graphics.DrawMeshNow(cylinderMesh, StartCapDraw(position, rotation, size));
        }
    
            static Vector3[] s_RectangleCapPointsCache = new Vector3[5];
    [System.Obsolete ("Use RectangleHandleCap instead")]
public static void RectangleCap(int controlID, Vector3 position, Quaternion rotation, float size)
        {
            RectangleCap(controlID, position, rotation, new Vector2(size, size));
        }
    
    
    internal static void RectangleCap(int controlID, Vector3 position, Quaternion rotation, Vector2 size)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Vector3 sideways = rotation * new Vector3(size.x, 0, 0);
            Vector3 up = rotation * new Vector3(0, size.y, 0);
            s_RectangleCapPointsCache[0] = position + sideways + up;
            s_RectangleCapPointsCache[1] = position + sideways - up;
            s_RectangleCapPointsCache[2] = position - sideways - up;
            s_RectangleCapPointsCache[3] = position - sideways + up;
            s_RectangleCapPointsCache[4] = position + sideways + up;
            Handles.DrawPolyLine(s_RectangleCapPointsCache);
        }
    
    
    public static void SelectionFrame(int controlID, Vector3 position, Quaternion rotation, float size)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Handles.StartCapDraw(position, rotation, size);
            Vector3 sideways = rotation * new Vector3(size, 0, 0);
            Vector3 up = rotation * new Vector3(0, size, 0);

            var point1 = position - sideways + up;
            var point2 = position + sideways + up;
            var point3 = position + sideways - up;
            var point4 = position - sideways - up;

            Handles.DrawLine(point1, point2);
            Handles.DrawLine(point2, point3);
            Handles.DrawLine(point3, point4);
            Handles.DrawLine(point4, point1);
        }
    
    
    [System.Obsolete ("Use DotHandleCap instead")]
public static void DotCap(int controlID, Vector3 position,  Quaternion rotation, float size)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            position = matrix.MultiplyPoint(position);

            Vector3 sideways = Camera.current.transform.right * size;
            Vector3 up = Camera.current.transform.up * size;

            Color col = color * new Color(1, 1, 1, 0.99f);
            HandleUtility.ApplyWireMaterial(zTest);
            GL.Begin(GL.QUADS);
            GL.Color(col);
            GL.Vertex(position + sideways + up);
            GL.Vertex(position + sideways - up);
            GL.Vertex(position - sideways - up);
            GL.Vertex(position - sideways + up);
            GL.End();
        }
    
    
    [System.Obsolete ("Use CircleHandleCap instead")]
public static void CircleCap(int controlID, Vector3 position, Quaternion rotation, float size)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            StartCapDraw(position, rotation, size);
            Vector3 forward = rotation * new Vector3(0, 0, 1);
            Handles.DrawWireDisc(position, forward, size);
        }
    
    
    [System.Obsolete ("Use ArrowHandleCap instead")]
public static void ArrowCap(int controlID, Vector3 position, Quaternion rotation, float size)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Vector3 direction = rotation * Vector3.forward;
            ConeCap(controlID, position + direction * size, Quaternion.LookRotation(direction), size * .2f);
            Handles.DrawLine(position, position + direction * size * .9f);
        }
    
    
    [System.Obsolete ("DrawCylinder has been renamed to CylinderCap.")]
public static void DrawCylinder(int controlID, Vector3 position, Quaternion rotation, float size)
        { CylinderCap(controlID, position, rotation, size); }
    
    
    [System.Obsolete ("DrawSphere has been renamed to SphereCap.")]
public static void DrawSphere(int controlID, Vector3 position, Quaternion rotation, float size)
        { SphereCap(controlID, position, rotation, size); }
    
    
    [System.Obsolete ("DrawRectangle has been renamed to RectangleCap.")]
public static void DrawRectangle(int controlID, Vector3 position, Quaternion rotation, float size)
        { RectangleCap(controlID, position, rotation, size); }
    
    
    
    [System.Obsolete ("DrawCube has been renamed to CubeCap.")]
public static void DrawCube(int controlID, Vector3 position, Quaternion rotation, float size)
        { CubeCap(controlID, position, rotation, size); }
    
    
    [System.Obsolete ("DrawArrow has been renamed to ArrowCap.")]
public static void DrawArrow(int controlID, Vector3 position, Quaternion rotation, float size)
        { ArrowCap(controlID, position, rotation, size); }
    
    
    [System.Obsolete ("DrawCone has been renamed to ConeCap.")]
public static void DrawCone(int controlID, Vector3 position, Quaternion rotation, float size)
        { ConeCap(controlID, position, rotation, size); }
    
    
    internal static void DrawAAPolyLine(Color[] colors, Vector3[] points)                { DoDrawAAPolyLine(colors, points, -1, null, 2, 0.75f); }
    internal static void DrawAAPolyLine(float width, Color[] colors, Vector3[] points)   { DoDrawAAPolyLine(colors, points, -1, null, width, 0.75f); }
    public static void DrawAAPolyLine(params Vector3[] points)                       { DoDrawAAPolyLine(null, points, -1, null, 2, 0.75f); }
    public static void DrawAAPolyLine(float width, params Vector3[] points)          { DoDrawAAPolyLine(null, points, -1, null, width, 0.75f); }
    public static void DrawAAPolyLine(Texture2D lineTex, params Vector3[] points)    { DoDrawAAPolyLine(null, points, -1, lineTex, lineTex.height / 2, 0.99f); }
    public static void DrawAAPolyLine(float width, int actualNumberOfPoints, params Vector3[] points) { DoDrawAAPolyLine(null, points, actualNumberOfPoints, null, width, 0.75f); }
    
    
    public static void DrawAAPolyLine(Texture2D lineTex, float width, params Vector3[] points) {  DoDrawAAPolyLine(null, points, -1, lineTex, width, 0.99f); }
    
    
    
    private static void DoDrawAAPolyLine(Color[] colors, Vector3[] points, int actualNumberOfPoints, Texture2D lineTex, float width, float alpha)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            HandleUtility.ApplyWireMaterial(zTest);

            Color defaultColor = new Color(1, 1, 1, alpha);

            if (colors != null)
            {
                for (int i = 0; i < colors.Length; i++)
                    colors[i] *= defaultColor;
            }
            else
                defaultColor *= color;

            Internal_DrawAAPolyLine(colors, points, defaultColor, actualNumberOfPoints, lineTex, width, matrix);
        }
    
    
    private static void Internal_DrawAAPolyLine (Color[] colors, Vector3[] points, Color defaultColor, int actualNumberOfPoints, Texture2D texture, float width, Matrix4x4 toWorld) {
        INTERNAL_CALL_Internal_DrawAAPolyLine ( colors, points, ref defaultColor, actualNumberOfPoints, texture, width, ref toWorld );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_DrawAAPolyLine (Color[] colors, Vector3[] points, ref Color defaultColor, int actualNumberOfPoints, Texture2D texture, float width, ref Matrix4x4 toWorld);
    public static void DrawAAConvexPolygon(params Vector3[] points) {  DoDrawAAConvexPolygon(points, -1, 1.0f); }
    
    
    private static void DoDrawAAConvexPolygon(Vector3[] points, int actualNumberOfPoints, float alpha)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            HandleUtility.ApplyWireMaterial(zTest);

            Color defaultColor = new Color(1, 1, 1, alpha) * color;
            Internal_DrawAAConvexPolygon(points, defaultColor, actualNumberOfPoints, matrix);
        }
    
    
    private static void Internal_DrawAAConvexPolygon (Vector3[] points, Color defaultColor, int actualNumberOfPoints, Matrix4x4 toWorld) {
        INTERNAL_CALL_Internal_DrawAAConvexPolygon ( points, ref defaultColor, actualNumberOfPoints, ref toWorld );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_DrawAAConvexPolygon (Vector3[] points, ref Color defaultColor, int actualNumberOfPoints, ref Matrix4x4 toWorld);
    public static void DrawBezier(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, Color color, Texture2D texture, float width)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            HandleUtility.ApplyWireMaterial(zTest);
            Internal_DrawBezier(startPosition, endPosition, startTangent, endTangent, color, texture, width, matrix);
        }
    
    
    private static void Internal_DrawBezier (Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, Color color, Texture2D texture, float width, Matrix4x4 toWorld) {
        INTERNAL_CALL_Internal_DrawBezier ( ref startPosition, ref endPosition, ref startTangent, ref endTangent, ref color, texture, width, ref toWorld );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_DrawBezier (ref Vector3 startPosition, ref Vector3 endPosition, ref Vector3 startTangent, ref Vector3 endTangent, ref Color color, Texture2D texture, float width, ref Matrix4x4 toWorld);
    public static void DrawWireDisc(Vector3 center, Vector3 normal, float radius)
        {
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < .001f)
                tangent = Vector3.Cross(normal, Vector3.right);
            DrawWireArc(center, normal, tangent, 360, radius);
        }
    
            private static readonly Vector3[] s_WireArcPoints = new Vector3[60];
    public static void DrawWireArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius)
        {
            SetDiscSectionPoints(s_WireArcPoints, center, normal, from, angle, radius);
            Handles.DrawPolyLine(s_WireArcPoints);
        }
    
    
    public static void DrawSolidRectangleWithOutline(Rect rectangle, Color faceColor, Color outlineColor)
        {
            Vector3[] points =
            {
                new Vector3(rectangle.xMin, rectangle.yMin, 0.0f),
                new Vector3(rectangle.xMax, rectangle.yMin, 0.0f),
                new Vector3(rectangle.xMax, rectangle.yMax, 0.0f),
                new Vector3(rectangle.xMin, rectangle.yMax, 0.0f)
            };

            Handles.DrawSolidRectangleWithOutline(points, faceColor, outlineColor);
        }
    
    
    public static void DrawSolidRectangleWithOutline(Vector3[] verts, Color faceColor, Color outlineColor)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            HandleUtility.ApplyWireMaterial(zTest);

            GL.PushMatrix();
            GL.MultMatrix(matrix);

            if (faceColor.a > 0)
            {
                Color col = faceColor * color;
                GL.Begin(GL.TRIANGLES);
                for (int i = 0; i < 2; i++)
                {
                    GL.Color(col);
                    GL.Vertex(verts[i * 2 + 0]);
                    GL.Vertex(verts[i * 2 + 1]);
                    GL.Vertex(verts[(i * 2 + 2) % 4]);

                    GL.Vertex(verts[i * 2 + 0]);
                    GL.Vertex(verts[(i * 2 + 2) % 4]);
                    GL.Vertex(verts[i * 2 + 1]);
                }
                GL.End();
            }

            if (outlineColor.a > 0)
            {
                Color col = outlineColor * color;
                GL.Begin(GL.LINES);
                GL.Color(col);
                for (int i = 0; i < 4; i++)
                {
                    GL.Vertex(verts[i]);
                    GL.Vertex(verts[(i + 1) % 4]);
                }
                GL.End();
            }

            GL.PopMatrix();
        }
    
    
    public static void DrawSolidDisc(Vector3 center, Vector3 normal, float radius)
        {
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < .001f)
                tangent = Vector3.Cross(normal, Vector3.right);
            DrawSolidArc(center, normal, tangent, 360, radius);
        }
    
    
    public static void DrawSolidArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            SetDiscSectionPoints(s_WireArcPoints, center, normal, from, angle, radius);

            Shader.SetGlobalColor("_HandleColor", color * new Color(1, 1, 1, .5f));
            Shader.SetGlobalFloat("_HandleSize", 1);

            HandleUtility.ApplyWireMaterial(zTest);

            GL.PushMatrix();
            GL.MultMatrix(matrix);
            GL.Begin(GL.TRIANGLES);
            for (int i = 1, count = s_WireArcPoints.Length; i < count; ++i)
            {
                GL.Color(color);
                GL.Vertex(center);
                GL.Vertex(s_WireArcPoints[i - 1]);
                GL.Vertex(s_WireArcPoints[i]);
                GL.Vertex(center);
                GL.Vertex(s_WireArcPoints[i]);
                GL.Vertex(s_WireArcPoints[i - 1]);
            }
            GL.End();
            GL.PopMatrix();
        }
    
    
    internal static void SetDiscSectionPoints (Vector3[] dest, Vector3 center, Vector3 normal, Vector3 from, float angle, float radius) {
        INTERNAL_CALL_SetDiscSectionPoints ( dest, ref center, ref normal, ref from, angle, radius );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetDiscSectionPoints (Vector3[] dest, ref Vector3 center, ref Vector3 normal, ref Vector3 from, float angle, float radius);
    
            internal static Mesh s_CubeMesh, s_SphereMesh, s_ConeMesh, s_CylinderMesh, s_QuadMesh;
    internal static void Init()
        {
            if (!s_CubeMesh)
            {
                GameObject handleGo = (GameObject)EditorGUIUtility.Load("SceneView/HandlesGO.fbx");
                if (!handleGo)
                {
                    Debug.Log("Couldn't find SceneView/HandlesGO.fbx");
                }
                handleGo.SetActive(false);

                const string k_AssertMessage = "mesh is null. A problem has occurred with `SceneView/HandlesGO.fbx`";

                foreach (Transform t in handleGo.transform)
                {
                    var meshFilter = t.GetComponent<MeshFilter>();
                    switch (t.name)
                    {
                        case "Cube":
                            s_CubeMesh = meshFilter.sharedMesh;
                            Debug.AssertFormat(s_CubeMesh != null, k_AssertMessage);
                            break;
                        case "Sphere":
                            s_SphereMesh = meshFilter.sharedMesh;
                            Debug.AssertFormat(s_SphereMesh != null, k_AssertMessage);
                            break;
                        case "Cone":
                            s_ConeMesh = meshFilter.sharedMesh;
                            Debug.AssertFormat(s_ConeMesh != null, k_AssertMessage);
                            break;
                        case "Cylinder":
                            s_CylinderMesh = meshFilter.sharedMesh;
                            Debug.AssertFormat(s_CylinderMesh != null, k_AssertMessage);
                            break;
                        case "Quad":
                            s_QuadMesh = meshFilter.sharedMesh;
                            Debug.AssertFormat(s_QuadMesh != null, k_AssertMessage);
                            break;
                    }
                }
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                     
                    ReplaceFontForWindows((Font)EditorGUIUtility.LoadRequired(EditorResourcesUtility.fontsPath + "Lucida Grande.ttf"));
                    ReplaceFontForWindows((Font)EditorGUIUtility.LoadRequired(EditorResourcesUtility.fontsPath + "Lucida Grande Bold.ttf"));
                    ReplaceFontForWindows((Font)EditorGUIUtility.LoadRequired(EditorResourcesUtility.fontsPath + "Lucida Grande Small.ttf"));
                    ReplaceFontForWindows((Font)EditorGUIUtility.LoadRequired(EditorResourcesUtility.fontsPath + "Lucida Grande Small Bold.ttf"));
                    ReplaceFontForWindows((Font)EditorGUIUtility.LoadRequired(EditorResourcesUtility.fontsPath + "Lucida Grande Big.ttf"));
                }
            }
        }
    
    static void ReplaceFontForWindows(Font font)
        {
            if (font.name.Contains("Bold"))
                font.fontNames = new string[] { "Verdana Bold", "Tahoma Bold" };
            else
                font.fontNames = new string[] { "Verdana", "Tahoma" };
            font.hideFlags = HideFlags.HideAndDontSave;
        }
    
    
    public static void Label(Vector3 position, string text)                          { Label(position, EditorGUIUtility.TempContent(text), GUI.skin.label); }
    public static void Label(Vector3 position, Texture image)                        { Label(position, EditorGUIUtility.TempContent(image), GUI.skin.label); }
    public static void Label(Vector3 position, GUIContent content)                   { Label(position, content, GUI.skin.label); }
    public static void Label(Vector3 position, string text, GUIStyle style)              { Label(position, EditorGUIUtility.TempContent(text), style); }
    public static void Label(Vector3 position, GUIContent content, GUIStyle style)
        {
            Handles.BeginGUI();
            GUI.Label(HandleUtility.WorldPointToSizedRect(position, content, style), content, style);
            Handles.EndGUI();
        }
    
    
    internal static Rect GetCameraRect(Rect position)
        {
            Rect screenRect = GUIClip.Unclip(position);
            Rect cameraRect = new Rect(screenRect.xMin, Screen.height - screenRect.yMax, screenRect.width, screenRect.height);
            return cameraRect;
        }
    
    
    public static Vector2 GetMainGameViewSize()
        {
            return GameView.GetMainGameViewTargetSize();
        }
    
    
    internal static bool IsCameraDrawModeEnabled(Camera camera, DrawCameraMode drawMode)
        {
            return Internal_IsCameraDrawModeEnabled(camera, (int)drawMode);
        }
    
    
    public static void ClearCamera(Rect position, Camera camera)
        {
            Event evt = Event.current;
            if (camera.targetTexture == null)
            {
                Rect screenRect = GUIClip.Unclip(position);
                screenRect = EditorGUIUtility.PointsToPixels(screenRect);
                Rect cameraRect = new Rect(screenRect.xMin, Screen.height - screenRect.yMax, screenRect.width, screenRect.height);
                camera.pixelRect = cameraRect;
            }
            else
            {
                camera.rect = new Rect(0, 0, 1, 1);
            }
            if (evt.type == EventType.Repaint)
                Internal_ClearCamera(camera);
            else
                Internal_SetCurrentCamera(camera);
        }
    
    
    [uei.ExcludeFromDocs]
internal static void DrawCameraImpl (Rect position, Camera camera, DrawCameraMode drawMode, bool drawGrid, DrawGridParameters gridParam, bool finish) {
    bool renderGizmos = true;
    DrawCameraImpl ( position, camera, drawMode, drawGrid, gridParam, finish, renderGizmos );
}

internal static void DrawCameraImpl(Rect position, Camera camera,
            DrawCameraMode drawMode, bool drawGrid, DrawGridParameters gridParam, bool finish, [uei.DefaultValue("true")]  bool renderGizmos )
        {
            Event evt = Event.current;

            if (evt.type == EventType.Repaint)
            {
                if (camera.targetTexture == null)
                {
                    Rect screenRect = GUIClip.Unclip(position);
                    screenRect = EditorGUIUtility.PointsToPixels(screenRect);
                    camera.pixelRect = new Rect(screenRect.xMin, Screen.height - screenRect.yMax, screenRect.width, screenRect.height);
                }
                else
                {
                    camera.rect = new Rect(0, 0, 1, 1);
                }
                if (drawMode == DrawCameraMode.Normal)
                {
                    RenderTexture temp = camera.targetTexture;
                    camera.targetTexture = RenderTexture.active;
                    camera.Render();
                    camera.targetTexture = temp;
                }
                else
                {
                    if (drawGrid)
                        Internal_DrawCameraWithGrid(camera, (int)drawMode, ref gridParam);
                    else
                        Internal_DrawCamera(camera, (int)drawMode);

                    if (finish && camera.cameraType != CameraType.VR)
                        Internal_FinishDrawingCamera(camera, renderGizmos);
                }
            }
            else
                Internal_SetCurrentCamera(camera);
        }

    
    
    internal static void DrawCamera(Rect position, Camera camera, DrawCameraMode drawMode, DrawGridParameters gridParam)
        {
            DrawCameraImpl(position, camera, drawMode, true, gridParam, true);
        }
    
    
    internal static void DrawCameraStep1(Rect position, Camera camera, DrawCameraMode drawMode, DrawGridParameters gridParam)
        {
            DrawCameraImpl(position, camera, drawMode, true, gridParam, false);
        }
    
    
    internal static void DrawCameraStep2(Camera camera, DrawCameraMode drawMode)
        {
            if (Event.current.type == EventType.Repaint && drawMode != DrawCameraMode.Normal)
                Internal_FinishDrawingCamera(camera);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void EmitGUIGeometryForCamera (Camera source, Camera dest) ;

    [uei.ExcludeFromDocs]
public static void DrawCamera (Rect position, Camera camera) {
    DrawCameraMode drawMode = DrawCameraMode.Normal;
    DrawCamera ( position, camera, drawMode );
}

public static void DrawCamera(Rect position, Camera camera, [uei.DefaultValue("DrawCameraMode.Normal")]  DrawCameraMode drawMode )
        {
            DrawGridParameters nullGridParam = new DrawGridParameters();
            DrawCameraImpl(position, camera, drawMode, false, nullGridParam, true);
        }

    
    
    internal enum FilterMode
        {
            Off = 0,
            ShowFiltered = 1,
            ShowRest = 2
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetCameraFilterMode (Camera camera, FilterMode mode) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  FilterMode GetCameraFilterMode (Camera camera) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void DrawCameraFade (Camera camera, float fade) ;

    public static void SetCamera(Camera camera)
        {
            if (Event.current.type == EventType.Repaint)
                Internal_SetupCamera(camera);
            else
                Internal_SetCurrentCamera(camera);
        }
    
    
    public static void SetCamera(Rect position, Camera camera)
        {
            Rect screenRect = GUIClip.Unclip(position);

            screenRect = EditorGUIUtility.PointsToPixels(screenRect);

            Rect cameraRect = new Rect(screenRect.xMin, Screen.height - screenRect.yMax, screenRect.width, screenRect.height);
            camera.pixelRect = cameraRect;

            Event evt = Event.current;

            if (evt.type == EventType.Repaint)
                Internal_SetupCamera(camera);
            else
                Internal_SetCurrentCamera(camera);
        }
    
    
    public static void BeginGUI()
        {
            if (Camera.current && Event.current.type == EventType.Repaint)
            {
                GUIClip.Reapply();
            }
        }
    
    
    [System.Obsolete ("Please use BeginGUI() with GUILayout.BeginArea(position) / GUILayout.EndArea()")]
public static void BeginGUI(Rect position)
        {
            GUILayout.BeginArea(position);
        }
    
    
    public static void EndGUI()
        {
            Camera cam = Camera.current;
            if (cam && Event.current.type == EventType.Repaint)
                Internal_SetupCamera(cam);
        }
    
    
    internal static void ShowStaticLabelIfNeeded(Vector3 pos)
        {
            if (!Tools.s_Hidden && EditorApplication.isPlaying && GameObjectUtility.ContainsStatic(Selection.gameObjects))
            {
                ShowStaticLabel(pos);
            }
        }
    
    
    internal static void ShowStaticLabel(Vector3 pos)
        {
            Handles.color = Color.white;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            GUIStyle style = "SC ViewAxisLabel";
            style.alignment = TextAnchor.MiddleLeft;
            style.fixedWidth = 0;
            Handles.BeginGUI();
            Rect rect = HandleUtility.WorldPointToSizedRect(pos, EditorGUIUtility.TempContent("Static"), style);
            rect.x += 10;
            rect.y += 10;
            GUI.Label(rect, EditorGUIUtility.TempContent("Static"), style);
            Handles.EndGUI();
        }
    
    
    private static Vector3[] Internal_MakeBezierPoints (Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, int division) {
        return INTERNAL_CALL_Internal_MakeBezierPoints ( ref startPosition, ref endPosition, ref startTangent, ref endTangent, division );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Vector3[] INTERNAL_CALL_Internal_MakeBezierPoints (ref Vector3 startPosition, ref Vector3 endPosition, ref Vector3 startTangent, ref Vector3 endTangent, int division);
    public static Vector3[] MakeBezierPoints(Vector3 startPosition, Vector3 endPosition, Vector3 startTangent, Vector3 endTangent, int division)
        {
            if (division < 1)
                throw new ArgumentOutOfRangeException("division", "Must be greater than zero");
            return Internal_MakeBezierPoints(startPosition, endPosition, startTangent, endTangent, division);
        }
    
    
}

}
