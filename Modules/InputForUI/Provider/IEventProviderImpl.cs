// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.InputForUI
{
    internal interface IEventProviderImpl
    {
        public void Initialize();
        public void Shutdown();
        public void Update();

        public void OnFocusChanged(bool focus);

        /// <summary>
        /// Immediately dispatches event containing current state based on type.
        /// For example when used with KeyEvent will dispatch KeyEvent of State type.
        /// </summary>
        /// <param name="type">State type to try to dispatch.</param>
        /// <returns>True if type was handled by provider. False otherwise.</returns>
        public bool RequestCurrentState(Event.Type type);

        /// <summary>
        /// Amount of input players currently active with-in the game.
        /// This potentially maps to platform native abstractions of local split screen multiplayer,
        /// where players join the same instance of the game and each get assigned a specific input device.
        /// </summary>
        public uint playerCount { get; }
    }
}
