// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.UIToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using AnimationChangeType = Unity.UIToolkit.Editor.StyleAnimationListView.AnimationChangeType;

namespace Unity.UI.Builder
{
    partial class BuilderInspectorStyleFields
    {
        public void BindStyleField(BuilderStyleRow styleRow, StyleAnimationListView animationListView)
        {
            GetOrCreateFieldListForStyleName(AnimationStyleNames.Animation).Add(animationListView);

            // The Animation window edits the clip on the global selection, which the Builder does not change,
            // so its rows offer creation only until the window's context can be driven from here.
            animationListView.canEditClipsInAnimationWindow = false;

            // The authoring host gets its per-longhand affordance menus from StylePropertyBinding; the Builder
            // has no such binding, so it supplies the Unset menu for each longhand here (applied on row bind).
            animationListView.SetLonghandContextMenu(StylePropertyId.AnimationNames, m => BuildAnimationLonghandMenu(m, AnimationStyleNames.Clip));
            animationListView.SetLonghandContextMenu(StylePropertyId.AnimationDuration, m => BuildAnimationLonghandMenu(m, AnimationStyleNames.Duration));
            animationListView.SetLonghandContextMenu(StylePropertyId.AnimationDelay, m => BuildAnimationLonghandMenu(m, AnimationStyleNames.Delay));
            animationListView.SetLonghandContextMenu(StylePropertyId.AnimationIterationCount, m => BuildAnimationLonghandMenu(m, AnimationStyleNames.IterationCount));
            animationListView.SetLonghandContextMenu(StylePropertyId.AnimationDirection, m => BuildAnimationLonghandMenu(m, AnimationStyleNames.Direction));
            animationListView.SetLonghandContextMenu(StylePropertyId.AnimationPlayStates, m => BuildAnimationLonghandMenu(m, AnimationStyleNames.PlayState));

            animationListView.RegisterCallback<AnimationLonghandListChangedEvent, StyleAnimationListView>(OnAnimationLonghandsChanged, animationListView);
        }

        public void RefreshStyleField(StyleAnimationListView animationListView)
        {
            using var data = new BuilderAnimationData(styleSheet, currentRule, currentVisualElement, m_Inspector.document.fileSettings.editorExtensionMode);

            var clips = ListPool<UIAnimationClip>.Get();
            var durations = ListPool<float>.Get();
            var delays = ListPool<float>.Get();
            var iterationCounts = ListPool<AnimationIterationCount>.Get();
            var directions = ListPool<AnimationDirection>.Get();
            var playStates = ListPool<AnimationPlayState>.Get();
            try
            {
                ReadClipList(data.clip, clips);
                ReadFloatList(data.duration, durations);
                ReadFloatList(data.delay, delays);
                ReadIterationCountList(data.iterationCount, iterationCounts);
                ReadDirectionList(data.direction, directions);
                ReadPlayStateList(data.playState, playStates);

                // The Builder always shows at least one row so the clip field (and its "New..." button) exist
                // even when nothing is authored; seed a single default entry when every longhand is unset.
                if (clips.Count == 0 && durations.Count == 0 && delays.Count == 0 &&
                    iterationCounts.Count == 0 && directions.Count == 0 && playStates.Count == 0)
                {
                    clips.Add(null);
                    durations.Add(0f);
                    delays.Add(0f);
                    iterationCounts.Add(new AnimationIterationCount(1f));
                    directions.Add(AnimationDirection.Normal);
                    playStates.Add(AnimationPlayState.Running);
                }

                // The control owns the max-count + CSS wrap semantics, so the raw per-longhand lists are pushed
                // as-is and it composes the rows.
                animationListView.SetLonghandListsWithoutNotify(clips, durations, delays, iterationCounts, directions, playStates);
                animationListView.overrides = GetCurrentAnimationOverrides();
            }
            finally
            {
                ListPool<UIAnimationClip>.Release(clips);
                ListPool<float>.Release(durations);
                ListPool<float>.Release(delays);
                ListPool<AnimationIterationCount>.Release(iterationCounts);
                ListPool<AnimationDirection>.Release(directions);
                ListPool<AnimationPlayState>.Release(playStates);
            }
        }

        // ---- Reading ---------------------------------------------------------------------------------

        static void ReadClipList(StylePropertyManipulator manipulator, List<UIAnimationClip> result)
        {
            if (null == manipulator.styleProperty)
                return;
            var count = manipulator.GetValuesCount();
            for (var i = 0; i < count; ++i)
            {
                var value = manipulator.GetValueContextAtIndex(i);
                result.Add(value.handle.valueType == StyleValueType.AssetReference
                    ? value.sheet.ReadAssetReference(value.handle) as UIAnimationClip
                    : null);
            }
        }

        static void ReadFloatList(StylePropertyManipulator manipulator, List<float> result)
        {
            if (null == manipulator.styleProperty)
                return;
            var count = manipulator.GetValuesCount();
            for (var i = 0; i < count; ++i)
            {
                var value = manipulator.GetValueContextAtIndex(i);
                switch (value.handle.valueType)
                {
                    case StyleValueType.Float:
                        result.Add(value.sheet.ReadFloat(value.handle));
                        break;
                    case StyleValueType.Dimension:
                        result.Add(value.sheet.ReadDimension(value.handle).value);
                        break;
                    default:
                        result.Add(0f);
                        break;
                }
            }
        }

        static void ReadIterationCountList(StylePropertyManipulator manipulator, List<AnimationIterationCount> result)
        {
            if (null == manipulator.styleProperty)
                return;
            var count = manipulator.GetValuesCount();
            for (var i = 0; i < count; ++i)
            {
                var value = manipulator.GetValueContextAtIndex(i);
                switch (value.handle.valueType)
                {
                    case StyleValueType.Float:
                        result.Add(new AnimationIterationCount(value.sheet.ReadFloat(value.handle)));
                        break;
                    case StyleValueType.Dimension:
                        result.Add(new AnimationIterationCount(value.sheet.ReadDimension(value.handle).value));
                        break;
                    case StyleValueType.Enum when string.Equals(value.sheet.ReadEnum(value.handle), "infinite", System.StringComparison.OrdinalIgnoreCase):
                        result.Add(AnimationIterationCount.Infinite());
                        break;
                    default:
                        result.Add(new AnimationIterationCount(1f));
                        break;
                }
            }
        }

        static void ReadDirectionList(StylePropertyManipulator manipulator, List<AnimationDirection> result)
        {
            if (null == manipulator.styleProperty)
                return;
            var count = manipulator.GetValuesCount();
            for (var i = 0; i < count; ++i)
            {
                var value = manipulator.GetValueContextAtIndex(i);
                result.Add(value.handle.valueType == StyleValueType.Enum &&
                    TryParseUssEnum<AnimationDirection>(value.sheet.ReadEnum(value.handle), out var direction)
                        ? direction
                        : AnimationDirection.Normal);
            }
        }

        static void ReadPlayStateList(StylePropertyManipulator manipulator, List<AnimationPlayState> result)
        {
            if (null == manipulator.styleProperty)
                return;
            var count = manipulator.GetValuesCount();
            for (var i = 0; i < count; ++i)
            {
                var value = manipulator.GetValueContextAtIndex(i);
                result.Add(value.handle.valueType == StyleValueType.Enum &&
                    TryParseUssEnum<AnimationPlayState>(value.sheet.ReadEnum(value.handle), out var playState)
                        ? playState
                        : AnimationPlayState.Running);
            }
        }

        static bool TryParseUssEnum<T>(string ussValue, out T value) where T : struct
        {
            // USS enum values are dash-cased (e.g. "alternate-reverse"); the C# enum is PascalCase.
            return System.Enum.TryParse(ussValue.Replace("-", string.Empty), true, out value);
        }

        // ---- Writing ---------------------------------------------------------------------------------

        void OnAnimationLonghandsChanged(AnimationLonghandListChangedEvent evt, StyleAnimationListView animationListView)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            s_StyleChangeList.Clear();

            if (evt.cleared)
            {
                // Removing the last row deletes every longhand; RefreshStyleField then re-seeds the placeholder
                // default row from the (now empty) StyleSheet.
                RemoveAnimationProperty(AnimationStyleNames.Clip);
                RemoveAnimationProperty(AnimationStyleNames.Duration);
                RemoveAnimationProperty(AnimationStyleNames.Delay);
                RemoveAnimationProperty(AnimationStyleNames.IterationCount);
                RemoveAnimationProperty(AnimationStyleNames.Direction);
                RemoveAnimationProperty(AnimationStyleNames.PlayState);
                s_StyleChangeList.Add(AnimationStyleNames.Animation);
                NotifyStyleChanges(s_StyleChangeList, true);
                animationListView.overrides = AnimationChangeType.None;
                return;
            }

            // A structural change (add/remove) touches every authored longhand list; an in-place edit only the
            // one longhand the event names. The control's public getters already hold the fresh lists.
            var toWrite = evt.structural ? GetCurrentAnimationOverrides() | evt.changeType : evt.changeType;

            if ((toWrite & AnimationChangeType.Clip) != 0) { WriteClipList(animationListView.animationNames); s_StyleChangeList.Add(AnimationStyleNames.Clip); }
            if ((toWrite & AnimationChangeType.Duration) != 0) { WriteFloatList(AnimationStyleNames.Duration, animationListView.animationDuration); s_StyleChangeList.Add(AnimationStyleNames.Duration); }
            if ((toWrite & AnimationChangeType.Delay) != 0) { WriteFloatList(AnimationStyleNames.Delay, animationListView.animationDelay); s_StyleChangeList.Add(AnimationStyleNames.Delay); }
            if ((toWrite & AnimationChangeType.IterationCount) != 0) { WriteIterationCountList(animationListView.animationIterationCount); s_StyleChangeList.Add(AnimationStyleNames.IterationCount); }
            if ((toWrite & AnimationChangeType.Direction) != 0) { WriteDirectionList(animationListView.animationDirection); s_StyleChangeList.Add(AnimationStyleNames.Direction); }
            if ((toWrite & AnimationChangeType.PlayState) != 0) { WritePlayStateList(animationListView.animationPlayStates); s_StyleChangeList.Add(AnimationStyleNames.PlayState); }

            // Notify with the animation shorthand on add/remove so the list view rebuilds its rows (the per-
            // longhand names route to a no-op refresh — see IsAnimationId). An in-place edit keeps its rows.
            if (evt.structural)
                s_StyleChangeList.Add(AnimationStyleNames.Animation);

            NotifyStyleChanges(s_StyleChangeList, true);
            animationListView.overrides = GetCurrentAnimationOverrides();
        }

        AnimationChangeType GetCurrentAnimationOverrides()
        {
            var overrides = AnimationChangeType.None;
            if (GetLastStyleProperty(currentRule, AnimationStyleNames.Clip) != null)
                overrides |= AnimationChangeType.Clip;
            if (GetLastStyleProperty(currentRule, AnimationStyleNames.Duration) != null)
                overrides |= AnimationChangeType.Duration;
            if (GetLastStyleProperty(currentRule, AnimationStyleNames.Delay) != null)
                overrides |= AnimationChangeType.Delay;
            if (GetLastStyleProperty(currentRule, AnimationStyleNames.IterationCount) != null)
                overrides |= AnimationChangeType.IterationCount;
            if (GetLastStyleProperty(currentRule, AnimationStyleNames.Direction) != null)
                overrides |= AnimationChangeType.Direction;
            if (GetLastStyleProperty(currentRule, AnimationStyleNames.PlayState) != null)
                overrides |= AnimationChangeType.PlayState;
            return overrides;
        }

        void WriteClipList(List<UIAnimationClip> clips)
        {
            // An all-null clip list (e.g. the seeded default row, or rows added before a clip is picked) writes
            // no animation-name property rather than a list of `none` keywords.
            if (clips == null || clips.Count == 0 || clips.TrueForAll(c => c == null))
            {
                RemoveAnimationProperty(AnimationStyleNames.Clip);
                return;
            }
            GetOrCreateStylePropertyByStyleName(AnimationStyleNames.Clip).SetUIAnimationClipList(styleSheet, clips);
        }

        void WriteFloatList(string styleName, List<float> values)
        {
            if (values == null || values.Count == 0)
            {
                RemoveAnimationProperty(styleName);
                return;
            }
            GetOrCreateStylePropertyByStyleName(styleName).SetFloatList(styleSheet, values);
        }

        void WriteIterationCountList(List<AnimationIterationCount> values)
        {
            if (values == null || values.Count == 0)
            {
                RemoveAnimationProperty(AnimationStyleNames.IterationCount);
                return;
            }
            GetOrCreateStylePropertyByStyleName(AnimationStyleNames.IterationCount).SetAnimationIterationCountList(styleSheet, values);
        }

        void WriteDirectionList(List<AnimationDirection> values)
        {
            if (values == null || values.Count == 0)
            {
                RemoveAnimationProperty(AnimationStyleNames.Direction);
                return;
            }
            GetOrCreateStylePropertyByStyleName(AnimationStyleNames.Direction).SetAnimationDirectionList(styleSheet, values);
        }

        void WritePlayStateList(List<AnimationPlayState> values)
        {
            if (values == null || values.Count == 0)
            {
                RemoveAnimationProperty(AnimationStyleNames.PlayState);
                return;
            }
            GetOrCreateStylePropertyByStyleName(AnimationStyleNames.PlayState).SetAnimationPlayStateList(styleSheet, values);
        }

        void RemoveAnimationProperty(string styleName)
        {
            var styleProperty = GetLastStyleProperty(currentRule, styleName);
            if (styleProperty != null)
                styleSheet.RemoveProperty(currentRule, styleProperty);
        }

        void BuildAnimationLonghandMenu(DropdownMenu menu, string styleName)
        {
            if (menu.MenuItems() != null && menu.MenuItems().Count > 0)
                return;

            var isSet = GetLastStyleProperty(currentRule, styleName) != null;
            menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                _ =>
                {
                    RemoveAnimationProperty(styleName);
                    s_StyleChangeList.Clear();
                    s_StyleChangeList.Add(styleName);
                    s_StyleChangeList.Add(AnimationStyleNames.Animation);
                    NotifyStyleChanges(s_StyleChangeList, true);
                },
                _ => isSet ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }
    }
}
