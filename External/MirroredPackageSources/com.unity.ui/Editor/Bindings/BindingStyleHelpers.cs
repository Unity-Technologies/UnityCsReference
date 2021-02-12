using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings
{
    internal static class BindingsStyleHelpers
    {
        internal static void UpdateElementStyle(VisualElement element, SerializedProperty prop)
        {
            if (element == null)
                return;

            if (element is Foldout)
            {
                // We only want to apply override styles onto the Foldout header, not the entire contents.
                element = element.Q(className: Foldout.toggleUssClassName);
            }
            else if (element.ClassListContains(BaseCompositeField<int, IntegerField, int>.ussClassName)
                     || element is BoundsField || element is BoundsIntField)
            {
                // The problem with compound fields is that they are bound at the parent level using
                // their parent value data type. For example, a Vector3Field is bound to the parent
                // SerializedProperty which uses the Vector3 data type. However, animation overrides
                // are not stored on the parent SerializedProperty but on the component child
                // SerializedProperties. So even though we're bound to the parent property, we still
                // have to dive inside and example the child SerializedProperties (ie. x, y, z, height)
                // and override the animation styles individually.

                var compositeField = element;

                // The element we style in the main pass is going to be just the label.
                element = element.Q(className: BaseField<int>.labelUssClassName);

                // Go through the inputs and find any that match the names of the child PropertyFields.
                var propCopy = prop.Copy();
                var endProperty = propCopy.GetEndProperty();
                propCopy.NextVisible(true);     // Expand the first child.
                do
                {
                    if (SerializedProperty.EqualContents(propCopy, endProperty))
                        break;

                    var subInputName = "unity-" + propCopy.name + "-input";
                    var subInput = compositeField.Q(subInputName);
                    if (subInput == null)
                        continue;

                    UpdateElementStyle(subInput, propCopy);
                }
                while (propCopy.NextVisible(false));     // Never expand children.
            }

            // It's possible for there to be no label in a compound field, for example. So, nothing to style.
            if (element == null)
                return;

            // Handle prefab state.
            UpdatePrefabStateStyle(element, prop);

            // Handle animated state.

            // Since we handle compound fields above, the element here will always be a single field
            // (or not a field at all). This means we can perform a faster query and search for
            // a single element.
            var inputElement = element.Q(className: BaseField<int>.inputUssClassName);
            if (inputElement == null)
            {
                return;
            }

            bool animated = AnimationMode.IsPropertyAnimated(prop.serializedObject.targetObject, prop.propertyPath);
            bool candidate = AnimationMode.IsPropertyCandidate(prop.serializedObject.targetObject, prop.propertyPath);
            bool recording = AnimationMode.InAnimationRecording();

            inputElement.EnableInClassList(BindingExtensions.animationRecordedUssClassName, animated && recording);
            inputElement.EnableInClassList(BindingExtensions.animationCandidateUssClassName, animated && !recording && candidate);
            inputElement.EnableInClassList(BindingExtensions.animationAnimatedUssClassName, animated && !recording && !candidate);
        }

        internal static void UpdatePrefabStateStyle(VisualElement element, SerializedProperty prop)
        {
            bool handlePrefabState = false;

            try
            {
                // This can throw if the serialized object changes type under our feet
                handlePrefabState = prop.serializedObject.targetObjects.Length == 1 &&
                    prop.isInstantiatedPrefab &&
                    prop.prefabOverride;
            }
            catch (Exception)
            {
                return;
            }

            // Handle prefab state.
            if (handlePrefabState)
            {
                if (!element.ClassListContains(BindingExtensions.prefabOverrideUssClassName))
                {
                    var container = FindPrefabOverrideBarCompatibleParent(element);
                    var barContainer = container?.prefabOverrideBlueBarsContainer;

                    element.AddToClassList(BindingExtensions.prefabOverrideUssClassName);

                    if (container != null && barContainer != null)
                    {
                        // Ideally, this blue bar would be a child of the field and just move
                        // outside the field in absolute offsets to hug the side of the field's
                        // container. However, right now we need to have overflow:hidden on
                        // fields because of case 1105567 (the inputs can grow beyond the field).
                        // Therefore, we have to add the blue bars as children of the container
                        // and move them down beside their respective field.

                        var prefabOverrideBar = new VisualElement();
                        prefabOverrideBar.name = BindingExtensions.prefabOverrideBarName;
                        prefabOverrideBar.userData = element;
                        prefabOverrideBar.AddToClassList(BindingExtensions.prefabOverrideBarUssClassName);
                        barContainer.Add(prefabOverrideBar);

                        element.SetProperty(BindingExtensions.prefabOverrideBarName, prefabOverrideBar);

                        // We need to try and set the bar style right away, even if the container
                        // didn't compute its layout yet. This is for when the override is done after
                        // everything has been layed out.
                        UpdatePrefabOverrideBarStyle(prefabOverrideBar);

                        // We intentionally re-register this event on the container per element and
                        // never unregister.
                        container.RegisterCallback<GeometryChangedEvent>(UpdatePrefabOverrideBarStyleEvent);
                    }
                }
            }
            else if (element.ClassListContains(BindingExtensions.prefabOverrideUssClassName))
            {
                element.RemoveFromClassList(BindingExtensions.prefabOverrideUssClassName);

                var container = FindPrefabOverrideBarCompatibleParent(element);
                var barContainer = container?.prefabOverrideBlueBarsContainer;

                if (container != null && barContainer != null)
                {
                    var prefabOverrideBar = element.GetProperty(BindingExtensions.prefabOverrideBarName) as VisualElement;
                    if (prefabOverrideBar != null)
                        prefabOverrideBar.RemoveFromHierarchy();
                }
            }
        }

        private static InspectorElement FindPrefabOverrideBarCompatibleParent(VisualElement field)
        {
            // For now we only support these blue prefab override bars within an InspectorElement.
            return field.GetFirstAncestorOfType<InspectorElement>();
        }

        private static void UpdatePrefabOverrideBarStyle(VisualElement blueBar)
        {
            var element = blueBar.userData as VisualElement;

            var container = FindPrefabOverrideBarCompatibleParent(element);
            if (container == null)
                return;

            // Move the bar to where the control is in the container.
            var top = element.worldBound.y - container.worldBound.y;
            if (float.IsNaN(top))     // If this is run before the container has been layed out.
                return;

            var elementHeight = element.resolvedStyle.height;

            // This is needed so if you have 2 overridden fields their blue
            // bars touch (and it looks like one long bar). They normally wouldn't
            // because most fields have a small margin.
            var bottomOffset = element.resolvedStyle.marginBottom;

            blueBar.style.top = top;
            blueBar.style.height = elementHeight + bottomOffset;
            blueBar.style.left = 0.0f;
        }

        private static void UpdatePrefabOverrideBarStyleEvent(GeometryChangedEvent evt)
        {
            var container = evt.target as InspectorElement;
            if (container == null)
                return;

            var barContainer = container.Q(BindingExtensions.prefabOverrideBarContainerName);
            if (barContainer == null)
                return;

            foreach (var bar in barContainer.Children())
                UpdatePrefabOverrideBarStyle(bar);
        }

        internal static void RegisterRightClickMenu<TValue>(BaseField<TValue> field, SerializedProperty property)
        {
            var fieldLabelElement = field.Q<Label>(className: BaseField<TValue>.labelUssClassName);
            if (fieldLabelElement != null)
            {
                fieldLabelElement.userData = property.Copy();
                fieldLabelElement.RegisterCallback<MouseUpEvent>(RightClickFieldMenuEvent);
            }
        }

        internal static void UnregisterRightClickMenu<TValue>(BaseField<TValue> field)
        {
            var fieldLabelElement = field.Q<Label>(className: BaseField<TValue>.labelUssClassName);
            fieldLabelElement?.UnregisterCallback<MouseUpEvent>(RightClickFieldMenuEvent);
        }

        internal static void RightClickFieldMenuEvent(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.RightMouse)
                return;

            var label = evt.target as Label;
            if (label == null)
                return;

            var property = label.userData as SerializedProperty;
            if (property == null)
                return;

            var menu = EditorGUI.FillPropertyContextMenu(property);
            var menuPosition = new Vector2(label.layout.xMin, label.layout.height);
            menuPosition = label.LocalToWorld(menuPosition);
            var menuRect = new Rect(menuPosition, Vector2.zero);
            menu.DropDown(menuRect);

            evt.PreventDefault();
            evt.StopPropagation();
        }
    }
}
