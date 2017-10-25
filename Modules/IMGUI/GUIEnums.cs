// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    // Scaling mode to draw textures with
    public enum ScaleMode
    {
        // Stretches the texture to fill the complete rectangle passed in to GUI.DrawTexture
        StretchToFill = 0,
        // Scales the texture, maintaining aspect ratio, so it completely covers the /position/ rectangle passed to GUI.DrawTexture. If the texture is being draw to a rectangle with a different aspect ratio than the original, the image is cropped.
        ScaleAndCrop = 1,
        // Scales the texture, maintaining aspect ratio, so it completely fits withing the /position/ rectangle passed to GUI.DrawTexture.
        ScaleToFit = 2
    }

    // Used by GUIUtility.GetcontrolID to inform the UnityGUI system if a given control can get keyboard focus.
    public enum FocusType
    {
        [Obsolete("FocusType.Native now behaves the same as FocusType.Passive in all OS cases. (UnityUpgradable) -> Passive", false)]
        Native = 0,
        // This is a proper keyboard control. It can have input focus on all platforms. Used for TextField and TextArea controls
        Keyboard = 1,
        // This control can never receive keyboard focus.
        Passive = 2
    }
}
