// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    partial class Camera
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property isOrthoGraphic has been deprecated. Use orthographic (UnityUpgradable) -> orthographic", true)]
        public bool isOrthoGraphic { get { return false; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Camera.GetScreenWidth has been deprecated. Use Screen.width instead (UnityUpgradable) -> Screen.width", true)]
        public float GetScreenWidth() { return 0.0f; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Camera.GetScreenHeight has been deprecated. Use Screen.height instead (UnityUpgradable) -> Screen.height", true)]
        public float GetScreenHeight() { return 0.0f; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property mainCamera has been deprecated. Use Camera.main instead (UnityUpgradable) -> main", true)]
        public static Camera mainCamera { get { return null; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Camera.DoClear has been deprecated (UnityUpgradable).", true)]
        public void DoClear() {}

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property near has been deprecated. Use Camera.nearClipPlane instead (UnityUpgradable) -> UnityEngine.Camera.nearClipPlane", false)]
        public float near { get { return nearClipPlane; } set { nearClipPlane = value; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property far has been deprecated. Use Camera.farClipPlane instead (UnityUpgradable) -> UnityEngine.Camera.farClipPlane", false)]
        public float far { get { return farClipPlane; } set { farClipPlane = value; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property fov has been deprecated. Use Camera.fieldOfView instead (UnityUpgradable) -> UnityEngine.Camera.fieldOfView", false)]
        public float fov { get { return fieldOfView; } set { fieldOfView = value; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Camera.ResetFieldOfView has been deprecated in Unity 5.6 and will be removed in the future. Please replace it by explicitly setting the camera's FOV to 60 degrees.", false)]
        // for some weird reason cpp ResetFieldOfView was doing much less than SetFOV. Now we explicitly call SetFOV(60)
        public void ResetFieldOfView() { fieldOfView = 60; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property hdr has been deprecated. Use Camera.allowHDR instead (UnityUpgradable) -> UnityEngine.Camera.allowHDR", false)]
        public bool hdr { get { return allowHDR; } set { allowHDR = value; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property stereoMirrorMode is no longer supported. Please use single pass stereo rendering instead.", true)]
        public bool stereoMirrorMode { get { return false; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Camera.SetStereoViewMatrices has been deprecated. Use SetStereoViewMatrix(StereoscopicEye eye) instead.", false)]
        public void SetStereoViewMatrices(Matrix4x4 leftMatrix, Matrix4x4 rightMatrix)
        {
            SetStereoViewMatrix(StereoscopicEye.Left, leftMatrix);
            SetStereoViewMatrix(StereoscopicEye.Right, rightMatrix);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Camera.SetStereoProjectionMatrices has been deprecated. Use SetStereoProjectionMatrix(StereoscopicEye eye) instead.", false)]
        public void SetStereoProjectionMatrices(Matrix4x4 leftMatrix, Matrix4x4 rightMatrix)
        {
            SetStereoProjectionMatrix(StereoscopicEye.Left, leftMatrix);
            SetStereoProjectionMatrix(StereoscopicEye.Right, rightMatrix);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Camera.GetStereoViewMatrices has been deprecated. Use GetStereoViewMatrix(StereoscopicEye eye) instead.", false)]
        public Matrix4x4[] GetStereoViewMatrices()
        {
            return new Matrix4x4[] { GetStereoViewMatrix(StereoscopicEye.Left), GetStereoViewMatrix(StereoscopicEye.Right) };
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Camera.GetStereoProjectionMatrices has been deprecated. Use GetStereoProjectionMatrix(StereoscopicEye eye) instead.", false)]
        public Matrix4x4[] GetStereoProjectionMatrices()
        {
            return new Matrix4x4[] { GetStereoProjectionMatrix(StereoscopicEye.Left), GetStereoProjectionMatrix(StereoscopicEye.Right) };
        }
    }
}
