// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditorInternal.APIUpdating;

namespace UnityEditor.ScriptUpdater
{
    enum ScriptUpdaterConsentType
    {
        ConsentOnce,
        ConsentForRestOfCompilation,
        NoConsent,
    }

    class UnityScriptUpdaterConsentAPI
    {
        public ScriptUpdaterConsentType AskFor(string[] filesToOverWrite)
        {
            APIUpdaterManager.numberOfTimesAsked = APIUpdaterManager.numberOfTimesAsked + 1;

            if (APIUpdaterManager.isInProjectCreation)
            {
                Console.WriteLine("Skipping ScriptUpdater consent dialog because we are in project creation mode.");
                return ScriptUpdaterConsentType.ConsentForRestOfCompilation;
            }

            if (APIUpdaterManager.DoesCommandLineIndicateAPIUpdatingShouldBeDeclined())
            {
                Console.WriteLine(
                    "Skipping ScriptUpdater consent dialog. Consent declined based on command line parameters.");
                return ScriptUpdaterConsentType.NoConsent;
            }

            if (APIUpdaterManager.DoesCommandLineIndicateAPIUpdatingShouldHappenWithoutConsent())
            {
                Console.WriteLine(
                    "Skipping ScriptUpdater consent dialog. Consent given based on command line parameters.");
                return ScriptUpdaterConsentType.ConsentForRestOfCompilation;
            }

            return AskThroughDialog(filesToOverWrite);
        }

        private static ScriptUpdaterConsentType AskThroughDialog(string[] filesToOverWrite)
        {
#pragma warning disable UAC2001 // Avoid Linq
            var selection = filesToOverWrite.Take(30).ToArray();
#pragma warning restore UAC2001
            var displayedFiles = selection.SeparateWith(Environment.NewLine);
            var omitted = filesToOverWrite.Length - selection.Length;
            if (omitted > 0)
                displayedFiles = displayedFiles + $"\n<+{omitted} more files>";

            var caption =
                $"Some of this projects source files refer to API that has changed. These can be automatically updated. It is recommended to have a backup of the project before updating. Do you want these files to be updated?\n\n{displayedFiles}";
            var result = EditorUtility.DisplayDialogComplex("Script Updating Consent", caption,
                "Yes, for these and other files that might be found later", "No", "Yes, just for these files");
            switch (result)
            {
                case 0:
                    return ScriptUpdaterConsentType.ConsentForRestOfCompilation;
                case 1:
                    return ScriptUpdaterConsentType.NoConsent;
                case 2:
                    return ScriptUpdaterConsentType.ConsentOnce;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
