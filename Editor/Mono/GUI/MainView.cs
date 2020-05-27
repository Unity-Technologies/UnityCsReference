// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class MainView : View, ICleanuppable
    {
        internal const float kToolbarHeight = 30;
        internal const float kStatusbarHeight = 20;

        private static readonly Vector2 kMinSize = new Vector2(875, 300);
        private static readonly Vector2 kMaxSize = new Vector2(10000, 10000);

        [SerializeField] private bool m_UseTopView;
        [SerializeField] private float m_TopViewHeight;
        [SerializeField] private bool m_UseBottomView;
        [SerializeField] private float m_BottomViewHeight;

        public bool useTopView { get => m_UseTopView; set => m_UseTopView = value; }
        public float topViewHeight { get => m_TopViewHeight; set => m_TopViewHeight = value; }

        public bool useBottomView { get => m_UseBottomView; set => m_UseBottomView = value; }
        public float bottomViewHeight { get => m_BottomViewHeight; set => m_BottomViewHeight = value; }

        public MainView()
        {
            topViewHeight = kToolbarHeight;
            bottomViewHeight = kStatusbarHeight;
            useTopView = useBottomView = true;
        }

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
                Toolbar t = children[0] as Toolbar;
                topViewHeight = t != null ? t.CalcHeight() : topViewHeight;
                children[0].position = new Rect(0, 0, newPos.width, topViewHeight);
                children[1].position = new Rect(0, topViewHeight, newPos.width, newPos.height - topViewHeight - children[2].position.height);
                children[2].position = new Rect(0, newPos.height - children[2].position.height, newPos.width, children[2].position.height);
            }
            else
            {
                // dock area view - status bar
                if (useBottomView && children.Length >= 2)
                {
                    children[0].position = new Rect(0, 0, newPos.width, newPos.height - children[1].position.height);
                    children[1].position = new Rect(0, newPos.height - children[1].position.height, newPos.width, children[1].position.height);
                }
                else if (useTopView && children.Length >= 2)
                {
                    Toolbar t = children[0] as Toolbar;
                    topViewHeight = t != null ? t.CalcHeight() : topViewHeight;
                    children[0].position = new Rect(0, 0, newPos.width, topViewHeight);
                    children[1].position = new Rect(0, children[0].position.height, newPos.width, newPos.height - children[0].position.height);
                }
                else if (children.Length >= 1)
                    children[0].position = new Rect(0, 0, newPos.width, newPos.height);
            }
        }

        protected override void ChildrenMinMaxChanged()
        {
            if (children.Length == 3 && children[0] is Toolbar)
            {
                // toolbar - dock area view - status bar
                Toolbar t = (Toolbar)children[0];
                var min = new Vector2(minSize.x, Mathf.Max(minSize.y, t.CalcHeight() + bottomViewHeight + children[1].minSize.y));
                SetMinMaxSizes(min, maxSize);
            }
            else if (children.Length == 2)
            {
                // dock area view - status bar
                var min = new Vector2(minSize.x, Mathf.Max(minSize.y, bottomViewHeight + children[1].minSize.y));
                SetMinMaxSizes(min, maxSize);
            }
            base.ChildrenMinMaxChanged();
        }

        public void Cleanup()
        {
            // If we only have one child left, this means all views have been dragged out.
            // So we resize the window to be just the toolbar
            // On windows, this might need some special handling for the main menu
            if (children.Length == 3 && children[1].children.Length == 0)
            {
                Rect r = window.position;
                Toolbar t = children[0] as Toolbar;
                topViewHeight = t != null ? t.CalcHeight() : topViewHeight;
                r.height = topViewHeight + bottomViewHeight;
            }
        }
    }
} //namespace
