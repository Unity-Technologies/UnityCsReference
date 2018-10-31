// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Playables;

namespace UnityEditor.Playables
{
    public static class PlayableOutputEditorExtensions
    {
        public static string GetEditorName<U>(this U output) where U : struct, IPlayableOutput
        {
            return output.GetHandle().GetEditorName();
        }
    }
}
