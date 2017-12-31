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
using UnityEngine.Events;
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

        internal static Material s_GUITextureBlit2SRGBMaterial;
        internal static Material GUITextureBlit2SRGBMaterial
        {
            get
            {
                if (!s_GUITextureBlit2SRGBMaterial)
                {
                    Shader shader = EditorGUIUtility.LoadRequired("SceneView/GUITextureBlit2SRGB.shader") as Shader;
                    s_GUITextureBlit2SRGBMaterial = new Material(shader);
                    s_GUITextureBlit2SRGBMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                s_GUITextureBlit2SRGBMaterial.SetFloat("_ManualTex2SRGB", QualitySettings.activeColorSpace == ColorSpace.Linear ? 1.0f : 0.0f);
                return s_GUITextureBlit2SRGBMaterial;
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

                AttributeHelper.MethodInfoSorter methods = AttributeHelper.GetMethodsWithAttribute<EditorHeaderItemAttribute>(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
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

        /// <summary>
        /// Use this container and helper class when implementing lock behaviour on a window when also using an <see cref="ActiveEditorTracker"/>.
        /// </summary>
        [Serializable]
        internal class EditorLockTrackerWithActiveEditorTracker : EditorLockTracker
        {
            internal override bool isLocked
            {
                get
                {
                    if (m_Tracker != null)
                    {
                        base.isLocked = m_Tracker.isLocked;
                        return m_Tracker.isLocked;
                    }
                    return base.isLocked;
                }
                set
                {
                    if (m_Tracker != null)
                    {
                        m_Tracker.isLocked = value;
                    }
                    base.isLocked = value;
                }
            }

            [SerializeField, HideInInspector]
            ActiveEditorTracker m_Tracker;

            internal ActiveEditorTracker tracker
            {
                get { return m_Tracker; }
                set
                {
                    m_Tracker = value;
                    if (m_Tracker != null)
                    {
                        isLocked = m_Tracker.isLocked;
                    }
                }
            }
        }

        /// <summary>
        /// Use this container and helper class when implementing lock behaviour on a window.
        /// </summary>
        [Serializable]
        internal class EditorLockTracker
        {
            [Serializable] public class LockStateEvent : UnityEvent<bool> {}
            [HideInInspector]
            internal LockStateEvent lockStateChanged = new LockStateEvent();

            const string k_LockMenuText = "Lock";
            static readonly GUIContent k_LockMenuGUIContent =  EditorGUIUtility.TextContent(k_LockMenuText);

            /// <summary>
            /// don't set or get this directly unless from within the <see cref="isLocked"/> property,
            /// as that property also keeps track of the potentially existing tracker in <see cref="EditorLockTrackerWithActiveEditorTracker"/>
            /// </summary>
            [SerializeField, HideInInspector]
            bool m_IsLocked;

            internal virtual bool isLocked
            {
                get
                {
                    return m_IsLocked;
                }
                set
                {
                    bool wasLocked = m_IsLocked;
                    m_IsLocked = value;

                    if (wasLocked != m_IsLocked)
                    {
                        lockStateChanged.Invoke(m_IsLocked);
                    }
                }
            }

            internal virtual void AddItemsToMenu(GenericMenu menu, bool disabled = false)
            {
                if (disabled)
                {
                    menu.AddDisabledItem(k_LockMenuGUIContent);
                }
                else
                {
                    menu.AddItem(k_LockMenuGUIContent, isLocked, FlipLocked);
                }
            }

            internal void ShowButton(Rect position, GUIStyle lockButtonStyle, bool disabled = false)
            {
                using (new EditorGUI.DisabledScope(disabled))
                {
                    EditorGUI.BeginChangeCheck();
                    bool newLock = GUI.Toggle(position, isLocked, GUIContent.none, lockButtonStyle);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (newLock != isLocked)
                            FlipLocked();
                    }
                }
            }

            void FlipLocked()
            {
                isLocked = !isLocked;
            }
        }
    }
}
