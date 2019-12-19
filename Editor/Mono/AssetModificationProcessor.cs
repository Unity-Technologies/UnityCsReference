// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEditorInternal.VersionControl;
using System.Linq;
using System.Reflection;

[Obsolete("Use UnityEditor.AssetModificationProcessor")]
public class AssetModificationProcessor
{
}

namespace UnityEditor
{
    public class AssetModificationProcessor
    {
    }

    internal class AssetModificationProcessorInternal
    {
        enum FileMode
        {
            Binary,
            Text
        }

        static bool CheckArgumentTypes(Type[] types, MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();

            if (types.Length != parameters.Length)
            {
                Debug.LogWarning("Parameter count did not match. Expected: " + types.Length + " Got: " + parameters.Length + " in " + method.DeclaringType + "." + method.Name);
                return false;
            }

            int i = 0;
            foreach (Type type in types)
            {
                ParameterInfo pInfo = parameters[i];
                if (type != pInfo.ParameterType)
                {
                    Debug.LogWarning("Parameter type mismatch at parameter " + i + ". Expected: " + type + " Got: " + pInfo.ParameterType + " in " + method.DeclaringType + "." + method.Name);
                    return false;
                }
                ++i;
            }

            return true;
        }

        static bool CheckArgumentTypesAndReturnType(Type[] types, MethodInfo method, System.Type returnType)
        {
            if (returnType != method.ReturnType)
            {
                Debug.LogWarning("Return type mismatch. Expected: " + returnType + " Got: " + method.ReturnType + " in " + method.DeclaringType + "." + method.Name);
                return false;
            }

            return CheckArgumentTypes(types, method);
        }

        static bool CheckArguments(object[] args, MethodInfo method)
        {
            Type[] types = new Type[args.Length];

            for (int i = 0; i < args.Length; i++)
                types[i] = args[i].GetType();

            return CheckArgumentTypes(types, method);
        }

        static bool CheckArgumentsAndReturnType(object[] args, MethodInfo method, System.Type returnType)
        {
            Type[] types = new Type[args.Length];

            for (int i = 0; i < args.Length; i++)
                types[i] = args[i].GetType();

            return CheckArgumentTypesAndReturnType(types, method, returnType);
        }

#pragma warning disable 0618
        static System.Collections.Generic.IEnumerable<System.Type> assetModificationProcessors = null;
        static System.Collections.Generic.IEnumerable<System.Type> AssetModificationProcessors
        {
            get
            {
                if (assetModificationProcessors == null)
                {
                    List<Type> processors = new List<Type>();
                    processors.AddRange(TypeCache.GetTypesDerivedFrom<AssetModificationProcessor>());
                    processors.AddRange(TypeCache.GetTypesDerivedFrom<global::AssetModificationProcessor>());
                    assetModificationProcessors = processors.ToArray();
                }
                return assetModificationProcessors;
            }
        }
#pragma warning restore 0618

        static void OnWillCreateAsset(string path)
        {
            foreach (var assetModificationProcessorClass in AssetModificationProcessors)
            {
                MethodInfo method = assetModificationProcessorClass.GetMethod("OnWillCreateAsset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    object[] args = { path };
                    if (!CheckArguments(args, method))
                        continue;

                    method.Invoke(null, args);
                }
            }
        }

        // ReSharper disable once UnusedMember.Local - invoked from native code
        static void FileModeChanged(string[] assets, UnityEditor.VersionControl.FileMode mode)
        {
            if (!Provider.enabled)
                return;

            // if we happen to be disconnected or work offline, there's not much we can do;
            // just ignore the file mode and hope that VCS client/project is setup to handle
            // appropriate file types correctly
            if (!Provider.isActive)
                return;

            // we'll want to re-serialize these assets in different (text vs binary) mode;
            // make sure they are editable first
            AssetDatabase.MakeEditable(assets);
            Provider.SetFileMode(assets, mode);
        }

        // Postprocess on all assets once an automatic import has completed
        // ReSharper disable once UnusedMember.Local - invoked from native code
        static void OnWillSaveAssets(string[] assets, out string[] assetsThatShouldBeSaved, out string[] assetsThatShouldBeReverted, bool explicitlySaveAsset)
        {
            assetsThatShouldBeReverted = new string[0];
            assetsThatShouldBeSaved = assets;

            bool showSaveDialog = assets.Length > 0 && EditorPrefs.GetBool("VerifySavingAssets", false) && InternalEditorUtility.isHumanControllingUs;

            // If we are only saving a single scene or prefab and the user explicitly said we should, skip the dialog. We don't need
            // to verify this twice.
            if (explicitlySaveAsset && assets.Length == 1 && (assets[0].EndsWith(".unity") || assets[0].EndsWith(".prefab")))
                showSaveDialog = false;

            if (showSaveDialog)
                AssetSaveDialog.ShowWindow(assets, out assetsThatShouldBeSaved);
            else
                assetsThatShouldBeSaved = assets;

            foreach (var assetModificationProcessorClass in AssetModificationProcessors)
            {
                MethodInfo method = assetModificationProcessorClass.GetMethod("OnWillSaveAssets", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    object[] args = { assetsThatShouldBeSaved };
                    if (!CheckArguments(args, method))
                        continue;

                    string[] result = (string[])method.Invoke(null, args);

                    if (result != null)
                        assetsThatShouldBeSaved = result;
                }
            }

            if (assetsThatShouldBeSaved == null)
            {
                return;
            }

            var assetsNotOpened = new List<string>();
            AssetDatabase.IsOpenForEdit(assetsThatShouldBeSaved, assetsNotOpened, StatusQueryOptions.ForceUpdate);
            assets = assetsNotOpened.ToArray();

            // Try to checkout if needed
            var notEditableAssets = new List<string>();
            if (assets.Length != 0 && !AssetDatabase.MakeEditable(assets, null, notEditableAssets))
            {
                // only save assets that can be made editable (not locked by someone else, etc.),
                // unless we are in the behavior mode that just overwrites everything anyway
                if (!EditorUserSettings.overwriteFailedCheckoutAssets)
                {
                    assetsThatShouldBeReverted = notEditableAssets.ToArray();
                    assetsThatShouldBeSaved = assetsThatShouldBeSaved.Except(assetsThatShouldBeReverted).ToArray();
                }
            }
        }

        static AssetMoveResult OnWillMoveAsset(string fromPath, string toPath, string[] newPaths, string[] NewMetaPaths)
        {
            AssetMoveResult finalResult = AssetMoveResult.DidNotMove;
            finalResult = AssetModificationHook.OnWillMoveAsset(fromPath, toPath);

            foreach (var assetModificationProcessorClass in AssetModificationProcessors)
            {
                MethodInfo method = assetModificationProcessorClass.GetMethod("OnWillMoveAsset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    object[] args = { fromPath, toPath };
                    if (!CheckArgumentsAndReturnType(args, method, finalResult.GetType()))
                        continue;

                    finalResult |= (AssetMoveResult)method.Invoke(null, args);
                }
            }

            return finalResult;
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            AssetDeleteResult finalResult = AssetDeleteResult.DidNotDelete;

            foreach (var assetModificationProcessorClass in AssetModificationProcessors)
            {
                MethodInfo method = assetModificationProcessorClass.GetMethod("OnWillDeleteAsset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    object[] args = { assetPath, options };
                    if (!CheckArgumentsAndReturnType(args, method, finalResult.GetType()))
                        continue;

                    finalResult |= (AssetDeleteResult)method.Invoke(null, args);
                }
            }

            if (finalResult != AssetDeleteResult.DidNotDelete)
                return finalResult;

            finalResult = AssetModificationHook.OnWillDeleteAsset(assetPath, options);

            return finalResult;
        }

        static void OnWillDeleteAssets(string[] assetPaths, AssetDeleteResult[] outPathDeletionResults, RemoveAssetOptions options)
        {
            for (int i = 0; i < outPathDeletionResults.Length; i++)
                outPathDeletionResults[i] = (int)AssetDeleteResult.DidNotDelete;

            List<string> nonDeletedPaths    = new List<string>();
            List<int> nonDeletedPathIndices = new List<int>();
            foreach (var assetModificationProcessorClass in AssetModificationProcessors)
            {
                MethodInfo method = assetModificationProcessorClass.GetMethod("OnWillDeleteAsset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method == null)
                    continue;

                for (int i = 0; i < assetPaths.Length; i++)
                {
                    object[] args = { assetPaths[i], options };
                    if (!CheckArgumentsAndReturnType(args, method, typeof(AssetDeleteResult)))
                        continue;

                    AssetDeleteResult callbackResult = (AssetDeleteResult)method.Invoke(null, args);
                    outPathDeletionResults[i] |= callbackResult;
                }
            }

            for (int i = 0; i < assetPaths.Length; i++)
            {
                if (outPathDeletionResults[i] == (int)AssetDeleteResult.DidNotDelete)
                {
                    nonDeletedPaths.Add(assetPaths[i]);
                    nonDeletedPathIndices.Add(i);
                }
            }

            if (nonDeletedPaths.Count > 0)
            {
                if (!Provider.enabled || EditorUserSettings.WorkOffline)
                    return;

                for (int i = 0; i < nonDeletedPaths.Count; i++)
                {
                    if (!Provider.PathIsVersioned(nonDeletedPaths[i]))
                    {
                        nonDeletedPaths.RemoveAt(i);
                        nonDeletedPathIndices.RemoveAt(i);
                        i--;
                    }
                }

                var nonDeletedPathDeletionResults = new AssetDeleteResult[nonDeletedPaths.Count];

                AssetModificationHook.OnWillDeleteAssets(nonDeletedPaths.ToArray(), nonDeletedPathDeletionResults, options);

                for (int i = 0; i < nonDeletedPathIndices.Count; i++)
                {
                    outPathDeletionResults[nonDeletedPathIndices[i]] = nonDeletedPathDeletionResults[i];
                }
            }
        }

        static MethodInfo[] isOpenForEditMethods = null;
        static MethodInfo[] GetIsOpenForEditMethods()
        {
            if (isOpenForEditMethods == null)
            {
                List<MethodInfo> mArray = new List<MethodInfo>();
                foreach (var assetModificationProcessorClass in AssetModificationProcessors)
                {
                    MethodInfo method = assetModificationProcessorClass.GetMethod("IsOpenForEdit", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (method != null)
                    {
                        string dummy = "";
                        bool bool_dummy = false;
                        Type[] types = { dummy.GetType(), dummy.GetType().MakeByRefType() };
                        if (!CheckArgumentTypesAndReturnType(types, method, bool_dummy.GetType()))
                            continue;

                        mArray.Add(method);
                    }
                }

                isOpenForEditMethods = mArray.ToArray();
            }

            return isOpenForEditMethods;
        }

        enum Editability
        {
            Never,
            Always,
            Maybe
        }

        static Editability GetPathEditability(string assetPath)
        {
            // read-only asset locations (e.g. shared packages) are considered not editable
            bool rootFolder, readOnly;
            bool validPath = AssetDatabase.GetAssetFolderInfo(assetPath, out rootFolder, out readOnly);
            if (validPath && readOnly)
                return Editability.Never;

            // other paths that are not know to asset database, and not versioned, are considered always editable
            if (!Provider.PathIsVersioned(assetPath))
                return Editability.Always;

            return Editability.Maybe;
        }

        static bool IsOpenForEditViaScriptCallbacks(string assetPath, ref string message)
        {
            foreach (var method in GetIsOpenForEditMethods())
            {
                object[] args = {assetPath, message};
                if (!(bool)method.Invoke(null, args))
                {
                    message = args[1] as string;
                    return false;
                }
            }
            return true;
        }

        internal static bool IsOpenForEdit(string assetPath, out string message, StatusQueryOptions statusOptions)
        {
            message = string.Empty;
            if (string.IsNullOrEmpty(assetPath))
                return true; // treat empty/null paths as editable (might be under Library folders etc.)

            var editability = GetPathEditability(assetPath);
            if (editability == Editability.Always)
                return true;
            if (editability == Editability.Never)
                return false;
            if (!AssetModificationHook.IsOpenForEdit(assetPath, out message, statusOptions))
                return false;
            if (!IsOpenForEditViaScriptCallbacks(assetPath, ref message))
                return false;

            return true;
        }

        internal static void IsOpenForEdit(string[] assetOrMetaFilePaths, List<string> outNotEditablePaths, StatusQueryOptions statusQueryOptions = StatusQueryOptions.UseCachedIfPossible)
        {
            outNotEditablePaths.Clear();
            if (assetOrMetaFilePaths == null || assetOrMetaFilePaths.Length == 0)
                return;

            var queryList = new List<string>();
            foreach (var path in assetOrMetaFilePaths)
            {
                if (string.IsNullOrEmpty(path))
                    continue; // treat empty/null paths as editable (might be under Library folders etc.)
                var editability = GetPathEditability(path);
                if (editability == Editability.Always)
                    continue;
                if (editability == Editability.Never)
                {
                    outNotEditablePaths.Add(path);
                    continue;
                }
                queryList.Add(path);
            }

            // check with VCS
            AssetModificationHook.IsOpenForEdit(queryList, outNotEditablePaths, statusQueryOptions);

            // check with possible script callbacks
            var scriptCallbacks = GetIsOpenForEditMethods();
            if (scriptCallbacks != null && scriptCallbacks.Length > 0)
            {
                var stillEditable = assetOrMetaFilePaths.Except(outNotEditablePaths).Where(f => !string.IsNullOrEmpty(f));
                var message = string.Empty;
                foreach (var path in stillEditable)
                {
                    if (!IsOpenForEditViaScriptCallbacks(path, ref message))
                        outNotEditablePaths.Add(path);
                }
            }
        }

        internal static void OnStatusUpdated()
        {
            WindowPending.OnStatusUpdated();

            foreach (var assetModificationProcessorClass in AssetModificationProcessors)
            {
                MethodInfo method = assetModificationProcessorClass.GetMethod("OnStatusUpdated", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    object[] args = {};
                    if (!CheckArgumentsAndReturnType(args, method, typeof(void)))
                        continue;

                    method.Invoke(null, args);
                }
            }
        }
    }
}
