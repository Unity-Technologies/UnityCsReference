// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal
{
    [ExcludeFromCodeCoverage]
    internal class DateTimeProxy
    {
        public virtual DateTime utcNow => DateTime.UtcNow;
    }
}
