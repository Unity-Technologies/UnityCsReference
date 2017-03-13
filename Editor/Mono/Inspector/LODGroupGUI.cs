// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    internal static class LODGroupGUI
    {
        // Default colors for each LOD group....
        public static readonly Color[] kLODColors =
        {
            new Color(0.4831376f, 0.6211768f, 0.0219608f, 1.0f),
            new Color(0.2792160f, 0.4078432f, 0.5835296f, 1.0f),
            new Color(0.2070592f, 0.5333336f, 0.6556864f, 1.0f),
            new Color(0.5333336f, 0.1600000f, 0.0282352f, 1.0f),
            new Color(0.3827448f, 0.2886272f, 0.5239216f, 1.0f),
            new Color(0.8000000f, 0.4423528f, 0.0000000f, 1.0f),
            new Color(0.4486272f, 0.4078432f, 0.0501960f, 1.0f),
            new Color(0.7749016f, 0.6368624f, 0.0250984f, 1.0f)
        };

        public static readonly Color kCulledLODColor = new Color(.4f, 0f, 0f, 1f);

        public const int kSceneLabelHalfWidth = 100;
        public const int kSceneLabelHeight = 45;
        public const int kSceneHeaderOffset = 40;

        public const int kSliderBarTopMargin = 18;
        public const int kSliderBarHeight = 30;
        public const int kSliderBarBottomMargin = 16;

        public const int kRenderersButtonHeight = 60;
        public const int kButtonPadding = 2;
        public const int kDeleteButtonSize = 20;

        public const int kSelectedLODRangePadding = 3;

        public const int kRenderAreaForegroundPadding = 3;

        public class GUIStyles
        {
            public readonly GUIStyle m_LODSliderBG = "LODSliderBG";
            public readonly GUIStyle m_LODSliderRange = "LODSliderRange";
            public readonly GUIStyle m_LODSliderRangeSelected = "LODSliderRangeSelected";
            public readonly GUIStyle m_LODSliderText = "LODSliderText";
            public readonly GUIStyle m_LODSliderTextSelected = "LODSliderTextSelected";
            public readonly GUIStyle m_LODStandardButton = "Button";
            public readonly GUIStyle m_LODRendererButton = "LODRendererButton";
            public readonly GUIStyle m_LODRendererAddButton = "LODRendererAddButton";
            public readonly GUIStyle m_LODRendererRemove = "LODRendererRemove";
            public readonly GUIStyle m_LODBlackBox = "LODBlackBox";
            public readonly GUIStyle m_LODCameraLine = "LODCameraLine";

            public readonly GUIStyle m_LODSceneText = "LODSceneText";
            public readonly GUIStyle m_LODRenderersText = "LODRenderersText";
            public readonly GUIStyle m_LODLevelNotifyText = "LODLevelNotifyText";

            public readonly GUIContent m_IconRendererPlus                   = EditorGUIUtility.IconContent("Toolbar Plus", "|Add New Renderers");
            public readonly GUIContent m_IconRendererMinus                  = EditorGUIUtility.IconContent("Toolbar Minus", "|Remove Renderer");
            public readonly GUIContent m_CameraIcon                         = EditorGUIUtility.IconContent("Camera Icon");

            public readonly GUIContent m_UploadToImporter                   = EditorGUIUtility.TextContent("Upload to Importer|Upload the modified screen percentages to the model importer.");
            public readonly GUIContent m_UploadToImporterDisabled           = EditorGUIUtility.TextContent("Upload to Importer|Number of LOD's in the scene instance differ from the number of LOD's in the imported model.");
            public readonly GUIContent m_RecalculateBounds                  = EditorGUIUtility.TextContent("Recalculate Bounds|Recalculate bounds to encapsulate all child renderers.");
            public readonly GUIContent m_RecalculateBoundsDisabled          = EditorGUIUtility.TextContent("Recalculate Bounds|Bounds are already up-to-date.");
            public readonly GUIContent m_LightmapScale                      = EditorGUIUtility.TextContent("Recalculate Lightmap Scale|Set the lightmap scale to match the LOD percentages.");
            public readonly GUIContent m_RendersTitle                       = EditorGUIUtility.TextContent("Renderers:");

            public readonly GUIContent m_AnimatedCrossFadeInvalidText       = EditorGUIUtility.TextContent("Animated cross-fading is currently disabled. Please enable \"Animate Between Next LOD\" on either the current or the previous LOD.");
            public readonly GUIContent m_AnimatedCrossFadeInconsistentText  = EditorGUIUtility.TextContent("Animated cross-fading is currently disabled. \"Animate Between Next LOD\" is enabled but the next LOD is not in Animated Cross Fade mode.");
            public readonly GUIContent m_AnimateBetweenPreviousLOD          = EditorGUIUtility.TextContent("Animate Between Previous LOD|Cross-fade animation plays when transits between this LOD and the previous (lower) LOD.");
        }

        private static GUIStyles s_Styles;

        public static GUIStyles Styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new GUIStyles();
                return s_Styles;
            }
        }

        public static float DelinearizeScreenPercentage(float percentage)
        {
            if (Mathf.Approximately(0.0f, percentage))
                return 0.0f;

            return Mathf.Sqrt(percentage);
        }

        public static float LinearizeScreenPercentage(float percentage)
        {
            return percentage * percentage;
        }

        public static Rect CalcLODButton(Rect totalRect, float percentage)
        {
            return new Rect(totalRect.x + (Mathf.Round(totalRect.width * (1.0f - percentage))) - 5, totalRect.y, 10, totalRect.height);
        }

        public static Rect GetCulledBox(Rect totalRect, float previousLODPercentage)
        {
            var r = CalcLODRange(totalRect, previousLODPercentage, 0.0f);
            r.height -= 2;
            r.width -= 1;
            r.center += new Vector2(0f, 1.0f);
            return r;
        }

        public class LODInfo
        {
            public Rect m_ButtonPosition;
            public Rect m_RangePosition;

            public LODInfo(int lodLevel, string name, float screenPercentage)
            {
                LODLevel = lodLevel;
                LODName = name;
                RawScreenPercent = screenPercentage;
            }

            public int LODLevel { get; private set; }
            public string LODName { get; private set; }
            public float RawScreenPercent { get; set; }

            public float ScreenPercent
            {
                get { return DelinearizeScreenPercentage(RawScreenPercent); }
                set { RawScreenPercent = LinearizeScreenPercentage(value); }
            }
        }

        public static List<LODInfo> CreateLODInfos(int numLODs, Rect area, Func<int, string> nameGen, Func<int, float> heightGen)
        {
            var lods = new List<LODInfo>();

            for (int i = 0; i < numLODs; ++i)
            {
                var lodInfo = new LODInfo(i, nameGen(i), heightGen(i));
                lodInfo.m_ButtonPosition = CalcLODButton(area, lodInfo.ScreenPercent);
                var previousPercentage = i == 0 ? 1.0f : lods[i - 1].ScreenPercent;
                lodInfo.m_RangePosition = CalcLODRange(area, previousPercentage, lodInfo.ScreenPercent);
                lods.Add(lodInfo);
            }

            return lods;
        }

        public static float GetCameraPercent(Vector2 position, Rect sliderRect)
        {
            var percentage = Mathf.Clamp(1.0f - (position.x - sliderRect.x) / sliderRect.width, 0.01f, 1.0f);
            percentage = LODGroupGUI.LinearizeScreenPercentage(percentage);
            return percentage;
        }

        public static void SetSelectedLODLevelPercentage(float newScreenPercentage, int lod, List<LODInfo> lods)
        {
            // Find the lower detail lod... clamp value to stop overlapping slider
            var minimum = 0.0f;
            var lowerLOD = lods.FirstOrDefault(x => x.LODLevel == lods[lod].LODLevel + 1);
            if (lowerLOD != null)
                minimum = lowerLOD.RawScreenPercent;

            // Find the higher detail lod... clamp value to stop overlapping slider
            var maximum = 1.0f;
            var higherLOD = lods.FirstOrDefault(x => x.LODLevel == lods[lod].LODLevel - 1);
            if (higherLOD != null)
                maximum = higherLOD.RawScreenPercent;

            maximum = Mathf.Clamp01(maximum);
            minimum = Mathf.Clamp01(minimum);

            // Set that value
            lods[lod].RawScreenPercent = Mathf.Clamp(newScreenPercentage, minimum, maximum);
        }

        public static void DrawLODSlider(Rect area, IList<LODInfo> lods, int selectedLevel)
        {
            Styles.m_LODSliderBG.Draw(area, GUIContent.none, false, false, false, false);
            for (int i = 0; i < lods.Count; i++)
            {
                var lod = lods[i];
                DrawLODRange(lod, i == 0 ? 1.0f : lods[i - 1].RawScreenPercent, i == selectedLevel);
                DrawLODButton(lod);
            }

            // Draw the last range (culled)
            DrawCulledRange(area, lods.Count > 0 ? lods[lods.Count - 1].RawScreenPercent : 1.0f);
        }

        public static void DrawMixedValueLODSlider(Rect area)
        {
            Styles.m_LODSliderBG.Draw(area, GUIContent.none, false, false, false, false);
            var r = GetCulledBox(area, 1.0f);
            // Draw the range of a lod level on the slider
            var tempColor = GUI.color;
            GUI.color = kLODColors[1] * 0.6f; // more greyish
            Styles.m_LODSliderRange.Draw(r, GUIContent.none, false, false, false, false);
            GUI.color = tempColor;
            var centeredStyle = new GUIStyle(EditorStyles.whiteLargeLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(area, "---", centeredStyle);
        }

        private static Rect CalcLODRange(Rect totalRect, float startPercent, float endPercent)
        {
            var startX = Mathf.Round(totalRect.width * (1.0f - startPercent));
            var endX = Mathf.Round(totalRect.width * (1.0f - endPercent));

            return new Rect(totalRect.x + startX, totalRect.y, endX - startX, totalRect.height);
        }

        private static void DrawLODButton(LODInfo currentLOD)
        {
            // Make the lod button areas a horizonal resizer
            EditorGUIUtility.AddCursorRect(currentLOD.m_ButtonPosition, MouseCursor.ResizeHorizontal);
        }

        private static void DrawLODRange(LODInfo currentLOD, float previousLODPercentage, bool isSelected)
        {
            var tempColor = GUI.backgroundColor;
            var startPercentageString = string.Format("{0}\n{1:0}%", currentLOD.LODName, previousLODPercentage * 100);
            if (isSelected)
            {
                var foreground = currentLOD.m_RangePosition;
                foreground.width -= kSelectedLODRangePadding * 2;
                foreground.height -= kSelectedLODRangePadding * 2;
                foreground.center += new Vector2(kSelectedLODRangePadding, kSelectedLODRangePadding);
                Styles.m_LODSliderRangeSelected.Draw(currentLOD.m_RangePosition, GUIContent.none, false, false, false, false);
                GUI.backgroundColor = kLODColors[currentLOD.LODLevel];
                if (foreground.width > 0)
                    Styles.m_LODSliderRange.Draw(foreground, GUIContent.none, false, false, false, false);
                Styles.m_LODSliderText.Draw(currentLOD.m_RangePosition, startPercentageString, false, false, false, false);
            }
            else
            {
                GUI.backgroundColor = kLODColors[currentLOD.LODLevel];
                GUI.backgroundColor *= 0.6f;
                Styles.m_LODSliderRange.Draw(currentLOD.m_RangePosition, GUIContent.none, false, false, false, false);
                Styles.m_LODSliderText.Draw(currentLOD.m_RangePosition, startPercentageString, false, false, false, false);
            }
            GUI.backgroundColor = tempColor;
        }

        private static void DrawCulledRange(Rect totalRect, float previousLODPercentage)
        {
            if (Mathf.Approximately(previousLODPercentage, 0.0f)) return;

            var r = GetCulledBox(totalRect, DelinearizeScreenPercentage(previousLODPercentage));
            // Draw the range of a lod level on the slider
            var tempColor = GUI.color;
            GUI.color = kCulledLODColor;
            Styles.m_LODSliderRange.Draw(r, GUIContent.none, false, false, false, false);
            GUI.color = tempColor;

            // Draw some details for the current marker
            var startPercentageString = string.Format("Culled\n{0:0}%", previousLODPercentage * 100);
            Styles.m_LODSliderText.Draw(r, startPercentageString, false, false, false, false);
        }
    }
}
