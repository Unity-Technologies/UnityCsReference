// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Licensing.UI.Events;
using UnityEditor.Licensing.UI.Helper;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEditor.Licensing.UI;

partial class LicenseManagedWrapper
{
    [AutoStaticsCleanupOnCodeReload]
    static ILicenseLogger k_LicenseLogger = new LicenseLogger();
    [AutoStaticsCleanupOnCodeReload]
    static INativeApiWrapper k_NativeApiWrapper = new NativeApiWrapper(k_LicenseLogger);
    [AutoStaticsCleanupOnCodeReload]
    static IModalWrapper k_ModalWrapper = new ModalWrapper();

    // made it public for testing purposes
    [AutoStaticsCleanupOnCodeReload]
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
