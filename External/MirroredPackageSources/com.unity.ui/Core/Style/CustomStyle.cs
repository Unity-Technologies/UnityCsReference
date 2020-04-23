using System;

namespace UnityEngine.UIElements
{
    public struct CustomStyleProperty<T> : IEquatable<CustomStyleProperty<T>>
    {
        public string name { get; private set; }

        public CustomStyleProperty(string propertyName)
        {
            if (!String.IsNullOrEmpty(propertyName) && !propertyName.StartsWith("--"))
                throw new ArgumentException($"Custom style property \"{propertyName}\" must start with \"--\" prefix.");

            name = propertyName;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CustomStyleProperty<T>))
                return false;

            return Equals((CustomStyleProperty<T>)obj);
        }

        public bool Equals(CustomStyleProperty<T> other)
        {
            return name == other.name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public static bool operator==(CustomStyleProperty<T> a, CustomStyleProperty<T> b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(CustomStyleProperty<T> a, CustomStyleProperty<T> b)
        {
            return !(a == b);
        }
    }

    public interface ICustomStyle
    {
        bool TryGetValue(CustomStyleProperty<float> property, out float value);
        bool TryGetValue(CustomStyleProperty<int> property, out int value);
        bool TryGetValue(CustomStyleProperty<bool> property, out bool value);
        bool TryGetValue(CustomStyleProperty<Color> property, out Color value);
        bool TryGetValue(CustomStyleProperty<Texture2D> property, out Texture2D value);
        bool TryGetValue(CustomStyleProperty<VectorImage> property, out VectorImage value);
        bool TryGetValue(CustomStyleProperty<string> property, out string value);
    }
}
