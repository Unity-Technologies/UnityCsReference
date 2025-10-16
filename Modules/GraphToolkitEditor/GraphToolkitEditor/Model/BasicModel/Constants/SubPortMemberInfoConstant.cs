// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;

namespace Unity.GraphToolkit.Editor
{
    abstract class SubPortMemberInfoConstant : Constant
    {
        protected PortModel PortModel => (PortModel)OwnerModel;

        protected MemberInfo m_MemberInfo;

        public SubPortMemberInfoConstant(PortModel portModel, MemberInfo memberInfo)
        {
            OwnerModel = portModel;
            m_MemberInfo = memberInfo;
        }

        public void SetMemberInfo(MemberInfo memberInfo)
        {
            m_MemberInfo = memberInfo;
        }
    }


    class SubPortFieldInfoConstant : SubPortMemberInfoConstant
    {
        public SubPortFieldInfoConstant(PortModel portModel, FieldInfo memberInfo)
            : base(portModel, memberInfo) { }

        FieldInfo FieldInfo => (FieldInfo)m_MemberInfo;

        public override object ObjectValue
        {
            get => FieldInfo.GetValue(PortModel.ParentPort.EmbeddedValue.ObjectValue);

            set
            {
                var parentValue = PortModel.ParentPort.EmbeddedValue.ObjectValue;
                if (parentValue != null)
                {
                    FieldInfo.SetValue(parentValue, value);
                    OwnerModel?.GraphModel?.CurrentGraphChangeDescription.AddChangedModel(OwnerModel, ChangeHint.Data);
                    if (parentValue.GetType().IsValueType)
                    {
                        PortModel.ParentPort.EmbeddedValue.ObjectValue = parentValue;
                    }
                }
            }
        }

        public override object DefaultValue => FieldInfo.FieldType.IsValueType ? Activator.CreateInstance(FieldInfo.FieldType) : null;
        public override Type Type => FieldInfo.FieldType;
        public override Constant Clone()
        {
            return new SubPortFieldInfoConstant(PortModel, FieldInfo);
        }
    }

    class SubPortPropertyInfoConstant : SubPortMemberInfoConstant
    {
        public SubPortPropertyInfoConstant(PortModel portModel, PropertyInfo memberInfo)
            : base(portModel, memberInfo) { }

        PropertyInfo PropertyInfo => (PropertyInfo)m_MemberInfo;

        public override object ObjectValue
        {
            get => PropertyInfo.GetValue(PortModel.ParentPort.EmbeddedValue.ObjectValue);

            set
            {
                var parentValue = PortModel.ParentPort.EmbeddedValue.ObjectValue;
                if (parentValue != null)
                {
                    PropertyInfo.SetValue(parentValue, value);
                    OwnerModel?.GraphModel?.CurrentGraphChangeDescription.AddChangedModel(OwnerModel, ChangeHint.Data);
                    if (parentValue.GetType().IsValueType)
                        PortModel.ParentPort.EmbeddedValue.ObjectValue = parentValue;
                }
            }
        }

        public override object DefaultValue => PropertyInfo.PropertyType.IsValueType ? Activator.CreateInstance(PropertyInfo.PropertyType) : null;
        public override Type Type => PropertyInfo.PropertyType;
        public override Constant Clone()
        {
            return new SubPortPropertyInfoConstant(PortModel, PropertyInfo);
        }
    }
}
