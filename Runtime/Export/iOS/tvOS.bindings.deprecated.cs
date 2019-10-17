// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Apple.TV
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("UnityEngine.Apple.TV.Remote has been deprecated. Use UnityEngine.tvOS.Remote instead (UnityUpgradable) -> UnityEngine.tvOS.Remote", true)]
    public sealed partial class Remote
    {
        public static bool allowExitToHome
        {
            get;
            set;
        }

        public static bool allowRemoteRotation
        {
            get;
            set;
        }

        public static bool reportAbsoluteDpadValues
        {
            get;
            set;
        }

        public static bool touchesEnabled
        {
            get;
            set;
        }
    }
}
