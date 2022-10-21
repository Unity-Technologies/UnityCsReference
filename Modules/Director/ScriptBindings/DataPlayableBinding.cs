// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEngine.Playables
{
    internal static class DataPlayableBinding
    {
        public static PlayableBinding Create<TDataStream, TPlayer>(string name, Object key)
            where TDataStream : new()
            where TPlayer : Object
        {
            return PlayableBinding.CreateInternal(name, key, typeof(TPlayer), CreateDataOutput<TDataStream>);
        }

        private static PlayableOutput CreateDataOutput<TDataStream>(PlayableGraph graph, string name) where TDataStream : new()
        {
            return UnityEngine.Playables.DataPlayableOutput.Create<TDataStream>(graph, name);
        }
    }
}
