// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.IO;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.RestService
{
    internal class PairingRestHandler : JSONHandler
    {
        protected override JSONValue HandlePost(Request request, JSONValue payload)
        {
            ScriptEditorSettings.ServerURL = payload["url"].AsString();
            ScriptEditorSettings.Name = payload.ContainsKey("name") ? payload["name"].AsString() : null;
            ScriptEditorSettings.ProcessId = payload.ContainsKey("processid") ? (int)payload["processid"].AsFloat() : -1;

            Logger.Log("[Pair] Name: " + (ScriptEditorSettings.Name ?? "<null>") +
                " ServerURL " + ScriptEditorSettings.ServerURL +
                " Process id: " + ScriptEditorSettings.ProcessId);

            var result = new JSONValue();
            result["unityprocessid"] = Process.GetCurrentProcess().Id;
            result["unityproject"] = Application.dataPath;
            return result;
        }

        internal static void Register()
        {
            Router.RegisterHandler("/unity/pair", new PairingRestHandler());
        }

        [OnOpenAsset]
        static bool OnOpenAsset(int instanceID, int line)
        {
            if (ScriptEditorSettings.ServerURL == null)
                return false;

            var assetpath = Path.GetFullPath(Application.dataPath + "/../" + AssetDatabase.GetAssetPath(instanceID)).Replace('\\', '/');
            var lowerAssetPath = assetpath.ToLower();

            if (!lowerAssetPath.EndsWith(".cs") && !lowerAssetPath.EndsWith(".js") && !lowerAssetPath.EndsWith(".boo"))
                return false;

            if (!IsScriptEditorRunning() || !RestRequest.Send("/openfile", "{ \"file\" : \"" + assetpath + "\", \"line\" : " + line + " }", 5000))
            {
                ScriptEditorSettings.ServerURL = null;
                ScriptEditorSettings.Name = null;
                ScriptEditorSettings.ProcessId = -1;

                return false;
            }

            return true;
        }

        static bool IsScriptEditorRunning()
        {
            if (ScriptEditorSettings.ProcessId < 0)
                return false;

            try
            {
                var process = Process.GetProcessById(ScriptEditorSettings.ProcessId);
                return !process.HasExited;
            }
            catch (Exception e)
            {
                Logger.Log(e);
                return false;
            }
        }
    }
}
