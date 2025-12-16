// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    partial class GraphViewEditorWindow
    {
        const string k_UnityThemeEnvVariablesClassName = "unity-theme-env-variables";
        readonly Dictionary<string, ToolbarDefinition> m_ToolbarDefinitions = new();

        internal bool TryGetOverlayWrapper(Type viewType, out GTKOverlayWrapper overlayWrapper)
        {
            return m_OverlayWrappers.TryGetValue(viewType, out overlayWrapper);
        }

        GTKOverlayWrapper<T> GetOrCreateOverlayWrapper<T>() where T : RootView
        {
            if (!m_OverlayWrappers.TryGetValue(typeof(T), out var overlayWrapper))
            {
                overlayWrapper = new GTKOverlayWrapper<T>();
                m_OverlayWrappers[typeof(T)] = overlayWrapper;
            }

            return overlayWrapper as GTKOverlayWrapper<T>;
        }

        internal GTKOverlayWrapper<BlackboardView> CreateAndSetupBlackboardView()
        {
            var blackboardWrapper = GetOrCreateOverlayWrapper<BlackboardView>();

            if (blackboardWrapper is { RootView: Editor.BlackboardView } || GraphTool == null)
            {
                return blackboardWrapper;
            }

            var blackboardView = CreateBlackboardView();
            if (blackboardView != null)
            {
                if (blackboardWrapper!= null)
                    blackboardWrapper.RootView = blackboardView;

                blackboardView.Initialize();
                blackboardView.AddToClassList(k_UnityThemeEnvVariablesClassName);
                blackboardView.RegisterCallback<TooltipEvent>((e) => e.StopPropagation());
                RegisterView(blackboardView);
            }

            return blackboardWrapper;
        }

        internal GTKOverlayWrapper CreateAndSetupMiniMapView()
        {
            GTKOverlayWrapper miniMapWrapper = GetOrCreateOverlayWrapper<MiniMapView>();

            if (miniMapWrapper is { RootView: MiniMapView } || GraphTool == null)
            {
                return miniMapWrapper;
            }

            var miniMapView = CreateMiniMapView();
            if (miniMapView != null)
            {
                if (miniMapWrapper != null)
                    miniMapWrapper.RootView = miniMapView;

                miniMapView.Initialize();
                miniMapView.AddToClassList(k_UnityThemeEnvVariablesClassName);
                miniMapView.RegisterCallback<TooltipEvent>((e) => e.StopPropagation());
                RegisterView(miniMapView);
            }

            return miniMapWrapper;
        }

        internal GTKOverlayWrapper CreateAndSetupInspectorView()
        {
            GTKOverlayWrapper wrapper = GetOrCreateOverlayWrapper<ModelInspectorView>();

            if (wrapper is { RootView: ModelInspectorView } || GraphTool == null)
                return wrapper;

            var viewModel = CreateModelInspectorViewModel();
            if (viewModel == null)
                return wrapper;

            ModelInspectorView modelInspectorView = CreateModelInspectorView(viewModel);
            if (modelInspectorView != null)
            {
                if (wrapper != null)
                    wrapper.RootView = modelInspectorView;

                modelInspectorView.Initialize();
                modelInspectorView.AddToClassList(k_UnityThemeEnvVariablesClassName);
                modelInspectorView.RegisterCallback<TooltipEvent>((e) => e.StopPropagation());
                RegisterView(modelInspectorView);
            }

            return wrapper;
        }

        void CreateOverlayContents()
        {
            CreateAndSetupBlackboardView();
            CreateAndSetupInspectorView();
            CreateAndSetupMiniMapView();
        }

        /// <summary>
        /// Creates the toolbar definition for a toolbar.
        /// </summary>
        /// <param name="toolbarId">The toolbar id.</param>
        /// <returns>A toolbar definition object.</returns>
        protected virtual ToolbarDefinition CreateToolbarDefinition(string toolbarId)
        {
            switch (toolbarId)
            {
                case MainToolbar.toolbarId:
                    return new MainToolbarDefinition();
                case PanelsToolbar.toolbarId:
                    return new PanelsToolbarDefinition();
                case ErrorToolbar.toolbarId:
                    return new ErrorToolbarDefinition();
                case OptionsMenuToolbar.toolbarId:
                    return new OptionsToolbarDefinition();
                case BreadcrumbsToolbar.toolbarId:
                    return new BreadcrumbsToolbarDefinition();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the toolbar definition for a toolbar.
        /// </summary>
        /// <param name="toolbar">The toolbar for which to get the definition.</param>
        /// <returns>The toolbar definition for the toolbar.</returns>
        internal ToolbarDefinition GetToolbarDefinition(Toolbar toolbar)
        {
            if (!m_ToolbarDefinitions.TryGetValue(toolbar.id, out var toolbarDefinition))
            {
                toolbarDefinition = CreateToolbarDefinition(toolbar.id);
                if (toolbarDefinition is null)
                    toolbar.IsEnabled = false;
                else
                    m_ToolbarDefinitions[toolbar.id] = toolbarDefinition;
            }

            return toolbarDefinition;
        }
    }
}
