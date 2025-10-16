// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.PlayMode.Editor
{
    static class TaskExtensions
    {
        /// <summary>
        /// Observes the task to avoid the task fail silently.
        /// </summary>
        public static void Forget(this Task task)
        {
            if (!task.IsCompleted || task.IsFaulted)
            {
                _ = ForgetAwaited(task);
            }

            static async Task ForgetAwaited(Task task)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }
    }
}
