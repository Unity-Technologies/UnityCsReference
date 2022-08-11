// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    internal class SetValueVisitor<TSrcValue> : PathVisitor
    {
        public static readonly UnityEngine.Pool.ObjectPool<SetValueVisitor<TSrcValue>> Pool = new UnityEngine.Pool.ObjectPool<SetValueVisitor<TSrcValue>>(() => new SetValueVisitor<TSrcValue>(), v => v.Reset());
        public TSrcValue Value;

        public ConversionRegistry ConversionRegistry { get; set; }

        public override void Reset()
        {
            base.Reset();
            Value = default;
            ConversionRegistry = default;
        }

        protected override void VisitPath<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container, ref TValue value)
        {
            if (property.IsReadOnly)
            {
                ReturnCode = VisitReturnCode.AccessViolation;
                return;
            }

            if (ConversionRegistry.ConverterCount > 0 &&
                UIConversion.TryConvert(ConversionRegistry, ref Value, out TValue local))
            {
                property.SetValue(ref container, local);
            }
            else if (UIConversion.TryConvert(ref Value, out TValue global))
            {
                property.SetValue(ref container, global);
            }
            else
            {
                ReturnCode = VisitReturnCode.InvalidCast;
            }
        }
    }
}
