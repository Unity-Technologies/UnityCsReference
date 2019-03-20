// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal interface IBaseOperation
    {
        event Action<Error> OnOperationError;
        event Action OnOperationFinalized;

        bool IsCompleted { get; }

        void Cancel();
    }
}
