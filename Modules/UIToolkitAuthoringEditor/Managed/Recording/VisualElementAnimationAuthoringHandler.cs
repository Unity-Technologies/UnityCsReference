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
using UnityEditor.AnimationWindowBuiltin;
using UnityEditorInternal;
using UnityEngine.TextCore.Text;
using System.Diagnostics.CodeAnalysis;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Singleton handler for UIToolkit VisualElement animation authoring in the Animation Window.
    /// Manages VisualElementSelection objects for proper selection behavior when animating UI elements,
    /// and provides channel grouping and custom value field rendering for UIElements style properties.
    /// </summary>
    internal partial class VisualElementAnimationAuthoringHandler : IAnimationWindowPropertyHandler
    {
        [NoAutoStaticsCleanup] // handler singleton, safe to persist
        private static VisualElementAnimationAuthoringHandler s_Instance;

        private const float k_ValueFieldWidth = 80f;
        private const float k_ValueFieldOffsetFromRightSide = 30f;
        private const float k_ValueLengthUnitFieldWidth = 50f;
        private const float k_ButtonSpacing = 1f;

        [NoAutoStaticsCleanup] // immutable type list, safe to persist
        private static readonly Type[] k_BackgroundImageTypes =
        {
            typeof(Texture2D),
            typeof(RenderTexture),
            typeof(Sprite),
            typeof(VectorImage),
        };

        // FontDefinition accepts either a legacy Font or an SDF FontAsset.
        [NoAutoStaticsCleanup] // immutable type list, safe to persist
        private static readonly Type[] k_FontDefinitionTypes =
        {
            typeof(Font),
            typeof(FontAsset),
        };

        // Base property name -> accepted asset types for PPtr curves whose property accepts several
        // unrelated types. A single EditorCurveBinding can't filter the object field to one type, so
        // these render a type-selecting object field (with an Asset Type context menu) instead.
        [NoAutoStaticsCleanup] // immutable type-by-property map, safe to persist
        private static readonly Dictionary<string, Type[]> k_MultiTypeObjectProperties = new()
        {
            { nameof(StylePropertyId.BackgroundImage), k_BackgroundImageTypes },
            { nameof(StylePropertyId.UnityFontDefinition), k_FontDefinitionTypes },
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

            var selection = VisualElementUtility.GetSelectionObject<VisualElementSelection>(element);

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
        [AutoStaticsCleanupOnCodeReload]
        private static Dictionary<(StylePropertyId, string), int> s_ChannelIndexLookup;

        [AutoStaticsCleanupOnCodeReload]
        private static HashSet<string> s_KnownSubChannelSuffixes;

        // (id, suffix) pairs whose property's only handler-grouped suffix is this one. Used
        // to keep the full property name as the AnimationWindow group label for lone sub-channels
        // (e.g. TextShadow.blurRadius) so the row doesn't collapse to the misleading parent
        // (e.g. plain "TextShadow"). "Handler-grouped" excludes the fast-path-shaped suffixes
        // (ending in .r/.g/.b/.a/.x/.y/.z/.w) that GetPropertyGroupName defers back to the
        // built-in fast path.
        [AutoStaticsCleanupOnCodeReload]
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

            // First pass: per-id count of suffixes this handler groups itself
            // (fast-path-shaped ones are deferred, so they don't count).
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

        // Suffixes shaped like ".<r|g|b|a|x|y|z|w>" (including multi-char ones like
        // ".color.r" or ".offset.x") that AnimationWindowUtility's built-in fast path can
        // group by stripping the trailing 2 chars. GetPropertyGroupName defers these back
        // to the fast path for every property except BackgroundImage.
        private static bool IsFastPathInterceptedSuffix(string suffix)
        {
            if (suffix == null || suffix.Length < 3) return false;
            if (suffix[suffix.Length - 2] != '.') return false;
            char last = suffix[suffix.Length - 1];
            return last == 'r' || last == 'g' || last == 'b' || last == 'a'
                || last == 'x' || last == 'y' || last == 'z' || last == 'w';
        }

        // Curves routed through the UIAnimationBinder (style channels) have no owning component, so
        // the Animation Window drops the type-name prefix - for both per-element UIAnimationClip and
        // panel-wide PanelRenderer clips. A PanelRenderer's own component properties are not binder
        // channels, so they keep the prefix; types this handler does not own also keep the default.
        public bool ShouldPrefixWithTypeName(Type animatableObjectType, string propertyName)
        {
            if (animatableObjectType != typeof(UIAnimationClip) && animatableObjectType != typeof(PanelRenderer))
                return true;
            return !IsStyleChannelBinding(propertyName);
        }

        // True when the base property name maps to a StylePropertyId. Pure managed parse so it never
        // logs, unlike the native attribute resolve which errors on non-style names.
        private static bool IsStyleChannelBinding(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return false;
            string baseName = UIAnimationPath.BaseName(propertyName);
            // Guard against TryParse accepting numeric strings ("5" -> (StylePropertyId)5); names are identifiers.
            if (baseName.Length == 0 || !char.IsLetter(baseName[0]))
                return false;
            return Enum.TryParse<StylePropertyId>(baseName, out var id)
                && id != StylePropertyId.Unknown && id != StylePropertyId.Custom;
        }

        public int GetChannelIndex([NotNull] string propertyName)
        {
            string suffix = UIAnimationPath.SubChannelSuffix(propertyName);
            // The suffix check runs before the enum parse so that the non-style properties
            // this handler now sees first (handlers run before the built-in fast path)
            // bail out on a hash lookup instead of an Enum.TryParse.
            if (suffix == null || !KnownSubChannelSuffixes.Contains(suffix))
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
            string suffix = UIAnimationPath.SubChannelSuffix(propertyName);
            if (suffix == null || !KnownSubChannelSuffixes.Contains(suffix))
                return null;

            // Resolve the property id from the binding name rather than the suffix because
            // the same suffix can belong to several properties (the same disambiguation
            // GetChannelIndex applies).
            string baseName = ExtractBasePropertyName(propertyName, suffix);
            StylePropertyId id = StylePropertyId.Unknown;
            if (!string.IsNullOrEmpty(baseName)
                && !Enum.TryParse(baseName, out id))
            {
                // NicifyPropertyGroupName re-runs grouping on the *display* name, where
                // NicifyVariableName has inserted spaces ("Background Image.gradient.angle").
                // Drop them so the id still resolves and the group rules below apply to
                // display names the same way they applied to the raw binding name.
                Enum.TryParse(baseName.Replace(" ", ""), out id);
            }

            // Filter groups are per-slot (filter.0, filter.1, ...); strip only the trailing
            // ".<sub>" so the ".<i>" slot prefix stays in the group name.
            if (id == StylePropertyId.Filter)
            {
                int secondDot = suffix.IndexOf('.', 1);
                if (secondDot > 0)
                    return propertyName.Substring(0, propertyName.Length - (suffix.Length - secondDot));
            }

            // All BackgroundImage channels (texture + gradient) group under the base
            // property name: one expandable BackgroundImage row in the Animation Window
            // and one "Background Image" entry in the Add Property popup, mirroring the
            // per-slot Filter groups.
            if (id == StylePropertyId.BackgroundImage)
                return propertyName.Substring(0, propertyName.Length - suffix.Length);

            // For every other property, defer fast-path-shaped suffixes (".color.r",
            // ".offset.x", ...) so the built-in grouping (strip the trailing 2 chars)
            // keeps its historical result, e.g. "TextShadow.color".
            if (IsFastPathInterceptedSuffix(suffix))
                return null;

            // Keep the full name for lone sub-channels (e.g. TextShadow.blurRadius) so the
            // AnimationWindow row reads with the suffix instead of collapsing to the parent.
            if (id != StylePropertyId.Unknown && LoneHandlerReachingSuffixes.Contains((id, suffix)))
                return propertyName;

            return propertyName.Substring(0, propertyName.Length - suffix.Length);
        }

        // Channel-suffix -> enum type for EditorGUI.EnumPopup. ".unit" is handled separately
        // since we only expose px / %.
        [NoAutoStaticsCleanup] // built once, safe to persist
        private static readonly Dictionary<string, Type> k_EnumPopupTypeBySuffix = BuildEnumPopupTypeBySuffix();

        private static Dictionary<string, Type> BuildEnumPopupTypeBySuffix()
        {
            var map = new Dictionary<string, Type>
            {
                { ".align", typeof(BackgroundPositionKeyword) },
                { ".type", typeof(BackgroundSizeType) },
                { ".gradient.type", typeof(GradientType) },
                { ".gradient.shape", typeof(BackgroundGradientShape) },
                { ".gradient.size", typeof(BackgroundGradientSize) },
            };
            for (int i = 0; i < UIAnimationBinder.kFilterSlotCount; ++i)
                map["." + i.ToString() + ".type"] = typeof(FilterFunctionType);
            return map;
        }

        internal class MultiTypeObjectHandlerData
        {
            public Type selectedType;
            public bool needsTypeValidation;
        }

        // Returns the accepted asset types for a multi-type PPtr binding (BackgroundImage,
        // FontDefinition), or null for any other property. Matches the trailing property segment so
        // both panel-wide ("BackgroundImage") and per-element ("#elem/BackgroundImage") names resolve.
        internal static Type[] GetMultiTypeObjectTypes(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return null;

            string baseName = UIAnimationPath.Tail(propertyName);
            // Composite PPtr channels carry a ".image" suffix (BackgroundImage.image);
            // legacy clips bound the bare property name.
            if (baseName.EndsWith(".image", StringComparison.Ordinal))
                baseName = baseName.Substring(0, baseName.Length - ".image".Length);
            return k_MultiTypeObjectProperties.TryGetValue(baseName, out var types) ? types : null;
        }

        // One row's parsed binding, kept in the Animation Window's per-node handlerData so the property
        // name is parsed once and reused across the handler's calls for that row. The unparsed name is
        // the cache key: a repair or an undo rewrites the binding under a surviving node, so a mismatch
        // reparses instead of trusting a stale split.
        internal sealed class NodeBindingData
        {
            string m_PropertyName;

            internal string ElementPath;          // null when the name has no path shape
            internal string SubChannelSuffix;
            internal string BasePropertyName;
            internal Type WholePropertyEnumType;  // only ever set when there is no sub-channel suffix
            internal Type[] MultiTypeObjectTypes;
            internal MultiTypeObjectHandlerData MultiType;

            internal void EnsureParsed(string propertyName)
            {
                if (ReferenceEquals(m_PropertyName, propertyName)
                    || string.Equals(m_PropertyName, propertyName, StringComparison.Ordinal))
                    return;

                m_PropertyName = propertyName;
                UIAnimationPath.TrySplitElementPath(propertyName, out var elementPath);
                ElementPath = elementPath;
                SubChannelSuffix = UIAnimationPath.SubChannelSuffix(propertyName);
                BasePropertyName = UIAnimationPath.BaseName(propertyName);
                WholePropertyEnumType =
                    SubChannelSuffix == null && TryGetWholePropertyEnumType(propertyName, out var enumType)
                        ? enumType
                        : null;
                MultiTypeObjectTypes = GetMultiTypeObjectTypes(propertyName);
                MultiType = null;   // the selection state belonged to the previous binding
            }

            // Default to the first accepted type until the user picks one or a value forces it.
            internal MultiTypeObjectHandlerData GetOrCreateMultiType() =>
                MultiType ??= new MultiTypeObjectHandlerData { selectedType = MultiTypeObjectTypes[0] };
        }

        private static NodeBindingData GetNodeData(ref object handlerData, string propertyName)
        {
            if (handlerData is not NodeBindingData data)
                handlerData = data = new NodeBindingData();

            data.EnsureParsed(propertyName);
            return data;
        }

        public bool TryDoValueField(AnimationWindowState state,
            Rect valueFieldRect, Rect valueFieldDragRect, int controlId,
            EditorCurveBinding curveBinding, Type curveValueType, Type animatableObjectType,
            object currentValue, out object newValue, ref object handlerData)
        {
            if (animatableObjectType != typeof(PanelRenderer) && animatableObjectType != typeof(UIAnimationClip))
            {
                newValue = currentValue;
                return false;
            }

            var data = GetNodeData(ref handlerData, curveBinding.propertyName);

            if (TryDrawMissingElementField(state, valueFieldRect, curveBinding, data.ElementPath))
            {
                newValue = currentValue;
                return true;
            }

            string suffix = data.SubChannelSuffix;

            // px / % popup for any Length.unit sub-channel (Length, BackgroundPosition.offset,
            // BackgroundSize.x/.y) and for gradient stop `.positionIsPercent` (0 = px, 1 = %).
            if (suffix != null && (suffix.EndsWith(".unit") || suffix.EndsWith(".positionIsPercent")))
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

            // Whole-property enum channels (e.g. UnityFontStyleAndWeight) carry no sub-channel
            // suffix; render the enum popup so only valid values can be entered.
            if (data.WholePropertyEnumType != null)
            {
                newValue = currentValue;
                HandleEnumProperty(data.WholePropertyEnumType, valueFieldRect, ref newValue);
                return true;
            }

            // BackgroundRepeat.x / .y reuse single-char suffixes shared with Translate / Scale /
            // TransformOrigin / Color, so we disambiguate by resolving the property from the
            // propertyName rather than the suffix alone. ".x" / ".y" are excluded from the
            // channel-index lookup for being single-char (the AnimationWindow's built-in component
            // lookup handles their channel index separately). Strip the element-path prefix
            // ("child/") because per-element bindings carry a path component before the property name.
            if ((suffix == ".x" || suffix == ".y")
                && data.BasePropertyName == nameof(StylePropertyId.BackgroundRepeat))
            {
                newValue = currentValue;
                return true;
            }

            if (data.MultiTypeObjectTypes != null)
            {
                newValue = currentValue;
                HandleMultiTypeObjectValueField(valueFieldRect, controlId, ref newValue, data);
                return true;
            }

            // Single-type PPtr channels (Font, Material, ...) already carry a specific
            // EditorCurveBinding.type from native BindValue, so the AnimationWindow renders the
            // right-typed object field for them without a handler hook.

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
            var tail = UIAnimationPath.Tail(propertyName);
            return tail.Substring(0, tail.Length - suffix.Length);
        }

        public bool TryDisplayPropertyGroup(AnimationWindowState state, Rect valueFieldRect,
            EditorCurveBinding curveBinding, Type curveValueType, Type animatableObjectType,
            ref object handlerData)
        {
            if (animatableObjectType != typeof(PanelRenderer) && animatableObjectType != typeof(UIAnimationClip))
                return false;

            // Every curve under a group shares the group's element path, so the group is broken exactly
            // when the curve standing in for it is - which is what lets a collapsed group answer for the
            // children it is hiding.
            var data = GetNodeData(ref handlerData, curveBinding.propertyName);
            return TryDrawMissingElementField(state, valueFieldRect, curveBinding, data.ElementPath);
        }

        public bool TryPopulateContextMenu(AnimationWindowState state, GenericMenu menu,
            EditorCurveBinding curveBinding, Type curveValueType, Type animatableObjectType,
            object handlerData, Action<object> setHandlerData)
        {
            if (animatableObjectType != typeof(PanelRenderer) && animatableObjectType != typeof(UIAnimationClip))
                return false;

            var addedRepairItems = TryPopulateRepairMenu(state, menu, curveBinding);

            var data = GetNodeData(ref handlerData, curveBinding.propertyName);
            var types = data.MultiTypeObjectTypes;
            if (types == null)
                return addedRepairItems;

            var multiType = data.GetOrCreateMultiType();
            var currentType = multiType.selectedType;

            menu.AddSeparator("");
            foreach (var type in types)
            {
                var capturedType = type;
                menu.AddItem(
                    new GUIContent("Asset Type/" + ObjectNames.NicifyVariableName(type.Name)),
                    currentType == type,
                    () =>
                    {
                        multiType.selectedType = capturedType;
                        if (capturedType != currentType)
                            multiType.needsTypeValidation = true;
                        setHandlerData(data);
                    });
            }

            return true;
        }

        // --- Broken-binding repair -------------------------------------------------------

        [AutoStaticsCleanupOnCodeReload]
        static GUIContent s_MissingElementContent;

        // What a row belongs to: the binder that can judge it, and the clip a repair would rewrite.
        readonly struct RemapRow
        {
            internal readonly UIToolkitAnimationSelectionItemBase UISelection;
            internal readonly UIAnimationBinder Binder;
            internal readonly AnimationClip Clip;
            internal readonly UIAnimationClip UIClip;

            internal RemapRow(UIToolkitAnimationSelectionItemBase uiSelection,
                UIAnimationBinder binder, AnimationClip clip, UIAnimationClip uiClip)
            {
                UISelection = uiSelection;
                Binder = binder;
                Clip = clip;
                UIClip = uiClip;
            }

            internal bool IsValid => Binder != null;
        }

        /// <summary>
        /// The binder and clip behind a row of the invoking selection, or false when the selection
        /// cannot account for it.
        /// </summary>
        // A UIAnimationClip row belongs to a UI Toolkit selection; a PanelRenderer row belongs to a
        // selection whose animated GameObject actually holds the PanelRenderer the binding names.
        static bool TryResolveRow(AnimationWindowState state, EditorCurveBinding curveBinding,
            out RemapRow row)
        {
            row = default;
            var selection = state?.selection;

            if (selection == null)
                return false;

            if (selection is UIToolkitAnimationSelectionItemBase uiSelection)
            {
                if (curveBinding.type != typeof(UIAnimationClip))
                    return false;

                var uiClip = uiSelection.uiAnimationClip;
                row = new RemapRow(uiSelection,
                    UIAnimationBindingResolution.GetBinderFor(uiSelection),
                    uiClip != null ? uiClip.animationClip : null, uiClip);

                return row.IsValid;
            }

            if (curveBinding.type != typeof(PanelRenderer))
                return false;

            var root = selection.rootGameObject;
            if (root == null ||
                AnimationUtility.GetAnimatedObject(root, curveBinding) is not PanelRenderer renderer)
                return false;

            row = new RemapRow(null, renderer.GetOrCreateAnimationBinder(),
                (selection.clip as AnimationWindowClip)?.animationClip, null);

            return row.IsValid;
        }

        // The value column is where a broken row is flagged, because the value itself is meaningless
        // once the element is gone. It is also the only route that needs no AnimationWindow change: the
        // built-in "(Missing!)" decoration sits behind an IsNodeLeftOverCurve check that returns early
        // on a null rootGameObject, which no UI Toolkit selection can satisfy.
        bool TryDrawMissingElementField(AnimationWindowState state,
            Rect valueFieldRect, EditorCurveBinding curveBinding, string elementPath)
        {
            if (!TryResolveRow(state, curveBinding, out var row))
                return false;

            if (UIAnimationBindingResolution.ResolveElementPath(row.Binder, elementPath) != BindingResolution.Broken)
                return false;

            s_MissingElementContent ??= new GUIContent(
                L10n.Tr("Missing"),
                EditorGUIUtility.IconContent("console.warnicon.sml").image,
                L10n.Tr("The element this curve animates no longer exists. Right-click the row to remap the curve to another element."));

            EditorGUI.LabelField(valueFieldRect, s_MissingElementContent);
            return true;
        }

        bool TryPopulateRepairMenu(AnimationWindowState state, GenericMenu menu,
            EditorCurveBinding curveBinding)
        {
            if (!TryResolveRow(state, curveBinding, out var row))
                return false;

            var binder = CanonicalBinderFor(row);
            if (!UIAnimationCurveRepair.TryGetOptions(row.Clip, binder, curveBinding.propertyName,
                    out var options))
                return false;

            menu.AddSeparator("");

            if (options.IsReadOnly)
            {
                menu.AddDisabledItem(new GUIContent(L10n.Tr("Remap to (clip is read-only)")));
                return true;
            }

            // Taken while the menu is being built: a GenericMenu callback runs after the click, when
            // Event.current no longer describes where the user actually clicked.
            var activator = new Rect(GUIUtility.GUIToScreenPoint(Event.current.mousePosition), Vector2.zero);
            var binding = curveBinding;

            menu.AddItem(
                new GUIContent(L10n.Tr("Remap to...")),
                false,
                () => ShowRemapPicker(state, binding, activator));

            return true;
        }

        // Asked for directly rather than taken from the draw-path cache: a repair aimed at a stale
        // canonical element would rewrite the wrong tree. A PanelRenderer row has no canonical element -
        // its binder is the panel's own.
        static UIAnimationBinder CanonicalBinderFor(in RemapRow row) =>
            row.UISelection != null ? row.UISelection.GetCanonicalBinder() : row.Binder;

        static void ShowRemapPicker(AnimationWindowState state, EditorCurveBinding curveBinding,
            Rect activator)
        {
            if (!TryResolveRow(state, curveBinding, out var row))
                return;

            var binder = CanonicalBinderFor(row);
            if (!UIAnimationCurveRepair.TryGetOptions(row.Clip, binder, curveBinding.propertyName,
                    out var options))
                return;

            UIAnimationCurveRemapWindow.Show(activator, binder, row.Clip, row.UIClip, options,
                targetPath => ApplyPickedTarget(state, curveBinding, targetPath));
        }

        // Picking is not permission to write. The popup is dismissable and outlives the click that opened
        // it, so the row is resolved again and re-checked before anything moves.
        static void ApplyPickedTarget(AnimationWindowState state, EditorCurveBinding curveBinding,
            string targetPath)
        {
            if (!TryResolveRow(state, curveBinding, out var row))
                return;

            if (!UIAnimationCurveRepair.TryGetOptions(row.Clip, CanonicalBinderFor(row),
                    curveBinding.propertyName, out var options) || options.IsReadOnly)
                return;

            // The popup outlives the click that opened it, so the clip may have gained a curve where the
            // picked subtree would land; applying anyway would leave the repair half-done.
            if (UIAnimationCurveRepair.WouldCollide(row.Clip, options.BrokenElementPath, targetPath))
                return;

            // The rows follow on their own: rewriting the curves raises onCurveWasModified, which the
            // selection item turns into a refresh of the window it belongs to.
            UIAnimationCurveRepair.Apply(row.Clip, options.BrokenElementPath, targetPath);
        }

        public bool TryValidateDragDrop(EditorCurveBinding curveBinding, Type curveValueType,
            Type animatableObjectType, UnityEngine.Object[] draggedObjects,
            out UnityEngine.Object[] validatedReferences)
        {
            validatedReferences = null;

            if (animatableObjectType != typeof(PanelRenderer) && animatableObjectType != typeof(UIAnimationClip))
                return false;

            var types = GetMultiTypeObjectTypes(curveBinding.propertyName);
            if (types == null)
                return false;

            foreach (var obj in draggedObjects)
            {
                if (obj == null || GetAcceptedType(types, obj) == null)
                    return false;
            }

            validatedReferences = draggedObjects;
            return true;
        }

        private void HandleMultiTypeObjectValueField(Rect valueFieldRect, int controlId, ref object value, NodeBindingData nodeData)
        {
            var types = nodeData.MultiTypeObjectTypes;
            var data = nodeData.GetOrCreateMultiType();
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

            if (TryHandleMultiTypeObjectDragAndDrop(valueFieldRect, types, data, ref value))
                return;

            value = EditorGUI.ObjectField(valueFieldRect, value as UnityEngine.Object, currentObjectType, false);
        }

        private static bool TryHandleMultiTypeObjectDragAndDrop(Rect dropRect, Type[] types, MultiTypeObjectHandlerData data, ref object value)
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

            var droppedType = GetAcceptedType(types, droppedObj);
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

        private static Type GetAcceptedType(Type[] types, UnityEngine.Object obj)
        {
            foreach (var type in types)
            {
                if (type.IsInstanceOfType(obj))
                    return type;
            }
            return null;
        }

        internal static bool TryGetWholePropertyEnumType(string propertyName, out Type enumType)
        {
            enumType = null;
            if (string.IsNullOrEmpty(propertyName))
                return false;

            // Per-element bindings carry an element-path prefix ("#elem/UnityFontStyleAndWeight").
            var baseName = UIAnimationPath.Tail(propertyName);
            // Guard against TryParse accepting numeric strings ("1" -> StylePropertyId.AlignContent); names are identifiers.
            if (baseName.Length == 0 || !char.IsLetter(baseName[0]))
                return false;

            if (!Enum.TryParse(baseName, out StylePropertyId id))
                return false;

            if (AnimationRecordingStyleBridge.GetRecordingChannelKind(id) != StylePropertyRecordingChannel.EnumInt)
                return false;

            var type = StyleDebug.GetComputedStyleType(id);
            if (type == null || !type.IsEnum)
                return false;

            enumType = type;
            return true;
        }

        private void HandleEnumProperty(System.Type enumType, Rect rect, ref object value)
        {
            // Value may be a float encoding an int via BitConverter; route through Int32.
            // Clamp through double: an out-of-range keyframe value must not overflow the conversion.
            int intValue = (int)Math.Clamp(Convert.ToDouble(value), int.MinValue, int.MaxValue);

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
