// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Connect
{
    internal static class AsyncUtils
    {
        /// <summary>
        /// Used to run an action on the main thread of Unity
        /// </summary>
        /// <returns>Awaitable task that indicates when the action is completed</returns>
        internal static Task RunNextActionOnMainThread(
            Action action,
            [CallerFilePath] string file = null,
            [CallerMemberName] string caller = null,
            [CallerLineNumber] int line = 0)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            EditorApplication.CallbackFunction callback = null;
            callback = () =>
            {
                EditorApplication.update -= callback;
                try
                {
                    action();
                    taskCompletionSource.SetResult(true);
                }
                catch (Exception e) when (caller != null && file != null && line != 0)
                {
                    taskCompletionSource.SetException(e);
                    throw new Exception($"Exception thrown from invocation made by '{file}'({line}) by {caller}", e);
                }
            };
            EditorApplication.update += callback;
            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Used to run a UnityWebRequest on the main thread of Unity
        /// </summary>
        /// <returns>Awaitable task that indicates when the web request is completed</returns>
        internal static Task RunUnityWebRequestOnMainThread(
            UnityWebRequest request,
            [CallerFilePath] string file = null,
            [CallerMemberName] string caller = null,
            [CallerLineNumber] int line = 0)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            EditorApplication.update += Callback;
            return taskCompletionSource.Task;

            void Callback()
            {
                EditorApplication.update -= Callback;
                try
                {
                    request.SendWebRequest().completed += RequestCompleted;
                }
                catch (Exception e) when (caller != null && file != null && line != 0)
                {
                    taskCompletionSource.SetException(e);
                    throw new Exception($"Exception thrown from invocation made by '{file}'({line}) by {caller}", e);
                }
            }

            void RequestCompleted(AsyncOperation _)
            {
                taskCompletionSource.SetResult(true);
            }
        }
    }
}
