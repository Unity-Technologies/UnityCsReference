// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Animations
{
    // This file contains the internal burst-compiled implementation of the AnimationStream API.

    [BurstCompile]
    public partial struct AnimationStream
    {
        #region LocalToParent Skeleton Get/Set Methods
        [BurstCompile]
        internal static void GetRotation(in AnimationStream stream, int index, out quaternion rotation)
        {
            int idx = index & 0x3; // equivalent to % 4
            ref readonly quaternion4 q4 = ref stream.m_LocalRotationData.GetRO(index >> 2);
            rotation = math.quaternion(q4.x[idx], q4.y[idx], q4.z[idx], q4.w[idx]);
        }

        [BurstCompile]
        static void GetLocalToParentTR(in AnimationStream stream, int index, out float3 translation, out quaternion rotation)
        {
            GetRotation(stream, index, out rotation);
            translation = stream.m_LocalTranslationData.GetRO(index);
        }

        [BurstCompile]
        static void GetLocalToParentTRS(in AnimationStream stream, int index, out float3 translation, out quaternion rotation, out float3 scale)
        {
            GetRotation(stream, index, out rotation);
            translation = stream.m_LocalTranslationData.GetRO(index);
            scale = stream.m_LocalScaleData.GetRO(index);
        }

        [BurstCompile]
        static void GetLocalToParentTRS(in AnimationStream stream, int index, out TRS trs)
        {
            GetRotation(stream, index, out var rot);
            trs = new TRS(stream.m_LocalTranslationData.GetRO(index), rot, stream.m_LocalScaleData.GetRO(index));
        }

        [BurstCompile]
        static void GetLocalToParentMatrix(in AnimationStream stream, int index, out AffineTransform tx)
        {
            GetRotation(stream, index, out var rot);
            tx = math.AffineTransform(stream.m_LocalTranslationData.GetRO(index), rot, stream.m_LocalScaleData.GetRO(index));
        }

        [BurstCompile]
        static void GetLocalToParentInverseMatrix(in AnimationStream stream, int index, out AffineTransform invTx)
        {
            GetLocalToParentTRS(stream, index, out var trs);
            invTx = MathUtility.inverse(trs);
        }

        [BurstCompile]
        static void SetLocalToParentTranslation(ref AnimationStream stream, int index, in float3 translation)
        {
            SetLocalToParentTranslationNoMaskWrite(ref stream, index, translation);

            var bitIdx = stream.Rig.Bindings.TranslationBindingIndex + index;
            stream.m_ChannelPassMasks.Set(bitIdx, true);
            stream.m_ChannelFrameMasks.Set(bitIdx, true);
        }

        [BurstCompile]
        static void SetLocalToParentTranslationNoMaskWrite(ref AnimationStream stream, int index, in float3 translation)
        {
            stream.m_LocalTranslationData.Set(index, translation);
        }

        [BurstCompile]
        static void SetRotation(ref AnimationStream stream, int index, in quaternion rotation)
        {
            SetRotationNoMaskWrite(ref stream, index, rotation);

            var bitIdx = stream.Rig.Bindings.RotationBindingIndex + index;
            stream.m_ChannelPassMasks.Set(bitIdx, true);
            stream.m_ChannelFrameMasks.Set(bitIdx, true);
        }

        [BurstCompile]
        static void SetRotationNoMaskWrite(ref AnimationStream stream, int index, in quaternion rotation)
        {
            int idx = index & 0x3; // equivalent to % 4
            ref quaternion4 q4 = ref stream.m_LocalRotationData.GetRW(index >> 2);
            q4.x[idx] = rotation.value.x;
            q4.y[idx] = rotation.value.y;
            q4.z[idx] = rotation.value.z;
            q4.w[idx] = rotation.value.w;
        }

        [BurstCompile]
        static void SetLocalToParentScale(ref AnimationStream stream, int index, in float3 scale)
        {
            stream.m_LocalScaleData.Set(index, scale);

            var bitIdx = stream.Rig.Bindings.ScaleBindingIndex + index;
            stream.m_ChannelPassMasks.Set(bitIdx, true);
            stream.m_ChannelFrameMasks.Set(bitIdx, true);
        }

        [BurstCompile]
        static void SetFloat(ref AnimationStream stream, int index, float value)
        {
            stream.m_FloatData.Set(index, value);

            var bitIdx = stream.Rig.Bindings.FloatBindingIndex + index;
            stream.m_ChannelPassMasks.Set(bitIdx, true);
            stream.m_ChannelFrameMasks.Set(bitIdx, true);
        }

        [BurstCompile]
        static void SetInt(ref AnimationStream stream, int index, int value)
        {
            stream.m_IntData.Set(index, value);

            var bitIdx = stream.Rig.Bindings.IntBindingIndex + index;
            stream.m_ChannelPassMasks.Set(bitIdx, true);
            stream.m_ChannelFrameMasks.Set(bitIdx, true);
        }

        [BurstCompile]
        static void SetEntityId(ref AnimationStream stream, int index, in EntityId entityId)
        {
            stream.m_EntityIdData.Set(index, entityId);

            var bitIdx = stream.Rig.Bindings.EntityIdBindingIndex + index;
            stream.m_ChannelPassMasks.Set(bitIdx, true);
            stream.m_ChannelFrameMasks.Set(bitIdx, true);
        }

        [BurstCompile]
        static void SetLocalToParentTR(ref AnimationStream stream, int index, in float3 translation, in quaternion rotation)
        {
            SetLocalToParentTranslation(ref stream, index, translation);
            SetRotation(ref stream, index, rotation);
        }

        [BurstCompile]
        static void SetLocalToParentTRS(ref AnimationStream stream, int index, in float3 translation, in quaternion rotation, in float3 scale)
        {
            SetLocalToParentTranslation(ref stream, index, translation);
            SetRotation(ref stream, index, rotation);
            SetLocalToParentScale(ref stream, index, scale);
        }

        #endregion

        #region LocalToCharacter Skeleton Get/Set Functions
        [BurstCompile]
        static void GetLocalToCharacterTranslation(in AnimationStream stream, int index, out float3 translation)
        {
            translation = stream.GetLocalToParentTranslation(index);
            if (index > 0)
                TransformPosition(stream, stream.Rig.Skeleton.ParentIndexes[index], ref translation);
        }

        [BurstCompile]
        static void SetLocalToCharacterTranslation(ref AnimationStream stream, int index, in float3 translation)
        {
            var localT = translation;
            if (index > 0)
                InverseTransformPosition(stream, stream.Rig.Skeleton.ParentIndexes[index], ref localT);

            stream.SetLocalToParentTranslation(index, localT);
        }

        [BurstCompile]
        static void GetLocalToCharacterRotation(in AnimationStream stream, int index, out quaternion rotation)
        {
            rotation = stream.GetLocalToParentRotation(index);
            if (index > 0)
                TransformRotation(stream, stream.Rig.Skeleton.ParentIndexes[index], ref rotation);
        }

        [BurstCompile]
        static void SetLocalToCharacterRotation(ref AnimationStream stream, int index, in quaternion rotation)
        {
            var localR = rotation;
            if (index > 0)
                InverseTransformRotation(stream, stream.Rig.Skeleton.ParentIndexes[index], ref localR);

            stream.SetLocalToParentRotation(index, localR);
        }

        [BurstCompile]
        static void GetLocalToCharacterScale(in AnimationStream stream, int index, out float3 scale)
        {
            ComputeLocalToCharacterRotationAndRSMatrix(stream, index, out quaternion rootR, out float3x3 rootRS);
            scale = MathUtility.extractScale(rootR, rootRS);
        }

        [BurstCompile]
        static void SetLocalToCharacterScale(ref AnimationStream stream, int index, in float3 scale)
        {
            var localS = scale;
            if (index > 0)
            {
                ComputeLocalToCharacterRotationAndRSMatrix(stream, stream.Rig.Skeleton.ParentIndexes[index], out var parentRootR, out var parentRootRS);

                quaternion localR = stream.GetLocalToParentRotation(index);
                float3x3 localRMat = math.float3x3(localR);
                float3x3 invRootRMat = math.float3x3(math.conjugate(math.mul(parentRootR, localR)));

                float3x3 parentRootS = math.mul(invRootRMat, math.mul(parentRootRS, localRMat));
                localS = MathUtility.rcpsafe(math.float3(parentRootS.c0.x, parentRootS.c1.y, parentRootS.c2.z)) * localS;
            }

            stream.SetLocalToParentScale(index, localS);
        }

        [BurstCompile]
        static void GetLocalToCharacterTR(in AnimationStream stream, int index, out float3 translation, out quaternion rotation)
        {
            translation = stream.GetLocalToParentTranslation(index);
            rotation = stream.GetLocalToParentRotation(index);

            if (index > 0)
                TransformPositionRotation(stream, stream.Rig.Skeleton.ParentIndexes[index], ref translation, ref rotation);
        }

        [BurstCompile]
        static void SetLocalToCharacterTR(ref AnimationStream stream, int index, in float3 translation, in quaternion rotation)
        {
            var localT = translation;
            var localR = rotation;
            if (index > 0)
                InverseTransformPositionRotation(stream, stream.Rig.Skeleton.ParentIndexes[index], ref localT, ref localR);

            stream.SetLocalToParentTR(index, localT, localR);
        }

        [BurstCompile]
        static void GetLocalToCharacterTRS(in AnimationStream stream, int index, out float3 translation, out quaternion rotation, out float3 scale)
        {
            var parentTRS = stream.GetLocalToParentTRS(index);
            translation = parentTRS.t;
            rotation = parentTRS.r;

            var rootRS = math.float3x3(parentTRS.r);
            rootRS = math.mulScale(rootRS, parentTRS.s);

            float3x3 parentRS;
            int parentIdx = stream.Rig.Skeleton.ParentIndexes[index];
            while (parentIdx != k_InvalidIndex)
            {
                parentTRS = stream.GetLocalToParentTRS(parentIdx);
                parentRS = math.float3x3(parentTRS.r);
                parentRS = math.mulScale(parentRS, parentTRS.s);
                rootRS = math.mul(parentRS, rootRS);

                translation = MathUtility.transform(parentTRS, translation);
                rotation = MathUtility.scaleMulQuat(parentTRS.s, rotation);
                rotation = math.mul(parentTRS.r, rotation);

                parentIdx = stream.Rig.Skeleton.ParentIndexes[parentIdx];
            }

            rotation = math.normalize(rotation);
            scale = MathUtility.extractScale(rotation, rootRS);
        }

        [BurstCompile]
        static void SetLocalToCharacterTRS(ref AnimationStream stream, int index, in float3 translation, in quaternion rotation, in float3 scale)
        {
            var localT = translation;
            var localR = rotation;
            var localS = scale;

            if (index > 0)
            {
                ComputeLocalToCharacterInverseRotationAndMatrix(stream, stream.Rig.Skeleton.ParentIndexes[index], out quaternion parentRootInvR, out AffineTransform parentRootInvTx);
                var parentRootInvScale = MathUtility.extractScale(parentRootInvR, parentRootInvTx.rs);

                localT = math.transform(parentRootInvTx, localT);
                localR = math.mul(parentRootInvR, localR);
                localR = math.normalize(MathUtility.scaleMulQuat(MathUtility.rcpsafe(parentRootInvScale), localR));

                var invRootR = math.float3x3(math.conjugate(rotation));
                var localRMat = math.float3x3(localR);
                var parentRootS = math.mul(invRootR, math.mul(math.pseudoinverse(parentRootInvTx.rs), localRMat));
                localS = MathUtility.rcpsafe(math.float3(parentRootS.c0.x, parentRootS.c1.y, parentRootS.c2.z)) * localS;
            }

            stream.SetLocalToParentTRS(index, localT, localR, localS);
        }

        [BurstCompile]
        static void GetLocalToCharacterMatrix(in AnimationStream stream, int index, out AffineTransform tx)
        {
            tx = stream.GetLocalToParentMatrix(index);

            int parentIdx = stream.Rig.Skeleton.ParentIndexes[index];
            while (parentIdx != k_InvalidIndex)
            {
                tx = math.mul(stream.GetLocalToParentMatrix(parentIdx), tx);
                parentIdx = stream.Rig.Skeleton.ParentIndexes[parentIdx];
            }
        }

        [BurstCompile]
        static void GetLocalToCharacterInverseMatrix(in AnimationStream stream, int index, out AffineTransform invTx)
        {
            invTx = stream.GetLocalToParentInverseMatrix(index);

            int parentIdx = stream.Rig.Skeleton.ParentIndexes[index];
            while (parentIdx != k_InvalidIndex)
            {
                invTx = math.mul(invTx, stream.GetLocalToParentInverseMatrix(parentIdx));
                parentIdx = stream.Rig.Skeleton.ParentIndexes[parentIdx];
            }
        }

        #endregion

        #region Transform Utils Methods
        [BurstCompile]
        static void TransformPosition(in AnimationStream stream, int index, ref float3 position)
        {
            while (index != k_InvalidIndex)
            {
                position = MathUtility.transform(stream.GetLocalToParentTRS(index), position);
                index = stream.Rig.Skeleton.ParentIndexes[index];
            }
        }

        [BurstCompile]
        static void InverseTransformPosition(in AnimationStream stream, int index, ref float3 position)
        {
            position = math.transform(stream.GetLocalToCharacterInverseMatrix(index), position);
        }

        [BurstCompile]
        static void TransformRotation(in AnimationStream stream, int index, ref quaternion rotation)
        {
            while (index != k_InvalidIndex)
            {
                rotation = MathUtility.scaleMulQuat(stream.GetLocalToParentScale(index), rotation);
                rotation = math.mul(stream.GetLocalToParentRotation(index), rotation);
                index = stream.Rig.Skeleton.ParentIndexes[index];
            }

            rotation = math.normalize(rotation);
        }

        [BurstCompile]
        static void InverseTransformRotation(in AnimationStream stream, int index, ref quaternion rotation)
        {
            ComputeLocalToCharacterRotationAndRSMatrix(stream, index, out var r, out var rs);
            rotation = math.mul(math.conjugate(r), rotation);
            rotation = math.normalize(MathUtility.scaleMulQuat(MathUtility.extractScale(r, rs), rotation));
        }

        [BurstCompile]
        static void TransformPositionRotation(in AnimationStream stream, int index, ref float3 position, ref quaternion rotation)
        {
            while (index != k_InvalidIndex)
            {
                var trs = stream.GetLocalToParentTRS(index);

                position = MathUtility.transform(trs, position);
                rotation = MathUtility.scaleMulQuat(trs.s, rotation);
                rotation = math.mul(trs.r, rotation);

                index = stream.Rig.Skeleton.ParentIndexes[index];
            }

            rotation = math.normalize(rotation);
        }

        [BurstCompile]
        static void InverseTransformPositionRotation(in AnimationStream stream, int index, ref float3 position, ref quaternion rotation)
        {
            ComputeLocalToCharacterInverseRotationAndMatrix(stream, index, out quaternion invR, out AffineTransform invTx);
            var invS = MathUtility.extractScale(invR, invTx.rs);

            position = math.transform(invTx, position);
            rotation = math.mul(invR, rotation);
            rotation = math.normalize(MathUtility.scaleMulQuat(MathUtility.rcpsafe(invS), rotation));
        }

        [BurstCompile]
        static void TransformVector(in AnimationStream stream, int index, ref float3 vector)
        {
            while (index != k_InvalidIndex)
            {
                var trs = stream.GetLocalToParentTRS(index);
                vector = math.mul(trs.r, vector * trs.s);
                index = stream.Rig.Skeleton.ParentIndexes[index];
            }
        }

        [BurstCompile]
        static void InverseTransformVector(in AnimationStream stream, int index, ref float3 vector)
        {
            ComputeLocalToCharacterInverseRotationAndRSMatrix(stream, index, out _, out var invRS);
            vector = math.mul(invRS, vector);
        }

        [BurstCompile]
        static void TransformDirection(in AnimationStream stream, int index, ref float3 direction)
        {
            direction = math.mul(stream.GetLocalToParentRotation(index), direction);
            var parentIndex = stream.Rig.Skeleton.ParentIndexes[index];
            while (parentIndex != k_InvalidIndex)
            {
                direction = math.chgsign(direction, math.sign(stream.GetLocalToParentScale(parentIndex)));
                direction = math.mul(stream.GetLocalToParentRotation(parentIndex), direction);
                parentIndex = stream.Rig.Skeleton.ParentIndexes[parentIndex];
            }
        }

        [BurstCompile]
        static void InverseTransformDirection(in AnimationStream stream, int index, ref float3 direction)
        {
            if (index > 0)
            {
                ComputeLocalToCharacterRotationAndRSMatrix(stream, stream.Rig.Skeleton.ParentIndexes[index], out var parentR, out var parentRS);
                direction = math.mul(math.conjugate(parentR), direction);
                direction = math.chgsign(direction, math.sign(MathUtility.extractScale(parentR, parentRS)));
            }

            direction = math.mul(math.conjugate(stream.GetLocalToParentRotation(index)), direction);
        }

        #endregion

        #region Socket Methods

        [BurstCompile]
        static void GetSocketLocalToParentMatrix(in AnimationStream stream, int index, out AffineTransform tx)
        {
            ref readonly var socket = ref stream.Rig.SocketValues[index];
            tx = math.AffineTransform(socket.LocalTranslationValue, socket.LocalRotationValue, socket.LocalScaleValue);
        }

        [BurstCompile]
        static void GetSocketLocalToParentInverseMatrix(in AnimationStream stream, int index, out AffineTransform invTx)
        {
            GetSocketLocalToParentTRS(stream, index, out var trs);
            invTx = MathUtility.inverse(trs);
        }

        [BurstCompile]
        static void GetSocketLocalToParentTR(in AnimationStream stream, int index, out float3 translation, out quaternion rotation)
        {
            ref readonly var socket = ref stream.Rig.SocketValues[index];
            translation = socket.LocalTranslationValue;
            rotation = socket.LocalRotationValue;
        }

        [BurstCompile]
        static void GetSocketLocalToParentTRS(in AnimationStream stream, int index, out float3 translation, out quaternion rotation, out float3 scale)
        {
            ref readonly var socket = ref stream.Rig.SocketValues[index];
            translation = socket.LocalTranslationValue;
            rotation = socket.LocalRotationValue;
            scale = socket.LocalScaleValue;
        }

        [BurstCompile]
        static void GetSocketLocalToParentTRS(in AnimationStream stream, int index, out TRS trs)
        {
            ref readonly var socket = ref stream.Rig.SocketValues[index];
            trs = new TRS(socket.LocalTranslationValue, socket.LocalRotationValue, socket.LocalScaleValue);
        }

        [BurstCompile]
        static void GetSocketLocalToCharacterTranslation(in AnimationStream stream, int index, out float3 translation)
        {
            ref readonly var socket = ref stream.Rig.SocketValues[index];
            translation = socket.LocalTranslationValue;
            TransformPosition(stream, socket.BoneParentIndex, ref translation);
        }

        [BurstCompile]
        static void GetSocketLocalToCharacterRotation(in AnimationStream stream, int index, out quaternion rotation)
        {
            ref readonly var socket = ref stream.Rig.SocketValues[index];
            rotation = socket.LocalRotationValue;
            TransformRotation(stream, socket.BoneParentIndex, ref rotation);
        }

        [BurstCompile]
        static void GetSocketLocalToCharacterScale(in AnimationStream stream, int index, out float3 scale)
        {
            ref readonly var socket = ref stream.Rig.SocketValues[index];

            var socketR = socket.LocalRotationValue;
            var socketRS = math.float3x3(socketR);
            socketRS = math.mulScale(socketRS, socket.LocalScaleValue);

            ComputeLocalToCharacterRotationAndRSMatrix(stream, socket.BoneParentIndex, out var parentR, out var parentRS);
            var parentS = MathUtility.extractScale(parentR, parentRS);

            socketRS = math.mul(parentRS, socketRS);

            socketR = MathUtility.scaleMulQuat(parentS, socketR);
            socketR = math.normalize(math.mul(parentR, socketR));

            scale = MathUtility.extractScale(socketR, socketRS);
        }

        [BurstCompile]
        static void GetSocketLocalToCharacterMatrix(in AnimationStream stream, int index, out AffineTransform tx)
        {
            ref readonly var socket = ref stream.Rig.SocketValues[index];
            GetLocalToCharacterMatrix(stream, socket.BoneParentIndex, out var parentTx);
            GetSocketLocalToParentMatrix(stream, index, out var socketTx);

            tx = math.mul(parentTx, socketTx);
        }

        [BurstCompile]
        static void GetSocketLocalToCharacterInverseMatrix(in AnimationStream stream, int index, out AffineTransform invTx)
        {
            ref readonly var socket = ref stream.Rig.SocketValues[index];
            GetLocalToCharacterInverseMatrix(stream, socket.BoneParentIndex, out var parentInvTx);
            GetSocketLocalToParentInverseMatrix(stream, index, out var socketInvTx);

            invTx = math.mul(socketInvTx, parentInvTx);
        }

        [BurstCompile]
        static void GetSocketLocalToCharacterTR(in AnimationStream stream, int index, out float3 translation, out quaternion rotation)
        {
            ref readonly var socket = ref stream.Rig.SocketValues[index];

            translation = socket.LocalTranslationValue;
            rotation = socket.LocalRotationValue;
            TransformPositionRotation(stream, socket.BoneParentIndex, ref translation, ref rotation);
        }

        [BurstCompile]
        static void GetSocketLocalToCharacterTRS(in AnimationStream stream, int index, out float3 translation, out quaternion rotation, out float3 scale)
        {
            ref readonly var socket = ref stream.Rig.SocketValues[index];

            GetSocketLocalToParentTRS(stream, index, out var parentTRS);
            translation = parentTRS.t;
            rotation = parentTRS.r;

            var socketRS = math.float3x3(parentTRS.r);
            socketRS = math.mulScale(socketRS, parentTRS.s);

            GetLocalToCharacterTRS(stream, socket.BoneParentIndex, out parentTRS.t, out parentTRS.r, out parentTRS.s);

            var parentRS = math.float3x3(parentTRS.r);
            parentRS = math.mulScale(parentRS, parentTRS.s);
            socketRS = math.mul(parentRS, socketRS);

            translation = MathUtility.transform(parentTRS, translation);
            rotation = MathUtility.scaleMulQuat(parentTRS.s, rotation);
            rotation = math.normalize(math.mul(parentTRS.r, rotation));

            scale = MathUtility.extractScale(rotation, socketRS);
        }

        #endregion

        #region Implementation Helpers
        [BurstCompile]
        static void ComputeLocalToCharacterRotationAndMatrix(in AnimationStream stream, int index, out quaternion rootR, out AffineTransform rootTx)
        {
            rootR = stream.GetLocalToParentRotation(index);
            rootTx = stream.GetLocalToParentMatrix(index);

            int parentIdx = stream.Rig.Skeleton.ParentIndexes[index];
            while (parentIdx != k_InvalidIndex)
            {
                rootTx = math.mul(stream.GetLocalToParentMatrix(parentIdx), rootTx);
                rootR = MathUtility.scaleMulQuat(stream.GetLocalToParentScale(parentIdx), rootR);
                rootR = math.mul(stream.GetLocalToParentRotation(parentIdx), rootR);

                parentIdx = stream.Rig.Skeleton.ParentIndexes[parentIdx];
            }

            rootR = math.normalize(rootR);
        }

        [BurstCompile]
        static void ComputeLocalToCharacterInverseRotationAndMatrix(in AnimationStream stream, int index, out quaternion invRootR, out AffineTransform invRootTx)
        {
            quaternion rootR = stream.GetLocalToParentRotation(index);
            invRootTx = stream.GetLocalToParentInverseMatrix(index);

            int parentIdx = stream.Rig.Skeleton.ParentIndexes[index];
            while (parentIdx != k_InvalidIndex)
            {
                invRootTx = math.mul(invRootTx, stream.GetLocalToParentInverseMatrix(parentIdx));

                rootR = MathUtility.scaleMulQuat(stream.GetLocalToParentScale(parentIdx), rootR);
                rootR = math.mul(stream.GetLocalToParentRotation(parentIdx), rootR);

                parentIdx = stream.Rig.Skeleton.ParentIndexes[parentIdx];
            }

            invRootR = math.inverse(rootR);
        }

        [BurstCompile]
        static void ComputeLocalToCharacterRotationAndRSMatrix(in AnimationStream stream, int index, out quaternion rootR, out float3x3 rootRS)
        {
            rootR = stream.GetLocalToParentRotation(index);
            rootRS = math.float3x3(rootR);
            rootRS = math.mulScale(rootRS, stream.GetLocalToParentScale(index));

            int parentIdx = stream.Rig.Skeleton.ParentIndexes[index];
            quaternion parentR;
            float3 parentS;
            float3x3 parentRS;
            while (parentIdx != k_InvalidIndex)
            {
                parentR = stream.GetLocalToParentRotation(parentIdx);
                parentS = stream.GetLocalToParentScale(parentIdx);

                parentRS = math.float3x3(parentR);
                parentRS = math.mulScale(parentRS, parentS);
                rootRS = math.mul(parentRS, rootRS);

                rootR = MathUtility.scaleMulQuat(parentS, rootR);
                rootR = math.mul(parentR, rootR);

                parentIdx = stream.Rig.Skeleton.ParentIndexes[parentIdx];
            }

            rootR = math.normalize(rootR);
        }

        [BurstCompile]
        static void ComputeLocalToCharacterInverseRotationAndRSMatrix(in AnimationStream stream, int index, out quaternion invRootR, out float3x3 invRootRS)
        {
            quaternion rootR = stream.GetLocalToParentRotation(index);
            invRootRS = math.float3x3(math.conjugate(rootR));
            invRootRS = math.scaleMul(MathUtility.rcpsafe(stream.GetLocalToParentScale(index)), invRootRS);

            int parentIdx = stream.Rig.Skeleton.ParentIndexes[index];
            quaternion parentR;
            float3 parentS;
            float3x3 invParentRS;
            while (parentIdx != k_InvalidIndex)
            {
                parentR = stream.GetLocalToParentRotation(parentIdx);
                parentS = stream.GetLocalToParentScale(parentIdx);

                invParentRS = math.float3x3(math.conjugate(parentR));
                invParentRS = math.scaleMul(MathUtility.rcpsafe(parentS), invParentRS);
                invRootRS = math.mul(invRootRS, invParentRS);

                rootR = MathUtility.scaleMulQuat(parentS, rootR);
                rootR = math.mul(parentR, rootR);

                parentIdx = stream.Rig.Skeleton.ParentIndexes[parentIdx];
            }

            invRootR = math.inverse(rootR);
        }

        #endregion
    }
}
