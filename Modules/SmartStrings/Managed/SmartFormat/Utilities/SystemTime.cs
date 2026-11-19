// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using Unity.Scripting.LifecycleManagement;

namespace Unity.SmartStrings.Utilities;

/// <summary>
/// Used for getting DateTime.Now or DateOffset.Now.
/// Mainly used for unit tests.
/// </summary>
static partial class SystemTime
{
    /// <summary>
    /// Normally this is a pass-through to DateTime.Now, but it can be overridden with SetDateTime( .. ) for unit testing and debugging.
    /// </summary>
    [AutoStaticsCleanupOnCodeReload] // may hold a test override; restore the pass-through
    public static Func<DateTime> Now { get; private set; } = () => DateTime.Now;

    /// <summary>
    /// Set time to return when SystemTime.Now() is called.
    /// </summary>
    public static void SetDateTime(DateTime dateTimeNow)
    {
        Now = () => dateTimeNow;
    }

    /// <summary>
    /// Normally this is a pass-through to DateTimeOffset.Now, but it can be overridden with SetDateTime( .. ) for unit testing and debugging.
    /// </summary>
    [AutoStaticsCleanupOnCodeReload] // may hold a test override; restore the pass-through
    public static Func<DateTimeOffset> OffsetNow { get; private set; } = () => DateTimeOffset.Now;

    /// <summary>
    /// Set time to return when SystemTime.OffsetNow() is called.
    /// </summary>
    public static void SetDateTimeOffset(DateTimeOffset dateTimeOffset)
    {
        OffsetNow = () => dateTimeOffset;
    }

    /// <summary>
    /// Resets SystemTime.Now() to return DateTime.Now.
    /// </summary>
    public static void ResetDateTime()
    {
        Now = () => DateTime.Now;
        OffsetNow = () => DateTimeOffset.Now;
    }
}
