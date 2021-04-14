// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.UIElements
{
    // This class hooks up to the Package Manager to allow the UI Toolkit Package asset conversion to automatically trigger
    // when the UI Toolkit package is detected as removed from the project.
    internal class PackageManagerHookGUIDConverter
    {
        [InitializeOnLoadMethod]
        public static void RegisterPackagesEventHandler()
        {
            PackageManager.Events.registeredPackages += RegisteredPackagesEventHandler;
        }

        private static void RegisteredPackagesEventHandler(PackageManager.PackageRegistrationEventArgs packageRegistrationEventArgs)
        {
            // If this code is running, we're in a version of Unity that has Runtime assets so we can assume that if they
            // uninstall the UI Toolkit package, they may have these types of assets to be converted so we trigger a project
            // scan straight up (and users will decide if they go through with conversion or not because there's a
            // confirmation message shown to the user).
            foreach (var removedPackage in packageRegistrationEventArgs.removed)
            {
                if (removedPackage.packageId.StartsWith(@"com.unity.ui@")) // Adding the '@' in the end avoids doing this for the UI Builder package.
                {
                    GUIDConverter.StartConversion();
                    return;
                }
            }
        }
    }
}
