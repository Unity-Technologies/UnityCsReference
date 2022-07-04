// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties.Internal;

namespace Unity.Properties
{
    /// <summary>
    /// Context object used during visitation when a <see cref="IProperty{TContainer}"/> is visited.
    /// </summary>
    /// <typeparam name="TContainer">The container type of the <see cref="IProperty"/>.</typeparam>
    /// <typeparam name="TValue">The value type of the <see cref="IProperty"/>.</typeparam>
    public readonly struct VisitContext<TContainer, TValue>
    {
        internal static VisitContext<TContainer, TValue> FromProperty(
            PropertyVisitor visitor,
            ReadOnlyAdapterCollection.Enumerator enumerator,
            Property<TContainer, TValue> property)
        {
            return new VisitContext<TContainer, TValue>(visitor, enumerator, property);
        }

        readonly ReadOnlyAdapterCollection.Enumerator m_Enumerator;
        readonly PropertyVisitor m_Visitor;

        /// <summary>
        /// The property being visited.
        /// </summary>
        public Property<TContainer, TValue> Property { get; }

        VisitContext(
            PropertyVisitor visitor,
            ReadOnlyAdapterCollection.Enumerator enumerator,
            Property<TContainer, TValue> property)
        {
            m_Visitor = visitor;
            m_Enumerator = enumerator;
            Property = property;
        }

        /// <summary>
        /// Continues visitation through the next visitation adapter.
        /// </summary>
        /// <param name="container">The container being visited.</param>
        /// <param name="value">The value being visited.</param>
        public void ContinueVisitation(ref TContainer container, ref TValue value)
        {
            m_Visitor.ContinueVisitation(Property, m_Enumerator, ref container, ref value);
        }

        /// <summary>
        /// Continues visitation while skipping the next visitation adapters.
        /// </summary>
        /// <param name="container">The container being visited.</param>
        /// <param name="value">The value being visited.</param>
        public void ContinueVisitationWithoutAdapters(ref TContainer container, ref TValue value)
        {
            m_Visitor.ContinueVisitationWithoutAdapters(Property, m_Enumerator, ref container, ref value);
        }
    }

    /// <summary>
    /// Context object used during visitation when a <see cref="IProperty{TContainer}"/> is visited.
    /// </summary>
    /// <typeparam name="TContainer">The container type of the <see cref="IProperty"/>.</typeparam>
    public readonly struct VisitContext<TContainer>
    {
        delegate void VisitDelegate(PropertyVisitor visitor, ReadOnlyAdapterCollection.Enumerator enumerator, IProperty<TContainer> property, ref TContainer container);
        delegate void VisitWithoutAdaptersDelegate(PropertyVisitor visitor, IProperty<TContainer> property, ref TContainer container);

        internal static VisitContext<TContainer> FromProperty<TValue>(
            PropertyVisitor visitor,
            ReadOnlyAdapterCollection.Enumerator enumerator,
            Property<TContainer, TValue> property)
        {
            return new VisitContext<TContainer>(visitor, enumerator, property, (PropertyVisitor v, ReadOnlyAdapterCollection.Enumerator e, IProperty<TContainer> p, ref TContainer c) =>
            {
                var typedProperty = (Property<TContainer, TValue>) p;
                var value = typedProperty.GetValue(ref c);
                v.ContinueVisitation(typedProperty, e, ref c, ref value);
            }, (PropertyVisitor v, IProperty<TContainer> p, ref TContainer c) =>
            {
                var typedProperty = (Property<TContainer, TValue>) p;
                var value = typedProperty.GetValue(ref c);
                v.ContinueVisitation(typedProperty, ref c, ref value);
            });
        }

        readonly ReadOnlyAdapterCollection.Enumerator m_Enumerator;
        readonly PropertyVisitor m_Visitor;
        readonly VisitDelegate m_Continue;
        readonly VisitWithoutAdaptersDelegate m_ContinueWithoutAdapters;

        /// <summary>
        /// The property being visited.
        /// </summary>
        public IProperty<TContainer> Property { get; }

        VisitContext(
            PropertyVisitor visitor,
            ReadOnlyAdapterCollection.Enumerator enumerator,
            IProperty<TContainer> property,
            VisitDelegate continueVisitation,
            VisitWithoutAdaptersDelegate continueVisitationWithoutAdapters)
        {
            m_Visitor = visitor;
            m_Enumerator = enumerator;
            Property = property;
            m_Continue = continueVisitation;
            m_ContinueWithoutAdapters = continueVisitationWithoutAdapters;
        }

        /// <summary>
        /// Continues visitation through the next visitation adapter.
        /// </summary>
        /// <param name="container">The container being visited.</param>
        public void ContinueVisitation(ref TContainer container)
        {
            m_Continue(m_Visitor, m_Enumerator, Property, ref container);
        }

        /// <summary>
        /// Continues visitation while skipping the next visitation adapters.
        /// </summary>
        /// <param name="container">The container being visited.</param>
        public void ContinueVisitationWithoutAdapters(ref TContainer container)
        {
            m_ContinueWithoutAdapters(m_Visitor, Property, ref container);
        }
    }
}
