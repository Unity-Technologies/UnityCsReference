// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.VFX;
using Unity.Scripting.LifecycleManagement;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX
{
    [NativeHeader("Modules/VFX/Public/VFXHelpers.h")]
    internal static class VFXExpressionHelper
    {
        [FreeFunction("VFX::GetTypeOfOperation", IsThreadSafe = true)]
        extern public static VFXValueType GetTypeOfOperation(VFXExpressionOperation op, int data0, int data1, int data2, int data3);

        [FreeFunction("VFX::GetSizeOfType", IsThreadSafe = true)]
        extern public static int GetSizeOfType(VFXValueType type);

        [FreeFunction("VFX::GetTextureDimension", IsThreadSafe = true)]
        extern public static TextureDimension GetTextureDimension(VFXValueType type);
    }

    internal struct VFXRendererSettings
    {
        public MotionVectorGenerationMode motionVectorGenerationMode;
        public ShadowCastingMode shadowCastingMode;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct VFXMapping
    {
        [NativeName("nameId")]
        public string name;
        public int index;

        public VFXMapping(string name, int index)
        {
            this.name = name;
            this.index = index;
        }
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct VFXMappingTemporary
    {
        public VFXMapping mapping;
        public uint pastFrameIndex;
        public bool perCameraBuffer;

        public VFXMappingTemporary(VFXMapping mapping, uint pastFrameIndex, bool perCameraBuffer)
        {
            this.mapping = mapping;
            this.pastFrameIndex = pastFrameIndex;
            this.perCameraBuffer = perCameraBuffer;
        }
    }

    internal struct VFXGPUBufferDesc
    {
        public VFXLayoutElementDesc[] layout;
        public uint capacity;
        public string debugName; // always there in editor
        public GraphicsBuffer.Target target;
        public uint size;
        public uint stride;
        public ComputeBufferMode mode;
    }

    internal struct VFXTemporaryGPUBufferDesc
    {
        public VFXGPUBufferDesc desc;
        public uint frameCount;
    }

    [UsedByNativeCode]
    internal sealed class VFXCPUBufferData : IDisposable
    {
        internal IntPtr m_Ptr;
        public VFXCPUBufferData()
        {
            m_Ptr = Internal_Create();
        }

        extern static internal IntPtr Internal_Create();

        private void Release()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        ~VFXCPUBufferData()
        {
            Release();
        }

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        [NativeMethod(IsThreadSafe = true)]
        extern static internal void Internal_Destroy(IntPtr ptr);

        extern public void PushUInt(uint v);
        extern public void PushInt(int v);
        extern public void PushFloat(float v);
        extern public void PushBool(bool v);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(VFXCPUBufferData data) => data.m_Ptr;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VFXCPUBufferDesc
    {
        public VFXLayoutElementDesc[] layout;
        public uint capacity;
        public string debugName;
        public uint stride;
        public VFXCPUBufferData initialData;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VFXShaderSourceDesc
    {
        public bool compute;
        public string name;
        public string source;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct VFXEditorTaskDesc
    {
        public VFXTaskType type;
        public VFXMapping[] buffers;
        public VFXMappingTemporary[] temporaryBuffers;
        public VFXMapping[] values;
        [NativeName("params")]
        public VFXMapping[] parameters;
        public UnityObject processor;
        public uint instanceSplitIndex;
        public int shaderSourceIndex;
        public EntityId modelId;
        public bool usesMaterialVariant;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct VFXInstanceSplitDesc
    {
        public uint[] values;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct VFXEditorSystemDesc
    {
        public VFXSystemType type;
        public VFXSystemFlag flags;
        public uint capacity;
        public uint layer;
        public string name;
        public VFXMapping[] buffers;
        public VFXMapping[] values;
        public VFXEditorTaskDesc[] tasks;
        public VFXInstanceSplitDesc[] instanceSplitDescs;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct VFXEventDesc
    {
        public string name;
        [NativeName("playSystems")]
        public uint[] startSystems;
        public uint[] stopSystems;
        public uint[] initSystems;
    }

    internal abstract class VFXExpressionValueContainerDesc
    {
        public uint expressionIndex = uint.MaxValue;
    }

    internal class VFXExpressionValueContainerDesc<T> : VFXExpressionValueContainerDesc
    {
        public T value = default(T);
    }

    internal class VFXExpressionObjectValueContainerDesc<T> : VFXExpressionValueContainerDesc
    {
        public EntityId entityId = EntityId.None;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct VFXExpressionDesc
    {
        public VFXExpressionOperation op;
        public fixed int data[4];

        public void SetData(int data0, int data1, int data2, int data3)
        {
            data[0] = data0;
            data[1] = data1;
            data[2] = data2;
            data[3] = data3;
        }
    }

    internal struct VFXLayoutOffset
    {
        public uint bucket;
        public uint structure;
        public uint element;
    }

    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct VFXLayoutElementDesc
    {
        public string name;
        public VFXValueType type;
        public VFXLayoutOffset offset;
    }

    internal struct VFXExposedMapping
    {
        public VFXMapping mapping;
        public VFXSpace space;
    }

    internal struct VFXExpressionSheet
    {
        public VFXExpressionDesc[] expressions;
        public VFXExpressionDesc[] expressionsPerSpawnEventAttribute;
        public VFXExpressionValueContainerDesc[] values;
        public VFXExposedMapping[] exposed;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct VFXExpressionValuesSheetInternal
    {
        public int[] intValues;
        public uint[] intValuesExpressions;
        public uint[] uintValues;
        public uint[] uintValuesExpressions;
        public float[] floatValues;
        public uint[] floatValuesExpressions;
        public Vector2[] vector2Values;
        public uint[] vector2ValuesExpressions;
        public Vector3[] vector3Values;
        public uint[] vector3ValuesExpressions;
        public Vector4[] vector4Values;
        public uint[] vector4ValuesExpressions;
        public Matrix4x4[] matrix4x4Values;
        public uint[] matrix4x4ValuesExpressions;
        public AnimationCurve[] animationCurveValues;
        public uint[] animationCurveValuesExpressions;
        public Gradient[] gradientValues;
        public uint[] gradientValuesExpressions;
        public EntityId[] textureValues;
        public uint[] textureValuesExpressions;
        public EntityId[] meshValues;
        public uint[] meshValuesExpressions;
        public EntityId[] skinnedMeshRendererValues;
        public uint[] skinnedMeshRendererValuesExpressions;
        public bool[] boolValues;
        public uint[] boolValuesExpressions;
    }

    [UsedByNativeCode]
    internal struct VFXExpressionSheetInternal
    {
        public VFXExpressionDesc[] expressions;
        public VFXExpressionDesc[] expressionsPerSpawnEventAttribute;
        public VFXExpressionValuesSheetInternal values;
        public VFXExposedMapping[] exposed;
    }


    /*Version history
    0 - before tracking of versions
    1 - gradient keys change to linear
    */

    [UsedByNativeCode]
    [NativeHeader("Modules/VFXEditor/Public/ScriptBindings/VisualEffectResourceBindings.h")]
    [NativeHeader("Modules/VFXEditor/Public/VisualEffectResource.h")]
    [NativeHeader("VFXScriptingClasses.h")]
    internal partial class VisualEffectResource : UnityObject
    {
        public VisualEffectResource()
        {
            CreateVisualEffectResource(this);
        }

        public const string Extension = ".vfx";
        extern private static void CreateVisualEffectResource([Writable] VisualEffectResource resource);

        //Must be kept in sync with C++
        public const int CurrentVersion = 1;

        //This intermediate struct is only used on U6 branch where marshalling of List isn't straighforward
        struct VFXExpressionValuesSheetInternalListBased
        {
            public List<int> intValues;
            public List<uint> intValuesExpressions;
            public List<uint> uintValues;
            public List<uint> uintValuesExpressions;
            public List<float> floatValues;
            public List<uint> floatValuesExpressions;
            public List<Vector2> vector2Values;
            public List<uint> vector2ValuesExpressions;
            public List<Vector3> vector3Values;
            public List<uint> vector3ValuesExpressions;
            public List<Vector4> vector4Values;
            public List<uint> vector4ValuesExpressions;
            public List<Matrix4x4> matrix4x4Values;
            public List<uint> matrix4x4ValuesExpressions;
            public List<AnimationCurve> animationCurveValues;
            public List<uint> animationCurveValuesExpressions;
            public List<Gradient> gradientValues;
            public List<uint> gradientValuesExpressions;
            public List<EntityId> textureValues;
            public List<uint> textureValuesExpressions;
            public List<EntityId> meshValues;
            public List<uint> meshValuesExpressions;
            public List<EntityId> skinnedMeshRendererValues;
            public List<uint> skinnedMeshRendererValuesExpressions;
            public List<bool> boolValues;
            public List<uint> boolValuesExpressions;

            public static implicit operator VFXExpressionValuesSheetInternal(VFXExpressionValuesSheetInternalListBased source)
            {
                var internalSheet = new VFXExpressionValuesSheetInternal
                {
                    intValues = source.intValues?.ToArray(),
                    intValuesExpressions = source.intValuesExpressions?.ToArray(),
                    uintValues = source.uintValues?.ToArray(),
                    uintValuesExpressions = source.uintValuesExpressions?.ToArray(),
                    floatValues = source.floatValues?.ToArray(),
                    floatValuesExpressions = source.floatValuesExpressions?.ToArray(),
                    vector2Values = source.vector2Values?.ToArray(),
                    vector2ValuesExpressions = source.vector2ValuesExpressions?.ToArray(),
                    vector3Values = source.vector3Values?.ToArray(),
                    vector3ValuesExpressions = source.vector3ValuesExpressions?.ToArray(),
                    vector4Values = source.vector4Values?.ToArray(),
                    vector4ValuesExpressions = source.vector4ValuesExpressions?.ToArray(),
                    matrix4x4Values = source.matrix4x4Values?.ToArray(),
                    matrix4x4ValuesExpressions = source.matrix4x4ValuesExpressions?.ToArray(),
                    animationCurveValues = source.animationCurveValues?.ToArray(),
                    animationCurveValuesExpressions = source.animationCurveValuesExpressions?.ToArray(),
                    gradientValues = source.gradientValues?.ToArray(),
                    gradientValuesExpressions = source.gradientValuesExpressions?.ToArray(),
                    textureValues = source.textureValues?.ToArray(),
                    textureValuesExpressions = source.textureValuesExpressions?.ToArray(),
                    meshValues = source.meshValues?.ToArray(),
                    meshValuesExpressions = source.meshValuesExpressions?.ToArray(),
                    skinnedMeshRendererValues = source.skinnedMeshRendererValues?.ToArray(),
                    skinnedMeshRendererValuesExpressions = source.skinnedMeshRendererValuesExpressions?.ToArray(),
                    boolValues = source.boolValues?.ToArray(),
                    boolValuesExpressions = source.boolValuesExpressions?.ToArray()
                };

                return internalSheet;
            }
        }

        public static VFXExpressionValuesSheetInternal CreateValueSheet(VFXExpressionValueContainerDesc[] values)
        {
            var internalSheet = new VFXExpressionValuesSheetInternalListBased();
            if (values == null)
                return internalSheet;

            foreach (var value in values)
            {
                if (value is VFXExpressionValueContainerDesc<int> castedInt)
                {
                    internalSheet.intValues ??= new();
                    internalSheet.intValuesExpressions ??= new();
                    internalSheet.intValues.Add(castedInt.value);
                    internalSheet.intValuesExpressions.Add(castedInt.expressionIndex);
                }
                else if (value is VFXExpressionValueContainerDesc<uint> castedUint)
                {
                    internalSheet.uintValues ??= new();
                    internalSheet.uintValuesExpressions ??= new();
                    internalSheet.uintValues.Add(castedUint.value);
                    internalSheet.uintValuesExpressions.Add(castedUint.expressionIndex);
                }
                else if (value is VFXExpressionValueContainerDesc<float> castedFloat)
                {
                    internalSheet.floatValues ??= new();
                    internalSheet.floatValuesExpressions ??= new();
                    internalSheet.floatValues.Add(castedFloat.value);
                    internalSheet.floatValuesExpressions.Add(castedFloat.expressionIndex);
                }
                else if (value is VFXExpressionValueContainerDesc<Vector2> castedVector2)
                {
                    internalSheet.vector2Values ??= new();
                    internalSheet.vector2ValuesExpressions ??= new();
                    internalSheet.vector2Values.Add(castedVector2.value);
                    internalSheet.vector2ValuesExpressions.Add(castedVector2.expressionIndex);
                }
                else if (value is VFXExpressionValueContainerDesc<Vector3> castedVector3)
                {
                    internalSheet.vector3Values ??= new();
                    internalSheet.vector3ValuesExpressions ??= new();
                    internalSheet.vector3Values.Add(castedVector3.value);
                    internalSheet.vector3ValuesExpressions.Add(castedVector3.expressionIndex);
                }
                else if (value is VFXExpressionValueContainerDesc<Vector4> castedVector4)
                {
                    internalSheet.vector4Values ??= new();
                    internalSheet.vector4ValuesExpressions ??= new();
                    internalSheet.vector4Values.Add(castedVector4.value);
                    internalSheet.vector4ValuesExpressions.Add(castedVector4.expressionIndex);
                }
                else if (value is VFXExpressionValueContainerDesc<Matrix4x4> castedMatrix4x4)
                {
                    internalSheet.matrix4x4Values ??= new();
                    internalSheet.matrix4x4ValuesExpressions ??= new();
                    internalSheet.matrix4x4Values.Add(castedMatrix4x4.value);
                    internalSheet.matrix4x4ValuesExpressions.Add(castedMatrix4x4.expressionIndex);
                }
                else if (value is VFXExpressionObjectValueContainerDesc<Texture> castedTexture)
                {
                    internalSheet.textureValues ??= new();
                    internalSheet.textureValuesExpressions ??= new();
                    internalSheet.textureValues.Add(castedTexture.entityId);
                    internalSheet.textureValuesExpressions.Add(castedTexture.expressionIndex);
                }
                else if (value is VFXExpressionObjectValueContainerDesc<Mesh> castedMesh)
                {
                    internalSheet.meshValues ??= new();
                    internalSheet.meshValuesExpressions ??= new();
                    internalSheet.meshValues.Add(castedMesh.entityId);
                    internalSheet.meshValuesExpressions.Add(castedMesh.expressionIndex);
                }
                else if (value is VFXExpressionObjectValueContainerDesc<SkinnedMeshRenderer> castedSkinnedMeshRenderer)
                {
                    internalSheet.skinnedMeshRendererValues ??= new();
                    internalSheet.skinnedMeshRendererValuesExpressions ??= new();
                    internalSheet.skinnedMeshRendererValues.Add(castedSkinnedMeshRenderer.entityId);
                    internalSheet.skinnedMeshRendererValuesExpressions.Add(castedSkinnedMeshRenderer.expressionIndex);
                }
                else if (value is VFXExpressionValueContainerDesc<Gradient> castedGradient)
                {
                    internalSheet.gradientValues ??= new();
                    internalSheet.gradientValuesExpressions ??= new();
                    internalSheet.gradientValues.Add(castedGradient.value);
                    internalSheet.gradientValuesExpressions.Add(castedGradient.expressionIndex);
                }
                else if (value is VFXExpressionValueContainerDesc<AnimationCurve> castedAnimationCurve)
                {
                    internalSheet.animationCurveValues ??= new();
                    internalSheet.animationCurveValuesExpressions ??= new();
                    internalSheet.animationCurveValues.Add(castedAnimationCurve.value);
                    internalSheet.animationCurveValuesExpressions.Add(castedAnimationCurve.expressionIndex);
                }
                else if (value is VFXExpressionValueContainerDesc<bool> castedBool)
                {
                    internalSheet.boolValues ??= new();
                    internalSheet.boolValuesExpressions ??= new();
                    internalSheet.boolValues.Add(castedBool.value);
                    internalSheet.boolValuesExpressions.Add(castedBool.expressionIndex);
                }
                else if (value is VFXExpressionValueContainerDesc<GraphicsBuffer>)
                {
                    //Nothing to do, graphicsBufferValues are always null, simply ignore
                }
                else
                {
                    throw new InvalidOperationException("Unknown VFXExpressionValueContainerDesc type : " + value.GetType());
                }
            }
            return internalSheet;
        }

        [FreeFunction(Name = "VisualEffectResourceBindings::GetShaderSourceCount", ThrowsException = true, HasExplicitThis = true)] public extern int GetShaderSourceCount();
        [FreeFunction(Name = "VisualEffectResourceBindings::GetShaderSourceName", ThrowsException = true, HasExplicitThis = true)] public extern string GetShaderSourceName(int index);
        [FreeFunction(Name = "VisualEffectResourceBindings::GetShaderSource", ThrowsException = true, HasExplicitThis = true)] public extern string GetShaderSource(int index);
        [FreeFunction(Name = "VisualEffectResourceBindings::GetShader", ThrowsException = true, HasExplicitThis = true)] public extern UnityObject GetShader(int index);

        public extern bool compileInitialVariants { get; set; }

        public const uint uncompiledVersion = 0;
        public const uint defaultVersion = 1;

        extern public VFXRendererSettings rendererSettings { get; set; }
        extern public VFXUpdateMode updateMode { get; set; }
        extern public float preWarmDeltaTime { get; set; }
        extern public uint preWarmStepCount { get; set; }
        extern public string initialEventName
        {
            [FreeFunction("VisualEffectResourceBindings::GetInitialEventName", HasExplicitThis = true)]
            get;
            [FreeFunction("VisualEffectResourceBindings::SetInitialEventName", HasExplicitThis = true)]
            set;
        }
        extern public VFXCullingFlags cullingFlags { get; set; }
        extern public VFXInstancingMode instancingMode { get; set; }
        extern public uint instancingCapacity { get; set; }
        extern public string assetPathString { get; set; }
        extern public void WriteAsset();
        public static extern VisualEffectResource GetResourceAtPath(string path);
        public static extern VisualEffectResource GetResourceAtPathAndForget(string path);
        static extern public void ForgetAtPath(string path);
        public extern void SetAssetPath(string path);

        public extern UnityObject[] GetContents();
        public extern void SetContents(UnityObject[] dependencies);

        public bool isSubgraph
        { get { return visualEffectObject is VisualEffectSubgraph; } }

        VisualEffectObject m_Object;
        public VisualEffectAsset asset
        { get { return visualEffectObject as VisualEffectAsset;} }
        public VisualEffectSubgraph subgraph
        { get { return visualEffectObject as VisualEffectSubgraph; } }

        public VisualEffectObject visualEffectObject
        {
            get
            {
                if (m_Object == null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(this);
                    m_Object = AssetDatabase.LoadAssetAtPath<VisualEffectObject>(assetPath);
                }
                return m_Object;
            }
        }
        extern public ScriptableObject graph { get; set; }

        extern public Material FindMaterial(ScriptableObject model);

        [UsedByNativeCode]
        internal static GUID[] FilterImportDependencies(GUID[] externalGuids, string[] externalPaths, bool sourceOnly)
        {
            if (onFilterImportDependencies != null)
                return onFilterImportDependencies(externalGuids, externalPaths, sourceOnly);

            return null;
        }

        // Re-registered only on code reload (VFXGraphPreprocessor static ctor).
        [AutoStaticsCleanupOnCodeReload]
        internal static Func<GUID[], string[], bool, GUID[]> onFilterImportDependencies;

        [UsedByNativeCode]
        internal static bool EarlyGetAuthoringCompileData(AssetImportContext context, GUID sourceGUID, IntPtr outDesc)
        {
            if (onEarlyGetAuthoringCompileData != null)
            {
                VisualEffectAssetDesc desc;
                if (onEarlyGetAuthoringCompileData(sourceGUID, context, out desc))
                {
                    var descInternal = VisualEffectAssetUtility.ConvertDescToInternal(desc);
                    VisualEffectAssetUtility.CopyVisualEffectAssetDesc(outDesc, descInternal);
                    return true;
                }
            }

            return false;
        }

        // Use actual delegate declaration instead of Func to be able to have out parameter
        internal delegate bool EarlyGetAuthoringCompileDataFunc(GUID id, AssetImportContext ctx, out VisualEffectAssetDesc outDesc);
        [AutoStaticsCleanupOnCodeReload]
        internal static EarlyGetAuthoringCompileDataFunc onEarlyGetAuthoringCompileData;

        [UsedByNativeCode]
        internal void CompileResource(AssetImportContext context, IntPtr outDesc)
        {
            if (onCompileResource != null)
            {
                var visualEffectAssetDesc = onCompileResource(this, context);
                var desc = VisualEffectAssetUtility.ConvertDescToInternal(visualEffectAssetDesc);
                VisualEffectAssetUtility.CopyVisualEffectAssetDesc(outDesc, desc);
            }
        }

        [AutoStaticsCleanupOnCodeReload]
        internal static Func<VisualEffectResource, AssetImportContext, VisualEffectAssetDesc> onCompileResource;
    }
}
