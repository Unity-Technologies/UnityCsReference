namespace UnityEngine.UIElements
{
    /// <summary>
    /// Styled visual element to match the IMGUI Box Style.
    /// </summary>
    public class Box : VisualElement
    {
        /// <summary>
        /// Instantiates a <see cref="Box"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Box> {}

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-box";

        /// <summary>
        ///  Initializes and returns an instance of Box.
        /// </summary>
        public Box()
        {
            AddToClassList(ussClassName);
        }
    }
}
