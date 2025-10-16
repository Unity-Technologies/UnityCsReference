// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum AnimatorControllerProperty
    {
        NumLayers,
        NumParameters,
        NumClips,
        SizeOnDisk,
        Num
    }

    enum AnimationClipProperty
    {
        IsEmpty,
        NumEvents,
        Framerate,
        Length,
        WrapMode,
        IsLooping,
        HasGenericRootTransform,
        HasMotionCurves,
        HasMotionFloatCurves,
        HasRootCurves,
        HumanMotion,
        IsLegacy,
        SizeOnDisk,
        Num
    }

    enum AvatarProperty
    {
        IsValid,
        IsHuman,
        NumHumanBones,
        NumSkeletonBones,
        UpperArmTwist,
        LowerArmTwist,
        UpperLegTwist,
        LowerLegTwist,
        ArmStretch,
        LegStretch,
        FeetSpacing,
        HasTranslationDoF,
        SizeOnDisk,
        Num
    }

    enum AvatarMaskProperty
    {
        NumTransforms,
        SizeOnDisk,
        Num
    }

    class AnimationModule : ModuleWithAnalyzers<AnimationAnalyzer>
    {
        static readonly IssueLayout k_AnimatorControllerLayout = new IssueLayout
        {
            Category = IssueCategory.AnimatorController,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "Controller Name", MaxAutoWidth = 500  },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimatorControllerProperty.NumLayers), Format = PropertyFormat.Integer, Name = "Layers", LongName = "Number of Layers" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimatorControllerProperty.NumParameters), Format = PropertyFormat.Integer, Name = "Params", LongName = "Number of Parameters" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimatorControllerProperty.NumClips), Format = PropertyFormat.Integer, Name = "Clips", LongName = "Number of Animation Clips" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimatorControllerProperty.SizeOnDisk), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Controller Size" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 }
            }
        };

        static readonly IssueLayout k_AnimationClipLayout = new IssueLayout
        {
            Category = IssueCategory.AnimationClip,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "Clip Name", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimationClipProperty.IsEmpty), Format = PropertyFormat.Bool, Name = "Empty", LongName = "Contains no curves and no events" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimationClipProperty.NumEvents), Format = PropertyFormat.Integer, Name = "Events", LongName = "Number of Events" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimationClipProperty.Framerate), Format = PropertyFormat.String, Name = "Frame Rate" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimationClipProperty.Length), Format = PropertyFormat.String, Name = "Length" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimationClipProperty.WrapMode), Format = PropertyFormat.String, Name = "Wrap Mode" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimationClipProperty.IsLooping), Format = PropertyFormat.Bool, Name = "Looping" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HasGenericRootTransform), Format = PropertyFormat.Bool, Name = "Generic Root Transform", LongName = "Has animation on the root transform" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HasMotionCurves), Format = PropertyFormat.Bool, Name = "Motion Curves", LongName = "Has root motion curves" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HasMotionFloatCurves), Format = PropertyFormat.Bool, Name = "Motion Float Curves", LongName = "Has editor curves for its root motion" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HasRootCurves), Format = PropertyFormat.Bool, Name = "Root Curves" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimationClipProperty.HumanMotion), Format = PropertyFormat.Bool, Name = "Human Motion", LongName = "Contains curves that drive a humanoid rig" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimationClipProperty.IsLegacy), Format = PropertyFormat.Bool, Name = "Legacy", LongName = "Is this clip used with a Legacy Animation component?" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AnimationClipProperty.SizeOnDisk), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Clip Size" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 }
            }
        };

        static readonly IssueLayout k_AvatarLayout = new IssueLayout
        {
            Category = IssueCategory.Avatar,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "Avatar Name", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarProperty.IsValid), Format = PropertyFormat.Bool, Name = "Valid" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarProperty.IsHuman), Format = PropertyFormat.Bool, Name = "Human" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarProperty.NumHumanBones), Format = PropertyFormat.Integer, Name = "Human Bones", LongName = "Number of bones mappings" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarProperty.NumSkeletonBones), Format = PropertyFormat.Integer, Name = "Skeleton Bones", LongName = "Number of bone transforms to include" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarProperty.UpperArmTwist), Format = PropertyFormat.String, Name = "Upper Arm Twist" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarProperty.LowerArmTwist), Format = PropertyFormat.String, Name = "Lower Arm Twist" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarProperty.UpperLegTwist), Format = PropertyFormat.String, Name = "Upper Leg Twist" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarProperty.LowerLegTwist), Format = PropertyFormat.String, Name = "Lower Leg Twist" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarProperty.ArmStretch), Format = PropertyFormat.String, Name = "Arm Stretch" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarProperty.LegStretch), Format = PropertyFormat.String, Name = "Leg Stretch" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarProperty.FeetSpacing), Format = PropertyFormat.String, Name = "Feet Spacing" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarProperty.HasTranslationDoF), Format = PropertyFormat.Bool, Name = "Translation DoF" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarProperty.SizeOnDisk), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Avatar Size" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 }
            }
        };

        static readonly IssueLayout k_AvatarMaskLayout = new IssueLayout
        {
            Category = IssueCategory.AvatarMask,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "Mask Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarMaskProperty.NumTransforms), Format = PropertyFormat.Integer, Name = "Transforms", LongName = "Number of Transforms" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AvatarMaskProperty.SizeOnDisk), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Mask Size" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path" }
            }
        };

        public override string Name => "Animation";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_AnimatorControllerLayout,
            k_AnimationClipLayout,
            k_AvatarLayout,
            k_AvatarMaskLayout,
            AssetsModule.k_IssueLayout
        };

        public override void Initialize()
        {
            base.Initialize();

            ProjectIssueExtensions.AddCustomComparer(IssueCategory.AnimationClip, PropertyTypeUtil.FromCustom(AnimationClipProperty.Length),
                (a, b) =>
                {
                    var strA = a.GetProperty(PropertyTypeUtil.FromCustom(AnimationClipProperty.Length));
                    var strB = b.GetProperty(PropertyTypeUtil.FromCustom(AnimationClipProperty.Length));

                    // Chop off the " s" ending that each of the strings have
                    var floatA = Single.Parse(strA.Substring(0, strA.Length - 2));
                    var floatB = Single.Parse(strB.Substring(0, strB.Length - 2));

                    return floatA < floatB ? -1 : floatA > floatB ? 1 : 0;
                });
        }

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var context = new AnalysisContext
            {
                Params = analysisParams
            };
            ProcessAnimatorControllers(context, progress);
            if (progress?.IsCancelled ?? false)
                return AnalysisResult.Cancelled;

            ProcessAnimationClips(context, progress);
            if (progress?.IsCancelled ?? false)
                return AnalysisResult.Cancelled;

            ProcessAvatars(context, progress);
            if (progress?.IsCancelled ?? false)
                return AnalysisResult.Cancelled;

            ProcessAvatarMasks(context, progress);
            if (progress?.IsCancelled ?? false)
                return AnalysisResult.Cancelled;

            return AnalysisResult.Success;
        }

        void ProcessAnimatorControllers(AnalysisContext context, IProgress progress)
        {
            var issues = new List<ReportItem>();

            var assetPaths = GetAssetPathsByFilter("t:animatorcontroller, a:assets", context);
            progress?.Start("Finding Animator Controllers", "Search in Progress...", assetPaths.Length);
            foreach (var assetPath in assetPaths)
            {
                if (progress?.IsCancelled ?? false)
                    return;

                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
                if (controller == null)
                {
                    Debug.LogError(assetPath + " is not an Animator Controller.");

                    continue;
                }

                // TODO: the size returned by the profiler may not be the exact size on the target platform. Needs to be fixed.
                var size = Profiler.GetRuntimeMemorySizeLong(controller);

                issues.Add(context.CreateInsight(k_AnimatorControllerLayout.Category, controller.name)
                    .WithCustomProperties(new object[(int)AnimatorControllerProperty.Num]
                    {
                        controller.layers.Length,
                        controller.parameters.Length,
                        controller.animationClips.Length,
                        size
                    })
                    .WithLocation(assetPath)
                );

                progress?.Advance();
            }

            if (issues.Any())
                context.Params.OnIncomingIssues(issues);

            progress?.Clear();
        }

        void ProcessAnimationClips(AnalysisContext context, IProgress progress)
        {
            var issues = new List<ReportItem>();
            var assetPaths = GetAssetPathsByFilter("t:animationclip, a:assets", context);

            progress?.Start("Finding Animation Clips", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                if (progress?.IsCancelled ?? false)
                    return;

                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (clip == null)
                {
                    Debug.LogError(assetPath + " is not an Animation Clip.");

                    continue;
                }

                // TODO: the size returned by the profiler may not be the exact size on the target platform. Needs to be fixed.
                var size = Profiler.GetRuntimeMemorySizeLong(clip);

                issues.Add(context.CreateInsight(k_AnimationClipLayout.Category, clip.name)
                    .WithCustomProperties(new object[(int)AnimationClipProperty.Num]
                    {
                        clip.empty,
                        clip.events.Length,
                        Formatting.FormatFramerate(clip.frameRate),
                        Formatting.FormatLengthInSeconds(clip.length),
                        clip.wrapMode,
                        clip.isLooping,
                        clip.hasGenericRootTransform,
                        clip.hasMotionCurves,
                        clip.hasMotionFloatCurves,
                        clip.hasRootCurves,
                        clip.humanMotion,
                        clip.legacy,
                        size
                    })
                    .WithLocation(assetPath)
                );

                progress?.Advance();
            }

            if (issues.Any())
                context.Params.OnIncomingIssues(issues);

            progress?.Clear();
        }

        void ProcessAvatars(AnalysisContext context, IProgress progress)
        {
            var issues = new List<ReportItem>();
            var assetPaths = GetAssetPathsByFilter("t:avatar, a:assets", context);

            progress?.Start("Finding Avatars", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                if (progress?.IsCancelled ?? false)
                    return;

                var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(assetPath);
                if (avatar == null)
                {
                    Debug.LogError(assetPath + " is not an Avatar.");

                    continue;
                }

                // TODO: the size returned by the profiler may not be the exact size on the target platform. Needs to be fixed.
                var size = Profiler.GetRuntimeMemorySizeLong(avatar);

                issues.Add(context.CreateInsight(k_AvatarLayout.Category, avatar.name)
                    .WithCustomProperties(new object[(int)AvatarProperty.Num]
                    {
                        avatar.isValid,
                        avatar.isHuman,
                        avatar.humanDescription.human.Length,
                        avatar.humanDescription.skeleton.Length,
                        avatar.humanDescription.upperArmTwist,
                        avatar.humanDescription.lowerArmTwist,
                        avatar.humanDescription.upperLegTwist,
                        avatar.humanDescription.lowerLegTwist,
                        avatar.humanDescription.armStretch,
                        avatar.humanDescription.legStretch,
                        avatar.humanDescription.feetSpacing,
                        avatar.humanDescription.hasTranslationDoF,
                        size
                    })
                    .WithLocation(assetPath)
                );

                progress?.Advance();
            }

            if (issues.Any())
                context.Params.OnIncomingIssues(issues);

            progress?.Clear();
        }

        void ProcessAvatarMasks(AnalysisContext context, IProgress progress)
        {
            var issues = new List<ReportItem>();
            var assetPaths = GetAssetPathsByFilter("t:avatarmask, a:assets", context);

            progress?.Start("Finding Avatar Masks", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                if (progress?.IsCancelled ?? false)
                    return;

                var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(assetPath);
                if (mask == null)
                {
                    Debug.LogError(assetPath + " is not an Avatar Mask.");

                    continue;
                }

                // TODO: the size returned by the profiler may not be the exact size on the target platform. Needs to be fixed.
                var size = Profiler.GetRuntimeMemorySizeLong(mask);

                issues.Add(context.CreateInsight(k_AvatarMaskLayout.Category, mask.name)
                    .WithCustomProperties(new object[(int)AvatarMaskProperty.Num]
                    {
                        mask.transformCount,
                        size
                    })
                    .WithLocation(assetPath)
                );

                progress?.Advance();
            }

            if (issues.Any())
                context.Params.OnIncomingIssues(issues);

            progress?.Clear();
        }
    }
}
