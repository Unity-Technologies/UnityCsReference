// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEngine.UIElements
{
    /// <summary>
    /// Global configuration options for UI Toolkit input.
    /// </summary>
    public static class UIToolkitInputConfiguration
    {
        /// <summary>
        /// Use this method to activate one of the two input backends available for UIToolkit events at runtime.
        /// The new Input System compatible backend allows the Input System package to send its input to UI Toolkit
        /// directly, removing the need for an <see cref="UnityEngine.EventSystems.EventSystem"/> in the user scene,
        /// and will automatically fall back to Input Manager input if the Input System package input isn't enabled in
        /// the Player Settings active input handling.
        /// Alternatively, use the legacy backend to always rely on Input Manager input only. In that case,
        /// if the Input Manager is not enabled as an active input handler, UI Toolkit runtime events will not work.
        /// </summary>
        /// <remarks>
        /// The Input System compatible backend is enabled by default.
        /// Call this method to disable it or to enable it again if it was otherwise disabled.
        /// </remarks>
        /// <remarks>
        /// Setting the runtime input backend has no impact on Editor-only content such as EditorWindows or
        /// custom inspectors.
        /// </remarks>
        /// <remarks>
        /// This method has no effect if there is an <see cref="UnityEngine.EventSystems.EventSystem"/> in the user
        /// scene. In that case, UI Toolkit runtime events will be provided by that EventSystem for as long as it
        /// remains enabled.
        /// </remarks>
        /// <param name="backend">
        /// The input backend to be used as the source of input for UI Toolkit events at runtime.
        /// </param>
        public static void SetRuntimeInputBackend(UIToolkitInputBackendOption backend)
        {
            UIElementsRuntimeUtility.defaultEventSystem.useInputForUI =
                backend != UIToolkitInputBackendOption.LegacyBackend;
        }
    }

    /// <summary>
    /// Input backend options for UI Toolkit events at runtime.
    /// </summary>
    public enum UIToolkitInputBackendOption
    {
        /// <summary>
        /// The initial configuration on UI Toolkit start up. This is equal to
        /// <see cref="InputSystemCompatibleBackend"/>.
        /// This option will use the Input System package is available, and the old Input Manager if not.
        /// </summary>
        Default,

        /// <summary>
        /// The new Input System compatible backend allows the Input System package to send its input to UI Toolkit
        /// directly, removing the need for an <see cref="UnityEngine.EventSystems.EventSystem"/> in the user scene.
        /// This option will use the Input System package is available, and the old Input Manager if not.
        /// </summary>
        InputSystemCompatibleBackend = Default,

        /// <summary>
        /// The legacy backend relies on the Input Manager <see cref="UnityEngine.Input"/> class only and is not
        /// compatible with the Input System package. This option will always try to use the old Input Manager.
        /// If the Input Manager is not enabled as an active input handler, UI Toolkit runtime events will not work.
        /// </summary>
        LegacyBackend
    }
}
