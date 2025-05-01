// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering
{
    public static class RenderPipelineGraphicsSettingsExtensions
    {
        public static void SetValueAndNotify<T>(this IRenderPipelineGraphicsSettings settings, ref T currentPropertyValue, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (GraphicsSettings.s_PropertyHelper.SetProperty(settings, ref currentPropertyValue, newValue, propertyName))
            {
                GraphicsSettings.CallOnIRenderPipelineGraphicsSettingsChange(settings, propertyName);
                GraphicsSettings.SetDirtyRenderPipelineGlobalSettingsContaining(settings);
            }
        }

        public static void NotifyValueChanged(this IRenderPipelineGraphicsSettings settings, [CallerMemberName] string propertyName = null)
        {
            GraphicsSettings.s_PropertyHelper.NotifyValueChange(settings, propertyName);
            GraphicsSettings.CallOnIRenderPipelineGraphicsSettingsChange(settings, propertyName);
            GraphicsSettings.SetDirtyRenderPipelineGlobalSettingsContaining(settings);
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RecreatePipelineOnChangeAttribute : Attribute { }
}
