// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    internal interface IPanelInputProvider
    {
        PanelInputSettings settings { get; }
    }

    internal static class PanelInputState
    {
        internal static IPanelInputProvider current { get; set; }
        internal static Action<IPanelInputProvider> onApply;
    }

    [Serializable]
    internal struct PanelInputSettings
    {
        // Mirrors PanelInputConfiguration.PanelInputRedirection with compile-time value identity.
        // Uses a separate enum to avoid a managed type reference to PanelInputConfiguration
        // (a MonoBehaviour). Referencing a nested type causes the IL2CPP linker's
        // MarkStep.MarkType to mark the declaring type, and RequiresMarkingOfModule returns
        // true for any UnityEngine.Object, which would force the UIElements engine module
        // into the build even when UIElements is unused.
        internal enum InputRedirection
        {
            [InspectorName("Auto-switch (redirect from EventSystem if present)")]
            AutoSwitch = (int)PanelInputConfiguration.PanelInputRedirection.AutoSwitch,
            [InspectorName("No input redirection")]
            Never = (int)PanelInputConfiguration.PanelInputRedirection.Never,
            [InspectorName("Always redirect from EventSystem (wait if unavailable)")]
            Always = (int)PanelInputConfiguration.PanelInputRedirection.Always,
        }

        private static PanelInputSettings s_Default = new()
        {
            m_ProcessWorldSpaceInput = true,
            m_InteractionLayers = Physics.DefaultRaycastLayers,
            m_MaxInteractionDistance = Mathf.Infinity,
            m_DefaultEventCameraIsMainCamera = true,
            m_EventCameras = Array.Empty<Camera>(),
            m_PanelInputRedirection = InputRedirection.AutoSwitch,
            m_AutoCreatePanelComponents = true
        };
        public static PanelInputSettings Default => s_Default;

        [Tooltip("Determines whether world space panels process input events. Disable this if you need UGUI support but do not require world space input to improve performance.")]
        [SerializeField] internal bool m_ProcessWorldSpaceInput;
        [Tooltip("Determines which layers can block input events on world space panels.")]
        [SerializeField] internal LayerMask m_InteractionLayers;
        [Tooltip("Sets how far away interactions with world-space UI are possible. Defaults to unlimited (infinity), but you can customize it for XR or performance needs. The distance uses GameObject units, consistent with transform positions and Camera clipping planes.")]
        [SerializeField] internal float m_MaxInteractionDistance;
        [Tooltip("Defines whether the Main Camera is used as the Event Camera for world space panels. Disable to specify alternative Event Camera(s) for raycasting input.")]
        [SerializeField] internal bool m_DefaultEventCameraIsMainCamera;
        [Tooltip("Defines the Event Camera(s) used for world space raycasting input.")]
        [SerializeField] internal Camera[] m_EventCameras;
        [Tooltip("Determines which input event system is used for UI interactions when combining UI Toolkit and UGUI.")]
        [SerializeField] internal InputRedirection m_PanelInputRedirection;
        [Tooltip("Automatically adds UI Toolkit components under the EventSystem to handle input redirection between UI Toolkit and UGUI panels. Disable to manually assign these components through code.")]
        [SerializeField] internal bool m_AutoCreatePanelComponents;

        public bool processWorldSpaceInput => m_ProcessWorldSpaceInput;
        public LayerMask interactionLayers => m_InteractionLayers;
        public float maxInteractionDistance => m_MaxInteractionDistance;
        public bool defaultEventCameraIsMainCamera => m_DefaultEventCameraIsMainCamera;
        public Camera[] eventCameras => m_EventCameras;
        public InputRedirection panelInputRedirection => m_PanelInputRedirection;
        public bool autoCreatePanelComponents => m_AutoCreatePanelComponents;

        internal bool shouldRedirectInput => m_PanelInputRedirection != InputRedirection.Never;
    }
}
