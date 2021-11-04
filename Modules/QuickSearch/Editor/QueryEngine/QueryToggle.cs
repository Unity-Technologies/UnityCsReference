// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Search
{
    readonly struct QueryToggle
    {
        public readonly StringView rawText;
        public readonly StringView value;

        public QueryToggle(in StringView rawText, in StringView value)
        {
            this.rawText = rawText;
            this.value = value;
        }
    }
}
