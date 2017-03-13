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
        [Obsolete("Property GetScreenWidth() has been deprecated. Use Screen.width instead (UnityUpgradable) -> Screen.width", true)]
        public float GetScreenWidth() { return 0.0f; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property GetScreenHeight() has been deprecated. Use Screen.height instead (UnityUpgradable) -> Screen.height", true)]
        public float GetScreenHeight() { return 0.0f; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property mainCamera has been deprecated. Use Camera.main instead (UnityUpgradable) -> main", true)]
        public static Camera mainCamera { get { return null; } }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Camera.DoClear has been deprecated (UnityUpgradable).", true)]
        public void DoClear() {}
    }
}
