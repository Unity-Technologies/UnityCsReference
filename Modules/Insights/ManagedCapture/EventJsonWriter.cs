// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;

namespace UnityEngine.ManagedCapture.Internal
{
    // Stateless, so both emission paths can write through it from any thread.
    internal static class EventJsonWriter
    {
        // Breadcrumb-only now: structured events serialize through ManagedCaptureHookEngine.Serialize.
        internal static void AppendOrigin(StringBuilder builder, string eventName, int eventType, string mediator, string method)
        {
            builder.Clear();
            builder.Append('{');
            AppendString(builder, "event_name", eventName);
            AppendInteger(builder, "mediation_class", eventType);
            AppendString(builder, "mediator", string.IsNullOrEmpty(mediator) ? null : mediator);
            AppendString(builder, "method", method);
        }

        // An unescaped quote in a name would close its own key and inject structure.
        internal static void AppendKey(StringBuilder builder, string name)
        {
            AppendSeparator(builder);
            builder.Append('"');
            if (name != null)
                EscapeAndAppend(builder, name);
            builder.Append("\":");
        }

        internal static void AppendString(StringBuilder builder, string name, string value)
        {
            if (value == null)
                return;

            AppendKey(builder, name);
            builder.Append('"');
            EscapeAndAppend(builder, value);
            builder.Append('"');
        }

        internal static void AppendInteger(StringBuilder builder, string name, long value)
        {
            AppendKey(builder, name);
            builder.Append(value);
        }

        internal static void AppendArg<T>(StringBuilder builder, int index, T value)
        {
            string text;
            try
            {
                // typeof(T).IsValueType is a JIT-time constant, so the null check and its box are
                // eliminated for value-type arguments. A ToString() override may return null.
                text = (!typeof(T).IsValueType && (object)value == null ? null : value.ToString()) ?? "null";
            }
            catch (Exception exception)
            {
                text = $"<ToString threw: {exception.GetType().Name}>";
            }

            AppendSeparator(builder);
            builder.Append("\"p").Append(index).Append("\":\"");
            EscapeAndAppend(builder, text);
            builder.Append('"');
        }

        // A written value always ends in '"', a digit, 'e' or 'l', so '{' can only be the just-opened
        // brace. Deriving the separator this way keeps a null first field from emitting a leading comma.
        static void AppendSeparator(StringBuilder builder)
        {
            if (builder[builder.Length - 1] != '{')
                builder.Append(',');
        }

        static void EscapeAndAppend(StringBuilder builder, string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                switch (character)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;

                    case '"':
                        builder.Append("\\\"");
                        break;

                    case '\n':
                        builder.Append("\\n");
                        break;

                    case '\r':
                        builder.Append("\\r");
                        break;

                    case '\t':
                        builder.Append("\\t");
                        break;

                    default:
                        // An unpaired surrogate has no UTF-8 representation; replace it rather than
                        // let it reach native as invalid bytes.
                        if (char.IsHighSurrogate(character))
                        {
                            if (i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                            {
                                builder.Append(character);
                                builder.Append(value[++i]);
                            }
                            else
                                builder.Append('\uFFFD');
                        }
                        else if (char.IsLowSurrogate(character))
                            builder.Append('\uFFFD');
                        else if (character < 0x20)
                            builder.AppendFormat("\\u{0:X4}", (int)character);
                        else
                            builder.Append(character);
                        break;
                }
            }
        }
    }
}
