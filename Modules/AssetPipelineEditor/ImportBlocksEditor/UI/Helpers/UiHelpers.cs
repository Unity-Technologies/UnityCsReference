// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    internal class ListViewBindingContext
    {
        public string AssetPath { get; set; }
        public SerializedObject SerializedObject { get; set; }
    }

    internal static class BlockListHelper
    {
        public static void AddItemToList(ListView listView, SerializedObject target, Type blockType,
            int? insertIndex = null)
        {
            var operation = (IBlock)Activator.CreateInstance(blockType);

            // Need to get the parent property (BlockCollection) first
            var collectionPath = listView.bindingPath.Substring(0, listView.bindingPath.Length - ".m_Blocks".Length);
            var collectionProperty = target.FindProperty(collectionPath);

            if (collectionProperty != null)
            {
                var blocksProperty = collectionProperty.FindPropertyRelative("m_Blocks");
                if (blocksProperty != null)
                {
                    target.Update();
                    // Use provided index or append to end
                    int indexToInsert = insertIndex ?? blocksProperty.arraySize;

                    blocksProperty.InsertArrayElementAtIndex(indexToInsert);
                    var blockProperty = blocksProperty.GetArrayElementAtIndex(indexToInsert);
                    blockProperty.managedReferenceValue = operation;
                    target.ApplyModifiedProperties();

                    listView.selectedIndex = indexToInsert;
                }
            }
        }
    }

    internal static partial class UiHelpers
    {
        const string k_BlockItemStyleSheetPath = "ImportBlocks/BlockItemElement.uss";
        const string k_BlockIconPath = "ImportBlocks/ImportBlockIcon.png";
        const string k_BlockItemTemplatePath = "ImportBlocks/BlockItemElement.uxml";
        const string k_BlockSelectionWindowTemplatePath = "ImportBlocks/BlockSelectionWindow.uxml";
        const string k_PipelineInspectorStyleSheetPath = "ImportBlocks/PipelineInspector.uss";
        const string k_PostprocessorElementTemplatePath = "ImportBlocks/PostprocessorElement.uxml";
        const string k_ExecutionViewTemplatePath = "ImportBlocks/ExecutionView.uxml";
        const string k_ExecutionRowTemplatePath = "ImportBlocks/ExecutionRow.uxml";

        // Shared with AssetImporterEditor, which sets or queries these to compose the pipeline inspector.
        internal const string k_AddButtonClassName = "block-add-button";
        internal const string k_MissingTypesHelpBoxName = "missing-types-helpbox";
        internal const string k_ExpandFoldoutClassName = "expand-block-collection-foldout";

        [AutoStaticsCleanupOnCodeReload]
        static StyleSheet s_CachedStyleSheet;
        [AutoStaticsCleanupOnCodeReload]
        [IgnoreForUAL0015("Template asset memo, reloaded from Editor Default Resources on the next miss")]
        static VisualTreeAsset s_CachedBlockItemTemplate;
        [AutoStaticsCleanupOnCodeReload]
        static VisualTreeAsset s_CachedBlockSelectionWindowTemplate;
        [AutoStaticsCleanupOnCodeReload]
        static StyleSheet s_CachedPipelineInspectorStyleSheet;
        [AutoStaticsCleanupOnCodeReload]
        static VisualTreeAsset s_CachedPostprocessorElementTemplate;
        [AutoStaticsCleanupOnCodeReload]
        static VisualTreeAsset s_CachedExecutionViewTemplate;
        [AutoStaticsCleanupOnCodeReload]
        static VisualTreeAsset s_CachedExecutionRowTemplate;
        [AutoStaticsCleanupOnCodeReload]
        static Dictionary<(Type, Type), string> s_InterfaceTooltipCache = new Dictionary<(Type, Type), string>();

        // Cache for MonoScript lookups to avoid repeated AssetDatabase searches
        [AutoStaticsCleanupOnCodeReload]
        static Dictionary<Type, MonoScript> s_ScriptCache = new Dictionary<Type, MonoScript>();

        internal static StyleSheet GetBlockItemStyleSheet()
        {
            if (s_CachedStyleSheet == null)
                s_CachedStyleSheet = EditorGUIUtility.Load(k_BlockItemStyleSheetPath) as StyleSheet;
            return s_CachedStyleSheet;
        }

        // LoadIcon prefixes the file name with d_ on the dark skin, so this also resolves d_ImportBlockIcon.png.
        internal static Texture2D GetBlockIcon() => EditorGUIUtility.LoadIcon(k_BlockIconPath);

        internal static VisualTreeAsset GetBlockItemTemplate()
        {
            if (s_CachedBlockItemTemplate == null)
                s_CachedBlockItemTemplate = EditorGUIUtility.Load(k_BlockItemTemplatePath) as VisualTreeAsset;
            return s_CachedBlockItemTemplate;
        }

        internal static VisualTreeAsset GetBlockSelectionWindowTemplate()
        {
            if (s_CachedBlockSelectionWindowTemplate == null)
                s_CachedBlockSelectionWindowTemplate =
                    EditorGUIUtility.Load(k_BlockSelectionWindowTemplatePath) as VisualTreeAsset;
            return s_CachedBlockSelectionWindowTemplate;
        }

        internal static StyleSheet GetPipelineInspectorStyleSheet()
        {
            if (s_CachedPipelineInspectorStyleSheet == null)
                s_CachedPipelineInspectorStyleSheet = EditorGUIUtility.Load(k_PipelineInspectorStyleSheetPath) as StyleSheet;
            return s_CachedPipelineInspectorStyleSheet;
        }

        internal static VisualTreeAsset GetExecutionViewTemplate()
        {
            if (s_CachedExecutionViewTemplate == null)
                s_CachedExecutionViewTemplate = EditorGUIUtility.Load(k_ExecutionViewTemplatePath) as VisualTreeAsset;
            return s_CachedExecutionViewTemplate;
        }

        internal static VisualTreeAsset GetExecutionRowTemplate()
        {
            if (s_CachedExecutionRowTemplate == null)
                s_CachedExecutionRowTemplate = EditorGUIUtility.Load(k_ExecutionRowTemplatePath) as VisualTreeAsset;
            return s_CachedExecutionRowTemplate;
        }

        internal static VisualTreeAsset GetPostprocessorElementTemplate()
        {
            if (s_CachedPostprocessorElementTemplate == null)
                s_CachedPostprocessorElementTemplate =
                    EditorGUIUtility.Load(k_PostprocessorElementTemplatePath) as VisualTreeAsset;
            return s_CachedPostprocessorElementTemplate;
        }

        /// <summary>
        /// Creates a fully configured ListView bound to a BlockCollection field, including
        /// add/remove, context menu, drag-drop support, and missing-type warnings.
        /// </summary>
        /// <param name="binding">Property path of the BlockCollection field to bind to.</param>
        /// <param name="serializedObject">The SerializedObject owning the collection.</param>
        /// <param name="header">Foldout header title; the collection's element interface name is appended.</param>
        /// <param name="readOnly">When true, disables add/remove controls.</param>
        /// <param name="collectionFieldType">Declared BlockCollection&lt;T&gt; field type, used to derive the element type for add-menu filtering.</param>
        public static VisualElement CreateListView(string binding, SerializedObject serializedObject,
            string header = "", bool readOnly = false, Type collectionFieldType = null)
        {
            var listView = new ListView();

            // For BlockCollection, we need to bind to the internal m_Blocks list
            listView.bindingPath = binding + ".m_Blocks";
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.showAddRemoveFooter = false; // We'll add our own button
            listView.reorderable = true;
            listView.reorderMode = ListViewReorderMode.Simple;
            listView.showFoldoutHeader = !readOnly;
            listView.showBoundCollectionSize = false;
            listView.selectionType = SelectionType.Single;
            listView.style.marginLeft = 13.5f;

            listView.viewDataKey = $"{binding}-listview";

            var collectionProperty = serializedObject.FindProperty(binding);

            // Determine the element type of the collection for filtering: BlockCollection<T> -> T.
            Type elementType = collectionFieldType is { IsGenericType: true }
                ? collectionFieldType.GetGenericArguments()[0]
                : typeof(IBlock);

            // Set header title with display name and interface type
            var displayName = !string.IsNullOrEmpty(header) ? header : "Block Sequence";

            var interfaceName = elementType.Name;
            if (interfaceName.StartsWith("I") && interfaceName.Length > 1 && char.IsUpper(interfaceName[1]))
            {
                interfaceName = interfaceName.Substring(1);
            }

            if (!string.IsNullOrEmpty(interfaceName))
            {
                interfaceName = Regex.Replace(interfaceName, "(?<!^)([A-Z][a-z]|[A-Z]+(?=[A-Z]|$))", " $1");
            }

            listView.headerTitle = $"{displayName} ({interfaceName})";

            listView.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var headerLabel = listView.Q<Label>(className: "unity-foldout__text");
                if (headerLabel != null)
                    headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            });

            var capturedElementType = elementType;
            var capturedReadOnly = readOnly;

            if (!capturedReadOnly)
            {
                listView.AddManipulator(new ContextualMenuManipulator((menuEvt) =>
                {
                    var currentSerializedObject = (listView.userData as ListViewBindingContext)?.SerializedObject ??
                                                  serializedObject;
                    var blocksProperty =
                        currentSerializedObject.FindProperty(binding)?.FindPropertyRelative("m_Blocks");
                    if (blocksProperty != null)
                    {
                        // Check if we clicked on a block item
                        var clickedElement = menuEvt.target as VisualElement;
                        bool isBlockItem = false;
                        if (clickedElement != null)
                        {
                            var element = clickedElement;
                            while (element != null && !(element is ListView))
                            {
                                if (element.ClassListContains("unity-list-view__item"))
                                {
                                    isBlockItem = true;
                                    break;
                                }

                                element = element.parent;
                            }
                        }

                        // Only show "Paste block" when clicking on empty space (not on a block item)
                        if (!isBlockItem)
                        {
                            if (BlockClipboard.HasBlockInClipboard())
                            {
                                menuEvt.menu.AppendAction("Paste block",
                                    (a) =>
                                    {
                                        BlockClipboard.PasteBlock(blocksProperty, blocksProperty.arraySize,
                                            capturedElementType);
                                    }, DropdownMenuAction.AlwaysEnabled);
                            }
                            else
                            {
                                menuEvt.menu.AppendAction("Paste block", (a) => { },
                                    DropdownMenuAction.Status.Disabled);
                            }

                            menuEvt.menu.AppendSeparator("");
                        }
                        else
                        {
                            // Add separator before collection operations when on a block item
                            menuEvt.menu.AppendSeparator("");
                        }

                        menuEvt.menu.AppendAction("Copy all blocks",
                            (a) => { BlockClipboard.CopyCollection(blocksProperty); },
                            (a) => blocksProperty.arraySize > 0
                                ? DropdownMenuAction.Status.Normal
                                : DropdownMenuAction.Status.Disabled);

                        if (BlockClipboard.HasCollectionInClipboard())
                        {
                            menuEvt.menu.AppendAction("Paste blocks (replace)",
                                (a) =>
                                {
                                    BlockClipboard.PasteCollection(blocksProperty, append: false, capturedElementType);
                                }, DropdownMenuAction.AlwaysEnabled);

                            menuEvt.menu.AppendAction("Paste blocks (append)",
                                (a) =>
                                {
                                    BlockClipboard.PasteCollection(blocksProperty, append: true, capturedElementType);
                                }, DropdownMenuAction.AlwaysEnabled);
                        }
                        else
                        {
                            menuEvt.menu.AppendAction("Paste blocks (replace)", (a) => { },
                                DropdownMenuAction.Status.Disabled);
                            menuEvt.menu.AppendAction("Paste blocks (append)", (a) => { },
                                DropdownMenuAction.Status.Disabled);
                        }

                        // Add "Select script" at the very bottom when clicking on a block item
                        if (isBlockItem)
                        {
                            menuEvt.menu.AppendSeparator("");

                            menuEvt.menu.AppendAction("Select script", (a) =>
                            {
                                var elementIndex = listView.selectedIndex;
                                if (elementIndex >= 0 && elementIndex < blocksProperty.arraySize)
                                {
                                    var itemProperty = blocksProperty.GetArrayElementAtIndex(elementIndex);
                                    UiHelpers.SelectBlockScript(itemProperty);
                                }
                            }, DropdownMenuAction.AlwaysEnabled);
                        }
                    }

                    menuEvt.StopPropagation();
                }));

            }

            var styleSheet = GetBlockItemStyleSheet();

            if (styleSheet != null)
                listView.styleSheets.Add(styleSheet);

            // Custom makeItem to create BlockItemElement wrapper
            listView.makeItem = () =>
            {
                // We return a placeholder that will be replaced in bindItem
                // because we need the SerializedProperty to create BlockItemElement
                return new VisualElement();
            };

            // Custom bindItem to create BlockItemElement and set content
            listView.bindItem = (element, index) =>
            {
                element.Clear();

                // Get the current SerializedObject from context if available, otherwise use captured parameter
                var currentSerializedObject =
                    (listView.userData as ListViewBindingContext)?.SerializedObject ?? serializedObject;

                var collectionProperty = currentSerializedObject.FindProperty(binding);
                if (collectionProperty == null) return;

                var blocksProperty = collectionProperty.FindPropertyRelative("m_Blocks");
                if (blocksProperty == null || index >= blocksProperty.arraySize) return;

                var blockProperty = blocksProperty.GetArrayElementAtIndex(index);

                var blockItem = new BlockItemElement(blockProperty, blocksProperty, index, capturedElementType,
                    listView, currentSerializedObject, capturedReadOnly);
                if (styleSheet != null)
                {
                    blockItem.styleSheets.Add(styleSheet);
                }

                // Attach the row before building its body. The tooltip below runs the block's own
                // Description getter and reflects over its interfaces; if that throws, the header —
                // which owns the remove button — is already attached, so the block stays deletable.
                element.Add(blockItem);

                if (blockProperty != null && blockProperty.managedReferenceValue is IBlock block)
                {
                    string tooltip = BuildBlockTooltip(block, capturedElementType);
                    blockItem.tooltip = tooltip;

                    // Standard drawer resolution picks the body: a [CustomPropertyDrawer] for the
                    // block's type if one exists, Block_PropertyDrawer's default fields otherwise.
                    // The explicit Bind is required — PropertyFields created outside an inspector
                    // never self-bind.
                    var content = new PropertyField(blockProperty);
                    content.Bind(currentSerializedObject);
                    blockItem.SetContent(content);
                }
            };

            // Custom unbindItem to handle cleanup
            listView.unbindItem = (element, index) => { element.Clear(); };

            if (collectionProperty != null)
            {
                var blocksProperty = collectionProperty.FindPropertyRelative("m_Blocks");
                if (blocksProperty != null)
                {
                    if (!capturedReadOnly)
                    {
                        listView.onAdd = view =>
                        {
                            var currentSerializedObject =
                                (listView.userData as ListViewBindingContext)?.SerializedObject ?? serializedObject;
                            ShowAddMenu(listView, currentSerializedObject, capturedElementType);
                        };

                        listView.itemIndexChanged += (sourceIndex, destinationIndex) =>
                        {
                            var currentSerializedObject =
                                (listView.userData as ListViewBindingContext)?.SerializedObject ?? serializedObject;
                            currentSerializedObject.ApplyModifiedProperties();
                        };
                    }

                    listView.TrackPropertyValue(blocksProperty, prop =>
                    {
                        if (listView != null)
                            listView.RefreshItems();
                    });
                }
            }

            var container = new VisualElement();

            if (collectionProperty != null)
            {
                var missingTypesHelpBox = new HelpBox(
                    "This block collection contains missing block types. Remove or replace the missing blocks.",
                    HelpBoxMessageType.Warning);
                missingTypesHelpBox.name = k_MissingTypesHelpBoxName;
                missingTypesHelpBox.style.display = HasMissingBlockTypes(collectionProperty)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                container.Add(missingTypesHelpBox);

                var blocksPropertyForTracking = collectionProperty.FindPropertyRelative("m_Blocks");
                if (blocksPropertyForTracking != null)
                {
                    listView.TrackPropertyValue(blocksPropertyForTracking, prop =>
                    {
                        var cp = prop.serializedObject.FindProperty(binding);
                        missingTypesHelpBox.style.display = HasMissingBlockTypes(cp)
                            ? DisplayStyle.Flex
                            : DisplayStyle.None;
                    });
                }
            }

            container.Add(listView);

            if (!capturedReadOnly)
            {
                Button addButton = null;
                addButton = new Button(() =>
                {
                    var currentSerializedObject = (listView.userData as ListViewBindingContext)?.SerializedObject ??
                                                  serializedObject;
                    ShowAddMenu(listView, currentSerializedObject, capturedElementType, anchor: addButton);
                });
                addButton.text = "Add block";
                addButton.AddToClassList(k_AddButtonClassName);

                addButton.style.width = 230;
                addButton.style.minWidth = 230;
                addButton.style.maxWidth = 230;
                addButton.style.height = 24;
                addButton.style.flexGrow = 0;
                addButton.style.flexShrink = 0;
                addButton.style.alignSelf = Align.Center;

                container.Add(addButton);
                addButton.style.display = DisplayStyle.None;

                listView.RegisterCallback<GeometryChangedEvent>(OnFirstLayout);

                void OnFirstLayout(GeometryChangedEvent _)
                {
                    listView.UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);

                    Foldout headerFoldout = null;
                    var foldouts = listView.Query<Foldout>().ToList();
                    foreach (var f in foldouts)
                    {
                        if (f.parent == listView)
                        {
                            headerFoldout = f;
                            break;
                        }
                    }

                    if (headerFoldout != null)
                    {
                        addButton.style.display = headerFoldout.value ? DisplayStyle.Flex : DisplayStyle.None;

                        headerFoldout.RegisterValueChangedCallback(evt =>
                        {
                            if (evt.target == headerFoldout)
                                addButton.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                        });
                    }
                }
            }

            container.RegisterCallback<AttachToPanelEvent>(OnAttachCheckFoldout);

            void OnAttachCheckFoldout(AttachToPanelEvent _)
            {
                container.UnregisterCallback<AttachToPanelEvent>(OnAttachCheckFoldout);

                var ancestor = container.parent;
                while (ancestor != null)
                {
                    if (ancestor.ClassListContains(k_ExpandFoldoutClassName))
                    {
                        listView.RegisterCallback<GeometryChangedEvent>(OnExpandFoldout);

                        void OnExpandFoldout(GeometryChangedEvent __)
                        {
                            listView.UnregisterCallback<GeometryChangedEvent>(OnExpandFoldout);
                            var foldouts = listView.Query<Foldout>().ToList();
                            foreach (var f in foldouts)
                            {
                                if (f.parent == listView)
                                {
                                    f.value = true;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                    ancestor = ancestor.parent;
                }
            }

            return container;
        }

        /// <summary>
        /// Creates default PropertyField content for a block
        /// </summary>
        public static VisualElement CreateDefaultContent(SerializedProperty blockProperty)
        {
            var content = new VisualElement();

            var blockTypeName = blockProperty.managedReferenceFullTypename;
            var blockPathLength = blockProperty.propertyPath.Length;

            // Iterate through all visible properties of the block
            var property = blockProperty.Copy();
            var endProperty = property.GetEndProperty();
            property.NextVisible(true); // Enter the block

            while (!SerializedProperty.EqualContents(property, endProperty))
            {
                // Skip the Name and Description properties as they're shown in the header
                // Also skip m_Enabled (has its own toggle) and m_BlockInstanceID (internal infrastructure)
                if (property.name != "Name" && property.name != "Description" && property.name != "m_Enabled" &&
                    property.name != "m_BlockInstanceID")
                {
                    var field = new PropertyField(property);

                    if (!string.IsNullOrEmpty(blockTypeName) && property.propertyPath.Length > blockPathLength)
                    {
                        var relativePath = property.propertyPath.Substring(blockPathLength + 1);
                        field.viewDataKey = $"block-prop-{blockTypeName}-{relativePath}";
                    }

                    field.BindProperty(property.Copy());
                    content.Add(field);
                }

                if (!property.NextVisible(false))
                    break;
            }

            return content;
        }

        static string BuildBlockTooltip(IBlock block, Type collectionElementType)
        {
            var blockType = block.GetType();

            var tooltip = new StringBuilder();
            tooltip.AppendLine($"<b>{block.Name}</b>");

            if (!string.IsNullOrEmpty(block.Description))
            {
                tooltip.AppendLine();
                tooltip.AppendLine("<b>Description:</b>");
                tooltip.AppendLine($"  {block.Description}");
            }

            var interfaceSection = GetInterfaceTooltipSection(blockType, collectionElementType);
            if (interfaceSection != null)
            {
                if (tooltip.Length > 0)
                    tooltip.AppendLine();
                tooltip.Append(interfaceSection);
            }

            return tooltip.ToString().TrimEnd();
        }

        // Cached per (blockType, elementType) — interface metadata is stable per type.
        static string GetInterfaceTooltipSection(Type blockType, Type collectionElementType)
        {
            var cacheKey = (blockType, collectionElementType);
            if (s_InterfaceTooltipCache.TryGetValue(cacheKey, out var cached))
                return cached;

            var interfaces = blockType.GetInterfaces();
            var allInterfaces = new List<Type>();
            foreach (var i in interfaces)
            {
                if (i != typeof(IBlock) && typeof(IBlock).IsAssignableFrom(i))
                    allInterfaces.Add(i);
            }

            var leafInterfaces = new List<Type>();
            foreach (var iface in allInterfaces)
            {
                bool isLeaf = true;
                foreach (var other in allInterfaces)
                {
                    if (other != iface && iface.IsAssignableFrom(other))
                    {
                        isLeaf = false;
                        break;
                    }
                }
                if (isLeaf)
                    leafInterfaces.Add(iface);
            }

            var filteredInterfaces = new List<Type>();
            foreach (var iface in leafInterfaces)
            {
                if (collectionElementType.IsAssignableFrom(iface))
                    filteredInterfaces.Add(iface);
            }

            filteredInterfaces.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            string result = null;
            if (filteredInterfaces.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("<b>Interfaces:</b>");
                foreach (var iface in filteredInterfaces)
                {
                    var interfaceName = iface.Name;
                    if (interfaceName.StartsWith("I") && interfaceName.Length > 1 && char.IsUpper(interfaceName[1]))
                        interfaceName = interfaceName.Substring(1);
                    sb.AppendLine($"  - {interfaceName}");
                }
                result = sb.ToString();
            }

            s_InterfaceTooltipCache[cacheKey] = result;
            return result;
        }

        public static void ShowAddMenu(ListView listView, SerializedObject target, Type requiredInterface,
            Vector2? mousePosition = null, int? insertIndex = null, VisualElement anchor = null)
        {
            // Anchor the picker to the triggering button when one is supplied, so it opens aligned under the
            // button and flips above it when there's no room below (the Add Component dropdown behaviour).
            // Otherwise open at the mouse position (the right-click "insert here" path).
            Rect anchorRect;
            if (anchor != null)
            {
                anchorRect = GUIUtility.GUIToScreenRect(anchor.worldBound);
            }
            else
            {
                Vector2 position = mousePosition ?? (UnityEngine.Event.current != null
                    ? UnityEngine.Event.current.mousePosition
                    : Vector2.zero);
                Vector2 screenPosition = GUIUtility.GUIToScreenPoint(position);
                anchorRect = new Rect(screenPosition.x, screenPosition.y, 0, 0);
            }

            BlockSelectionWindow.Show(
                anchorRect,
                requiredInterface,
                blockType => BlockListHelper.AddItemToList(listView, target, blockType, insertIndex)
            );
        }

        /// <summary>
        /// Finds the MonoScript asset for a given type, using cached results to avoid repeated AssetDatabase searches.
        /// </summary>
        public static MonoScript FindScriptForType(Type type)
        {
            if (type == null) return null;

            if (s_ScriptCache.TryGetValue(type, out var cachedScript))
            {
                return cachedScript;
            }

            var guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

                if (script != null && script.GetClass() == type)
                {
                    s_ScriptCache[type] = script;
                    return script;
                }
            }

            // Cache null result to avoid repeated failed searches
            s_ScriptCache[type] = null;
            return null;
        }

        public static void SelectBlockScript(SerializedProperty blockProperty)
        {
            if (blockProperty.managedReferenceValue == null) return;

            var blockType = blockProperty.managedReferenceValue.GetType();
            var script = FindScriptForType(blockType);

            if (script != null)
            {
                // Select and highlight the script in the Project window
                Selection.activeObject = script;
                EditorGUIUtility.PingObject(script);
            }
            else
            {
                Debug.LogWarning($"Could not find script for block type: {blockType.Name}");
            }
        }

        static bool HasMissingBlockTypes(SerializedProperty collectionProperty)
        {
            if (collectionProperty == null) return false;

            var blocksProperty = collectionProperty.FindPropertyRelative("m_Blocks");
            if (blocksProperty == null) return false;

            for (int i = 0; i < blocksProperty.arraySize; i++)
            {
                var block = blocksProperty.GetArrayElementAtIndex(i);
                if (block.managedReferenceValue == null)
                    return true;
            }

            return false;
        }

    }
}
