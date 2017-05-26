// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public partial class TouchScreenKeyboard
    {
        // The status of the on-screen keyboard
        public enum Status
        {
            // The on-screen keyboard is open.
            Visible = 0,
            // The on-screen keyboard was closed with ok / done buttons.
            Done = 1,
            // The on-screen keyboard was closed with a back button.
            Canceled = 2,
            // The on-screen keyboard was closed by touching outside of the keyboard.
            LostFocus = 3,
        };
    }
}
