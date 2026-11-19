// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor
{
    internal interface IGameViewSizeMenuUser
    {
        void SizeSelectionCallback(int indexClicked, object objectSelected);
        int pixelSize { get; set; }
        bool vSyncEnabled { get; set; }
    }
}
