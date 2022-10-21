// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEngine.Playables
{
    internal interface IDataPlayer
    {
        /// <summary>
        /// Called when a DataPlayableOutput is associated with the Player.
        /// </summary>
        /// Use this to provide the stream to the DataPlayableOutput.
        /// <param name="output">The output associated with the Player.</param>
        public void Bind(DataPlayableOutput output);

        /// <summary>
        /// Called when a DataPlayableOutput is dissociated from the Player.
        /// </summary>
        /// Use this to clean up any memory held by the player for the output.
        /// <param name="output">The output associated with the Player.</param>
        public void Release(DataPlayableOutput output);
    }
}
