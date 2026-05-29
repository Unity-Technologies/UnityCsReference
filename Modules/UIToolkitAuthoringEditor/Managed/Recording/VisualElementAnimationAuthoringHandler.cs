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

        // (id, suffix) -> channel index, built lazily from the generator's channel-suffix tables.
        // Keying by property id disambiguates suffixes shared across properties (e.g. ".x.value"
        // appears in BackgroundSize.x and BackgroundPosition.offset). Single-char component
        // suffixes (.x, .r, ...) are intentionally skipped; AnimationWindowUtility.GetComponentIndex
        // handles those.
        private static Dictionary<(StylePropertyId, string), int> s_ChannelIndexLookup;

        private static HashSet<string> s_KnownSubChannelSuffixes;

        // (id, suffix) pairs whose property's only handler-reaching suffix is this one. Used
        // to keep the full property name as the AnimationWindow group label for lone sub-channels
        // (e.g. TextShadow.blurRadius) so the row doesn't collapse to the misleading parent
        // (e.g. plain "TextShadow"). "Handler-reaching" excludes the 2-char fast-path suffixes
        // (.r/.g/.b/.a/.x/.y/.z/.w) that AnimationWindowUtility strips before consulting handlers.
        private static HashSet<(StylePropertyId, string)> s_LoneHandlerReachingSuffixes;

        private static Dictionary<(StylePropertyId, string), int> ChannelIndexLookup
        {
            get
            {
                EnsureLookups();
                return s_ChannelIndexLookup;
            }
        }

        private static HashSet<string> KnownSubChannelSuffixes
        {
            get
            {
                EnsureLookups();
                return s_KnownSubChannelSuffixes;
            }
        }

        private static HashSet<(StylePropertyId, string)> LoneHandlerReachingSuffixes
        {
            get
            {
                EnsureLookups();
                return s_LoneHandlerReachingSuffixes;
            }
        }

        private static void EnsureLookups()
        {
            if (s_ChannelIndexLookup != null)
                return;

            int idCount = UIAnimationBinder.StylePropertyIdCount;

            // First pass: per-id count of suffixes that actually reach this handler at
            // grouping time (fast-path-intercepted ones don't, so they don't count).
            var handlerReachingCount = new int[idCount];
            for (int i = 0; i < idCount; i++)
            {
                var suffixes = UIAnimationBinder.GetChannelSuffixes((StylePropertyId)i);
                for (int c = 0; c < suffixes.Count; c++)
                {
                    string suffix = suffixes[c];
                    if (string.IsNullOrEmpty(suffix)) continue;
                    if (IsSingleCharComponentSuffix(suffix)) continue;
                    if (IsFastPathInterceptedSuffix(suffix)) continue;
                    handlerReachingCount[i]++;
                }
            }

            var indexDict = new Dictionary<(StylePropertyId, string), int>();
            var knownSuffixes = new HashSet<string>();
            var loneSuffixes = new HashSet<(StylePropertyId, string)>();
            for (int i = 0; i < idCount; i++)
            {
                var id = (StylePropertyId)i;
                var suffixes = UIAnimationBinder.GetChannelSuffixes(id);
                bool kindHasLoneHandlerReachingChannel = handlerReachingCount[i] == 1;
                for (int c = 0; c < suffixes.Count; c++)
                {
                    string suffix = suffixes[c];
                    if (string.IsNullOrEmpty(suffix)) continue;
                    if (IsSingleCharComponentSuffix(suffix)) continue;

                    indexDict[(id, suffix)] = c;
                    knownSuffixes.Add(suffix);

                    if (kindHasLoneHandlerReachingChannel && !IsFastPathInterceptedSuffix(suffix))
                        loneSuffixes.Add((id, suffix));
                }
            }
            s_ChannelIndexLookup = indexDict;
            s_KnownSubChannelSuffixes = knownSuffixes;
            s_LoneHandlerReachingSuffixes = loneSuffixes;
        }

        // Single-char channel components like ".x", ".r", ".w" are resolved by
        // AnimationWindowUtility.GetComponentIndex directly and do not need a handler hook.
        private static bool IsSingleCharComponentSuffix(string suffix) =>
            suffix.Length == 2 && suffix[0] == '.';

        // AnimationWindowUtility strips any ".<r|g|b|a|x|y|z|w>" suffix BEFORE consulting
        // handlers, so suffixes matching that shape (including multi-char ones like
        // ".color.r" or ".offset.x") never reach our GetPropertyGroupName.
        private static bool IsFastPathInterceptedSuffix(string suffix)
        {
            if (suffix == null || suffix.Length < 3) return false;
            if (suffix[suffix.Length - 2] != '.') return false;
            char last = suffix[suffix.Length - 1];
            return last == 'r' || last == 'g' || last == 'b' || last == 'a'
                || last == 'x' || last == 'y' || last == 'z' || last == 'w';
        }

        // Returns the suffix starting at the first '.' after the trailing path segment
        // (e.g. ".offset.value") or null when there is no sub-channel.
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

            string baseName = ExtractBasePropertyName(propertyName, suffix);
            if (string.IsNullOrEmpty(baseName))
                return -1;

            if (!Enum.TryParse<StylePropertyId>(baseName, out var id) || id == StylePropertyId.Unknown)
                return -1;

            if (!ChannelIndexLookup.TryGetValue((id, suffix), out var channelIndex))
                return -1;

            // Filter slots present as independent groups (filter.0, filter.1, ...) so we
            // report the slot-local sub-index (0..17), not the flat 0..71 channel index.
            if (id == StylePropertyId.Filter)
                return channelIndex % UIAnimationBinder.kFilterChannelsPerSlot;

            return channelIndex;
        }

        public string GetPropertyGroupName([NotNull] string propertyName)
        {
            string suffix = ExtractSubChannelSuffix(propertyName);
            if (suffix == null || !KnownSubChannelSuffixes.Contains(suffix))
                return null;

            // Resolve the property id from the binding name rather than the suffix because
            // the same suffix can belong to several properties (the same disambiguation
            // GetChannelIndex applies).
            string baseName = ExtractBasePropertyName(propertyName, suffix);
            StylePropertyId id = StylePropertyId.Unknown;
            if (!string.IsNullOrEmpty(baseName))
                Enum.TryParse(baseName, out id);

            // Filter groups are per-slot (filter.0, filter.1, ...); strip only the trailing
            // ".<sub>" so the ".<i>" slot prefix stays in the group name.
            if (id == StylePropertyId.Filter)
            {
                int secondDot = suffix.IndexOf('.', 1);
                if (secondDot > 0)
                    return propertyName.Substring(0, propertyName.Length - (suffix.Length - secondDot));
            }

            // Keep the full name for lone sub-channels (e.g. TextShadow.blurRadius) so the
            // AnimationWindow row reads with the suffix instead of collapsing to the parent.
            if (id != StylePropertyId.Unknown && LoneHandlerReachingSuffixes.Contains((id, suffix)))
                return propertyName;

            return propertyName.Substring(0, propertyName.Length - suffix.Length);
        }

        // Channel-suffix -> enum type for EditorGUI.EnumPopup. ".unit" is handled separately
        // since we only expose px / %.
        private static readonly Dictionary<string, Type> k_EnumPopupTypeBySuffix = BuildEnumPopupTypeBySuffix();

        private static Dictionary<string, Type> BuildEnumPopupTypeBySuffix()
        {
            var map = new Dictionary<string, Type>
            {
                { ".align", typeof(BackgroundPositionKeyword) },
                { ".type", typeof(BackgroundSizeType) },
            };
            for (int i = 0; i < UIAnimationBinder.kFilterSlotCount; ++i)
                map["." + i.ToString() + ".type"] = typeof(FilterFunctionType);
            return map;
        }

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
            if (animatableObjectType != typeof(PanelRenderer) && animatableObjectType != typeof(UIAnimationClip))
            {
                newValue = currentValue;
                return false;
            }

            var propName = curveBinding.propertyName;
            string suffix = ExtractSubChannelSuffix(propName);

            // px / % popup for any Length.unit sub-channel (Length, BackgroundPosition.offset, BackgroundSize.x/.y).
            if (suffix != null && suffix.EndsWith(".unit"))
            {
                newValue = currentValue;
                HandleLengthUnitProperty(valueFieldRect, ref newValue);
                return true;
            }

            if (suffix != null && k_EnumPopupTypeBySuffix.TryGetValue(suffix, out var enumType))
            {
                newValue = currentValue;
                HandleEnumProperty(enumType, valueFieldRect, ref newValue);
                return true;
            }

            // BackgroundRepeat.x / .y reuse single-char suffixes shared with Translate / Scale /
            // TransformOrigin / Color, so we disambiguate by resolving the property from the
            // propertyName rather than the suffix alone. ".x" / ".y" are excluded from the
            // channel-index lookup for being single-char (the AnimationWindow's built-in component
            // lookup handles their channel index separately). Strip the element-path prefix
            // ("child/") because per-element bindings carry a path component before the property name.
            if (suffix == ".x" || suffix == ".y")
            {
                var basePropertyName = ExtractBasePropertyName(propName, suffix);
                if (basePropertyName == nameof(StylePropertyId.BackgroundRepeat))
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
            // yet. UIAnimationBinder.GetChannelKind already carries the metadata needed to
            // wire this up once the first PPtr composite lands.

            newValue = currentValue;
            return false;
        }

        // Returns the bare property name (last path segment without sub-channel suffix)
        // from a binding's propertyName. Handles both panel-wide and per-element shapes:
        //   "BackgroundRepeat.x"            -> "BackgroundRepeat"
        //   "child/BackgroundRepeat.x"      -> "BackgroundRepeat"
        //   "panel/sub/BackgroundRepeat.x"  -> "BackgroundRepeat"
        private static string ExtractBasePropertyName(string propertyName, string suffix)
        {
            int suffixStart = propertyName.Length - suffix.Length;
            int lastSlash = propertyName.LastIndexOf('/', suffixStart - 1);
            int baseStart = lastSlash + 1;
            return propertyName.Substring(baseStart, suffixStart - baseStart);
        }

        public bool TryPopulateContextMenu(GenericMenu menu,
            EditorCurveBinding curveBinding, Type curveValueType, Type animatableObjectType,
            object handlerData, Action<object> setHandlerData)
        {
            if (animatableObjectType != typeof(PanelRenderer) && animatableObjectType != typeof(UIAnimationClip))
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

            if (animatableObjectType != typeof(PanelRenderer) && animatableObjectType != typeof(UIAnimationClip))
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
            // Value may be a float encoding an int via BitConverter; route through Int32.
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
