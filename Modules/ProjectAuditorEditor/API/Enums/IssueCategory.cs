// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Collections;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Identifiers for all the categories of issues reported by Project Auditor.
    /// </summary>
    /// <remarks>
    /// As Project Auditor's remit has expanded, so has the definition of what constitutes an issue category.
    /// For example, categories relating to assets, shaders or build reports represent categories of information about the project's content but do not necessarily qualify as issues that should be addressed.
    /// </remarks>
    public enum IssueCategory
    {
        /// <summary>
        /// Category for the summary of issues relating to optimizing the project
        /// </summary>
        OptimizationSummary = 0,

        /// <summary>
        /// Category for General statistics about the analysis process and its results.
        /// </summary>
        [System.Obsolete("Use OptimizationSummary instead (UnityUpgradable) -> OptimizationSummary", true)]
        Metadata = 0,

        /// <summary>
        /// Issues relating to asset data or asset import settings
        /// </summary>
        AssetIssue,

        /// <summary>
        /// Category for reporting shaders in the project
        /// </summary>
        Shader,

        /// <summary>
        /// Category for reporting shader variants
        /// </summary>
        ShaderVariant,

        /// <summary>
        /// Code Issues, discovered by static code analysis
        /// </summary>
        Code,

        /// <summary>
        /// Compiler errors and warnings generated whilst compiling code for static analysis
        /// </summary>
        CodeCompilerMessage,

        /// <summary>
        /// Issues relating to project settings
        /// </summary>
        ProjectSetting,

        /// <summary>
        /// Category for displaying information about files created during the project build process
        /// </summary>
        BuildFile,

        /// <summary>
        /// Category for displaying information about the steps of the build process and how long they took
        /// </summary>
        BuildStep,

        /// <summary>
        /// Category for build summary information
        /// </summary>
        BuildSummary,

        /// <summary>
        /// Category for information about all of the code assemblies in the project
        /// </summary>
        Assembly,

        /// <summary>
        /// Category for information about precompiled assemblies
        /// </summary>
        PrecompiledAssembly,

        /// <summary>
        /// Issues reported by the shader compiler
        /// </summary>
        ShaderCompilerMessage,

        /// <summary>
        /// Category for displaying installed packages
        /// </summary>
        Package,

        /// <summary>
        /// Issues relating to texture assets and texture import settings
        /// </summary>
        Texture,

        /// <summary>
        /// Issues relating to Audio Clip assets and import settings
        /// </summary>
        AudioClip,

        /// <summary>
        /// Category for displaying variants of compute shaders
        /// </summary>
        ComputeShaderVariant,

        /// <summary>
        /// Issues relating to Mesh assets and import settings
        /// </summary>
        Mesh,

        /// <summary>
        /// Issues relating to Sprite Atlas assets and import settings
        /// </summary>
        SpriteAtlas,

        /// <summary>
        /// Category for showing materials grouped by shader
        /// </summary>
        Material,

        /// <summary>
        /// Issues relating to animator controllers
        /// </summary>
        AnimatorController,

        /// <summary>
        /// Issues relating to animation clips
        /// </summary>
        AnimationClip,

        /// <summary>
        /// Issues relating to avatars
        /// </summary>
        Avatar,

        /// <summary>
        /// Issues relating to avatar masks
        /// </summary>
        AvatarMask,

        /// <summary>
        /// Issues that could result in undesired behavior if domain reloading is disabled
        /// </summary>
        DomainReload,

        /// <summary>
        /// Category for showing GameObjects
        /// </summary>
        GameObject,

        /// <summary>
        /// A library of deprecated API
        /// </summary>
        ObsoleteAPI,
        
        /// <summary>
        /// Category for showing MeshColliders
        /// </summary>
        MeshCollider,

        /// <summary>
        /// Category for the summary of issues relating to upgrading the project
        /// </summary>
        UpgradeSummary,

        /// <summary>
        /// Category for the summary of issues relating to migrating the project to URP
        /// </summary>
        MigrateToURPSummary,

        /// <summary>
        /// Category for the summary of issues relating to migrating the project to CoreCLR
        /// </summary>
        MigrateToCoreCLRSummary
    }

    internal static class IssueCategoryExtensions
    {
        public static bool IsSummary(this IssueCategory category) => category == IssueCategory.OptimizationSummary || category == IssueCategory.UpgradeSummary || category == IssueCategory.MigrateToURPSummary || category == IssueCategory.MigrateToCoreCLRSummary;
        public static bool IsPopulatedByPlayerBuild(this IssueCategory category) => category == IssueCategory.ShaderVariant || category == IssueCategory.ComputeShaderVariant;
        public static readonly IssueCategory FirstCustomCategory = ((IReadOnlyList<IssueCategory>)System.Enum.GetValues(typeof(IssueCategory))).Max() + 1;
    }
}
