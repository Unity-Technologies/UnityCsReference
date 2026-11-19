// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Properties
{
    /// <summary>
    /// Base class to implement a visitor responsible for getting an object's concrete type as a generic.
    /// </summary>
    /// <remarks>
    /// It is required that the visited object is a container type with a property bag.
    /// </remarks>
    public abstract class ConcreteTypeVisitor : IPropertyBagVisitor
    {
        /// <summary>
        /// Implement this method to receive the strongly typed callback for a given container.
        /// </summary>
        /// <param name="container">The reference to the container.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        protected abstract void VisitContainer<TContainer>(ref TContainer container);

        void IPropertyBagVisitor.Visit<TContainer>(IPropertyBag<TContainer> properties, ref TContainer container)
            => VisitContainer(ref container);
    }
}
