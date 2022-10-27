// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The answer to whether it is allowed to have multiple instances of a data output variable.
    /// </summary>
    enum AllowMultipleDataOutputInstances
    {
        Allow,
        Disallow,
        AllowWithWarning // Allowed, but with a warning.
    }
}
