// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using UnityEngineInternal;

namespace UnityEngine.Playables
{
    public partial struct PlayableGraph
    {
        public int GetScriptOutputCount()
        {
            return InternalScriptOutputCount(ref this);
        }

        public ScriptPlayableOutput GetScriptOutput(int index)
        {
            ScriptPlayableOutput output = new ScriptPlayableOutput();
            if (!InternalGetScriptOutput(ref this, index, out output.m_Output))
                return ScriptPlayableOutput.Null;
            return output;
        }

        public PlayableHandle CreateScriptPlayable<T>() where T : class, IScriptPlayable, IPlayable, new()
        {
            PlayableHandle handle = CreatePlayable();
            if (!handle.IsValid())
                return PlayableHandle.Null;

            IPlayable scriptPlayable = null;

            if (typeof(UnityEngine.ScriptableObject).IsAssignableFrom(typeof(T)))
                scriptPlayable = ScriptableObject.CreateInstance(typeof(T)) as T;
            else
                scriptPlayable = new T();

            if (scriptPlayable == null)
            {
                handle.Destroy();
                Debug.LogError("Could not create a ScriptPlayable of Type " + typeof(T).ToString());
                return PlayableHandle.Null;
            }

            SetScriptInstance(ref handle, scriptPlayable);
            scriptPlayable.playableHandle = handle;

            return handle;
        }

        public PlayableHandle CreateScriptMixerPlayable<T>(int inputCount) where T : class, IScriptPlayable, IPlayable, new()
        {
            var handle = CreateScriptPlayable<T>();
            if (handle.IsValid())
                handle.inputCount = inputCount;
            return handle;
        }

        public PlayableHandle CloneScriptPlayable(IScriptPlayable source)
        {
            if (source == null)
                throw new ArgumentNullException("source parameter cannot be null");

            ScriptableObject scriptableObject = source as ScriptableObject;
            UnityEngine.Object engineObject = source as UnityEngine.Object;
            if (scriptableObject != null)
                return InternalCloneScriptableObjectPlayable(this, scriptableObject, scriptableObject.GetType());
            else if (engineObject != null)
                return InternalCloneEngineObjectPlayable(this, engineObject);

            return InternalCloneObjectPlayable(this, source);
        }

        internal static PlayableHandle InternalCloneScriptableObjectPlayable(PlayableGraph graph, ScriptableObject source, Type type)
        {
            PlayableHandle handle = graph.CreatePlayable();
            if (!handle.IsValid())
                return PlayableHandle.Null;

            var scriptPlayable = Object.Instantiate(source);
            if (scriptPlayable == null)
            {
                Debug.LogError("Could not clone a ScriptPlayable of Type " + type.ToString());
                handle.Destroy();
                return PlayableHandle.Null;
            }

            SetScriptInstance(ref handle, scriptPlayable);

            var playable = (IPlayable)scriptPlayable;
            playable.playableHandle = handle;
            scriptPlayable.hideFlags |= HideFlags.DontSave;
            return handle;
        }

        internal static PlayableHandle InternalCloneEngineObjectPlayable(PlayableGraph graph, UnityEngine.Object source)
        {
            PlayableHandle handle = graph.CreatePlayable();
            if (!handle.IsValid())
                return PlayableHandle.Null;

            var scriptPlayable = Object.Instantiate(source);
            if (scriptPlayable == null)
            {
                Debug.LogError("Could not clone a ScriptPlayable of Type " + source.GetType().ToString());
                handle.Destroy();
                return PlayableHandle.Null;
            }

            SetScriptInstance(ref handle, scriptPlayable);

            var playable = (IPlayable)scriptPlayable;
            playable.playableHandle = handle;
            scriptPlayable.hideFlags |= HideFlags.DontSave;
            return handle;
        }

        internal static PlayableHandle InternalCloneObjectPlayable(PlayableGraph graph, object source)
        {
            PlayableHandle handle = graph.CreatePlayable();
            if (!handle.IsValid())
                return PlayableHandle.Null;

            ICloneable cloneable = source as ICloneable;
            if (cloneable == null)
            {
                Debug.LogError("Could not clone a ScriptPlayable of Type " + source.GetType().ToString() + " as it does not implement ICloneable");
                handle.Destroy();
                return PlayableHandle.Null;
            }

            var scriptPlayable = cloneable.Clone();
            if (scriptPlayable == null)
            {
                Debug.LogError("Could not clone a ScriptPlayable of Type " + source.GetType().ToString());
                handle.Destroy();
                return PlayableHandle.Null;
            }

            SetScriptInstance(ref handle, scriptPlayable);

            var playable = (IPlayable)scriptPlayable;
            playable.playableHandle = handle;

            return handle;
        }
    }
}
