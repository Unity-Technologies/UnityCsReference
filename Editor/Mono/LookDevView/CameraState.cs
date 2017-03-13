// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor
{
    [Serializable]
    internal class CameraState
    {
        private static readonly Quaternion  kDefaultRotation    = Quaternion.LookRotation(new Vector3(0.0f, 0.0f, 1.0f));
        private  const float                kDefaultViewSize    = 10f;
        private static readonly Vector3     kDefaultPivot       = Vector3.zero;
        private const float                 kDefaultFoV         = 90f;

        [SerializeField] private AnimVector3        m_Pivot     = new AnimVector3(kDefaultPivot);
        [SerializeField] private AnimQuaternion     m_Rotation  = new AnimQuaternion(kDefaultRotation);
        [SerializeField] private AnimFloat          m_ViewSize  = new AnimFloat(kDefaultViewSize);

        public float GetCameraDistance()
        {
            float fov = kDefaultFoV;
            return m_ViewSize.value / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        }

        public void FixNegativeSize()
        {
            float fov = kDefaultFoV;
            if (m_ViewSize.value < 0)
            {
                float distance = m_ViewSize.value / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
                Vector3 p = m_Pivot.value + m_Rotation.value * new Vector3(0, 0, -distance);
                m_ViewSize.value = -m_ViewSize.value;
                distance = m_ViewSize.value / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
                m_Pivot.value = p + m_Rotation.value * new Vector3(0, 0, distance);
            }
        }

        public void UpdateCamera(Camera camera)
        {
            camera.transform.rotation = m_Rotation.value;
            camera.transform.position = m_Pivot.value + camera.transform.rotation * new Vector3(0, 0, -GetCameraDistance());

            float farClip = Mathf.Max(1000f, 2000f * m_ViewSize.value);
            camera.nearClipPlane = farClip * 0.000005f;
            camera.farClipPlane = farClip;
        }

        public CameraState Clone()
        {
            CameraState newState = new CameraState();
            newState.pivot.value = pivot.value;
            newState.rotation.value = rotation.value;
            newState.viewSize.value = viewSize.value;

            return newState;
        }

        public void Copy(CameraState cameraStateIn)
        {
            pivot.value = cameraStateIn.pivot.value;
            rotation.value = cameraStateIn.rotation.value;
            viewSize.value = cameraStateIn.viewSize.value;
        }

        public AnimVector3      pivot       { get { return m_Pivot; } set { m_Pivot = value; } }
        public AnimQuaternion   rotation    { get { return m_Rotation; } set { m_Rotation = value; } }
        public AnimFloat        viewSize    { get { return m_ViewSize; } set { m_ViewSize = value; } }
    }
}
