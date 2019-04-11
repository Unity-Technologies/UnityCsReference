// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor
{
    internal interface IFileOpenInfo
    {
        string FilePath { get; set; }
        int LineNumber { get; set; }
    }
}
