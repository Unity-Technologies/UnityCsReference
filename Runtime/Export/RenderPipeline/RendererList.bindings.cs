// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using Unity.Collections;

namespace UnityEngine.Rendering.RendererUtils
{
    public enum RendererListStatus
    {
        kRendererListInvalid = -2,
        kRendererListProcessing = -1,
        kRendererListEmpty = 0,
        kRendererListPopulated = 1,
    };

    [NativeHeader("Runtime/Graphics/ScriptableRenderLoop/RendererList.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct RendererList
    {
        internal UIntPtr context;
        internal UInt32  index;
        internal UInt32  frame;

        extern public bool isValid { get; }

        public static readonly RendererList nullRendererList = new RendererList(UIntPtr.Zero, UInt32.MaxValue);

        internal RendererList(UIntPtr ctx, UInt32 indx)
        {
            context = ctx;
            index = indx;
            frame = 0;
        }
    }

    /// <summary>
    /// Renderer list creation descriptor.
    /// </summary>
    public struct RendererListDesc
    {
        /// <summary>
        /// SortingCriteria for this renderer list.
        /// </summary>
        public SortingCriteria sortingCriteria;
        /// <summary>
        /// PerObjectData configuration for this renderer list.
        /// </summary>
        public PerObjectData rendererConfiguration;
        /// <summary>
        /// RenderQueueRange of this renderer list.
        /// </summary>
        public RenderQueueRange renderQueueRange;
        /// <summary>
        /// Optional RenderStateBlock for this renderer list.
        /// </summary>
        public RenderStateBlock? stateBlock;
        /// <summary>
        /// Override material for this renderer list.
        /// </summary>
        public Material overrideMaterial;
        /// <summary>
        /// Exclude object with motion from this renderer list.
        /// </summary>
        public bool excludeObjectMotionVectors;
        /// <summary>
        /// Rendering layer mask used for filtering this renderer list.
        /// </summary>
        public int layerMask;
        /// <summary>
        /// Pass index for the override material.
        /// </summary>
        public int overrideMaterialPassIndex;

        // Mandatory parameters passed through constructors
        internal CullingResults cullingResult { get; private set; }
        internal Camera camera { get; set; }
        internal ShaderTagId passName { get; private set; }
        internal ShaderTagId[] passNames { get; private set; }

        /// <summary>
        /// RendererListDesc constructor
        /// </summary>
        /// <param name="passName">Pass name used for this renderer list.</param>
        /// <param name="cullingResult">Culling result used to create the renderer list.</param>
        /// <param name="camera">Camera used to determine sorting parameters.</param>
        public RendererListDesc(ShaderTagId passName, CullingResults cullingResult, Camera camera)
            : this()
        {
            this.passName = passName;
            this.passNames = null;
            this.cullingResult = cullingResult;
            this.camera = camera;
            this.layerMask = -1;
            this.overrideMaterialPassIndex = 0;
        }

        /// <summary>
        /// RendererListDesc constructor
        /// </summary>
        /// <param name="passNames">List of pass names used for this renderer list.</param>
        /// <param name="cullingResult">Culling result used to create the renderer list.</param>
        /// <param name="camera">Camera used to determine sorting parameters.</param>
        public RendererListDesc(ShaderTagId[] passNames, CullingResults cullingResult, Camera camera)
            : this()
        {
            this.passNames = passNames;
            this.passName = ShaderTagId.none;
            this.cullingResult = cullingResult;
            this.camera = camera;
            this.layerMask = -1;
            this.overrideMaterialPassIndex = 0;
        }

        /// <summary>
        /// Returns true if the descriptor is valid.
        /// </summary>
        /// <returns>True if the descriptor is valid.</returns>
        public bool IsValid()
        {
            if (camera == null || (passName == ShaderTagId.none && (passNames == null || passNames.Length == 0)))
                return false;

            return true;
        }
    }

    internal struct RendererListParams
    {
        static readonly ShaderTagId s_EmptyName = new ShaderTagId("");

        /// <summary>
        /// Default null renderer list.
        /// </summary>
        public static readonly RendererListParams nullRendererList = new RendererListParams();

        /// <summary>
        /// True if the renderer list is valid.
        /// </summary>
        public bool isValid { get; private set; }
        /// <summary>
        /// CullingResults associated with the renderer list.
        /// </summary>
        internal CullingResults cullingResult;
        /// <summary>
        /// DrawingSettings associated with the renderer list.
        /// </summary>
        internal DrawingSettings drawSettings;
        /// <summary>
        /// FilteringSettings associated with the renderer list.
        /// </summary>
        internal FilteringSettings filteringSettings;
        /// <summary>
        /// Optional RenderStateBlock associated with the renderer list.
        /// </summary>
        internal RenderStateBlock? stateBlock;

        /// <summary>
        /// Creates a new renderer list.
        /// </summary>
        /// <param name="desc">Parameters for renderer list creation.</param>
        /// <returns>A new renderer list.</returns>
        internal static RendererListParams Create(in RendererListDesc desc)
        {
            RendererListParams newRenderList = new RendererListParams();

            // At this point the RendererList is invalid and will be caught when using it.
            // It's fine because to simplify setup code you might not always have a valid desc. The important part is to catch it if used.
            if (!desc.IsValid())
                return newRenderList;

            var sortingSettings = new SortingSettings(desc.camera)
            {
                criteria = desc.sortingCriteria
            };

            var drawSettings = new DrawingSettings(s_EmptyName, sortingSettings)
            {
                perObjectData = desc.rendererConfiguration
            };

            if (desc.passName != ShaderTagId.none)
            {
                Debug.Assert(desc.passNames == null);
                drawSettings.SetShaderPassName(0, desc.passName);
            }
            else
            {
                for (int i = 0; i < desc.passNames.Length; ++i)
                {
                    drawSettings.SetShaderPassName(i, desc.passNames[i]);
                }
            }

            if (desc.overrideMaterial != null)
            {
                drawSettings.overrideMaterial = desc.overrideMaterial;
                drawSettings.overrideMaterialPassIndex = desc.overrideMaterialPassIndex;
            }

            var filterSettings = new FilteringSettings(desc.renderQueueRange, desc.layerMask)
            {
                excludeMotionVectorObjects = desc.excludeObjectMotionVectors
            };

            newRenderList.isValid = true;
            newRenderList.cullingResult = desc.cullingResult;
            newRenderList.drawSettings = drawSettings;
            newRenderList.filteringSettings = filterSettings;
            newRenderList.stateBlock = desc.stateBlock;

            return newRenderList;
        }
    }
}
