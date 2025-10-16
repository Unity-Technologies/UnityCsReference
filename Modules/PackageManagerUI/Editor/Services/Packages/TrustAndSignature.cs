// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal enum TrustAndSignature
{
    NotApplicable,
    // We want to single out valid signature Unity (rather than relying on checking if a unity package is from a Unity registry)
    // so that we show the correct signature info even when a unity signed package gets pushed to other sources. Additionally,
    // the org name is not always set for unity signed packages, so we need to do some special handling
    FullTrustUnitySignature,
    FullTrustValidSignature,
    // There are exceptions for packages without signature to still be fully trusted, for example, packages bundled with Unity
    FullTrustNoSignature,
    LimitedTrust,
    UntrustedNoSignature,
    UntrustedInvalidSignature,
}
