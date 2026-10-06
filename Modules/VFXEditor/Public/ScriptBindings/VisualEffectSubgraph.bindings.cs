// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [UsedByNativeCode]
    [NativeHeader("Modules/VFXEditor/Public/VisualEffectSubgraph.h")]
    [NativeHeader("VFXScriptingClasses.h")]
    [NativeClass("VisualEffectSubgraph", PersistentTypeId = 0x3B4A7520)]
    internal abstract class VisualEffectSubgraph : VisualEffectObject
    {
        protected VisualEffectSubgraph() {}
        protected internal VisualEffectSubgraph(global::UnityEngine.EntityId id) : base(id) {}
    }

    [UsedByNativeCode]
    [NativeHeader("Modules/VFXEditor/Public/VisualEffectSubgraph.h")]
    [NativeHeader("VFXScriptingClasses.h")]
    [NativeClass("VisualEffectSubgraphOperator", PersistentTypeId = 0x3B4A752B)]
    internal class VisualEffectSubgraphOperator : VisualEffectSubgraph
    {
        internal VisualEffectSubgraphOperator(global::UnityEngine.EntityId id) : base(id) {}
        public const string Extension = ".vfxoperator";

        public VisualEffectSubgraphOperator()
        {
            SetEntityIdFromConstructor(CreateVisualEffectSubgraph());
        }

        private static extern EntityId CreateVisualEffectSubgraph();
    }

    [UsedByNativeCode]
    [NativeHeader("Modules/VFXEditor/Public/VisualEffectSubgraph.h")]
    [NativeHeader("VFXScriptingClasses.h")]
    [NativeClass("VisualEffectSubgraphBlock", PersistentTypeId = 0x3B4A752C)]
    internal class VisualEffectSubgraphBlock : VisualEffectSubgraph
    {
        internal VisualEffectSubgraphBlock(global::UnityEngine.EntityId id) : base(id) {}
        public const string Extension = ".vfxblock";
        public VisualEffectSubgraphBlock()
        {
            SetEntityIdFromConstructor(CreateVisualEffectSubgraph());
        }

        private static extern EntityId CreateVisualEffectSubgraph();
    }
}
