// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace UnityEditor.RestService
{
    internal class AssetRestHandler
    {
        internal class AssetHandler : JSONHandler
        {
            protected override JSONValue HandleDelete(Request request, JSONValue payload)
            {
                var assetPath = request.Url.Substring("/unity/".Length);
                var success = AssetDatabase.DeleteAsset(assetPath);

                if (!success)
                {
                    throw new RestRequestException
                    {
                        HttpStatusCode = HttpStatusCode.InternalServerError,
                        RestErrorString = "FailedDeletingAsset",
                        RestErrorDescription = "DeleteAsset() returned false"
                    };
                }
                return new JSONValue();
            }

            protected override JSONValue HandlePost(Request request, JSONValue payload)
            {
                var action = payload.Get("action").AsString();
                switch (action)
                {
                    case "move":
                        var oldFile = request.Url.Substring("/unity/".Length);
                        var newFile = payload.Get("newpath").AsString();
                        MoveAsset(oldFile, newFile);
                        break;
                    case "create":
                        var createPath = request.Url.Substring("/unity/".Length);
                        var contents = payload.Get("contents").AsString();
                        byte[] convertedBytes = Convert.FromBase64String(contents);
                        contents = Encoding.UTF8.GetString(convertedBytes);
                        CreateAsset(createPath, contents);
                        break;
                    default:
                        throw new RestRequestException {HttpStatusCode = HttpStatusCode.BadRequest, RestErrorString = "Uknown action: " + action};
                }
                return new JSONValue();
            }

            internal bool MoveAsset(string from, string to)
            {
                var result = AssetDatabase.MoveAsset(from, to);
                if (result.Length > 0)
                    throw new RestRequestException(HttpStatusCode.BadRequest, "MoveAsset failed with error: " + result);
                return result.Length == 0;
            }

            internal void CreateAsset(string assetPath, string contents)
            {
                var fullPath = Path.GetFullPath(assetPath);
                try
                {
                    using (StreamWriter writer = new StreamWriter(File.OpenWrite(fullPath)))
                    {
                        writer.Write(contents);
                        writer.Close();
                    }
                }
                catch (Exception e)
                {
                    throw new RestRequestException(HttpStatusCode.BadRequest, "FailedCreatingAsset", "Caught exception: " + e);
                }
            }

            protected override JSONValue HandleGet(Request request, JSONValue payload)
            {
                int splitIndex = request.Url.ToLowerInvariant().IndexOf("/assets/");
                string assetPath = request.Url.ToLowerInvariant().Substring(splitIndex + 1);
                return GetAssetText(assetPath);
            }

            internal JSONValue GetAssetText(string assetPath)
            {
                var theAsset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
                if (theAsset == null)
                    throw new RestRequestException(HttpStatusCode.BadRequest, "AssetNotFound");

                var result = new JSONValue();
                result["file"] = assetPath;
                result["contents"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(theAsset.ToString()));
                return result;
            }
        }

        internal class LibraryHandler : JSONHandler
        {
            //GET Requests, right now only "get all"
            protected override JSONValue HandleGet(Request request, JSONValue payload)
            {
                var result = new JSONValue();
                result["assets"] = ToJSON(AssetDatabase.FindAssets("", new[] {"Assets"}));
                return result;
            }
        }

        internal static void Register()
        {
            Router.RegisterHandler("/unity/assets", new LibraryHandler());
            Router.RegisterHandler("/unity/assets/*", new AssetHandler());
        }
    }
}
