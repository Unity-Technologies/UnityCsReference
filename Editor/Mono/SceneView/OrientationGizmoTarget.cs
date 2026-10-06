// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

// Decouples OrientationGizmoElement from SceneView, so a future host beyond the Scene View only
// needs its own OrientationGizmoTarget implementation.
abstract class OrientationGizmoTarget
{
    public abstract Quaternion rotation { get; }
    public abstract bool orthographic { get; }
    public abstract bool supportsRotationLock { get; }
    public abstract bool isRotationLocked { get; }
    public abstract void SetRotationLocked(bool locked);
    public abstract void AlignTo(Quaternion rotation, bool orthographic);
    public abstract void TogglePerspective();
}

sealed class SceneViewGizmoTarget : OrientationGizmoTarget
{
    readonly SceneView m_View;

    public SceneViewGizmoTarget(SceneView view)
    {
        m_View = view;
    }

    public override Quaternion rotation => m_View.rotation;
    public override bool orthographic => m_View.orthographic;
    public override bool supportsRotationLock => true;
    public override bool isRotationLocked => m_View.isRotationLocked;
    public override void SetRotationLocked(bool locked) => m_View.isRotationLocked = locked;

    public override void AlignTo(Quaternion rotation, bool orthographic)
    {
        m_View.LookAt(m_View.pivot, rotation, m_View.size, orthographic);
    }

    public override void TogglePerspective()
    {
        m_View.LookAt(m_View.pivot, m_View.rotation, m_View.size, !m_View.orthographic);
    }
}
