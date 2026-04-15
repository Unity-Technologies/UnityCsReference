// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System;
namespace UnityEditor
{
    // This needs to match the native StackFrame struct in GUIViewDebuggerHelper.bindings.h
    [NativeHeader("Editor/Mono/GUIDebugger/GUIViewDebuggerHelper.bindings.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct StackFrame
    {
        public uint   lineNumber;
        public string sourceFile;
        public string methodName;
        public string signature;
        public string moduleName;
    }

    [NativeHeader("Editor/Mono/GUIDebugger/GUIViewDebuggerHelper.bindings.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMGUIDrawInstruction
    {
        public Rect         rect;
        public Rect         visibleRect;
        [UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public GUIStyle     usedGUIStyle;
        [UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public GUIContent   usedGUIContent;
        public string       label;
        public StackFrame[] stackframes;

        public void Reset()
        {
            rect = new Rect();
            visibleRect = new Rect();
            usedGUIStyle = GUIStyle.none;
            usedGUIContent = GUIContent.none;
        }
    }

    [NativeHeader("Editor/Mono/GUIDebugger/GUIViewDebuggerHelper.bindings.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMGUIClipInstruction
    {
        public Rect screenRect;
        public Rect unclippedScreenRect;
        public Vector2 scrollOffset;
        public Vector2 renderOffset;
        public bool resetOffset;
        public int level;

        public StackFrame[] pushStacktrace;
        public StackFrame[] popStacktrace;
    }

    [NativeHeader("Editor/Mono/GUIDebugger/GUIViewDebuggerHelper.bindings.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMGUILayoutInstruction
    {
        public int level;
        public Rect unclippedRect;

        public int marginLeft;
        public int marginRight;
        public int marginTop;
        public int marginBottom;

        [UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public GUIStyle style;


        public StackFrame[] stack;

        public int isGroup;
        public int isVertical;
    }

    [NativeHeader("Editor/Mono/GUIDebugger/GUIViewDebuggerHelper.bindings.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMGUINamedControlInstruction
    {
        public string name;
        public Rect rect;
        public int id;
    }

    [NativeHeader("Editor/Mono/GUIDebugger/GUIViewDebuggerHelper.bindings.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMGUIPropertyInstruction
    {
        public string targetTypeName;
        public string path;
        public Rect rect;
        public StackFrame[] beginStacktrace;
        public StackFrame[] endStacktrace;
    }

    //unified
    internal enum InstructionType
    {
        kStyleDraw = 1,
        kClipPush = 2,
        kClipPop = 3,
        kLayoutBeginGroup = 4,
        kLayoutEndGroup = 5,
        kLayoutEntry = 6,
        kPropertyBegin = 7,
        kPropertyEnd = 8,
        kLayoutNamedControl = 9
    }

    //TODO: we are symbolicating the stacktraces of all instructions,
    //and even worse, we do it everyframe.
    //We should load the stacktrace info lazily.

    [NativeHeader("Editor/Mono/GUIDebugger/GUIViewDebuggerHelper.bindings.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMGUIInstruction
    {
        public InstructionType type;
        public int level;
        public Rect unclippedRect;
        public StackFrame[] stack;

        public int typeInstructionIndex;
        public bool enabled;
    }

    [NativeHeader("Editor/Mono/GUIDebugger/GUIViewDebuggerHelper.bindings.h")]
    [UsedByNativeCode]
    internal static partial class GUIViewDebuggerHelper
    {
        static internal extern void GetViews([Out,NotNull] List<GUIView> views);

        static internal extern void DebugWindow(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(GUIView.NativeHandleMarshaller))]
            GUIView view);

        [FreeFunction("GetGUIDebuggerManager().StopDebuggingAll")]
        static internal extern void StopDebugging();

        [RequiredByNativeCode]
        private static GUIContent CreateGUIContent(string text, Texture image, string tooltip)
        {
            return new GUIContent(text, image, tooltip);
        }

        internal static extern void GetDrawInstructions([Out] List<IMGUIDrawInstruction> drawInstructions, bool includeStackTraces = true);

        internal static extern void GetClipInstructions([Out] List<IMGUIClipInstruction> clipInstructions, bool includeStackTraces = true);

        internal static extern void GetNamedControlInstructions([Out] List<IMGUINamedControlInstruction> namedControlInstructions);

        internal static extern void GetPropertyInstructions([Out] List<IMGUIPropertyInstruction> propertyInstructions, bool includeStackTraces = true);

        internal static extern void GetLayoutInstructions([Out] List<IMGUILayoutInstruction> layoutInstructions, bool includeStackTraces = true);

        internal static extern void GetUnifiedInstructions([Out] List<IMGUIInstruction> instructions, bool includeStackTraces = true);

        [FreeFunction("GetGUIDebuggerManager().ClearInstructions")]
        internal static extern void ClearInstructions();
    }
} //namespace
