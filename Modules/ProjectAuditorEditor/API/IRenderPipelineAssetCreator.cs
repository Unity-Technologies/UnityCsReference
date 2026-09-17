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
        /// Creates a render pipeline asset and assigns it as the project-wide default render pipeline.
        /// </summary>
        /// <returns>The asset assigned as the default, or null if one could not be created.</returns>
        RenderPipelineAsset CreateAndAssignDefault();
    }
}
