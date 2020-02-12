// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.VFX;
using UnityEditor;

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
        public bool receiveShadows;
        public ReflectionProbeUsage reflectionProbeUsage;
        public LightProbeUsage lightProbeUsage;
    }

    [UsedByNativeCode]
    [NativeType(CodegenOptions.Custom, "ScriptingVFXMapping")]
    internal struct VFXMapping
    {
        public string name;
        public int index;

        public VFXMapping(string name, int index)
        {
            this.name = name;
            this.index = index;
        }
    }

    [UsedByNativeCode]
    [NativeType(CodegenOptions.Custom, "ScriptingVFXMappingTemporary")]
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
        public ComputeBufferType type;
        public uint size;
        public uint stride;
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
    }

    [NativeType(CodegenOptions.Custom, "ScriptingVFXCPUBufferDesc")]
    internal struct VFXCPUBufferDesc
    {
        public VFXLayoutElementDesc[] layout;
        public uint capacity;
        public uint stride;
        public VFXCPUBufferData initialData;
    }

    [NativeType(CodegenOptions.Custom, "ScriptingVFXShaderSourceDesc")]
    internal struct VFXShaderSourceDesc
    {
        public bool compute;
        public string name;
        public string source;
    }

    [UsedByNativeCode]
    [NativeType(CodegenOptions.Custom, "ScriptingVFXEditorTaskDesc")]
    internal struct VFXEditorTaskDesc
    {
        public VFXTaskType type;
        public VFXMapping[] buffers;
        public VFXMappingTemporary[] temporaryBuffers;
        public VFXMapping[] values;
        public VFXMapping[] parameters;
        private UnityObject processor;

        public UnityObject externalProcessor
        {
            get
            {
                return processor;
            }
            set
            {
                processor = value;
                m_ShaderSourceIndex = -1;
            }
        }
        private int m_ShaderSourceIndex;
        public int shaderSourceIndex
        {
            get
            {
                return m_ShaderSourceIndex;
            }
            set
            {
                processor = null;
                m_ShaderSourceIndex = value;
            }
        }
    }
    [UsedByNativeCode]
    [NativeType(CodegenOptions.Custom, "ScriptingVFXEditorSystemDesc")]
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
    }

    [NativeType(CodegenOptions.Custom, "ScriptingVFXEventDesc")]
    internal struct VFXEventDesc
    {
        public string name;
        public uint[] startSystems;
        public uint[] stopSystems;
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
        public int instanceID = 0;
    }

    [NativeType(CodegenOptions.Custom, "ScriptingVFXExpressionDesc")]
    internal struct VFXExpressionDesc
    {
        public VFXExpressionOperation op;
        public int[] data;
    }

    internal struct VFXLayoutOffset
    {
        public uint bucket;
        public uint structure;
        public uint element;
    }

    [RequiredByNativeCode]
    [NativeType(CodegenOptions.Custom, "ScriptingVFXLayoutElementDesc")]
    internal struct VFXLayoutElementDesc
    {
        public string name;
        public VFXValueType type;
        public VFXLayoutOffset offset;
    }

    internal struct VFXExpressionSheet
    {
        public VFXExpressionDesc[] expressions;
        public VFXExpressionDesc[] expressionsPerSpawnEventAttribute;
        public VFXExpressionValueContainerDesc[] values;
        public VFXMapping[] exposed;
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
        public int[] textureValues;
        public uint[] textureValuesExpressions;
        public int[] meshValues;
        public uint[] meshValuesExpressions;
        public bool[] boolValues;
        public uint[] boolValuesExpressions;
    }

    [UsedByNativeCode]
    internal struct VFXExpressionSheetInternal
    {
        public VFXExpressionDesc[] expressions;
        public VFXExpressionDesc[] expressionsPerSpawnEventAttribute;
        public VFXExpressionValuesSheetInternal values;
        public VFXMapping[] exposed;
    }

    [UsedByNativeCode]
    [NativeHeader("Modules/VFXEditor/Public/ScriptBindings/VisualEffectResourceBindings.h")]
    [NativeHeader("Modules/VFXEditor/Public/VisualEffectResource.h")]
    [NativeHeader("VFXScriptingClasses.h")]
    internal class VisualEffectResource : UnityObject
    {
        public VisualEffectResource()
        {
            CreateVisualEffectResource(this);
        }

        public const string Extension = ".vfx";
        extern private static void CreateVisualEffectResource([Writable] VisualEffectResource resource);
        extern public void ClearRuntimeData();

        private static VFXExpressionValuesSheetInternal CreateValueSheet(VFXExpressionValueContainerDesc[] values)
        {
            var internalSheet = new VFXExpressionValuesSheetInternal();
            foreach (var group in values.GroupBy(o => o.GetType()))
            {
                if (group.Key == typeof(VFXExpressionValueContainerDesc<int>))
                {
                    var v = group.Cast<VFXExpressionValueContainerDesc<int>>().ToArray();
                    internalSheet.intValues = v.Select(o => o.value).ToArray();
                    internalSheet.intValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                else if (group.Key == typeof(VFXExpressionValueContainerDesc<UInt32>))
                {
                    var v = group.Cast<VFXExpressionValueContainerDesc<UInt32>>().ToArray();
                    internalSheet.uintValues = v.Select(o => o.value).ToArray();
                    internalSheet.uintValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                else if (group.Key == typeof(VFXExpressionValueContainerDesc<float>))
                {
                    var v = group.Cast<VFXExpressionValueContainerDesc<float>>().ToArray();
                    internalSheet.floatValues = v.Select(o => o.value).ToArray();
                    internalSheet.floatValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                else if (group.Key == typeof(VFXExpressionValueContainerDesc<Vector2>))
                {
                    var v = group.Cast<VFXExpressionValueContainerDesc<Vector2>>().ToArray();
                    internalSheet.vector2Values = v.Select(o => o.value).ToArray();
                    internalSheet.vector2ValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                else if (group.Key == typeof(VFXExpressionValueContainerDesc<Vector3>))
                {
                    var v = group.Cast<VFXExpressionValueContainerDesc<Vector3>>().ToArray();
                    internalSheet.vector3Values = v.Select(o => o.value).ToArray();
                    internalSheet.vector3ValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                else if (group.Key == typeof(VFXExpressionValueContainerDesc<Vector4>))
                {
                    var v = group.Cast<VFXExpressionValueContainerDesc<Vector4>>().ToArray();
                    internalSheet.vector4Values = v.Select(o => o.value).ToArray();
                    internalSheet.vector4ValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                else if (group.Key == typeof(VFXExpressionValueContainerDesc<Matrix4x4>))
                {
                    var v = group.Cast<VFXExpressionValueContainerDesc<Matrix4x4>>().ToArray();
                    internalSheet.matrix4x4Values = v.Select(o => o.value).ToArray();
                    internalSheet.matrix4x4ValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                else if (group.Key == typeof(VFXExpressionObjectValueContainerDesc<Texture>))
                {
                    var v = group.Cast<VFXExpressionObjectValueContainerDesc<Texture>>().ToArray();
                    internalSheet.textureValues = v.Select(o => o.instanceID).ToArray();
                    internalSheet.textureValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                else if (group.Key == typeof(VFXExpressionObjectValueContainerDesc<Mesh>))
                {
                    var v = group.Cast<VFXExpressionObjectValueContainerDesc<Mesh>>().ToArray();
                    for (int i = 0; i < v.Length; ++i)
                    {
                    }
                    internalSheet.meshValues = v.Select(o => o.instanceID).ToArray();
                    internalSheet.meshValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                else if (group.Key == typeof(VFXExpressionValueContainerDesc<Gradient>))
                {
                    var v = group.Cast<VFXExpressionValueContainerDesc<Gradient>>().ToArray();
                    internalSheet.gradientValues = v.Select(o => o.value).ToArray();
                    internalSheet.gradientValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                else if (group.Key == typeof(VFXExpressionValueContainerDesc<AnimationCurve>))
                {
                    var v = group.Cast<VFXExpressionValueContainerDesc<AnimationCurve>>().ToArray();
                    internalSheet.animationCurveValues = v.Select(o => o.value).ToArray();
                    internalSheet.animationCurveValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                else if (group.Key == typeof(VFXExpressionValueContainerDesc<bool>))
                {
                    var v = group.Cast<VFXExpressionValueContainerDesc<bool>>().ToArray();
                    internalSheet.boolValues = v.Select(o => o.value).ToArray();
                    internalSheet.boolValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                //For backward compatibility, Obsoleted by compile on import PR
                else if (group.Key == typeof(VFXExpressionValueContainerDesc<Texture>))
                {
                    var v = group.Cast<VFXExpressionValueContainerDesc<Texture>>().ToArray();
                    internalSheet.textureValues = v.Select(o => o.value != null ? o.value.GetInstanceID() : 0).ToArray();
                    internalSheet.textureValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                //For backward compatibility, Obsoleted by compile on import PR
                else if (group.Key == typeof(VFXExpressionValueContainerDesc<Mesh>))
                {
                    var v = group.Cast<VFXExpressionValueContainerDesc<Mesh>>().ToArray();
                    internalSheet.meshValues = v.Select(o => o.value != null ? o.value.GetInstanceID() : 0).ToArray();
                    internalSheet.meshValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                else
                {
                    throw new Exception("Unknown VFXExpressionValueContainerDesc type : " + group.Key);
                }
            }
            return internalSheet;
        }

        public void SetValueSheet(VFXExpressionValueContainerDesc[] values)
        {
            var sheet = CreateValueSheet(values);
            SetValueSheet(sheet);
        }

        extern private void SetValueSheet(VFXExpressionValuesSheetInternal sheet);

        public extern VFXShaderSourceDesc[] shaderSources { get; set; }

        public extern int GetShaderSourceCount();

        [FreeFunction(Name = "VisualEffectResourceBindings::GetShaderSourceName", ThrowsException = true, HasExplicitThis = true)] public extern string GetShaderSourceName(int index);
        [FreeFunction(Name = "VisualEffectResourceBindings::GetShaderSource", ThrowsException = true, HasExplicitThis = true)] public extern string GetShaderSource(int index);

        public extern bool compileInitialVariants { get; set; }

        public const uint uncompiledVersion = 0;
        public const uint defaultVersion = 1;

        public void SetRuntimeData(VFXExpressionSheet sheet,
            VFXEditorSystemDesc[] systemDesc,
            VFXEventDesc[] eventDesc,
            VFXGPUBufferDesc[] bufferDesc,
            VFXCPUBufferDesc[] cpuBufferDesc,
            VFXTemporaryGPUBufferDesc[] temporaryBufferDesc,
            VFXShaderSourceDesc[] shaderSourceDesc,
            ShadowCastingMode shadowCastingMode,
            MotionVectorGenerationMode motionVectorGenerationMode,
            uint version = defaultVersion)
        {
            var internalSheet = new VFXExpressionSheetInternal();
            internalSheet.expressions = sheet.expressions;
            internalSheet.expressionsPerSpawnEventAttribute = sheet.expressionsPerSpawnEventAttribute;
            internalSheet.values = CreateValueSheet(sheet.values);
            internalSheet.exposed = sheet.exposed;

            SetRuntimeData(internalSheet, systemDesc, eventDesc, bufferDesc, temporaryBufferDesc, cpuBufferDesc, shaderSourceDesc, shadowCastingMode, motionVectorGenerationMode, version);
        }

        //This version is for backward compatibility
        public void SetRuntimeData(VFXExpressionSheet sheet,
            VFXEditorSystemDesc[] systemDesc,
            VFXEventDesc[] eventDesc,
            VFXGPUBufferDesc[] bufferDesc,
            VFXCPUBufferDesc[] cpuBufferDesc,
            VFXTemporaryGPUBufferDesc[] temporaryBufferDesc)
        {
            var internalSheet = new VFXExpressionSheetInternal();
            internalSheet.expressions = sheet.expressions;
            internalSheet.expressionsPerSpawnEventAttribute = sheet.expressionsPerSpawnEventAttribute;
            internalSheet.values = CreateValueSheet(sheet.values);
            internalSheet.exposed = sheet.exposed;

            SetRuntimeDataDeprecated(internalSheet, systemDesc, eventDesc, bufferDesc, temporaryBufferDesc, cpuBufferDesc, this.shaderSources, defaultVersion);
        }

        [NativeThrows]
        extern private void SetRuntimeData(VFXExpressionSheetInternal sheet,
            VFXEditorSystemDesc[] systemDesc,
            VFXEventDesc[] eventDesc,
            VFXGPUBufferDesc[] bufferDesc,
            VFXTemporaryGPUBufferDesc[] temporaryBufferDesc,
            VFXCPUBufferDesc[] cpuBufferDesc,
            VFXShaderSourceDesc[] shaderSourceDesc,
            ShadowCastingMode shadowCastingMode,
            MotionVectorGenerationMode motionVectorGenerationMode,
            uint version);


        //This version is for backward compatilibity
        [NativeThrows]
        extern private void SetRuntimeDataDeprecated(VFXExpressionSheetInternal sheet,
            VFXEditorSystemDesc[] systemDesc,
            VFXEventDesc[] eventDesc,
            VFXGPUBufferDesc[] bufferDesc,
            VFXTemporaryGPUBufferDesc[] temporaryBufferDesc,
            VFXCPUBufferDesc[] cpuBufferDesc,
            VFXShaderSourceDesc[] shaderSourceDesc,
            uint version);
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

        extern public void MarkRuntimeVersion();
        extern public void ValidateAsset();
        extern public void WriteAsset();

        public static extern VisualEffectResource GetResourceAtPath(string path);
        static extern public void DeleteAtPath(string path);
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
        extern public int GetShaderIndex(UnityObject shader);
        public extern void ShowGeneratedShaderFile(int index, int line = 0);

        extern public void ClearSourceDependencies();
        extern public void AddSourceDependency(string dep);
        extern public void ClearImportDependencies();
        extern public void AddImportDependency(string dep);

        [UsedByNativeCode]
        internal static string[] AddResourceDependencies(string assetPath)
        {
            if (onAddResourceDependencies != null)
                return onAddResourceDependencies(assetPath);

            return null;
        }

        internal static Func<string, string[]> onAddResourceDependencies;

        [UsedByNativeCode]
        internal void CompileResource()
        {
            if (onCompileResource != null)
                onCompileResource(this);
        }

        internal static Action<VisualEffectResource> onCompileResource;
    }
}
