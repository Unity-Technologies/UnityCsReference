// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

namespace UnityEngine.Playables
{
    [RequiredByNativeCode]
    public interface INotificationReceiver
    {
        void OnNotify(Playable origin, INotification notification, object context);
    }
}
