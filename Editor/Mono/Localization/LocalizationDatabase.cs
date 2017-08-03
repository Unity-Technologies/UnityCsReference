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
            return LocalizationDatabase.GetLocalizedString(str);
        }
    }
}
