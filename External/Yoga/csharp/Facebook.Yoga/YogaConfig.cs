/**
 * Copyright (c) 2014-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */

using System;
using System.Runtime.InteropServices;


// BEGIN_UNITY @jonathanma 
// The facebook implementation use the YGConfig context to keep a mapping between the native and managed object.
// However, since we don't really use YogaConfig this has been removed to avoid pinning the object.
// END_UNITY
namespace UnityEngine.Yoga
{
    internal class YogaConfig
    {
        internal static readonly YogaConfig Default = new YogaConfig(Native.YGConfigGetDefault());

        private IntPtr _ygConfig;
        private Logger _logger;

        private YogaConfig(IntPtr ygConfig)
        {
            _ygConfig = ygConfig;
            if (_ygConfig == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to allocate native memory");
            }

            // BEGIN_UNITY @jonathanma RemoveContext
            //_ygConfig.SetContext(this);
            // END_UNITY

            // BEGIN_UNITY @jonathanma DisableLogger
            //if (_ygConfig == YGConfigHandle.Default)
            //{
                //_managedLogger = LoggerInternal;
                //Native.YGInteropSetLogger(_managedLogger);
            //}
            // END_UNITY
        }

        public YogaConfig()
          : this(Native.YGConfigNew())
        {
        }

        ~YogaConfig()
        {
            if (this.Handle != Default.Handle)
            {
                Native.YGConfigFree(this.Handle);
            }
        }

        internal IntPtr Handle
        {
            get {
                return _ygConfig;
            }
        }

    // BEGIN_UNITY @jonathanma Do not support C# logger
    // END_UNITY

    public Logger Logger
        {
            get {
                return _logger;
            }

            set {
                _logger = value;
            }
        }

        public void SetExperimentalFeatureEnabled(
            YogaExperimentalFeature feature,
            bool enabled)
        {
            Native.YGConfigSetExperimentalFeatureEnabled(_ygConfig, feature, enabled);
        }

        public bool IsExperimentalFeatureEnabled(YogaExperimentalFeature feature)
        {
            return Native.YGConfigIsExperimentalFeatureEnabled(_ygConfig, feature);
        }

        public bool UseWebDefaults
        {
            get
            {
                return Native.YGConfigGetUseWebDefaults(_ygConfig);
            }

            set
            {
                Native.YGConfigSetUseWebDefaults(_ygConfig, value);
            }
        }

        public float PointScaleFactor
        {
            set
            {
                Native.YGConfigSetPointScaleFactor(_ygConfig, value);
            }
        }

        public static int GetInstanceCount()
        {
            return Native.YGConfigGetInstanceCount();
        }

        public static void SetDefaultLogger(Logger logger)
        {
            Default.Logger = logger;
        }
    }
}
