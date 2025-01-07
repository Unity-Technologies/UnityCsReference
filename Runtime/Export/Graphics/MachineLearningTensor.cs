// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Rendering
{
    public partial struct MachineLearningTensorShape
    {
#pragma warning disable CS0169
        public UInt32 rank;
        public UInt32 D0;
        public UInt32 D1;
        public UInt32 D2;
        public UInt32 D3;
        public UInt32 D4;
        public UInt32 D5;
        public UInt32 D6;
        public UInt32 D7;
#pragma warning restore CS0169
    }
    public partial struct MachineLearningTensorDescriptor
    {
        internal bool hasValue;
        public MachineLearningDataType dataType;
        public MachineLearningTensorShape shape;

        public MachineLearningTensorDescriptor(MachineLearningDataType dataType, MachineLearningTensorShape shape)
        {
            this.hasValue = true;
            this.dataType = dataType;
            this.shape = shape;
        }

        public static MachineLearningTensorDescriptor NullTensor()
        {
            return new MachineLearningTensorDescriptor
            {
                hasValue = false,
                dataType = MachineLearningDataType.Float32,
                shape = new MachineLearningTensorShape()
            };
        }
    }
}
