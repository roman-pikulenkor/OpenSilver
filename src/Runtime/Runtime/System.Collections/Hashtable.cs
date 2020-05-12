#if BRIDGE

using System.Collections.Generic;

namespace System.Collections
{
    public class Hashtable : IDictionary
    {
        private Dictionary<object, object> _entries;

        #region Constructors

        public Hashtable()
        {
            this._entries = new Dictionary<object, object>(3);
        }

        public Hashtable(int capacity)
        {
            this._entries = new Dictionary<object, object>(capacity);
        }

        #endregion

        #region IDictionary

        public object this[object key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException("key");
                }
                if (this._entries.TryGetValue(key, out object value))
                {
                    return value;
                }
                return null;
            }

            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException("key");
                }
                this._entries[key] = value;
            }
        }

        public ICollection Keys
        {
            get
            {
                return this._entries.Keys;
            }
        }

        public ICollection Values
        {
            get
            {
                return this._entries.Values;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public int Count
        {
            get
            {
                return this._entries.Count;
            }
        }

        public object SyncRoot
        {
            get
            {
                return ((ICollection)this._entries).SyncRoot;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public void Add(object key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            if (this._entries.ContainsKey(key))
            {
                throw new ArgumentException("Can't add duplicated keys in hashtable !");
            }
            this._entries.Add(key, value);
        }

        public void Clear()
        {
            this._entries.Clear();
        }

        public bool Contains(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            return this._entries.ContainsKey(key);
        }

        public void CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new ArgumentException("Only single dimensional arrays are supported for the requested action.");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("Non-negative number required.");
            if (array.Length - arrayIndex < this.Count)
                throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");
            ((ICollection)this._entries).CopyTo(array, arrayIndex);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return this._entries.GetEnumerator();
        }

        public void Remove(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            this._entries.Remove(key);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._entries.GetEnumerator();
        }

        #endregion
    }
}
#endif