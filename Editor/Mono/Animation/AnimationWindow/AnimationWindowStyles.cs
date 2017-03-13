// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class AnimationWindowStyles
    {
        public static Texture2D pointIcon = EditorGUIUtility.LoadIcon("animationkeyframe");

        public static GUIContent playContent = EditorGUIUtility.IconContent("Animation.Play", "|Play the animation clip.");
        public static GUIContent recordContent = EditorGUIUtility.IconContent("Animation.Record", "|Enable/disable keyframe recording mode.");
        public static GUIContent previewContent = EditorGUIUtility.TextContent("Preview|Enable/disable scene preview mode.");
        public static GUIContent prevKeyContent = EditorGUIUtility.IconContent("Animation.PrevKey", "|Go to previous keyframe.");
        public static GUIContent nextKeyContent = EditorGUIUtility.IconContent("Animation.NextKey", "|Go to next keyframe.");
        public static GUIContent firstKeyContent = EditorGUIUtility.IconContent("Animation.FirstKey", "|Go to the beginning of the animation clip.");
        public static GUIContent lastKeyContent = EditorGUIUtility.IconContent("Animation.LastKey", "|Go to the end of the animation clip.");
        public static GUIContent addKeyframeContent = EditorGUIUtility.IconContent("Animation.AddKeyframe", "|Add keyframe.");
        public static GUIContent addEventContent = EditorGUIUtility.IconContent("Animation.AddEvent", "|Add event.");
        public static GUIContent sequencerLinkContent = EditorGUIUtility.IconContent("Animation.SequencerLink", "|Animation Window is linked to Sequence Editor.  Press to Unlink.");

        public static GUIContent noAnimatableObjectSelectedText = EditorGUIUtility.TextContent("No animatable object selected.");
        public static GUIContent formatIsMissing = EditorGUIUtility.TextContent("To begin animating {0}, create {1}.");
        public static GUIContent animatorAndAnimationClip = EditorGUIUtility.TextContent("an Animator and an Animation Clip");
        public static GUIContent animationClip = EditorGUIUtility.TextContent("an Animation Clip");
        public static GUIContent create = EditorGUIUtility.TextContent("Create");
        public static GUIContent dopesheet = EditorGUIUtility.TextContent("Dopesheet");
        public static GUIContent curves = EditorGUIUtility.TextContent("Curves");
        public static GUIContent samples = EditorGUIUtility.TextContent("Samples");
        public static GUIContent createNewClip = EditorGUIUtility.TextContent("Create New Clip...");

        public static GUIContent animatorOptimizedText = EditorGUIUtility.TextContent("Editing and playback of animations on optimized game object hierarchy is not supported.\nPlease select a game object that does not have 'Optimize Game Objects' applied.");

        public static GUIStyle playHead = "AnimationPlayHead";

        public static GUIStyle curveEditorBackground = "CurveEditorBackground";
        public static GUIStyle curveEditorLabelTickmarks = "CurveEditorLabelTickmarks";
        public static GUIStyle eventBackground = "AnimationEventBackground";
        public static GUIStyle eventTooltip = "AnimationEventTooltip";
        public static GUIStyle eventTooltipArrow = "AnimationEventTooltipArrow";
        public static GUIStyle keyframeBackground = "AnimationKeyframeBackground";
        public static GUIStyle timelineTick = "AnimationTimelineTick";
        public static GUIStyle dopeSheetKeyframe = "Dopesheetkeyframe";
        public static GUIStyle dopeSheetBackground = "DopesheetBackground";
        public static GUIStyle popupCurveDropdown = "PopupCurveDropdown";
        public static GUIStyle popupCurveEditorBackground = "PopupCurveEditorBackground";
        public static GUIStyle popupCurveEditorSwatch = "PopupCurveEditorSwatch";
        public static GUIStyle popupCurveSwatchBackground = "PopupCurveSwatchBackground";

        public static GUIStyle miniToolbar = new GUIStyle(EditorStyles.toolbar);
        public static GUIStyle miniToolbarButton = new GUIStyle(EditorStyles.toolbarButton);
        public static GUIStyle toolbarLabel = new GUIStyle(EditorStyles.toolbarPopup);

        public static void Initialize()
        {
            toolbarLabel.normal.background = null;
            miniToolbarButton.padding.top = 0;
            miniToolbarButton.padding.bottom = 3;
        }
    }
}
