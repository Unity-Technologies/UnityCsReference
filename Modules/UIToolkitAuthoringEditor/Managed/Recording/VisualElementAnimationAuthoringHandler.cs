// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEditorInternal;
using System.Diagnostics.CodeAnalysis;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Singleton handler for UIToolkit VisualElement animation authoring in the Animation Window.
    /// Manages VisualElementSelection objects for proper selection behavior when animating UI elements,
    /// and provides channel grouping and custom value field rendering for UIElements style properties.
    /// </summary>
    internal class VisualElementAnimationAuthoringHandler : IAnimationWindowPropertyHandler
    {
        private static VisualElementAnimationAuthoringHandler s_Instance;

        private const float k_ValueFieldWidth = 80f;
        private const float k_ValueFieldOffsetFromRightSide = 30f;
        private const float k_ValueLengthUnitFieldWidth = 50f;
        private const float k_ButtonSpacing = 1f;
        private const float k_ObjectFieldAdditionalWidth = 30f;
        private const float k_ObjectFieldAdditionalOffset = k_ObjectFieldAdditionalWidth + 30f;
        private const float k_ObjectFieldMaxHeight = 18f;

        private const string k_BackgroundImageSuffix = "/BackgroundImage";

        private static readonly Type[] k_BackgroundImageTypes =
        {
            typeof(Texture2D),
            typeof(RenderTexture),
            typeof(Sprite),
            typeof(VectorImage),
        };

        internal static VisualElementAnimationAuthoringHandler Instance => s_Instance;

        internal static void Register()
        {
            s_Instance = new VisualElementAnimationAuthoringHandler();
            UIAnimationBinder.s_GetSelectionEntityIdCallback = GetSelectionEntityIdCallback;
            AnimationWindowUtility.RegisterPropertyHandler(s_Instance);
        }

        private static EntityId GetSelectionEntityIdCallback(VisualElement element)
        {
            return s_Instance?.GetSelectionEntityIdForElement(element) ?? EntityId.None;
        }

        internal EntityId GetSelectionEntityIdForElement(VisualElement element)
        {
            if (element == null)
                return EntityId.None;

            var selection = VisualElementUtility.GetSelectionObject(element);

            if(selection == null)
            {
                selection = ScriptableObject.CreateInstance<VisualElementSelection>();
                selection.Element = element;
                selection.hideFlags = HideFlags.HideAndDontSave;
                VisualElementUtility.SetSelectionObject(element, selection);
            }

            return selection != null ? selection.GetEntityId() : EntityId.None;
        }

        // --- IAnimationWindowPropertyHandler ---

        // Unique sub-channel suffix (e.g. ".value", ".offset.unit", ".x.value") to channel
        // index, built from the generator's channel-suffix tables at first use. Single-char
        // suffixes (.x / .y / .z / .r / .g / .b / .a / .w) are excluded because
        // AnimationWindowUtility.GetComponentIndex already resolves them on its built-in
        // path; including them here would shadow that fast path without benefit.
        //
        // This replaces the previous k_*Suffix constants + EndsWith cascades. Adding a new
        // composite property with fresh Length sub-structs or a new enum selector channel
        // now requires zero changes here: the generator emits the suffix, this map picks
        // it up. The map also carries AnimationChannelKind so TryDoValueField can dispatch
        // unit/enum/PPtr popups uniformly.
        private static Dictionary<string, ChannelEntry> s_SuffixLookup;

        private readonly struct ChannelEntry
        {
            public readonly int ChannelIndex;
            public readonly UIAnimationBinder.AnimationChannelKind Kind;
            public readonly StylePropertyId PropertyId;

            public ChannelEntry(int channelIndex, UIAnimationBinder.AnimationChannelKind kind, StylePropertyId propertyId)
            {
                ChannelIndex = channelIndex;
                Kind = kind;
                PropertyId = propertyId;
            }
        }

        private static Dictionary<string, ChannelEntry> SuffixLookup
        {
            get
            {
                if (s_SuffixLookup != null)
                    return s_SuffixLookup;

                var dict = new Dictionary<string, ChannelEntry>();
                int idCount = UIAnimationBinder.StylePropertyIdCount;
                for (int i = 0; i < idCount; i++)
                {
                    var id = (StylePropertyId)i;
                    var suffixes = UIAnimationBinder.GetChannelSuffixes(id);
                    for (int c = 0; c < suffixes.Count; c++)
                    {
                        string suffix = suffixes[c];
                        if (string.IsNullOrEmpty(suffix)) continue;
                        if (IsSingleCharComponentSuffix(suffix)) continue;

                        var kind = UIAnimationBinder.GetChannelKind(id, c);
                        // The generator ensures uniqueness: each non-single-char suffix
                        // (.value, .unit, .align, .type, .offset.value, ...) belongs to at
                        // most one channel of at most one property. If this invariant ever
                        // breaks, the assignment simply picks the last winner and existing
                        // tests will flag the discrepancy.
                        dict[suffix] = new ChannelEntry(c, kind, id);
                    }
                }
                s_SuffixLookup = dict;
                return dict;
            }
        }

        // Single-char channel components like ".x", ".r", ".w" are resolved by
        // AnimationWindowUtility.GetComponentIndex directly and do not need a handler hook.
        private static bool IsSingleCharComponentSuffix(string suffix) =>
            suffix.Length == 2 && suffix[0] == '.';

        // Extracts the first '.' that separates the property name from its sub-channel,
        // returning the full sub-channel suffix (leading dot included, e.g. ".offset.value")
        // or null if there is no valid sub-channel. Property paths never contain dots so
        // locating the slash-relative first dot is unambiguous.
        private static string ExtractSubChannelSuffix(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return null;

            int pathEnd = propertyName.LastIndexOf('/');
            int firstDot = propertyName.IndexOf('.', pathEnd + 1);
            if (firstDot < 0 || firstDot == propertyName.Length - 1)
                return null;

            return propertyName.Substring(firstDot);
        }

        public int GetChannelIndex([NotNull] string propertyName)
        {
            string suffix = ExtractSubChannelSuffix(propertyName);
            if (suffix == null)
                return -1;

            return SuffixLookup.TryGetValue(suffix, out var entry) ? entry.ChannelIndex : -1;
        }

        public string GetPropertyGroupName([NotNull] string propertyName)
        {
            string suffix = ExtractSubChannelSuffix(propertyName);
            if (suffix == null || !SuffixLookup.ContainsKey(suffix))
                return null;

            return propertyName.Substring(0, propertyName.Length - suffix.Length);
        }

        // Maps a Length-unit-like channel suffix to the enum type whose values populate the
        // EditorGUI.EnumPopup. The LengthUnit suffix uses a custom popup (not this map)
        // because we only expose "px" / "%" to users.
        private static readonly Dictionary<string, Type> k_EnumPopupTypeBySuffix = new()
        {
            { ".align", typeof(BackgroundPositionKeyword) },
            { ".type", typeof(BackgroundSizeType) },
        };

        internal class BackgroundImageHandlerData
        {
            public Type selectedType = typeof(Texture2D);
            public bool needsTypeValidation;
        }

        internal static bool IsBackgroundImageProperty(string propertyName)
        {
            return propertyName != null && propertyName.EndsWith(k_BackgroundImageSuffix);
        }

        private static BackgroundImageHandlerData GetOrCreateHandlerData(ref object handlerData)
        {
            if (handlerData is BackgroundImageHandlerData data)
                return data;

            data = new BackgroundImageHandlerData();
            handlerData = data;
            return data;
        }

        public bool TryDoValueField(Rect valueFieldRect, Rect valueFieldDragRect, int controlId,
            EditorCurveBinding curveBinding, Type curveValueType, Type animatableObjectType,
            object currentValue, out object newValue, ref object handlerData)
        {
            if (animatableObjectType != typeof(PanelRenderer))
            {
                newValue = currentValue;
                return false;
            }

            var propName = curveBinding.propertyName;
            string suffix = ExtractSubChannelSuffix(propName);

            // LengthUnit popup (px / %): matches every Length.unit sub-channel regardless of
            // container (Length itself, BackgroundPosition.offset, BackgroundSize.x/.y, ...)
            if (suffix != null && suffix.EndsWith(".unit"))
            {
                newValue = currentValue;
                HandleLengthUnitProperty(valueFieldRect, ref newValue);
                return true;
            }

            // Simple enum-popup suffixes (.align, .type).
            if (suffix != null && k_EnumPopupTypeBySuffix.TryGetValue(suffix, out var enumType))
            {
                newValue = currentValue;
                HandleEnumProperty(enumType, valueFieldRect, ref newValue);
                return true;
            }

            // BackgroundRepeat.x / .y reuse single-char suffixes shared with Translate /
            // Scale / TransformOrigin, so we disambiguate by resolving the property from the
            // propertyName rather than the suffix alone. The dedicated entry in
            // SuffixLookup doesn't apply here because ".x" / ".y" are excluded for being
            // single-char (the AnimationWindow's built-in component lookup handles their
            // channel index separately).
            if (suffix == ".x" || suffix == ".y")
            {
                var fullPropertyName = propName.Substring(0, propName.Length - suffix.Length);
                {
                    newValue = currentValue;
                    return true;
                }
            }
			
			if (IsBackgroundImageProperty(curveBinding.propertyName))
            {
                newValue = currentValue;
                HandleBackgroundImageValueField(valueFieldRect, controlId, ref newValue, ref handlerData);
                return true;
            }

            // PPtr sub-channels would render an EditorGUI.ObjectField at this point; no
            // composite currently emits a PPtr sub-channel suffix, so nothing to dispatch
            // yet. SuffixLookup already carries the Kind metadata needed to wire this up
            // once the first PPtr composite lands.

            newValue = currentValue;
            return false;
        }

        public bool TryPopulateContextMenu(GenericMenu menu,
            EditorCurveBinding curveBinding, Type curveValueType, Type animatableObjectType,
            object handlerData, Action<object> setHandlerData)
        {
            if (animatableObjectType != typeof(PanelRenderer))
                return false;

            if (!IsBackgroundImageProperty(curveBinding.propertyName))
                return false;

            var data = GetOrCreateHandlerData(ref handlerData);
            var currentType = data.selectedType;

            menu.AddSeparator("");
            foreach (var type in k_BackgroundImageTypes)
            {
                var capturedType = type;
                menu.AddItem(
                    new GUIContent("Asset Type/" + ObjectNames.NicifyVariableName(type.Name)),
                    currentType == type,
                    () =>
                    {
                        data.selectedType = capturedType;
                        if (capturedType != currentType)
                            data.needsTypeValidation = true;
                        setHandlerData(data);
                    });
            }

            return true;
        }

        public bool TryValidateDragDrop(EditorCurveBinding curveBinding, Type curveValueType,
            Type animatableObjectType, UnityEngine.Object[] draggedObjects,
            out UnityEngine.Object[] validatedReferences)
        {
            validatedReferences = null;

            if (animatableObjectType != typeof(PanelRenderer))
                return false;

            if (!IsBackgroundImageProperty(curveBinding.propertyName))
                return false;

            foreach (var obj in draggedObjects)
            {
                if (obj == null || GetAcceptedBackgroundImageType(obj) == null)
                    return false;
            }

            validatedReferences = draggedObjects;
            return true;
        }

        private void HandleBackgroundImageValueField(Rect valueFieldRect, int controlId, ref object value, ref object handlerData)
        {
            var data = GetOrCreateHandlerData(ref handlerData);
            var currentObj = value as UnityEngine.Object;
            var currentObjectType = data.selectedType;
            if (data.needsTypeValidation)
            {
                if (currentObj != null && !data.selectedType.IsInstanceOfType(currentObj))
                {
                    if (Event.current.type != EventType.Repaint) // The update will happen on the next imgui event sent on the hierarchy
                    {
                        data.needsTypeValidation = false;
                        GUI.changed = true; // So the invoking code can save the value
                        value = null;
                    }
                } else
                {
                    data.needsTypeValidation = false;
                }
            }
            else if (currentObj != null)
            {
                // We override user's explicit type selection during playback/preview if the animated value changes.
                // This can happen if the curve hold mixed types
                currentObjectType = currentObj.GetType();
            }

            var height = Mathf.Min(k_ObjectFieldMaxHeight, valueFieldRect.height);
            var yOffset = (valueFieldRect.height - height) * 0.5f;
            var fieldRect = new Rect(
                valueFieldRect.x - k_ObjectFieldAdditionalOffset,
                valueFieldRect.y + yOffset,
                valueFieldRect.width + k_ObjectFieldAdditionalWidth,
                height);

            if (TryHandleBackgroundImageDragAndDrop(fieldRect, data, ref value))
                return;

            value = EditorGUI.ObjectField(fieldRect, value as UnityEngine.Object, currentObjectType, false);
        }

        private static bool TryHandleBackgroundImageDragAndDrop(Rect dropRect, BackgroundImageHandlerData data, ref object value)
        {
            var evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
                return false;

            if (!dropRect.Contains(evt.mousePosition) || !GUI.enabled)
                return false;

            var references = DragAndDrop.objectReferences;
            if (references == null || references.Length == 0)
                return false;

            var droppedObj = references[0];
            if (droppedObj == null)
                return false;

            var droppedType = GetAcceptedBackgroundImageType(droppedObj);
            if (droppedType == null)
                return false;

            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

            if (evt.type == EventType.DragPerform)
            {
                data.selectedType = droppedType;
                value = droppedObj;
                GUI.changed = true;
                DragAndDrop.AcceptDrag();
                DragAndDrop.activeControlID = 0;
            }

            evt.Use();
            return true;
        }

        private static Type GetAcceptedBackgroundImageType(UnityEngine.Object obj)
        {
            foreach (var type in k_BackgroundImageTypes)
            {
                if (type.IsInstanceOfType(obj))
                    return type;
            }
            return null;
        }

        private void HandleEnumProperty(System.Type enumType, Rect rect, ref object value)
        {
            // The incoming value may be a float encoding an int via BitConverter; convert
            // through Int32 so Enum.ToObject does not reject a boxed float.
            int intValue = Convert.ToInt32(value);

            Rect valueFieldRect = new Rect(rect.xMax - k_ValueFieldWidth - k_ValueFieldOffsetFromRightSide, rect.y, k_ValueFieldWidth, rect.height);

            var enumValue = (Enum)Enum.ToObject(enumType, intValue);
            value = Convert.ToInt32(EditorGUI.EnumPopup(valueFieldRect, GUIContent.none, enumValue, EditorStyles.popup));
        }

        private static readonly GUIContent[] k_LengthUnitOptions =
        {
            new GUIContent("px"),
            new GUIContent("%"),
        };

        private void HandleLengthUnitProperty(Rect rect, ref object value)
        {
            Rect valueFieldRect = new Rect(rect.xMax - k_ValueLengthUnitFieldWidth - k_ValueFieldOffsetFromRightSide, rect.y, k_ValueLengthUnitFieldWidth, rect.height);
            value = EditorGUI.Popup(valueFieldRect, GUIContent.none, Convert.ToInt32(value), k_LengthUnitOptions, EditorStyles.popup);
        }
    }
}
