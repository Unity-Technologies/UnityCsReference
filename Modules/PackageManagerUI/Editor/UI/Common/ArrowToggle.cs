// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class ArrowToggle : Arrow
    {
        internal new class UxmlFactory : UxmlFactory<ArrowToggle> {}

        private bool _Expanded;
        public bool Expanded
        {
            get { return _Expanded; }
            set
            {
                _Expanded = value;

                this.EnableClassToggle("expanded", "collapsed", Expanded);

                if (_Expanded)
                    SetDirection(Direction.Down);
                else
                    SetDirection(Direction.Right);
            }
        }

        public ArrowToggle()
        {
            Expanded = false;
        }

        public void Toggle()
        {
            Expanded = !Expanded;
        }
    }
}
