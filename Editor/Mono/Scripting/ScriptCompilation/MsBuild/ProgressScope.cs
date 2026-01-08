// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild;

class ProgressScope : IDisposable
{
    private readonly int _progressId;

    public ProgressScope(int progressId)
    {
        _progressId = progressId;
    }
    public void Dispose()
    {
        Progress.Finish(_progressId);
    }
}
