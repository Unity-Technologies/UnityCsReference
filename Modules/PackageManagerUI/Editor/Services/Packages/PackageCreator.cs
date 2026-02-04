// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal;

internal interface IPackageCreator: IService
{
    public event Action<string> onPackageCreated;
    public void CreatePackage(string displayName);
}

internal class PackageCreator : BaseService<IPackageCreator>, IPackageCreator
{
    public event Action<string> onPackageCreated = delegate { };
    private static readonly string k_DefaultTemplateFolder = "Resources/PackageManager/PackageTemplates/standard";
    private static readonly string k_TempFolder = "Temp/";
    private static readonly string k_PackagesFolder = "Packages/";
    // The maximum value of a package name follows this documentation: https://docs.unity3d.com/Manual/cus-naming.html
    private const int k_MaxPackageNameLength = 214;

    private const string k_DefaultOrgName = "Undefined";
    private const string k_DefaultDisplayName = "New Package";

    public static readonly string k_GeneralExceptionErrorMessage = L10n.Tr("An error occurred while trying to create the new package.");
    public static readonly string k_TempAndPackagesFolderAreReadOnlyErrorMessage = L10n.Tr("An error occurred while trying to create the new package. The project contains read-only folders ('Temp' and 'Packages'). Change the permissions to read and write and try again.");
    public static readonly string k_TempFolderIsReadOnlyErrorMessage = L10n.Tr("An error occurred while trying to create the new package. The 'Temp' folder in the project is a read-only folder. Change the permissions to read and write and try again.");
    public static readonly string k_PackagesFolderIsReadOnlyErrorMessage = L10n.Tr("An error occurred while trying to create the new package. The 'Packages' folder in the project is a read-only folder. Change the permissions to read and write and try again.");
    public static readonly string k_PermissionDeniedGeneralErrorMessage = L10n.Tr("An error occurred while trying to create the new package. You might not have permissions to write to the 'Temp' or 'Packages' folders. This error can happen when you don't open the Unity Editor in administrator mode. Close the Editor, then open it in administrator mode and try again. Error: ");
    public static readonly string k_PathIsTooLongErrorMessage = L10n.Tr("An error occurred while trying to create the new package. The path to your project might be too long. Move your project to another location with a shorter path, then try again.");
    public static readonly string k_NameIsTooLongErrorMessage = L10n.Tr("The package display name is too long.");
    public static readonly string k_PackageDisplayNameChangedMessage = L10n.Tr("The display name you entered contained invalid characters. It has been updated from \"{0}\" to \"{1}\".");

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

    public void CreatePackage(string displayName)
    {
        var organization = IsValidPackageNameAndNamespaceAfterSanitization(m_UnityConnectProxy.userPrimaryOrg)
            ? m_UnityConnectProxy.userPrimaryOrg
            : k_DefaultOrgName;
        var oldDisplayName = displayName;
        displayName = IsValidPackageNameAndNamespaceAfterSanitization(displayName)
            ? PackageValidator.SanitizeDisplayName(displayName)
            : k_DefaultDisplayName;

        if(!displayName.Equals(oldDisplayName))
            Debug.LogWarning(string.Format(k_PackageDisplayNameChangedMessage, oldDisplayName, displayName));

        var packageName = GenerateUniquePackageNameAndUpdateDisplayName(organization, ref displayName);
        var rootNamespace = PackageValidator.SanitizeNamespace(organization) + "." + PackageValidator.SanitizeNamespace(displayName);

        var destinationDirName = IOUtils.PathsCombine("Packages", packageName);
        var variables = CreateTemplateVariables(packageName, displayName, rootNamespace);
        var tempFolder = m_IOProxy.GetUniqueTempPathInProject();
        try
        {
            var templateFolderPath =
                IOUtils.PathsCombine(EditorApplication.applicationContentsPath, k_DefaultTemplateFolder);
            m_IOProxy.DirectoryCopy(templateFolderPath, tempFolder);

            var templateFiles = m_IOProxy.GetFiles(tempFolder, "*.*", true);
            foreach (var file in templateFiles)
            {
                var filePath = file.ToString();
                var fileAttributes = m_IOProxy.GetFileAttributes(filePath);
                if ((fileAttributes & (FileAttributes.Hidden | FileAttributes.ReadOnly)) == 0)
                    m_IOProxy.SetFileAttributes(filePath,
                        fileAttributes & ~(FileAttributes.Hidden | FileAttributes.ReadOnly));
                ReplaceVariablesInFile(variables, filePath);
                RenameFileWithVariables(variables, filePath);
            }

            m_IOProxy.Move(tempFolder, destinationDirName);
            TriggerOnPackageCreatedEventAndResolve(packageName);
        }
        catch (Exception e)
        {
            HandleException(destinationDirName, e);
            throw;
        }
        finally
        {
            m_IOProxy.DeleteIfExists(tempFolder);
        }
    }

    private void HandleException(string destinationDirName, Exception e)
    {
        switch (e)
        {
            case PathTooLongException:
                Debug.LogError(k_PathIsTooLongErrorMessage);
                break;
            case UnauthorizedAccessException or IOException:
                var isTempFolderReadOnly = m_IOProxy.GetFileAttributes(k_TempFolder).HasFlag(FileAttributes.ReadOnly);
                var isPackagesFolderReadOnly = m_IOProxy.GetFileAttributes(k_PackagesFolder).HasFlag(FileAttributes.ReadOnly);
                if (isTempFolderReadOnly && isPackagesFolderReadOnly)
                    Debug.LogError(k_TempAndPackagesFolderAreReadOnlyErrorMessage);
                else if (isTempFolderReadOnly)
                    Debug.LogError(k_TempFolderIsReadOnlyErrorMessage);
                else if (isPackagesFolderReadOnly)
                    Debug.LogError(k_PackagesFolderIsReadOnlyErrorMessage);
                else
                    Debug.LogError($"{k_PermissionDeniedGeneralErrorMessage} {e.Message}");
                break;
            default:
                Debug.LogError($"{k_GeneralExceptionErrorMessage} {e.Message}");
                break;
        }
        m_IOProxy.DeleteIfExists(destinationDirName);;
    }

    private void TriggerOnPackageCreatedEventAndResolve(string packageName)
    {
        onPackageCreated?.Invoke(packageName);
        m_UpmClient.Resolve(true);
    }

    private string GenerateUniquePackageNameAndUpdateDisplayName(string organization, ref string displayName)
    {
        var packageName = "com." + PackageValidator.SanitizePackageTechnicalName(organization) + "." + PackageValidator.SanitizePackageTechnicalName(displayName);
        var packageNameWithoutSuffixNumber = RemoveSuffixNumber(packageName);

        var newSuffix = 0;
        while (m_UpmCache.GetInstalledPackageInfo(packageName) != null)
        {
            newSuffix++;
            packageName = $"{packageNameWithoutSuffixNumber}{newSuffix}";
        }

        if (newSuffix > 0)
            displayName = $"{RemoveSuffixNumber(displayName)} {newSuffix}";

        if (packageName.Length > k_MaxPackageNameLength)
            throw new ArgumentException(k_NameIsTooLongErrorMessage);

        return packageName;
    }

    private static string RemoveSuffixNumber(string value)
    {
        return Regex.Replace(value, @"\d+$", "").Trim();
    }

    // Since namespace has a stricter rule than package name, if a string is good enough to be used as a namespace after sanitization,
    // we know for sure it will also be good to be used as package name after sanitization.
    private bool IsValidPackageNameAndNamespaceAfterSanitization(string value)
    {
        var sanitizedValue = PackageValidator.SanitizeNamespace(value);
        return !string.IsNullOrEmpty(sanitizedValue) && ValidateNamespace(sanitizedValue);
    }

    private bool ValidateNamespace(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        try
        {
            CodeGenerator.ValidateIdentifiers(new CodeNamespace(name));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
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
        var updatedFileName = ReplaceVariablesInString(variables, originalFileName);

        if (updatedFileName == originalFileName)
            return;

        var folder = IOUtils.GetParentDirectory(file);
        m_IOProxy.Move(file, IOUtils.PathsCombine(folder, updatedFileName));
    }

    private static string ReplaceVariablesInString(Dictionary<string, string> variables, string input)
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

    private Dictionary<string, string> CreateTemplateVariables(string packageName, string displayName, string rootNamespace)
    {
        var fullVersion = InternalEditorUtility.GetFullUnityVersion();
        var versionParts = fullVersion.Split(' ')[0].Split('.');

        var majorMinor = $"{versionParts[0]}.{versionParts[1]}";
        var build = versionParts.Length > 2 ? versionParts[2] : string.Empty;
        return new Dictionary<string, string>
        {
            { "#NAME#", packageName },
            { "#DISPLAYNAME#", displayName },
            { "#ROOTNAMESPACE#", rootNamespace },
            { "#UNITYVERSION#", majorMinor },
            { "#UNITYRELEASE#", build },
            { "#CURRENTDATE#", m_DateTimeProxy.utcNow.ToString("yyyy-MM-dd") },
            { "#AUTHORNAME#", m_UnityConnectProxy.displayName }
        };
    }
}
