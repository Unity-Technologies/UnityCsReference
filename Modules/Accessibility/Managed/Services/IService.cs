// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Accessibility
{
    internal interface IService
    {
        /// <summary>
        /// Sets up everything the service needs to work.
        /// </summary>
        void Start();

        /// <summary>
        /// Tears the service down without needing to create a new service.
        /// </summary>
        void Stop();
    }
}
