#if BRIDGE

using System.Collections.Generic;

namespace System.Collections
{
    public class ArrayList : IList
    {
        private List<object> _list;

        #region Constructors

        // Constructs a ArrayList. The list is initially empty and has a capacity
        // of zero. Upon adding the first element to the list the capacity is
        // increased to _defaultCapacity, and then increased in multiples of two as required.
        public ArrayList()
        {
            _list = new List<object>();
        }

        // Constructs a ArrayList with a given initial capacity. The list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required.
        // 
        public ArrayList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity", "capacity can't be negative.");

            if (capacity == 0)
                _list = new List<object>(0);
            else
                _list = new List<object>(capacity);
        }

        // Constructs a ArrayList, copying the contents of the given collection. The
        // size and capacity of the new list will both be equal to the size of the
        // given collection.
        // 
        public ArrayList(ICollection c)
        {
            if (c == null)
                throw new ArgumentNullException("c");

            int count = c.Count;
            if (count == 0)
            {
                _list = new List<object>(0);
            }
            else
            {
                _list = new List<object>(c.Count);
                foreach (object item in c)
                {
                    _list.Add(item);
                }
            }
        }

        #endregion

        #region IList

        public object this[int index]
        {
            get
            {
                return _list[index];
            }

            set
            {
                _list[index] = value;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public object SyncRoot
        {
            get
            {
                return ((ICollection)_list).SyncRoot;
            }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public int Add(object value)
        {
            _list.Add(value);
            return _list.Count;
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(object value)
        {
            return _list.Contains(value);
        }

        public void CopyTo(Array array, int arrayIndex)
        {
            ((IList)_list).CopyTo(array, arrayIndex);
        }

        public IEnumerator GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(object value)
        {
            return _list.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            _list.Insert(index, value);
        }

        public void Remove(object value)
        {
            _list.Remove(value);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        #endregion
    }
}

#endif