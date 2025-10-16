// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.DataModel;

internal class LocalIdentifierToObjectIDWrapper : IUDMObjectIDGenerator
{
    ulong LocalIdentifierInFile;
    ulong Offset;
    internal LocalIdentifierToObjectIDWrapper(ulong localIdentifierInFile)
    {
        LocalIdentifierInFile = localIdentifierInFile;
        Offset = 0;
    }

    public UdmObjectId NextUDMObjectID()
    {
        var id = LocalIdentifierInFile + Offset;
        Offset++;
        return id;
    }
}
