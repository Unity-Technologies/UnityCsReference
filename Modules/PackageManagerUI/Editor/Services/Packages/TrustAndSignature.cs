// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AssetPackage;

namespace UnityEditor.PackageManager.UI.Internal;

internal enum TrustAndSignature
{
    NotApplicable,
    // We want to single out valid signature Unity (rather than relying on checking if a unity package is from a Unity registry)
    // so that we show the correct signature info even when a unity signed package gets pushed to other sources. Additionally,
    // the org name is not always set for unity signed packages, so we need to do some special handling
    FullTrustUnitySignature,
    FullTrustValidSignature,
    // There are exceptions for packages without signature to still be fully trusted, for example, packages from the Asset Store without a signature, while we wait for them to get all signed
    FullTrustNoSignature,
    // There are exceptions for built-in packages to still be fully trusted even if unsigned, this is only for packages with a builtin source
    FullTrustBuiltInPackage,
    LimitedTrust,
    UntrustedNoSignature,
    UntrustedInvalidSignature,
}

internal static class TrustAndSignatureHelper
{
    public static TrustAndSignature GetTrustAndSignature(PackageInfo packageInfo, bool isInstalled)
    {
        if (!isInstalled)
            return TrustAndSignature.NotApplicable;

        return GetTrustAndSignature(packageInfo.trustLevel, packageInfo.signature, packageInfo.source == PackageSource.BuiltIn);
    }

    public static TrustAndSignature GetTrustAndSignature(AssetPackageInfo assetPackageInfo)
    {
        return GetTrustAndSignature(assetPackageInfo.trustLevel, assetPackageInfo.signature, isBuiltIn: false);
    }

    private static TrustAndSignature GetTrustAndSignature(TrustLevel trustLevel, SignatureInfo signature, bool isBuiltIn)
    {
        switch (trustLevel)
        {
            case TrustLevel.FullTrust:
            {
                if (signature?.status == SignatureStatus.Valid)
                {
                    var publishingChannel = signature.attestation?.publishingChannel ?? "";
                    // When the signature is valid but the publishing channel is empty, it's a legacy Unity signature
                    // We will check it this way before the UpmClient provides a better way to identify legacy signatures
                    if (publishingChannel is "unity" or "")
                        return TrustAndSignature.FullTrustUnitySignature;
                    return TrustAndSignature.FullTrustValidSignature;
                }

                if (isBuiltIn)
                    return TrustAndSignature.FullTrustBuiltInPackage;

                return TrustAndSignature.FullTrustNoSignature;
            }
            case TrustLevel.LimitedTrust:
                return TrustAndSignature.LimitedTrust;
            case TrustLevel.Untrusted:
                switch (signature?.status)
                {
                    case SignatureStatus.Unsigned:
                        return TrustAndSignature.UntrustedNoSignature;
                    case SignatureStatus.Invalid:
                        return TrustAndSignature.UntrustedInvalidSignature;
                }
                break;
        }
        return TrustAndSignature.NotApplicable;
    }
}
