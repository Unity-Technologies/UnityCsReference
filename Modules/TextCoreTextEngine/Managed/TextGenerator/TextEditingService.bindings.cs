// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{
    [NativeHeader("Modules/TextCoreTextEngine/Native/TextEditingService.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal class TextEditingService
    {
        [NativeMethod(Name = "TextEditingService::GetText")]
        internal static extern string GetText(IntPtr textGenerationInfo);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        [NativeMethod(Name = "TextEditingService::SetText")]
        internal static extern bool SetText(IntPtr textGenerationInfo, string text);

        [NativeMethod(Name = "TextEditingService::DeleteSelection")]
        internal static extern int DeleteSelection(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextEditingService::Delete")]
        internal static extern int Delete(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextEditingService::Backspace")]
        internal static extern int Backspace(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextEditingService::DeleteWordBack")]
        internal static extern int DeleteWordBack(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextEditingService::DeleteWordForward")]
        internal static extern int DeleteWordForward(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextEditingService::DeleteLineBack")]
        internal static extern int DeleteLineBack(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextEditingService::ReplaceSelection")]
        internal static extern int ReplaceSelection(IntPtr textGenerationInfo, string replace);

        [NativeMethod(Name = "TextEditingService::RestoreCursorState")]
        internal static extern void RestoreCursorState(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextEditingService::EnableCursorPreviewState")]
        internal static extern void EnableCursorPreviewState(IntPtr textGenerationInfo, int compositionStringLength);

        [NativeMethod(Name = "TextEditingService::MoveSelectionToAltCursor")]
        internal static extern int MoveSelectionToAltCursor(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextEditingService::Cut")]
        internal static extern int Cut(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextEditingService::OnBlur")]
        internal static extern int OnBlur(IntPtr textGenerationInfo);
    }
}
