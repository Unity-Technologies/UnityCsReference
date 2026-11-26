// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor
{
    internal static partial class WindowIdentityResolver
    {
        internal const string k_FirstPartyPrefix = "unity:";
        internal const string k_PackagePrefix = "pkg:";
        internal const string k_ThirdPartyId = "thirdParty";
        const string k_UnityPackageNamePrefix = "com.unity.";

        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<Type, string> s_Ids = new Dictionary<Type, string>();

        [NoAutoStaticsCleanup]
        static readonly string k_EditorContentsPath = Path.GetFullPath(EditorApplication.applicationContentsPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        internal static string Resolve(Type windowType)
        {
            if (s_Ids.TryGetValue(windowType, out var id))
                return id;

            id = Compute(windowType);
            s_Ids[windowType] = id;
            return id;
        }

        // Ids must never contain a window title or file path.
        static string Compute(Type type)
        {
            var assembly = type.Assembly;
            if (assembly.IsDynamic)
                return k_ThirdPartyId;

            // A closed generic's FullName spells out its arguments with assembly-qualified names.
            var name = type.IsGenericType ? type.GetGenericTypeDefinition().FullName : type.FullName;

            if (IsFirstPartyAssembly(assembly))
                return k_FirstPartyPrefix + name;

            var package = FindUnityPackage(assembly);
            if (package != null)
                return k_PackagePrefix + package.name + "/" + name;

            return k_ThirdPartyId;
        }

        static bool IsFirstPartyAssembly(Assembly assembly)
        {
            return AssemblyHelper.IsUnityEngineModule(assembly) || IsShippedWithEditor(assembly);
        }

        static bool IsShippedWithEditor(Assembly assembly)
        {
            var location = assembly.GetLoadedAssemblyPath();
            if (string.IsNullOrEmpty(location))
                return false;

            return Path.GetFullPath(location).StartsWith(k_EditorContentsPath, StringComparison.OrdinalIgnoreCase);
        }

        static PackageManager.PackageInfo FindUnityPackage(Assembly assembly)
        {
            var package = PackageManager.PackageInfo.FindForAssembly(assembly);
            if (package == null || !package.name.StartsWith(k_UnityPackageNamePrefix, StringComparison.Ordinal))
                return null;

            return IsShippedByUnity(package) ? package : null;
        }

        // Any registry can publish a com.unity.* name, so only Unity's own registry is trusted.
        static bool IsShippedByUnity(PackageManager.PackageInfo package)
        {
            if (package.source == PackageManager.PackageSource.BuiltIn)
                return true;

            return package.source == PackageManager.PackageSource.Registry
                && package.registry != null && package.registry.isUnityRegistry;
        }
    }
}
