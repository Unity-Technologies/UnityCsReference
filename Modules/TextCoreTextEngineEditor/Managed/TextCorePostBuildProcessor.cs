// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.TextCore.Text;
using UnityEditor.Build;

namespace UnityEditor.TextCore.Text
{
    // Pairs with TextCorePreBuildProcessor. IPostprocessBuildWithContext is used rather than
    // IPostprocessBuildWithReport because it also runs when the build fails or is cancelled.
    internal class TextCorePostBuildProcessor : IPostprocessBuildWithContext
    {
        public int callbackOrder { get { return 0; } }

        public void OnPostprocessBuild(BuildCallbackContext ctx)
        {
            FontAsset.OnBuildCompletedForAll();
        }
    }
}
