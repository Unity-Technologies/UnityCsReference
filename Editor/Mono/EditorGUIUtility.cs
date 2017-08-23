// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public sealed partial class EditorGUIUtility
    {
        public class IconSizeScope : GUI.Scope
        {
            Vector2 m_OriginalIconSize;

            public IconSizeScope(Vector2 iconSizeWithinScope)
            {
                m_OriginalIconSize = GetIconSize();
                SetIconSize(iconSizeWithinScope);
            }

            protected override void CloseScope()
            {
                SetIconSize(m_OriginalIconSize);
            }
        }

        internal static void RepaintCurrentWindow()
        {
            CheckOnGUI();
            GUIView.current.Repaint();
        }

        internal static bool HasCurrentWindowKeyFocus()
        {
            CheckOnGUI();
            return GUIView.current.hasFocus;
        }

        internal static Material s_GUITextureBlitColorspaceMaterial;
        internal static Material GUITextureBlitColorspaceMaterial
        {
            get
            {
                if (!s_GUITextureBlitColorspaceMaterial)
                {
                    Shader shader = EditorGUIUtility.LoadRequired("SceneView/GUITextureBlitColorspace.shader") as Shader;
                    s_GUITextureBlitColorspaceMaterial = new Material(shader);
                    SetGUITextureBlitColorspaceSettings(s_GUITextureBlitColorspaceMaterial);
                }
                return s_GUITextureBlitColorspaceMaterial;
            }
        }

        internal static void SetGUITextureBlitColorspaceSettings(Material mat)
        {
            bool needsGammaConversion = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal;

            if (needsGammaConversion && QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                mat.SetFloat("_ConvertToGamma", 1.0f);
            }
            else
            {
                mat.SetFloat("_ConvertToGamma", 0.0f);
            }
        }

        /// The current UI scaling factor for high-DPI displays. For instance, 2.0 on a retina display

        public
        static new float pixelsPerPoint
        {
            get
            {
                return GUIUtility.pixelsPerPoint;
            }
        }

        public
        static Rect PointsToPixels(Rect rect)
        {
            var cachedPixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            rect.x *= cachedPixelsPerPoint;
            rect.y *= cachedPixelsPerPoint;
            rect.width *= cachedPixelsPerPoint;
            rect.height *= cachedPixelsPerPoint;
            return rect;
        }

        public
        static Rect PixelsToPoints(Rect rect)
        {
            var cachedInvPixelsPerPoint = 1f / EditorGUIUtility.pixelsPerPoint;
            rect.x *= cachedInvPixelsPerPoint;
            rect.y *= cachedInvPixelsPerPoint;
            rect.width *= cachedInvPixelsPerPoint;
            rect.height *= cachedInvPixelsPerPoint;
            return rect;
        }

        public
        static Vector2 PointsToPixels(Vector2 position)
        {
            var cachedPixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            position.x *= cachedPixelsPerPoint;
            position.y *= cachedPixelsPerPoint;
            return position;
        }

        public
        static Vector2 PixelsToPoints(Vector2 position)
        {
            var cachedInvPixelsPerPoint = 1f / EditorGUIUtility.pixelsPerPoint;
            position.x *= cachedInvPixelsPerPoint;
            position.y *= cachedInvPixelsPerPoint;
            return position;
        }

        // Given a rectangle, GUI style and a list of items, lay them out sequentially;
        // left to right, top to bottom.
        public static List<Rect> GetFlowLayoutedRects(Rect rect, GUIStyle style, float horizontalSpacing, float verticalSpacing, List<string> items)
        {
            var result = new List<Rect>(items.Count);
            var curPos = rect.position;
            for (var i = 0; i < items.Count; ++i)
            {
                var gc = EditorGUIUtility.TempContent(items[i]);
                var itemSize = style.CalcSize(gc);
                var itemRect = new Rect(curPos, itemSize);

                // Reached right side, go to next row
                if (curPos.x + itemSize.x + horizontalSpacing >= rect.xMax)
                {
                    curPos.x = rect.x;
                    curPos.y += itemSize.y + verticalSpacing;
                    itemRect.position = curPos;
                }
                result.Add(itemRect);

                // Move next item to the left
                curPos.x += itemSize.x + horizontalSpacing;
            }

            return result;
        }

        internal class SkinnedColor
        {
            Color normalColor;
            Color proColor;

            public SkinnedColor(Color color, Color proColor)
            {
                normalColor = color;
                this.proColor = proColor;
            }

            public SkinnedColor(Color color)
            {
                normalColor = color;
                proColor = color;
            }

            public Color color
            {
                get
                {
                    if (EditorGUIUtility.isProSkin)
                        return proColor;
                    else
                        return normalColor;
                }

                set
                {
                    if (EditorGUIUtility.isProSkin)
                        proColor = value;
                    else
                        normalColor = value;
                }
            }

            public static implicit operator Color(SkinnedColor colorSkin)
            {
                return colorSkin.color;
            }
        }

        internal static void ShowObjectPicker<T>(Object obj, bool allowSceneObjects, string searchFilter, ObjectSelectorReceiver objectSelectorReceiver) where T : Object
        {
            System.Type objType = typeof(T);
            ObjectSelector.get.Show(obj, objType, null, allowSceneObjects);
            ObjectSelector.get.objectSelectorReceiver = objectSelectorReceiver;
            ObjectSelector.get.searchFilter = searchFilter;
        }

        private delegate bool HeaderItemDelegate(Rect rectangle, Object[] targets);
        private static List<HeaderItemDelegate> s_EditorHeaderItemsMethods = null;
        internal static Rect DrawEditorHeaderItems(Rect rectangle, Object[] targetObjs)
        {
            if (targetObjs.Length == 0 || (targetObjs.Length == 1 && targetObjs[0].GetType() == typeof(System.Object)))
                return rectangle;

            if (s_EditorHeaderItemsMethods == null)
            {
                List<Type> targetObjTypes = new List<Type>();
                var type = targetObjs[0].GetType();
                while (type.BaseType != null)
                {
                    targetObjTypes.Add(type);
                    type = type.BaseType;
                }

                AttributeHelper.MethodInfoSorter methods = AttributeHelper.GetMethodsWithAttribute<EditorHeaderItemAttribute>(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
                Func<EditorHeaderItemAttribute, bool> filter = (a) => targetObjTypes.Any(c => a.TargetType == c);
                var methodInfos = methods.FilterAndSortOnAttribute(filter, (a) => a.callbackOrder);
                s_EditorHeaderItemsMethods = new List<HeaderItemDelegate>();
                foreach (MethodInfo methodInfo in methodInfos)
                {
                    s_EditorHeaderItemsMethods.Add((HeaderItemDelegate)Delegate.CreateDelegate(typeof(HeaderItemDelegate), methodInfo));
                }
            }

            for (var index = 0; index < s_EditorHeaderItemsMethods.Count; index++)
            {
                HeaderItemDelegate dele = s_EditorHeaderItemsMethods[index];
                if (dele(rectangle, targetObjs))
                {
                    rectangle.x -= rectangle.width;
                }
            }

            return rectangle;
        }
    }
}
