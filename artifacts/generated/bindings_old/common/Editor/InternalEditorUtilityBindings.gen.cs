// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using Object = UnityEngine.Object;

using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor.Scripting.ScriptCompilation;


namespace UnityEditorInternal
{
public sealed partial class InternalEditorUtility
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  DragAndDropVisualMode ProjectWindowDrag (HierarchyProperty property, bool perform) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  DragAndDropVisualMode HierarchyWindowDrag (HierarchyProperty property, bool perform, HierarchyDropMode dropMode) ;

    public static void SetRectTransformTemporaryRect (RectTransform rectTransform, Rect rect) {
        INTERNAL_CALL_SetRectTransformTemporaryRect ( rectTransform, ref rect );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetRectTransformTemporaryRect (RectTransform rectTransform, ref Rect rect);
    public static LayerMask ConcatenatedLayersMaskToLayerMask (int concatenatedLayersMask) {
        LayerMask result;
        INTERNAL_CALL_ConcatenatedLayersMaskToLayerMask ( concatenatedLayersMask, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ConcatenatedLayersMaskToLayerMask (int concatenatedLayersMask, out LayerMask value);
    public static int LayerMaskToConcatenatedLayersMask (LayerMask mask) {
        return INTERNAL_CALL_LayerMaskToConcatenatedLayersMask ( ref mask );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_LayerMaskToConcatenatedLayersMask (ref LayerMask mask);
    public static Vector4 GetSpriteOuterUV (Sprite sprite, bool getAtlasData) {
        Vector4 result;
        INTERNAL_CALL_GetSpriteOuterUV ( sprite, getAtlasData, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetSpriteOuterUV (Sprite sprite, bool getAtlasData, out Vector4 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object GetObjectFromInstanceID (int instanceID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Type GetTypeWithoutLoadingObject (int instanceID) ;

    public static Color[] ReadScreenPixel (Vector2 pixelPos, int sizex, int sizey) {
        return INTERNAL_CALL_ReadScreenPixel ( ref pixelPos, sizex, sizey );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Color[] INTERNAL_CALL_ReadScreenPixel (ref Vector2 pixelPos, int sizex, int sizey);
    public static Bounds TransformBounds (Bounds b, Transform t) {
        Bounds result;
        INTERNAL_CALL_TransformBounds ( ref b, t, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_TransformBounds (ref Bounds b, Transform t, out Bounds value);
    public static Bounds CalculateSelectionBounds (bool usePivotOnlyForParticles, bool onlyUseActiveSelection) {
        Bounds result;
        INTERNAL_CALL_CalculateSelectionBounds ( usePivotOnlyForParticles, onlyUseActiveSelection, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CalculateSelectionBounds (bool usePivotOnlyForParticles, bool onlyUseActiveSelection, out Bounds value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void PrepareDragAndDropTestingInternal (GUIView guiView) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string[] GetCompilationDefines (EditorScriptCompilationOptions options, BuildTargetGroup targetGroup, BuildTarget target, ApiCompatibilityLevel apiCompatibilityLevel) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  PrecompiledAssembly[] GetUnityAssemblies (bool buildingForEditor, BuildTargetGroup buildTargetGroup, BuildTarget target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  PrecompiledAssembly[] GetPrecompiledAssemblies (bool buildingForEditor, BuildTargetGroup buildTargetGroup, BuildTarget target) ;

}

}
