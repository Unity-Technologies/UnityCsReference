// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// Represents the list of players as a visual element in the multiplayer play mode.
    /// </summary>
    class PlayersListView : VisualElement
    {
        // Empty

        // [UxmlElement] does no codegen in trunk (6000.2); we have to provide the generated UxmlSerializedData manually.
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new PlayersListView();
        }
    }
}
