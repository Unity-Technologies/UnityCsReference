// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Profiling.Editor
{
    // TODO This type will be made public as part of the Extensibility API.
    [System.Serializable]
    internal struct ProfilerCounterDescriptor
    {
        public ProfilerCounterDescriptor(string name, ProfilerCategory category) : this(name, category.Name) {}
        public ProfilerCounterDescriptor(string name, string categoryName)
        {
            Name = name;
            CategoryName = categoryName;
        }

        public string Name;
        public string CategoryName;

        public override string ToString() => $"{Name} ({CategoryName})";
    }
}
