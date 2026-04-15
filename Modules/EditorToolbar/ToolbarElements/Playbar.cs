// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Bindings;
using UnityEngine.Assertions;

namespace UnityEditor.Toolbars
{
    [VisibleToOtherModules("UnityEditor.PlayModeModule")]
    sealed class Playbar : ScriptableSingleton<Playbar>
    {
        const string k_ElementId = "Play Mode Controls";
        const float k_ImguiOverrideWidth = 240f;

        bool m_IsAvailable = true;
        bool m_HasImguiOverride = false;

        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement(k_ElementId, ussName = "PlayMode", defaultDockIndex = 0,
                            defaultDockPosition = MainToolbarDockPosition.Middle, menuPriority = MainToolbarElementAttribute.defaultMenuPriority - 1)]
        static IEnumerable<MainToolbarElement> Create()
        {
            return instance.Build();
        }

        void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            ModeService.modeChanged += OnModeChanged;

            //Immediately after a domain reload, Modes might be initialized after the toolbar so we wait a frame to check it
            EditorApplication.delayCall += () =>
            {
                CheckAvailability();
                CheckImguiOverride();
            };
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            ModeService.modeChanged -= OnModeChanged;
        }

        IEnumerable<MainToolbarElement> Build()
        {
            List<MainToolbarElement> elements = new List<MainToolbarElement>(3);

            if (m_IsAvailable)
            {
                if (!m_HasImguiOverride)
                {
                    var defaultButtonsInstantiators = TypeCache.GetMethodsWithAttribute<PlaybarDefaultButtonsAttribute>();
                    Assert.IsTrue(defaultButtonsInstantiators.Count == 1, "There should be exactly one method with PlaybarDefaultButtonsAttribute");

                    var instantiator = defaultButtonsInstantiators[0];
                    Assert.IsTrue(instantiator.ReturnType == typeof(IEnumerable<MainToolbarElement>), "Methods with PlaybarDefaultButtonsAttribute must return IEnumerable<MainToolbarElement>.");

                    elements.AddRange((IEnumerable<MainToolbarElement>)instantiator.Invoke(null, [k_ElementId]));
                }
                else
                {
                    elements.Add(new MainToolbarCustom(() =>
                    {
                        var imgui = new IMGUIContainer(OverrideGUIHandler);
                        imgui.style.width = k_ImguiOverrideWidth;
                        return imgui;
                    }));
                }
            }

            return elements;
        }

        void OnModeChanged(ModeService.ModeChangedArgs args)
        {
            CheckAvailability();
            CheckImguiOverride();
        }

        void CheckAvailability()
        {
            bool wasAvailable = m_IsAvailable;
            m_IsAvailable = ModeService.HasCapability(ModeCapability.Playbar, true);

            if (m_IsAvailable != wasAvailable)
                MainToolbar.Refresh(k_ElementId);
        }

        // this is a bug that will be covered in the ticket [UUM-135117] Fixing m_HasImguiOverride not being set 

        void CheckImguiOverride()
        {
            var wasOverriden = ModeService.HasExecuteHandler("gui_playbar");
            if (wasOverriden != m_HasImguiOverride)
            {
                MainToolbar.Refresh(k_ElementId);
            }
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Refresh();
        }

        void OnPauseStateChanged(PauseState state)
        {
            Refresh();
        }

        void Refresh()
        {
            MainToolbar.Refresh(k_ElementId);
        }

        void OverrideGUIHandler()
        {
            ModeService.Execute("gui_playbar", EditorApplication.isPlayingOrWillChangePlaymode);
        }

        [VisibleToOtherModules("UnityEditor.PlayModeModule")]
        internal class PlaybarDefaultButtonsAttribute : Attribute { }
    }
}
