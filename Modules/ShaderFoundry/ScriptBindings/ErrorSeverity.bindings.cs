// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.ShaderFoundry
{
    // THIS ENUM MUST BE KEPT IN SYNC WITH THE ENUM IN IErrorHandler.h
    [Flags]
    internal enum ErrorSeverity
    {
        None = 0,
        kWarning = 1 << 0,
        kError = 1 << 1,
    };
}
