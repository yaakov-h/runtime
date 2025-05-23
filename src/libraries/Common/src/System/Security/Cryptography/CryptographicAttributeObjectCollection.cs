// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
#if BUILDING_PKCS
    public
#else
    #pragma warning disable CA1510, CA1512
    internal
#endif
    sealed class CryptographicAttributeObjectCollection : ICollection
    {
        public CryptographicAttributeObjectCollection()
        {
            _list = new List<CryptographicAttributeObject>();
        }

        public CryptographicAttributeObjectCollection(CryptographicAttributeObject attribute)
        {
            _list = new List<CryptographicAttributeObject>();
            _list.Add(attribute);
        }

        public int Add(AsnEncodedData asnEncodedData)
        {
            ArgumentNullException.ThrowIfNull(asnEncodedData);

            return Add(new CryptographicAttributeObject(asnEncodedData.Oid!, new AsnEncodedDataCollection(asnEncodedData)));
        }

        public int Add(CryptographicAttributeObject attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            //
            // Merge with existing attribute, if already existed, else add as new.
            //
            string? szOid1 = attribute.Oid.Value;
            for (int index = 0; index < _list.Count; index++)
            {
                CryptographicAttributeObject existing = _list[index];

                // To prevent caller to add the existing item into the collection again
                // Otherwise the merge will be an infinite loop
                if (object.ReferenceEquals(existing.Values, attribute.Values))
                    throw new InvalidOperationException(SR.InvalidOperation_DuplicateItemNotAllowed);

                string? szOid2 = existing.Oid.Value;
                if (string.Equals(szOid1, szOid2, StringComparison.OrdinalIgnoreCase))
                {
                    //
                    // Only allow one signing time, per RFC.
                    //
                    if (string.Equals(szOid1, Oids.SigningTime, StringComparison.OrdinalIgnoreCase))
                        throw new CryptographicException(SR.Cryptography_Pkcs9_MultipleSigningTimeNotAllowed);

                    foreach (AsnEncodedData asnEncodedData in attribute.Values)
                    {
                        existing.Values.Add(asnEncodedData);
                    }
                    return index;
                }
            }

            int indexOfNewItem = _list.Count;
            _list.Add(attribute);
            return indexOfNewItem;
        }

        internal void AddWithoutMerge(CryptographicAttributeObject attribute)
        {
            Debug.Assert(attribute != null);
            _list.Add(attribute);
        }

        public void Remove(CryptographicAttributeObject attribute)
        {
            ArgumentNullException.ThrowIfNull(attribute);

            _list.Remove(attribute);
        }

        public CryptographicAttributeObject this[int index]
        {
            get
            {
                return _list[index];
            }
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public object SyncRoot
        {
            get
            {
                return this;
            }
        }

        public CryptographicAttributeObjectEnumerator GetEnumerator()
        {
            return new CryptographicAttributeObjectEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new CryptographicAttributeObjectEnumerator(this);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLess);
            if (index > array.Length - Count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            for (int i = 0; i < Count; i++)
            {
                array.SetValue(this[i], index);
                index++;
            }
        }

        public void CopyTo(CryptographicAttributeObject[] array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLess);
            if (index > array.Length - Count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            _list.CopyTo(array, index);
        }

        private readonly List<CryptographicAttributeObject> _list;
    }
}
