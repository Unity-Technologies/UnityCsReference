// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditorInternal;

namespace UnityEditor.RestService
{
    internal class OpenDocumentsRestHandler : JSONHandler
    {
        protected override JSONValue HandlePost(Request request, JSONValue payload)
        {
            ScriptEditorSettings.OpenDocuments = payload.ContainsKey("documents") ?
                payload["documents"].AsList().Select(d => d.AsString()).ToList() :
                new List<string>();
            ScriptEditorSettings.Save();
            return new JSONValue();
        }

        protected override JSONValue HandleGet(Request request, JSONValue payload)
        {
            var result = new JSONValue();
            result["documents"] = ToJSON(ScriptEditorSettings.OpenDocuments);
            return result;
        }

        internal static void Register()
        {
            Router.RegisterHandler("/unity/opendocuments", new OpenDocumentsRestHandler());
        }
    }
}
