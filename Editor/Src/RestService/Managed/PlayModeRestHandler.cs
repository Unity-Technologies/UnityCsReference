// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;

namespace UnityEditor.RestService
{
    internal class PlayModeRestHandler : JSONHandler
    {
        protected override JSONValue HandlePost(Request request, JSONValue payload)
        {
            var action = payload.Get("action").AsString();
            string oldState = CurrentState();
            switch (action)
            {
                case "play":
                    EditorApplication.isPlaying = true;
                    EditorApplication.isPaused = false;
                    break;
                case "pause":
                    EditorApplication.isPaused = true;
                    break;
                case "stop":
                    EditorApplication.isPlaying = false;
                    break;
                default:
                    throw new RestRequestException {HttpStatusCode = HttpStatusCode.BadRequest, RestErrorString = "Invalid action: " + action};
            }

            var result = new JSONValue();
            result["oldstate"] = oldState;
            result["newstate"] = CurrentState();
            return result;
        }

        protected override JSONValue HandleGet(Request request, JSONValue payload)
        {
            var result = new JSONValue();
            result["state"] = CurrentState();
            return result;
        }

        internal static void Register()
        {
            Router.RegisterHandler("/unity/playmode", new PlayModeRestHandler());
        }

        internal string CurrentState()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                return "stopped";

            return EditorApplication.isPaused ? "paused" : "playing";
        }
    }
}
