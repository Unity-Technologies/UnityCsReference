// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: MecanimAnimation not yet converted
using UnityEngine;

namespace UnityEditor
{
    internal class AnimationWindowStyles
    {
        public static readonly Texture2D pointIcon = EditorGUIUtility.LoadIcon("animationkeyframe");

        public static readonly GUIContent playContent = EditorGUIUtility.TrIconContent("Animation.Play", "Play the animation clip.");
        public static readonly GUIContent recordContent = EditorGUIUtility.TrIconContent("Animation.Record", "Enable/disable keyframe recording mode.");
        public static readonly GUIContent previewContent = EditorGUIUtility.TrTextContent("Preview", "Enable/disable scene preview mode.");
        public static readonly GUIContent prevKeyContent = EditorGUIUtility.TrIconContent("Animation.PrevKey", "Go to previous keyframe.");
        public static readonly GUIContent nextKeyContent = EditorGUIUtility.TrIconContent("Animation.NextKey", "Go to next keyframe.");
        public static readonly GUIContent firstKeyContent = EditorGUIUtility.TrIconContent("Animation.FirstKey", "Go to the beginning of the animation clip.");
        public static readonly GUIContent lastKeyContent = EditorGUIUtility.TrIconContent("Animation.LastKey", "Go to the end of the animation clip.");
        public static readonly GUIContent addKeyframeContent = EditorGUIUtility.TrIconContent("Animation.AddKeyframe", "Add keyframe.");
        public static readonly GUIContent addEventContent = EditorGUIUtility.TrIconContent("Animation.AddEvent", "Add event.");
        public static readonly GUIContent filterBySelectionContent = EditorGUIUtility.TrIconContent("Animation.FilterBySelection", "Filter by selection.");
        public static readonly GUIContent sequencerLinkContent = EditorGUIUtility.TrIconContent("Animation.SequencerLink", "Animation Window is linked to Timeline Editor.  Press to Unlink.");

        public static readonly GUIContent noAnimatableObjectSelectedText = EditorGUIUtility.TrTextContent("No animatable object selected.");
        public static readonly GUIContent formatIsMissing = EditorGUIUtility.TrTextContent("To begin animating {0}, create {1}.");
        public static readonly GUIContent animatorAndAnimationClip = EditorGUIUtility.TrTextContent("an Animator and an Animation Clip");
        public static readonly GUIContent animationClip = EditorGUIUtility.TrTextContent("an Animation Clip");
        public static readonly GUIContent create = EditorGUIUtility.TrTextContent("Create");
        public static readonly GUIContent dopesheet = EditorGUIUtility.TrTextContent("Dopesheet");
        public static readonly GUIContent curves = EditorGUIUtility.TrTextContent("Curves");
        public static readonly GUIContent samples = EditorGUIUtility.TrTextContent("Samples");
        public static readonly GUIContent createNewClip = EditorGUIUtility.TrTextContent("Create New Clip...");

        public static readonly GUIContent animatorOptimizedText = EditorGUIUtility.TrTextContent("Editing and playback of animations on optimized game object hierarchy is not supported.\nPlease select a game object that does not have 'Optimize Game Objects' applied.");
        public static readonly GUIContent readOnlyPropertiesLabel = EditorGUIUtility.TrTextContent("Animation Clip is Read-Only");
        public static readonly GUIContent readOnlyPropertiesButton = EditorGUIUtility.TrTextContent("Show Read-Only Properties");

        public static readonly GUIContent applyChanges = EditorGUIUtility.TrTextContent("Apply");
        public static readonly GUIContent discardChanges = EditorGUIUtility.TrTextContent("Discard");

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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
