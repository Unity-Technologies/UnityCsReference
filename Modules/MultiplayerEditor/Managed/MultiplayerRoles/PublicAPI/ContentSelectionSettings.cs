// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using InternalManager = UnityEditor.Multiplayer.Internal.EditorMultiplayerManager;
using System.Runtime.CompilerServices;
using Unity.Multiplayer.Internal;

namespace Unity.Multiplayer.Editor
{
    internal class ContentSelectionBuildPreprocessor: IPreprocessBuildWithContext
    {
        public static event Action<BuildCallbackContext> OnPreprocessBuildCallback = null;

        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildCallbackContext ctx)
        {
            OnPreprocessBuildCallback?.Invoke(ctx);
        }

    }

    [FilePath("ProjectSettings/Packages/com.unity.dedicated-server/ContentSelectionSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class ContentSelectionSettings : SyncedSingleton<ContentSelectionSettings>
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Events.registeredPackages += OnPackagesRegistered;
            if(!DedicatedServerMigrationUtility.ShouldEnableDedicatedServer())
            {
                return;
            }
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            CleanupRestrictedComponents();
        }

        private static void Reinitialize()
        {
            Events.registeredPackages -= OnPackagesRegistered;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Initialize();
        }

        public ContentSelectionSettings()
        {            
            EditorApplication.quitting -= SaveIfDirty;
            EditorApplication.quitting += SaveIfDirty;

            ContentSelectionBuildPreprocessor.OnPreprocessBuildCallback -= OnPreprocessBuild;
            ContentSelectionBuildPreprocessor.OnPreprocessBuildCallback += OnPreprocessBuild;
        }

        ~ContentSelectionSettings()
        {
            ContentSelectionBuildPreprocessor.OnPreprocessBuildCallback -= OnPreprocessBuild;
        }

        private static void OnPackagesRegistered(PackageRegistrationEventArgs args)
        {
            foreach (var package in args.added)
            {
                ProcessPackageAdded(package.name);
            }
            DedicatedServerMigrationUtility.Reinitialize();
            Reinitialize();
        }

        internal static void ProcessPackageAdded(string packageName)
        {
            if (packageName == "com.unity.dedicated-server")
                EditorMultiplayerRolesManager.EnableMultiplayerRoles = true;
        }
        
        public void OnPreprocessBuild(BuildCallbackContext ctx)
        {
            SaveToInternalMultiplayerManager();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // TODO: We have seen cases where the state is ExitingEditMode but isPlaying (or maybe isPlayingOrEnteringPlayMode) is still true.
            //       That needs to be investigated and fixed.
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                instance.SaveToInternalMultiplayerManager();
            }
        }

        private static void CleanupRestrictedComponents()
        {
            var customList = AutomaticSelection.GetCustomComponents();
            // Create a copy because the below loop will modify the dictionary and we can't do that while iterating Keys
            var types = new List<Type>(customList.Keys);
            var count = types.Count;
            var reasign = false;

            foreach (var type in types)
            {
                if (type == null || type.GetCustomAttribute<MultiplayerRoleRestrictedAttribute>() == null)
                    continue;

                customList.Remove(type);
                reasign = true;
            }

            if (reasign)
                AutomaticSelection.SetCustomComponents(customList);
        }

        [SerializeField] private bool m_EnableSafetyChecks = true;
        [SerializeField] private AutomaticSelectionOptions m_AutomaticSelectionOptions;

        public static bool EnableSafetyChecks
        {
            get => instance.m_EnableSafetyChecks;
            set => instance.m_EnableSafetyChecks = value;
        }

        public static ref AutomaticSelectionOptions AutomaticSelection => ref ContentSelectionSettings.instance.m_AutomaticSelectionOptions;

        internal void SaveIfDirty()
        {
            if (EditorUtility.IsDirty(this))
            {
                Save(true);
            }
        }

        private void SaveToInternalMultiplayerManager()
        {
            var strippingComponentsPerRole = new Dictionary<MultiplayerRole, HashSet<Type>>();
            var roleValues = Enum.GetValues(typeof(MultiplayerRole));
            foreach (MultiplayerRole role in roleValues)
                strippingComponentsPerRole[role] = new();

            foreach (var component in AutomaticSelection.CompleteComponentsList)
            {
                foreach (MultiplayerRole role in roleValues)
                {
                    if ((component.Value & (MultiplayerRoleFlags)(1 << (int)role)) == 0)
                        strippingComponentsPerRole[role].Add(component.Key);
                }
            }

            foreach (MultiplayerRole role in roleValues)
            {
                var roleTypes = strippingComponentsPerRole[role];
                var asArray = new Type[roleTypes.Count];
                roleTypes.CopyTo(asArray);
                InternalManager.SetStrippingTypesForRole(role, asArray);
            }
        }
    }
}
