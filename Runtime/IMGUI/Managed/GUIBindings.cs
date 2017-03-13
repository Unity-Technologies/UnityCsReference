// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public partial class GUI
    {
        /// Undocumented, but needs to be public because the implementations are public
        public abstract class Scope : IDisposable
        {
            bool m_Disposed;

            protected abstract void CloseScope();
            ~Scope()
            {
                if (!m_Disposed)
                    // Can warn again because we have the ExitingGUI hint
                    Debug.LogError("Scope was not disposed! You should use the 'using' keyword or manually call Dispose.");
                // ...but can't actually close scope because we can't do gui stuff from finalizer thread :-|
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                if (!GUIUtility.guiIsExiting)
                    CloseScope();
            }
        }

        public class GroupScope : Scope
        {
            public GroupScope(Rect position)
            {
                BeginGroup(position);
            }

            public GroupScope(Rect position, string text)
            {
                BeginGroup(position, text);
            }

            public GroupScope(Rect position, Texture image)
            {
                BeginGroup(position, image);
            }

            public GroupScope(Rect position, GUIContent content)
            {
                BeginGroup(position, content);
            }

            public GroupScope(Rect position, GUIStyle style)
            {
                BeginGroup(position, style);
            }

            public GroupScope(Rect position, string text, GUIStyle style)
            {
                BeginGroup(position, text, style);
            }

            public GroupScope(Rect position, Texture image, GUIStyle style)
            {
                BeginGroup(position, image, style);
            }

            protected override void CloseScope()
            {
                EndGroup();
            }
        }

        public class ScrollViewScope : Scope
        {
            public Vector2 scrollPosition { get; private set; }
            public bool handleScrollWheel { get; set; }

            public ScrollViewScope(Rect position, Vector2 scrollPosition, Rect viewRect)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(position, scrollPosition, viewRect);
            }

            public ScrollViewScope(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical);
            }

            public ScrollViewScope(Rect position, Vector2 scrollPosition, Rect viewRect, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(position, scrollPosition, viewRect, horizontalScrollbar, verticalScrollbar);
            }

            public ScrollViewScope(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar);
            }

            internal ScrollViewScope(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background);
            }

            protected override void CloseScope()
            {
                EndScrollView(handleScrollWheel);
            }
        }

        public class ClipScope : GUI.Scope
        {
            public ClipScope(Rect position)
            {
                BeginClip(position);
            }

            protected override void CloseScope()
            {
                EndClip();
            }
        }
    }
}
