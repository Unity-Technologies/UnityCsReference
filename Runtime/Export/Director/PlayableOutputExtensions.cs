// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Playables
{
    public static partial class PlayableOutputExtensions
    {
        public static bool IsOutputNull<U>(this U output) where U : struct, IPlayableOutput
        {
            return output.GetHandle().IsNull();
        }

        public static bool IsOutputValid<U>(this U output) where U : struct, IPlayableOutput
        {
            return output.GetHandle().IsValid();
        }

        public static Object GetReferenceObject<U>(this U output) where U : struct, IPlayableOutput
        {
            return output.GetHandle().GetReferenceObject();
        }

        public static void SetReferenceObject<U>(this U output, Object value) where U : struct, IPlayableOutput
        {
            output.GetHandle().SetReferenceObject(value);
        }

        public static Object GetUserData<U>(this U output) where U : struct, IPlayableOutput
        {
            return output.GetHandle().GetUserData();
        }

        public static void SetUserData<U>(this U output, Object value) where U : struct, IPlayableOutput
        {
            output.GetHandle().SetUserData(value);
        }

        public static Playable GetSourcePlayable<U>(this U output) where U : struct, IPlayableOutput
        {
            return new Playable(output.GetHandle().GetSourcePlayable());
        }

        public static void SetSourcePlayable<U, V>(this U output, V value)
            where U : struct, IPlayableOutput
            where V : struct, IPlayable
        {
            output.GetHandle().SetSourcePlayable(value.GetHandle());
        }

        public static void SetSourcePlayable<U, V>(this U output, V value, int port)
            where U : struct, IPlayableOutput
            where V : struct, IPlayable
        {
            var handle = output.GetHandle();
            handle.SetSourcePlayable(value.GetHandle());
            handle.SetSourceOutputPort(port);
        }

        public static int GetSourceOutputPort<U>(this U output) where U : struct, IPlayableOutput
        {
            return output.GetHandle().GetSourceOutputPort();
        }

        public static void SetSourceOutputPort<U>(this U output, int value) where U : struct, IPlayableOutput
        {
            output.GetHandle().SetSourceOutputPort(value);
        }

        public static float GetWeight<U>(this U output) where U : struct, IPlayableOutput
        {
            return output.GetHandle().GetWeight();
        }

        public static void SetWeight<U>(this U output, float value) where U : struct, IPlayableOutput
        {
            output.GetHandle().SetWeight(value);
        }

        public static void PushNotification<U>(this U output, Playable origin, INotification notification, object context = null) where U : struct, IPlayableOutput
        {
            output.GetHandle().PushNotification(origin.GetHandle(), notification, context);
        }

        public static INotificationReceiver[] GetNotificationReceivers<U>(this U output) where U : struct, IPlayableOutput
        {
            return output.GetHandle().GetNotificationReceivers();
        }

        public static void AddNotificationReceiver<U>(this U output, INotificationReceiver receiver) where U : struct, IPlayableOutput
        {
            output.GetHandle().AddNotificationReceiver(receiver);
        }

        public static void RemoveNotificationReceiver<U>(this U output, INotificationReceiver receiver) where U : struct, IPlayableOutput
        {
            output.GetHandle().RemoveNotificationReceiver(receiver);
        }
    }
}
