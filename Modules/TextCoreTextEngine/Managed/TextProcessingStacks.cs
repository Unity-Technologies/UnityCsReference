// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;

namespace UnityEngine.TextCore.Text
{
    /// <summary>
    /// Structure used to track basic XML tags which are binary (on / off)
    /// </summary>
    internal struct FontStyleStack
    {
        public byte bold;
        public byte italic;
        public byte underline;
        public byte strikethrough;
        public byte highlight;
        public byte superscript;
        public byte subscript;
        public byte uppercase;
        public byte lowercase;
        public byte smallcaps;

        /// <summary>
        /// Clear the basic XML tag stack.
        /// </summary>
        public void Clear()
        {
            bold = 0;
            italic = 0;
            underline = 0;
            strikethrough = 0;
            highlight = 0;
            superscript = 0;
            subscript = 0;
            uppercase = 0;
            lowercase = 0;
            smallcaps = 0;
        }

        public byte Add(FontStyles style)
        {
            switch (style)
            {
                case FontStyles.Bold:
                    bold++;
                    return bold;
                case FontStyles.Italic:
                    italic++;
                    return italic;
                case FontStyles.Underline:
                    underline++;
                    return underline;
                case FontStyles.UpperCase:
                    uppercase++;
                    return uppercase;
                case FontStyles.LowerCase:
                    lowercase++;
                    return lowercase;
                case FontStyles.Strikethrough:
                    strikethrough++;
                    return strikethrough;
                case FontStyles.Superscript:
                    superscript++;
                    return superscript;
                case FontStyles.Subscript:
                    subscript++;
                    return subscript;
                case FontStyles.Highlight:
                    highlight++;
                    return highlight;
            }

            return 0;
        }

        public byte Remove(FontStyles style)
        {
            switch (style)
            {
                case FontStyles.Bold:
                    if (bold > 1)
                        bold--;
                    else
                        bold = 0;
                    return bold;
                case FontStyles.Italic:
                    if (italic > 1)
                        italic--;
                    else
                        italic = 0;
                    return italic;
                case FontStyles.Underline:
                    if (underline > 1)
                        underline--;
                    else
                        underline = 0;
                    return underline;
                case FontStyles.UpperCase:
                    if (uppercase > 1)
                        uppercase--;
                    else
                        uppercase = 0;
                    return uppercase;
                case FontStyles.LowerCase:
                    if (lowercase > 1)
                        lowercase--;
                    else
                        lowercase = 0;
                    return lowercase;
                case FontStyles.Strikethrough:
                    if (strikethrough > 1)
                        strikethrough--;
                    else
                        strikethrough = 0;
                    return strikethrough;
                case FontStyles.Highlight:
                    if (highlight > 1)
                        highlight--;
                    else
                        highlight = 0;
                    return highlight;
                case FontStyles.Superscript:
                    if (superscript > 1)
                        superscript--;
                    else
                        superscript = 0;
                    return superscript;
                case FontStyles.Subscript:
                    if (subscript > 1)
                        subscript--;
                    else
                        subscript = 0;
                    return subscript;
            }

            return 0;
        }
    }


    /// <summary>
    /// Structure used to track XML tags of various types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Item count = {m_Count}")]
    internal struct TextProcessingStack<T>
    {
        public T[] itemStack;
        public int index;

        T m_DefaultItem;
        int m_Capacity;
        int m_RolloverSize;
        int m_Count;

        const int k_DefaultCapacity = 4;


        /// <summary>
        /// Constructor to create a new item stack.
        /// </summary>
        /// <param name="stack"></param>
        public TextProcessingStack(T[] stack)
        {
            itemStack = stack;
            m_Capacity = stack.Length;
            index = 0;
            m_RolloverSize = 0;

            m_DefaultItem = default;
            m_Count = 0;
        }

        /// <summary>
        /// Constructor for a new item stack with the given capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public TextProcessingStack(int capacity)
        {
            itemStack = new T[capacity];
            m_Capacity = capacity;
            index = 0;
            m_RolloverSize = 0;

            m_DefaultItem = default;
            m_Count = 0;
        }

        public TextProcessingStack(int capacity, int rolloverSize)
        {
            itemStack = new T[capacity];
            m_Capacity = capacity;
            index = 0;
            m_RolloverSize = rolloverSize;

            m_DefaultItem = default;
            m_Count = 0;
        }

        /// <summary>
        ///
        /// </summary>
        public int Count
        {
            get { return m_Count; }
        }


        /// <summary>
        /// Returns the current item on the stack.
        /// </summary>
        public T current
        {
            get
            {
                if (index > 0)
                    return itemStack[index - 1];

                return itemStack[0];
            }
        }


        /// <summary>
        ///
        /// </summary>
        public int rolloverSize
        {
            get { return m_RolloverSize; }
            set
            {
                m_RolloverSize = value;

                //if (m_Capacity < m_RolloverSize)
                //    Array.Resize(ref itemStack, m_RolloverSize);
            }
        }


        /// <summary>
        /// Function to clear and reset stack to first item.
        /// </summary>
        public void Clear()
        {
            index = 0;
            m_Count = 0;
        }

        /// <summary>
        /// Function to set the first item on the stack and reset index.
        /// </summary>
        /// <param name="item"></param>
        public void SetDefault(T item)
        {
            if (itemStack == null)
            {
                m_Capacity = k_DefaultCapacity;
                itemStack = new T[m_Capacity];
                m_DefaultItem = default;
            }

            itemStack[0] = item;
            index = 1;
        }

        /// <summary>
        /// Function to add a new item to the stack.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            if (index < itemStack.Length)
            {
                itemStack[index] = item;
                index += 1;
            }
        }

        /// <summary>
        /// Function to retrieve an item from the stack.
        /// </summary>
        /// <returns></returns>
        public T Remove()
        {
            index -= 1;

            if (index <= 0)
            {
                index = 1;
                return itemStack[0];
            }

            return itemStack[index - 1];
        }

        public void Push(T item)
        {
            if (index == m_Capacity)
            {
                m_Capacity *= 2;
                if (m_Capacity == 0)
                    m_Capacity = k_DefaultCapacity;

                Array.Resize(ref itemStack, m_Capacity);
            }

            itemStack[index] = item;

            if (m_RolloverSize == 0)
            {
                index += 1;
                m_Count += 1;
            }
            else
            {
                index = (index + 1) % m_RolloverSize;
                m_Count = m_Count < m_RolloverSize ? m_Count + 1 : m_RolloverSize;
            }
        }

        public T Pop()
        {
            if (index == 0 && m_RolloverSize == 0)
                return default;

            if (m_RolloverSize == 0)
                index -= 1;
            else
            {
                index = (index - 1) % m_RolloverSize;
                index = index < 0 ? index + m_RolloverSize : index;
            }

            T item = itemStack[index];
            itemStack[index] = m_DefaultItem;

            m_Count = m_Count > 0 ? m_Count - 1 : 0;

            return item;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            if (index == 0)
                return m_DefaultItem;

            return itemStack[index - 1];
        }

        /// <summary>
        /// Function to retrieve the current item from the stack.
        /// </summary>
        /// <returns>itemStack <T></returns>
        public T CurrentItem()
        {
            if (index > 0)
                return itemStack[index - 1];

            return itemStack[0];
        }

        /// <summary>
        /// Function to retrieve the previous item without affecting the stack.
        /// </summary>
        /// <returns></returns>
        public T PreviousItem()
        {
            if (index > 1)
                return itemStack[index - 2];

            return itemStack[0];
        }
    }
}
