// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Properties
{
    public static partial class PropertyBag
    {
        /// <summary>
        /// Accepts visitation for the given property bag and tries to invoke the most specialized visitor first.
        /// </summary>
        /// <param name="properties">The property bag to visit.</param>
        /// <param name="visitor">The visitor or specialized visitor to invoke.</param>
        /// <param name="container">The container being visited.</param>
        /// <typeparam name="TContainer">The container type.</typeparam>
        /// <exception cref="ArgumentNullException">The given property bag is null.</exception>
        public static void AcceptWithSpecializedVisitor<TContainer>(IPropertyBag<TContainer> properties, IPropertyBagVisitor visitor, ref TContainer container)
        {
            if (null == properties)
                throw new ArgumentNullException(nameof(properties));

            switch (properties)
            {
                case IDictionaryPropertyBagAccept<TContainer> accept when visitor is IDictionaryPropertyBagVisitor typedVisitor:
                    accept.Accept(typedVisitor, ref container);
                    break;
                case IListPropertyBagAccept<TContainer> accept when visitor is IListPropertyBagVisitor typedVisitor:
                    accept.Accept(typedVisitor, ref container);
                    break;
                case ISetPropertyBagAccept<TContainer> accept when visitor is ISetPropertyBagVisitor typedVisitor:
                    accept.Accept(typedVisitor, ref container);
                    break;
                case ICollectionPropertyBagAccept<TContainer> accept when visitor is ICollectionPropertyBagVisitor typedVisitor:
                    accept.Accept(typedVisitor, ref container);
                    break;
                default:
                    properties.Accept(visitor, ref container);
                    break;
            }
        }
    }
}
