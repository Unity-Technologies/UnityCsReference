// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore.Text
{
    [Flags]
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
    internal enum EditingEventFlags
    {
        None = 0,
        CursorIndexChanged = 1 << 0,
        SelectIndexChanged = 1 << 1,
        RevealCursorChanged = 1 << 2,
        TextChanged = 1 << 3,
    }

    [NativeHeader("Modules/TextCoreTextEngine/Native/TextSelectionService.h")]
    [VisibleToOtherModules("UnityEngine.UIElementsModule","Unity.UIElements.PlayModeTests")]
    internal class TextSelectionService
    {
        [NativeMethod(Name = "TextSelectionService::PreviousCodePointIndex")]
        internal static extern int PreviousCodePointIndex(IntPtr textGenerationInfo, int currentIndex);

        [NativeMethod(Name = "TextSelectionService::NextCodePointIndex")]
        internal static extern int NextCodePointIndex(IntPtr textGenerationInfo, int currentIndex);

        [NativeMethod(Name = "TextSelectionService::GetCursorLogicalIndexFromPosition")]
        internal static extern int GetCursorLogicalIndexFromPosition(IntPtr textGenerationInfo, Vector2 position);

        [NativeMethod(Name = "TextSelectionService::GetCursorPositionFromLogicalIndex")]
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern Vector2 GetCursorPositionFromLogicalIndex(IntPtr textGenerationInfo, int logicalIndex);

        [NativeMethod(Name = "TextSelectionService::GetCursorPositionFromCursorIndex")]
        internal static extern Vector2 GetCursorPositionFromCursorIndex(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::GetHighlightRectangles")]
        internal static extern Rect[] GetHighlightRectangles(IntPtr textGenerationInfo, int cursorIndex, int selectIndex);

        [NativeMethod(Name = "TextSelectionService::GetCharacterHeightFromIndex")]
        internal static extern float GetCharacterHeightFromIndex(IntPtr textGenerationInfo, int index);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        [NativeMethod(Name = "TextSelectionService::GetStartOfNextWord")]
        internal static extern int GetStartOfNextWord(IntPtr textGenerationInfo, int currentIndex);

        [NativeMethod(Name = "TextSelectionService::GetEndOfPreviousWord")]
        internal static extern int GetEndOfPreviousWord(IntPtr textGenerationInfo, int currentIndex);

        /// <summary>
        /// Returns both the start (inclusive) and end (exclusive) of the word at index (read-only).
        /// startIndex is set to -1 when index is on whitespace; endIndex still marks the segment boundary.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        [NativeMethod(Name = "TextSelectionService::ComputeWordBounds")]
        internal static extern void ComputeWordBounds(IntPtr textGenerationInfo, int index, out int startIndex, out int endIndex);

        [NativeMethod(Name = "TextSelectionService::GetFirstCharacterIndexOnLine")]
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern int GetFirstCharacterIndexOnLine(IntPtr textGenerationInfo, int currentIndex);

        [NativeMethod(Name = "TextSelectionService::GetLastCharacterIndexOnLine")]
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern int GetLastCharacterIndexOnLine(IntPtr textGenerationInfo, int currentIndex);

        [NativeMethod(Name = "TextSelectionService::GetLineNumberFromLogicalIndex")]
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern int GetLineNumber(IntPtr textGenerationInfo, int logicalIndex);

        [NativeMethod(Name = "TextSelectionService::GetGlyphIndex")]
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern int GetGlyphIndex(IntPtr textGenerationInfo, int logicalIndex);

        [NativeMethod(Name = "TextSelectionService::GetValidPointIndex")]
        internal static extern int GetValidPointIndex(IntPtr textGenerationInfo, int index);

        // State accessors
        [NativeMethod(Name = "TextSelectionService::GetCursorIndex")]
        internal static extern int GetCursorIndex(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SetCursorIndex")]
        internal static extern bool SetCursorIndex(IntPtr textGenerationInfo, int value);

        [NativeMethod(Name = "TextSelectionService::GetCursorIndexNoValidation")]
        internal static extern int GetCursorIndexNoValidation(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::GetSelectIndex")]
        internal static extern int GetSelectIndex(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SetSelectIndex")]
        internal static extern bool SetSelectIndex(IntPtr textGenerationInfo, int value);

        [NativeMethod(Name = "TextSelectionService::GetSelectIndexNoValidation")]
        internal static extern int GetSelectIndexNoValidation(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::GetHasSelection")]
        internal static extern bool GetHasSelection(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::GetRevealCursor")]
        internal static extern bool GetRevealCursor(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SetRevealCursor")]
        internal static extern bool SetRevealCursor(IntPtr textGenerationInfo, bool value);

        [NativeMethod(Name = "TextSelectionService::GetIAltCursorPos")]
        internal static extern int GetIAltCursorPos(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SetIAltCursorPos")]
        internal static extern void SetIAltCursorPos(IntPtr textGenerationInfo, int value);

        [NativeMethod(Name = "TextSelectionService::GetSelectedText")]
        internal static extern string GetSelectedText(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::ClearCursorPos")]
        internal static extern void ClearCursorPos(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectAll")]
        internal static extern int SelectAll(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectNone")]
        internal static extern int SelectNone(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectLeft")]
        internal static extern int SelectLeft(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectRight")]
        internal static extern int SelectRight(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectUp")]
        internal static extern int SelectUp(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectDown")]
        internal static extern int SelectDown(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectTextStart")]
        internal static extern int SelectTextStart(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectTextEnd")]
        internal static extern int SelectTextEnd(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectToStartOfNextWord")]
        internal static extern int SelectToStartOfNextWord(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectToEndOfPreviousWord")]
        internal static extern int SelectToEndOfPreviousWord(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectWordRight")]
        internal static extern int SelectWordRight(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectWordLeft")]
        internal static extern int SelectWordLeft(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectGraphicalLineStart")]
        internal static extern int SelectGraphicalLineStart(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectGraphicalLineEnd")]
        internal static extern int SelectGraphicalLineEnd(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectParagraphForward")]
        internal static extern int SelectParagraphForward(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectParagraphBackward")]
        internal static extern int SelectParagraphBackward(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectCurrentWord")]
        internal static extern int SelectCurrentWord(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectCurrentParagraph")]
        internal static extern int SelectCurrentParagraph(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectToNextParagraph")]
        internal static extern int SelectToNextParagraph(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectToEndOfParagraph")]
        internal static extern int SelectToEndOfParagraph(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectToPreviousParagraph")]
        internal static extern int SelectToPreviousParagraph(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SelectToStartOfParagraph")]
        internal static extern int SelectToStartOfParagraph(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveLeft")]
        internal static extern int MoveLeft(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveRight")]
        internal static extern int MoveRight(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveUp")]
        internal static extern int MoveUp(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveDown")]
        internal static extern int MoveDown(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveLineStart")]
        internal static extern int MoveLineStart(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveLineEnd")]
        internal static extern int MoveLineEnd(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveGraphicalLineStart")]
        internal static extern int MoveGraphicalLineStart(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveGraphicalLineEnd")]
        internal static extern int MoveGraphicalLineEnd(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveTextStart")]
        internal static extern int MoveTextStart(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveTextEnd")]
        internal static extern int MoveTextEnd(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveParagraphForward")]
        internal static extern int MoveParagraphForward(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveParagraphBackward")]
        internal static extern int MoveParagraphBackward(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveWordRight")]
        internal static extern int MoveWordRight(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveWordLeft")]
        internal static extern int MoveWordLeft(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveToStartOfNextWord")]
        internal static extern int MoveToStartOfNextWord(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MoveToEndOfPreviousWord")]
        internal static extern int MoveToEndOfPreviousWord(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::ExpandSelectGraphicalLineStart")]
        internal static extern int ExpandSelectGraphicalLineStart(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::ExpandSelectGraphicalLineEnd")]
        internal static extern int ExpandSelectGraphicalLineEnd(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::MouseDragSelectsWholeWords")]
        internal static extern void MouseDragSelectsWholeWords(IntPtr textGenerationInfo, bool on);

        [NativeMethod(Name = "TextSelectionService::GetDblClickSnap")]
        internal static extern int GetDblClickSnap(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SetDblClickSnap")]
        internal static extern void SetDblClickSnap(IntPtr textGenerationInfo, int snap);

        [NativeMethod(Name = "TextSelectionService::GetHasHorizontalCursorPos")]
        internal static extern bool GetHasHorizontalCursorPos(IntPtr textGenerationInfo);

        [NativeMethod(Name = "TextSelectionService::SetHasHorizontalCursorPos")]
        internal static extern void SetHasHorizontalCursorPos(IntPtr textGenerationInfo, bool value);

        [NativeMethod(Name = "TextSelectionService::MoveCursorToPosition")]
        internal static extern int MoveCursorToPosition(IntPtr textGenerationInfo, Vector2 position, bool shift);

        [NativeMethod(Name = "TextSelectionService::MoveAltCursorToPosition")]
        internal static extern void MoveAltCursorToPosition(IntPtr textGenerationInfo, Vector2 position);

        [NativeMethod(Name = "TextSelectionService::IsOverSelection")]
        internal static extern bool IsOverSelection(IntPtr textGenerationInfo, Vector2 position);

        [NativeMethod(Name = "TextSelectionService::SelectToPosition")]
        internal static extern int SelectToPosition(IntPtr textGenerationInfo, Vector2 position);
    }
}
