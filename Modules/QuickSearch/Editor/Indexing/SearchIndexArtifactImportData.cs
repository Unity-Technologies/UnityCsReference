// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Experimental;
using UnityEngine;

namespace UnityEditor.Search
{
    struct SearchIndexArtifactImportData
    {
        public int[] ImporterHashCodes;
        public OutOfProcessImportState[] OutOfProcessImportStates;
        public ImportResultID[] ImportResultIds;
        public ArtifactKey[] ArtifactKeys;

        public int Length => ImporterHashCodes.Length;

        public SearchIndexArtifactImportData(int count)
        {
            ImporterHashCodes = new int[count];
            OutOfProcessImportStates = new OutOfProcessImportState[count];
            ImportResultIds = new ImportResultID[count];
            ArtifactKeys = new ArtifactKey[count];
        }

        public Batch AsBatch()
        {
            return new Batch(
                importerHashCodes: ImporterHashCodes.AsSpan(),
                onDemandStates: OutOfProcessImportStates.AsSpan(),
                importResultIds: ImportResultIds.AsSpan(),
                artifactKeys: ArtifactKeys.AsSpan());
        }

        public Batch AsBatch(int start, int length)
        {
            return new Batch(
                importerHashCodes: ImporterHashCodes.AsSpan(start, length),
                onDemandStates: OutOfProcessImportStates.AsSpan(start, length),
                importResultIds: ImportResultIds.AsSpan(start, length),
                artifactKeys: ArtifactKeys.AsSpan(start, length));
        }

        public readonly ref struct Batch
        {
            public readonly Span<int> ImporterHashCodes;
            public readonly Span<OutOfProcessImportState> OutOfProcessImportStates;
            public readonly Span<ImportResultID> ImportResultIds;
            public readonly Span<ArtifactKey> ArtifactKeys;

            public int Length => ImporterHashCodes.Length;

            internal Batch(Span<int> importerHashCodes, Span<OutOfProcessImportState> onDemandStates, Span<ImportResultID> importResultIds, Span<ArtifactKey> artifactKeys)
            {
                ImporterHashCodes = importerHashCodes;
                ArtifactKeys = artifactKeys;
                OutOfProcessImportStates = onDemandStates;
                ImportResultIds = importResultIds;
            }
        }
    }
}
