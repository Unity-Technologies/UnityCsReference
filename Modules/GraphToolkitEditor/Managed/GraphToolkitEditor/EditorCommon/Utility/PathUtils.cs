// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;

namespace Unity.GraphToolkit.Editor;

static class PathUtils
{
    public static string NormalizePath(this string path)
    {
        if (Path.DirectorySeparatorChar == '\\')
            return path.Replace('/', Path.DirectorySeparatorChar);
        return path.Replace('\\', Path.DirectorySeparatorChar);
    }
}
