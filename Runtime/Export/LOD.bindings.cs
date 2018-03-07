// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public enum LODFadeMode
    {
        None = 0,
        CrossFade = 1,
        SpeedTree = 2
    }

    [UsedByNativeCode]
    public struct LOD
    {
        // Construct a LOD
        public LOD(float screenRelativeTransitionHeight, Renderer[] renderers)
        {
            this.screenRelativeTransitionHeight = screenRelativeTransitionHeight;
            this.fadeTransitionWidth = 0;
            this.renderers = renderers;
        }

        // The screen relative height to use for the transition [0-1]
        public float screenRelativeTransitionHeight;
        // Width of the transition (proportion to the current LOD's whole length).
        public float fadeTransitionWidth;
        // List of renderers for this LOD level
        public Renderer[] renderers;
    }

    // LODGroup lets you group multiple Renderers into LOD levels.
    [NativeHeader("Runtime/Graphics/LOD/LODGroup.h")]
    [NativeHeader("Runtime/Graphics/LOD/LODGroupManager.h")]
    [NativeHeader("Runtime/Graphics/LOD/LODUtility.h")]
    [StaticAccessor("GetLODGroupManager()", StaticAccessorType.Dot)]
    public class LODGroup : Component
    {
        // The local reference point against which the LOD distance is calculated.
        extern public Vector3 localReferencePoint { get; set; }

        // The size of LOD object in local space
        extern public float size { get; set; }

        // The number of LOD levels
        extern public int lodCount  {[NativeMethod("GetLODCount")] get; }

        // The fade mode
        extern public LODFadeMode fadeMode  { get; set; }

        // Is cross-fading animated?
        extern public bool animateCrossFading  { get; set; }

        // Enable / Disable the LODGroup - Disabling will turn off all renderers.
        extern public bool enabled  { get; set; }

        // Recalculate the bounding region for the LODGroup (Relatively slow, do not call often)
        [FreeFunction("UpdateLODGroupBoundingBox", HasExplicitThis = true)]
        extern public void RecalculateBounds();

        [FreeFunction("GetLODs_Binding", HasExplicitThis = true)]
        extern public LOD[] GetLODs();

        [Obsolete("Use SetLODs instead.")]
        public void SetLODS(LOD[] lods) { SetLODs(lods); }

        // Set the LODs for the LOD group. This will remove any existing LODs configured on the LODGroup
        [FreeFunction("SetLODs_Binding", HasExplicitThis = true)]
        extern public void SetLODs(LOD[] lods);

        // Force a LOD level on this LOD group
        //
        // @param index The LOD level to use. Passing index < 0 will return to standard LOD processing
        [FreeFunction("ForceLODLevel", HasExplicitThis = true)]
        extern public void ForceLOD(int index);

        [StaticAccessor("GetLODGroupManager()")]
        extern public static float crossFadeAnimationDuration { get; set; }
    }
}
