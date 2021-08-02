// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Overlays;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    sealed class LegacyOverlay : IMGUIOverlay, ITransientOverlay
    {
        public OverlayWindow data { get; set; }

        public bool showRequested {get; set; }

        public override void OnGUI()
        {
            data?.sceneViewFunc?.Invoke(data.target, (SceneView)containerWindow);
        }

        public bool visible => showRequested;
    }

    // Deprecated. Use `TransientSceneViewOverlay` instead.
    class SceneViewOverlay
    {
        // This enum is for better overview of the ordering of our builtin overlays
        public enum Ordering
        {
            // Lower order is below high order when showed together
            Camera = -100,
            ClothConstraints = 0,
            ClothSelfAndInterCollision = 100,
            OcclusionCulling = 200,
            Lightmapping = 300,
            NavMesh = 400,
            PhysicsDebug = 450,
            TilemapRenderer = 500,
            ParticleEffect = 600
        }

        public enum WindowDisplayOption
        {
            MultipleWindowsPerTarget,
            OneWindowPerTarget,
            OneWindowPerTitle
        }

        public delegate void WindowFunction(Object target, SceneView sceneView);

        public SceneViewOverlay(SceneView sceneView) {}

        public void Begin() {}

        public void End() {}

        const string k_ObsoleteMessage = "Please use UnityEditor.Overlays.TransientSceneViewOverlay in place of SceneViewOverlay.";

        [Obsolete(k_ObsoleteMessage)]
        // pass window parameter to render in sceneviews that are not the active view.
        public static void Window(GUIContent title, WindowFunction sceneViewFunc, int order, WindowDisplayOption option)
        {
            Window(title, sceneViewFunc, order, null, option);
        }

        [Obsolete(k_ObsoleteMessage)]
        // pass window parameter to render in sceneviews that are not the active view.
        public static void Window(GUIContent title, WindowFunction sceneViewFunc, int order, Object target, WindowDisplayOption option, EditorWindow window = null)
        {
            if (Event.current.type != EventType.Layout)
                return;

            ShowWindow(new OverlayWindow(title, sceneViewFunc, order, target, option));
        }

        [Obsolete(k_ObsoleteMessage)]
        public static void ShowWindow(OverlayWindow window)
        {
            if (Event.current.type != EventType.Layout)
                return;

            if (SceneView.currentDrawingSceneView == null)
            {
                Debug.Log("SceneViewOverlay.ShowWindow can only be called from the scene view GUI");
                return;
            }

            SceneView.currentDrawingSceneView.ShowLegacyOverlay(window);
        }
    }

    class OverlayWindow : IComparable<OverlayWindow>
    {
        public OverlayWindow(GUIContent title, SceneViewOverlay.WindowFunction guiFunction, int primaryOrder, Object target,
                             SceneViewOverlay.WindowDisplayOption option)
        {
            this.title = title;
            this.sceneViewFunc = guiFunction;
            this.primaryOrder = primaryOrder;
            this.option = option;
            this.target = target;
            this.canCollapse = true;
            this.expanded = true;
        }

        public SceneViewOverlay.WindowFunction sceneViewFunc { get;  }
        public int primaryOrder { get; }
        public int secondaryOrder { get; set; }
        public Object target { get; }
        public EditorWindow editorWindow { get; set; }

        public SceneViewOverlay.WindowDisplayOption option { get; } =
            SceneViewOverlay.WindowDisplayOption.MultipleWindowsPerTarget;

        public bool canCollapse { get; set; }
        public bool expanded { get; set; }

        public GUIContent title { get; }

        public int CompareTo(OverlayWindow other)
        {
            return 0;
        }
    }
}
