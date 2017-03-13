// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using ExCSS;
using UnityEngine;
using UnityEditor;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;
using ParserStyleSheet = ExCSS.StyleSheet;
using ParserStyleRule = ExCSS.StyleRule;
using UnityStyleSheet = UnityEngine.ScriptableObject;
namespace UnityEditor.StyleSheets
{
    class StyleSheetImporter
    {
        [RequiredByNativeCode]
        public static void ImportStyleSheet(UnityStyleSheet asset, string contents)
        {
        }

    }
}
