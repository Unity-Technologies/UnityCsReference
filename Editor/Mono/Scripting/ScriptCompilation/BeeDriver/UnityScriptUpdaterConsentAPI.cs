// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using NiceIO;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditorInternal.APIUpdating;

namespace UnityEditor.ScriptUpdater
{
    class UnityScriptUpdaterConsentAPI
    {
        public UnitySourceFileUpdatersResultHandler.ScriptUpdaterConsentType AskFor(NPath[] filesToOverWrite)
        {
            APIUpdaterManager.numberOfTimesAsked = APIUpdaterManager.numberOfTimesAsked + 1;

            if (APIUpdaterManager.isInProjectCreation)
            {
                Console.WriteLine("Skipping ScriptUpdater consent dialog because we are in project creation mode.");
                return UnitySourceFileUpdatersResultHandler.ScriptUpdaterConsentType.ConsentForRestOfCompilation;
            }

            if (APIUpdaterManager.DoesCommandLineIndicateAPIUpdatingShouldBeDeclined())
            {
                Console.WriteLine(
                    "Skipping ScriptUpdater consent dialog. Consent declined based on command line parameters.");
                return UnitySourceFileUpdatersResultHandler.ScriptUpdaterConsentType.NoConsent;
            }

            if (APIUpdaterManager.DoesCommandLineIndicateAPIUpdatingShouldHappenWithoutConsent())
            {
                Console.WriteLine(
                    "Skipping ScriptUpdater consent dialog. Consent given based on command line parameters.");
                return UnitySourceFileUpdatersResultHandler.ScriptUpdaterConsentType.ConsentForRestOfCompilation;
            }

            return AskThroughDialog(filesToOverWrite);
        }

        private static UnitySourceFileUpdatersResultHandler.ScriptUpdaterConsentType AskThroughDialog(NPath[] filesToOverWrite)
        {
            var selection = filesToOverWrite.Take(30).ToArray();
            var displayedFiles = selection.Select(f => f.ToString()).SeparateWith(Environment.NewLine);
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
                    return UnitySourceFileUpdatersResultHandler.ScriptUpdaterConsentType.ConsentForRestOfCompilation;
                case 1:
                    return UnitySourceFileUpdatersResultHandler.ScriptUpdaterConsentType.NoConsent;
                case 2:
                    return UnitySourceFileUpdatersResultHandler.ScriptUpdaterConsentType.ConsentOnce;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
