// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.ManagedCapture.Internal
{
    internal static partial class ManagedCaptureHookEngine
    {
        const int k_BreadcrumbBufferCapacity = 256;
        const string k_BreadcrumbEventName = "method_called";

        // Caps a single DispatchFallback call's payload size.
        const int k_MaxBreadcrumbArgs = 32;

        // Separate from the structured buffer: a breadcrumb dispatched from inside an open structured
        // event would otherwise clobber it.
        [ThreadStatic, NoAutoStaticsCleanup] static StringBuilder s_Breadcrumb;

        public static void Dispatch(int eventType, string mediator, string method)
        {
            if (IsSuppressed(eventType))
                return;
            EmitBreadcrumb(eventType, BeginBreadcrumb(eventType, mediator, method));
        }

        public static void Dispatch<T1>(int eventType, string mediator, string method, T1 p0)
        {
            if (IsSuppressed(eventType))
                return;

            var builder = BeginBreadcrumb(eventType, mediator, method);
            EventJsonWriter.AppendArg(builder, 0, p0);
            EmitBreadcrumb(eventType, builder);
        }

        public static void Dispatch<T1, T2>(int eventType, string mediator, string method, T1 p0, T2 p1)
        {
            if (IsSuppressed(eventType))
                return;

            var builder = BeginBreadcrumb(eventType, mediator, method);
            EventJsonWriter.AppendArg(builder, 0, p0);
            EventJsonWriter.AppendArg(builder, 1, p1);
            EmitBreadcrumb(eventType, builder);
        }

        public static void Dispatch<T1, T2, T3>(int eventType, string mediator, string method, T1 p0, T2 p1, T3 p2)
        {
            if (IsSuppressed(eventType))
                return;

            var builder = BeginBreadcrumb(eventType, mediator, method);
            EventJsonWriter.AppendArg(builder, 0, p0);
            EventJsonWriter.AppendArg(builder, 1, p1);
            EventJsonWriter.AppendArg(builder, 2, p2);
            EmitBreadcrumb(eventType, builder);
        }

        public static void Dispatch<T1, T2, T3, T4>(int eventType, string mediator, string method, T1 p0, T2 p1, T3 p2, T4 p3)
        {
            if (IsSuppressed(eventType))
                return;

            var builder = BeginBreadcrumb(eventType, mediator, method);
            EventJsonWriter.AppendArg(builder, 0, p0);
            EventJsonWriter.AppendArg(builder, 1, p1);
            EventJsonWriter.AppendArg(builder, 2, p2);
            EventJsonWriter.AppendArg(builder, 3, p3);
            EmitBreadcrumb(eventType, builder);
        }

        public static void DispatchFallback(int eventType, string mediator, string method, object[] args)
        {
            if (IsSuppressed(eventType))
                return;

            var builder = BeginBreadcrumb(eventType, mediator, method);
            if (args != null)
            {
                int count = Math.Min(args.Length, k_MaxBreadcrumbArgs);
                for (int i = 0; i < count; i++)
                    EventJsonWriter.AppendArg(builder, i, args[i]);
            }
            EmitBreadcrumb(eventType, builder);
        }

        static StringBuilder BreadcrumbBuffer() => RentBuffer(ref s_Breadcrumb, k_BreadcrumbBufferCapacity);

        static StringBuilder BeginBreadcrumb(int eventType, string mediator, string method)
        {
            var builder = BreadcrumbBuffer();
            EventJsonWriter.AppendOrigin(builder, k_BreadcrumbEventName, eventType, mediator, method);
            return builder;
        }

        static void EmitBreadcrumb(int eventType, StringBuilder builder)
        {
            builder.Append('}');

            var record = RentRecord();
            record.EventType = eventType;
            record.PreSerializedBody = builder.ToString();
            DispatchRecord(record, LogEventOverride);
        }
    }
}
