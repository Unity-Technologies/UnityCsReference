// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A utility class for overriding the translation method on strings.
    /// </summary>
    internal static class L10nUtility
    {
        /// <summary>
        /// By default, it returns the input string unchanged.
        /// </summary>
        static Func<string, string> TranslateString = TranslateFunc;

        /// <summary>
        /// Overwrites the string translation method.
        /// </summary>
        /// <param name="translateFunc">A function that accepts a string and returns a string.</param>
        /// <remarks>
        /// If null is passed, it defaults to returning the input string.
        /// </remarks>
        internal static void SetTranslateFunc(Func<string, string> translateFunc)
        {
            if (translateFunc == null)
            {
                TranslateString = TranslateFunc;
                return;
            }

            TranslateString = translateFunc;
        }

        /// <summary>
        /// The default translate function.
        /// </summary>
        /// <param name="str">The string to be returned.</param>
        /// <returns>The same value as nothing is applied.</returns>
        internal static string TranslateFunc(string str) => str;

        /// <summary>
        /// Translates the given string.
        /// </summary>
        /// <param name="str">The value to be translated.</param>
        /// <returns>The translated string if a Translation method is provided. Otherwise, it returns the same string.</returns>
        public static string GetTranslation(string str) => TranslateString?.Invoke(str) ?? str;
    }
}
