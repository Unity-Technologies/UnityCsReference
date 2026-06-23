// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Apple.TV
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("UnityEngine.Apple.TV.Remote has been deprecated. Use UnityEngine.tvOS.Remote instead (UnityUpgradable) -> UnityEngine.tvOS.Remote", true)]
    public sealed partial class Remote
    {
        [NoAutoStaticsCleanup] // extern property; delegates to native, no C# static state
        public static bool allowExitToHome
        {
            get;
            set;
        }

        [NoAutoStaticsCleanup] // extern property; delegates to native, no C# static state
        public static bool allowRemoteRotation
        {
            get;
            set;
        }

        [NoAutoStaticsCleanup] // extern property; delegates to native, no C# static state
        public static bool reportAbsoluteDpadValues
        {
            get;
            set;
        }

        [NoAutoStaticsCleanup] // extern property; delegates to native, no C# static state
        public static bool touchesEnabled
        {
            get;
            set;
        }
    }
}
