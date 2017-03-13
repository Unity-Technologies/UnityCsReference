// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class AssetBundleNameGUI
    {
        static private readonly GUIContent kAssetBundleName = new GUIContent("AssetBundle");
        static private readonly int kAssetBundleNameFieldIdHash = "AssetBundleNameFieldHash".GetHashCode();

        static private readonly int kAssetBundleVariantFieldIdHash = "AssetBundleVariantFieldHash".GetHashCode();

        private class Styles
        {
            private static GUISkin s_DarkSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);

            public static GUIStyle label = GetStyle("ControlLabel");
            public static GUIStyle popup = GetStyle("MiniPopup");
            public static GUIStyle textField = GetStyle("textField");

            public static Color cursorColor = s_DarkSkin.settings.cursorColor;

            private static GUIStyle GetStyle(string name)
            {
                return new GUIStyle(s_DarkSkin.GetStyle(name));
            }
        }

        private bool m_ShowAssetBundleNameTextField = false;
        private bool m_ShowAssetBundleVariantTextField = false;

        public void OnAssetBundleNameGUI(IEnumerable<Object> assets)
        {
            EditorGUIUtility.labelWidth = 90f;

            Rect bundleRect  = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight);
            Rect variantRect = bundleRect;

            bundleRect.width *= 0.8f;
            variantRect.xMin += bundleRect.width + EditorGUI.kSpacing;


            int id = GUIUtility.GetControlID(kAssetBundleNameFieldIdHash, FocusType.Passive, bundleRect);

            bundleRect = EditorGUI.PrefixLabel(bundleRect, id, kAssetBundleName, Styles.label);
            if (m_ShowAssetBundleNameTextField)
                AssetBundleTextField(bundleRect, id, assets, false);
            else
                AssetBundlePopup(bundleRect, id, assets, false);

            id = GUIUtility.GetControlID(kAssetBundleVariantFieldIdHash, FocusType.Passive, variantRect);

            if (m_ShowAssetBundleVariantTextField)
                AssetBundleTextField(variantRect, id, assets, true);
            else
                AssetBundlePopup(variantRect, id, assets, true);
        }

        private void ShowNewAssetBundleField(bool isVariant)
        {
            m_ShowAssetBundleNameTextField = !isVariant;
            m_ShowAssetBundleVariantTextField = isVariant;

            EditorGUIUtility.editingTextField = true;
        }

        private void AssetBundleTextField(Rect rect, int id, IEnumerable<Object> assets, bool isVariant)
        {
            // CursorColor is stored at the GUISkin level, but the styles we are using don't necessarily match the GUI.skin.
            // TextField assumes that the style matches the current GUI.skin, this is wrong and should be fixed. We'll workaround for now.
            Color oldCursorColor = GUI.skin.settings.cursorColor;
            GUI.skin.settings.cursorColor = Styles.cursorColor;

            EditorGUI.BeginChangeCheck();
            string temp = EditorGUI.DelayedTextFieldInternal(rect, id, GUIContent.none, "", null, Styles.textField);
            if (EditorGUI.EndChangeCheck())
            {
                SetAssetBundleForAssets(assets, temp, isVariant);
                ShowAssetBundlePopup();
            }

            GUI.skin.settings.cursorColor = oldCursorColor;

            // editing was cancelled
            if (EditorGUI.IsEditingTextField() == false && Event.current.type != EventType.Layout)
                ShowAssetBundlePopup();
        }

        private void ShowAssetBundlePopup()
        {
            m_ShowAssetBundleNameTextField = false;
            m_ShowAssetBundleVariantTextField = false;
        }

        private void AssetBundlePopup(Rect rect, int id, IEnumerable<Object> assets, bool isVariant)
        {
            List<string> displayedOptions = new List<string>();
            displayedOptions.Add("None");
            displayedOptions.Add("");  // seperator

            // Anyway to optimize this by caching GetAssetBundleNameFromAssets() and GetAllAssetBundleNames() when they actually change?
            // As we can change the assetBundle name by script, the UI needs to detect this kind of change.
            bool mixedValue;
            IEnumerable<string> assetBundleFromAssets = GetAssetBundlesFromAssets(assets, isVariant, out mixedValue);

            string[] assetBundles = isVariant ? AssetDatabase.GetAllAssetBundleVariants() : AssetDatabase.GetAllAssetBundleNamesWithoutVariant();
            displayedOptions.AddRange(assetBundles);

            displayedOptions.Add("");  // seperator
            int newAssetBundleIndex = displayedOptions.Count;
            displayedOptions.Add("New...");

            // These two options are invalid for variant, so skip them for variant.
            int removeUnusedIndex = -1;
            int filterSelectedIndex = -1;
            if (!isVariant)
            {
                removeUnusedIndex = displayedOptions.Count;
                displayedOptions.Add("Remove Unused Names");
                filterSelectedIndex = displayedOptions.Count;
                if (assetBundleFromAssets.Count() != 0)
                    displayedOptions.Add("Filter Selected Name" + (mixedValue ? "s" : ""));
            }

            int selectedIndex = 0;
            string firstAssetBundle = assetBundleFromAssets.FirstOrDefault();
            if (!String.IsNullOrEmpty(firstAssetBundle))
                selectedIndex = displayedOptions.IndexOf(firstAssetBundle);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixedValue;
            selectedIndex = EditorGUI.DoPopup(rect, id, selectedIndex, EditorGUIUtility.TempContent(displayedOptions.ToArray()), Styles.popup);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedIndex == 0) // None
                    SetAssetBundleForAssets(assets, null, isVariant);
                else if (selectedIndex == newAssetBundleIndex) // New...
                    ShowNewAssetBundleField(isVariant);
                else if (selectedIndex == removeUnusedIndex) // Remove Unused Names
                    AssetDatabase.RemoveUnusedAssetBundleNames();
                else if (selectedIndex == filterSelectedIndex) // Filter Selected Name(s)
                    FilterSelected(assetBundleFromAssets);
                else
                    SetAssetBundleForAssets(assets, displayedOptions[selectedIndex], isVariant);
            }
        }

        private void FilterSelected(IEnumerable<string> assetBundleNames)
        {
            var searchFilter = new SearchFilter();
            searchFilter.assetBundleNames = assetBundleNames.Where(name => !String.IsNullOrEmpty(name)).ToArray();

            if (ProjectBrowser.s_LastInteractedProjectBrowser != null)
                ProjectBrowser.s_LastInteractedProjectBrowser.SetSearch(searchFilter);
            else
                Debug.LogWarning("No Project Browser found to apply AssetBundle filter.");
        }

        private IEnumerable<string> GetAssetBundlesFromAssets(IEnumerable<Object> assets, bool isVariant, out bool isMixed)
        {
            var assetBundles = new HashSet<string>();
            string lastAssetBundle = null;
            isMixed = false;

            foreach (Object obj in assets)
            {
                if (obj is MonoScript)
                    continue;

                AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj));
                if (importer == null)
                    continue;

                string currentAssetBundle = isVariant ? importer.assetBundleVariant : importer.assetBundleName;

                if (lastAssetBundle != null && lastAssetBundle != currentAssetBundle)
                    isMixed = true;
                lastAssetBundle = currentAssetBundle;

                if (!String.IsNullOrEmpty(currentAssetBundle))
                    assetBundles.Add(currentAssetBundle);
            }

            return assetBundles;
        }

        private void SetAssetBundleForAssets(IEnumerable<Object> assets, string name, bool isVariant)
        {
            bool assetBundleNameChanged = false;
            foreach (Object obj in assets)
            {
                if (obj is MonoScript)
                    continue;

                AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(obj));
                if (importer == null)
                    continue;

                if (isVariant)
                    importer.assetBundleVariant = name;
                else
                    importer.assetBundleName = name;

                assetBundleNameChanged = true;
            }

            if (assetBundleNameChanged)
            {
                EditorApplication.Internal_CallAssetBundleNameChanged();
            }
        }
    }
}
