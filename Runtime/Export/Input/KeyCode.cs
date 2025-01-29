// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    // Key codes returned by Event.keyCode.
    // If "Use Physical Keys" is enabled in Input Manager in Project Settings, these map directly to a physical key on the keyboard.
    // If "Use Physical Keys" is disabled these map to language dependent mapping, different for every platform and cannot be guaranteed to work.
    // "Use Physical Keys" is enabled by default from 2022.1
    public enum KeyCode
    {
        // Not assigned (never returned as the result of a keystroke)
        None = 0,
        // The backspace key
        Backspace       = 8,
        // The forward delete key
        Delete      = 127,
        // The tab key
        Tab     = 9,
        // The Clear key
        Clear       = 12,
        // Return key
        Return      = 13,
        // Pause on PC machines
        Pause       = 19,
        // Escape key
        Escape      = 27,
        // Space key
        Space       = 32,

        // Numeric keypad 0
        Keypad0     = 256,
        // Numeric keypad 1
        Keypad1     = 257,
        // Numeric keypad 2
        Keypad2     = 258,
        // Numeric keypad 3
        Keypad3     = 259,
        // Numeric keypad 4
        Keypad4     = 260,
        // Numeric keypad 5
        Keypad5     = 261,
        // Numeric keypad 6
        Keypad6     = 262,
        // Numeric keypad 7
        Keypad7     = 263,
        // Numeric keypad 8
        Keypad8     = 264,
        // Numeric keypad 9
        Keypad9     = 265,
        // Numeric keypad '.'
        KeypadPeriod    = 266,
        // Numeric keypad '/'
        KeypadDivide        = 267,
        // Numeric keypad '*'
        KeypadMultiply  = 268,
        // Numeric keypad '-'
        KeypadMinus     = 269,
        // Numeric keypad '+'
        KeypadPlus      = 270,
        // Numeric keypad enter
        KeypadEnter     = 271,
        // Numeric keypad '='
        KeypadEquals        = 272,

        // Up arrow key
        UpArrow         = 273,
        // Down arrow key
        DownArrow       = 274,
        // Right arrow key
        RightArrow      = 275,
        // Left arrow key
        LeftArrow       = 276,
        // Insert key key
        Insert      = 277,
        // Home key
        Home        = 278,
        // End key
        End     = 279,
        // Page up
        PageUp      = 280,
        // Page down
        PageDown        = 281,

        // F1 function key
        F1          = 282,
        // F2 function key
        F2          = 283,
        // F3 function key
        F3          = 284,
        // F4 function key
        F4          = 285,
        // F5 function key
        F5          = 286,
        // F6 function key
        F6          = 287,
        // F7 function key
        F7          = 288,
        // F8 function key
        F8          = 289,
        // F9 function key
        F9          = 290,
        // F10 function key
        F10     = 291,
        // F11 function key
        F11     = 292,
        // F12 function key
        F12     = 293,
        // F13 function key
        F13     = 294,
        // F14 function key
        F14     = 295,
        // F15 function key
        F15     = 296,

        // The '0' key on the top of the alphanumeric keyboard.
        Alpha0          = 48,
        // The '1' key on the top of the alphanumeric keyboard.
        Alpha1          = 49,
        // The '2' key on the top of the alphanumeric keyboard.
        Alpha2          = 50,
        // The '3' key on the top of the alphanumeric keyboard.
        Alpha3          = 51,
        // The '4' key on the top of the alphanumeric keyboard.
        Alpha4          = 52,
        // The '5' key on the top of the alphanumeric keyboard.
        Alpha5          = 53,
        // The '6' key on the top of the alphanumeric keyboard.
        Alpha6          = 54,
        // The '7' key on the top of the alphanumeric keyboard.
        Alpha7          = 55,
        // The '8' key on the top of the alphanumeric keyboard.
        Alpha8          = 56,
        // The '9' key on the top of the alphanumeric keyboard.
        Alpha9          = 57,

        // Exclamation mark key '!'. Deprecated if "Use Physical Keys" is enabled, use Alpha1 instead.
        Exclaim     = 33,
        // Double quote key '"'. Deprecated if "Use Physical Keys" is enabled, use Quote instead.
        DoubleQuote     = 34,
        // Hash key '#'. Deprecated if "Use Physical Keys" is enabled, use Alpha3 instead.
        Hash        = 35,
        // Dollar sign key '$'. Deprecated if "Use Physical Keys" is enabled, use Alpha4 instead.
        Dollar      = 36,
        // Percent sign key '%'. Deprecated if "Use Physical Keys" is enabled, use Alpha5 instead.
        Percent     = 37,
        // Ampersand key '&'. Deprecated if "Use Physical Keys" is enabled, use Alpha7 instead.
        Ampersand       = 38,
        // Quote key '
        Quote       = 39,
        // Left Parenthesis key '('. Deprecated if "Use Physical Keys" is enabled, use Alpha9 instead.
        LeftParen       = 40,
        // Right Parenthesis key ')'. Deprecated if "Use Physical Keys" is enabled, use Alpha0 instead.
        RightParen      = 41,
        // Asterisk key '*'. Deprecated if "Use Physical Keys" is enabled, use Alpha8 instead.
        Asterisk        = 42,
        // Plus key '+'. Deprecated if "Use Physical Keys" is enabled, use Equals instead.
        Plus        = 43,
        // Comma ',' key
        Comma       = 44,

        // Minus '-' key
        Minus       = 45,
        // Period '.' key
        Period      = 46,
        // Slash '/' key
        Slash       = 47,

        // Colon ':' key. Deprecated if "Use Physical Keys" is enabled, use Semicolon instead.
        Colon       = 58,
        // Semicolon ';' key
        Semicolon      = 59,
        // Less than '<' key. Deprecated if "Use Physical Keys" is enabled, use Comma instead.
        Less        = 60,
        // Equals '=' key
        Equals      = 61,
        // Greater than '>' key. Deprecated if "Use Physical Keys" is enabled, use Period instead.
        Greater     = 62,
        // Question mark '?' key. Deprecated if "Use Physical Keys" is enabled, use Slash instead.
        Question        = 63,
        // At key '@'. Deprecated if "Use Physical Keys" is enabled, use Alpha2 instead.
        At          = 64,

        // Left square bracket key '['
        LeftBracket = 91,
        // Backslash key '\'
        Backslash       = 92,
        // Right square bracket key ']'
        RightBracket    = 93,
        // Caret key '^'. Deprecated if "Use Physical Keys" is enabled, use Alpha6 instead.
        Caret       = 94,
        // Underscore '_' key. Deprecated if "Use Physical Keys" is enabled, use Minus instead.
        Underscore      = 95,
        // Back quote key '`'
        BackQuote       = 96,

        // 'a' key
        A           = 97,
        // 'b' key
        B           = 98,
        // 'c' key
        C           = 99,
        // 'd' key
        D           = 100,
        // 'e' key
        E           = 101,
        // 'f' key
        F           = 102,
        // 'g' key
        G           = 103,
        // 'h' key
        H           = 104,
        // 'i' key
        I           = 105,
        // 'j' key
        J           = 106,
        // 'k' key
        K           = 107,
        // 'l' key
        L           = 108,
        // 'm' key
        M           = 109,
        // 'n' key
        N           = 110,
        // 'o' key
        O           = 111,
        // 'p' key
        P           = 112,
        // 'q' key
        Q           = 113,
        // 'r' key
        R           = 114,
        // 's' key
        S           = 115,
        // 't' key
        T           = 116,
        // 'u' key
        U           = 117,
        // 'v' key
        V           = 118,
        // 'w' key
        W           = 119,
        // 'x' key
        X           = 120,
        // 'y' key
        Y           = 121,
        // 'z' key
        Z           = 122,

        // Left curly bracket key '{'. Deprecated if "Use Physical Keys" is enabled, use LeftBracket instead.
        LeftCurlyBracket        = 123,
        // Pipe key '|'. Deprecated if "Use Physical Keys" is enabled, use Backslash instead.
        Pipe        = 124,
        // Right curly bracket key '}'. Deprecated if "Use Physical Keys" is enabled, use RightBracket instead.
        RightCurlyBracket       = 125,
        // Tilde key '~'. Deprecated if "Use Physical Keys" is enabled, use BackQuote instead.
        Tilde       = 126,

        // Numlock key
        Numlock     = 300,
        // Capslock key
        CapsLock        = 301,
        // Scroll lock key
        ScrollLock      = 302,
        // Right shift key
        RightShift      = 303,
        // Left shift key
        LeftShift       = 304,
        // Right Control key
        RightControl        = 305,
        // Left Control key
        LeftControl     = 306,
        // Right Alt key
        RightAlt        = 307,
        // Left Alt key
        LeftAlt     = 308,

        // Left Meta key
        LeftMeta        = 310,
        // Left Command key
        LeftCommand     = 310,
        // Left Command key
        LeftApple       = 310,
        // Left Windows key. Deprecated if "Use Physical Keys" is enabled, use LeftMeta instead.
        LeftWindows     = 311,
        // Right Meta key
        RightMeta       = 309,
        // Right Command key
        RightCommand    = 309,
        // Right Command key
        RightApple      = 309,
        // Right Windows key. Deprecated if "Use Physical Keys" is enabled, use RightMeta instead.
        RightWindows    = 312,
        // Alt Gr key. Deprecated if "Use Physical Keys" is enabled, use RightAlt instead.
        AltGr           = 313,

        // Help key. Deprecated if "Use Physical Keys" is enabled, doesn't map to any physical key.
        Help        = 315,
        // Print key
        Print       = 316,
        // Sys Req key. Deprecated if "Use Physical Keys" is enabled, doesn't map to any physical key.
        SysReq      = 317,
        // Break key. Deprecated if "Use Physical Keys" is enabled, doesn't map to any physical key.
        Break       = 318,
        // Menu key
        Menu        = 319,

        // Mouse wheel up (roll forward)
        // This code overlaps SDL_EURO value (for European keyboards) which we don't currently use
        WheelUp = 321,
        // Mouse wheel down (roll backward)
        // This code overlaps SDL_UNDO value (for Atari keyboards) which we don't currently use
        WheelDown = 322,

        // F16 function key
        F16 = 670,
        // F17 function key
        F17 = 671,
        // F18 function key
        F18 = 672,
        // F19 function key
        F19 = 673,
        // F20 function key
        F20 = 674,
        // F21 function key
        F21 = 675,
        // F22 function key
        F22 = 676,
        // F23 function key
        F23 = 677,
        // F24 function key
        F24 = 678,

        // First (primary) mouse button
        Mouse0      = 323,
        // Second (secondary) mouse button
        Mouse1      = 324,
        // Third mouse button
        Mouse2      = 325,
        // Fourth mouse button
        Mouse3      = 326,
        // Fifth mouse button
        Mouse4      = 327,
        // Sixth mouse button
        Mouse5      = 328,
        // Seventh mouse button
        Mouse6      = 329,

        // Button 0 on any joystick
        JoystickButton0     = 330,
        // Button 1 on any joystick
        JoystickButton1     = JoystickButton0 + 1,
        // Button 2 on any joystick
        JoystickButton2     = JoystickButton0 + 2,
        // Button 3 on any joystick
        JoystickButton3     = JoystickButton0 + 3,
        // Button 4 on any joystick
        JoystickButton4     = JoystickButton0 + 4,
        // Button 5 on any joystick
        JoystickButton5     = JoystickButton0 + 5,
        // Button 6 on any joystick
        JoystickButton6     = JoystickButton0 + 6,
        // Button 7 on any joystick
        JoystickButton7     = JoystickButton0 + 7,
        // Button 8 on any joystick
        JoystickButton8     = JoystickButton0 + 8,
        // Button 9 on any joystick
        JoystickButton9     = JoystickButton0 + 9,
        // Button 10 on any joystick
        JoystickButton10    = JoystickButton0 + 10,
        // Button 11 on any joystick
        JoystickButton11    = JoystickButton0 + 11,
        // Button 12 on any joystick
        JoystickButton12    = JoystickButton0 + 12,
        // Button 13 on any joystick
        JoystickButton13    = JoystickButton0 + 13,
        // Button 14 on any joystick
        JoystickButton14    = JoystickButton0 + 14,
        // Button 15 on any joystick
        JoystickButton15    = JoystickButton0 + 15,
        // Button 16 on any joystick
        JoystickButton16    = JoystickButton0 + 16,
        // Button 17 on any joystick
        JoystickButton17    = JoystickButton0 + 17,
        // Button 18 on any joystick
        JoystickButton18    = JoystickButton0 + 18,
        // Button 19 on any joystick
        JoystickButton19    = JoystickButton0 + 19,

        // Button 0 on first joystick
        Joystick1Button0        = JoystickButton19 + 1,
        // Button 1 on first joystick
        Joystick1Button1        = Joystick1Button0 + 1,
        // Button 2 on first joystick
        Joystick1Button2        = Joystick1Button0 + 2,
        // Button 3 on first joystick
        Joystick1Button3        = Joystick1Button0 + 3,
        // Button 4 on first joystick
        Joystick1Button4        = Joystick1Button0 + 4,
        // Button 5 on first joystick
        Joystick1Button5        = Joystick1Button0 + 5,
        // Button 6 on first joystick
        Joystick1Button6        = Joystick1Button0 + 6,
        // Button 7 on first joystick
        Joystick1Button7        = Joystick1Button0 + 7,
        // Button 8 on first joystick
        Joystick1Button8        = Joystick1Button0 + 8,
        // Button 9 on first joystick
        Joystick1Button9        = Joystick1Button0 + 9,
        // Button 10 on first joystick
        Joystick1Button10   = Joystick1Button0 + 10,
        // Button 11 on first joystick
        Joystick1Button11   = Joystick1Button0 + 11,
        // Button 12 on first joystick
        Joystick1Button12   = Joystick1Button0 + 12,
        // Button 13 on first joystick
        Joystick1Button13   = Joystick1Button0 + 13,
        // Button 14 on first joystick
        Joystick1Button14   = Joystick1Button0 + 14,
        // Button 15 on first joystick
        Joystick1Button15   = Joystick1Button0 + 15,
        // Button 16 on first joystick
        Joystick1Button16   = Joystick1Button0 + 16,
        // Button 17 on first joystick
        Joystick1Button17   = Joystick1Button0 + 17,
        // Button 18 on first joystick
        Joystick1Button18   = Joystick1Button0 + 18,
        // Button 19 on first joystick
        Joystick1Button19   = Joystick1Button0 + 19,

        // Button 0 on second joystick
        Joystick2Button0        = Joystick1Button19 + 1,
        // Button 1 on second joystick
        Joystick2Button1        = Joystick2Button0 + 1,
        // Button 2 on second joystick
        Joystick2Button2        = Joystick2Button0 + 2,
        // Button 3 on second joystick
        Joystick2Button3        = Joystick2Button0 + 3,
        // Button 4 on second joystick
        Joystick2Button4        = Joystick2Button0 + 4,
        // Button 5 on second joystick
        Joystick2Button5        = Joystick2Button0 + 5,
        // Button 6 on second joystick
        Joystick2Button6        = Joystick2Button0 + 6,
        // Button 7 on second joystick
        Joystick2Button7        = Joystick2Button0 + 7,
        // Button 8 on second joystick
        Joystick2Button8        = Joystick2Button0 + 8,
        // Button 9 on second joystick
        Joystick2Button9        = Joystick2Button0 + 9,
        // Button 10 on second joystick
        Joystick2Button10   = Joystick2Button0 + 10,
        // Button 11 on second joystick
        Joystick2Button11   = Joystick2Button0 + 11,
        // Button 12 on second joystick
        Joystick2Button12   = Joystick2Button0 + 12,
        // Button 13 on second joystick
        Joystick2Button13   = Joystick2Button0 + 13,
        // Button 14 on second joystick
        Joystick2Button14   = Joystick2Button0 + 14,
        // Button 15 on second joystick
        Joystick2Button15   = Joystick2Button0 + 15,
        // Button 16 on second joystick
        Joystick2Button16   = Joystick2Button0 + 16,
        // Button 17 on second joystick
        Joystick2Button17   = Joystick2Button0 + 17,
        // Button 18 on second joystick
        Joystick2Button18   = Joystick2Button0 + 18,
        // Button 19 on second joystick
        Joystick2Button19   = Joystick2Button0 + 19,

        // Button 0 on third joystick
        Joystick3Button0        = Joystick2Button19 + 1,
        // Button 1 on third joystick
        Joystick3Button1        = Joystick3Button0 + 1,
        // Button 2 on third joystick
        Joystick3Button2        = Joystick3Button0 + 2,
        // Button 3 on third joystick
        Joystick3Button3        = Joystick3Button0 + 3,
        // Button 4 on third joystick
        Joystick3Button4        = Joystick3Button0 + 4,
        // Button 5 on third joystick
        Joystick3Button5        = Joystick3Button0 + 5,
        // Button 6 on third joystick
        Joystick3Button6        = Joystick3Button0 + 6,
        // Button 7 on third joystick
        Joystick3Button7        = Joystick3Button0 + 7,
        // Button 8 on third joystick
        Joystick3Button8        = Joystick3Button0 + 8,
        // Button 9 on third joystick
        Joystick3Button9        = Joystick3Button0 + 9,
        // Button 10 on third joystick
        Joystick3Button10   = Joystick3Button0 + 10,
        // Button 11 on third joystick
        Joystick3Button11   = Joystick3Button0 + 11,
        // Button 12 on third joystick
        Joystick3Button12   = Joystick3Button0 + 12,
        // Button 13 on third joystick
        Joystick3Button13   = Joystick3Button0 + 13,
        // Button 14 on third joystick
        Joystick3Button14   = Joystick3Button0 + 14,
        // Button 15 on third joystick
        Joystick3Button15   = Joystick3Button0 + 15,
        // Button 16 on third joystick
        Joystick3Button16   = Joystick3Button0 + 16,
        // Button 17 on third joystick
        Joystick3Button17   = Joystick3Button0 + 17,
        // Button 18 on third joystick
        Joystick3Button18   = Joystick3Button0 + 18,
        // Button 19 on third joystick
        Joystick3Button19   = Joystick3Button0 + 19,

        // Button 0 on forth joystick
        Joystick4Button0        = Joystick3Button19 + 1,
        // Button 1 on forth joystick
        Joystick4Button1        = Joystick4Button0 + 1,
        // Button 2 on forth joystick
        Joystick4Button2        = Joystick4Button0 + 2,
        // Button 3 on forth joystick
        Joystick4Button3        = Joystick4Button0 + 3,
        // Button 4 on forth joystick
        Joystick4Button4        = Joystick4Button0 + 4,
        // Button 5 on forth joystick
        Joystick4Button5        = Joystick4Button0 + 5,
        // Button 6 on forth joystick
        Joystick4Button6        = Joystick4Button0 + 6,
        // Button 7 on forth joystick
        Joystick4Button7        = Joystick4Button0 + 7,
        // Button 8 on forth joystick
        Joystick4Button8        = Joystick4Button0 + 8,
        // Button 9 on forth joystick
        Joystick4Button9        = Joystick4Button0 + 9,
        // Button 10 on forth joystick
        Joystick4Button10   = Joystick4Button0 + 10,
        // Button 11 on forth joystick
        Joystick4Button11   = Joystick4Button0 + 11,
        // Button 12 on forth joystick
        Joystick4Button12   = Joystick4Button0 + 12,
        // Button 13 on forth joystick
        Joystick4Button13   = Joystick4Button0 + 13,
        // Button 14 on forth joystick
        Joystick4Button14   = Joystick4Button0 + 14,
        // Button 15 on forth joystick
        Joystick4Button15   = Joystick4Button0 + 15,
        // Button 16 on forth joystick
        Joystick4Button16   = Joystick4Button0 + 16,
        // Button 17 on forth joystick
        Joystick4Button17   = Joystick4Button0 + 17,
        // Button 18 on forth joystick
        Joystick4Button18   = Joystick4Button0 + 18,
        // Button 19 on forth joystick
        Joystick4Button19   = Joystick4Button0 + 19,

        // Button 0 on fifth joystick
        Joystick5Button0        = Joystick4Button19 + 1,
        // Button 1 on fifth joystick
        Joystick5Button1        = Joystick5Button0 + 1,
        // Button 2 on fifth joystick
        Joystick5Button2        = Joystick5Button0 + 2,
        // Button 3 on fifth joystick
        Joystick5Button3        = Joystick5Button0 + 3,
        // Button 4 on fifth joystick
        Joystick5Button4        = Joystick5Button0 + 4,
        // Button 5 on fifth joystick
        Joystick5Button5        = Joystick5Button0 + 5,
        // Button 6 on fifth joystick
        Joystick5Button6        = Joystick5Button0 + 6,
        // Button 7 on fifth joystick
        Joystick5Button7        = Joystick5Button0 + 7,
        // Button 8 on fifth joystick
        Joystick5Button8        = Joystick5Button0 + 8,
        // Button 9 on fifth joystick
        Joystick5Button9        = Joystick5Button0 + 9,
        // Button 10 on fifth joystick
        Joystick5Button10   = Joystick5Button0 + 10,
        // Button 11 on fifth joystick
        Joystick5Button11   = Joystick5Button0 + 11,
        // Button 12 on fifth joystick
        Joystick5Button12   = Joystick5Button0 + 12,
        // Button 13 on fifth joystick
        Joystick5Button13   = Joystick5Button0 + 13,
        // Button 14 on fifth joystick
        Joystick5Button14   = Joystick5Button0 + 14,
        // Button 15 on fifth joystick
        Joystick5Button15   = Joystick5Button0 + 15,
        // Button 16 on fifth joystick
        Joystick5Button16   = Joystick5Button0 + 16,
        // Button 17 on fifth joystick
        Joystick5Button17   = Joystick5Button0 + 17,
        // Button 18 on fifth joystick
        Joystick5Button18   = Joystick5Button0 + 18,
        // Button 19 on fifth joystick
        Joystick5Button19   = Joystick5Button0 + 19,

        // Button 0 on sixth joystick
        Joystick6Button0        = Joystick5Button19 + 1,
        // Button 1 on sixth joystick
        Joystick6Button1        = Joystick6Button0 + 1,
        // Button 2 on sixth joystick
        Joystick6Button2        = Joystick6Button0 + 2,
        // Button 3 on sixth joystick
        Joystick6Button3        = Joystick6Button0 + 3,
        // Button 4 on sixth joystick
        Joystick6Button4        = Joystick6Button0 + 4,
        // Button 5 on sixth joystick
        Joystick6Button5        = Joystick6Button0 + 5,
        // Button 6 on sixth joystick
        Joystick6Button6        = Joystick6Button0 + 6,
        // Button 7 on sixth joystick
        Joystick6Button7        = Joystick6Button0 + 7,
        // Button 8 on sixth joystick
        Joystick6Button8        = Joystick6Button0 + 8,
        // Button 9 on sixth joystick
        Joystick6Button9        = Joystick6Button0 + 9,
        // Button 10 on sixth joystick
        Joystick6Button10   = Joystick6Button0 + 10,
        // Button 11 on sixth joystick
        Joystick6Button11   = Joystick6Button0 + 11,
        // Button 12 on sixth joystick
        Joystick6Button12   = Joystick6Button0 + 12,
        // Button 13 on sixth joystick
        Joystick6Button13   = Joystick6Button0 + 13,
        // Button 14 on sixth joystick
        Joystick6Button14   = Joystick6Button0 + 14,
        // Button 15 on sixth joystick
        Joystick6Button15   = Joystick6Button0 + 15,
        // Button 16 on sixth joystick
        Joystick6Button16   = Joystick6Button0 + 16,
        // Button 17 on sixth joystick
        Joystick6Button17   = Joystick6Button0 + 17,
        // Button 18 on sixth joystick
        Joystick6Button18   = Joystick6Button0 + 18,
        // Button 19 on sixth joystick
        Joystick6Button19   = Joystick6Button0 + 19,

        // Button 0 on seventh joystick
        Joystick7Button0        = Joystick6Button19 + 1,
        // Button 1 on seventh joystick
        Joystick7Button1        = Joystick7Button0 + 1,
        // Button 2 on seventh joystick
        Joystick7Button2        = Joystick7Button0 + 2,
        // Button 3 on seventh joystick
        Joystick7Button3        = Joystick7Button0 + 3,
        // Button 4 on seventh joystick
        Joystick7Button4        = Joystick7Button0 + 4,
        // Button 5 on seventh joystick
        Joystick7Button5        = Joystick7Button0 + 5,
        // Button 6 on seventh joystick
        Joystick7Button6        = Joystick7Button0 + 6,
        // Button 7 on seventh joystick
        Joystick7Button7        = Joystick7Button0 + 7,
        // Button 8 on seventh joystick
        Joystick7Button8        = Joystick7Button0 + 8,
        // Button 9 on seventh joystick
        Joystick7Button9        = Joystick7Button0 + 9,
        // Button 10 on seventh joystick
        Joystick7Button10   = Joystick7Button0 + 10,
        // Button 11 on seventh joystick
        Joystick7Button11   = Joystick7Button0 + 11,
        // Button 12 on seventh joystick
        Joystick7Button12   = Joystick7Button0 + 12,
        // Button 13 on seventh joystick
        Joystick7Button13   = Joystick7Button0 + 13,
        // Button 14 on seventh joystick
        Joystick7Button14   = Joystick7Button0 + 14,
        // Button 15 on seventh joystick
        Joystick7Button15   = Joystick7Button0 + 15,
        // Button 16 on seventh joystick
        Joystick7Button16   = Joystick7Button0 + 16,
        // Button 17 on seventh joystick
        Joystick7Button17   = Joystick7Button0 + 17,
        // Button 18 on seventh joystick
        Joystick7Button18   = Joystick7Button0 + 18,
        // Button 19 on seventh joystick
        Joystick7Button19   = Joystick7Button0 + 19,

        // Button 0 on eight joystick
        Joystick8Button0        = Joystick7Button19 + 1,
        // Button 1 on eight joystick
        Joystick8Button1        = Joystick8Button0 + 1,
        // Button 2 on eight joystick
        Joystick8Button2        = Joystick8Button0 + 2,
        // Button 3 on eight joystick
        Joystick8Button3        = Joystick8Button0 + 3,
        // Button 4 on eight joystick
        Joystick8Button4        = Joystick8Button0 + 4,
        // Button 5 on eight joystick
        Joystick8Button5        = Joystick8Button0 + 5,
        // Button 6 on eight joystick
        Joystick8Button6        = Joystick8Button0 + 6,
        // Button 7 on eight joystick
        Joystick8Button7        = Joystick8Button0 + 7,
        // Button 8 on eight joystick
        Joystick8Button8        = Joystick8Button0 + 8,
        // Button 9 on eight joystick
        Joystick8Button9        = Joystick8Button0 + 9,
        // Button 10 on eight joystick
        Joystick8Button10   = Joystick8Button0 + 10,
        // Button 11 on eight joystick
        Joystick8Button11   = Joystick8Button0 + 11,
        // Button 12 on eight joystick
        Joystick8Button12   = Joystick8Button0 + 12,
        // Button 13 on eight joystick
        Joystick8Button13   = Joystick8Button0 + 13,
        // Button 14 on eight joystick
        Joystick8Button14   = Joystick8Button0 + 14,
        // Button 15 on eight joystick
        Joystick8Button15   = Joystick8Button0 + 15,
        // Button 16 on eight joystick
        Joystick8Button16   = Joystick8Button0 + 16,
        // Button 17 on eight joystick
        Joystick8Button17   = Joystick8Button0 + 17,
        // Button 18 on eight joystick
        Joystick8Button18   = Joystick8Button0 + 18,
        // Button 19 on eight joystick
        Joystick8Button19   = Joystick8Button0 + 19,

        // We could expose all 10 joysticks here, but I think that a user would rarely want to explicitly
        // specify an eight or higher joystick (and they still can using the string version of Input.KeyDown).
        // Eight joysticks, however, are a common setup (especially on consoles), and we've had a bug report
        // for only exposing three, so I added eight.
    }
}
