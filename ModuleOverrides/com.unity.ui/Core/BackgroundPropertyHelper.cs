// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Helper to convert between background properties and ScaleMode.
    /// </summary>
    public static class BackgroundPropertyHelper
    {
        /// <summary>
        /// Converts ScaleMode to the equivalent BackgroundPosition property.
        /// </summary>
        /// <param name="scaleMode">The ScaleMode to convert.</param>
        /// <returns>BackgroundPosition.</returns>
        public static BackgroundPosition ConvertScaleModeToBackgroundPosition(ScaleMode scaleMode = ScaleMode.StretchToFill)
        {
            return new BackgroundPosition(BackgroundPositionKeyword.Center);
        }

        /// <summary>
        /// Converts ScaleMode to the equivalent BackgroundRepeat property.
        /// </summary>
        /// <param name="scaleMode">The ScaleMode to convert.</param>
        /// <returns>BackgroundRepeat.</returns>
        public static BackgroundRepeat ConvertScaleModeToBackgroundRepeat(ScaleMode scaleMode = ScaleMode.StretchToFill)
        {
            return new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        }

        /// <summary>
        /// Converts ScaleMode to the equivalent BackgroundSize property.
        /// </summary>
        /// <param name="scaleMode">The ScaleMode to convert.</param>
        /// <returns>BackgroundSize.</returns>
        public static BackgroundSize ConvertScaleModeToBackgroundSize(ScaleMode scaleMode = ScaleMode.StretchToFill)
        {
            if (scaleMode == ScaleMode.ScaleAndCrop)
            {
                return new BackgroundSize(BackgroundSizeType.Cover);
            }
            else if (scaleMode == ScaleMode.ScaleToFit)
            {
                return new BackgroundSize(BackgroundSizeType.Contain);
            }
            else // ScaleMode.StretchToFill
            {
                return new BackgroundSize(Length.Percent(100.0f), Length.Percent(100.0f));
            }
        }

        /// <summary>
        /// Resolves the background properties to a valid ScaleMode.
        /// </summary>
        /// <param name="backgroundPositionX">The X BackgroundPosition to resolve.</param>
        /// <param name="backgroundPositionY">The Y BackgroundPosition to resolve.</param>
        /// <param name="backgroundRepeat">The BackgroundRepeat to resolve.</param>
        /// <param name="backgroundSize">The BackgroundSize to resolve.</param>
        /// <param name="valid">Indicates whether the background properties resolve to a valid ScaleMode.</param>
        /// <returns>ScaleMode.</returns>
        public static ScaleMode ResolveUnityBackgroundScaleMode(BackgroundPosition backgroundPositionX, BackgroundPosition backgroundPositionY,
            BackgroundRepeat backgroundRepeat, BackgroundSize backgroundSize, out bool valid)
        {

            if (backgroundPositionX == BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleAndCrop) &&
                backgroundPositionY == BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleAndCrop) &&
                backgroundRepeat == BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(ScaleMode.ScaleAndCrop) &&
                backgroundSize == BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.ScaleAndCrop))
            {
                valid = true;
                return ScaleMode.ScaleAndCrop;
            }
            else if (backgroundPositionX == BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleToFit) &&
                     backgroundPositionY == BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleToFit) &&
                     backgroundRepeat == BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(ScaleMode.ScaleToFit) &&
                     backgroundSize == BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.ScaleToFit))
            {
                valid = true;
                return ScaleMode.ScaleToFit;
            }
            else if (backgroundPositionX == BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.StretchToFill) &&
                     backgroundPositionY == BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.StretchToFill) &&
                     backgroundRepeat == BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(ScaleMode.StretchToFill) &&
                     backgroundSize == BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.StretchToFill))
            {
                valid = true;
                return ScaleMode.StretchToFill;
            }
            else
            {
                valid = false;
                return ScaleMode.StretchToFill;
            }
        }
    }
}
