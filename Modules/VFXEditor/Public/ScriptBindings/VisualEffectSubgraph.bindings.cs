// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    [UsedByNativeCode]
    [NativeHeader("Modules/VFXEditor/Public/VisualEffectSubgraph.h")]
    [NativeHeader("VFXScriptingClasses.h")]
    internal abstract class VisualEffectSubgraph : VisualEffectObject
    {
    }

    [UsedByNativeCode]
    [NativeHeader("Modules/VFXEditor/Public/VisualEffectSubgraph.h")]
    [NativeHeader("VFXScriptingClasses.h")]
    internal class VisualEffectSubgraphOperator : VisualEffectSubgraph
    {
        public const string Extension = ".vfxoperator";

        public VisualEffectSubgraphOperator()
        {
            CreateVisualEffectSubgraph(this);
        }

        private static extern void CreateVisualEffectSubgraph([Writable] VisualEffectSubgraphOperator subGraph);
    }

    [UsedByNativeCode]
    [NativeHeader("Modules/VFXEditor/Public/VisualEffectSubgraph.h")]
    [NativeHeader("VFXScriptingClasses.h")]
    internal class VisualEffectSubgraphBlock : VisualEffectSubgraph
    {
        public const string Extension = ".vfxblock";
        public VisualEffectSubgraphBlock()
        {
            CreateVisualEffectSubgraph(this);
        }

        private static extern void CreateVisualEffectSubgraph([Writable] VisualEffectSubgraphBlock subGraph);
    }
}
