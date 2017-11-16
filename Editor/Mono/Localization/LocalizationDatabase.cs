// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Security.Cryptography;

namespace UnityEditor
{
    /// *undocumented*
    internal class L10n
    {
        private L10n() {}

        public static string Tr(string str)
        {
            var new_str = LocalizationDatabase.GetLocalizedString(str);
            return new_str;
        }

        public static string[] Tr(string[] str_list)
        {
            var res = new string[str_list.Length];
            for (var i = 0; i < res.Length; ++i)
                res[i] = Tr(str_list[i]);
            return res;
        }

        public static string TrPath(string path)
        {
            string[] separatingChars = { "/" };
            var result = new System.Text.StringBuilder(256);
            var items = path.Split(separatingChars, System.StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < items.Length; ++i)
            {
                result.Append(Tr(items[i]));
                if (i < items.Length - 1)
                    result.Append("/");
            }
            return result.ToString();
        }
    }
}
