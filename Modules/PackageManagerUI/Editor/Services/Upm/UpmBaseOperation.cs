// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI
{
    internal abstract class UpmBaseOperation : IBaseOperation
    {
        public static string GroupName(PackageSource origin, string type)
        {
            var group = PackageGroupOrigins.Packages.ToString();
            if (PackageInfo.IsPackageBuiltIn(origin, type))
                group = PackageGroupOrigins.BuiltInPackages.ToString();

            return group;
        }

        protected static IEnumerable<PackageInfo> FromUpmPackageInfo(PackageManager.PackageInfo info, bool isCurrent = true)
        {
            var packages = new List<PackageInfo>();
            var displayName = info.displayName;
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = info.name.Replace(PackageInfo.ModulePrefix, "");
                displayName = displayName.Replace(PackageInfo.UnityPrefix, "");
                displayName = new CultureInfo("en-US").TextInfo.ToTitleCase(displayName);
            }

            string author = info.author.name;
            if (string.IsNullOrEmpty(info.author.name) && info.name.StartsWith(PackageInfo.UnityPrefix))
                author = "Unity Technologies Inc.";

            var lastCompatible = info.versions.latestCompatible;
            var versions = new List<string>();
            versions.AddRange(info.versions.compatible);
            if (versions.FindIndex(version => version == info.version) == -1)
            {
                versions.Add(info.version);

                versions.Sort((left, right) =>
                {
                    if (left == null || right == null) return 0;

                    SemVersion leftVersion = left;
                    SemVersion righVersion = right;
                    return leftVersion.CompareByPrecedence(righVersion);
                });

                SemVersion packageVersion = info.version;
                if (!string.IsNullOrEmpty(lastCompatible))
                {
                    SemVersion lastCompatibleVersion =
                        string.IsNullOrEmpty(lastCompatible) ? (SemVersion)null : lastCompatible;
                    if (packageVersion != null && string.IsNullOrEmpty(packageVersion.Prerelease) &&
                        packageVersion.CompareByPrecedence(lastCompatibleVersion) > 0)
                        lastCompatible = info.version;
                }
                else
                {
                    if (packageVersion != null && string.IsNullOrEmpty(packageVersion.Prerelease))
                        lastCompatible = info.version;
                }
            }

            foreach (var version in versions)
            {
                var isVersionCurrent = version == info.version && isCurrent;
                var isBuiltIn = info.source == PackageSource.BuiltIn;
                var isVerified = string.IsNullOrEmpty(SemVersion.Parse(version).Prerelease) && version == info.versions.verified;
                var state = (isBuiltIn || info.version == lastCompatible ||   !isCurrent) ? PackageState.UpToDate : PackageState.Outdated;

                // Happens mostly when using a package that hasn't been in production yet.
                if (info.versions.all.Length <= 0)
                    state = PackageState.UpToDate;

                if (info.errors.Length > 0)
                    state = PackageState.Error;

                var packageInfo = new PackageInfo
                {
                    Name = info.name,
                    DisplayName = displayName,
                    PackageId = version == info.version ? info.packageId : null,
                    Version = version,
                    Description = info.description,
                    Category = info.category,
                    IsInstalled = isVersionCurrent,
                    IsLatest = version == lastCompatible,
                    IsVerified = isVerified,
                    Errors = info.errors.ToList(),
                    Group = GroupName(info.source, info.type),
                    Type = info.type,
                    State = state,
                    Origin = isBuiltIn || isVersionCurrent ? info.source : PackageSource.Registry,
                    Author = author,
                    Info = info,
                    HasFullFetch = version == info.version
                };

                if (version == info.version && !string.IsNullOrEmpty(info.resolvedPath))
                {
                    var loadedSamples = SampleJsonHelper.LoadSamplesFromPackageJson(info.resolvedPath);
                    packageInfo.Samples = new List<Sample>();
                    foreach (var sample in loadedSamples)
                    {
                        sample.resolvedPath = Path.Combine(info.resolvedPath, sample.path);
                        sample.importPath = IOUtils.CombinePaths
                            (
                                Application.dataPath,
                                "Samples",
                                IOUtils.SanitizeFileName(packageInfo.DisplayName),
                                packageInfo.Version.ToString(),
                                IOUtils.SanitizeFileName(sample.displayName)
                            );
                        packageInfo.Samples.Add(new Sample(sample));
                    }
                }
                packages.Add(packageInfo);
            }

            return packages;
        }

        public static event Action<UpmBaseOperation> OnOperationStart = delegate {};

        public event Action<Error> OnOperationError = delegate {};
        public event Action OnOperationFinalized = delegate {};

        public Error ForceError { get; set; }                // Allow external component to force an error on the requests (eg: testing)
        public Error Error { get; protected set; }        // Keep last error

        public bool IsCompleted { get; private set; }

        protected abstract Request CreateRequest();

        [SerializeField]
        protected Request CurrentRequest;
        public readonly ThreadedDelay Delay = new ThreadedDelay();

        protected abstract void ProcessData();

        protected void Start()
        {
            Error = null;
            OnOperationStart(this);

            Delay.Start();

            if (TryForcedError())
                return;

            EditorApplication.update += Progress;
        }

        // Common progress code for all classes
        private void Progress()
        {
            if (!Delay.IsDone)
                return;

            // Create the request after the delay
            if (CurrentRequest == null)
            {
                try
                {
                    CurrentRequest = CreateRequest();
                }
                catch (Exception e)
                {
                    OnError(new Error(NativeErrorCode.Unknown, e.Message));
                    return;
                }
            }

            // Since CurrentRequest's error property is private, we need to simulate
            // an error instead of just setting it.
            if (TryForcedError())
                return;

            if (CurrentRequest.IsCompleted)
            {
                if (CurrentRequest.Status == StatusCode.Success)
                    OnDone();
                else if (CurrentRequest.Status >= StatusCode.Failure)
                    OnError(CurrentRequest.Error);
                else
                    Debug.LogError("Unsupported progress state " + CurrentRequest.Status);
            }
        }

        private void OnError(Error error)
        {
            try
            {
                Error = error;

                var message = "Cannot perform upm operation.";
                if (error != null)
                    message = "Cannot perform upm operation: " + Error.message + " [" + Error.errorCode + "]";

                Debug.LogError(message);

                OnOperationError(Error);
            }
            catch (Exception exception)
            {
                Debug.LogError("Package Manager Window had an error while reporting an error in an operation: " + exception);
            }

            FinalizeOperation();
        }

        private void OnDone()
        {
            try
            {
                ProcessData();
            }
            catch (Exception error)
            {
                Debug.LogError("Package Manager Window had an error while completing an operation: " + error);
            }

            FinalizeOperation();
        }

        private void FinalizeOperation()
        {
            EditorApplication.update -= Progress;
            OnOperationFinalized();
            IsCompleted = true;
        }

        public void Cancel()
        {
            EditorApplication.update -= Progress;
            OnOperationError = delegate {};
            OnOperationFinalized = delegate {};
            IsCompleted = true;
        }

        private bool TryForcedError()
        {
            if (ForceError != null)
            {
                OnError(ForceError);
                return true;
            }

            return false;
        }
    }
}
