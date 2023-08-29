// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Licensing.UI.Data.Events;
using UnityEditor.Licensing.UI.Events.Handlers;

namespace UnityEditor.Licensing.UI.Events
{
interface ILicenseNotificationHandlerFactory
{
    public INotificationHandler GetHandler(NotificationType notificationType, string jsonNotification);
}
}
