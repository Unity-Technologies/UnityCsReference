namespace UnityEngine.UIElements
{
    public interface IBindable
    {
        IBinding binding { get; set; }
        string bindingPath { get; set; }
    }

    public interface IBinding
    {
        void PreUpdate();
        void Update();
        void Release();
    }

    public static class IBindingExtensions
    {
        public static bool IsBound(this IBindable control)
        {
            return control?.binding != null;
        }
    }
}
