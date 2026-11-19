// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animations
{
    class RigDebugView
    {
        UnityObjectRef<Avatar> m_Rig;

        bool IsValid => Rig.IsCreated;

        BlobAssetReference<RigDefinition> Rig
        {
            get
            {
                // Return reference that we dispose right away, the blob asset will be used immediately after, without
                // giving room for the blob asset to be unloaded.
                using var blobObjectReference = m_Rig.Value.ToRigReference();
                return blobObjectReference.AsBlobAssetReference();
            }
        }

        public RigDebugView(UnityObjectRef<Avatar> rig)
        {
            m_Rig = rig;
        }

        public StringHash[] SkeletonIds
        {
            get
            {
                return IsValid ? Rig.Value.Skeleton.Ids.ToArray() : Array.Empty<StringHash>();
            }
        }

        public int[] SkeletonParentIndices
        {
            get
            {
                return IsValid ? Rig.Value.Skeleton.ParentIndexes.ToArray() : Array.Empty<int>();
            }
        }


        public StringHash[] TranslationBindings
        {
            get
            {
                return IsValid ? Rig.Value.Bindings.TranslationBindings.ToArray() : Array.Empty<StringHash>();
            }
        }

        public StringHash[] RotationBindings
        {
            get
            {
                return IsValid ? Rig.Value.Bindings.RotationBindings.ToArray() : Array.Empty<StringHash>();
            }
        }

        public StringHash[] ScaleBindings
        {
            get
            {
                return IsValid ? Rig.Value.Bindings.ScaleBindings.ToArray() : Array.Empty<StringHash>();
            }
        }

        public StringHash[] FloatBindings
        {
            get
            {
                return IsValid ? Rig.Value.Bindings.FloatBindings.ToArray() : Array.Empty<StringHash>();
            }
        }

        public StringHash[] IntBindings
        {
            get
            {
                return IsValid ? Rig.Value.Bindings.IntBindings.ToArray() : Array.Empty<StringHash>();
            }
        }

        public unsafe float3[] TranslationDefaultValues
        {
            get
            {
                if (!IsValid)
                {
                    return Array.Empty<float3>();
                }

                var count = Rig.Value.Bindings.TranslationCurveCount / BindingSet.TranslationKeyFloatCount;

                float* floatPtr = (float*)Rig.Value.DefaultValues.GetUnsafePtr();
                var data = new Ptr<float3>((float3*)(floatPtr + Rig.Value.Bindings.TranslationSamplesOffset));

                float3[] values = new float3[count];
                for (int i = 0; i < count; i++)
                    values[i] = data.GetRO(i);

                return values;
            }
        }

        public unsafe quaternion[] RotationDefaultValues
        {
            get
            {
                if (!IsValid)
                {
                    return Array.Empty<quaternion>();
                }

                var count = Rig.Value.Bindings.RotationCurveCount / BindingSet.RotationKeyFloatCount;

                float* floatPtr = (float*)Rig.Value.DefaultValues.GetUnsafePtr();
                var data = new Ptr<quaternion4>((quaternion4*)(floatPtr + Rig.Value.Bindings.RotationSamplesOffset));

                quaternion[] values = new quaternion[count];
                for (int i = 0; i < count; i++)
                {
                    int idx = i & 0x3; // equivalent to % 4
                    ref readonly quaternion4 q4 = ref data.GetRO(i >> 2);
                    values[i] = math.quaternion(q4.x[idx], q4.y[idx], q4.z[idx], q4.w[idx]);
                }

                return values;
            }
        }

        public unsafe float3[] ScaleDefaultValues
        {
            get
            {
                if (!IsValid)
                {
                    return Array.Empty<float3>();
                }

                var count = Rig.Value.Bindings.ScaleCurveCount / BindingSet.ScaleKeyFloatCount;

                float* floatPtr = (float*)Rig.Value.DefaultValues.GetUnsafePtr();
                var data = new Ptr<float3>((float3*)(floatPtr + Rig.Value.Bindings.ScaleSamplesOffset));

                float3[] values = new float3[count];
                for (int i = 0; i < count; i++)
                    values[i] = data.GetRO(i);

                return values;
            }
        }

        public unsafe float[] FloatDefaultValues
        {
            get
            {
                if (!IsValid)
                {
                    return Array.Empty<float>();
                }

                var count = Rig.Value.Bindings.FloatCurveCount / BindingSet.FloatKeyFloatCount;

                float* floatPtr = (float*)Rig.Value.DefaultValues.GetUnsafePtr();
                var data = new Ptr<float>((float*)(floatPtr + Rig.Value.Bindings.FloatSamplesOffset));

                float[] values = new float[count];
                for (int i = 0; i < count; i++)
                    values[i] = data.GetRO(i);

                return values;
            }
        }

        public unsafe int[] IntDefaultValues
        {
            get
            {
                if (!IsValid)
                {
                    return Array.Empty<int>();
                }

                var count = Rig.Value.Bindings.IntCurveCount / BindingSet.IntKeyFloatCount;

                float* floatPtr = (float*)Rig.Value.DefaultValues.GetUnsafePtr();
                var data = new Ptr<int>((int*)(floatPtr + Rig.Value.Bindings.IntSamplesOffset));

                int[] values = new int[count];
                for (int i = 0; i < count; i++)
                    values[i] = data.GetRO(i);

                return values;
            }
        }
    }
}
