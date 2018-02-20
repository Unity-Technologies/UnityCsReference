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
    [RequiredByNativeCode]
    internal struct IMGUIDrawInstruction
    {
        public Rect         rect;
        public Rect         visibleRect;
        public GUIStyle     usedGUIStyle;
        public GUIContent   usedGUIContent;
        public StackFrame[] stackframes;

        public void Reset()
        {
            rect = new Rect();
            visibleRect = new Rect();
            usedGUIStyle = GUIStyle.none;
            usedGUIContent = GUIContent.none;
        }
    }
    [RequiredByNativeCode]
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

    [RequiredByNativeCode]
    internal struct IMGUILayoutInstruction
    {
        public int level;
        public Rect unclippedRect;

        public int marginLeft;
        public int marginRight;
        public int marginTop;
        public int marginBottom;

        public GUIStyle style;


        public StackFrame[] stack;

        public int isGroup;
        public int isVertical;
    }

    [RequiredByNativeCode]
    internal struct IMGUINamedControlInstruction
    {
        public string name;
        public Rect rect;
        public int id;
    }

    [RequiredByNativeCode]
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

    [RequiredByNativeCode]
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
        [NativeThrows]
        static internal extern void GetViews(List<GUIView> views);

        static internal extern void DebugWindow([Unmarshalled] GUIView view);

        [FreeFunction("GetGUIDebuggerManager().StopDebuggingAll")]
        static internal extern void StopDebugging();

        private static GUIContent CreateGUIContent(string text, Texture image, string tooltip)
        {
            return new GUIContent(text, image, tooltip);
        }

        internal static extern void GetDrawInstructions(List<IMGUIDrawInstruction> drawInstructions);

        internal static extern void GetClipInstructions(List<IMGUIClipInstruction> clipInstructions);

        internal static extern void GetNamedControlInstructions(List<IMGUINamedControlInstruction> namedControlInstructions);

        internal static extern void GetPropertyInstructions(List<IMGUIPropertyInstruction> namedControlInstructions);

        internal static extern void GetLayoutInstructions(List<IMGUILayoutInstruction> layoutInstructions);

        internal static extern void GetUnifiedInstructions(List<IMGUIInstruction> layoutInstructions);

        [FreeFunction("GetGUIDebuggerManager().ClearInstructions")]
        internal static extern void ClearInstructions();
    }
} //namespace
