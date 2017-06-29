// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LODParameters
    {
        // has to be int for marshaling
        private int m_IsOrthographic;
        private Vector3 m_CameraPosition;
        private float m_FieldOfView;
        private float m_OrthoSize;
        private int m_CameraPixelHeight;

        public bool isOrthographic
        {
            get { return Convert.ToBoolean(m_IsOrthographic); }
            set { m_IsOrthographic = Convert.ToInt32(value); }
        }

        public Vector3 cameraPosition
        {
            get { return m_CameraPosition; }
            set { m_CameraPosition = value; }
        }

        public float fieldOfView
        {
            get { return m_FieldOfView; }
            set { m_FieldOfView = value; }
        }

        public float orthoSize
        {
            get { return m_OrthoSize; }
            set { m_OrthoSize = value; }
        }

        public int cameraPixelHeight
        {
            get { return m_CameraPixelHeight; }
            set { m_CameraPixelHeight = value; }
        }
    }
}
