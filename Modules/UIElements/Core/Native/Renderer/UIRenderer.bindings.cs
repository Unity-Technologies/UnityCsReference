// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A renderer Component that should be added next to a UIDocument Component to allow
    /// world-space rendering. This Component is added automatically by the UIDocument when
    /// the PanelSettings asset is configured in world-space.
    /// </summary>
    [NativeHeader("Modules/UIElements/Core/Native/Renderer/UIRenderer.h")]
    [NativeClass("UIRenderer", PersistentTypeId = 0x731E9095)]
    public sealed class UIRenderer : Renderer
    {
        internal UIRenderer(global::UnityEngine.EntityId id) : base(id) {}
        public UIRenderer() {}
        internal volatile List<CommandList>[] commandLists;

        internal extern void AddDrawCallData(int safeFrameIndex, Material mat, uint textureSlotCount, uint forceRenderType, IntPtr serializedCommandsPtr, int commandCount, CommandListState state);
        internal extern void ResetDrawCallData(int safeFrameIndex);
        internal extern void ResetAllDrawCallData();
        internal extern int GetDrawCallDataCount();
    }
}
