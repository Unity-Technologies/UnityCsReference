// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEditorInternal.VersionControl;
using System.Reflection;

namespace UnityEditor
{
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
                Debug.LogWarning("Parameter count did not match. Expected: " + types.Length.ToString() + " Got: " + parameters.Length.ToString() + " in " + method.DeclaringType.ToString() + "." + method.Name);
                return false;
            }

            int i = 0;
            foreach (Type type in types)
            {
                ParameterInfo pInfo = parameters[i];
                if (type != pInfo.ParameterType)
                {
                    Debug.LogWarning("Parameter type mismatch at parameter " + i + ". Expected: " + type.ToString() + " Got: " + pInfo.ParameterType.ToString() + " in " + method.DeclaringType.ToString() + "." + method.Name);
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
                Debug.LogWarning("Return type mismatch. Expected: " + returnType.ToString() + " Got: " + method.ReturnType.ToString() + " in " + method.DeclaringType.ToString() + "." + method.Name);
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
                    processors.AddRange(EditorAssemblies.SubclassesOf(typeof(UnityEditor.AssetModificationProcessor)));
                    processors.AddRange(EditorAssemblies.SubclassesOf(typeof(global::AssetModificationProcessor)));
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

        static void FileModeChanged(string[] assets, UnityEditor.VersionControl.FileMode mode)
        {
            // Make sure that all assets are checked out in version control and
            // that we have the most recent status
            if (Provider.enabled)
            {
                if (Provider.PromptAndCheckoutIfNeeded(assets, ""))
                    Provider.SetFileMode(assets, mode);
            }
        }

        // Postprocess on all assets once an automatic import has completed
        static void OnWillSaveAssets(string[] assets, out string[] assetsThatShouldBeSaved, out string[] assetsThatShouldBeReverted, int explicitlySaveAsset)
        {
            assetsThatShouldBeReverted = new string[0];
            assetsThatShouldBeSaved = assets;

            bool showSaveDialog = assets.Length > 0 && EditorPrefs.GetBool("VerifySavingAssets", false) && InternalEditorUtility.isHumanControllingUs;

            // If we are only saving a single scene or prefab and the user explicitly said we should, skip the dialog. We don't need
            // to verify this twice.
            if (explicitlySaveAsset != 0 && assets.Length == 1 && (assets[0].EndsWith(".unity") || assets[0].EndsWith(".prefab")))
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
            foreach (string asset in assetsThatShouldBeSaved)
            {
                if (!AssetDatabase.IsOpenForEdit(asset, StatusQueryOptions.ForceUpdate))
                    assetsNotOpened.Add(asset);
            }
            assets = assetsNotOpened.ToArray();

            // Try to checkout if needed. This may fail but is catched below.
            if (assets.Length != 0 && !Provider.PromptAndCheckoutIfNeeded(assets, ""))
            {
                Debug.LogError("Could not check out the following files in version control before saving: " +
                    string.Join(", ", assets));
                assetsThatShouldBeSaved = new string[0];
                return;
            }
        }

        static void RequireTeamLicense()
        {
            if (!InternalEditorUtility.HasTeamLicense())
                throw new MethodAccessException("Requires team license");
        }

        static AssetMoveResult OnWillMoveAsset(string fromPath, string toPath, string[] newPaths, string[] NewMetaPaths)
        {
            AssetMoveResult finalResult = AssetMoveResult.DidNotMove;
            if (!InternalEditorUtility.HasTeamLicense())
                return finalResult;

            finalResult = AssetModificationHook.OnWillMoveAsset(fromPath, toPath);

            foreach (var assetModificationProcessorClass in AssetModificationProcessors)
            {
                MethodInfo method = assetModificationProcessorClass.GetMethod("OnWillMoveAsset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    RequireTeamLicense();

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
            if (!InternalEditorUtility.HasTeamLicense())
                return finalResult;

            foreach (var assetModificationProcessorClass in AssetModificationProcessors)
            {
                MethodInfo method = assetModificationProcessorClass.GetMethod("OnWillDeleteAsset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    RequireTeamLicense();

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

        internal static MethodInfo[] isOpenForEditMethods = null;
        internal static MethodInfo[] GetIsOpenForEditMethods()
        {
            if (isOpenForEditMethods == null)
            {
                List<MethodInfo> mArray = new List<MethodInfo>();
                foreach (var assetModificationProcessorClass in AssetModificationProcessors)
                {
                    MethodInfo method = assetModificationProcessorClass.GetMethod("IsOpenForEdit", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (method != null)
                    {
                        RequireTeamLicense();

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

        internal static bool IsOpenForEdit(string assetPath, out string message, StatusQueryOptions statusOptions)
        {
            message = "";
            if (String.IsNullOrEmpty(assetPath))
                return true; // assetPath can be empty in some cases where Unity is checking for stuff in Library folder

            if (AssetDatabase.IsPackagedAssetPath(assetPath))
            {
                return false;
            }

            bool finalResult = AssetModificationHook.IsOpenForEdit(assetPath, out message, statusOptions);

            foreach (var method in GetIsOpenForEditMethods())
            {
                object[] args = {assetPath, message};
                if (!((bool)method.Invoke(null, args)))
                {
                    message = args[1] as string;
                    return false;
                }
            }

            return finalResult;
        }

        internal static void OnStatusUpdated()
        {
            WindowPending.OnStatusUpdated();

            foreach (var assetModificationProcessorClass in AssetModificationProcessors)
            {
                MethodInfo method = assetModificationProcessorClass.GetMethod("OnStatusUpdated", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                {
                    RequireTeamLicense();

                    object[] args = {};
                    if (!CheckArgumentsAndReturnType(args, method, typeof(void)))
                        continue;

                    method.Invoke(null, args);
                }
            }
        }
    }
}
