// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class AnimationWindowStyles
    {
        public static readonly Texture2D pointIcon = EditorGUIUtility.LoadIcon("animationkeyframe");

        public static readonly GUIContent playContent = L10n.IconContent("Animation.Play", "Play the animation clip.", null);
        public static readonly GUIContent recordContent = L10n.IconContent("Animation.Record", "Enable/disable keyframe recording mode.", null);
        public static readonly GUIContent previewContent = L10n.TextContent("Preview", "Enable/disable scene preview mode.", null, null);
        public static readonly GUIContent prevKeyContent = L10n.IconContent("Animation.PrevKey", "Go to previous keyframe.", null);
        public static readonly GUIContent nextKeyContent = L10n.IconContent("Animation.NextKey", "Go to next keyframe.", null);
        public static readonly GUIContent firstKeyContent = L10n.IconContent("Animation.FirstKey", "Go to the beginning of the animation clip.", null);
        public static readonly GUIContent lastKeyContent = L10n.IconContent("Animation.LastKey", "Go to the end of the animation clip.", null);
        public static readonly GUIContent addKeyframeContent = L10n.IconContent("Animation.AddKeyframe", "Add keyframe.", null);
        public static readonly GUIContent addEventContent = L10n.IconContent("Animation.AddEvent", "Add event.", null);
        public static readonly GUIContent filterBySelectionContent = L10n.IconContent("Animation.FilterBySelection", "Filter by selection.", null);
        public static readonly GUIContent sequencerLinkContent = L10n.IconContent("Animation.SequencerLink", "Animation Window is linked to Timeline Editor.  Press to Unlink.", null);

        public static readonly GUIContent noAnimatableObjectSelectedText = L10n.TextContent("No animatable object selected.", null, null, null);
        public static readonly GUIContent formatIsMissing = L10n.TextContent("To begin animating {0}, create {1}.", null, null, null);
        public static readonly GUIContent animatorAndAnimationClip = L10n.TextContent("an Animator and an Animation Clip", null, null, null);
        public static readonly GUIContent animationClip = L10n.TextContent("an Animation Clip", null, null, null);
        public static readonly GUIContent create = L10n.TextContent("Create", null, null, null);
        public static readonly GUIContent dopesheet = L10n.TextContent("Dopesheet", null, null, null);
        public static readonly GUIContent curves = L10n.TextContent("Curves", null, null, null);
        public static readonly GUIContent samples = L10n.TextContent("Samples", null, null, null);
        public static readonly GUIContent createNewClip = L10n.TextContent("Create New Clip...", null, null, null);

        public static readonly GUIContent animatorOptimizedText = L10n.TextContent("Editing and playback of animations on optimized game object hierarchy is not supported.\nPlease select a game object that does not have 'Optimize Game Objects' applied.", null, null, null);
        public static readonly GUIContent readOnlyPropertiesLabel = L10n.TextContent("Animation Clip is Read-Only", null, null, null);
        public static readonly GUIContent readOnlyPropertiesButton = L10n.TextContent("Show Read-Only Properties", null, null, null);

        public static readonly GUIContent applyChanges = L10n.TextContent("Apply", null, null, null);
        public static readonly GUIContent discardChanges = L10n.TextContent("Discard", null, null, null);

        public static readonly GUIContent optionsContent = EditorGUIUtility.IconContent("_Menu");

        public static readonly GUIStyle playHead = "AnimationPlayHead";

        public static readonly GUIStyle animPlayToolBar = "AnimPlayToolbar";
        public static readonly GUIStyle animClipToolBar = "AnimClipToolbar";
        public static readonly GUIStyle animClipToolbarButton = "AnimClipToolbarButton";
        public static readonly GUIStyle animClipToolbarPopup = "AnimClipToolbarPopup";
        public static readonly GUIStyle timeRulerBackground = "TimeRulerBackground";
        public static readonly GUIStyle curveEditorBackground = "CurveEditorBackground";
        public static readonly GUIStyle curveEditorLabelTickmarks = "CurveEditorLabelTickmarks";
        public static readonly GUIStyle eventBackground = "AnimationEventBackground";
        public static readonly GUIStyle eventTooltip = "AnimationEventTooltip";
        public static readonly GUIStyle eventTooltipArrow = "AnimationEventTooltipArrow";
        public static readonly GUIStyle keyframeBackground = "AnimationKeyframeBackground";
        public static readonly GUIStyle timelineTick = "AnimationTimelineTick";
        public static readonly GUIStyle dopeSheetKeyframe = "Dopesheetkeyframe";
        public static readonly GUIStyle dopeSheetBackground = "DopesheetBackground";
        public static readonly GUIStyle popupCurveDropdown = "PopupCurveDropdown";
        public static readonly GUIStyle popupCurveEditorBackground = "PopupCurveEditorBackground";
        public static readonly GUIStyle popupCurveEditorSwatch = "PopupCurveEditorSwatch";
        public static readonly GUIStyle popupCurveSwatchBackground = "PopupCurveSwatchBackground";
        public static readonly GUIStyle separator = new GUIStyle("AnimLeftPaneSeparator");

        public static readonly GUIStyle toolbarBottom = "ToolbarBottom";
        public static readonly GUIStyle optionsButton = new GUIStyle(EditorStyles.toolbarButtonRight);
        public static readonly GUIStyle miniToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
        public static readonly GUIStyle toolbarLabel = new GUIStyle(AnimationWindowStyles.animClipToolbarPopup);

        public static readonly GUIStyle plusButton = "IconButton";
        public static readonly GUIStyle plusButtonBackground = "Tag MenuItem";

        public static void Initialize()
        {
            toolbarLabel.normal.background = null;
            optionsButton.padding = new RectOffset();
            optionsButton.imagePosition = ImagePosition.ImageOnly;
            optionsButton.contentOffset = new Vector2(-7, 0);
        }
    }
}
