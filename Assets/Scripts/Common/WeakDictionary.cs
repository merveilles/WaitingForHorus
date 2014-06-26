#region Author
/************************************************************************************************************
Author: EpixCode (Keven Poulin)
Website: http://www.EpixCode.com
GitHub: https://github.com/EpixCode
Twitter: https://twitter.com/EpixCode (@EpixCode)
LinkedIn: http://www.linkedin.com/in/kevenpoulin
************************************************************************************************************/
#endregion

#region Copyright
/************************************************************************************************************
Copyright (C) 2014 EpixCode

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute,
sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished 
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
************************************************************************************************************/
#endregion

#region Class Documentation
/************************************************************************************************************
Class Name:     WeakDictionary.cs
Namespace:      Com.EpixCode.Util.WeakReference.WeakDictionary
Type:           Util / Collection
Definition:
                [WeakDictionary] is an alternative to [ConditionalWeakTable] (Only available in .NET Framework 4 +) 
                for Unity. It gives the ability to index a dictionary with weak key objects reference that will automatically 
                be released if they are no longer used.
Example:
                WeakDictionary<object, string> myWeakDictionary = new WeakDictionary<object, string>();
                myWeakDictionary.Add(myObject, "myString");
************************************************************************************************************/
#endregion

#region Using
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
#endregion

namespace Com.EpixCode.Util.WeakReference.WeakDictionary
{
    internal class HashableWeakRef<T> : IEquatable<HashableWeakRef<T>>, IDisposable where T : class
    {
        private System.WeakReference _weakReference;
        private int _hashcode;

        public T Target 
        { 
            get 
            { 
                return (T)_weakReference.Target; 
            } 
        }

        public HashableWeakRef(T aTarget)
        {
            if (aTarget != null)
            {
                _weakReference = new System.WeakReference(aTarget);
                _hashcode = aTarget.GetHashCode();
            }
            else
            {
                throw new InvalidOperationException("Can't create HashableWeakRef out of a null reference!");
            }
        }

        public override int GetHashCode()
        {
            return _hashcode;
        }

        public override bool Equals(object aObj)
        {
            if (_weakReference != null)
            {
                return this.Target.Equals(aObj);
            }
            return false;
        }

        public bool Equals(HashableWeakRef<T> aObj)
        {
            if (_weakReference != null)
            {
                return this.Target.Equals(aObj);
            }
            return false;
        }

        public static bool operator ==(HashableWeakRef<T> aFirstRef, HashableWeakRef<T> aSecondRef)
        {
            return aFirstRef.Equals(aSecondRef);
        }

        public static bool operator !=(HashableWeakRef<T> aFirstRef, HashableWeakRef<T> aSecondRef)
        {
            return !aFirstRef.Equals(aSecondRef);
        }

        public bool IsAlive
        {
            get
            {
                return (bool)(_weakReference != null && _weakReference.IsAlive && !_weakReference.Target.Equals(null));
            }
        }

        public void Dispose()
        {
            if(_weakReference != null && !_weakReference.Target.Equals(null))
            {
                if(_weakReference.Target is IDisposable)
                {
                    (_weakReference.Target as IDisposable).Dispose();
                }
            }
        }
    }

    public class WeakDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, ICollection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>, IEnumerable 
            where TKey : class
    {
        /*********************************************************
        * Member Variables
        *********************************************************/
        private IDictionary<object, TValue> _refDict = new Dictionary<object, TValue>();
        private bool _isAutoCean = true;
        
        /*********************************************************
        * Accessors / Mutators
        *********************************************************/
        public int Count
        {
            get
            {
                return _refDict.Count;
            }
        }

        public bool IsReadOnly
        {
            get 
            { 
                return _refDict.IsReadOnly; 
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                ICollection<TKey> keyList = new List<TKey>();
                foreach (KeyValuePair<TKey, TValue> entry in this)
                {
                    keyList.Add(entry.Key);
                }
                return keyList;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                return _refDict.Values;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                return _refDict[key];
            }

            set
            {
                _refDict[key] = value;
            }
        }

        public bool IsAutoClean
        {
            get
            {
                return _isAutoCean;
            }
            set
            {
                _isAutoCean = value;
            }
        }

        /**********************************************************
        * Explicit Methods
        *********************************************************/
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            return _refDict.Remove(key);
        }

        /**********************************************************
        * Public Methods
        *********************************************************/
        public void Add(TKey aKey, TValue aValue)
        {
            HashableWeakRef<TKey> weakKey = new HashableWeakRef<TKey>(aKey);
            _refDict.Add(weakKey, aValue);
        }

        public void Add(KeyValuePair<TKey, TValue> aItem)
        {
            Add(aItem.Key, aItem.Value);
        }

        public bool Remove(TKey aKey)
        {
            return  _refDict.Remove(aKey);
        }

        public bool Remove(KeyValuePair<TKey, TValue> aItem)
        {
            return Remove(aItem.Key);
        }

        public bool ContainsKey(TKey aKey)
        {
            return _refDict.ContainsKey(aKey);
        }

        public bool Contains(KeyValuePair<TKey, TValue> aItem)
        {
            return _refDict.ContainsKey(aItem.Key);
        }

        public void Clear()
        {
            _refDict.Clear();
        }

        public bool Clean()
        {
            List<HashableWeakRef<TKey>> weakKeyListToRemove = new List<HashableWeakRef<TKey>>();
            bool success = false;

            foreach (KeyValuePair<object, TValue> entry in _refDict)
            {
                HashableWeakRef<TKey> weakKey = (HashableWeakRef<TKey>)entry.Key;
                if (!weakKey.IsAlive)
                {
                    weakKeyListToRemove.Add(weakKey);
                }
            }

            for (int i = 0; i < weakKeyListToRemove.Count; i++)
            {
                Clean(weakKeyListToRemove[i]);
                success = true;
            }

            return success;
        }

        private bool Clean(HashableWeakRef<TKey> aKey)
        {
            bool success = false;

            if (!aKey.IsAlive)
            {
                if (_refDict[aKey] is IDisposable)
                {
                    (_refDict[aKey] as IDisposable).Dispose();
                }
                _refDict.Remove(aKey);
                success = true;
            }

            return success;
        }

        public bool TryGetValue(TKey aKey, out TValue aValue)
        {
            if (ContainsKey(aKey))
            {
                aValue = this[aKey];
                return true;
            }

            aValue = default(TValue);
            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] aArray, int aIndex)
        {
            foreach (KeyValuePair<object, TValue> entry in _refDict)
            {
                KeyValuePair<TKey, TValue> pair = new KeyValuePair<TKey, TValue>(((HashableWeakRef<TKey>)entry.Key).Target, entry.Value);
                aArray.SetValue(pair, aIndex);
                aIndex = aIndex + 1;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (IsAutoClean)
            {
                Clean();
            }

            foreach (KeyValuePair<object, TValue> entry in _refDict)
            {
                HashableWeakRef<TKey> weakKey = (HashableWeakRef<TKey>)entry.Key;
                yield return new KeyValuePair<TKey, TValue>(weakKey.Target, entry.Value);
            }
        }
    }
}