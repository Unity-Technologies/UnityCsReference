// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Profiling.Editor
{
    [System.Serializable]
    public readonly struct ProfilerCounterDescriptor
    {
        public ProfilerCounterDescriptor(string name, ProfilerCategory category) : this(name, category.Name) {}
        public ProfilerCounterDescriptor(string name, string categoryName)
        {
            Name = name;
            CategoryName = categoryName;
        }

        public readonly string Name { get; }
        public readonly string CategoryName { get; }

        public override string ToString() => $"{Name} ({CategoryName})";
    }
}
