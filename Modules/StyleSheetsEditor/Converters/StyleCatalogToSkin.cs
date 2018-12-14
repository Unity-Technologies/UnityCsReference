// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.StyleSheets
{
    internal static class StyleCatalogToSkin
    {
        internal static GUISkin CreateGUISkinFromSheetsFolder(SkinTarget target, IEnumerable<string> folders, GUISkin skin = null)
        {
            skin = skin ?? ConverterUtils.CreateDefaultGUISkin();
            PopulateSkin(target, folders, skin);
            return skin;
        }

        internal static void PopulateSkin(StyleSheetResolver resolver, GUISkin skin)
        {
            var catalog = new StyleCatalog();
            catalog.Load(resolver);

            // Global values:
            var globalBlock = catalog.GetStyle("*");
            if (globalBlock.IsValid())
            {
                skin.font = globalBlock.GetResource(ConverterUtils.k_Font, skin.font);
            }

            // builtin (type selector) GUIStyle
            skin.ForEachGUIStyleProperty((name, style) =>
            {
                PopulateStyle(catalog, name.Capitalize(), style);
            });

            // CustomStyles
            var customStyleBlockNames = resolver.Rules.Select(r => r.Key)
                .Where(ConverterUtils.IsCustomStyleSelector)
                .Select(GUIStyleExtensions.RuleNameToBlockName)
                .ToArray();

            var blockNameToStyleDict = new Dictionary<string, GUIStyle>();
            if (skin.customStyles != null)
            {
                // Add Existing style: ready to be overridden:
                foreach (var customStyle in skin.customStyles)
                {
                    // GUISkin by default adds a null Style
                    if (customStyle != null)
                    {
                        var blockName = GUIStyleExtensions.StyleNameToBlockName(customStyle.name);
                        blockNameToStyleDict.TryAdd(blockName, customStyle);
                    }
                }
            }

            foreach (var customStyleBlockName in customStyleBlockNames)
            {
                GUIStyle customStyle;

                // Check if we are overriding an existing style or if we are creating a new custom style:
                if (!blockNameToStyleDict.TryGetValue(customStyleBlockName, out customStyle))
                {
                    // New style being added:
                    customStyle = new GUIStyle();
                    blockNameToStyleDict.Add(customStyleBlockName, customStyle);
                }

                PopulateStyle(catalog, customStyleBlockName, customStyle);
            }

            skin.customStyles = blockNameToStyleDict.Values.ToArray();
            Array.Sort(skin.customStyles, (s1, s2) => s1.name.CompareTo(s2.name));

            // GUISettings
            var settingsBlock = catalog.GetStyle("imgui-skin-settings");
            if (settingsBlock.IsValid())
            {
                skin.settings.selectionColor = settingsBlock.GetColor(ConverterUtils.k_SelectionColor, skin.settings.selectionColor);
                skin.settings.cursorColor = settingsBlock.GetColor(ConverterUtils.k_CursorColor, skin.settings.cursorColor);
                skin.settings.cursorFlashSpeed = settingsBlock.GetFloat(ConverterUtils.k_CursorFlashSpeed, skin.settings.cursorFlashSpeed);
                skin.settings.doubleClickSelectsWord = settingsBlock.GetBool(ConverterUtils.k_DoubleClickSelectsWord, skin.settings.doubleClickSelectsWord);
                skin.settings.tripleClickSelectsLine = settingsBlock.GetBool(ConverterUtils.k_TripleClickSelectsLine, skin.settings.tripleClickSelectsLine);
            }
        }

        internal static void PopulateSkin(SkinTarget target, IEnumerable<string> folders, GUISkin skin)
        {
            var resolver = ConverterUtils.ResolveFromSheetsFolder(folders, target);
            resolver.Resolve();
            PopulateSkin(resolver, skin);
        }

        internal static void PopulateStyle(StyleCatalog catalog, string styleBlockName, GUIStyle style)
        {
            GUIStyleExtensions.PopulateStyle(catalog, style, styleBlockName, false);
        }
    }
}
