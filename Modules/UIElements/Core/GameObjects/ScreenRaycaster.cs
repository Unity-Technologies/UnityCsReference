// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace UnityEngine.UIElements;

internal interface IScreenRaycaster
{
    void Update();
    IEnumerable<(Ray ray, Camera camera)> MakeRay(Vector2 mousePosition, int pointerId, int? targetDisplay);
}

internal class CameraScreenRaycaster : IScreenRaycaster
{
    public Camera[] cameras = Array.Empty<Camera>();
    public Camera[] singleCamera = new Camera[1];
    public int layerMask = ~0;

    public virtual void Update()
    {
        Array.Sort(cameras, (a, b) => -a.depth.CompareTo(b.depth));
    }

    public IEnumerable<(Ray, Camera)> MakeRay(Vector2 mousePosition, int pointerId, int? targetDisplay)
    {
        var capturingCamera = singleCamera[0] = PointerDeviceState.GetCameraWithSoftPointerCapture(pointerId);
        return CameraRayEnumerator.GetPooled(capturingCamera != null ? singleCamera : cameras, layerMask, mousePosition, targetDisplay);
    }

    private static bool IsValid(Camera camera, int layerMask, int? targetDisplay)
    {
        return camera != null && (camera.cullingMask & layerMask) != 0 &&
               (targetDisplay == null || camera.targetDisplay == targetDisplay);
    }

    private static Ray MakeRay(Camera camera, Vector2 mousePosition)
    {
        var screenPosition =
            UIElementsRuntimeUtility.PanelToScreenBottomLeftPosition(mousePosition, camera.targetDisplay);
        return camera.ScreenPointToRay(screenPosition);
    }

    public class CameraRayEnumerator : IEnumerator<(Ray, Camera)>, IEnumerable<(Ray, Camera)>
    {
        private Camera[] m_Cameras;
        private int m_LayerMask;
        private Vector2 m_MousePosition;
        private int? m_TargetDisplay;
        private int m_Index = -1;

        public bool MoveNext()
        {
            while (++m_Index < m_Cameras.Length)
                if (IsValid(m_Cameras[m_Index], m_LayerMask, m_TargetDisplay))
                    return true;
            return false;
        }

        public void Reset() => m_Index = -1;
        public (Ray, Camera) Current => (MakeRay(m_Cameras[m_Index], m_MousePosition), m_Cameras[m_Index]);
        object IEnumerator.Current => Current;
        public IEnumerator<(Ray, Camera)> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static CameraRayEnumerator GetPooled(Camera[] cameras, int layerMask, Vector2 mousePosition, int? targetDisplay)
        {
            var result = GenericPool<CameraRayEnumerator>.Get();
            result.m_Cameras = cameras;
            result.m_LayerMask = layerMask;
            result.m_MousePosition = mousePosition;
            result.m_TargetDisplay = targetDisplay;
            return result;
        }

        public void Dispose()
        {
            Reset();
            m_Cameras = default; // Clear references
            GenericPool<CameraRayEnumerator>.Release(this);
        }
    }
}

internal class MainCameraScreenRaycaster : CameraScreenRaycaster
{
    private Camera[] singleCameraArray = new Camera[1];

    public MainCameraScreenRaycaster() => ResolveCamera();
    public override void Update() => ResolveCamera();

    private void ResolveCamera()
    {
        var mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameras = singleCameraArray;
            cameras[0] = mainCamera;
        }
        else
        {
            cameras = Array.Empty<Camera>();
        }
    }
}
