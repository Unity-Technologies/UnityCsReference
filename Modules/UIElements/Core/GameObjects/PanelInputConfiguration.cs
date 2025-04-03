// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Configures how input is routed to Panels in runtime.
    /// If no Input Configuration component is active, default configurations are used.
    /// </summary>
    [HelpURL("UIE-get-started-with-runtime-ui")]
    [AddComponentMenu("UI Toolkit/Panel Input Configuration", 1), ExecuteAlways, DisallowMultipleComponent]
    public sealed partial class PanelInputConfiguration : MonoBehaviour
    {
        internal static PanelInputConfiguration current { get; set; }

        internal static int s_ActiveInstances = 0;

        internal static Action<PanelInputConfiguration> onApply = null;

        /// <summary>
        /// Indicates whether the uGUI EventSystem redirects panel input.
        /// </summary>
        /// <remarks>
        /// When the uGUI EventSystem redirects panel input, panel components
        /// handle the EventSystem events and translate them into equivalent
        /// UI Toolkit events if applicable.
        /// This allows scenes to contain a mix of UI Toolkit and uGUI content
        /// with a common logic.
        /// </remarks>
        public enum PanelInputRedirection
        {
            /// <summary>
            /// As long as an EventSystem is active, it serves as the source of input for all UI.
            /// Otherwise, UIToolkit's built-in input handles the input.
            /// </summary>
            /// <remarks>This is the default option.</remarks>
            [InspectorName("Auto-switch (redirect from EventSystem if present)")]
            AutoSwitch = default,
            /// <summary>
            /// UIToolkit's built-in input handles the input, regardless of whether an EventSystem is active.
            /// </summary>
            [InspectorName("No input redirection")]
            Never,
            /// <summary>
            /// Input remains disabled until an EventSystem is active, at which point it becomes the input source.
            /// Use this mode to dynamically activate or deactivate all UI input by toggling
            /// the EventSystem component.
            /// </summary>
            [InspectorName("Always redirect from EventSystem (wait if unavailable)")]
            Always,
        }

        [Serializable]
        internal partial struct Settings
        {
            private static Settings s_Default = new()
            {
                m_ProcessWorldSpaceInput = true,
                m_InteractionLayers = Physics.DefaultRaycastLayers,
                m_MaxInteractionDistance = Mathf.Infinity,
                m_DefaultEventCameraIsMainCamera = true,
                m_EventCameras = Array.Empty<Camera>(),
                m_PanelInputRedirection = PanelInputRedirection.AutoSwitch,
                m_AutoCreatePanelComponents = true
            };
            public static Settings Default => s_Default;

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
            [SerializeField] internal PanelInputRedirection m_PanelInputRedirection;
            [Tooltip("Automatically adds UI Toolkit components under the EventSystem to handle input redirection between UI Toolkit and UGUI panels. Disable to manually assign these components through code.")]
            [SerializeField] internal bool m_AutoCreatePanelComponents;

            public bool processWorldSpaceInput => m_ProcessWorldSpaceInput;
            public LayerMask interactionLayers => m_InteractionLayers;
            public float maxInteractionDistance => m_MaxInteractionDistance;
            public bool defaultEventCameraIsMainCamera => m_DefaultEventCameraIsMainCamera;
            public Camera[] eventCameras => m_EventCameras;
            public PanelInputRedirection panelInputRedirection => m_PanelInputRedirection;
            public bool autoCreatePanelComponents => m_AutoCreatePanelComponents;
        }

        [SerializeField] private Settings m_Settings = Settings.Default;
        internal Settings settings => m_Settings;

        internal const string SettingsProperty = nameof(m_Settings);

        /// <summary>
        /// This option is enabled by default, allowing panels that use the World Space rendering mode to receive events.
        /// When disabled, only Screen Space Overlay panels receive events.
        /// </summary>
        /// <remarks>
        /// World Space events require at least one Event Camera to transform screen positions into world-space rays.
        /// </remarks>
        public bool processWorldSpaceInput
        {
            get => m_Settings.m_ProcessWorldSpaceInput;
            set
            {
                if (m_Settings.m_ProcessWorldSpaceInput == value) return;
                m_Settings.m_ProcessWorldSpaceInput = value;
                Apply(this);
            }
        }

        /// <summary>
        /// The Physics layers considered when casting world-space rays to determine the target of pointer events.
        /// Any <see cref="UnityEngine.UIElements.UIDocument"/> with one or more Collider on the Interaction Layers,
        /// or with GameObject children with colliders on the Interaction Layers,
        /// sends pointer events corresponding to the input rays to its panel.
        /// Other Colliders on those layers block panel events.
        /// </summary>
        /// <remarks>All layers except the Ignore Raycasts layer are enabled by default.</remarks>
        /// <remarks>If World Space input is disabled, this property has no effect.</remarks>
        public LayerMask interactionLayers
        {
            get => m_Settings.m_InteractionLayers;
            set
            {
                if (m_Settings.m_InteractionLayers == value) return;
                m_Settings.m_InteractionLayers = value;
                Apply(this);
            }
        }

        /// <summary>
        /// The maximal distance considered when casting world-space rays to determine the target of pointer events.
        /// </summary>
        /// <remarks>The default Raycasting Distance is <see cref="Mathf.Infinity"/>.</remarks>
        /// <remarks>If World Space input is disabled, this property has no effect.</remarks>
        public float maxInteractionDistance
        {
            get => m_Settings.m_MaxInteractionDistance;
            set
            {
                if (m_Settings.m_MaxInteractionDistance == value) return;
                m_Settings.m_MaxInteractionDistance = value;
                Apply(this);
            }
        }

        /// <summary>
        /// This option is enabled by default, automatically selecting the Main Camera (if available) as the Event Camera
        /// to transform screen positions into world-space rays.
        /// </summary>
        /// <remarks>
        /// If the Main Camera changes, the Event Camera is updated automatically with the new Camera.
        /// </remarks>
        /// <remarks>
        /// If no Main Camera is active, screen-based events are disabled until a Main Camera becomes active again.
        /// </remarks>
        /// <remarks>If World Space input is disabled, this property has no effect.</remarks>
        public bool defaultEventCameraIsMainCamera
        {
            get => m_Settings.m_DefaultEventCameraIsMainCamera;
            set
            {
                if (m_Settings.m_DefaultEventCameraIsMainCamera == value) return;
                m_Settings.m_DefaultEventCameraIsMainCamera = value;
                Apply(this);
            }
        }

        /// <summary>
        /// The list of Event Cameras used to transform screen positions into world-space rays.
        /// </summary>
        /// <remarks>
        /// If multiple Event Cameras are specified, the Event Cameras are processed sequentially
        /// until one of the cameras produces a ray that hits a Collider.
        /// </remarks>
        /// <remarks>If no Event Camera is specified, screen-based events are disabled.</remarks>
        /// <remarks>If World Space input is disabled, this property has no effect.</remarks>
        public Camera[] eventCameras
        {
            get => m_Settings.m_EventCameras;
            set
            {
                if (m_Settings.m_EventCameras == value) return;
                m_Settings.m_EventCameras = value;
                Apply(this);
            }
        }

        /// <summary>
        /// Selected option for whether or not panel input should be redirected
        /// from uGUI's EventSystem.
        /// </summary>
        /// <remarks>
        /// The default option is <see cref="PanelInputRedirection.AutoSwitch"/>.
        /// </remarks>
        public PanelInputRedirection panelInputRedirection
        {
            get => m_Settings.m_PanelInputRedirection;
            set
            {
                if (m_Settings.m_PanelInputRedirection == value) return;
                m_Settings.m_PanelInputRedirection = value;
                Apply(this);
            }
        }

        /// <summary>
        /// When enabled, automatically creates child GameObjects with raycaster and event handler components
        /// attached to uGUI's EventSystem.
        /// </summary>
        /// <remarks>
        /// If this option is enabled, for each panel that uses the screen space overlay render mode, one child
        /// GameObject is created with a PanelRaycaster and PanelEventHandler components associated to it.
        /// </remarks>
        /// <remarks>
        /// If the <see cref="defaultEventCameraIsMainCamera"/> option is enabled, Unity automatically adds one
        /// WorldDocumentRaycaster to handle inputs for world-space UI.
        /// When created automatically, the WorldDocumentRaycaster's Event Camera is not assigned.
        /// Instead, it automatically detects and uses the Main Camera in the Scene as the source for input.
        /// Otherwise, for each Camera in the <see cref="eventCameras"/> list, a distinct WorldDocumentRaycaster
        /// component is created and its Event Camera property is assigned to that Camera.
        /// </remarks>
        /// <remarks>
        /// If this option is disabled, UI Toolkit events are disabled unless you manually add
        /// raycaster and event handler components to the scene manually and initialize them accordingly.
        /// </remarks>
        /// <remarks>
        /// This property has no effect in the following situations:
        ///
        ///- If there is no active EventSystem.
        ///- If the <see cref="panelInputRedirection"/> option is set to not interact with the EventSystem.
        /// </remarks>
        public bool autoCreatePanelComponents
        {
            get => m_Settings.m_AutoCreatePanelComponents;
            set
            {
                if (m_Settings.m_AutoCreatePanelComponents == value) return;
                m_Settings.m_AutoCreatePanelComponents = value;
                Apply(this);
            }
        }

        private void OnEnable()
        {
            s_ActiveInstances++;

            if (current != null)
            {
                Debug.LogWarning("Multiple Input Configuration components active. Only the first one will be considered.\nEnabled: " + current + ". Disabled: " + this + ".");
                enabled = false;
                return;
            }

            current = this;
            Apply(this);
        }

        private void OnDisable()
        {
            s_ActiveInstances--;

            if (current != this)
                return;

            current = null;
            Apply(null);
        }

        private void OnValidate()
        {
            Apply(this);
        }

        static void Apply(PanelInputConfiguration input)
        {
            var settings = input != null ? input.settings : Settings.Default;

            UIElementsRuntimeUtility.overrideUseDefaultEventSystem = settings.panelInputRedirection switch
            {
                PanelInputRedirection.Never => true,
                PanelInputRedirection.Always => false,
                _ => null
            };
            UIElementsRuntimeUtility.defaultEventSystem.worldSpaceLayers = settings.interactionLayers;
            UIElementsRuntimeUtility.defaultEventSystem.worldSpaceMaxDistance = settings.maxInteractionDistance;
            UIElementsRuntimeUtility.defaultEventSystem.raycaster = settings.processWorldSpaceInput
                ? settings.defaultEventCameraIsMainCamera
                    ? new MainCameraScreenRaycaster()
                    : new CameraScreenRaycaster { cameras = (Camera[]) settings.eventCameras.Clone() }
                : new CameraScreenRaycaster { cameras = Array.Empty<Camera>() };

            onApply?.Invoke(input);
        }
    }
}
