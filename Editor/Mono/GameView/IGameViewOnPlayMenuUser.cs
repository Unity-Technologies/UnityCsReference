// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor
{
    internal interface IGameViewOnPlayMenuUser
    {
        void OnPlayPopupSelection(int indexClicked, object objectSelected);
        bool playFocused { get; set; }
        bool vSyncEnabled { get; set; }
    }
}
