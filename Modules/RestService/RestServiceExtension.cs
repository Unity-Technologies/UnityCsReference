// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.RestService
{
    [InitializeOnLoad]
    internal class RestServiceRegistration
    {
        static RestServiceRegistration()
        {
            OpenDocumentsRestHandler.Register();
            ProjectStateRestHandler.Register();
            AssetRestHandler.Register();
            PairingRestHandler.Register();
            PlayModeRestHandler.Register();
        }
    }
}
