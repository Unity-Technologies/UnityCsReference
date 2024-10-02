// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{
    [NativeHeader("Modules/TextCoreTextEngine/Native/TextSelectionService.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule","Unity.UIElements.PlayModeTests")]
    internal class TextSelectionService
    {
        [NativeMethod(Name = "TextSelectionService::Substring")]
        internal static extern string Substring(IntPtr textGenerationInfo, int startIndex, int endIndex);

        [NativeMethod(Name = "TextSelectionService::SelectCurrentWord")]
        internal static extern void SelectCurrentWord(IntPtr textGenerationInfo, int currentIndex, ref int startIndex, ref int endIndex);

        [NativeMethod(Name = "TextSelectionService::PreviousCodePointIndex")]
        internal static extern int PreviousCodePointIndex(IntPtr textGenerationInfo, int currentIndex);

        [NativeMethod(Name = "TextSelectionService::NextCodePointIndex")]
        internal static extern int NextCodePointIndex(IntPtr textGenerationInfo, int currentIndex);

        [NativeMethod(Name = "TextSelectionService::GetCursorLogicalIndexFromPosition")]
        internal static extern int GetCursorLogicalIndexFromPosition(IntPtr textGenerationInfo, Vector2 position);

        [NativeMethod(Name = "TextSelectionService::GetCursorPositionFromLogicalIndex")]
        internal static extern Vector2 GetCursorPositionFromLogicalIndex(IntPtr textGenerationInfo, int logicalIndex);

        [NativeMethod(Name = "TextSelectionService::LineUpCharacterPosition")]
        internal static extern int LineUpCharacterPosition(IntPtr textGenerationInfo, int originalPos);

        [NativeMethod(Name = "TextSelectionService::LineDownCharacterPosition")]
        internal static extern int LineDownCharacterPosition(IntPtr textGenerationInfo, int originalPos);

        [NativeMethod(Name = "TextSelectionService::GetHighlightRectangles")]
        internal static extern Rect[] GetHighlightRectangles(IntPtr textGenerationInfo, int cursorIndex, int selectIndex);

        [NativeMethod(Name = "TextSelectionService::GetCharacterHeightFromIndex")]
        internal static extern float GetCharacterHeightFromIndex(IntPtr textGenerationInfo, int index);

        [NativeMethod(Name = "TextSelectionService::GetStartOfNextWord")]
        internal static extern int GetStartOfNextWord(IntPtr textGenerationInfo, int currentIndex);

        [NativeMethod(Name = "TextSelectionService::GetEndOfPreviousWord")]
        internal static extern int GetEndOfPreviousWord(IntPtr textGenerationInfo, int currentIndex);

        [NativeMethod(Name = "TextSelectionService::GetFirstCharacterIndexOnLine")]
        internal static extern int GetFirstCharacterIndexOnLine(IntPtr textGenerationInfo, int currentIndex);

        [NativeMethod(Name = "TextSelectionService::GetLastCharacterIndexOnLine")]
        internal static extern int GetLastCharacterIndexOnLine(IntPtr textGenerationInfo, int currentIndex);

        [NativeMethod(Name = "TextSelectionService::GetLineHeight")]
        internal static extern float GetLineHeight(IntPtr textGenerationInfo, int lineIndex);

        [NativeMethod(Name = "TextSelectionService::GetLineNumberFromLogicalIndex")]
        internal static extern int GetLineNumber(IntPtr textGenerationInfo, int logicalIndex);

        [NativeMethod(Name = "TextSelectionService::SelectToPreviousParagraph")]
        internal static extern void SelectToPreviousParagraph(IntPtr textGenerationInfo, ref int cursorIndex);

        [NativeMethod(Name = "TextSelectionService::SelectToStartOfParagraph")]
        internal static extern void SelectToStartOfParagraph(IntPtr textGenerationInfo, ref int cursorIndex);

        [NativeMethod(Name = "TextSelectionService::SelectToEndOfParagraph")]
        internal static extern void SelectToEndOfParagraph(IntPtr textGenerationInfo, ref int cursorIndex);

        [NativeMethod(Name = "TextSelectionService::SelectToNextParagraph")]
        internal static extern void SelectToNextParagraph(IntPtr textGenerationInfo, ref int cursorIndex);

        [NativeMethod(Name = "TextSelectionService::SelectCurrentParagraph")]
        internal static extern void SelectCurrentParagraph(IntPtr textGenerationInfo, ref int cursorIndex, ref int selectIndex);
    }
}
