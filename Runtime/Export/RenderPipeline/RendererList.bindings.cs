// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Graphics/ScriptableRenderLoop/RendererList.h")]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom("UnityEngine.Rendering.RendererUtils")]
    public struct RendererList
    {
        internal UIntPtr context;
        internal UInt32 index;
        internal UInt32 frame;
        internal UInt32 type;

        extern public bool isValid { get; }

        public static readonly RendererList nullRendererList = new RendererList(UIntPtr.Zero, UInt32.MaxValue);

        internal RendererList(UIntPtr ctx, UInt32 indx)
        {
            context = ctx;
            index = indx;
            frame = 0;
            type = 0;
        }
    }

    [MovedFrom("UnityEngine.Rendering.RendererUtils")]
    public enum RendererListStatus
    {
        kRendererListInvalid = -2,
        kRendererListProcessing = -1,
        kRendererListEmpty = 0,
        kRendererListPopulated = 1,
    };

    public struct RendererListParams : IEquatable<RendererListParams>
    {
        public static readonly RendererListParams Invalid = new RendererListParams();

        public CullingResults cullingResults;
        public DrawingSettings drawSettings;
        public FilteringSettings filteringSettings;
        public ShaderTagId tagName;
        public bool isPassTagName;
        public NativeArray<ShaderTagId>? tagValues;
        public NativeArray<RenderStateBlock>? stateBlocks;

        public RendererListParams(CullingResults cullingResults, DrawingSettings drawSettings, FilteringSettings filteringSettings)
        {
            this.cullingResults = cullingResults;
            this.drawSettings = drawSettings;
            this.filteringSettings = filteringSettings;
            tagName = ShaderTagId.none;
            isPassTagName = false;
            tagValues = null;
            stateBlocks = null;
        }

        internal int numStateBlocks
        {
            get
            {
                if (tagValues != null)
                    return tagValues.Value.Length;
                return 0;
            }
        }
        internal unsafe IntPtr stateBlocksPtr
        {
            get
            {
                if (stateBlocks == null) return IntPtr.Zero;
                return (IntPtr)stateBlocks.Value.GetUnsafeReadOnlyPtr();
            }
        }
        internal unsafe IntPtr tagsValuePtr
        {
            get
            {
                if (tagValues == null) return IntPtr.Zero;
                return (IntPtr)tagValues.Value.GetUnsafeReadOnlyPtr();
            }
        }
        internal void Dispose()
        {
            if (stateBlocks != null)
            {
                stateBlocks.Value.Dispose();
                stateBlocks = null;
            }
            if (tagValues != null)
            {
                tagValues.Value.Dispose();
                tagValues = null;
            }
        }

        internal void Validate()
        {
            cullingResults.Validate();
            if (tagValues.HasValue && stateBlocks.HasValue)
            {
                if (tagValues.Value.Length != stateBlocks.Value.Length)
                    throw new ArgumentException($"Arrays {nameof(tagValues)} and {nameof(stateBlocks)} should have same length, but {nameof(tagValues)} had length {tagValues.Value.Length} while {nameof(stateBlocks)} had length {stateBlocks.Value.Length}.");
            }
            else if ( (tagValues.HasValue && !stateBlocks.HasValue) || (!tagValues.HasValue && stateBlocks.HasValue))
            {
                throw new ArgumentException($"Arrays {nameof(tagValues)} and {nameof(stateBlocks)} should have same length, but one of them is null ({nameof(tagValues)} : {tagValues.HasValue}, {nameof(stateBlocks)} : {stateBlocks.HasValue}).");
            }
        }

        public bool Equals(RendererListParams other)
        {
            return cullingResults == other.cullingResults &&
                   drawSettings == other.drawSettings &&
                   filteringSettings == other.filteringSettings &&
                   tagName == other.tagName &&
                   isPassTagName == other.isPassTagName &&
                   tagValues == other.tagValues &&
                   stateBlocks == other.stateBlocks;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RendererListParams && Equals((RendererListParams)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = cullingResults.GetHashCode();
                hashCode = (hashCode * 397) ^ drawSettings.GetHashCode();
                hashCode = (hashCode * 397) ^ filteringSettings.GetHashCode();
                hashCode = (hashCode * 397) ^ tagName.GetHashCode();
                hashCode = (hashCode * 397) ^ (isPassTagName ? 0 : 1);
                hashCode = (hashCode * 397) ^ tagValues.GetHashCode();
                hashCode = (hashCode * 397) ^ stateBlocks.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(RendererListParams left, RendererListParams right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RendererListParams left, RendererListParams right)
        {
            return !left.Equals(right);
        }
    }
}


namespace UnityEngine.Rendering.RendererUtils
{
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
        /// Override shader for this renderer list.
        /// </summary>
        public Shader overrideShader;
        /// <summary>
        /// Override material for this renderer list.
        /// </summary>
        public Material overrideMaterial;
        /// <summary>
        /// Exclude object with motion from this renderer list.
        /// </summary>
        public bool excludeObjectMotionVectors;
        /// <summary>
        /// The layer mask to use for filtering this RendererList.
        /// </summary>
        public int layerMask;
        /// <summary>
        /// The rendering layer mask to use for filtering this RendererList.
        /// </summary>
        public uint renderingLayerMask;
        /// <summary>
        /// Pass index for the override material.
        /// </summary>
        public int overrideMaterialPassIndex;
        /// <summary>
        /// Pass index for the override shader.
        /// </summary>
        public int overrideShaderPassIndex;

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
            this.renderingLayerMask = uint.MaxValue;
            this.overrideMaterialPassIndex = 0;
            this.overrideShaderPassIndex = 0;
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
            this.renderingLayerMask = uint.MaxValue;
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

        static readonly ShaderTagId s_EmptyName = new ShaderTagId("");
        public static RendererListParams ConvertToParameters(in RendererListDesc desc)
        {
            // At this point the RendererList is invalid and will be caught when using it.
            // It's fine because to simplify setup code you might not always have a valid desc. The important part is to catch it if used.
            if (!desc.IsValid())
                return RendererListParams.Invalid;

            RendererListParams rlParams = new RendererListParams();

            var sortingSettings = new SortingSettings(desc.camera)
            {
                criteria = desc.sortingCriteria
            };

            var drawSettings = new DrawingSettings(s_EmptyName, sortingSettings)
            {
                perObjectData = desc.rendererConfiguration,
                //enableDynamicBatching
                //enableInstancing
                //mainLightIndex
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

            if (desc.overrideShader != null)
            {
                drawSettings.overrideShader = desc.overrideShader;
                drawSettings.overrideShaderPassIndex = desc.overrideShaderPassIndex;
            }

            if (desc.overrideMaterial != null)
            {
                drawSettings.overrideMaterial = desc.overrideMaterial;
                drawSettings.overrideMaterialPassIndex = desc.overrideMaterialPassIndex;
            }

            //drawSettings.fallbackMaterial

            var filterSettings = new FilteringSettings(desc.renderQueueRange, desc.layerMask, desc.renderingLayerMask)
            {
                excludeMotionVectorObjects = desc.excludeObjectMotionVectors,
               // sortingLayerRange
            };

            rlParams.cullingResults = desc.cullingResult;
            rlParams.drawSettings = drawSettings;
            rlParams.filteringSettings = filterSettings;

            rlParams.tagName = ShaderTagId.none;
            rlParams.isPassTagName = false;

            if (desc.stateBlock != null && desc.stateBlock.HasValue)
            {
                rlParams.stateBlocks = new NativeArray<RenderStateBlock>(1, Allocator.Temp) {
                    [0] = desc.stateBlock.Value
                };
                rlParams.tagValues = new NativeArray<ShaderTagId>(1, Allocator.Temp) {
                    [0] = ShaderTagId.none
                };
            }

            return rlParams;
        }
    }

}
