using System;
using System.Reflection;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal class TextHandleFactory
    {
        /// <summary>
        /// DO NOT USE CreateEditorHandle directly, use GetEditorHandle to guarantee the creation of the right handle.
        /// </summary>
        internal static Func<ITextHandle> CreateEditorHandle;
        /// <summary>
        /// DO NOT USE CreateRuntimeHandle directly, use GetRuntimeHandle to guarantee the creation of the right handle.
        /// </summary>
        internal static Func<ITextHandle> CreateRuntimeHandle;

        public static ITextHandle GetEditorHandle()
        {
            if (CreateEditorHandle != null)
                return CreateEditorHandle();
            return TextNativeHandle.New();
        }

        public static ITextHandle GetRuntimeHandle()
        {
            if (CreateRuntimeHandle != null)
                return CreateRuntimeHandle();

            return TextCoreHandle.New();
        }
    }

    internal class TextDelegates
    {
        internal static Func<UnityEngine.Object, bool> IsFontAsset;
        internal static Func<Object> GetTextSettings;
        internal static Func<Object, Font> GetFont;
        internal static Func<VisualElement, TextCoreSettings> GetTextCoreSettingsForElement;
        internal static Func<int> GetIDGradientScale;
        internal static Action ImportDefaultTextSettings;
        internal static Action OnTextSettingsImported;
        internal static Func<bool> HasTextSettings;

        internal static bool IsFontAssetSafe(UnityEngine.Object obj)
        {
            if (IsFontAsset == null)
                return false;

            return IsFontAsset(obj);
        }

        internal static int GetIDGradientScaleSafe()
        {
            if (GetIDGradientScale == null)
                return Shader.PropertyToID("_GradientScale");

            return GetIDGradientScale();
        }

        internal static TextCoreSettings GetTextCoreSettingsForElementSafe(VisualElement ve)
        {
            if (GetTextCoreSettingsForElement == null)
                return new TextCoreSettings();

            return GetTextCoreSettingsForElement(ve);
        }
        

        
        //Inspector events when FontAsset changed
        internal static void RaiseTextAssetChange(Object font) => OnTextAssetChange?.Invoke(font);
        internal static event Action<Object> OnTextAssetChange;
    }
}
