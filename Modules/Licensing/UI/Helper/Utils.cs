// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Licensing.UI.Helper;

static class Utils
{
    public static string GetOxfordCommaString(IList<string> words)
    {
        if (words == null || words.Count == 0)
        {
            return "";
        }

        switch (words.Count)
        {
            case 1:
                return words[0];
            case 2:
                return words[0] + " and " + words[1];
            default:
            {
                var oxfordString = "";
                for (var i = 0; i < words.Count - 1; i++)
                {
                    oxfordString += words[i] + ", ";
                }

                oxfordString += "and " + words[^1];
                return oxfordString;
            }
        }
    }

    public static string GetDescriptionMessageForProducts(IList<string> productNames, string singularPhrase, string pluralPhrase)
    {
        return string.Format(productNames.Count == 1 ? singularPhrase : pluralPhrase, GetOxfordCommaString(productNames));
    }
}
