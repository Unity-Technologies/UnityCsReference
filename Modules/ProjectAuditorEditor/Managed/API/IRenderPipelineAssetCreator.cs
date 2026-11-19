// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Implemented by a render pipeline package so that the Migrate to URP workflow can create and assign a
    /// render pipeline asset without Project Auditor referencing that package.
    /// </summary>
    public interface IRenderPipelineAssetCreator
    {
        /// <summary>
        /// Creates a render pipeline asset configured for the given target pipeline, and assigns it as the
        /// project-wide default render pipeline.
        /// </summary>
        /// <param name="targetPipeline">The target pipeline identifier used by the Render Pipeline Converter (eg "BuiltInToURP" or "BuiltInToURP2D").</param>
        /// <returns>The asset assigned as the default, or null if one could not be created.</returns>
        RenderPipelineAsset CreateAndAssignDefault(string targetPipeline);

        /// <summary>
        /// Checks whether a render pipeline asset is already configured for the given target pipeline.
        /// </summary>
        /// <param name="asset">The render pipeline asset to check.</param>
        /// <param name="targetPipeline">The Migrate to URP workflow's target pipeline identifier.</param>
        /// <returns>True if the asset is already set up for the target pipeline.</returns>
        bool MatchesTargetPipeline(RenderPipelineAsset asset, string targetPipeline);
    }
}
