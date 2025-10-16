// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The view to show the available <see cref="OnboardingProviders"/>. Displayed in a new window, when no graph is selected.
    /// </summary>
    [UnityRestricted]
    internal class BlankPage : VisualElement
    {
        /// <summary>
        /// The USS class name added to the <see cref="BlankPage"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-blank-page";

        ICommandTarget m_CommandTarget;
        GraphObjectDragAndDropHandler m_GraphObjectDragAndDropHandler;
        List<Type> m_DroppableAssetTypes;

        public IReadOnlyList<OnboardingProvider> OnboardingProviders { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlankPage"/> class.
        /// </summary>
        /// <param name="commandTarget">The command dispatcher.</param>
        /// <param name="onboardingProviders">The list of <see cref="OnboardingProviders"/> to display.</param>
        public BlankPage(ICommandTarget commandTarget, IReadOnlyList<OnboardingProvider> onboardingProviders)
        {
            m_CommandTarget = commandTarget;
            OnboardingProviders = onboardingProviders;

            m_DroppableAssetTypes = new List<Type>();
            foreach (var provider in OnboardingProviders)
                m_DroppableAssetTypes.AddRange(provider.GetAcceptedGraphObjectTypes());

            RegisterCallback<DragEnterEvent>(OnDragEnter);
            RegisterCallback<DragLeaveEvent>(OnDragLeave);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragExitedEvent>(OnDragExited);
        }

        public virtual void CreateUI()
        {
            Clear();

            AddToClassList(ussClassName);

            if (m_CommandTarget != null)
            {
                foreach (var provider in OnboardingProviders)
                {
                    Add(provider.CreateInitialOnboardingElements(m_CommandTarget));
                }
            }
        }

        void OnDragEnter(DragEnterEvent e)
        {
            m_GraphObjectDragAndDropHandler ??= new GraphObjectDragAndDropHandler(m_CommandTarget, m_DroppableAssetTypes);

            if (m_GraphObjectDragAndDropHandler?.CanHandleDrop() ?? false)
                m_GraphObjectDragAndDropHandler.OnDragEnter(e);
        }

        void OnDragLeave(DragLeaveEvent e)
        {
            m_GraphObjectDragAndDropHandler?.OnDragLeave(e);
        }

        void OnDragUpdated(DragUpdatedEvent e)
        {
            if (m_GraphObjectDragAndDropHandler != null)
                m_GraphObjectDragAndDropHandler.OnDragUpdated(e);
            else
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
        }

        void OnDragPerform(DragPerformEvent e)
        {
            m_GraphObjectDragAndDropHandler?.OnDragPerform(e);
            m_GraphObjectDragAndDropHandler = null;
        }

        void OnDragExited(DragExitedEvent e)
        {
            m_GraphObjectDragAndDropHandler?.OnDragExited(e);
            m_GraphObjectDragAndDropHandler = null;
        }

        internal static void GetExtensionsForAssetTypes(IReadOnlyList<Type> assetTypes, HashSet<string> extensions)
        {
            foreach (var assetType in assetTypes)
            {
                var extensionsForAssetType = GraphObjectFactory.GetExtensionsForAssetType(assetType);
                extensions.UnionWith(extensionsForAssetType);
            }
        }
    }
}
