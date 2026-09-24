// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

internal interface IPackageCreator: IService
{
    public event Action<string> onPackageCreated;
    public bool CanGenerateValidNamespace(string displayName);
    public void CreatePackage(string displayName, Action<string> errorCallback);
}

internal class PackageCreator : BaseService<IPackageCreator>, IPackageCreator
{
    public event Action<string> onPackageCreated = delegate { };
    private const string k_DefaultTemplateFolder = "Resources/PackageManager/PackageTemplates/standard";
    private const string k_TempFolder = "Temp/";
    private const string k_PackagesFolder = "Packages/";

    private const string k_DefaultOrgNamespace = "Undefined";
    private const string k_DefaultPackageNamespace = "NewPackage";
    private const string k_DefaultOrgTechnicalName = "undefined";
    private const string k_DefaultTechnicalNameWithoutOrg = "newpackage";

    public static readonly string k_GeneralExceptionConsoleErrorMessage = L10n.Tr("An error occurred while trying to create the new package.");
    public static readonly string k_TempAndPackagesFolderAreReadOnlyConsoleErrorMessage = L10n.Tr("An error occurred while trying to create the new package. The project contains read-only folders ('Temp' and 'Packages'). Change the permissions to read and write and try again.");
    public static readonly string k_TempFolderIsReadOnlyConsoleErrorMessage = L10n.Tr("An error occurred while trying to create the new package. The 'Temp' folder in the project is a read-only folder. Change the permissions to read and write and try again.");
    public static readonly string k_PackagesFolderIsReadOnlyConsoleErrorMessage = L10n.Tr("An error occurred while trying to create the new package. The 'Packages' folder in the project is a read-only folder. Change the permissions to read and write and try again.");
    public static readonly string k_PermissionDeniedGeneralConsoleErrorMessage = L10n.Tr("An error occurred while trying to create the new package. You might not have permissions to write to the 'Temp' or 'Packages' folders. This error can happen when you don't open the Unity Editor in administrator mode. Close the Editor, then open it in administrator mode and try again. Error: ");
    public static readonly string k_PathIsTooLongConsoleErrorMessage = L10n.Tr("An error occurred while trying to create the new package. The path to your project might be too long. Move your project to another location with a shorter path, then try again.");

    public static readonly string k_NameIsTooLongUiErrorMessage = L10n.Tr("The package display name is too long.");
    public static readonly string k_GeneralExceptionUiErrorMessage = L10n.Tr("An error occured while creating the package. See console for more details.");

    private readonly IUnityConnectProxy m_UnityConnectProxy;
    private readonly IUpmCache m_UpmCache;
    private readonly IUpmClient m_UpmClient;
    private readonly IIOProxy m_IOProxy;
    private readonly IDateTimeProxy m_DateTimeProxy;

    public PackageCreator(IUpmClient upmClient, IUpmCache upmCache, IUnityConnectProxy unityConnectProxy,
        IIOProxy ioProxy, IDateTimeProxy dateTimeProxy)
    {
        m_UpmClient = RegisterDependency(upmClient);
        m_UpmCache = RegisterDependency(upmCache);
        m_UnityConnectProxy = RegisterDependency(unityConnectProxy);
        m_IOProxy = RegisterDependency(ioProxy);
        m_DateTimeProxy = RegisterDependency(dateTimeProxy);
    }

    // Since namespace has a stricter rule than package name, if we can sanitize a string to be used as a namespace,
    // we know for sure it will also be good to be used as package name after sanitization.
    public bool CanGenerateValidNamespace(string value) => !string.IsNullOrEmpty(PackageValidator.SanitizeNamespace(value));

    private static string SanitizeOrUseDefault(string value, Func<string, string> sanitizationFunction, string defaultValue)
    {
        var sanitizedValue = sanitizationFunction?.Invoke(value);
        return string.IsNullOrEmpty(sanitizedValue) ? defaultValue : sanitizedValue;
    }

    private void ModifySuffixToAvoidConflictIfNeeded(ref string technicalName, ref string rootNamespace)
    {
        // We modify the technical name and the namespace together because we want to keep the suffix in sync as much as possible. Not doing that could lead to
        // cases like a package with technical name `com.org.package2` but the namespace is  `Org.Package` or even `Org.Package1`, which can be confusing.
        var technicalNameWithoutSuffixNumber = RemoveSuffixNumber(technicalName);
        var newSuffix = 0;
        while (m_UpmCache.GetInstalledPackageInfo(technicalName) != null)
        {
            newSuffix++;
            technicalName = $"{technicalNameWithoutSuffixNumber}{newSuffix}";
        }

        if (newSuffix <= 0)
            return;

        rootNamespace = $"{RemoveSuffixNumber(rootNamespace)}{newSuffix}";
    }

    private void GetOrganizationName(Action<string> onResult)
    {
        var orgName = m_UnityConnectProxy.projectOrgName;
        if (!string.IsNullOrEmpty(orgName))
            onResult?.Invoke(orgName);
        else
            m_UnityConnectProxy.ParseOrganizationInfosAsync(organizationInfos =>
            {
                if (organizationInfos.Length > 0)
                    orgName = organizationInfos[0].name;
                onResult?.Invoke(!string.IsNullOrEmpty(orgName) ? orgName : m_UnityConnectProxy.userPrimaryOrg);
            });
    }

    public void CreatePackage(string displayName, Action<string> errorCallback)
    {
        GetOrganizationName(orgName =>
        {
            var orgTechnicalName = SanitizeOrUseDefault(orgName, PackageValidator.SanitizePackageTechnicalName, k_DefaultOrgTechnicalName);

            var technicalNameWithoutOrg = SanitizeOrUseDefault(displayName, PackageValidator.SanitizePackageTechnicalName, k_DefaultTechnicalNameWithoutOrg);
            var technicalName = "com." + orgTechnicalName + "." + technicalNameWithoutOrg;

            var organizationNamespace = SanitizeOrUseDefault(orgName, PackageValidator.SanitizeNamespace, k_DefaultOrgNamespace);
            var packageNamespace = SanitizeOrUseDefault(displayName, PackageValidator.SanitizeNamespace, k_DefaultPackageNamespace);
            var rootNamespace = organizationNamespace + "." + packageNamespace;

            ModifySuffixToAvoidConflictIfNeeded(ref technicalName, ref rootNamespace);

            if (technicalName.Length > PackageValidator.k_MaxAllowedCharsInTechnicalName)
            {
                errorCallback?.Invoke(k_NameIsTooLongUiErrorMessage);
                return;
            }

            var destinationDirName = IOUtils.PathsCombine("Packages", technicalName);
            var variables = CreateTemplateVariables(technicalName, displayName, rootNamespace);
            var tempFolder = m_IOProxy.GetUniqueTempPathInProject();
            try
            {
                var templateFolderPath =
                    IOUtils.PathsCombine(EditorApplication.applicationContentsPath, k_DefaultTemplateFolder);
                m_IOProxy.DirectoryCopy(templateFolderPath, tempFolder);

                var templateFiles = m_IOProxy.GetFiles(tempFolder, "*.*", SearchOption.AllDirectories);
                foreach (var filePath in templateFiles)
                {
                    var fileAttributes = m_IOProxy.GetFileAttributes(filePath);
                    if ((fileAttributes & (FileAttributes.Hidden | FileAttributes.ReadOnly)) == 0)
                        m_IOProxy.SetFileAttributes(filePath,
                            fileAttributes & ~(FileAttributes.Hidden | FileAttributes.ReadOnly));
                    ReplaceVariablesInFile(variables, filePath);
                    RenameFileWithVariables(variables, filePath);
                }

                m_IOProxy.Move(tempFolder, destinationDirName);
                TriggerOnPackageCreatedEventAndResolve(technicalName);
            }
            catch (Exception e)
            {
                HandleException(destinationDirName, e);
                errorCallback?.Invoke(k_GeneralExceptionUiErrorMessage);
            }
            finally
            {
                m_IOProxy.DeleteIfExists(tempFolder);
            }
        });
    }

    private void HandleException(string destinationDirName, Exception e)
    {
        switch (e)
        {
            case PathTooLongException:
                Debug.LogError(k_PathIsTooLongConsoleErrorMessage);
                break;
            case UnauthorizedAccessException or IOException:
                var isTempFolderReadOnly = m_IOProxy.GetFileAttributes(k_TempFolder).HasFlag(FileAttributes.ReadOnly);
                var isPackagesFolderReadOnly = m_IOProxy.GetFileAttributes(k_PackagesFolder).HasFlag(FileAttributes.ReadOnly);
                if (isTempFolderReadOnly && isPackagesFolderReadOnly)
                    Debug.LogError(k_TempAndPackagesFolderAreReadOnlyConsoleErrorMessage);
                else if (isTempFolderReadOnly)
                    Debug.LogError(k_TempFolderIsReadOnlyConsoleErrorMessage);
                else if (isPackagesFolderReadOnly)
                    Debug.LogError(k_PackagesFolderIsReadOnlyConsoleErrorMessage);
                else
                    Debug.LogError($"{k_PermissionDeniedGeneralConsoleErrorMessage} {e.Message}");
                break;
            default:
                Debug.LogError($"{k_GeneralExceptionConsoleErrorMessage} {e.Message}");
                break;
        }
        m_IOProxy.DeleteIfExists(destinationDirName);;
    }

    private void TriggerOnPackageCreatedEventAndResolve(string packageName)
    {
        onPackageCreated?.Invoke(packageName);
        m_UpmClient.Resolve(true);
    }

    private static string RemoveSuffixNumber(string value)
    {
        return Regex.Replace(value, @"\d+$", "").Trim();
    }

    private void ReplaceVariablesInFile(Dictionary<string, string> variables, string file)
    {
        var original = m_IOProxy.FileReadAllText(file);
        var updated = ReplaceVariablesInString(variables, original);
        if (original != updated)
            m_IOProxy.FileWriteAllText(file, updated);
    }

    private void RenameFileWithVariables(Dictionary<string, string> variables, string file)
    {
        var originalFileName = IOUtils.GetFileName(file);
        var updatedFileName =  ReplaceVariablesInString(variables, originalFileName);
        if (updatedFileName == originalFileName)
            return;

        var folder = IOUtils.GetParentDirectory(file);
        m_IOProxy.Move(file, IOUtils.PathsCombine(folder, updatedFileName));
    }

    private static string ReplaceVariablesInString(IDictionary<string, string> variables, string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder(input.Length);
        for (var i = 0; i < input.Length; )
        {
            var start = input.IndexOf('#', i);
            var end = start < 0 ? -1 : input.IndexOf('#', start + 1);
            if (start < 0 || end < 0)
            {
                sb.Append(input, i, input.Length - i);
                break;
            }

            sb.Append(input, i, start - i);
            // The key here refers to the `#XYZ#` template variable, including the `#` on both sides
            var key = input.Substring(start, end - start + 1);
            if (variables.TryGetValue(key, out var value))
            {
                sb.Append(value);
                i = end + 1;
            }
            else
            {
                // If the key is not found, we copy everything except for the last `#`, as it could be the starting `#` of the next template variable
                sb.Append(input, start, end - start);
                i = end;
            }
        }
        return sb.ToString();
    }

    private Dictionary<string, string> CreateTemplateVariables(string packageTechnicalName, string displayName, string rootNamespace)
    {
        var fullVersion = InternalEditorUtility.GetFullUnityVersion();
        var versionParts = fullVersion.Split(' ')[0].Split('.');

        var majorMinor = $"{versionParts[0]}.{versionParts[1]}";
        var build = versionParts.Length > 2 ? versionParts[2] : string.Empty;
        return new Dictionary<string, string>
        {
            // We only call `EscapeStringForJson` for display name and author name because they are the only ones that could contain characters that need escape
            { "#NAME_JSON#", packageTechnicalName },
            { "#UNITYVERSION_JSON#", majorMinor },
            { "#UNITYRELEASE_JSON#", build },
            { "#DISPLAYNAME_JSON#", EscapeStringForJson(displayName) },
            { "#AUTHORNAME_JSON#", EscapeStringForJson(m_UnityConnectProxy.displayName) },

            { "#DISPLAYNAME#", displayName },
            { "#CURRENTDATE#", m_DateTimeProxy.utcNow.ToString("yyyy-MM-dd") },

            // Since namespace already has strict rules we don't need additional sanitization for it to be used in json or file name
            { "#ROOTNAMESPACE#", rootNamespace },
        };
    }

    private static string EscapeStringForJson(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(value);
    }
}
