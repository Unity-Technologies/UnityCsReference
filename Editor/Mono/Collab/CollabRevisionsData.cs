// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Collaboration
{
    // Keep internal and undocumented until we expose more functionality
    //*undocumented
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    internal struct RevisionsData
    {
        private int m_RevisionsInRepo;
        private int m_RevisionOffset;
        private int m_ReturnedRevisions;
        private Revision[] m_Revisions;

        public int RevisionsInRepo {get { return m_RevisionsInRepo; }}
        public int RevisionOffset {get { return m_RevisionOffset; }}
        public int ReturnedRevisions {get { return m_ReturnedRevisions; }}
        public Revision[] Revisions {get { return m_Revisions; }}
    }
}
