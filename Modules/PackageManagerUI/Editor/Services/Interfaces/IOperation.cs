// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal interface IOperation
    {
        event Action<IOperation, UIError> onOperationError;
        event Action<IOperation> onOperationSuccess;
        event Action<IOperation> onOperationFinalized;

        // `onOperationProgress` will only be triggered if `isProgressTrackable` is true
        event Action<IOperation> onOperationProgress;

        // the special unique id is used when neither package unique id or version unique id applies
        // e.g. git url, tar ball path that does not contain any package name or version
        string specialUniqueId { get; }

        string packageUniqueId { get; }
        string versionUniqueId { get; }

        // a timestamp is added to keep track of how `fresh` the result is
        // in the case of an online operation, it is the time when the operation starts
        // in the case of an offline operation, it is set to the timestamp of the last online operation
        long timestamp { get; }
        long lastSuccessTimestamp { get; }
        bool isOfflineMode { get; }
        bool isInProgress { get; }
        bool isProgressVisible { get; }

        bool isProgressTrackable { get; }

        // returns a value in the range of [0, 1]
        // if the operation's progress is not trackable, 0 will be returned
        float progressPercentage { get; }

        RefreshOptions refreshOptions { get; }
    }
}
