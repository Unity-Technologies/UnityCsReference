// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    static class SavePromptUtility
    {
        internal const int nameCharacterLimit = 64;

        static readonly string k_SaveNameTooLong = L10n.Tr("{0} Name exceeds maximum length ("+nameCharacterLimit+")");
        static readonly string k_SaveNameEmpty = L10n.Tr("{0} Name is empty");
        static readonly string k_SaveNameWithUnsupportedCharacters = L10n.Tr("{0} Name has unsupported characters");

        static bool ValidateCharacters(string name)
        {
            return name.IndexOfAny(EditorUtility.GetInvalidFilenameChars().ToCharArray()) == -1 &&
                   name.IndexOfAny("_%#^".ToCharArray()) == -1 &&
                   name.Trim(" ".ToCharArray()).Length == name.Length;
        }

        public static string GetSaveError(string saveType, string saveName, Func<string, string> additionalValidation = null)
        {
            if (string.IsNullOrWhiteSpace(saveName))
                return string.Format(k_SaveNameEmpty, saveType);

            if (saveName.Length > nameCharacterLimit)
                return string.Format(k_SaveNameTooLong, saveType);

            if (!ValidateCharacters(saveName))
                return string.Format(k_SaveNameWithUnsupportedCharacters, saveType);

            return additionalValidation != null ? additionalValidation.Invoke(saveName) : null;
        }
    }
}
