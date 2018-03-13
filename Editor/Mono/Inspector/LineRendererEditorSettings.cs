// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [FilePathAttribute("Library/LineRendererEditorSettings", FilePathAttribute.Location.ProjectFolder)]
    internal class LineRendererEditorSettings : ScriptableSingleton<LineRendererEditorSettings>
    {
        [SerializeField] float m_SimplifyTolerance = 1;
        [SerializeField] bool m_ShowSimplifyPreview = true;
        [SerializeField] float m_CreatePointSeparation = 1;
        [SerializeField] float m_CreationOffset = 5;
        [SerializeField] InputMode m_InputMode = InputMode.MousePosition;
        [SerializeField] LayerMask m_RaycastMask;
        [SerializeField] bool m_ShowWireframe;

        public enum InputMode
        {
            MousePosition,
            PhysicsRaycast
        }

        static void SetValue<T>(ref T original, T value)
        {
            if (!original.Equals(value))
            {
                original = value;
                instance.Save(false);
            }
        }

        public static bool showSimplifyPreview
        {
            get { return instance.m_ShowSimplifyPreview; }
            set { SetValue(ref instance.m_ShowSimplifyPreview, value); }
        }

        public static float createPointSeparation
        {
            get { return instance.m_CreatePointSeparation; }
            set { SetValue(ref instance.m_CreatePointSeparation, value); }
        }

        public static float creationOffset
        {
            get { return instance.m_CreationOffset; }
            set { SetValue(ref instance.m_CreationOffset, value); }
        }

        public static InputMode inputMode
        {
            get { return instance.m_InputMode; }
            set { SetValue(ref instance.m_InputMode, value); }
        }

        public static LayerMask raycastMask
        {
            get { return instance.m_RaycastMask; }
            set { SetValue(ref instance.m_RaycastMask, value); }
        }

        public static bool showWireframe
        {
            get { return instance.m_ShowWireframe; }
            set { SetValue(ref instance.m_ShowWireframe, value); }
        }

        public static float simplifyTolerance
        {
            get { return instance.m_SimplifyTolerance; }
            set { SetValue(ref instance.m_SimplifyTolerance, value); }
        }
    }
}
