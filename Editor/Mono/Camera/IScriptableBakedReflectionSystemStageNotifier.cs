// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Experimental.Rendering
{
    public interface IScriptableBakedReflectionSystemStageNotifier
    {
        void EnterStage(int stage, string progressMessage, float progress);
        void ExitStage(int stage);

        void SetIsDone(bool isDone);
    }
}
