// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Collections.Generic;
using Object = UnityEngine.Object;

/* Some notes on icon caches and loading
 *
 *
 * Caches:
 *       Icon name to Texture2D:
 *          gNamedTextures (ObjectImages::Texture2DNamed): string to Texture2D
 *
 *      InstanceID to Image
 *          gCachedThumbnails (AssetImporter::GetThumbnailForInstanceID) : where image is retrieved from Texture from gNamedTextures
 *          Used for setting icon for hierarchyProperty
 *
 *      InstanceID to Texture2D
 *          ms_HackedCache (AssetDatabaseProperty::GetCachedAssetDatabaseIcon) : where tetxure2D icon is generated from the image from gCachedThumbnails
 *          Cache max size: 200
 *          Called from C# by AssetDatabase.GetCachedIcon (string assetpath)
 *
 *
 *
 *
    Icon loading in Editor Default Resources project:
    - Texture2DNamed handles reading local files instead of editor resources bundle

    Icon loading in other projects
    - When reimport of asset (cpp): AssetDatabase::ImportAsset -> MonoImporter::GenerateAssetData -> ImageForMonoScript -> GetImageNames -> ObjectImages::Texture2DNamed
    - IconContent -> LoadIconRequired -> LoadIcon -> LoadIconForSkin -> Load (EditorResourcesUtility.iconsPath + name) as Texture2D;
    - AssetDatabase.GetCachedIcon (assetpath) -> CPP -> AssetDatabaseProperty::GetCachedAssetDatabaseIcon
    - InternalEditorUtility.GetIconForFile (filename) -> EditorGUIUtility.FindTexture("boo Script Icon") -> CPP -> ObjectImages::Texture2DNamed(cpp)

    */

namespace UnityEditorInternal
{
    public class GenerateIconsWithMipLevels
    {
        static string k_IconSourceFolder = "Assets/MipLevels For Icons/";
        static string k_IconTargetFolder = "Assets/Editor Default Resources/Icons/Processed";
        static string k_IconMipIdentifier = "@";

        class InputData
        {
            public string sourceFolder;
            public string targetFolder;
            public string mipIdentifier;
            public string mipFileExtension;
            public List<string> generatedFileNames = new List<string>(); // for internal use

            public string GetMipFileName(string baseName, int mipResolution)
            {
                return sourceFolder + baseName + mipIdentifier + mipResolution + "." + mipFileExtension;
            }
        }

        private static InputData GetInputData()
        {
            InputData data = new InputData();
            data.sourceFolder = k_IconSourceFolder;
            data.targetFolder = k_IconTargetFolder;
            data.mipIdentifier = k_IconMipIdentifier;
            data.mipFileExtension = "png";
            return data;
        }

        // Called from BuildEditorAssetBundles
        public static void GenerateAllIconsWithMipLevels()
        {
            var data = GetInputData();

            EnsureFolderIsCreated(data.targetFolder);

            float startTime = Time.realtimeSinceStartup;
            GenerateIconsWithMips(data);
            Debug.Log(string.Format("Generated {0} icons with mip levels in {1} seconds", data.generatedFileNames.Count, Time.realtimeSinceStartup - startTime));

            RemoveUnusedFiles(data.generatedFileNames);
            AssetDatabase.Refresh(); // For some reason we creash if we dont refresh twice...
            InternalEditorUtility.RepaintAllViews();
        }

        public static bool VerifyIconPath(string assetPath, bool logError)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }
            else if (assetPath.IndexOf(k_IconSourceFolder) < 0)
            {
                if (logError)
                    Debug.Log("Selection is not a valid mip texture, it should be located in: " + k_IconSourceFolder);
                return false;
            }
            else if (assetPath.IndexOf(k_IconMipIdentifier) < 0)
            {
                if (logError)
                    Debug.Log("Selection does not have a valid mip identifier " + assetPath + "  " + k_IconMipIdentifier);
                return false;
            }
            return true;
        }

        // Refresh just one icon (Used in Editor Resources project, find it in Tools/)
        public static void GenerateSelectedIconsWithMips()
        {
            // If no selection do all
            if (Selection.activeInstanceID == 0)
            {
                Debug.Log("Ensure to select a mip texture..." + Selection.activeInstanceID);
                return;
            }

            var data = GetInputData();

            int instanceID = Selection.activeInstanceID;
            string assetPath = AssetDatabase.GetAssetPath(instanceID);

            if (!VerifyIconPath(assetPath, true))
            {
                return;
            }

            float startTime = Time.realtimeSinceStartup;

            string baseName = assetPath.Replace(data.sourceFolder, "");
            baseName = baseName.Substring(0, baseName.LastIndexOf(data.mipIdentifier));

            List<string> assetPaths = GetIconAssetPaths(data.sourceFolder, data.mipIdentifier, data.mipFileExtension);

            EnsureFolderIsCreated(data.targetFolder);
            GenerateIcon(data, baseName, assetPaths, null, null);
            Debug.Log(string.Format("Generated {0} icon with mip levels in {1} seconds", baseName, Time.realtimeSinceStartup - startTime));
            InternalEditorUtility.RepaintAllViews();
        }

        // Refresh just one icon with provided mip levels
        public static void GenerateIconWithMipLevels(string assetPath, Dictionary<int, Texture2D> mipTextures, FileInfo fileInfo)
        {
            if (!VerifyIconPath(assetPath, true))
            {
                return;
            }

            var data = GetInputData();

            float startTime = Time.realtimeSinceStartup;

            string baseName = assetPath.Replace(data.sourceFolder, "");
            baseName = baseName.Substring(0, baseName.LastIndexOf(data.mipIdentifier));

            List<string> assetPaths = GetIconAssetPaths(data.sourceFolder, data.mipIdentifier, data.mipFileExtension);

            EnsureFolderIsCreated(data.targetFolder);
            if (GenerateIcon(data, baseName, assetPaths, mipTextures, fileInfo))
            {
                Debug.Log(string.Format("Generated {0} icon with mip levels in {1} seconds", baseName, Time.realtimeSinceStartup - startTime));
            }
            InternalEditorUtility.RepaintAllViews();
        }

        // Search for the mip level encoded in the asset path
        public static int MipLevelForAssetPath(string assetPath, string separator)
        {
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(separator))
                return -1;

            int separatorIndex = assetPath.IndexOf(separator);
            if (separatorIndex == -1)
            {
                Debug.LogError("\"" + separator + "\" could not be found in asset path: " + assetPath);
                return -1;
            }

            int startIndex = separatorIndex + separator.Length;
            int endIndex = assetPath.IndexOf(".", startIndex);
            if (endIndex == -1)
            {
                Debug.LogError("Could not find path extension in asset path: " + assetPath);
                return -1;
            }

            return Int32.Parse(assetPath.Substring(startIndex, endIndex - startIndex));
        }

        // Private section
        //----------------
        private static void GenerateIconsWithMips(InputData inputData)
        {
            List<string> files = GetIconAssetPaths(inputData.sourceFolder, inputData.mipIdentifier, inputData.mipFileExtension);

            if (files.Count == 0)
            {
                Debug.LogWarning("No mip files found for generating icons! Searching in: " + inputData.sourceFolder + ", for files with extension: " + inputData.mipFileExtension);
            }

            string[] baseNames = GetBaseNames(inputData, files);

            // Base name is assumed to be like: "Assets/Icons/..."
            foreach (string baseName in baseNames)
                GenerateIcon(inputData, baseName, files, null, null);
        }

        private static bool GenerateIcon(InputData inputData, string baseName, List<string> assetPathsOfAllIcons, Dictionary<int, Texture2D> mipTextures, FileInfo sourceFileInfo)
        {
            //We need to create the folder before we Create the Asset.
            //AssetDatabase.CreateFolder will trigger a Asset Garbage Collection and stack items are not preserved.
            string generatedPath = inputData.targetFolder + "/" + baseName + " Icon" + ".asset";

            if (sourceFileInfo != null && File.Exists(generatedPath))
            {
                var fileInfo = new FileInfo(generatedPath);
                if (fileInfo.LastWriteTime > sourceFileInfo.LastWriteTime)
                {
                    // The resulting MIPS is newer than the source. Nothing to do.
                    return false;
                }
            }

            Debug.Log("Generating MIP levels for " + generatedPath);

            EnsureFolderIsCreatedRecursively(Path.GetDirectoryName(generatedPath));

            Texture2D iconWithMips = CreateIconWithMipLevels(inputData, baseName, assetPathsOfAllIcons, mipTextures);
            if (iconWithMips == null)
            {
                Debug.Log("CreateIconWithMipLevels failed");
                return false;
            }

            iconWithMips.name = baseName + " Icon" + ".png"; // asset name must have .png as extension (used when loading the icon, search for LoadIconForSkin)
            // Write texture to disk


            AssetDatabase.CreateAsset(iconWithMips, generatedPath);
            inputData.generatedFileNames.Add(generatedPath);

            return true;
        }

        private static Texture2D CreateIconWithMipLevels(InputData inputData, string baseName, List<string> assetPathsOfAllIcons, Dictionary<int, Texture2D> mipTextures)
        {
            List<string> allMipPaths = assetPathsOfAllIcons.FindAll(delegate(string o) { return o.IndexOf('/' + baseName + inputData.mipIdentifier) >= 0; });
            List<Texture2D> allMips = new List<Texture2D>();
            foreach (string path in allMipPaths)
            {
                int mipLevel = MipLevelForAssetPath(path, inputData.mipIdentifier);

                Texture2D mip = null;
                if (mipTextures != null && mipTextures.ContainsKey(mipLevel))
                    mip = mipTextures[mipLevel];
                else
                    mip = GetTexture2D(path);

                if (mip != null)
                    allMips.Add(mip);
                else
                    Debug.LogError("Mip not found " + path);
            }

            // Sort the mips by size (largest mip first)
            allMips.Sort(delegate(Texture2D first, Texture2D second)
                {
                    if (first.width == second.width)
                        return 0;
                    else if (first.width < second.width)
                        return 1;
                    else
                        return -1;
                });

            int minResolution = 99999;
            int maxResolution = 0;

            foreach (Texture2D mip in allMips)
            {
                int width = mip.width;
                if (width > maxResolution)
                    maxResolution = width;
                if (width < minResolution)
                    minResolution = width;
            }

            if (maxResolution == 0)
                return null;

            // Create our icon
            Texture2D iconWithMips = new Texture2D(maxResolution, maxResolution, TextureFormat.RGBA32, true, true);

            // Add max mip
            if (BlitMip(iconWithMips, allMips, 0))
                iconWithMips.Apply(true);
            else
                return iconWithMips; // ERROR

            // Keep for debugging
            //Debug.Log ("Processing max mip file: " + inputData.GetMipFileName (baseName, maxResolution) );

            // Now add the rest
            int resolution = maxResolution;
            for (int i = 1; i < iconWithMips.mipmapCount; i++)
            {
                resolution /= 2;
                if (resolution < minResolution)
                    break;

                BlitMip(iconWithMips, allMips, i);
            }
            iconWithMips.Apply(false, true);


            return iconWithMips;
        }

        private static bool BlitMip(Texture2D iconWithMips, List<Texture2D> sortedTextures, int mipLevel)
        {
            if (mipLevel < 0 || mipLevel >= sortedTextures.Count)
            {
                Debug.LogError("Invalid mip level: " + mipLevel);
                return false;
            }

            Texture2D tex = sortedTextures[mipLevel];
            if (tex)
            {
                Blit(tex, iconWithMips, mipLevel);
                return true;
            }
            else
            {
                Debug.LogError("No texture at mip level: " + mipLevel);
            }

            return false;
        }

        private static Texture2D GetTexture2D(string path)
        {
            return AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture2D;
        }

        static List<string> GetIconAssetPaths(string folderPath, string mustHaveIdentifier, string extension)
        {
            string curDirectory = Directory.GetCurrentDirectory();
            string absolute = Path.Combine(curDirectory, folderPath);
            Uri absoluteUri = new Uri(absolute);
            List<string> files =  new List<string>(Directory.GetFiles(absolute, "*." + extension, SearchOption.AllDirectories));
            files.RemoveAll(delegate(string o) {return o.IndexOf(mustHaveIdentifier) < 0; }); // // Remove all files that do not have the 'mustHaveIdentifier'
            for (int i = 0; i < files.Count; ++i)
            {
                Uri fileUri = new Uri(files[i]);
                Uri relativeUri = absoluteUri.MakeRelativeUri(fileUri);
                files[i] = folderPath + relativeUri.ToString();
            }
            return files;
        }

        static void Blit(Texture2D source, Texture2D dest, int mipLevel)
        {
            Color32[] pixels = source.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                if (p.a >= 3)
                    p.a -= 3;
                pixels[i] = p;
            }
            dest.SetPixels32(pixels, mipLevel);
        }

        private static void EnsureFolderIsCreatedRecursively(string targetFolder)
        {
            if (AssetDatabase.GetMainAssetInstanceID(targetFolder) == 0)
            {
                EnsureFolderIsCreatedRecursively(Path.GetDirectoryName(targetFolder));
                Debug.Log("Created target folder " + targetFolder);
                AssetDatabase.CreateFolder(Path.GetDirectoryName(targetFolder), Path.GetFileName(targetFolder));
            }
        }

        private static void EnsureFolderIsCreated(string targetFolder)
        {
            if (AssetDatabase.GetMainAssetInstanceID(targetFolder) == 0)
            {
                Debug.Log("Created target folder " + targetFolder);
                AssetDatabase.CreateFolder(Path.GetDirectoryName(targetFolder), Path.GetFileName(targetFolder));
            }
        }

        static void DeleteFile(string file)
        {
            if (AssetDatabase.GetMainAssetInstanceID(file) != 0)
            {
                Debug.Log("Deleted unused file: " + file);
                AssetDatabase.DeleteAsset(file);
            }
        }

        // Get rid of old icons in the Icons folder (with same filename as a generated icon)
        static void RemoveUnusedFiles(List<string> generatedFiles)
        {
            for (int i = 0; i < generatedFiles.Count; ++i)
            {
                string deleteFile = generatedFiles[i].Replace("Icons/Processed", "Icons");
                deleteFile = deleteFile.Replace(".asset", ".png");
                DeleteFile(deleteFile);

                // Remove the d_ version as well
                string fileName = Path.GetFileNameWithoutExtension(deleteFile);
                if (!fileName.StartsWith("d_"))
                {
                    deleteFile = deleteFile.Replace(fileName, ("d_" + fileName));
                    DeleteFile(deleteFile);
                }
            }
            AssetDatabase.Refresh();
        }

        private static string[] GetBaseNames(InputData inputData, List<string> files)
        {
            string[] baseNames = new string[files.Count];
            int startPos = inputData.sourceFolder.Length;
            for (int i = 0; i < files.Count; ++i)
            {
                baseNames[i] = files[i].Substring(startPos, files[i].IndexOf(inputData.mipIdentifier) - startPos);
            }
            HashSet<string> hashset = new HashSet<string>(baseNames);
            baseNames = new string[hashset.Count];
            hashset.CopyTo(baseNames);

            return baseNames;
        }
    }
} // namespace UnityEditor
