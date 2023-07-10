// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    internal class SetValueVisitor<TSrcValue> : PathVisitor
    {
        public static readonly UnityEngine.Pool.ObjectPool<SetValueVisitor<TSrcValue>> Pool = new (() => new SetValueVisitor<TSrcValue>(), v => v.Reset());
        public TSrcValue Value;

        public ConverterGroup group { get; set; }

        public override void Reset()
        {
            base.Reset();
            Value = default;
            group = default;
        }

        protected override void VisitPath<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container, ref TValue value)
        {
            if (property.IsReadOnly)
            {
                ReturnCode = VisitReturnCode.AccessViolation;
                return;
            }

            if (null != group && group.TryConvert(ref Value, out TValue local))
            {
                property.SetValue(ref container, local);
            }
            else if (ConverterGroups.TryConvert(ref Value, out TValue global))
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
