// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.Licensing.UI.Data.Events.Base;

[Serializable]
class Notification
{
    public NotificationType NotificationTypeAsEnum => Enum.TryParse<NotificationType>(
        notificationType, true, out var targetType)
        ? targetType
        : Events.NotificationType.Unknown;

    public string message;
    public string messageType;
    public string notificationType;
    public string sentDate;
    public string title;

    public override string ToString()
    {
        return $"[{notificationType}] {title}: {message}. Sent at: {sentDate}";
    }
}
