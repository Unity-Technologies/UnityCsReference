using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.UIElements
{
    internal class UIDocumentList
    {
        internal List<UIDocument> m_AttachedUIDocuments = new List<UIDocument>();

        internal void RemoveFromListAndFromVisualTree(UIDocument uiDocument)
        {
            m_AttachedUIDocuments.Remove(uiDocument);
            uiDocument.rootVisualElement?.RemoveFromHierarchy();
        }

        internal void AddToListAndToVisualTree(UIDocument uiDocument, VisualElement visualTree, int firstInsertIndex = 0)
        {
            int index = 0;
            foreach (var sibling in m_AttachedUIDocuments)
            {
                if (uiDocument.sortingOrder > sibling.sortingOrder)
                {
                    index++;
                    continue;
                }

                if (uiDocument.sortingOrder < sibling.sortingOrder)
                {
                    break;
                }

                // They're the same value, compare their count (UIDocuments created first show up first).
                if (uiDocument.m_UIDocumentCount > sibling.m_UIDocumentCount)
                {
                    index++;
                    continue;
                }

                break;
            }

            if (index < m_AttachedUIDocuments.Count)
            {
                m_AttachedUIDocuments.Insert(index, uiDocument);

                if (visualTree == null || uiDocument.rootVisualElement == null)
                {
                    return;
                }

                // Not every UIDocument is in the tree already (because their root is null, for example), so we need
                // to figure out the insertion point.
                if (index > 0)
                {
                    VisualElement previousInTree = null;
                    int i = 1;
                    while (previousInTree == null && index - i >= 0)
                    {
                        var previousUIDocument = m_AttachedUIDocuments[index - i++];
                        previousInTree = previousUIDocument.rootVisualElement;
                    }

                    if (previousInTree != null)
                    {
                        index = visualTree.IndexOf(previousInTree) + 1;
                    }
                }

                if (index > visualTree.childCount)
                {
                    index = visualTree.childCount;
                }
            }
            else
            {
                // Add in the end.
                m_AttachedUIDocuments.Add(uiDocument);
            }

            if (visualTree == null || uiDocument.rootVisualElement == null)
            {
                return;
            }

            int insertionIndex = firstInsertIndex + index;
            if (insertionIndex < visualTree.childCount)
            {
                visualTree.Insert(insertionIndex, uiDocument.rootVisualElement);
            }
            else
            {
                visualTree.Add(uiDocument.rootVisualElement);
            }
        }
    }

    /// <summary>
    /// Defines a Component that connects VisualElements to GameObjects. This makes it
    /// possible to render UI defined in UXML documents in the Game view.
    /// </summary>
    [AddComponentMenu("UI Toolkit/UI Document"), ExecuteAlways, DisallowMultipleComponent]
    public sealed class UIDocument : MonoBehaviour
    {
        internal const string k_RootStyleClassName = "unity-ui-document__root";

        internal const string k_VisualElementNameSuffix = "-container";

        private const int k_DefaultSortingOrder = 0;

        // We count instances of UIDocument to be able to insert UIDocuments that have the same sort order in a
        // deterministic way (i.e. instances created before will be placed before in the visual tree).
        private static int s_CurrentUIDocumentCounter = 0;
        internal readonly int m_UIDocumentCount;

        internal static Func<bool> IsEditorPlaying;
        internal static Func<bool> IsEditorPlayingOrWillChangePlaymode;

        [SerializeField]
        private PanelSettings m_PanelSettings;

        // For Reset, we need to always keep track of what our previous PanelSettings was so we can react to being
        // removed from it (as our PanelSettings becomes null in that operation).
        private PanelSettings m_PreviousPanelSettings = null;

        /// <summary>
        /// Specifies the PanelSettings instance to connect this UIDocument component to.
        /// </summary>
        /// <remarks>
        /// The Panel Settings asset defines the panel that renders UI in the game view. See <see cref="PanelSettings"/>.
        ///
        /// If this UIDocument has a parent UIDocument, it uses the parent's PanelSettings automatically.
        /// </remarks>
        public PanelSettings panelSettings
        {
            get
            {
                return m_PanelSettings;
            }
            set
            {
                if (parentUI == null)
                {
                    if (m_PanelSettings == value)
                    {
                        m_PreviousPanelSettings = m_PanelSettings;
                        return;
                    }

                    if (m_PanelSettings != null)
                    {
                        m_PanelSettings.DetachUIDocument(this);
                    }
                    m_PanelSettings = value;

                    if (m_PanelSettings != null)
                    {
                        m_PanelSettings.AttachAndInsertUIDocumentToVisualTree(this);
                    }
                }
                else
                {
                    // Children only hold the same instance as the parent, they don't attach themselves directly.
                    Assert.AreEqual(parentUI.m_PanelSettings, value);
                    m_PanelSettings = parentUI.m_PanelSettings;
                }

                if (m_ChildrenContent != null)
                {
                    // Guarantee changes to panel settings trickles down the hierarchy.
                    foreach (var child in m_ChildrenContent.m_AttachedUIDocuments)
                    {
                        child.panelSettings = m_PanelSettings;
                    }
                }

                m_PreviousPanelSettings = m_PanelSettings;
            }
        }

        /// <summary>
        /// If the GameObject that this UIDocument component is attached to has a parent GameObject, and
        /// that parent GameObject also has a UIDocument component attached to it, this value is set to
        /// the parent GameObject's UIDocument component automatically.
        /// </summary>
        /// <remarks>
        /// If a UIDocument has a parent, you cannot add it directly to a panel. Unity adds it to
        /// the parent's root visual element instead.
        /// </remarks>
        public UIDocument parentUI
        {
            get => m_ParentUI;
            private set => m_ParentUI = value;
        }

        [SerializeField]
        private UIDocument m_ParentUI;


        // If this UIDocument has UIDocument children (1st level only, 2nd level would be the child's
        // children), they're added to this list instead of to the PanelSetting's list.
        private UIDocumentList m_ChildrenContent = null;
        private List<UIDocument> m_ChildrenContentCopy = null;

        [SerializeField]
        private VisualTreeAsset sourceAsset;

        /// <summary>
        /// The <see cref="VisualTreeAsset"/> loaded into the root visual element automatically.
        /// </summary>
        /// <remarks>
        /// If you leave this empty, the root visual element is also empty.
        /// </remarks>
        public VisualTreeAsset visualTreeAsset
        {
            get { return sourceAsset; }
            set
            {
                sourceAsset = value;
                RecreateUI();
            }
        }

        private VisualElement m_RootVisualElement;

        /// <summary>
        /// The root visual element where the UI hierarchy starts.
        /// </summary>
        public VisualElement rootVisualElement
        {
            get { return m_RootVisualElement; }
        }
        private int m_FirstChildInsertIndex;

        internal int firstChildInserIndex
        {
            get => m_FirstChildInsertIndex;
        }

        [SerializeField]
        private float m_SortingOrder = k_DefaultSortingOrder;

        /// <summary>
        /// The order in which this UIDocument will show up on the hierarchy in relation to other UIDocuments either
        /// attached to the same PanelSettings, or with the same UIDocument parent.
        /// </summary>
        public float sortingOrder
        {
            get => m_SortingOrder;
            set
            {
                if (m_SortingOrder == value)
                {
                    return;
                }

                m_SortingOrder = value;
                AddRootVisualElementToTree();
            }
        }

        internal void ApplySortingOrder()
        {
            AddRootVisualElementToTree();
        }

        internal static Func<UIDocument, ILiveReloadAssetTracker<VisualTreeAsset>> CreateLiveReloadVisualTreeAssetTracker;
        private ILiveReloadAssetTracker<VisualTreeAsset> m_LiveReloadVisualTreeAssetTracker;


        // Private constructor so it's not present on the public API file.
        private UIDocument()
        {
            m_UIDocumentCount = s_CurrentUIDocumentCounter++;
        }

        private void Awake()
        {
            if (IsEditorPlayingOrWillChangePlaymode.Invoke() && !IsEditorPlaying.Invoke())
            {
                // We're in a weird transition state that causes an error with the logic below so let's skip it.
                return;
            }

            // By default, the UI Content will try to attach itself to a parent somewhere in the hierarchy.
            // This is done to mimic the behaviour we get from UGUI's Canvas/Game Object relationship.
            SetupFromHierarchy();
        }

        private void OnEnable()
        {
            if (IsEditorPlayingOrWillChangePlaymode.Invoke() && !IsEditorPlaying.Invoke())
            {
                // We're in a weird transition state that causes an error with the logic below so let's skip it.
                return;
            }
            if (parentUI != null && m_PanelSettings == null)
            {
                // Ensures we have the same PanelSettings set as our parent, as the
                // initialization of the parent may have happened after ours.
                m_PanelSettings = parentUI.m_PanelSettings;
            }

            if (m_RootVisualElement == null)
            {
                RecreateUI();
            }
            else
            {
                AddRootVisualElementToTree();
            }
        }

        /// <summary>
        /// Orders UIDocument components based on the way their GameObjects are ordered in the Hierarchy View.
        /// </summary>
        private void SetupFromHierarchy()
        {
            if (parentUI != null)
            {
                parentUI.RemoveChild(this);
            }
            parentUI = FindUIDocumentParent();
        }

        private UIDocument FindUIDocumentParent()
        {
            // Go up looking for a parent UIDocument, which we'd add ourselves too.
            // If that fails, we'll just add ourselves to the runtime panel through the PanelSettings
            // (assuming one is set, otherwise nothing gets drawn so it's pointless to not be
            // parented by another UIDocument OR have a PanelSettings set).
            Transform t = transform;
            Transform parentTransform = t.parent;
            if (parentTransform != null)
            {
                // We need to make sure we can get a parent even if they're disabled/inactive to reflect the good values.
                var potentialParents = parentTransform.GetComponentsInParent<UIDocument>(true);
                if (potentialParents != null && potentialParents.Length > 0)
                {
                    return potentialParents[0];
                }
            }

            return null;
        }

        internal void Reset()
        {
            if (parentUI == null)
            {
                m_PreviousPanelSettings?.DetachUIDocument(this);
                panelSettings = null;
            }

            SetupFromHierarchy();

            if (parentUI != null)
            {
                m_PanelSettings = parentUI.m_PanelSettings;
                AddRootVisualElementToTree();
            }
            else if (m_PanelSettings != null)
            {
                AddRootVisualElementToTree();
            }
            OnValidate();
        }

        private void AddChildAndInsertContentToVisualTree(UIDocument child)
        {
            if (m_ChildrenContent == null)
            {
                m_ChildrenContent = new UIDocumentList();
            }
            else
            {
                // Before adding, we need to make sure it's nowhere else in the list (and in the hierarchy) as if we're
                // re-adding, the position probably changed.
                m_ChildrenContent.RemoveFromListAndFromVisualTree(child);
            }

            m_ChildrenContent.AddToListAndToVisualTree(child, m_RootVisualElement, m_FirstChildInsertIndex);
        }

        private void RemoveChild(UIDocument child)
        {
            m_ChildrenContent?.RemoveFromListAndFromVisualTree(child);
        }

        /// <summary>
        /// Force rebuild the UI from UXML (if one is attached) and of all children (if any).
        /// </summary>
        private void RecreateUI()
        {
            if (m_RootVisualElement != null)
            {
                m_RootVisualElement.RemoveFromHierarchy();
                m_RootVisualElement = null;
            }

            // Even though the root element is of type VisualElement, we use a TemplateContainer internally
            // because we still want to use it as a TemplateContainer.
            if (sourceAsset != null)
            {
                m_RootVisualElement = sourceAsset.Instantiate();

                // This shouldn't happen but if it does we don't fail silently.
                if (m_RootVisualElement == null)
                {
                    Debug.LogError("The UXML file set for the UIDocument could not be cloned.");
                }
            }

            if (m_RootVisualElement == null)
            {
                // Empty container if no UXML is set or if there was an error with cloning the set UXML.
                m_RootVisualElement = new TemplateContainer() { name = gameObject.name + k_VisualElementNameSuffix };
            }
            else
            {
                m_RootVisualElement.name = gameObject.name + k_VisualElementNameSuffix;
            }
            m_RootVisualElement.pickingMode = PickingMode.Ignore;

            // Setting the live reload tracker has to be done prior to attaching to panel in order to work properly
            if (m_LiveReloadVisualTreeAssetTracker == null)
            {
                m_LiveReloadVisualTreeAssetTracker = CreateLiveReloadVisualTreeAssetTracker.Invoke(this);
            }
            m_RootVisualElement.visualTreeAssetTracker = m_LiveReloadVisualTreeAssetTracker;

            if (isActiveAndEnabled)
            {
                AddRootVisualElementToTree();
            }

            // Save the last VisualElement before we start adding children so we can guarantee
            // the order from the game object hierarchy.
            m_FirstChildInsertIndex = m_RootVisualElement.childCount;

            // Finally, we re-add our known children's element.
            // This makes sure the hierarchy of game objects reflects on the order of VisualElements.
            if (m_ChildrenContent != null)
            {
                // We need a copy to iterate because in the process of creating the children UI we modify the list.
                if (m_ChildrenContentCopy == null)
                {
                    m_ChildrenContentCopy = new List<UIDocument>(m_ChildrenContent.m_AttachedUIDocuments);
                }
                else
                {
                    m_ChildrenContentCopy.AddRange(m_ChildrenContent.m_AttachedUIDocuments);
                }

                foreach (var child in m_ChildrenContentCopy)
                {
                    if (child.isActiveAndEnabled)
                    {
                        if (child.m_RootVisualElement == null)
                        {
                            child.RecreateUI();
                        }
                        else
                        {
                            // Since the root is already created, make sure it's inserted into the right position.
                            AddChildAndInsertContentToVisualTree(child);
                        }
                    }
                }

                m_ChildrenContentCopy.Clear();
            }

            SetupRootClassList();
        }

        private void SetupRootClassList()
        {
            // If we're not a child of any other UIDocument stretch to take the full screen.
            m_RootVisualElement?.EnableInClassList(k_RootStyleClassName, parentUI == null);
        }

        private void AddRootVisualElementToTree()
        {
            // If we do have a parent, it will add us.
            if (parentUI != null)
            {
                parentUI.AddChildAndInsertContentToVisualTree(this);
            }
            else if (m_PanelSettings != null)
            {
                m_PanelSettings.AttachAndInsertUIDocumentToVisualTree(this);
            }
        }

        private void OnDisable()
        {
            if (m_RootVisualElement != null)
            {
                m_RootVisualElement.RemoveFromHierarchy();
                // Unhook tracking, we're going down (but only after we detach from the panel).
                m_RootVisualElement.visualTreeAssetTracker = null;
                m_RootVisualElement = null;
            }
        }

        private void OnDestroy()
        {
            if (parentUI != null)
            {
                parentUI.RemoveChild(this);
            }
            else if (m_PanelSettings != null)
            {
                m_PanelSettings.DetachUIDocument(this);
            }
        }

        private void OnTransformChildrenChanged()
        {
            // In Editor, when not playing, we let a watcher listen for hierarchy changed events, except if
            // we're disabled in which case the watcher can't find us.
            if (!IsEditorPlaying.Invoke() && isActiveAndEnabled)
            {
                return;
            }
            if (m_ChildrenContent != null)
            {
                // The list may change inside the call to ReactToHierarchyChanged so we need a copy.
                if (m_ChildrenContentCopy == null)
                {
                    m_ChildrenContentCopy = new List<UIDocument>(m_ChildrenContent.m_AttachedUIDocuments);
                }
                else
                {
                    m_ChildrenContentCopy.AddRange(m_ChildrenContent.m_AttachedUIDocuments);
                }
                foreach (var child in m_ChildrenContentCopy)
                {
                    child.ReactToHierarchyChanged();
                }
                m_ChildrenContentCopy.Clear();
            }
        }

        private void OnTransformParentChanged()
        {
            // In Editor, when not playing, we let a watcher listen for hierarchy changed events, except if
            // we're disabled in which case the watcher can't find us.
            if (!IsEditorPlaying.Invoke() && isActiveAndEnabled)
            {
                return;
            }

            ReactToHierarchyChanged();
        }

        internal void ReactToHierarchyChanged()
        {
            SetupFromHierarchy();

            if (parentUI != null)
            {
                // Using the property guarantees the change trickles down the hierarchy (if there is one).
                panelSettings = parentUI.m_PanelSettings;
            }

            m_RootVisualElement?.RemoveFromHierarchy();
            AddRootVisualElementToTree();

            SetupRootClassList();
        }

        internal void OnLiveReloadOptionChanged()
        {
            // We not only have to recreate ourselves but also our children (and their children).
            ClearChildrenRecursively();

            HandleLiveReload();
        }

        private void ClearChildrenRecursively()
        {
            if (m_ChildrenContent == null)
            {
                return;
            }

            foreach (var child in m_ChildrenContent.m_AttachedUIDocuments)
            {
                if (child.m_RootVisualElement != null)
                {
                    child.m_RootVisualElement.RemoveFromHierarchy();
                    child.m_RootVisualElement = null;
                }

                child.ClearChildrenRecursively();
            }
        }

        internal void HandleLiveReload()
        {
            var disabledCompanions = DisableCompanions();

            RecreateUI();

            if (disabledCompanions != null && disabledCompanions.Count > 0)
            {
                EnableCompanions(disabledCompanions);
            }
            else if (IsEditorPlaying.Invoke())
            {
                Debug.LogWarning("UI was recreated and no companion MonoBehaviour found, some UI functionality may have been lost.");
            }
        }

        private HashSet<MonoBehaviour> DisableCompanions()
        {
            HashSet<MonoBehaviour> disabledCompanions = null;

            var companions = GetComponents<MonoBehaviour>();

            if (companions != null && companions.Length > 1) // If only one is found, it's this UIDocument.
            {
                disabledCompanions = new HashSet<MonoBehaviour>();

                foreach (var companion in companions)
                {
                    if (companion != this && companion.isActiveAndEnabled)
                    {
                        companion.enabled = false;
                        disabledCompanions.Add(companion);
                    }
                }
            }

            return disabledCompanions;
        }

        private void EnableCompanions(HashSet<MonoBehaviour> disabledCompanions)
        {
            foreach (var companion in disabledCompanions)
            {
                companion.enabled = true;
            }
        }

        private VisualTreeAsset m_OldUxml = null;

        private void OnValidate()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (m_OldUxml != sourceAsset)
            {
                visualTreeAsset = sourceAsset;
                m_OldUxml = sourceAsset;
            }

            if (m_PreviousPanelSettings != m_PanelSettings)
            {
                // We'll use the setter as it guarantees the right behavior.
                // It's necessary for the setter that the old value is still in place.
                var tempPanelSettings = m_PanelSettings;
                m_PanelSettings = m_PreviousPanelSettings;
                panelSettings = tempPanelSettings;
            }
        }

    }
}
