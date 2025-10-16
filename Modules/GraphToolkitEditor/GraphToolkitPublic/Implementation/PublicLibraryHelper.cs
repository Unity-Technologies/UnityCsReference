// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor.Implementation
{
    class PublicLibraryHelper : ItemLibraryHelper
    {
        public PublicLibraryHelper(GraphModel graphModel) : base(graphModel) { }
        public override IItemDatabaseProvider GetItemDatabaseProvider()
        {
            return m_DatabaseProvider ??= new PublicDatabaseProviderImp(GraphModel);
        }
    }
}
