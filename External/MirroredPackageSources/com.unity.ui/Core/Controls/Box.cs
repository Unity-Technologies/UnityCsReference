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
