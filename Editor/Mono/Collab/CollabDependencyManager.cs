// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System.Collections.Generic;

namespace UnityEditor.Collaboration
{
    [InitializeOnLoad]
    internal class CollabDependencyManager
    {
        private static CollabDependencyManager s_Instance;

        public static CollabDependencyManager instance
        {
            get
            {
                return s_Instance;
            }
        }

        // Private so everyone uses the Singleton
        private CollabDependencyManager()
        {
        }

        static CollabDependencyManager()
        {
            s_Instance = new CollabDependencyManager();
        }

        // Summary: Given a list of files, determine which operations are valie
        // Param "assetPaths": The paths of the assets the user wants to query for which operations are valid
        // Returns: List of valid CollabOperations
        // Remarks: The output list will include a subset of either (Publish, Diff, (Exclude|Include), Revert) or
        //          (ChooseMine, ChooseTheirs, ExternalMerge) depending on whether Collab is in a conflict state
        //          If an operation is valid for any file in the list, it will be included in the output
        //          Exclude/Include are mutually exclusive operations. If any file can be excluded, Exclude will
        //          be in the output. If all files are excluded already, Include will be in the output list.
        public List<CollabOperation> GetValidOperations(List<string> assetPaths)
        {
            return new List<CollabOperation>
            {
                CollabOperation.Publish, CollabOperation.Diff, CollabOperation.Exclude,
                CollabOperation.Include, CollabOperation.Revert
            };
        }

        // Summary: Given an operation and a list of files, output the files that are valid for that operation.
        // Param "operation": The CollabOperation the user wishes to perform
        // Param "assetPaths": The paths of the assets on which the user desires to perform the operation
        // Param "validPaths": Output list of asset paths which are valid for the requested operation
        // Param "results": Output list of messages indicating which (if any) files need to be added to the
        //                  operation for it to succeed
        // Returns: true if operation can succeed with just the files in validPaths
        //          false if there are errors or warnings in the results list that require user intervention
        // Remarks: If a file needs additional files included in the commit in order to succeed, the file path
        //          is returned in the message param and the MissingPaths list contains the list of all missing paths
        public bool GetValidFilesForOperation(CollabOperation operation, List<string> assetPaths, out List<string> validPaths,
            out List<CollabValidationMessage> results)
        {
            validPaths = assetPaths;
            results = new List<CollabValidationMessage>();

            return true;
        }
    }


    internal class CollabValidationMessage
    {
        public enum Severity
        {
            Debug,
            Info,
            Warning,
            Error
        }

        public Severity severity { get; protected set; }
        public string message { get; protected set; }
        public List<string> missingPaths { get; protected set; }

        public CollabValidationMessage(Severity sev, string msg, List<string> pathsToAdd)
        {
            severity = sev;
            message = msg;
            missingPaths = pathsToAdd;
        }
    }
}
