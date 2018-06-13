// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.Experimental.VFX
{
    public struct VFXGPUBufferDesc
    {
        public VFXLayoutElementDesc[] layout;
        public uint capacity;
        public ComputeBufferType type;
        public uint size;
    }

    [UsedByNativeCode]
    public sealed class VFXCPUBufferData : IDisposable
    {
        internal IntPtr m_Ptr;
        public VFXCPUBufferData()
        {
            m_Ptr = Internal_Create();
        }

        extern static internal IntPtr Internal_Create();

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        extern static internal void Internal_Destroy(IntPtr ptr);

        extern public void PushUInt(uint v);
        extern public void PushInt(int v);
        extern public void PushFloat(float v);
        extern public void PushBool(bool v);
    }

    [NativeType(CodegenOptions.Custom, "ScriptingVFXCPUBufferDesc")]
    public struct VFXCPUBufferDesc
    {
        public VFXLayoutElementDesc[] layout;
        public uint capacity;
        public uint stride;
        public VFXCPUBufferData initialData;
    }

    [UsedByNativeCode]
    [NativeType(CodegenOptions.Custom, "ScriptingVFXMapping")]
    public struct VFXMapping
    {
        public string name;
        public int index;

        public VFXMapping(string name, int index)
        {
            this.name = name;
            this.index = index;
        }
    }

    [NativeType(CodegenOptions.Custom, "ScriptingVFXTaskDesc")]
    public struct VFXTaskDesc
    {
        public VFXTaskType type;
        public VFXMapping[] buffers;
        public VFXMapping[] values;
        public VFXMapping[] parameters;
        public Object processor;
    }

    public struct VFXSystemDesc
    {
        public VFXSystemType type;
        public VFXSystemFlag flags;
        public uint capacity;
        public VFXMapping[] buffers;
        public VFXMapping[] values;
        public VFXTaskDesc[] tasks;
    };

    public struct VFXRendererSettings
    {
        public MotionVectorGenerationMode motionVectorGenerationMode;
        public ShadowCastingMode shadowCastingMode;
        public bool receiveShadows;
        public ReflectionProbeUsage reflectionProbeUsage;
        public LightProbeUsage lightProbeUsage;
    }

    [NativeType(CodegenOptions.Custom, "ScriptingVFXEventDesc")]
    public struct VFXEventDesc
    {
        public string name;
        public uint[] startSystems;
        public uint[] stopSystems;
    };

    public abstract class VFXExpressionValueContainerDescAbstract
    {
        public uint expressionIndex;
    }

    public class VFXExpressionValueContainerDesc<T> : VFXExpressionValueContainerDescAbstract
    {
        public T value;
    }

    public struct VFXExpressionDesc
    {
        public VFXExpressionOperation op;
        public int[] data;
    }

    public struct VFXLayoutOffset
    {
        public uint bucket;
        public uint structure;
        public uint element;
    }

    [RequiredByNativeCode]
    [NativeType(CodegenOptions.Custom, "ScriptingVFXLayoutElementDesc")]
    public struct VFXLayoutElementDesc
    {
        public string name;
        public VFXValueType type;
        public VFXLayoutOffset offset;
    }

    public struct VFXExpressionSheet
    {
        public VFXExpressionDesc[] expressions;
        public VFXExpressionValueContainerDescAbstract[] values;
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
        public Texture[] textureValues;
        public uint[] textureValuesExpressions;
        public Mesh[] meshValues;
        public uint[] meshValuesExpressions;
        public bool[] boolValues;
        public uint[] boolValuesExpressions;
    }

    [UsedByNativeCode]
    internal struct VFXExpressionSheetInternal
    {
        public VFXExpressionDesc[] expressions;
        public VFXExpressionValuesSheetInternal values;
        public VFXMapping[] exposed;
    }
}
namespace UnityEngine.Experimental.VFX
{
    [UsedByNativeCode]
    [NativeHeader("Modules/VFX/Public/ScriptBindings/VisualEffectAssetBindings.h")]
    [NativeHeader("Modules/VFX/Public/VisualEffectAsset.h")]
    [NativeHeader("VFXScriptingClasses.h")]
    public class VisualEffectAsset : Object
    {
        public VisualEffectAsset()
        {
            CreateVisualEffectAsset(this);
        }

        extern private static void CreateVisualEffectAsset([Writable] VisualEffectAsset scriptingVfx);

        extern public void ClearPropertyData();

        private static VFXExpressionValuesSheetInternal CreateValueSheet(VFXExpressionValueContainerDescAbstract[] values)
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
                else if (group.Key == typeof(VFXExpressionValueContainerDesc<Texture>))
                {
                    var v = group.Cast<VFXExpressionValueContainerDesc<Texture>>().ToArray();
                    internalSheet.textureValues = v.Select(o => o.value).ToArray();
                    internalSheet.textureValuesExpressions = v.Select(o => o.expressionIndex).ToArray();
                }
                else if (group.Key == typeof(VFXExpressionValueContainerDesc<Mesh>))
                {
                    var v = group.Cast<VFXExpressionValueContainerDesc<Mesh>>().ToArray();
                    internalSheet.meshValues = v.Select(o => o.value).ToArray();
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
                else
                {
                    throw new Exception("Unknown VFXExpressionValueContainerDesc type : " + group.Key);
                }
            }
            return internalSheet;
        }

        public void SetExpressionSheet(VFXExpressionSheet sheet)
        {
            var internalSheet = new VFXExpressionSheetInternal();
            internalSheet.expressions = sheet.expressions;
            internalSheet.values = CreateValueSheet(sheet.values);
            internalSheet.exposed = sheet.exposed;
            SetExpressionSheetInternal(internalSheet);
        }

        extern private void SetExpressionSheetInternal(VFXExpressionSheetInternal sheet);

        public void SetValueSheet(VFXExpressionValueContainerDescAbstract[] values)
        {
            var sheet = CreateValueSheet(values);
            SetValueSheet(sheet);
        }

        extern private void SetValueSheet(VFXExpressionValuesSheetInternal sheet);

        [NativeThrows]
        extern public void SetSystems(VFXSystemDesc[] taskDesc, VFXEventDesc[] eventDesc, VFXGPUBufferDesc[] bufferDesc, VFXCPUBufferDesc[] cpuBufferDesc);
        extern public void MarkRuntimeVersion();
        extern public VFXRendererSettings rendererSettings { get; set; }
        extern public VFXCullingFlags cullingFlags { get; set; }

        extern public ScriptableObject graph { get; set; }
    }

    public enum VFXManagerUpdateMode
    {
        Default,
        Force20Hz,
        Force30Hz
    };

    [NativeHeader("Modules/VFX/Public/VFXManager.h")]
    [StaticAccessor("GetVFXManager()", StaticAccessorType.Dot)]
    public static class VFXManager
    {
        extern public static VisualEffect[] GetComponents();
        extern public static string renderPipeSettingsPath { get; }
        extern public static VFXManagerUpdateMode updateMode { get; set; }
        extern public static uint frameIndex { get; }
    }

    [NativeHeader("Modules/VFX/Public/VFXHelpers.h")]
    public static class VFXExpressionHelper
    {
        [FreeFunction("VFX::GetTypeOfOperation", IsThreadSafe = true)]
        extern public static VFXValueType GetTypeOfOperation(VFXExpressionOperation op, int data0, int data1, int data2, int data3);

        [FreeFunction("VFX::GetSizeOfType", IsThreadSafe = true)]
        extern public static int GetSizeOfType(VFXValueType type);

        [FreeFunction("VFX::GetTextureDimension", IsThreadSafe = true)]
        extern public static TextureDimension GetTextureDimension(VFXValueType type);
    }
}
