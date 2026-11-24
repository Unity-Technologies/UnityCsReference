// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// Visual element that wraps a block with consistent UI chrome. The static structure is defined in
    /// Editor Default Resources/ImportBlocks/BlockItemElement.uxml; this class clones that template and
    /// wires the behavior (foldout, enable/remove/menu actions, bindings, read-only state, selection).
    /// </summary>
    internal class BlockItemElement : VisualElement
    {
        SerializedProperty m_BlockProperty;
        SerializedProperty m_BlocksArrayProperty;
        int m_Index;
        Type m_RequiredInterface;
        ListView m_ListView;
        SerializedObject m_SerializedObject;
        VisualElement m_ContentContainer;
        Toggle m_EnableToggle;
        Button m_MenuButton;
        Button m_RemoveButton;
        bool m_ReadOnly;
        Label m_NameLabel;
        Action<IEnumerable<object>> m_SelectionChangedHandler;

        public BlockItemElement(SerializedProperty blockProperty, SerializedProperty blocksArrayProperty, int index,
            Type requiredInterface = null, ListView listView = null, SerializedObject serializedObject = null,
            bool readOnly = false)
        {
            m_BlockProperty = blockProperty;
            m_BlocksArrayProperty = blocksArrayProperty;
            m_Index = index;
            m_RequiredInterface = requiredInterface;
            m_ListView = listView;
            m_SerializedObject = serializedObject;
            m_ReadOnly = readOnly;

            AddToClassList("block-item");

            style.backgroundColor = StyleKeyword.Null;

            if (!m_ReadOnly)
                this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));

            // Structure comes from BlockItemElement.uxml; clone it into this element, then wire behavior
            // onto the named children below.
            UiHelpers.GetBlockItemTemplate()?.CloneTree(this);

            var container = this.Q<VisualElement>("block-item-container");
            var headerBar = this.Q<VisualElement>("block-item-header");
            var foldoutToggle = this.Q<Toggle>("foldout-toggle");
            var blockIcon = this.Q<VisualElement>("block-icon");
            m_EnableToggle = this.Q<Toggle>("enable-toggle");
            m_NameLabel = this.Q<Label>("name-label");
            m_RemoveButton = this.Q<Button>("remove-button");
            m_MenuButton = this.Q<Button>("menu-button");
            m_ContentContainer = this.Q<VisualElement>("content-container");

            foldoutToggle.RegisterValueChangedCallback(evt =>
            {
                if (m_ContentContainer != null)
                    m_ContentContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
            foldoutToggle.value = false;

            var blockTypeName = blockProperty.managedReferenceFullTypename;
            if (!string.IsNullOrEmpty(blockTypeName))
                foldoutToggle.viewDataKey = $"block-foldout-{blockTypeName}";

            if (m_ReadOnly)
            {
                foldoutToggle.SetEnabled(true);
                foldoutToggle.focusable = true;
                headerBar.pickingMode = PickingMode.Position;
            }

            var blockIconTexture = UiHelpers.GetBlockIcon();
            if (blockIconTexture != null)
                blockIcon.style.backgroundImage = new StyleBackground(blockIconTexture);

            var enabledProperty = blockProperty.FindPropertyRelative("m_Enabled");
            if (enabledProperty != null)
            {
                m_EnableToggle.BindProperty(enabledProperty);
                UpdateDisabledVisualState(enabledProperty.boolValue);
                m_EnableToggle.RegisterValueChangedCallback(evt => UpdateDisabledVisualState(evt.newValue));
            }
            if (m_ReadOnly)
                m_EnableToggle.SetEnabled(false);

            m_NameLabel.text = GetBlockName();
            if (m_ReadOnly)
                m_NameLabel.AddToClassList("block-item-name-label--readonly");

            m_RemoveButton.clicked += DeleteBlock;
            if (m_ReadOnly)
                m_RemoveButton.style.display = DisplayStyle.None;

            // IconContent always returns the dark skin's "pane options", so the light one is loaded by path.
            var menuIcon = EditorGUIUtility.isProSkin
                ? EditorGUIUtility.IconContent("pane options").image
                : EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/pane options.png") as Texture;
            if (menuIcon != null)
                m_MenuButton.style.backgroundImage = new StyleBackground((Texture2D)menuIcon);
            m_MenuButton.clicked += OnMenuButtonClicked;
            if (m_ReadOnly)
                m_MenuButton.style.display = DisplayStyle.None;

            if (m_ReadOnly)
            {
                container.SetEnabled(true);
                container.pickingMode = PickingMode.Position;
            }

            // Content starts collapsed; the foldout callback drives its visibility.
            m_ContentContainer.style.display = DisplayStyle.None;

            // Make the header clickable to toggle the foldout and select the row.
            m_NameLabel.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    foldoutToggle.value = !foldoutToggle.value;
                    if (m_ListView != null)
                        m_ListView.selectedIndex = m_Index;
                    evt.StopPropagation();
                }
            });

            foldoutToggle.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button == 0 && m_ListView != null)
                    m_ListView.selectedIndex = m_Index;
            });

            if (m_ReadOnly)
            {
                headerBar.RegisterCallback<PointerUpEvent>(evt =>
                {
                    if (evt.button == 0 &&
                        (evt.target == headerBar || evt.target == blockIcon || evt.target == m_NameLabel))
                    {
                        foldoutToggle.value = !foldoutToggle.value;
                        evt.StopPropagation();
                    }
                });
            }

            if (!m_ReadOnly)
            {
                RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Delete && m_ListView != null && m_ListView.selectedIndex == m_Index)
                    {
                        DeleteBlock();
                        evt.StopPropagation();
                    }
                });
            }

            m_SelectionChangedHandler = _ => UpdateSelectionState();

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                UpdateSelectionState();
                if (m_ListView != null)
                    m_ListView.selectionChanged += m_SelectionChangedHandler;
            });

            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                if (m_ListView != null)
                    m_ListView.selectionChanged -= m_SelectionChangedHandler;
            });

            if (m_ReadOnly)
            {
                SetEnabled(true);
                pickingMode = PickingMode.Position;
            }
        }

        void UpdateSelectionState()
        {
            var itemParent = parent;
            if (itemParent == null) return;
            itemParent.style.backgroundColor = StyleKeyword.Null;
            bool isSelected = itemParent.ClassListContains("unity-collection-view__item--selected") ||
                              itemParent.ClassListContains("unity-list-view__item--selected");
            if (isSelected)
                m_NameLabel.AddToClassList("block-item-name-label--selected");
            else
                m_NameLabel.RemoveFromClassList("block-item-name-label--selected");
        }

        /// <summary>
        /// Sets the content UI for this block item
        /// </summary>
        public void SetContent(VisualElement content)
        {
            if (m_ContentContainer == null) return;

            m_ContentContainer.Clear();

            if (content != null)
            {
                m_ContentContainer.Add(content);
                // Read-only governs the block's structural chrome (add/remove/reorder/menu/enable), not
                // its fields: the reference editor renders inherited leaf fields as editable so editing one
                // is captured as a per-reference override. Safe only because the override system (this
                // branch) is what captures the edit.
            }
        }

        /// <summary>Lets a block's own drawer supply a shorter header than <see cref="IBlock.Name"/>.</summary>
        internal void OverrideHeaderLabel(string text)
        {
            m_NameLabel.text = text;
        }

        string GetBlockName()
        {
            if (m_BlockProperty != null && m_BlockProperty.managedReferenceValue is IBlock block)
            {
                return block.Name;
            }

            return m_BlockProperty?.displayName ?? "Block";
        }


        void UpdateDisabledVisualState(bool isEnabled)
        {
            if (isEnabled)
            {
                RemoveFromClassList("block-item-disabled");
            }
            else
            {
                AddToClassList("block-item-disabled");
            }
        }

        void OnMenuButtonClicked()
        {
            var triggerEvent = PointerDownEvent.GetPooled();
            var dropdownMenu = new DropdownMenu();
            var evt = ContextualMenuPopulateEvent.GetPooled(triggerEvent, dropdownMenu, this, null);

            BuildContextMenu(evt);

            var menu = new UnityEditor.GenericMenu();
            foreach (var item in evt.menu.MenuItems())
            {
                var action = item as DropdownMenuAction;
                if (action != null)
                {
                    if (action.status == DropdownMenuAction.Status.Disabled)
                    {
                        menu.AddDisabledItem(new UnityEngine.GUIContent(action.name));
                    }
                    else
                    {
                        menu.AddItem(new UnityEngine.GUIContent(action.name), false, () => action.Execute());
                    }
                }
                else if (item is DropdownMenuSeparator)
                {
                    menu.AddSeparator("");
                }
            }

            menu.DropDown(m_MenuButton.worldBound);
            evt.Dispose();
            triggerEvent.Dispose();
        }

        void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendSeparator("");

            var mousePosition = evt.mousePosition;

            evt.menu.AppendAction("Add block", (a) => AddBlock(mousePosition), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Remove block", (a) => DeleteBlock(), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Duplicate block", (a) => DuplicateBlock(), DropdownMenuAction.AlwaysEnabled);

            evt.menu.AppendSeparator("");

            evt.menu.AppendAction("Copy block", (a) => CopyBlock(), DropdownMenuAction.AlwaysEnabled);

            if (BlockClipboard.HasBlockInClipboard())
            {
                evt.menu.AppendAction("Paste block", (a) => PasteBlock(), DropdownMenuAction.AlwaysEnabled);
            }
            else
            {
                evt.menu.AppendAction("Paste block", (a) => { }, DropdownMenuAction.Status.Disabled);
            }

            var currentBlockType = m_BlockProperty?.managedReferenceValue?.GetType();
            if (currentBlockType != null && BlockClipboard.HasBlockOfType(currentBlockType))
            {
                evt.menu.AppendAction("Paste block values", (a) => PasteBlockValues(),
                    DropdownMenuAction.AlwaysEnabled);
            }
            else
            {
                evt.menu.AppendAction("Paste block values", (a) => { }, DropdownMenuAction.Status.Disabled);
            }

            evt.menu.AppendSeparator("");

            evt.menu.AppendAction("Copy all blocks", (a) => CopyAllBlocks(),
                (a) => m_BlocksArrayProperty.arraySize > 0
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);

            if (BlockClipboard.HasCollectionInClipboard())
            {
                evt.menu.AppendAction("Paste blocks (replace)", (a) => PasteBlocksReplace(),
                    DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Paste blocks (append)", (a) => PasteBlocksAppend(),
                    DropdownMenuAction.AlwaysEnabled);
            }
            else
            {
                evt.menu.AppendAction("Paste blocks (replace)", (a) => { }, DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Paste blocks (append)", (a) => { }, DropdownMenuAction.Status.Disabled);
            }
        }

        void CopyBlock()
        {
            BlockClipboard.CopyBlock(m_BlockProperty);
        }

        void PasteBlock()
        {
            // Only move the selection when the paste actually inserted a block.
            if (BlockClipboard.PasteBlock(m_BlocksArrayProperty, m_Index + 1, m_RequiredInterface)
                && m_ListView != null)
            {
                m_ListView.selectedIndex = m_Index + 1;
            }
        }

        void PasteBlockValues()
        {
            BlockClipboard.PasteBlockValues(m_BlockProperty);
        }

        void AddBlock(Vector2 mousePosition)
        {
            if (m_ListView != null && m_SerializedObject != null)
            {
                UiHelpers.ShowAddMenu(m_ListView, m_SerializedObject, m_RequiredInterface, mousePosition, m_Index + 1);
            }
        }

        void DuplicateBlock()
        {
            // Select the newly duplicated block (inserted at index + 1) only if duplication succeeded.
            if (BlockClipboard.DuplicateBlock(m_BlocksArrayProperty, m_Index, m_RequiredInterface)
                && m_ListView != null)
            {
                m_ListView.selectedIndex = m_Index + 1;
            }
        }

        void DeleteBlock()
        {
            if (m_BlocksArrayProperty != null && m_Index >= 0 && m_Index < m_BlocksArrayProperty.arraySize)
            {
                m_BlocksArrayProperty.DeleteArrayElementAtIndex(m_Index);
                m_BlocksArrayProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        void CopyAllBlocks()
        {
            BlockClipboard.CopyCollection(m_BlocksArrayProperty);
        }

        void PasteBlocksReplace()
        {
            BlockClipboard.PasteCollection(m_BlocksArrayProperty, append: false, m_RequiredInterface);
        }

        void PasteBlocksAppend()
        {
            BlockClipboard.PasteCollection(m_BlocksArrayProperty, append: true, m_RequiredInterface);
        }
    }
}
