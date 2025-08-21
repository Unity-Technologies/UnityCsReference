// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    struct StateRepositoryDelegates
    {
        // This simply represents the static methods of SessionState
        internal delegate string GetStringOrDefault(string key);
        internal delegate void Save(string key, string value);
        internal delegate void Clear(string key);

        internal GetStringOrDefault GetStringOrDefaultFunc;
        internal Save SaveFunc;
        internal Clear ClearFunc;
    }

    static class SessionStateRepository
    {
        public static StateRepositoryDelegates Get => new StateRepositoryDelegates
        {
            GetStringOrDefaultFunc = key => SessionState.GetString(key, string.Empty),
            SaveFunc = SessionState.SetString,
            ClearFunc = SessionState.EraseString,
        };
    }
}
