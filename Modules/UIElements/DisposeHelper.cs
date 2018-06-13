// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    /// <summary>
    /// Some classes that implement the IDisposable interface expect Dispose to be called. When this happens,
    /// <c>GC.SuppressFinalize</c> is called, preventing the finalizer from running. In these cases, the finalizer
    /// can be used as a way to detect situations where Dispose has not been called. This class allows to centralize
    /// the processing that we want to apply when that happens.
    /// </summary>
    /// <remarks>
    /// Remember that finalizers incur a significant performance hit and should be avoided. Unless they actually
    /// perform disposal logic, the finalizers should be surrounded with #if UNITY_UIELEMENTS_DEBUG_DISPOSE .. #endif.
    ///
    /// The UNITY_UIELEMENTS_DEBUG_DISPOSE compilation symbol should normally not be defined, especially during tests because it
    /// could lead to instability: the GC is not called between each test and is not deterministic anyway, so
    /// finalizers can run any time, potentially causing error messages to be logged in tests that did not instantiate
    /// the disposable objects that have not been disposed, causing those tests to fail due to unexpected log messages.
    /// </remarks>
    internal class DisposeHelper
    {
        [System.Diagnostics.Conditional("UNITY_UIELEMENTS_DEBUG_DISPOSE")]
        public static void NotifyMissingDispose(System.IDisposable disposable)
        {
            if (disposable == null)
                return;

            Debug.LogError($"An IDisposable instance of type '{disposable.GetType().FullName}' has not been disposed.");
        }
    }
}
