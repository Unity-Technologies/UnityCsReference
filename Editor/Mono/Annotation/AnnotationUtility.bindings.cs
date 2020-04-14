// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeType(CodegenOptions.Custom, "AnnotationBindings")]
    internal partial struct Annotation
    {
        public int iconEnabled;
        public int gizmoEnabled;
        public int flags;
        public int classID;
        public string scriptClass;
    }

    [NativeHeader("Editor/Mono/Annotation/AnnotationUtility.bindings.h")]
    [NativeHeader("Editor/Src/AnnotationManager.h")]
    internal sealed partial class AnnotationUtility
    {
        extern internal static  Annotation[] GetAnnotations();

        extern internal static  Annotation[] GetRecentlyChangedAnnotations();

        [StaticAccessor("GetAnnotationManager()", StaticAccessorType.Dot)]
        extern internal static  string GetNameOfCurrentSetup();

        extern internal static  void SetGizmoEnabled(int classID, string scriptClass, int gizmoEnabled, bool addToMostRecentChanged);

        extern internal static  void SetIconEnabled(int classID, string scriptClass, int iconEnabled);

        [StaticAccessor("GizmoManager::Get()", StaticAccessorType.Dot)]
        extern internal static  int SetGizmosDirty();

        [StaticAccessor("GetAnnotationManager()", StaticAccessorType.Dot)]
        extern internal static  string[] GetPresetList();

        [StaticAccessor("GetAnnotationManager()", StaticAccessorType.Dot)]
        extern internal static  void LoadPreset(string presetName);

        [StaticAccessor("GetAnnotationManager()", StaticAccessorType.Dot)]
        extern internal static  void SavePreset(string presetName);

        [StaticAccessor("GetAnnotationManager()", StaticAccessorType.Dot)]
        extern internal static  void DeletePreset(string presetName);

        [StaticAccessor("GetAnnotationManager()", StaticAccessorType.Dot)]
        [NativeMethod("ResetPresetsToFactorySettings")]
        extern internal static  void ResetToFactorySettings();

        [StaticAccessor("GetAnnotationManager()", StaticAccessorType.Dot)]
        [NativeMethod("3dGizmosEnabled")]
        internal extern static bool use3dGizmos { get; set; }

        [StaticAccessor("GetAnnotationManager()", StaticAccessorType.Dot)]
        // Thomas Tu: 2019-06-20. Will be marked as Obsolete.
        // We need to deal with code dependency in packages first.
        internal extern static bool showGrid { get; set; }

        [StaticAccessor("GetAnnotationManager()", StaticAccessorType.Dot)]
        internal extern static bool showSelectionOutline { get; set; }

        [StaticAccessor("GetAnnotationManager()", StaticAccessorType.Dot)]
        internal extern static bool showSelectionWire { get; set; }

        [StaticAccessor("GetAnnotationManager()", StaticAccessorType.Dot)]
        internal extern static float iconSize { get; set; }
    }
}
