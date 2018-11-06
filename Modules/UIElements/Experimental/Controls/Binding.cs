// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
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
