// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Connect;
using UnityEditor.Marketplace;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Asset Store entitlement claim tracker for build profile initialization. Claims run before the
    /// package install request, so a package gated behind an entitlement the user does not hold can
    /// still be installed.
    /// </summary>
    [Serializable]
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    class BuildProfileEntitlementClaimInfo
    {
        public Action OnClaimRequestComplete;
        public enum Status
        {
            Pending,
            Processing,
            Granted,
            Failed,
        }

        public enum Failure
        {
            None,

            /// <summary>
            /// The product endpoint has no product for the package. Expected for packages
            /// that are not Asset Store products or is missing from the endpoint's whitelist.
            /// </summary>
            NoProduct,

            Rejected,
            ServiceError,
            Transport,
            Timeout,
            NoOrganization,
        }

        [Serializable]
        public class Claim
        {
            public string packageName;

            /// <summary>
            /// Marketplace product ID to claim against. Empty until the entry is resolved.
            /// </summary>
            public string productId;

            /// <summary>
            /// Whether the package is classified (e.g., Asset Store vs. registry package).
            /// </summary>
            public bool isClassified;

            public Status status;
            public Failure failure;
            public string detail;
        }

        const int k_SessionTimeoutSeconds = 90;
        const int k_RetryDelaySeconds = 1;
        const int k_AttemptTimeoutSeconds = 30;

        /// <summary>
        /// Claims to make, one per package.
        /// </summary>
        [SerializeField]
        public Claim[] claims = Array.Empty<Claim>();

        /// <summary>
        /// Wall clock tick the claim session expires at.
        /// </summary>
        [SerializeField]
        internal long m_SessionTicks;

        internal long m_NextAttemptTicks;

        Task m_InFlightClaim;
        internal Task inFlightClaim => m_InFlightClaim;
        CancellationTokenSource m_Cancellation;

        bool m_CallbacksSubscribed;

        IPackageServiceInfoProvider m_Provider;

        /// <summary>
        /// Only a test-injected provider is stored. The shared one is read through on every access,
        /// because UAL0018 forbids capturing it into a field.
        /// </summary>
        internal IPackageServiceInfoProvider provider
        {
            get => m_Provider ?? BuildProfileContext.packageServiceInfoProvider;
            set => m_Provider = value;
        }

        internal static BuildProfileEntitlementClaimInfo Create(
            BuildTargetDiscovery.PlatformPackageIdentifier[] packagesToAdd,
            IPackageServiceInfoProvider providerOverride = null)
        {
            var provider = providerOverride ?? BuildProfileContext.packageServiceInfoProvider;
            var claims = new List<Claim>();

            foreach (var package in packagesToAdd)
            {
                var packageName = package.name;

                if (!provider.HasPackageInfo(packageName))
                {
                    claims.Add(new Claim { packageName = packageName });
                    continue;
                }

                if (!provider.IsAssetStorePackage(packageName))
                    continue;

                if (provider.HasMarketplaceEntitlement(packageName) == true)
                    continue;

                claims.Add(new Claim
                {
                    packageName = packageName,
                    productId = provider.GetMarketplaceProductId(packageName),
                    isClassified = true,
                });
            }

            return claims.Count == 0
                ? null
                : new BuildProfileEntitlementClaimInfo { claims = claims.ToArray(), provider = providerOverride };
        }

        /// <summary>
        /// Begins or resumes claiming entitlements.
        /// </summary>
        public void RequestEntitlementClaims()
        {
            if (m_CallbacksSubscribed || IsClaimRequestDone())
                return;

            if (m_SessionTicks == 0)
                m_SessionTicks = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(k_SessionTimeoutSeconds).Ticks;

            EditorApplication.update += ProcessClaims;
            AssemblyReloadEvents.beforeAssemblyReload += UnsubscribeRequestCallbacks;
            m_CallbacksSubscribed = true;
        }

        /// <summary>
        /// Check if all claim requests are completed.
        /// </summary>
        public bool IsClaimRequestDone() => NextOutstandingClaim() == null;

        /// <summary>
        /// Whether the package is known to be installable: the entitlement is held,
        /// the package is not an Asset Store product and so needs none, or it was never
        /// classified because the lookup that would have classified it failed.
        /// </summary>
        public bool IsPackageInstallable(string packageName)
        {
            foreach (var claim in claims)
            {
                if (claim.packageName != packageName)
                    continue;

                if (claim.status == Status.Granted)
                    return true;

                if (claim.status != Status.Failed)
                    return false;

                return claim.failure == Failure.NoProduct || !claim.isClassified;
            }

            return true;
        }

        public void Cleanup()
        {
            UnsubscribeRequestCallbacks();
            OnClaimRequestComplete = null;
        }

        /// <summary>
        /// Detaches the editor update loop and the domain-reload hook, and abandons any request in
        /// flight.
        /// </summary>
        void UnsubscribeRequestCallbacks()
        {
            if (!m_CallbacksSubscribed)
                return;

            m_CallbacksSubscribed = false;
            AssemblyReloadEvents.beforeAssemblyReload -= UnsubscribeRequestCallbacks;
            EditorApplication.update -= ProcessClaims;
            m_Cancellation?.Cancel();
        }

        internal void ProcessClaims()
        {
            var claim = NextOutstandingClaim();
            if (claim == null)
            {
                FinishClaimRequest();
                return;
            }

            // Checked before the deadline, so it cannot fail an entry a running request is about to
            // resolve.
            if (m_InFlightClaim != null)
                return;

            var now = DateTime.UtcNow.Ticks;
            if (now >= m_SessionTicks)
            {
                FailOutstandingClaims(Failure.Timeout);
                FinishClaimRequest();
                return;
            }

            if (now < m_NextAttemptTicks)
                return;

            var attempt = AttemptClaimAsync(claim);
            if (!attempt.IsCompleted)
                m_InFlightClaim = attempt;
        }

        void FinishClaimRequest()
        {
            UnsubscribeRequestCallbacks();
            OnClaimRequestComplete?.Invoke();
        }

        async Task AttemptClaimAsync(Claim claim)
        {
            m_Cancellation = new CancellationTokenSource();
            m_Cancellation.CancelAfter(TimeSpan.FromSeconds(k_AttemptTimeoutSeconds));

            var isClaimAttempt = !string.IsNullOrEmpty(claim.productId);

            try
            {
                if (isClaimAttempt)
                    await PostClaimAsync(claim, m_Cancellation.Token);
                else
                    await ResolveProductIdAsync(claim, m_Cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // Timed out, or aborted by a domain reload. Either way the entry is untouched, so the
                // next tick retries it until the deadline.
            }
            catch (NoOrganizationIdException exception)
            {
                FailOutstandingClaims(Failure.NoOrganization);
                Debug.LogError("Could not claim the Asset Store entitlements: " + exception.Message);
                FinishClaimRequest();
            }
            catch (MarketplaceApiException exception)
            {
                // Only a claim the service answered can be a refusal. A failed product lookup is the
                // metadata call and says nothing about entitlement.
                SetClaimFailed(claim, isClaimAttempt && exception.error.status < 500 ? Failure.Rejected : Failure.ServiceError);
                claim.detail = exception.Message;
                LogClaimFailure(claim);
            }
            catch (UnityConnectWebRequestException exception)
            {
                SetClaimFailed(claim, Failure.Transport);
                claim.detail = exception.Message;
                LogClaimFailure(claim);
            }
            catch (Exception exception)
            {
                SetClaimFailed(claim, Failure.ServiceError);
                claim.detail = exception.Message;
                LogClaimFailure(claim);
            }
            finally
            {
                m_Cancellation?.Dispose();
                m_Cancellation = null;
                m_InFlightClaim = null;
            }
        }

        async Task ResolveProductIdAsync(Claim claim, CancellationToken cancellationToken)
        {
            var productInfo = await AwaitOrAbandon(
                provider.GetOrFetchProductInfoAsync(claim.packageName), cancellationToken);

            claim.isClassified = true;

            if (productInfo == null || string.IsNullOrEmpty(productInfo.productId))
            {
                SetClaimFailed(claim, Failure.NoProduct);
                return;
            }

            if (productInfo.entitlement?.hasEntitlement == true)
            {
                claim.status = Status.Granted;
                return;
            }

            claim.productId = productInfo.productId;
        }

        async Task PostClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            var result = await AwaitOrAbandon(
                provider.ClaimEntitlementAsync(claim.productId), cancellationToken);

            switch (result.status)
            {
                case ClaimStatus.Granted:
                    claim.status = Status.Granted;
                    break;

                case ClaimStatus.Processing:
                    claim.status = Status.Processing;
                    m_NextAttemptTicks = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(k_RetryDelaySeconds).Ticks;
                    break;

                default:
                    SetClaimFailed(claim, Failure.ServiceError);
                    claim.detail = "The claim service answered with an unrecognized status.";
                    LogClaimFailure(claim);
                    break;
            }
        }

        /// <summary>
        /// Awaits a gateway request, or abandons it when cancelled. Those requests carry no cancellation
        /// token by design, because one may be shared with another caller. Hence, cancel here means
        /// leaving the request to finish for whoever else wants it.
        /// </summary>
        static async Task<T> AwaitOrAbandon<T>(Task<T> request, CancellationToken cancellationToken)
        {
            var cancelled = Task.Delay(Timeout.Infinite, cancellationToken);
            if (await Task.WhenAny(request, cancelled) != request)
            {
                // Once abandoned, nobody may be left to observe a failure on it.
                _ = request.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                throw new OperationCanceledException(cancellationToken);
            }

            return await request;
        }

        static bool IsOutstanding(Claim claim) => claim.status == Status.Pending || claim.status == Status.Processing;

        Claim NextOutstandingClaim()
        {
            foreach (var claim in claims)
            {
                if (IsOutstanding(claim))
                    return claim;
            }

            return null;
        }

        void FailOutstandingClaims(Failure failure)
        {
            foreach (var claim in claims)
            {
                if (!IsOutstanding(claim))
                    continue;

                SetClaimFailed(claim, failure);
                LogClaimFailure(claim);
            }
        }

        static void SetClaimFailed(Claim claim, Failure failure)
        {
            claim.status = Status.Failed;
            claim.failure = failure;
        }

        static void LogClaimFailure(Claim claim)
        {
            if (claim.failure == Failure.NoProduct || claim.failure == Failure.NoOrganization || !claim.isClassified)
                return;

            var reason = string.IsNullOrEmpty(claim.detail) ? claim.failure.ToString() : claim.detail;
            Debug.LogError($"Could not claim the Asset Store entitlement for '{claim.packageName}': {reason}");
        }
    }
}
