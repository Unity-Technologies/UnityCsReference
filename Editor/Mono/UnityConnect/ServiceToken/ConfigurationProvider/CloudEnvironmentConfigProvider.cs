// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Connect
{
    class CloudEnvironmentConfigProvider : ICloudEnvironmentConfigProvider
    {
        internal const string CloudEnvironmentArg = "-cloudEnvironment";
        internal const string StagingEnv = "staging";

        public bool IsStaging()
        {
            return GetCloudEnvironment(Environment.GetCommandLineArgs()) == StagingEnv;
        }

        internal string GetCloudEnvironment(string[] commandLineArgs)
        {
            string cloudEnvironmentField = null;

            foreach (var arg in commandLineArgs)
            {
                if (arg.StartsWith(CloudEnvironmentArg))
                {
                    cloudEnvironmentField = arg;
                    break;
                }
            }

            if (cloudEnvironmentField != null)
            {
                var cloudEnvironmentIndex = Array.IndexOf(commandLineArgs, cloudEnvironmentField);

                if (cloudEnvironmentField == CloudEnvironmentArg)
                {
                    if (cloudEnvironmentIndex <= commandLineArgs.Length - 2)
                    {
                        return commandLineArgs[cloudEnvironmentIndex + 1];
                    }
                }
                else if (cloudEnvironmentField.Contains('='))
                {
                    var value = cloudEnvironmentField.Substring(cloudEnvironmentField.IndexOf('=') + 1);
                    return !string.IsNullOrEmpty(value) ? value : null;
                }
            }

            return null;
        }
    }
}
