// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.XR.WSA
{
    partial class HolographicSettings
    {
        [Obsolete("Support for toggling latent frame presentation has been removed", true)]
        static public void ActivateLatentFramePresentation(bool activated)
        {
        }

        [Obsolete("Support for toggling latent frame presentation has been removed, and IsLatentFramePresentation will always return true", false)]
        static public bool IsLatentFramePresentation
        {
            get
            {
                return true;
            }
        }
    }
}
