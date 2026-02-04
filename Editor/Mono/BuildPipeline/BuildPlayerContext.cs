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
    ///<summary>Get a BuildPlayerContext object from a <see cref="Build.BuildPlayerProcessor.PrepareForBuild" /> callback.</summary>
    public sealed class BuildPlayerContext
    {
        // Temporary tracking through a global instance
        internal static BuildPlayerContext ActiveInstance { get; private set; }

        ///<summary>The player build options associated with this build.</summary>
        public BuildPlayerOptions BuildPlayerOptions { get; }

        internal BuildPlayerContext(BuildPlayerOptions buildPlayerOptions)
        {
            BuildPlayerOptions = buildPlayerOptions;
            ActiveInstance = this;
        }

        // Streaming Assets
        private Dictionary<NPath, NPath> StreamingAssetFiles { get; } = new Dictionary<NPath, NPath>();
        private List<string> AdditionalMetadataLocations = new List<string>();
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        internal IEnumerable<(NPath dst, NPath src)> StreamingAssets => StreamingAssetFiles.Select(e => (e.Key, e.Value));
#pragma warning restore UA2001

        ///<summary>Add an additional metadataPath to the BuildPlayerOptions.previousBuildMetadataLocations array passed in as part of the build. This is useful if you want the player build 
        ///to automatically retrieve type stripping information from builds you do before the player build itself.</summary>
        ///<remarks>
        ///If this method is called on the same path multiple times, it will only be called once.
        ///
        ///If the path passed into this method is not a valid build metadata directory, at build time an error will be thrown. 
        ///
        ///For more information on locating the metadata directory associated with a given build, refer to the UnityEditor.Build.BuildMetadata class</remarks>
        ///<param name="metadataPath">The path to a build metadata directory. If the path is invalid, an error will be thrown during the build process.</param>
        /*UCBP-PUBLIC*/ internal void AddAdditionalMetadataPathToPlayerOptions(string metadataPath)
        {
            if (!AdditionalMetadataLocations.Contains(metadataPath))
                AdditionalMetadataLocations.Add(metadataPath);
        }

        internal string[] RetrieveAdditionalMetadataLocations()
        {
            return AdditionalMetadataLocations.ToArray();
        }

        ///<summary>Add additional streaming assets to the built player data. For example, you can include AssetBundles or other streaming assets without first putting them in the project StreamingAssets folder.</summary>
        ///<remarks>You can add a single file or an entire directory.
        ///
        ///If a file or directory with the same destination path has already been added to the BuildPlayerContext, then this function throws an ArgumentException.
        ///
        ///If a file or directory with the same destination path already exists in the project, an exception is thrown later in the build process.</remarks>
        ///<param name="directoryOrFile">Path representing an existing file or directory. If the path doesn't exist, this function throws a FileNotFoundException.</param>
        ///<param name="pathInStreamingAssets">The path within the StreamingAssets folder at which to place the additional assets. If null, the file or directory is placed directly in the StreamingAssets folder.</param>
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
