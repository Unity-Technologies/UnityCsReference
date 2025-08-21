// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using Unity.Multiplayer.PlayMode.Editor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class PathsUtility
    {
        public static string GetProjectPathByIdentifier(VirtualProjectIdentifier identifier, params string[] additionalPaths)
        {
            var paths = new List<string>
            {
                Paths.CurrentProjectVirtualProjectsFolder,
                identifier.ToString(),
            };
            paths.AddRange(additionalPaths);
            return Path.GetFullPath(Path.Combine(paths.ToArray()));
        }
    }
}
