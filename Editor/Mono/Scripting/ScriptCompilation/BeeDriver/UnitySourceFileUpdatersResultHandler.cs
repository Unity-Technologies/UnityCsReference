// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Bee.BeeDriver;
using UnityEditor.ScriptUpdater;
using UnityEditorInternal;

namespace UnityEditor.Scripting.ScriptCompilation
{
    // Hands BeeDriver's script updater results to ScriptUpdatesHandler, on the main thread.
    class UnitySourceFileUpdatersResultHandler : SourceFileUpdatersResultHandler
    {
        readonly ScriptUpdatesHandler m_Handler = new ScriptUpdatesHandler();

        public UnitySourceFileUpdatersResultHandler() : base(captureSynchronizationContext: true)
        {
            if (!InternalEditorUtility.CurrentThreadIsMainThread())
                throw new Exception($"{nameof(UnitySourceFileUpdatersResultHandler)} can only be created on the main thread");
        }

        protected override bool ProcessUpdaterResults(SourceFileUpdaterBase.Update[] updates)
        {
            var scriptUpdates = new ScriptUpdate[updates.Length];
            for (int i = 0; i < updates.Length; ++i)
                scriptUpdates[i] = new ScriptUpdate(updates[i].originalFileWithError, updates[i].tempFileWithNewContents);

            return m_Handler.Apply(scriptUpdates);
        }
    }
}
