// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text;
using Unity.IntegerTime;

namespace UnityEngine.InputForUI
{
    /// <summary>
    /// Triggered when printable characters are entered via either:
    /// - Direct key pressed on languages where it's possible like in US English layout pressing "A" key on keyboard would trigger text input event with "a" character (unless Shift is held).
    /// - IME input popup submitting the string, that would trigger a text input event per character in the string.
    /// - Emoji input popup, that would trigger a text input event per emoji part (one emoji could be multiple Unicode codepoints)
    /// - On-screen keyboard submitting string, that would trigger a text input event per character in the string.
    ///
    /// This event shouldn't be relied upon for text editing like backspace, tab, del, etc.
    /// </summary>
    internal struct TextInputEvent : IEventProperties
    {
        // TODO Store UTF-32 because UTF-16 might store individual codepoint over multiple UTF-16 code units.
        // TODO is it a good idea? or should we stick with char? if we use char what happens if some text events are dropped?
        // TODO maybe we should just pass string to submit an atomic transaction.
        public char character;

        public DiscreteTime timestamp { get; set; }
        public EventSource eventSource { get; set; }
        public uint playerId { get; set; }
        public EventModifiers eventModifiers { get; set; }

        public override string ToString()
        {
            var str = character == 0 ? string.Empty : character.ToString();
            return $"text input 0x{(int)character:x8} '{str}'";
        }

        public static bool ShouldBeProcessed(char character)
        {
            // Only process printable characters
            return (character > 31 && character != 127);
        }
    }
}
