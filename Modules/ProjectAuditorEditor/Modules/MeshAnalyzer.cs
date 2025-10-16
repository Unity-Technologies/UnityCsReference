// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class MeshAnalyzer : MeshModuleAnalyzer
    {
        internal const string PAA1000 = nameof(PAA1000);
        internal const string PAA1001 = nameof(PAA1001);

        internal static readonly Descriptor k_MeshReadWriteEnabledDescriptor = new Descriptor(
            PAA1000,
            "Mesh: Read/Write enabled",
            Areas.Memory,
            "The <b>Read/Write Enabled</b> flag in the Model Import Settings is enabled. This causes the mesh data to be duplicated in memory.",
            "If not required, disable the <b>Read/Write Enabled</b> option in the Model Import Settings."
        )
        {
            MessageFormat = "Mesh '{0}' Read/Write is enabled",
            DocumentationUrl = "https://docs.unity3d.com/Manual/FBXImporter-Model.html"
        };

        internal static readonly Descriptor k_Mesh32BitIndexFormatUsedDescriptor = new Descriptor(
            PAA1001,
            "Mesh: Index Format is 32 bits",
            Areas.Memory,
            "The <b>Index Format</b> in the Model Import Settings is set to <b>32 bit</b>. This increases the mesh size and may not work on certain mobile devices.",
            "Consider using changing the <b>Index Format</b> option in the Model Import Settings. This should be set to either <b>16 bits</b> or <b>Auto</b>."
        )
        {
            MessageFormat = "Mesh '{0}' Index Format is 32 bits",
            DocumentationUrl = "https://docs.unity3d.com/Manual/FBXImporter-Model.html"
        };

        // TODO: Uncomment these when it's time to re-implement vertex/triangle count analysis.
        // [DiagnosticParameter("MeshVertexCountLimit", 5000)]
        // int m_VertexCountLimit;
        //
        // [DiagnosticParameter("MeshTriangleCountLimit", 5000)]
        // int m_TriangleCountLimit;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_MeshReadWriteEnabledDescriptor);
            registerDescriptor(k_Mesh32BitIndexFormatUsedDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(MeshAnalysisContext context)
        {
            var mesh = context.Mesh;
            if (mesh.isReadable)
            {
                yield return context.CreateIssue(IssueCategory.AssetIssue, k_MeshReadWriteEnabledDescriptor.Id, context.Name)
                    .WithLocation(context.Importer.assetPath);
            }

            if (mesh.indexFormat == IndexFormat.UInt32 &&
                mesh.vertexCount <= 65535)
            {
                yield return context.CreateIssue(IssueCategory.AssetIssue,
                    k_Mesh32BitIndexFormatUsedDescriptor.Id, context.Name)
                    .WithLocation(context.Importer.assetPath);
            }
        }
    }
}
