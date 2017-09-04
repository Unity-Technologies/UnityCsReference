// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEngine.XR.WSA
{
    public partial class WorldManager
    {
        [Obsolete("The option for toggling latent frame presentation has been removed, and is on for performance reasons. This property will be removed in a future release.", false)]
        static public bool IsLatentFramePresentation
        {
            get
            {
                return true;
            }
        }

        [Obsolete("The option for toggling latent frame presentation has been removed, and is on for performance reasons. This method will be removed in a future release.", false)]
        static public void ActivateLatentFramePresentation(bool activated)
        {
        }
    }
}
