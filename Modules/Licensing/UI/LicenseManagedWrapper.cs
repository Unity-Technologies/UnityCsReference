// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Licensing.UI.Events;
using UnityEditor.Licensing.UI.Helper;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEditor.Licensing.UI
{
class LicenseManagedWrapper
{
    static readonly ILicenseLogger k_LicenseLogger = new LicenseLogger();
    static readonly INativeApiWrapper k_NativeApiWrapper = new NativeApiWrapper(k_LicenseLogger);
    static readonly IModalWrapper k_ModalWrapper = new ModalWrapper();

    // made it public for testing purposes
    public static INotificationDispatcher notificationDispatcher =
        new LicenseNotificationDispatcher(
            new LicenseNotificationHandlerFactory(k_LicenseLogger, k_NativeApiWrapper, k_ModalWrapper),
            k_LicenseLogger,
            k_NativeApiWrapper);

    [ExcludeFromDocs]
    [RequiredByNativeCode]
    public static void HandleNotification(string jsonNotification)
    {
        notificationDispatcher.Dispatch(jsonNotification);
    }
}
}
