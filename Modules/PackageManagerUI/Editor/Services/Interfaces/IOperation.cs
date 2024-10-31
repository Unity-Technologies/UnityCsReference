// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IOperation
    {
        event Action<IOperation, UIError> onOperationError;
        event Action<IOperation> onOperationSuccess;
        event Action<IOperation> onOperationFinalized;

        // `onOperationProgress` will only be triggered if `isProgressTrackable` is true
        event Action<IOperation> onOperationProgress;

        string packageUniqueId { get; }
        long productId { get; }

        long timestamp { get; }
        long lastSuccessTimestamp { get; }
        bool isOfflineMode { get; }
        bool isInProgress { get; }
        bool isInPause { get; }
        bool isProgressVisible { get; }

        bool isProgressTrackable { get; }

        // returns a value in the range of [0, 1]
        // if the operation's progress is not trackable, 0 will be returned
        float progressPercentage { get; }

        RefreshOptions refreshOptions { get; }
    }
}
