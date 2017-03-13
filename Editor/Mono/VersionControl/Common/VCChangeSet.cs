// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.VersionControl
{
    public partial class ChangeSet
    {
        public static string defaultID = "-1";

        public ChangeSet()
        {
            InternalCreate();
        }

        public ChangeSet(string description)
        {
            InternalCreateFromString(description);
        }

        public ChangeSet(string description, string revision)
        {
            InternalCreateFromStringString(description, revision);
        }

        public ChangeSet(ChangeSet other)
        {
            InternalCopyConstruct(other);
        }

        ~ChangeSet()
        {
            Dispose();
        }
    }
}
