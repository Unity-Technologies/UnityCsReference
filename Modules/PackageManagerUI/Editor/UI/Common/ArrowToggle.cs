// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class ArrowToggle : Arrow
    {
        internal new class UxmlFactory : UxmlFactory<ArrowToggle> {}

        private bool m_Expanded;
        public bool expanded
        {
            get { return m_Expanded; }
            set
            {
                m_Expanded = value;

                this.EnableClassToggle("expanded", "collapsed", expanded);

                if (m_Expanded)
                    SetDirection(Direction.Down);
                else
                    SetDirection(Direction.Right);
            }
        }

        public ArrowToggle()
        {
            expanded = false;
        }

        public void Toggle()
        {
            expanded = !expanded;
        }
    }
}
