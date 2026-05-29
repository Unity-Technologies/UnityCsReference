// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IDateTimeProxy : IService
    {
        DateTime now { get; }
        DateTime utcNow { get; }
    }

    [ExcludeFromCodeCoverage]
    internal class DateTimeProxy : BaseService<IDateTimeProxy>, IDateTimeProxy
    {
        public DateTime now => DateTime.Now;
        public DateTime utcNow => DateTime.UtcNow;
    }
}
