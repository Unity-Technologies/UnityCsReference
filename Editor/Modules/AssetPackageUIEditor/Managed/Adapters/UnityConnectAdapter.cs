// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading.Tasks;
using UnityEditor.Connect;
using UnityEditor.PackageManager.UI.Internal;

namespace UnityEditor.AssetPackage;

internal interface IUnityConnectAdapter
{
    public bool isUserLoggedIn { get; }
    public Task<OrganizationInfo[]> ParseOrganizationInfos(Action<OrganizationInfo[]> onResult);
}

internal class UnityConnectAdapter : IUnityConnectAdapter
{
    private readonly UnityConnectProxy.OrganizationInfoParser m_OrganizationInfoParser = new();

    public Task<OrganizationInfo[]> ParseOrganizationInfos(Action<OrganizationInfo[]> onResult) => m_OrganizationInfoParser.ParseAsync(onResult);

    public bool isUserLoggedIn => UnityConnect.instance.isUserInfoReady && !string.IsNullOrEmpty(UnityConnect.instance.userInfo.accessToken);
}
