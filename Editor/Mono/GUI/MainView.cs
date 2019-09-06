// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class MainView : View, ICleanuppable
    {
        const float kStatusbarHeight = 20;

        private static readonly Vector2 kMinSize = new Vector2(875, 300);
        private static readonly Vector2 kMaxSize = new Vector2(10000, 10000);

        void OnEnable()
        {
            SetMinMaxSizes(kMinSize, kMaxSize);
        }

        protected override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            if (children.Length == 0)
                return;
            if (children.Length > 2)
            {
                // toolbar - dock area view - status bar
                Toolbar t = (Toolbar)children[0];
                children[0].position = new Rect(0, 0, newPos.width, t.CalcHeight());
                children[1].position = new Rect(0,  t.CalcHeight(), newPos.width, newPos.height - t.CalcHeight() - children[2].position.height);
                children[2].position = new Rect(0, newPos.height - children[2].position.height, newPos.width, children[2].position.height);
            }
            else
            {
                // dock area view - status bar
                children[0].position = new Rect(0, 0, newPos.width, newPos.height - children[1].position.height);
                children[1].position = new Rect(0, newPos.height - children[1].position.height, newPos.width, children[1].position.height);
            }
        }

        protected override void ChildrenMinMaxChanged()
        {
            if (children.Length == 3 && children[0] is Toolbar)
            {
                // toolbar - dock area view - status bar
                Toolbar t = (Toolbar)children[0];
                var min = new Vector2(minSize.x, Mathf.Max(minSize.y, t.CalcHeight() + kStatusbarHeight + children[1].minSize.y));
                SetMinMaxSizes(min, maxSize);
            }
            else if (children.Length == 2)
            {
                // dock area view - status bar
                var min = new Vector2(minSize.x, Mathf.Max(minSize.y, kStatusbarHeight + children[1].minSize.y));
                SetMinMaxSizes(min, maxSize);
            }
            base.ChildrenMinMaxChanged();
        }

        public static void MakeMain()
        {
            //Set up default window size
            ContainerWindow cw = ScriptableObject.CreateInstance<ContainerWindow>();
            var main = ScriptableObject.CreateInstance<MainView>();
            main.SetMinMaxSizes(kMinSize, kMaxSize);
            cw.rootView = main;

            Resolution res = Screen.currentResolution;
            int width = Mathf.Clamp(res.width * 3 / 4, 800, 1400);
            int height = Mathf.Clamp(res.height * 3 / 4, 600, 950);
            cw.position = new Rect(60, 20, width, height);

            cw.Show(ShowMode.MainWindow, loadPosition: true, displayImmediately: true, setFocus: true);
            cw.DisplayAllViews();
        }

        public void Cleanup()
        {
            // If we only have one child left, this means all views have been dragged out.
            // So we resize the window to be just the toolbar
            // On windows, this might need some special handling for the main menu
            if (children.Length == 3 && children[1].children.Length == 0)
            {
                Rect r = window.position;
                Toolbar t = (Toolbar)children[0];
                r.height = t.CalcHeight() + kStatusbarHeight;
            }
        }
    }
} //namespace
