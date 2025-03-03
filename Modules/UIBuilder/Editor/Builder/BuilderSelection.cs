// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal enum BuilderSelectionType
    {
        Nothing,
        Element,
        StyleSheet,
        StyleSelector,
        ParentStyleSelector,
        ElementInTemplateInstance,
        ElementInControlInstance,
        VisualTreeAsset,
        ElementInParentDocument
    }

    [Flags]
    internal enum BuilderHierarchyChangeType
    {
        ChildrenAdded = 1 << 0,
        ChildrenRemoved = 1 << 1,
        Attributes = 1 << 2,
        ElementName = 1 << 3,
        ClassList = 1 << 4,
        InlineStyle = 1 << 5,
        FullRefresh = 1 << 6, // Skip this flag if you want to avoid expensive refresh during changes (ex. on drag operations)

        All = ~0
    }

    internal enum BuilderStylingChangeType
    {
        Default,
        RefreshOnly
    }

    internal interface IBuilderSelectionNotifier
    {
        void BeforeSelectionChanged()
        {
        }

        void SelectionChanged();
        void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType);
        void StylingChanged(List<string> styles, BuilderStylingChangeType changeType);
    }

    internal class BuilderSelection
    {
        static readonly StylePropertyReader s_StylePropertyReader = new StylePropertyReader();
        List<IBuilderSelectionNotifier> m_Notifiers;

        List<StylingChangeNotification> m_Notifications;
        Action m_NextPostStylingAction;

        VisualElement m_Root;
        BuilderPaneWindow m_PaneWindow;
        VisualElement m_DocumentRootElement;
        VisualElement m_DummyElementForStyleChangeNotifications;

        struct StylingChangeNotification
        {
            public IBuilderSelectionNotifier notifier { get; set; }
            public List<string> styleList { get; set; }
            public BuilderStylingChangeType changeType { get; set; }
        }

        public BuilderSelectionType selectionType
        {
            get
            {
                if (m_Selection.Count == 0)
                    return BuilderSelectionType.Nothing;

                var selectedElement = m_Selection[0];

                if (BuilderSharedStyles.IsDocumentElement(selectedElement))
                    return BuilderSelectionType.VisualTreeAsset;
                if (BuilderSharedStyles.IsParentSelectorElement(selectedElement))
                    return BuilderSelectionType.ParentStyleSelector;
                if (BuilderSharedStyles.IsSelectorElement(selectedElement))
                    return BuilderSelectionType.StyleSelector;
                if (BuilderSharedStyles.IsStyleSheetElement(selectedElement))
                    return BuilderSelectionType.StyleSheet;
                if (selectedElement.GetVisualElementAsset() == null)
                {
                    if (selectedElement.HasProperty(VisualTreeAsset.LinkedVEAInTemplatePropertyName)
                        && BuilderAssetUtilities.GetVisualElementRootTemplate(selectedElement) != null
                        && !BuilderAssetUtilities.HasDynamicallyCreatedTemplateAncestor(selectedElement))
                    {
                        return BuilderSelectionType.ElementInTemplateInstance;
                    }

                    return BuilderSelectionType.ElementInControlInstance;
                }
                if (selectedElement.IsPartOfActiveVisualTreeAsset(m_PaneWindow.document))
                    return BuilderSelectionType.Element;

                return BuilderSelectionType.ElementInParentDocument;
            }
        }

        List<VisualElement> m_Selection;

        public int selectionCount => m_Selection.Count;

        public IEnumerable<VisualElement> selection => m_Selection;

        public VisualElement documentRootElement
        {
            get { return m_DocumentRootElement; }
            set { m_DocumentRootElement = value; }
        }

        public bool isEmpty { get { return m_Selection.Count == 0; } }

        public BuilderSelection(VisualElement root, BuilderPaneWindow paneWindow)
        {
            m_Notifiers = new List<IBuilderSelectionNotifier>();
            m_Selection = new List<VisualElement>();
            m_Notifications = new List<StylingChangeNotification>();

            m_Root = root;
            m_PaneWindow = paneWindow;

            m_DummyElementForStyleChangeNotifications = new VisualElement();
            m_DummyElementForStyleChangeNotifications.name = "unity-dummy-element-for-style-change-notifications";
            m_DummyElementForStyleChangeNotifications.style.position = Position.Absolute;
            m_DummyElementForStyleChangeNotifications.style.top = -1000;
            m_DummyElementForStyleChangeNotifications.style.left = -1000;
            m_DummyElementForStyleChangeNotifications.style.width = 1;
            m_DummyElementForStyleChangeNotifications.RegisterCallback<GeometryChangedEvent>(AfterPanelUpdaterChange);
            m_Root.Add(m_DummyElementForStyleChangeNotifications);
        }

        internal VisualElement GetFirstSelectedElement()
        {
            foreach (var element in m_Selection)
            {
                return element;
            }

            return null;
        }

        public void AssignNotifiers(IEnumerable<IBuilderSelectionNotifier> notifiers)
        {
            m_Notifiers.Clear();
            foreach (var notifier in notifiers)
                m_Notifiers.Add(notifier);
        }

        public void AddNotifier(IBuilderSelectionNotifier notifier)
        {
            if (!m_Notifiers.Contains(notifier))
                m_Notifiers.Add(notifier);
        }

        public void RemoveNotifier(IBuilderSelectionNotifier notifier)
        {
            m_Notifiers.Remove(notifier);
        }

        public void ForceReselection(IBuilderSelectionNotifier source = null)
        {
            NotifyOfSelectionChange(source);
        }

        public void Select(IBuilderSelectionNotifier source, VisualElement ve)
        {
            if (ve == null)
                return;

            NotifyOfBeforeSelectionChange(source);

            foreach (var sel in m_Selection)
            {
                if (sel == null)
                    continue;

                BuilderAssetUtilities.RemoveElementFromSelectionInAsset(m_PaneWindow.document, sel);
            }

            m_Selection.Clear();

            m_Selection.Add(ve);
            BuilderAssetUtilities.AddElementToSelectionInAsset(m_PaneWindow.document, ve);

            NotifyOfSelectionChange(source);
        }

        public void AddToSelection(IBuilderSelectionNotifier source, VisualElement ve)
        {
            AddToSelection(source, ve, true, true);
        }

        void AddToSelection(IBuilderSelectionNotifier source, VisualElement ve, bool undo, bool sort)
        {
            if (ve == null)
                return;

            NotifyOfBeforeSelectionChange(source);

            m_Selection.Add(ve);

            if (sort)
                SortSelection();

            if (undo)
                BuilderAssetUtilities.AddElementToSelectionInAsset(m_PaneWindow.document, ve);

            NotifyOfSelectionChange(source);
        }

        public void RemoveFromSelection(IBuilderSelectionNotifier source, VisualElement ve)
        {
            NotifyOfBeforeSelectionChange(source);

            m_Selection.Remove(ve);
            BuilderAssetUtilities.RemoveElementFromSelectionInAsset(m_PaneWindow.document, ve);

            NotifyOfSelectionChange(source);
        }

        public void ClearSelection(IBuilderSelectionNotifier source, bool undo = true)
        {
            if (isEmpty)
                return;

            NotifyOfBeforeSelectionChange(source);

            if (undo)
                foreach (var sel in m_Selection)
                    BuilderAssetUtilities.RemoveElementFromSelectionInAsset(m_PaneWindow.document, sel);

            m_Selection.Clear();

            NotifyOfSelectionChange(source);
        }

        public void RestoreSelectionFromDocument(VisualElement sharedStylesAndDocumentElement)
        {
            ClearSelection(null, false);

            var selectedElements = sharedStylesAndDocumentElement.FindSelectedElements();
            foreach (var selectedElement in selectedElements)
                AddToSelection(null, selectedElement, false, false);

            SortSelection();
        }

        public void NotifyOfHierarchyChange(
            IBuilderSelectionNotifier source = null,
            VisualElement element = null,
            BuilderHierarchyChangeType changeType = BuilderHierarchyChangeType.All)
        {
            ForceVisualAssetUpdateWithoutSave(element, changeType);

            // This is so anyone interested can refresh their use of this UXML with
            // the latest (unsaved to disk) changes.
            EditorUtility.SetDirty(m_PaneWindow.document.visualTreeAsset);

            foreach (var notifier in m_Notifiers)
                if (notifier != source)
                    notifier.HierarchyChanged(element, changeType);

            if (hasUnsavedChanges && !isAnonymousDocument)
            {
                var liveReloadChanges = ((changeType & BuilderHierarchyChangeType.InlineStyle) != 0
                                            ? BuilderAssetUtilities.LiveReloadChanges.Styles
                                            : 0) |
                                        ((changeType & ~BuilderHierarchyChangeType.InlineStyle) != 0
                                            ? BuilderAssetUtilities.LiveReloadChanges.Hierarchy
                                            : 0);
                BuilderAssetUtilities.LiveReload(liveReloadChanges);
            }
        }

        internal void ForceVisualAssetUpdateWithoutSave(
            VisualElement element = null,
            BuilderHierarchyChangeType changeType = BuilderHierarchyChangeType.All)
        {
            if (m_Notifiers == null || m_Notifiers.Count == 0)
                return;

            VisualElementAsset vea = element?.GetVisualElementAsset();
            if (vea != null && vea.ruleIndex >= 0 && changeType.HasFlag(BuilderHierarchyChangeType.InlineStyle))
            {
                var vta = m_PaneWindow.document.visualTreeAsset;
                var rule = vta.GetOrCreateInlineStyleRule(vea);

                element.UpdateInlineRule(vta.inlineSheet, rule);

                // Need to enforce this specific style is updated.
                element.IncrementVersion(VersionChangeType.Opacity | VersionChangeType.Overflow | VersionChangeType.StyleSheet);
            }
            else if (m_DocumentRootElement != null)
            {
                m_PaneWindow.document.RefreshStyle(m_DocumentRootElement);
            }
        }

        public void NotifyOfStylingChange(IBuilderSelectionNotifier source = null, List<string> styles = null, BuilderStylingChangeType changeType = BuilderStylingChangeType.Default)
        {
            if (m_Notifiers == null || m_Notifiers.Count == 0)
                return;

            if (m_DocumentRootElement != null)
                m_PaneWindow.document.RefreshStyle(m_DocumentRootElement);

            // If there's already a notification with styles and the same changeType, merge the new styles into it.
            foreach (var notification in m_Notifications)
            {
                if (notification.changeType == changeType && notification.styleList != null && styles != null)
                {
                    notification.styleList.AddRange(styles);
                    return;
                }
            }

            m_Notifications.Add(new StylingChangeNotification
            {
                notifier = source,
                styleList = styles,
                changeType = changeType
            });

            QueueUpPostPanelUpdaterChangeAction(NotifyOfStylingChangePostStylingUpdate);
        }

        void NotifyOfBeforeSelectionChange(IBuilderSelectionNotifier source)
        {
            if (m_Notifiers == null || m_Notifiers.Count == 0)
                return;

            foreach (var notifier in m_Notifiers)
                if (notifier != source)
                    notifier.BeforeSelectionChanged();
        }

        void NotifyOfSelectionChange(IBuilderSelectionNotifier source)
        {
            if (m_Notifiers == null || m_Notifiers.Count == 0)
                return;

            if (m_DocumentRootElement != null)
                m_PaneWindow.document.RefreshStyle(m_DocumentRootElement);

            foreach (var notifier in m_Notifiers)
                if (notifier != source)
                    notifier.SelectionChanged();
        }

        void NotifyOfStylingChangePostStylingUpdate()
        {
            // This is so anyone interested can refresh their use of this USS with
            // the latest (unsaved to disk) changes.
            //RetainedMode.FlagStyleSheetChange(); // Works but TOO SLOW.
            m_PaneWindow.document.MarkStyleSheetsDirty();

            // Order notifications from least to most specific.

            m_Notifications.Sort((left, right) =>
            {
                // Prioritize Default changeType over RefreshOnly
                if (left.changeType == BuilderStylingChangeType.Default && right.changeType != BuilderStylingChangeType.Default)
                    return -1;
                if (left.changeType != BuilderStylingChangeType.Default && right.changeType == BuilderStylingChangeType.Default)
                    return 1;

                // If both have the same changeType, sort by styleList length
                var leftStyleCount = left.styleList?.Count ?? 0;
                var rightStyleCount = right.styleList?.Count ?? 0;

                if (leftStyleCount < rightStyleCount)
                    return -1;
                if (leftStyleCount > rightStyleCount)
                    return 1;

                // If all else is equal, keep the order stable
                return 0;
            });

            var notifiedNotifiers = new HashSet<IBuilderSelectionNotifier>();
            foreach (var notification in m_Notifications)
            {
                foreach (var notifier in m_Notifiers)
                {
                    if (!notifiedNotifiers.Contains(notifier) && notifier != notification.notifier)
                    {
                        notifier.StylingChanged(notification.styleList, notification.changeType);
                        notifiedNotifiers.Add(notifier); // Mark as notified to avoid duplicate notifications
                    }
                }
            }

            m_Notifications.Clear();

            if (hasUnsavedChanges && !isAnonymousDocument)
            {
                BuilderAssetUtilities.LiveReload(BuilderAssetUtilities.LiveReloadChanges.Styles);
            }
        }

        void QueueUpPostPanelUpdaterChangeAction(Action action)
        {
            m_NextPostStylingAction = action;
            if (m_DummyElementForStyleChangeNotifications.resolvedStyle.width > 0)
                m_DummyElementForStyleChangeNotifications.style.width = -1;
            else
                m_DummyElementForStyleChangeNotifications.style.width = 1;
        }

        void AfterPanelUpdaterChange(GeometryChangedEvent evt)
        {
            if (m_NextPostStylingAction == null)
                return;

            m_NextPostStylingAction();

            m_NextPostStylingAction = null;
        }

        int GetSelectedItemOrder(VisualElement element)
        {
            var vea = element.GetVisualElementAsset();
            if (vea != null)
                return vea.orderInDocument;

            var selector = element.GetStyleComplexSelector();
            if (selector != null)
            {
                var styleSheetElement = element.parent;
                var styleSheetIndex = styleSheetElement.parent.IndexOf(styleSheetElement);
                var elementIndex = styleSheetElement.IndexOf(element);

                return (styleSheetIndex * 10000) + elementIndex;
            }

            return 0;
        }

        void SortSelection()
        {
            if (m_Selection.Count <= 1)
                return;

            m_Selection.Sort((left, right) =>
            {
                var leftOrder = GetSelectedItemOrder(left);
                var rightOrder = GetSelectedItemOrder(right);
                return leftOrder.CompareTo(rightOrder);
            });
        }

        public bool hasUnsavedChanges
        {
            get { return m_PaneWindow.document.hasUnsavedChanges; }
            private set { m_PaneWindow.document.hasUnsavedChanges = value; }
        }

        public void ResetUnsavedChanges()
        {
            hasUnsavedChanges = false;
        }

        private bool isAnonymousDocument
        {
            get { return m_PaneWindow.document.activeOpenUXMLFile.isAnonymousDocument; }
        }
    }
}
