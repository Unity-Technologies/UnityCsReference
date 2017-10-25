// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public partial class GUILayout
    {
        public class HorizontalScope : GUI.Scope
        {
            public HorizontalScope(params GUILayoutOption[] options)
            {
                BeginHorizontal(options);
            }

            public HorizontalScope(GUIStyle style, params GUILayoutOption[] options)
            {
                BeginHorizontal(style, options);
            }

            public HorizontalScope(string text, GUIStyle style, params GUILayoutOption[] options)
            {
                BeginHorizontal(text, style, options);
            }

            public HorizontalScope(Texture image, GUIStyle style, params GUILayoutOption[] options)
            {
                BeginHorizontal(image, style, options);
            }

            public HorizontalScope(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
            {
                BeginHorizontal(content, style, options);
            }

            protected override void CloseScope()
            {
                EndHorizontal();
            }
        }

        public class VerticalScope : GUI.Scope
        {
            public VerticalScope(params GUILayoutOption[] options)
            {
                BeginVertical(options);
            }

            public VerticalScope(GUIStyle style, params GUILayoutOption[] options)
            {
                BeginVertical(style, options);
            }

            public VerticalScope(string text, GUIStyle style, params GUILayoutOption[] options)
            {
                BeginVertical(text, style, options);
            }

            public VerticalScope(Texture image, GUIStyle style, params GUILayoutOption[] options)
            {
                BeginVertical(image, style, options);
            }

            public VerticalScope(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
            {
                BeginVertical(content, style, options);
            }

            protected override void CloseScope()
            {
                EndVertical();
            }
        }

        public class AreaScope : GUI.Scope
        {
            public AreaScope(Rect screenRect)
            {
                BeginArea(screenRect);
            }

            public AreaScope(Rect screenRect, string text)
            {
                BeginArea(screenRect, text);
            }

            public AreaScope(Rect screenRect, Texture image)
            {
                BeginArea(screenRect, image);
            }

            public AreaScope(Rect screenRect, GUIContent content)
            {
                BeginArea(screenRect, content);
            }

            public AreaScope(Rect screenRect, string text, GUIStyle style)
            {
                BeginArea(screenRect, text, style);
            }

            public AreaScope(Rect screenRect, Texture image, GUIStyle style)
            {
                BeginArea(screenRect, image, style);
            }

            public AreaScope(Rect screenRect, GUIContent content, GUIStyle style)
            {
                BeginArea(screenRect, content, style);
            }

            protected override void CloseScope()
            {
                EndArea();
            }
        }

        public class ScrollViewScope : GUI.Scope
        {
            public Vector2 scrollPosition { get; private set; }
            public bool handleScrollWheel { get; set; }

            public ScrollViewScope(Vector2 scrollPosition, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, options);
            }

            public ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, options);
            }

            public ScrollViewScope(Vector2 scrollPosition, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, horizontalScrollbar, verticalScrollbar, options);
            }

            public ScrollViewScope(Vector2 scrollPosition, GUIStyle style, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, style, options);
            }

            public ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, options);
            }

            public ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background, options);
            }

            protected override void CloseScope()
            {
                EndScrollView(handleScrollWheel);
            }
        }
    }
}
