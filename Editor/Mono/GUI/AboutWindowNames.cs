// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;

namespace UnityEditor
{
    static class AboutWindowNames
    {
        const int kChunkSize = 100;

        private static string s_Country = null;
        private static string[] s_CachedNames = {};

        // private static Dictionary<string,string> s_flags = new Dictionary<string,string>() {
        //     { "DK", "��" },
        //     { "LT", "��" },
        //     { "PT", "��" },
        //     { "US", "��" }
        // };

        public class CreditEntry
        {
#pragma warning disable 649
            public string name;
            // Store diacritic free name for easing search
            public string normalizedName;
            public string country_code;
            public string office;
            public string region;
            public string twitter;
            public string nationality;
            public string gravatar_hash;
            public bool alumni;

            public string FormattedName
            {
                get
                {
                    var formatted = name;

                    // Emoji flags don't work yet
                    // if (!String.IsNullOrEmpty(nationality) && s_flags.ContainsKey(nationality))
                    //     formatted += " " + s_flags[nationality];

                    if (!String.IsNullOrEmpty(twitter))
                        formatted += " ( @" + twitter + " )";
                    return formatted;
                }
            }
        }

        public static List<CreditEntry> s_Credits = new List<CreditEntry>();

        private static string CreditsFilePath
        {
            get
            {
                return Path.Combine(
                    EditorApplication.applicationContentsPath,
                    "Resources/credits.csv"
                    );
            }
        }

        public static string RemoveDiacritics(string text)
        {
            string formD = text.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char ch in formD)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        static void ParseCredits()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("credits.csv"))
            {
                using (var reader = new StreamReader(stream))
                {
                    string line;
                    do
                    {
                        line = reader.ReadLine();
                        if (line != null)
                        {
                            if (line.Length > 0)
                            {
                                string[] entry = line.Split(',');
                                CreditEntry c = new CreditEntry() {
                                    name = entry[0],
                                    normalizedName = RemoveDiacritics(entry[0]),
                                    alumni = (entry[1] == "1"),
                                    country_code = entry[2],
                                    region = (entry[3]),
                                    twitter = entry[4],
                                };
                                s_Credits.Add(c);
                            }
                        }
                    }
                    while (line != null);
                }
            }
        }

        public static void ParseCreditsIfNecessary()
        {
            if (s_Credits.Count == 0)
                ParseCredits();
        }

        // TODO: Proper choice is pagination, and page size choice
        public static string[] Names(string country_filter = null, bool chunked = false)
        {
            if ((s_Country == country_filter)
                && (s_CachedNames.Length > 0))
            {
                // TODO: Honor chunking
                return s_CachedNames;
            }
            s_Country = country_filter;
            List<string> newNames = new List<string>();

            foreach (var entry in s_Credits)
            {
                if (String.IsNullOrEmpty(country_filter) || (entry.country_code == country_filter))
                {
                    newNames.Add(entry.FormattedName);
                }
            }
            if (!chunked)
            {
                s_CachedNames = newNames.ToArray();
            }
            else
            {
                string[] nameChunks = new string[newNames.Count / kChunkSize + 1];

                for (int i = 0; i * kChunkSize < newNames.Count; i++)
                    nameChunks[i] = string.Join(", ", newNames.Skip(i * kChunkSize).Take(kChunkSize).ToArray());
                s_CachedNames = nameChunks.ToArray();
            }
            return s_CachedNames;
        }
    }
}
