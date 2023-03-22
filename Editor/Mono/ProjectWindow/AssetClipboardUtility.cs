// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.Internal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal static class AssetClipboardUtility
    {
        static HashSet<ObjectIdentifier> assetClipboard = new HashSet<ObjectIdentifier>();
        static PerformedAction performedAction = PerformedAction.None;
        internal static void DuplicateSelectedAssets()
        {
            performedAction = PerformedAction.None;
            Selection.objects = DuplicateAssets(Selection.objects).ToArray();
        }

        internal static void CutCopySelectedAssets(PerformedAction action)
        {
            performedAction = action;

            if (Selection.objects.Length > 0)
                CutCopyAssets(Selection.objects);
        }

        internal static void PasteSelectedAssets(bool isTwoColumnView)
        {
            string targetPath;

            // If we are using OneColumnLayout rely on object selection, otherwise rely on active folder
            if (!isTwoColumnView && Selection.objects.Length == 1)
            {
                targetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

                // If selected object is folder, make sure we paste into it.
                if (AssetDatabase.IsValidFolder(targetPath))
                {
                    targetPath += "/";
                }
                else
                {
                    targetPath = Path.GetDirectoryName(targetPath);
                }
            }
            else
            {
                targetPath = ProjectWindowUtil.GetActiveFolderPath();
            }

            switch (performedAction)
            {
                case PerformedAction.Copy:
                    Selection.objects = PasteCopiedAssets(targetPath).ToArray();
                    break;
                case PerformedAction.Cut:
                    Selection.objects = PasteCutAssets(targetPath).ToArray();
                    performedAction = PerformedAction.None;
                    break;
            }
        }

        internal static IEnumerable<Object> DuplicateAssets(IEnumerable<Object> assets)
        {
            CutCopyAssets(assets);
            return PasteCopiedAssets();
        }

        internal static IEnumerable<Object> DuplicateAssets(IEnumerable<int> instanceIDs)
        {
            return DuplicateAssets(instanceIDs.Select(id => EditorUtility.InstanceIDToObject(id)));
        }

        static void CutCopyAssets(IEnumerable<Object> assets)
        {
            Reset();
            foreach (var asset in assets)
            {
                ObjectIdentifier.GetObjectIdentifierFromObject(asset, out var identifier);
                assetClipboard.Add(identifier);
            }
        }

        static IEnumerable<Object> PasteCopiedAssets(string destination = null)
        {
            Object firstDuplicatedObjectToFail = null;
            List<string> pastedObjects = new List<string>();

            try
            {
                // StartAssetEditing begins a batch operation on the AssetDatabase
                // such that we don't do any actual imports until
                // AssetDatabase.StopAssetImporting is called.
                // This means that while function calls are allowed to happen
                // imports won't actually happen until AssetDatabase.StopAssetImporting
                // is called below.
                AssetDatabase.StartAssetEditing();

                // We batch the copies here
                foreach (var item in assetClipboard)
                {
                    var asset = ObjectIdentifier.ToObject(item);
                    var assetPath = AssetDatabase.GetAssetPath(asset);

                    // if duplicating a sub-asset, then create a copy next to the main asset file
                    if (asset != null && !String.IsNullOrEmpty(assetPath) && AssetDatabase.IsSubAsset(asset))
                    {
                        if (asset is ISubAssetNotDuplicatable || asset is GameObject)
                        {
                            firstDuplicatedObjectToFail = firstDuplicatedObjectToFail ? firstDuplicatedObjectToFail : asset;
                            continue;
                        }

                        var extension = NativeFormatImporterUtility.GetExtensionForAsset(asset);

                        // We dot sanitize or block unclean the asset filename (asset.name)
                        // since the assertdb will do it for us and has a whole tailored logic for that.

                        // It feels wrong that the asset name (that can apparently contain any char)
                        // is conflated with the orthogonal notion of filename. From the user's POV
                        // it will force an asset dup but with mangled names if the original name contained
                        // "invalid chars" for filenames.
                        // Path.Combine is not used here to avoid blocking asset names that might
                        // contain chars not allowed in filenames.
                        if ((new HashSet<char>(Path.GetInvalidFileNameChars())).Intersect(asset.name).Count() != 0)
                        {
                            Debug.LogWarning(string.Format("Duplicated asset name '{0}' contains invalid characters. Those will be replaced in the duplicated asset name.", asset.name));
                        }

                        var newPath = AssetDatabase.GenerateUniqueAssetPath(
                            string.Format("{0}{1}{2}.{3}",
                                Path.GetDirectoryName(assetPath),
                                Path.DirectorySeparatorChar,
                                asset.name,
                                extension)
                        );

                        assetPath = GetValidPath(newPath, destination);
                        AssetDatabase.CreateAsset(Object.Instantiate(asset), assetPath);
                        pastedObjects.Add(assetPath);
                    }
                    // Otherwise duplicate the main asset file
                    else if (EditorUtility.IsPersistent(asset))
                    {
                        var newPath = AssetDatabase.GenerateUniqueAssetPath(GetValidPath(assetPath, destination));
                        if (newPath.Length > 0 && AssetDatabase.CopyAsset(assetPath, newPath))
                            pastedObjects.Add(newPath);
                    }
                }
            }
            finally
            {
                // Batch import all assets that were created & copied in the try{} block above
                AssetDatabase.StopAssetEditing();
            }

            if (firstDuplicatedObjectToFail != null)
            {
                var errString = string.Format(
                    "Duplication error: One or more sub assets (with types of {0}) can not be duplicated directly, use the appropriate editor instead",
                    firstDuplicatedObjectToFail.GetType().Name
                );

                Debug.LogError(errString, firstDuplicatedObjectToFail);
            }

            return pastedObjects.Select(AssetDatabase.LoadMainAssetAtPath);
        }

        static IEnumerable<Object> PasteCutAssets(string destination = null)
        {
            List<string> pastedObjects = new List<string>();
            string[] assetPaths = new string[assetClipboard.Count];

            int counter = 0;
            foreach (var item in assetClipboard)
            {
                assetPaths[counter] = AssetDatabase.GetAssetPath(ObjectIdentifier.ToObject(item));
                counter++;
            }

            if (counter > 0)
                Undo.RegisterAssetsMoveUndo(assetPaths);

            counter = 0;
            foreach (var item in assetClipboard)
            {
                var assetPath = assetPaths[counter];
                var validPath = GetValidPath(assetPath, destination);
                var obj = AssetDatabase.LoadAssetAtPath(validPath, typeof(Object));
                var newPath = assetPath != validPath && obj != null ? AssetDatabase.GenerateUniqueAssetPath(validPath) : validPath;

                if (String.IsNullOrEmpty(AssetDatabase.ValidateMoveAsset(assetPath, newPath)))
                {
                    AssetDatabase.MoveAsset(assetPath, newPath);
                    pastedObjects.Add(newPath);
                }

                counter++;
            }

            Reset();
            AssetDatabase.Refresh();

            return pastedObjects.Select(AssetDatabase.LoadMainAssetAtPath);
        }

        // Returns list of duplicated instanceIDs
        internal static int[] DuplicateFolders(int[] instanceIDs)
        {
            CutCopySelectedFolders(instanceIDs, PerformedAction.Copy);
            return PasteFoldersAfterCopy();
        }

        internal static void CutCopySelectedFolders(int[] instanceIDs, PerformedAction action)
        {
            Reset();
            performedAction = action;
            AssetDatabase.Refresh();

            int assetsFolderInstanceID = AssetDatabase.GetMainAssetOrInProgressProxyInstanceID("Assets");

            foreach (int instanceID in instanceIDs)
            {
                if (instanceID == assetsFolderInstanceID)
                    continue;

                ObjectIdentifier.GetObjectIdentifierFromInstanceID(instanceID, out var identifier);
                assetClipboard.Add(identifier);
            }
        }

        internal static int[] PasteFolders()
        {
            if (performedAction == PerformedAction.Cut)
            {
                return PasteFoldersAfterCut(ProjectWindowUtil.GetActiveFolderPath());
            }
            else
            {
                return PasteFoldersAfterCopy(ProjectWindowUtil.GetActiveFolderPath());
            }
        }

        static int[] PasteFoldersAfterCopy(string destination = null)
        {
            List<string> copiedPaths = new List<string>();
            bool failed = false;
            foreach (var item in assetClipboard)
            {
                string assetPath = AssetDatabase.GetAssetPath(ObjectIdentifier.ToObject(item));
                var newPath = AssetDatabase.GenerateUniqueAssetPath(GetValidPath(assetPath, destination));

                if (newPath.Length != 0)
                    failed |= !AssetDatabase.CopyAsset(assetPath, newPath);
                else
                    failed = true;

                if (!failed)
                {
                    copiedPaths.Add(newPath);
                }
            }

            AssetDatabase.Refresh();

            int[] copiedAssets = new int[copiedPaths.Count];
            for (int i = 0; i < copiedPaths.Count; i++)
            {
                copiedAssets[i] = AssetDatabase.LoadMainAssetAtPath(copiedPaths[i]).GetInstanceID();
            }

            return copiedAssets;
        }

        static int[] PasteFoldersAfterCut(string destination = null)
        {
            List<string> pastedObjects = new List<string>();
            string[] assetPaths = new string[assetClipboard.Count];

            int counter = 0;
            foreach (var item in assetClipboard)
            {
                assetPaths[counter] = AssetDatabase.GetAssetPath(ObjectIdentifier.ToObject(item));
                counter++;
            }

            if (counter > 0)
                Undo.RegisterAssetsMoveUndo(assetPaths);

            counter = 0;
            foreach (var item in assetClipboard)
            {
                var assetPath = assetPaths[counter];

                var newPath = AssetDatabase.GenerateUniqueAssetPath(GetValidPath(assetPath, destination));
                if (String.IsNullOrEmpty(AssetDatabase.MoveAsset(assetPath, newPath)))
                {
                    pastedObjects.Add(newPath);
                    counter++;
                }
            }

            Reset();
            performedAction = PerformedAction.None;
            AssetDatabase.Refresh();

            int[] copiedAssets = new int[pastedObjects.Count];

            for (int i = 0; i < pastedObjects.Count; i++)
            {
                copiedAssets[i] = AssetDatabase.LoadMainAssetAtPath(pastedObjects[i]).GetInstanceID();
            }

            return copiedAssets;
        }

        internal static bool CanPaste()
        {
            return performedAction != PerformedAction.None;
        }

        internal static bool HasCutAsset(int instanceID)
        {
            if (performedAction == PerformedAction.Cut)
            {
                Object obj = EditorUtility.InstanceIDToObject(instanceID);
                ObjectIdentifier.GetObjectIdentifierFromInstanceID(instanceID, out var identifier);
                return obj != null && assetClipboard.Contains(identifier);
            }

            return false;
        }

        internal static void CancelCut()
        {
            Reset();
            performedAction = PerformedAction.None;
        }

        static void Reset()
        {
            assetClipboard.Clear();
        }

        static string GetValidPath(string assetPath, string target)
        {
            if (target == null)
                return assetPath;

            string[] pathSplit = assetPath.Split('/');
            return target + '/' + pathSplit[pathSplit.Length - 1];
        }

        internal enum PerformedAction
        {
            Cut,
            Copy,
            None
        };
    }
}
