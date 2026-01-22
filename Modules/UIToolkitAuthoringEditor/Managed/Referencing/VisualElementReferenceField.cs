// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using Unity.UIToolkit.Editor;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements;

/// <summary>
/// Makes a field to receive any <see cref="VisualElementReference"/> type.
/// </summary>
public class VisualElementReferenceField : BaseField<VisualElementReference>
{
    /// <summary>
    /// USS class name of elements of this type.
    /// </summary>
    public new static readonly string ussClassName = "unity-visual-element-reference-field";
    /// <summary>
    /// USS class name of labels in elements of this type.
    /// </summary>
    public new static readonly string labelUssClassName = ussClassName + "__label";
    /// <summary>
    /// USS class name of input elements in elements of this type.
    /// </summary>
    public new static readonly string inputUssClassName = ussClassName + "__input";
    /// <summary>
    /// USS class name of object elements in elements of this type.
    /// </summary>
    public static readonly string objectUssClassName = ussClassName + "__object";

    /// <summary>
    /// USS class name of selector elements in elements of this type.
    /// </summary>
    public static readonly string selectorUssClassName = ussClassName + "__selector";

    internal static readonly PropertyName serializedPropertyKey = new PropertyName("--unity-visual-element-reference-field-serialized-property");

    static readonly string k_PickerHeading = L10n.Tr("{0} Element Reference");

    /// <summary>
    /// The type that can be assigned, must be a <see cref="VisualElement"/> or derive from it.
    /// </summary>
    public Type elementType
    {
        get => m_Type;
        set
        {
            if (m_Type == value)
                return;

            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (!typeof(VisualElement).IsAssignableFrom(value))
                throw new ArgumentException($"Type must be a VisualElement or derive from it. Type '{value.FullName}' is not valid.", nameof(value));

            m_Type = value;
            UpdateDisplay();
        }
    }

    internal VisualElementReferenceFieldDisplay m_ObjectFieldDisplay;
    Type m_Type = typeof(VisualElement);

    /// <summary>
    /// Initializes a new instance of the VisualElementReferenceField class with the specified label.
    /// </summary>
    /// <param name="label">The text to display as the label for the field. Can be null or empty to display no label.</param>
    public VisualElementReferenceField(string label) : base(label, null)
    {
        visualInput.focusable = false;
        labelElement.focusable = false;

        AddToClassList(ussClassName);
        AddToClassList(alignedFieldUssClassName);
        labelElement.AddToClassList(labelUssClassName);

        m_ObjectFieldDisplay = new VisualElementReferenceFieldDisplay(this);
        m_ObjectFieldDisplay.AddToClassList(objectUssClassName);
        var objectSelector = new VisualElementReferenceFieldSelector(this);
        objectSelector.AddToClassList(selectorUssClassName);

        var objectFieldInput = new VisualElement();
        objectFieldInput.AddToClassList(inputUssClassName);
        objectFieldInput.Add(m_ObjectFieldDisplay);
        objectFieldInput.Add(objectSelector);
        visualInput.Add(objectFieldInput);

        RegisterCallback<SerializedPropertyBindEvent>(evt =>
        {
            SetProperty(serializedPropertyKey, evt.bindProperty);
            m_ObjectFieldDisplay?.Update();
        });
    }

    /// <inheritdoc />
    public override void SetValueWithoutNotify(VisualElementReference newValue)
    {
        base.SetValueWithoutNotify(newValue);
        UpdateDisplay();
    }

    /// <inheritdoc />
    protected override void UpdateMixedValueContent()
    {
        m_ObjectFieldDisplay?.ShowMixedValue(showMixedValue);
    }

    internal virtual void UpdateDisplay()
    {
        UpdateMixedValueContent();
    }

    void ShowObjectSelector()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var property = GetProperty(serializedPropertyKey) as SerializedProperty;
        if (property?.serializedObject?.targetObject is Behaviour behaviour)
            scene = behaviour.gameObject.scene;

        var provider = new VisualElementReferenceSearchProvider(elementType, scene);
        var context = Search.SearchService.CreateContext(provider);
        var title = string.Format(k_PickerHeading, elementType.Name);
        var state = SearchViewState.CreatePickerState(title, context, SelectFromPicker, null, null, SearchViewFlags.ListView | SearchViewFlags.CompactView);
        state.hideTabs = true;
        var view = Search.SearchService.ShowPicker(state);
    }

    void SelectFromPicker(SearchItem item, bool cancelled)
    {
        if (cancelled)
            return;

        if (item == SearchItem.clear)
        {
            value = new VisualElementReference();
            return;
        }

        if (item.data is VisualElementReferenceSearchProvider.Data searchData)
        {
            var authoringIdPath = searchData.GeneratePath();
            VisualElementReferenceTools.TryCreateReference(searchData.panelRenderer, authoringIdPath);
            value = new VisualElementReference(searchData.panelRenderer, authoringIdPath);
        }
    }

    class VisualElementReferenceFieldSelector : VisualElement
    {
        private readonly VisualElementReferenceField m_Field;

        public VisualElementReferenceFieldSelector(VisualElementReferenceField objectField)
        {
            m_Field = objectField;
        }

        [EventInterest(typeof(MouseDownEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (evt is MouseDownEvent mouseEvent && mouseEvent.button == (int)MouseButton.LeftMouse)
            {
                m_Field.ShowObjectSelector();
                evt.StopPropagation();
            }
        }
    }

    internal class VisualElementReferenceFieldDisplay : VisualElement
    {
        public static readonly string ussClassName = "unity-visual-element-reference-field-display";
        public static readonly string iconUssClassName = ussClassName + "__icon";
        public static readonly string labelUssClassName = ussClassName + "__label";
        public static readonly string nullLabelUssClassName = labelUssClassName + "--value-null";
        public static readonly string nullIconUssClassName = iconUssClassName + "--value-null";
        public static readonly string acceptDropVariantUssClassName = ussClassName + "--accept-drop";

        internal static readonly string k_MissingReferenceLabel = L10n.Tr("Missing Reference ({0})");
        internal static readonly string k_NoneLabel = L10n.Tr("None ({0})");
        internal static readonly string k_TypeMismatchLabel = L10n.Tr("Type mismatch");
        internal static readonly string k_SceneMismatch = L10n.Tr("Scene mismatch (cross scene references not supported)");

        readonly VisualElementReferenceField m_Field;
        readonly Image m_ObjectIcon = new Image { scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
        readonly Label m_ObjectLabel = new Label { pickingMode = PickingMode.Ignore };

        public VisualElementReferenceFieldDisplay(VisualElementReferenceField field)
        {
            focusable = true;

            AddToClassList(ussClassName);
            m_Field = field;
            m_ObjectIcon.AddToClassList(iconUssClassName);
            m_ObjectLabel.AddToClassList(labelUssClassName);

            Update();

            Add(m_ObjectIcon);
            Add(m_ObjectLabel);
        }

        public void ShowMixedValue(bool show)
        {
            if (show)
            {
                m_ObjectLabel.text = mixedValueString;
                m_ObjectLabel.AddToClassList(mixedValueLabelUssClassName);
                m_ObjectIcon.image = null;
            }
            else
            {
                m_ObjectLabel.RemoveFromClassList(mixedValueLabelUssClassName);
                Update();
            }
        }

        bool TryFindReferencedAssets(out VisualElementAsset asset, out PanelRenderer renderer)
        {
            asset = null;

            if (m_Field.value != null && m_Field.value.authoringPath.path.Length != 0 && m_Field.value.panelRenderer != null)
            {
                renderer = m_Field.value.panelRenderer;
                var vta = renderer.visualTreeAsset;
                if (vta != null)
                    asset = vta.FindElementByPath(m_Field.value.authoringPath);
                return asset != null && renderer != null;
            }

            renderer = null;
            return true;
        }

        public void Update()
        {
            bool isMissingPanelRenderer = false;
            SerializedProperty panelRendererProp = null;

            var property = m_Field.GetProperty(VisualElementReferenceField.serializedPropertyKey) as SerializedProperty;
            if (property?.FindPropertyRelative("m_PanelRenderer") is { } prop)
            {
                panelRendererProp = prop;
                isMissingPanelRenderer = ObjectField.IsMissingObjectReference(prop);
            }
            bool isMissingVisualElementAsset = !TryFindReferencedAssets(out var resolvedVisualElementAsset, out var renderer);

            string label;
            Texture2D icon = null;

            if (resolvedVisualElementAsset == null)
            {
                if (isMissingPanelRenderer || isMissingVisualElementAsset)
                {
                    label = string.Format(k_MissingReferenceLabel, m_Field.elementType.Name);
                }
                else
                {
                    label = string.Format(k_NoneLabel, m_Field.elementType.Name);
                }
            }
            else
            {
                // Check the type is compatible
                var elementType = resolvedVisualElementAsset.serializedData?.GetType()?.DeclaringType ?? typeof(VisualElement);
                if (!m_Field.elementType.IsAssignableFrom(elementType))
                {
                    label = k_TypeMismatchLabel;
                    icon = UIResources.GetIconForType(m_Field.elementType, UIResources.RequestSize.Px16).texture;
                }
                // Check for cross-scene references
                else if (EditorSceneManager.preventCrossSceneReferences &&
                    property != null &&
                    EditorGUI.CheckForCrossSceneReferencing(renderer, property.serializedObject.targetObject))
                {
                    label = k_SceneMismatch;
                }
                else
                { 
                    label = VisualElementReferenceTools.GenerateVisualElementAssetLabel(resolvedVisualElementAsset);
                    if (resolvedVisualElementAsset.serializedData is VisualElement.UxmlSerializedData veSerializedData)
                        icon = UIResources.GetIconForType(veSerializedData.GetType().DeclaringType, UIResources.RequestSize.Px16).texture;
                }
            }

            m_ObjectLabel.text = label;
            m_ObjectIcon.image = icon ?? UIResources.GetIconForType(m_Field.elementType, UIResources.RequestSize.Px16).texture;
            m_ObjectIcon.EnableInClassList(nullIconUssClassName, resolvedVisualElementAsset == null);
            m_ObjectLabel.EnableInClassList(nullLabelUssClassName, resolvedVisualElementAsset == null);
        }

        [EventInterest(typeof(MouseDownEvent), typeof(KeyDownEvent),
            typeof(DragUpdatedEvent), typeof(DragPerformEvent), typeof(DragLeaveEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (evt is MouseDownEvent mouseDownEvent && mouseDownEvent.button == (int)MouseButton.LeftMouse)
            {
                OnMouseDown(mouseDownEvent);
            }
            else if (evt is KeyDownEvent keyDownEvent)
            {
                if (keyDownEvent.keyCode == KeyCode.Space ||
                    keyDownEvent.keyCode == KeyCode.KeypadEnter ||
                    keyDownEvent.keyCode == KeyCode.Return)
                {
                    OnKeyboardEnter();
                }
                else if (keyDownEvent.keyCode == KeyCode.Delete || keyDownEvent.keyCode == KeyCode.Backspace)
                {
                    OnKeyboardDelete();
                }
            }

            // Drag events should not reach this point but we are adding this check for extra safety, because
            // it might cause a crash depending on the object type. See case 1416878.
            if (!enabledInHierarchy)
                return;

            switch(evt)
            {
                case DragUpdatedEvent: OnDragUpdated(evt); break;
                case DragPerformEvent: OnDragPerform(evt); break;
                case DragLeaveEvent: OnDragLeave(); break;
            }
        }

        [EventInterest(typeof(MouseDownEvent))]
        internal override void HandleEventBubbleUpDisabled(EventBase evt)
        {
            base.HandleEventBubbleUpDisabled(evt);

            if (evt is MouseDownEvent mouseDownEvent && mouseDownEvent.button == (int)MouseButton.LeftMouse)
                OnMouseDown(mouseDownEvent);
        }

        void OnDragLeave()
        {
            // Make sure we've cleared the accept drop look, whether we we in a drop operation or not.
            EnableInClassList(acceptDropVariantUssClassName, false);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            // One click pings referenced element.
            if (evt.clickCount == 1)
            {
                // ping object
                bool anyModifiersPressed = evt.shiftKey || evt.ctrlKey;
                if (!anyModifiersPressed)
                {
                    PingObject();
                }
                evt.StopPropagation();
            }
            // Double click opens the VisualTreeAsset for editing
            else if (evt.clickCount == 2)
            {
                if (TryFindReferencedAssets(out var resolvedAsset, out var _) && resolvedAsset != null)
                {
                    AssetDatabase.OpenAsset(resolvedAsset.visualTreeAsset);
                }
                evt.StopPropagation();
            }
        }

        void PingObject()
        {
            if (m_Field.value?.panelRenderer == null)
                return;

            var table = m_Field.value.panelRenderer.referenceProvider.referenceTable;
            if (table?.TryGetReference<VisualElement>(m_Field.value.authoringPath, out var targetElement) == true)
            {
                // Extract the EntityId for the element so we can go through the PingObject api.
                foreach (var window in EditorWindow.activeEditorWindows)
                {
                    if (window is HierarchyWindow hierarchyWindow)
                    {
                        var nodeHandler = hierarchyWindow.View.Source.GetOrCreateNodeTypeHandler<HierarchyVisualElementHandler>();
                        if (nodeHandler.GetMappings().TryGetNode(targetElement, out var node))
                        {
                            var entityId = ((IHierarchyEntityIdConverter)nodeHandler).GetEntityId(node);
                            EditorGUIUtility.PingObject(entityId);
                        }
                    }
                }
            }
            else
            {
                // Ping the PanelRenderer, the elements may not be in the scene at the moment
                EditorGUIUtility.PingObject(m_Field.value?.panelRenderer);
            }
        }

        void OnKeyboardEnter() => m_Field.ShowObjectSelector();

        void OnKeyboardDelete()
        {
            m_Field.SetProperty(VisualElementReferenceField.serializedPropertyKey, null);
            m_Field.value = new VisualElementReference();
        }

        VisualElement GetDraggedElement()
        {
            if (DragAndDrop.GetGenericData(VisualElementNodeTypeHandler.DraggedVisualElementKey) is List<VisualElement> draggedElements &&
                draggedElements.Count == 1 &&
                m_Field.elementType.IsAssignableFrom(draggedElements[0].GetType()) &&
                (draggedElements[0].visualElementAsset != null || draggedElements[0] is IPanelComponentRootElement))
            {
                return draggedElements[0];
            }
            return null;
        }

        void OnDragUpdated(EventBase evt)
        {
            var element = GetDraggedElement();
            var panelComponent = element?.FindRootPanelComponent() as Object;
            if (element != null && panelComponent != null && !IsCrossSceneReference(panelComponent))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                evt.StopPropagation();
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
        }

        bool IsCrossSceneReference(Object panelComponent)
        {
            var property = m_Field.GetProperty(VisualElementReferenceField.serializedPropertyKey) as SerializedProperty;
            return EditorSceneManager.preventCrossSceneReferences &&
                    property != null &&
                    EditorGUI.CheckForCrossSceneReferencing(panelComponent, property.serializedObject.targetObject);
        }

        void OnDragPerform(EventBase evt)
        {
            var element = GetDraggedElement();
            if (element != null && VisualElementReferenceTools.TryCreateReference(element, out var panelRenderer, out var authoringIdPath))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                m_Field.value = new VisualElementReference(panelRenderer, authoringIdPath);

                DragAndDrop.AcceptDrag();
                RemoveFromClassList(acceptDropVariantUssClassName);
                evt.StopPropagation();
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
        }
    }
}
