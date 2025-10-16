// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    class SubPortCustomConstant : Constant
    {
        Delegate m_Getter;
        Delegate m_Setter;

        PortModel PortModel => (PortModel)OwnerModel;

        public SubPortCustomConstant(PortModel portModel, Delegate getter, Delegate setter)
        {
            OwnerModel = portModel;
            m_Getter = getter;
            m_Setter = setter;
        }

        /// <inheritdoc />
        public override object ObjectValue
        {
            get => m_Getter.DynamicInvoke();

            set => m_Setter.DynamicInvoke(value);
        }
        /// <inheritdoc />
        public override object DefaultValue => Type.IsValueType ? Activator.CreateInstance(m_Getter.Method.ReturnType) : null;

        /// <inheritdoc />
        public override Type Type => PortModel.PortDataType;

        /// <inheritdoc />
        public override Constant Clone()
        {
            return new SubPortCustomConstant(PortModel, m_Getter, m_Setter);
        }

        public void Set(Delegate getter, Delegate setter)
        {
            m_Getter = getter;
            m_Setter = setter;
        }
    }
}
