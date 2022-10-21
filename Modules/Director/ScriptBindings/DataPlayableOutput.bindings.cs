// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Playables
{
    [NativeHeader("Modules/Director/ScriptBindings/DataPlayableOutput.bindings.h")]
    [NativeHeader("Modules/Director/ScriptBindings/DataPlayableOutputExtensions.bindings.h")]
    [NativeHeader("Modules/Director/DataPlayableOutput.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableGraph.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [StaticAccessor("DataPlayableOutputBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    internal struct DataPlayableOutput : IPlayableOutput
    {
        private PlayableOutputHandle m_Handle;
        public System.Type GetStreamType() { return InternalGetType(ref m_Handle); }

        public bool GetConnectionChanged() { return InternalGetConnectionChanged(ref m_Handle); }

        public void ClearConnectionChanged() { InternalClearConnectionChanged(ref m_Handle); }

        public TDataStream GetDataStream<TDataStream>()
            where TDataStream: new()
        {
            object stream = InternalGetStream(ref m_Handle);
            if (stream is TDataStream)
            {
                return (TDataStream)stream;
            }
            return default;
        }

        public void SetDataStream<TDataStream>(TDataStream stream)
            where TDataStream : new()
        {
            Type streamType = GetStreamType();
            if ( !streamType.IsAssignableFrom(typeof(TDataStream)) )
                throw new ArgumentException($"{nameof(stream)} is of the wrong type. This output only accepts streams with type {streamType} or inheriting from type {streamType}", nameof(stream));

            InternalSetStream(ref m_Handle, stream);
        }

        public static DataPlayableOutput Create<TDataStream>(PlayableGraph graph, string name)
            where TDataStream : new()
        {
            PlayableOutputHandle handle;
            if (!DataPlayableOutputExtensions.InternalCreateDataOutput(ref graph, name, typeof(TDataStream), out handle))
                return Null;

            DataPlayableOutput output = new DataPlayableOutput(handle);

            return output;
        }

        internal DataPlayableOutput(PlayableOutputHandle handle)
        {
            if (handle.IsValid())
            {
                if (!handle.IsPlayableOutputOfType<DataPlayableOutput>())
                    throw new InvalidCastException("Can't set handle: the playable is not a DataPlayableOutput.");
            }

            m_Handle = handle;
        }

        public static DataPlayableOutput Null
        {
            get { return new DataPlayableOutput(PlayableOutputHandle.Null); } 
        }

        public PlayableOutputHandle GetHandle()
        {
            return m_Handle;
        }

        public static implicit operator PlayableOutput(DataPlayableOutput output)
        {
            return new PlayableOutput(output.GetHandle());
        }

        public static explicit operator DataPlayableOutput(PlayableOutput output)
        {
            return new DataPlayableOutput(output.GetHandle());
        }

        public IDataPlayer GetPlayer()
        {
            return InternalGetPlayer(ref m_Handle) as IDataPlayer;
        }

        public void SetPlayer<TPlayer>(TPlayer player) where TPlayer: Object, IDataPlayer
        {
            InternalSetPlayer(ref m_Handle, player);
        }

        [NativeThrows]
        extern private static Object InternalGetPlayer(ref PlayableOutputHandle handle);

        [NativeThrows]
        extern private static void InternalSetPlayer(ref PlayableOutputHandle handle, Object player);

        [NativeThrows]
        private extern static Type InternalGetType(ref PlayableOutputHandle handle);

        [NativeThrows]
        private extern static void InternalSetStream(ref PlayableOutputHandle handle, object stream);

        [NativeThrows]
        private extern static object InternalGetStream(ref PlayableOutputHandle handle);

        [NativeThrows]
        private extern static bool InternalGetConnectionChanged(ref PlayableOutputHandle handle);

        [NativeThrows]
        private extern static void InternalClearConnectionChanged(ref PlayableOutputHandle handle);


        [RequiredByNativeCode]
        private static void Internal_CallOnPlayerChanged(PlayableOutputHandle handle, object previousPlayer, object currentPlayer)
        {
            var output = new DataPlayableOutput(handle);
            if (previousPlayer is IDataPlayer previousDataPlayer)
            {
                previousDataPlayer.Release(output);
            }

            if (currentPlayer is IDataPlayer currentDataPlayer)
            {
                currentDataPlayer.Bind(output);
            }
        }
    }
}
