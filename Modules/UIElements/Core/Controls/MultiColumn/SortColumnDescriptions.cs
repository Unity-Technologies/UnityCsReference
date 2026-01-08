// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Internal;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This represents a collection or SortColumnDescriptions in multi SortColumnDescription views.
    /// </summary>
    [UxmlObject]
    public partial class SortColumnDescriptions : ICollection<SortColumnDescription>
    {
        static readonly BindingId sortColumnDescriptionsProperty = nameof(sortColumnDescriptions);

        [ExcludeFromDocs, Serializable]
        public class UxmlSerializedData : UIElements.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(sortColumnDescriptions), "sort-column-descriptions"),
                }, false);
            }

            #pragma warning disable 649
            [SerializeReference, UxmlObjectReference] List<SortColumnDescription.UxmlSerializedData> sortColumnDescriptions;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags sortColumnDescriptions_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new SortColumnDescriptions();

            public override void Deserialize(object obj)
            {
                if (ShouldWriteAttributeValue(sortColumnDescriptions_UxmlAttributeFlags) && sortColumnDescriptions != null)
                {
                    var e = (SortColumnDescriptions)obj;
                    foreach (var scdData in sortColumnDescriptions)
                    {
                        var scd = (SortColumnDescription)scdData.CreateInstance();
                        scdData.Deserialize(scd);
                        e.Add(scd);
                    }
                }
            }
        }

        [SerializeField]
        private readonly IList<SortColumnDescription> m_Descriptions = new List<SortColumnDescription>();

        private IList<SortColumnDescription> sortColumnDescriptions => m_Descriptions;

        /// <summary>
        /// Event sent when the descriptions changed.
        /// </summary>
        internal event Action changed;

        /// <summary>
        /// Called when a property has changed.
        /// </summary>
        internal event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<SortColumnDescription> GetEnumerator()
        {
            return m_Descriptions.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds a sort description at the end of the collection.
        /// </summary>
        /// <param name="item">The sort description to add.</param>
        public void Add(SortColumnDescription item)
        {
            Insert(m_Descriptions.Count, item);
        }

        /// <summary>
        /// Removes all sort descriptions from the collection.
        /// </summary>
        public void Clear()
        {
            while (m_Descriptions.Count > 0)
            {
                Remove(m_Descriptions[0]);
            }
        }

        /// <summary>
        /// Determines whether the current collection contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the current collection.</param>
        /// <returns>Whether the item is in the collection or not.</returns>
        public bool Contains(SortColumnDescription  item)
        {
            return m_Descriptions.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the current collection to a Array, starting at the specified index.
        /// </summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="arrayIndex">The starting index.</param>
        public void CopyTo(SortColumnDescription[] array, int arrayIndex)
        {
            m_Descriptions.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the first occurence of a sort description from the collection.
        /// </summary>
        /// <param name="desc">The sort description to remove.</param>
        /// <returns>Whether it was removed or not.</returns>
        public bool Remove(SortColumnDescription desc)
        {
            if (desc == null)
                throw new ArgumentException("Cannot remove null description");

            if (m_Descriptions.Remove(desc))
            {
                desc.column = null;
                desc.propertyChanged -= OnDescriptionPropertyChanged;
                desc.changed -= OnDescriptionChanged;
                changed?.Invoke();
                NotifyPropertyChanged(sortColumnDescriptionsProperty);
                return true;
            }
            return false;
        }

        void OnDescriptionChanged(SortColumnDescription  desc)
        {
            changed?.Invoke();
        }

        void OnDescriptionPropertyChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            var desc = (SortColumnDescription)sender;
            var index = m_Descriptions.IndexOf(desc);
            if (index >= 0)
            {
                var fullPath = $"{sortColumnDescriptionsProperty}[{index}].{args.propertyName}";
                NotifyPropertyChanged(fullPath);
            }
        }

        /// <summary>
        /// Gets the number of sort descriptions in the collection.
        /// </summary>
        public int Count => m_Descriptions.Count;

        /// <summary>
        /// Gets a value indicating whether the collection is readonly.
        /// </summary>
        public bool IsReadOnly => m_Descriptions.IsReadOnly;

        /// <summary>
        /// Returns the index of the specified <see cref="SortColumnDescription"/> if it is contained in the collection; returns -1 otherwise.
        /// </summary>
        /// <param name="desc">The description to locate in the <see cref="SortColumnDescriptions"/>.</param>
        /// <returns>The index of the <see cref="SortColumnDescriptions"/> if found in the collection; otherwise, -1.</returns>
        public int IndexOf(SortColumnDescription desc)
        {
            return m_Descriptions.IndexOf(desc);
        }

        /// <summary>
        /// Inserts a sort description into the current instance at the specified index.
        /// </summary>
        /// <param name="index">Index to insert to.</param>
        /// <param name="desc">The sort description to insert.</param>
        public void Insert(int index, SortColumnDescription desc)
        {
            if (desc == null)
                throw new ArgumentException("Cannot insert null description");

            if (Contains(desc))
                throw new ArgumentException("Already contains this description");

            m_Descriptions.Insert(index, desc);
            desc.propertyChanged += OnDescriptionPropertyChanged;
            desc.changed += OnDescriptionChanged;
            changed?.Invoke();
            NotifyPropertyChanged(sortColumnDescriptionsProperty);
        }

        /// <summary>
        /// Removes the sort description at the specified index.
        /// </summary>
        /// <param name="index">The index of the sort description to remove.</param>
        public void RemoveAt(int index)
        {
            Remove(m_Descriptions[index]);
        }

        /// <summary>
        /// Returns the SortColumnDescription at the specified index.
        /// </summary>
        /// <param name="index">The index of the SortColumnDescription to locate.</param>
        /// <returns>The SortColumnDescription at the specified index.</returns>
        public SortColumnDescription this[int index]
        {
            get { return m_Descriptions[index]; }
        }

        void NotifyPropertyChanged(in BindingId property)
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }
    }
}
