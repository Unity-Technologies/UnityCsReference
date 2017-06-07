// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using UnityEngineInternal;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine.Playables
{
    public struct ScriptPlayable<T> : IPlayable, IEquatable<ScriptPlayable<T>>
        where T : class, IPlayableBehaviour, new()
    {
        private PlayableHandle m_Handle;

        static readonly ScriptPlayable<T> m_NullPlayable = new ScriptPlayable<T>(PlayableHandle.Null);
        public static ScriptPlayable<T> Null { get { return m_NullPlayable; } }

        public static ScriptPlayable<T> Create(PlayableGraph graph, int inputCount = 0)
        {
            var handle = CreateHandle(graph, null, inputCount);
            return new ScriptPlayable<T>(handle);
        }

        public static ScriptPlayable<T> Create(PlayableGraph graph, T template, int inputCount = 0)
        {
            var handle = CreateHandle(graph, template, inputCount);
            return new ScriptPlayable<T>(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, T template, int inputCount)
        {
            object scriptInstance = null;

            if (template == null)
            {
                // We are creating a new script instance.
                scriptInstance = CreateScriptInstance();
            }
            else
            {
                // We are not creating from scratch, we are creating from a template.
                scriptInstance = CloneScriptInstance(template);
            }

            if (scriptInstance == null)
            {
                Debug.LogError("Could not create a ScriptPlayable of Type " + typeof(T).ToString());
                return PlayableHandle.Null;
            }

            PlayableHandle handle = graph.CreatePlayableHandle();
            if (!handle.IsValid())
                return PlayableHandle.Null;

            handle.SetInputCount(inputCount);

            // This line should be the last one because it eventually calls
            // IPlayableBehaviour.OnPlayableCreate() on scriptInstance.
            handle.SetScriptInstance(scriptInstance);

            return handle;
        }

        private static object CreateScriptInstance()
        {
            IPlayableBehaviour data = null;

            if (typeof(UnityEngine.ScriptableObject).IsAssignableFrom(typeof(T)))
                data = ScriptableObject.CreateInstance(typeof(T)) as T;
            else
                data = new T();

            return data;
        }

        private static object CloneScriptInstance(IPlayableBehaviour source)
        {
            UnityEngine.Object engineObject = source as UnityEngine.Object;
            if (engineObject != null)
                return CloneScriptInstanceFromEngineObject(engineObject);

            ICloneable cloneableObject = source as ICloneable;
            if (cloneableObject != null)
                return CloneScriptInstanceFromIClonable(cloneableObject);

            return null;
        }

        private static object CloneScriptInstanceFromEngineObject(UnityEngine.Object source)
        {
            var scriptPlayable = Object.Instantiate(source);
            if (scriptPlayable != null)
            {
                scriptPlayable.hideFlags |= HideFlags.DontSave;
            }
            return scriptPlayable;
        }

        private static object CloneScriptInstanceFromIClonable(ICloneable source)
        {
            return source.Clone();
        }

        internal ScriptPlayable(PlayableHandle handle)
        {
            if (handle.IsValid())
            {
                if (!typeof(T).IsAssignableFrom(handle.GetPlayableType()))
                    throw new InvalidCastException(
                        String.Format(
                            "Incompatible handle: Trying to assign a playable data of type `{0}` that is not compatible with the PlayableBehaviour of type `{1}`.",
                            handle.GetPlayableType(), typeof(T)));
            }

            m_Handle = handle;
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public T GetBehaviour()
        {
            return m_Handle.GetObject<T>();
        }

        public static implicit operator Playable(ScriptPlayable<T> playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator ScriptPlayable<T>(Playable playable)
        {
            return new ScriptPlayable<T>(playable.GetHandle());
        }

        public bool Equals(ScriptPlayable<T> other)
        {
            return GetHandle() == other.GetHandle();
        }
    }
}
