// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    [FoundryAPI]
    internal class TemplateProviderSettings
    {
        const string SharedScopeName = "_Shared";
        Dictionary<string, Dictionary<string, string>> settingScopes = new Dictionary<string, Dictionary<string, string>>();

        public void Add(string name, string value, string scope = null)
        {
            // If no scope is specified, use the shared scope key
            string scopeName = scope ?? SharedScopeName;
            // Create the scopes map if needed
            if (!settingScopes.ContainsKey(scopeName))
                settingScopes.Add(scopeName, new Dictionary<string, string>());
            // Last setting wins, always override
            settingScopes[scopeName].Add(name, value);
        }

        public string Find(string name, string scope = null)
        {
            // Check the provided scope. If we fail to find the name there
            // or there was no provided scope then check the shared scope.
            string result = FindSetting(name, scope);
            if (result == null)
                result = FindSetting(name, SharedScopeName);
            return result;
        }

        string FindSetting(string name, string scope)
        {
            if (scope != null)
            {
                if (settingScopes.TryGetValue(scope, out var scopedSettings))
                {
                    scopedSettings.TryGetValue(name, out var value);
                    return value;
                }
            }
            return null;
        }
    }

    [FoundryAPI]
    internal interface ITemplateProvider
    {
        public abstract string Name { get; }
        public abstract IEnumerable<CustomizationPoint> GetCustomizationPoints();
        public abstract void ConfigureSettings(TemplateProviderSettings settings);
        public abstract IEnumerable<Template> GetTemplates(ShaderContainer container);
    }
}
