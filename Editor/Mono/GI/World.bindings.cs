// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.LightTransport
{
    public interface IWorld : IDisposable
    {
        // Functionality like AddInstance/RemoveInstance will be added in the future.
    }
    internal class WintermuteWorld : IWorld
    {
        private IntegrationContext integrationContext;

        public void Dispose()
        {
        }

        public IntegrationContext GetIntegrationContext()
        {
            return integrationContext;
        }
        public void SetIntegrationContext(IntegrationContext context)
        {
            integrationContext = context;
        }
    }
    public class RadeonRaysWorld : IWorld
    {
        private IntegrationContext integrationContext;

        public void Dispose()
        {
        }

        public IntegrationContext GetIntegrationContext()
        {
            return integrationContext;
        }
        public void SetIntegrationContext(IntegrationContext context)
        {
            integrationContext = context;
        }
    }
}
