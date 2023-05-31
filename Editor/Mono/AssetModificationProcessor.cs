// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEditorInternal.VersionControl;
using System.Linq;
using System.Reflection;
using UnityEditor.Profiling;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor
{
    [MovedFrom("")]
    public class AssetModificationProcessor
    {
    }

    internal class AssetModificationProcessorInternal
    {
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
                    assetModificationProcessors = processors.ToArray();
                }
                return assetModificationProcessors;
            }
        }
#pragma warning restore 0618

        [RequiredByNativeCode]
        internal static void OnWillCreateAsset(string path)
        {
            foreach (var assetModificationProcessorClass in AssetModificationProcessors)
            {
                const string methodName = "OnWillCreateAsset";
                MethodInfo method = assetModificationProcessorClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    object[] args = { path };
                    if (!CheckArguments(args, method))
                        continue;

                    using (new EditorPerformanceMarker($"{assetModificationProcessorClass.Name}.{methodName}", assetModificationProcessorClass).Auto())
                        method.Invoke(null, args);
                }
            }
        }

        // ReSharper disable once UnusedMember.Local - invoked from native code
        [RequiredByNativeCode]
        static void FileModeChanged(string[] assets, FileMode mode)
        {
            AssetModificationHook.FileModeChanged(assets, mode);

            object[] args = { assets, mode };
            foreach (var type in AssetModificationProcessors)
            {
                const string methodName = "FileModeChanged";
                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method == null)
                    continue;
                if (!CheckArgumentsAndReturnType(args, method, typeof(void)))
                    continue;
                using (new EditorPerformanceMarker($"{type.Name}.{methodName}", type).Auto())
                    method.Invoke(null, args);
            }
        }

        // Postprocess on all assets once an automatic import has completed
        // ReSharper disable once UnusedMember.Local - invoked from native code
        [RequiredByNativeCode]
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
                const string methodName = "OnWillSaveAssets";
                MethodInfo method = assetModificationProcessorClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    object[] args = { assetsThatShouldBeSaved };
                    if (!CheckArguments(args, method))
                        continue;

                    string[] result;
                    using (new EditorPerformanceMarker($"{assetModificationProcessorClass.Name}.{methodName}", assetModificationProcessorClass).Auto())
                        result = (string[])method.Invoke(null, args);

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

        [RequiredByNativeCode]
        static AssetMoveResult OnWillMoveAsset(string fromPath, string toPath, string[] newPaths, string[] NewMetaPaths)
        {
            AssetMoveResult finalResult = AssetMoveResult.DidNotMove;
            finalResult = AssetModificationHook.OnWillMoveAsset(fromPath, toPath);

            foreach (var assetModificationProcessorClass in AssetModificationProcessors)
            {
                const string methodName = "OnWillMoveAsset";
                MethodInfo method = assetModificationProcessorClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    object[] args = { fromPath, toPath };
                    if (!CheckArgumentsAndReturnType(args, method, finalResult.GetType()))
                        continue;

                    using (new EditorPerformanceMarker($"{assetModificationProcessorClass.Name}.{methodName}", assetModificationProcessorClass).Auto())
                        finalResult |= (AssetMoveResult)method.Invoke(null, args);
                }
            }

            return finalResult;
        }

        [RequiredByNativeCode]
        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            AssetDeleteResult finalResult = AssetDeleteResult.DidNotDelete;

            foreach (var assetModificationProcessorClass in AssetModificationProcessors)
            {
                const string methodName = "OnWillDeleteAsset";
                MethodInfo method = assetModificationProcessorClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    object[] args = { assetPath, options };
                    if (!CheckArgumentsAndReturnType(args, method, finalResult.GetType()))
                        continue;

                    using (new EditorPerformanceMarker($"{assetModificationProcessorClass.Name}.{methodName}", assetModificationProcessorClass).Auto())
                        finalResult |= (AssetDeleteResult)method.Invoke(null, args);
                }
            }

            if (finalResult != AssetDeleteResult.DidNotDelete)
                return finalResult;

            finalResult = AssetModificationHook.OnWillDeleteAsset(assetPath, options);

            return finalResult;
        }

        [RequiredByNativeCode]
        static void OnWillDeleteAssets(string[] assetPaths, AssetDeleteResult[] outPathDeletionResults, RemoveAssetOptions options)
        {
            for (int i = 0; i < outPathDeletionResults.Length; i++)
                outPathDeletionResults[i] = (int)AssetDeleteResult.DidNotDelete;

            List<string> nonDeletedPaths    = new List<string>();
            List<int> nonDeletedPathIndices = new List<int>();
            foreach (var assetModificationProcessorClass in AssetModificationProcessors)
            {
                const string methodName = "OnWillDeleteAsset";
                MethodInfo method = assetModificationProcessorClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method == null)
                    continue;

                for (int i = 0; i < assetPaths.Length; i++)
                {
                    object[] args = { assetPaths[i], options };
                    if (!CheckArgumentsAndReturnType(args, method, typeof(AssetDeleteResult)))
                        continue;


                    using (new EditorPerformanceMarker($"{assetModificationProcessorClass.Name}.{methodName}", assetModificationProcessorClass).Auto())
                    {
                        AssetDeleteResult callbackResult = (AssetDeleteResult)method.Invoke(null, args);
                        outPathDeletionResults[i] |= callbackResult;
                    }
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

        static MethodInfo[] s_CanOpenForEditMethods;
        static MethodInfo[] s_LegacyCanOpenForEditMethods;
        static MethodInfo[] s_IsOpenForEditMethods;
        static MethodInfo[] s_LegacyIsOpenForEditMethods;

        static void GetOpenForEditMethods(bool canOpenForEditVariant, out MethodInfo[] methods, out MethodInfo[] legacyMethods)
        {
            if (canOpenForEditVariant)
            {
                if (s_CanOpenForEditMethods == null)
                    GetOpenForEditMethods("CanOpenForEdit", out s_CanOpenForEditMethods, out s_LegacyCanOpenForEditMethods);
                methods = s_CanOpenForEditMethods;
                legacyMethods = s_LegacyCanOpenForEditMethods;
            }
            else
            {
                if (s_IsOpenForEditMethods == null)
                    GetOpenForEditMethods("IsOpenForEdit", out s_IsOpenForEditMethods, out s_LegacyIsOpenForEditMethods);
                methods = s_IsOpenForEditMethods;
                legacyMethods = s_LegacyIsOpenForEditMethods;
            }
        }

        static void GetOpenForEditMethods(string name, out MethodInfo[] methods, out MethodInfo[] legacyMethods)
        {
            var methodList = new List<MethodInfo>();
            var legacyMethodList = new List<MethodInfo>();

            Type[] types = { typeof(string[]), typeof(List<string>), typeof(StatusQueryOptions) };
            Type[] legacyTypes = { typeof(string), typeof(string).MakeByRefType() };

            foreach (var type in AssetModificationProcessors)
            {
                // First look for a method with a signature that accepts multiple paths "bool (string[] assetOrMetaFilePaths, List<string> outNotEditablePaths, StatusQueryOptions statusQueryOptions)".
                MethodInfo method;
                try
                {
                    method = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
                }
                catch (AmbiguousMatchException)
                {
                    Debug.LogWarning($"Ambiguous {name} methods in {type.Name}.");
                    continue;
                }
                if (method?.ReturnType == typeof(bool))
                {
                    methodList.Add(method);
                    continue;
                }

                // Then look for a legacy method that accepts single path only "bool (string assetOrMetaFilePath, out string message)".
                MethodInfo legacyMethod;
                try
                {
                    legacyMethod = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                }
                catch (AmbiguousMatchException)
                {
                    Debug.LogWarning($"Ambiguous {name} methods in {type.Name}.");
                    continue;
                }
                if (legacyMethod != null && CheckArgumentTypesAndReturnType(legacyTypes, legacyMethod, typeof(bool)))
                    legacyMethodList.Add(legacyMethod);
            }

            methods = methodList.ToArray();
            legacyMethods = legacyMethodList.ToArray();
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
            bool validPath = AssetDatabase.TryGetAssetFolderInfo(assetPath, out rootFolder, out readOnly);
            if (validPath && readOnly)
                return Editability.Never;

            // other paths that are not know to asset database, and not versioned, are considered always editable
            if (!VersionControlUtils.IsPathVersioned(assetPath))
                return Editability.Always;

            return Editability.Maybe;
        }

        static bool GetOpenForEditViaScriptCallbacks(bool canOpenForEditVariant, string[] paths, List<string> notEditablePaths, out string message, StatusQueryOptions options)
        {
            message = string.Empty;
            GetOpenForEditMethods(canOpenForEditVariant, out var methods, out var legacyMethods);

            if (methods.Length != 0)
            {
                object[] args = { paths, notEditablePaths, options };
                foreach (var method in methods)
                {
                    if (!(bool)method.Invoke(null, args))
                        return false;
                }
            }

            if (legacyMethods.Length != 0)
            {
                foreach (var legacyMethod in legacyMethods)
                {
                    var result = true;
                    foreach (var path in paths)
                    {
                        object[] legacyArgs = { path, null };
                        if (!(bool)legacyMethod.Invoke(null, legacyArgs))
                        {
                            result = false;
                            notEditablePaths.Add(path);
                            message = legacyArgs[1] as string;
                        }
                    }

                    if (!result)
                        return false;
                }
            }

            return true;
        }

        internal static bool CanOpenForEdit(string assetPath, out string message, StatusQueryOptions statusOptions)
        {
            if (IsOpenForEdit(assetPath, out message, statusOptions))
                return true;

            // Status has just been updated so there's no need to force update again.
            if (statusOptions == StatusQueryOptions.ForceUpdate)
                statusOptions = StatusQueryOptions.UseCachedIfPossible;

            return GetOpenForEdit(true, assetPath, out message, statusOptions);
        }

        internal static bool IsOpenForEdit(string assetPath, out string message, StatusQueryOptions statusOptions)
        {
            return GetOpenForEdit(false, assetPath, out message, statusOptions);
        }

        static bool GetOpenForEdit(bool canOpenForEditVariant, string assetPath, out string message, StatusQueryOptions statusOptions)
        {
            message = string.Empty;
            if (string.IsNullOrEmpty(assetPath))
                return true; // treat empty/null paths as editable (might be under Library folders etc.)

            var editability = GetPathEditability(assetPath);
            if (editability == Editability.Always)
                return true;
            if (editability == Editability.Never)
                return false;

            if (!AssetModificationHook.GetOpenForEdit(canOpenForEditVariant, assetPath, out message, statusOptions))
                return false;

            return GetOpenForEditViaScriptCallbacks(canOpenForEditVariant, new[] { assetPath }, new List<string>(), out message, statusOptions);
        }

        internal static bool CanOpenForEdit(string[] assetOrMetaFilePaths, List<string> outNotEditablePaths, StatusQueryOptions statusQueryOptions)
        {
            outNotEditablePaths.Clear();
            if (assetOrMetaFilePaths == null || assetOrMetaFilePaths.Length == 0)
                return true;

            var queryList = GetQueryList(assetOrMetaFilePaths, outNotEditablePaths);
            if (queryList.Count == 0)
                return outNotEditablePaths.Count == 0;

            // Get a list of paths that are not open for edit.
            var notOpenForEditPaths = new List<string>();
            AssetModificationHook.GetOpenForEdit(false, queryList, notOpenForEditPaths, statusQueryOptions);
            GetOpenForEditViaScriptCallbacks(false, queryList.ToArray(), notOpenForEditPaths, out var message, statusQueryOptions);

            if (notOpenForEditPaths.Count == 0)
                return outNotEditablePaths.Count == 0;

            // Status has just been updated so there's no need to force update again.
            if (statusQueryOptions == StatusQueryOptions.ForceUpdate)
                statusQueryOptions = StatusQueryOptions.UseCachedIfPossible;

            // Check paths that are not open for edit.
            if (!AssetModificationHook.GetOpenForEdit(true, notOpenForEditPaths, outNotEditablePaths, statusQueryOptions))
                return false;

            return GetOpenForEditViaScriptCallbacks(true, notOpenForEditPaths.ToArray(), outNotEditablePaths, out message, statusQueryOptions);
        }

        internal static bool IsOpenForEdit(string[] assetOrMetaFilePaths, List<string> outNotEditablePaths, StatusQueryOptions statusQueryOptions)
        {
            outNotEditablePaths.Clear();
            if (assetOrMetaFilePaths == null || assetOrMetaFilePaths.Length == 0)
                return true;

            var queryList = GetQueryList(assetOrMetaFilePaths, outNotEditablePaths);
            if (queryList.Count == 0)
                return outNotEditablePaths.Count == 0;

            // check with VCS
            if (!AssetModificationHook.GetOpenForEdit(false, queryList, outNotEditablePaths, statusQueryOptions))
                return false;

            // check with possible script callbacks
            return GetOpenForEditViaScriptCallbacks(false, queryList.ToArray(), outNotEditablePaths, out var message, statusQueryOptions);
        }

        static List<string> GetQueryList(string[] paths, List<string> outNotEditablePaths)
        {
            var result = new List<string>(paths.Length);
            foreach (var path in paths)
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
                result.Add(path);
            }
            return result;
        }

        [RequiredByNativeCode]
        internal static void OnStatusUpdated()
        {
            WindowPending.OnStatusUpdated();

            foreach (var assetModificationProcessorClass in AssetModificationProcessors)
            {
                const string methodName = "OnStatusUpdated";
                MethodInfo method = assetModificationProcessorClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    object[] args = {};
                    if (!CheckArgumentsAndReturnType(args, method, typeof(void)))
                        continue;

                    using (new EditorPerformanceMarker($"{assetModificationProcessorClass.Name}.{methodName}", assetModificationProcessorClass).Auto())
                        method.Invoke(null, args);
                }
            }
        }

        static MethodInfo[] s_MakeEditableMethods;

        static MethodInfo[] GetMakeEditableMethods()
        {
            if (s_MakeEditableMethods == null)
            {
                var methods = new List<MethodInfo>();
                Type[] types = { typeof(string[]), typeof(string), typeof(List<string>) };
                foreach (var type in AssetModificationProcessors)
                {
                    MethodInfo method;
                    try
                    {
                        method = type.GetMethod("MakeEditable", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                    catch (AmbiguousMatchException)
                    {
                        Debug.LogWarning($"Ambiguous MakeEditable methods in {type.Name}.");
                        continue;
                    }
                    if (method != null && CheckArgumentTypesAndReturnType(types, method, typeof(bool)))
                        methods.Add(method);
                }
                s_MakeEditableMethods = methods.ToArray();
            }
            return s_MakeEditableMethods;
        }

        internal static bool MakeEditable(string[] paths, string prompt, List<string> outNotEditablePaths)
        {
            var methods = GetMakeEditableMethods();
            if (methods == null || methods.Length == 0)
                return true;

            object[] args = { paths, prompt ?? string.Empty, outNotEditablePaths ?? new List<string>() };
            foreach (var method in methods)
            {
                if (!(bool)method.Invoke(null, args))
                    return false;
            }

            return true;
        }
    }
}
