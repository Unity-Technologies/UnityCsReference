// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    // Key codes returned by Event.keyCode. These map directly to a physical key on the keyboard.
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

        // Exclamation mark key '!'
        Exclaim     = 33,
        // Double quote key '"'
        DoubleQuote     = 34,
        // Hash key '#'
        Hash        = 35,
        // Dollar sign key '$'
        Dollar      = 36,
        // Ampersand key '&'
        Ampersand       = 38,
        // Quote key '
        Quote       = 39,
        // Left Parenthesis key '('
        LeftParen       = 40,
        // Right Parenthesis key ')'
        RightParen      = 41,
        // Asterisk key '*'
        Asterisk        = 42,
        // Plus key '+'
        Plus        = 43,
        // Comma ',' key
        Comma       = 44,

        // Minus '-' key
        Minus       = 45,
        // Period '.' key
        Period      = 46,
        // Slash '/' key
        Slash       = 47,

        // Colon ':' key
        Colon       = 58,
        // Semicolon ';' key
        Semicolon      = 59,
        // Less than '<' key
        Less        = 60,
        // Equals '=' key
        Equals      = 61,
        // Greater than '>' key
        Greater     = 62,
        // Question mark '?' key
        Question        = 63,
        // At key '@'
        At          = 64,

        // Left square bracket key '['
        LeftBracket = 91,
        // Backslash key '\'
        Backslash       = 92,
        // Right square bracket key ']'
        RightBracket    = 93,
        // Caret key '^'
        Caret       = 94,
        // Underscore '_' key
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

        // Left Command key
        LeftCommand     = 310,
        // Left Command key
        LeftApple       = 310,
        // Left Windows key
        LeftWindows     = 311,
        // Right Command key
        RightCommand    = 309,
        // Right Command key
        RightApple      = 309,
        // Right Windows key
        RightWindows        = 312,
        // Alt Gr key
        AltGr       = 313,

        // Help key
        Help        = 315,
        // Print key
        Print       = 316,
        // Sys Req key
        SysReq      = 317,
        // Break key
        Break       = 318,
        // Menu key
        Menu        = 319,

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
        JoystickButton1     = 331,
        // Button 2 on any joystick
        JoystickButton2     = 332,
        // Button 3 on any joystick
        JoystickButton3     = 333,
        // Button 4 on any joystick
        JoystickButton4     = 334,
        // Button 5 on any joystick
        JoystickButton5     = 335,
        // Button 6 on any joystick
        JoystickButton6     = 336,
        // Button 7 on any joystick
        JoystickButton7     = 337,
        // Button 8 on any joystick
        JoystickButton8     = 338,
        // Button 9 on any joystick
        JoystickButton9     = 339,
        // Button 10 on any joystick
        JoystickButton10    = 340,
        // Button 11 on any joystick
        JoystickButton11    = 341,
        // Button 12 on any joystick
        JoystickButton12    = 342,
        // Button 13 on any joystick
        JoystickButton13    = 343,
        // Button 14 on any joystick
        JoystickButton14    = 344,
        // Button 15 on any joystick
        JoystickButton15    = 345,
        // Button 16 on any joystick
        JoystickButton16    = 346,
        // Button 17 on any joystick
        JoystickButton17    = 347,
        // Button 18 on any joystick
        JoystickButton18    = 348,
        // Button 19 on any joystick
        JoystickButton19    = 349,

        // Button 0 on first joystick
        Joystick1Button0        = 350,
        // Button 1 on first joystick
        Joystick1Button1        = 351,
        // Button 2 on first joystick
        Joystick1Button2        = 352,
        // Button 3 on first joystick
        Joystick1Button3        = 353,
        // Button 4 on first joystick
        Joystick1Button4        = 354,
        // Button 5 on first joystick
        Joystick1Button5        = 355,
        // Button 6 on first joystick
        Joystick1Button6        = 356,
        // Button 7 on first joystick
        Joystick1Button7        = 357,
        // Button 8 on first joystick
        Joystick1Button8        = 358,
        // Button 9 on first joystick
        Joystick1Button9        = 359,
        // Button 10 on first joystick
        Joystick1Button10   = 360,
        // Button 11 on first joystick
        Joystick1Button11   = 361,
        // Button 12 on first joystick
        Joystick1Button12   = 362,
        // Button 13 on first joystick
        Joystick1Button13   = 363,
        // Button 14 on first joystick
        Joystick1Button14   = 364,
        // Button 15 on first joystick
        Joystick1Button15   = 365,
        // Button 16 on first joystick
        Joystick1Button16   = 366,
        // Button 17 on first joystick
        Joystick1Button17   = 367,
        // Button 18 on first joystick
        Joystick1Button18   = 368,
        // Button 19 on first joystick
        Joystick1Button19   = 369,

        // Button 0 on second joystick
        Joystick2Button0        = 370,
        // Button 1 on second joystick
        Joystick2Button1        = 371,
        // Button 2 on second joystick
        Joystick2Button2        = 372,
        // Button 3 on second joystick
        Joystick2Button3        = 373,
        // Button 4 on second joystick
        Joystick2Button4        = 374,
        // Button 5 on second joystick
        Joystick2Button5        = 375,
        // Button 6 on second joystick
        Joystick2Button6        = 376,
        // Button 7 on second joystick
        Joystick2Button7        = 377,
        // Button 8 on second joystick
        Joystick2Button8        = 378,
        // Button 9 on second joystick
        Joystick2Button9        = 379,
        // Button 10 on second joystick
        Joystick2Button10   = 380,
        // Button 11 on second joystick
        Joystick2Button11   = 381,
        // Button 12 on second joystick
        Joystick2Button12   = 382,
        // Button 13 on second joystick
        Joystick2Button13   = 383,
        // Button 14 on second joystick
        Joystick2Button14   = 384,
        // Button 15 on second joystick
        Joystick2Button15   = 385,
        // Button 16 on second joystick
        Joystick2Button16   = 386,
        // Button 17 on second joystick
        Joystick2Button17   = 387,
        // Button 18 on second joystick
        Joystick2Button18   = 388,
        // Button 19 on second joystick
        Joystick2Button19   = 389,

        // Button 0 on third joystick
        Joystick3Button0        = 390,
        // Button 1 on third joystick
        Joystick3Button1        = 391,
        // Button 2 on third joystick
        Joystick3Button2        = 392,
        // Button 3 on third joystick
        Joystick3Button3        = 393,
        // Button 4 on third joystick
        Joystick3Button4        = 394,
        // Button 5 on third joystick
        Joystick3Button5        = 395,
        // Button 6 on third joystick
        Joystick3Button6        = 396,
        // Button 7 on third joystick
        Joystick3Button7        = 397,
        // Button 8 on third joystick
        Joystick3Button8        = 398,
        // Button 9 on third joystick
        Joystick3Button9        = 399,
        // Button 10 on third joystick
        Joystick3Button10   = 400,
        // Button 11 on third joystick
        Joystick3Button11   = 401,
        // Button 12 on third joystick
        Joystick3Button12   = 402,
        // Button 13 on third joystick
        Joystick3Button13   = 403,
        // Button 14 on third joystick
        Joystick3Button14   = 404,
        // Button 15 on third joystick
        Joystick3Button15   = 405,
        // Button 16 on third joystick
        Joystick3Button16   = 406,
        // Button 17 on third joystick
        Joystick3Button17   = 407,
        // Button 18 on third joystick
        Joystick3Button18   = 408,
        // Button 19 on third joystick
        Joystick3Button19   = 409,

        // Button 0 on forth joystick
        Joystick4Button0        = 410,
        // Button 1 on forth joystick
        Joystick4Button1        = 411,
        // Button 2 on forth joystick
        Joystick4Button2        = 412,
        // Button 3 on forth joystick
        Joystick4Button3        = 413,
        // Button 4 on forth joystick
        Joystick4Button4        = 414,
        // Button 5 on forth joystick
        Joystick4Button5        = 415,
        // Button 6 on forth joystick
        Joystick4Button6        = 416,
        // Button 7 on forth joystick
        Joystick4Button7        = 417,
        // Button 8 on forth joystick
        Joystick4Button8        = 418,
        // Button 9 on forth joystick
        Joystick4Button9        = 419,
        // Button 10 on forth joystick
        Joystick4Button10   = 420,
        // Button 11 on forth joystick
        Joystick4Button11   = 421,
        // Button 12 on forth joystick
        Joystick4Button12   = 422,
        // Button 13 on forth joystick
        Joystick4Button13   = 423,
        // Button 14 on forth joystick
        Joystick4Button14   = 424,
        // Button 15 on forth joystick
        Joystick4Button15   = 425,
        // Button 16 on forth joystick
        Joystick4Button16   = 426,
        // Button 17 on forth joystick
        Joystick4Button17   = 427,
        // Button 18 on forth joystick
        Joystick4Button18   = 428,
        // Button 19 on forth joystick
        Joystick4Button19   = 429,

        // Button 0 on fifth joystick
        Joystick5Button0        = 430,
        // Button 1 on fifth joystick
        Joystick5Button1        = 431,
        // Button 2 on fifth joystick
        Joystick5Button2        = 432,
        // Button 3 on fifth joystick
        Joystick5Button3        = 433,
        // Button 4 on fifth joystick
        Joystick5Button4        = 434,
        // Button 5 on fifth joystick
        Joystick5Button5        = 435,
        // Button 6 on fifth joystick
        Joystick5Button6        = 436,
        // Button 7 on fifth joystick
        Joystick5Button7        = 437,
        // Button 8 on fifth joystick
        Joystick5Button8        = 438,
        // Button 9 on fifth joystick
        Joystick5Button9        = 439,
        // Button 10 on fifth joystick
        Joystick5Button10   = 440,
        // Button 11 on fifth joystick
        Joystick5Button11   = 441,
        // Button 12 on fifth joystick
        Joystick5Button12   = 442,
        // Button 13 on fifth joystick
        Joystick5Button13   = 443,
        // Button 14 on fifth joystick
        Joystick5Button14   = 444,
        // Button 15 on fifth joystick
        Joystick5Button15   = 445,
        // Button 16 on fifth joystick
        Joystick5Button16   = 446,
        // Button 17 on fifth joystick
        Joystick5Button17   = 447,
        // Button 18 on fifth joystick
        Joystick5Button18   = 448,
        // Button 19 on fifth joystick
        Joystick5Button19   = 449,

        // Button 0 on sixth joystick
        Joystick6Button0        = 450,
        // Button 1 on sixth joystick
        Joystick6Button1        = 451,
        // Button 2 on sixth joystick
        Joystick6Button2        = 452,
        // Button 3 on sixth joystick
        Joystick6Button3        = 453,
        // Button 4 on sixth joystick
        Joystick6Button4        = 454,
        // Button 5 on sixth joystick
        Joystick6Button5        = 455,
        // Button 6 on sixth joystick
        Joystick6Button6        = 456,
        // Button 7 on sixth joystick
        Joystick6Button7        = 457,
        // Button 8 on sixth joystick
        Joystick6Button8        = 458,
        // Button 9 on sixth joystick
        Joystick6Button9        = 459,
        // Button 10 on sixth joystick
        Joystick6Button10   = 460,
        // Button 11 on sixth joystick
        Joystick6Button11   = 461,
        // Button 12 on sixth joystick
        Joystick6Button12   = 462,
        // Button 13 on sixth joystick
        Joystick6Button13   = 463,
        // Button 14 on sixth joystick
        Joystick6Button14   = 464,
        // Button 15 on sixth joystick
        Joystick6Button15   = 465,
        // Button 16 on sixth joystick
        Joystick6Button16   = 466,
        // Button 17 on sixth joystick
        Joystick6Button17   = 467,
        // Button 18 on sixth joystick
        Joystick6Button18   = 468,
        // Button 19 on sixth joystick
        Joystick6Button19   = 469,

        // Button 0 on seventh joystick
        Joystick7Button0        = 470,
        // Button 1 on seventh joystick
        Joystick7Button1        = 471,
        // Button 2 on seventh joystick
        Joystick7Button2        = 472,
        // Button 3 on seventh joystick
        Joystick7Button3        = 473,
        // Button 4 on seventh joystick
        Joystick7Button4        = 474,
        // Button 5 on seventh joystick
        Joystick7Button5        = 475,
        // Button 6 on seventh joystick
        Joystick7Button6        = 476,
        // Button 7 on seventh joystick
        Joystick7Button7        = 477,
        // Button 8 on seventh joystick
        Joystick7Button8        = 478,
        // Button 9 on seventh joystick
        Joystick7Button9        = 479,
        // Button 10 on seventh joystick
        Joystick7Button10   = 480,
        // Button 11 on seventh joystick
        Joystick7Button11   = 481,
        // Button 12 on seventh joystick
        Joystick7Button12   = 482,
        // Button 13 on seventh joystick
        Joystick7Button13   = 483,
        // Button 14 on seventh joystick
        Joystick7Button14   = 484,
        // Button 15 on seventh joystick
        Joystick7Button15   = 485,
        // Button 16 on seventh joystick
        Joystick7Button16   = 486,
        // Button 17 on seventh joystick
        Joystick7Button17   = 487,
        // Button 18 on seventh joystick
        Joystick7Button18   = 488,
        // Button 19 on seventh joystick
        Joystick7Button19   = 489,

        // Button 0 on eight joystick
        Joystick8Button0        = 490,
        // Button 1 on eight joystick
        Joystick8Button1        = 491,
        // Button 2 on eight joystick
        Joystick8Button2        = 492,
        // Button 3 on eight joystick
        Joystick8Button3        = 493,
        // Button 4 on eight joystick
        Joystick8Button4        = 494,
        // Button 5 on eight joystick
        Joystick8Button5        = 495,
        // Button 6 on eight joystick
        Joystick8Button6        = 496,
        // Button 7 on eight joystick
        Joystick8Button7        = 497,
        // Button 8 on eight joystick
        Joystick8Button8        = 498,
        // Button 9 on eight joystick
        Joystick8Button9        = 499,
        // Button 10 on eight joystick
        Joystick8Button10   = 500,
        // Button 11 on eight joystick
        Joystick8Button11   = 501,
        // Button 12 on eight joystick
        Joystick8Button12   = 502,
        // Button 13 on eight joystick
        Joystick8Button13   = 503,
        // Button 14 on eight joystick
        Joystick8Button14   = 504,
        // Button 15 on eight joystick
        Joystick8Button15   = 505,
        // Button 16 on eight joystick
        Joystick8Button16   = 506,
        // Button 17 on eight joystick
        Joystick8Button17   = 507,
        // Button 18 on eight joystick
        Joystick8Button18   = 508,
        // Button 19 on eight joystick
        Joystick8Button19   = 509,

        // We could expose all 10 joysticks here, but I think that a user would rarely want to explicitly
        // specify an eight or higher joystick (and they still can using the string version of Input.KeyDown).
        // Eight joysticks, however, are a common setup (especially on consoles), and we've had a bug report
        // for only exposing three, so I added eight.
    }
}
