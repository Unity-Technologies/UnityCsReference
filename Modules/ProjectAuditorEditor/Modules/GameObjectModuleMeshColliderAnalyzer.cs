// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal class GameObjectModuleMeshColliderAnalyzer : GameObjectModuleAnalyzer
    {
        internal const string PAA6010 = nameof(PAA6010);
        internal const string PAA6011 = nameof(PAA6011);
        internal const string PAA6012 = nameof(PAA6012);

        internal static readonly Descriptor k_MeshColliderReadWriteDescriptor = new Descriptor
            (
            PAA6010,
            "MeshCollider: Mesh requires Read/Write",
            Areas.Quality | Areas.Upgrade,
            "A MeshCollider requires CPU access to mesh data due to non-uniform or negative scale. In future versions of Unity, the build process will no longer automatically enable Read/Write for Meshes referenced by Mesh Colliders.",
            "Enable Read/Write in the Mesh's import settings, or use uniform positive scale on the GameObject."
            )
        {
            MessageFormat = "Mesh '{0}' used by MeshCollider on '{1}' requires Read/Write to compute collision data",
            Fixer = (issue, analysisParams) =>
            {
                if (!InternalEditorUtility.CanMeshBeModifiedFromCode(issue.RelativePath))
                {
                    Debug.LogWarning($"Cannot modify Mesh located at '{issue.RelativePath}'. Please fix the Mesh manually or assign a different Mesh to the Mesh Collider.");
                    return false;
                }
                return InternalEditorUtility.ImportMeshAsReadable(issue.RelativePath);
            }
        };

        internal static readonly Descriptor k_MeshColliderConvexBakingDescriptor = new Descriptor
            (
            PAA6011,
            "MeshCollider: Convex collision not pre-baked",
            Areas.Quality | Areas.Upgrade,
            "A convex MeshCollider's mesh is missing pre-baked convex collision data. In future versions of Unity, the build process will no longer pre-bake collision data for Meshes referenced by Mesh Colliders.",
            "Enable the 'Bake Convex' collision mode in the Mesh's import settings to pre-bake collision data at import time."
            )
        {
            MessageFormat = "Mesh '{0}' used by convex MeshCollider on '{1}' is missing pre-baked convex collision",
            Fixer = (issue, analysisParams) =>
            {
                if (!InternalEditorUtility.CanMeshBeModifiedFromCode(issue.RelativePath))
                {
                    Debug.LogWarning($"Cannot modify Mesh located at '{issue.RelativePath}'. Please fix the Mesh manually or assign a different Mesh to the Mesh Collider.");
                    return false;
                }
                return InternalEditorUtility.ImportMeshWithPreBakeCollision(issue.RelativePath, true);
            }
        };

        internal static readonly Descriptor k_MeshColliderTriangleBakingDescriptor = new Descriptor
            (
            PAA6012,
            "MeshCollider: Triangle collision not pre-baked",
            Areas.Quality | Areas.Upgrade,
            "A non-convex MeshCollider's mesh is missing pre-baked triangle collision data. In future versions of Unity, the build process will no longer pre-bake collision data for Meshes referenced by Mesh Colliders.",
            "Enable the 'Bake Triangle' collision mode in the Mesh's import settings to pre-bake collision data at import time."
            )
        {
            MessageFormat = "Mesh '{0}' used by triangle MeshCollider on '{1}' is missing pre-baked triangle collision",
            Fixer = (issue, analysisParams) =>
            {
                if (!InternalEditorUtility.CanMeshBeModifiedFromCode(issue.RelativePath))
                {
                    Debug.LogWarning($"Cannot modify Mesh located at '{issue.RelativePath}'. Please fix the Mesh manually or assign a different Mesh to the Mesh Collider.");
                    return false;
                }
                return InternalEditorUtility.ImportMeshWithPreBakeCollision(issue.RelativePath, false);
            }
        };

        readonly HashSet<EntityId> m_VisitedAssets = new HashSet<EntityId>(512);

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_MeshColliderReadWriteDescriptor);
            registerDescriptor(k_MeshColliderConvexBakingDescriptor);
            registerDescriptor(k_MeshColliderTriangleBakingDescriptor);
        }

        internal override void OnAnalysisStarted()
        {
            m_VisitedAssets.Clear();
        }

        public override IEnumerable<ReportItemBuilder> Analyze(GameObjectAnalysisContext context)
        {
            var meshCollider = context.GameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
                yield break;

            var mesh = meshCollider.sharedMesh;
            if (mesh == null)
                yield break;

            // Only add one issue per mesh
            if (m_VisitedAssets.Contains(mesh.GetEntityId()))
                yield break;

            // Scale baking requires Read/Write
            if (meshCollider.IsScaleBakingRequired())
            {
                if (!mesh.isReadable)
                    yield return CreateIssue(context, mesh, k_MeshColliderReadWriteDescriptor);
            }
            // Convex collider needs BakeConvex
            else if (meshCollider.convex && !mesh.HasPreBakeCollisionMesh(true))
            {
                yield return CreateIssue(context, mesh, k_MeshColliderConvexBakingDescriptor);
            }
            // Non-convex collider needs BakeTriangle
            else if (!meshCollider.convex && !mesh.HasPreBakeCollisionMesh(false))
            {
                yield return CreateIssue(context, mesh, k_MeshColliderTriangleBakingDescriptor);
            }
        }

        private ReportItemBuilder CreateIssue(GameObjectAnalysisContext context, Mesh mesh, Descriptor descriptor)
        {
            m_VisitedAssets.Add(mesh.GetEntityId());

            return context.CreateIssue
            (
                IssueCategory.GameObject,
                descriptor.Id,
                mesh.name,
                context.GameObject.name
            )
            .WithLocation(AssetDatabase.GetAssetPath(mesh));
        }
    }
}
