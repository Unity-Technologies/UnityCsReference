// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements
{
    using UIR;

    /// <summary>
    /// A renderer Component that should be added next to a UIDocument Component to allow
    /// world-space rendering. This Component is added automatically by the UIDocument when
    /// the PanelSettings asset is configured in world-space.
    /// </summary>
    [NativeHeader("Modules/UIElements/Core/Native/Renderer/UIRenderer.h")]
    public sealed class UIRenderer : Renderer
    {
        internal volatile List<CommandList>[] commandLists;
        internal volatile bool skipRendering;

        internal extern void AddDrawCallData(int safeFrameIndex, int cmdListIndex, Material mat, uint textureSlotCount, uint forceRenderType);
        internal extern void ResetDrawCallData();
        internal extern int GetDrawCallDataCount();

        [RequiredByNativeCode]
        static void OnRenderNodeExecute(UIRenderer renderer, int safeFrameIndex, int cmdListIndex)
        {
            if (renderer.skipRendering)
                return;

            var commandLists = renderer.commandLists;
            var cmdList = commandLists != null ? commandLists[safeFrameIndex] : null;
            if (cmdList != null && cmdListIndex < cmdList.Count)
                cmdList[cmdListIndex]?.Execute();
        }
    }
}
