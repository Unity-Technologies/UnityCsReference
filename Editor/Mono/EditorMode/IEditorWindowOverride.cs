// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Experimental.UIElements;

namespace Unity.Experimental.EditorMode
{
    internal interface IEditorWindowOverride
    {
        /// <summary>
        /// Root element to use for the override.
        /// </summary>
        VisualElement Root { get; }

        /// <summary>
        /// Indicates if the OnGUI method should be called on the EditorWindow.
        /// </summary>
        bool InvokeOnGUIEnabled { get; }

        /// <summary>
        /// Called once when the override is loaded.
        /// </summary>
        void OnEnable();

        /// <summary>
        /// Called once when the override is unloaded.
        /// </summary>
        void OnDisable();

        /// <summary>
        /// Called every time the associated window becomes visible in its Dock Area.
        /// </summary>
        void OnBecameVisible();

        /// <summary>
        /// Called every time the associated window becomes invisible in its Dock Area.
        /// </summary>
        void OnBecameInvisible();

        /// <summary>
        /// Called multiple times per second when the associated window is visible.
        /// </summary>
        void Update();

        /// <summary>
        /// Called when the associated window gets keyboard focus.
        /// </summary>
        void OnFocus();

        /// <summary>
        /// Called when the associated window loses keyboard focus.
        /// </summary>
        void OnLostFocus();

        /// <summary>
        /// Called whenever the selection has changed.
        /// </summary>
        void OnSelectionChanged();

        /// <summary>
        /// Called when the project is changed.
        /// </summary>
        void OnProjectChange();

        /// <summary>
        /// Called when a scene is opened.
        /// </summary>
        void OnDidOpenScene();

        /// <summary>
        /// OnInspectorUpdate is called at 10 frames per second to give the inspector a chance to update.
        /// </summary>
        void OnInspectorUpdate();

        /// <summary>
        /// Handler for message that is sent when an object or group of objects in the hierarchy changes.
        /// </summary>
        void OnHierarchyChange();

        /// <summary>
        /// Called when the associated window is resized.
        /// </summary>
        void OnResize();

        /// <summary>
        /// Called whenever the keyboard modifier keys have changed.
        /// </summary>
        void ModifierKeysChanged();

        /// <summary>
        /// Called whenever the associated window dropdown menu is requested.
        /// </summary>
        /// <param name="menu"> The menu we should add items to. </param>
        /// <returns> true if the menu items of the associated window should be added; false otherwise. </returns>
        bool OnAddItemsToMenu(GenericMenu menu);

        /// <summary>
        /// Called whenever the user requests to default mode for a specific window.
        /// </summary>
        void OnSwitchedToDefault();

        /// <summary>
        /// Called whenever the override is switched back on for a specific window.
        /// </summary>
        void OnSwitchedToOverride();
    }
}
