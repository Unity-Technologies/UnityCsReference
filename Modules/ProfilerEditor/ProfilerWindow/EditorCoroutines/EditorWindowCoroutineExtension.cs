// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Unity.Profiling.Editor.UI
{
    internal static class EditorWindowCoroutineExtension
    {
        /// <summary>
        /// Start an <see cref="EditorCoroutine">EditorCoroutine</see>, owned by the calling <see cref="EditorWindow">EditorWindow</see> instance.
        /// <code>
        /// using System.Collections;
        /// using Unity.Profiling.Editor.UI;
        /// using UnityEditor;
        ///
        /// public class ExampleWindow : EditorWindow
        /// {
        ///     void OnEnable()
        ///     {
        ///         this.StartCoroutine(CloseWindowDelayed());
        ///     }
        ///
        ///     IEnumerator CloseWindowDelayed() //close the window after 1000 frames have elapsed
        ///     {
        ///         int count = 1000;
        ///         while (count > 0)
        ///         {
        ///             yield return null;
        ///         }
        ///         Close();
        ///     }
        /// }
        /// </code>
        /// </summary>
        /// <param name="routine"></param>
        /// <returns></returns>
        public static EditorCoroutine StartCoroutine(this EditorWindow window, IEnumerator routine)
        {
            return new EditorCoroutine(routine, window);
        }

        /// <summary>
        /// Immediately stop an <see cref="EditorCoroutine">EditorCoroutine</see> that was started by the calling <see cref="EditorWindow"/> instance. This method is safe to call on an already completed <see cref="EditorCoroutine">EditorCoroutine</see>.
        /// <code>
        /// using System.Collections;
        /// using Unity.Profiling.Editor.UI;
        /// using UnityEditor;
        /// using UnityEngine;
        ///
        /// public class ExampleWindow : EditorWindow
        /// {
        ///     EditorCoroutine coroutine;
        ///     void OnEnable()
        ///     {
        ///         coroutine = this.StartCoroutine(CloseWindowDelayed());
        ///     }
        ///
        ///     private void OnDisable()
        ///     {
        ///         this.StopCoroutine(coroutine);
        ///     }
        ///
        ///     IEnumerator CloseWindowDelayed()
        ///     {
        ///         while (true)
        ///         {
        ///             Debug.Log("Running");
        ///             yield return null;
        ///         }
        ///     }
        /// }
        /// </code>
        /// </summary>
        /// <param name="coroutine"></param>
        public static void StopCoroutine(this EditorWindow window, EditorCoroutine coroutine)
        {
            if(coroutine == null)
            {
                Debug.LogAssertion("Provided EditorCoroutine handle is null.");
                return;
            }

            if(coroutine.m_Owner == null)
            {
                Debug.LogError("The EditorCoroutine is ownerless. Please use EditorCoroutineEditor.StopCoroutine to terminate such coroutines.");
                return;
            }

            if (!coroutine.m_Owner.IsAlive)
                return; //The EditorCoroutine's owner was already terminated execution will cease next time it is processed

            var owner = coroutine.m_Owner.Target as EditorWindow;

            if (owner == null || owner != null && owner != window)
            {
                Debug.LogErrorFormat("The EditorCoroutine is owned by another object: {0}.", coroutine.m_Owner.Target);
                return;
            }

            EditorCoroutineUtility.StopCoroutine(coroutine);
        }
    }
}
