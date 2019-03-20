// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// This is the Interface we will use to create the facade we need for testing.
    /// In the case of the Fake factory, we can create fake operations with doctored data we use for our tests.
    /// </summary>
    internal interface IOperationFactory
    {
        IListOperation CreateListOperation(bool offlineMode = false);
        ISearchOperation CreateSearchOperation();
        IAddOperation CreateAddOperation();
        IRemoveOperation CreateRemoveOperation();
    }
}
