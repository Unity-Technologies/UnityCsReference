// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    public class Box : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<Box> {}

        public static readonly string ussClassName = "unity-box";

        public Box()
        {
            AddToClassList(ussClassName);
        }
    }
}
