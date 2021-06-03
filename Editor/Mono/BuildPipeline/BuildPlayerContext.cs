// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NiceIO;

namespace UnityEditor.Build
{
    public sealed class BuildPlayerContext
    {
        // Temporary tracking through a global instance
        internal static BuildPlayerContext ActiveInstance { get; private set; }

        public BuildPlayerOptions BuildPlayerOptions { get; }

        internal BuildPlayerContext(BuildPlayerOptions buildPlayerOptions)
        {
            BuildPlayerOptions = buildPlayerOptions;
            ActiveInstance = this;
        }

        // Streaming Assets
        private Dictionary<NPath, NPath> StreamingAssetFiles { get; } = new Dictionary<NPath, NPath>();
        internal IEnumerable<(NPath dst, NPath src)> StreamingAssets => StreamingAssetFiles.Select(e => (e.Key, e.Value));

        public void AddAdditionalPathToStreamingAssets(string directoryOrFile, string pathInStreamingAssets = null)
        {
            NPath sourcePath = directoryOrFile;
            if (sourcePath.DirectoryExists())
            {
                NPath targetPath = pathInStreamingAssets ?? "";
                foreach (var file in sourcePath.Files(true))
                    AddAdditionalFileToStreamingAssets(file, targetPath.Combine(file.RelativeTo(sourcePath)));
            }
            else if (sourcePath.FileExists())
            {
                NPath targetPath = pathInStreamingAssets ?? sourcePath.FileName;
                AddAdditionalFileToStreamingAssets(sourcePath, targetPath);
            }
            else
            {
                throw new FileNotFoundException("No such file or directory.", sourcePath.ToString());
            }
        }

        private void AddAdditionalFileToStreamingAssets(NPath sourceFile, NPath targetPath)
        {
            if (StreamingAssetFiles.TryGetValue(targetPath, out var existingValue))
            {
                // If someone is adding the same file more than once we ignore subsequent adds
                if (existingValue == sourceFile)
                    return;

                // Throw an exception and tell the user what the problem is
                throw new ArgumentException(
                    $"Unable to add '{sourceFile}' to StreamingAssets. An entry for '{targetPath}' has already been added, '{existingValue}'.");
            }
            StreamingAssetFiles.Add(targetPath, sourceFile);
        }
    }
}
