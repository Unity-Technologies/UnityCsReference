// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    internal class UpmOperationFactory : IOperationFactory
    {
        public IListOperation CreateListOperation(bool offlineMode = false)
        {
            return new UpmListOperation(offlineMode);
        }

        public ISearchOperation CreateSearchOperation()
        {
            return new UpmSearchOperation();
        }

        public IAddOperation CreateAddOperation()
        {
            return new UpmAddOperation();
        }

        public IRemoveOperation CreateRemoveOperation()
        {
            return new UpmRemoveOperation();
        }

        public IEmbedOperation CreateEmbedOperation()
        {
            return new UpmEmbedOperation();
        }
    }
}
