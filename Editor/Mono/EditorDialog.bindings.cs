// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/EditorDialog.bindings.h")]
    public sealed partial class EditorDialog
    {
        [StaticAccessor(nameof(EditorDialog), StaticAccessorType.DoubleColon)]
        static extern void DisplayAlertDialogNative(string messageText, DialogIconType iconType, string titleText, string buttonText);

        [StaticAccessor(nameof(EditorDialog), StaticAccessorType.DoubleColon)]

        static extern void DisplayAlertDialogWithOptOutNative(string optOutMessage, string messageText, DialogIconType iconType, string titleText, string buttonText, out bool optOut);

        [StaticAccessor(nameof(EditorDialog), StaticAccessorType.DoubleColon)]
        static extern bool DisplayDecisionDialogNative(string messageText, DialogIconType iconType, string titleText, string okButtonText, string cancelButtonText);

        [StaticAccessor(nameof(EditorDialog), StaticAccessorType.DoubleColon)]
        static extern bool DisplayDecisionDialogWithOptOutNative(string optOutMessage, string messageText, DialogIconType iconType, string titleText, string okButtonText, string cancelButtonText, out bool optOut);

        [StaticAccessor(nameof(EditorDialog), StaticAccessorType.DoubleColon)]
        static extern DialogResult DisplayComplexDecisionDialogNative(string messageText, DialogIconType iconType, string titleText, string defaultOptionText, string alternateOptionText, string cancelButtonText);

        [StaticAccessor(nameof(EditorDialog), StaticAccessorType.DoubleColon)]
        static extern DialogResult DisplayComplexDecisionDialogWithOptOutNative(string optOutMessage, string messageText, DialogIconType iconType, string titleText, string defaultOptionText, string alternateOptionText, string cancelButtonText, out bool optOut);

        [StaticAccessor(nameof(EditorDialog), StaticAccessorType.DoubleColon)]
        static extern string GetDialogResponseFromInteractionContextNative(string titleText);
    }
}
