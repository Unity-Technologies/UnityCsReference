// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    // Describes the type of keyboard.
    public enum TouchScreenKeyboardType
    {
        // The default keyboard layout of the target platform.
        Default = 0,
        // Keyboard with standard ASCII keys.
        ASCIICapable = 1,
        // Keyboard with numbers and punctuation mark keys.
        NumbersAndPunctuation = 2,
        // Keyboard optimized for URL entry, features ".", "/", and ".com"
        URL = 3,
        // Keyboard with standard numeric keys, suitable for typing PINs or passwords
        NumberPad = 4,
        // Keyboard with a layout suitable for typing telephone numbers, has the numeric 0 to 9, the "*", and "#" keys
        PhonePad = 5,
        // Keyboard with alphanumeric keys designed for entering a person's name or phone number.
        NamePhonePad = 6,
        // Keyboard with additional keys suitable for typing email addresses, features the "@" and "."
        EmailAddress = 7,
        // Keyboard with the Nintendo Network Account key layout (only available on the Wii U)
        NintendoNetworkAccount = 8,
        // Keyboard with symbol keys often used on social media such as Twitter, features the "@" (and "#" on iOS/tvOS)
        Social = 9,
        // Keyboard optimized for search terms, features the space and "."
        Search = 10
    }
}
