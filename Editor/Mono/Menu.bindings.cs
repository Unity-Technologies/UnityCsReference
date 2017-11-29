// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/MenuController.h")]
    public sealed class Menu
    {
        [NativeMethod("MenuController::SetChecked", true)]
        public static extern void SetChecked(string menuPath, bool isChecked);

        [NativeMethod("MenuController::GetChecked", true)]
        public static extern bool GetChecked(string menuPath);
    }
}
