//------------------------------------------------------------------------------
// <license file="UintMap.cs">
//     
//      The use and distribution terms for this software are contained in the file
//      named 'LICENSE', which can be found in the resources directory of this
//		distribution.
//
//      By using this software in any fashion, you are agreeing to be bound by the
//      terms of this license.
//     
// </license>                                                                
//------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace EcmaScript.NET.Collections
{

    /// <summary> Map to associate non-negative integers to objects or integers.
    /// The map does not synchronize any of its operation, so either use
    /// it from a single thread or do own synchronization or perform all mutation
    /// operations on one thread before passing the map to others.
    /// 
    /// </summary>

    public class UintMap
    {
        virtual public bool Empty
        {
            get
            {
                return keyCount == 0;
            }

        }
        /// <summary>Return array of present keys </summary>
        virtual public int [] Keys
        {
            get
            {
                int [] keys = this.keys;
                int n = keyCount;
                int [] result = new int [n];
                for (int i = 0; n != 0; ++i) {
                    int entry = keys [i];
                    if (entry != EMPTY && entry != DELETED) {
                        result [--n] = entry;
                    }
                }
                return result;
            }

        }

        // Map implementation via hashtable,
        // follows "The Art of Computer Programming" by Donald E. Knuth

        public UintMap ()
            : this (4)
        {
        }

        public UintMap (int initialCapacity)
        {
            if (initialCapacity < 0)
                Context.CodeBug ();
            // Table grow when number of stored keys >= 3/4 of max capacity
            int minimalCapacity = initialCapacity * 4 / 3;
            int i;
            for (i = 2; (1 << i) < minimalCapacity; ++i) {
            }
            power = i;
            if (check && power < 2)
                Context.CodeBug ();
        }

        public virtual int size ()
        {
            return keyCount;
        }

        public virtual bool has (int key)
        {
            if (key < 0)
                Context.CodeBug ();
            return 0 <= findIndex (key);
        }

        /// <summary> Get object value assigned with key.</summary>
        /// <returns> key object value or null if key is absent
        /// </returns>
        public virtual object getObject (int key)
        {
            if (key < 0)
                Context.CodeBug ();
            if (values != null) {
                int index = findIndex (key);
                if (0 <= index) {
                    return values [index];
                }
            }
            return null;
        }

        /// <summary> Get integer value assigned with key.</summary>
        /// <returns> key integer value or defaultValue if key is absent
        /// </returns>
        public virtual int getInt (int key, int defaultValue)
        {
            if (key < 0)
                Context.CodeBug ();
            int index = findIndex (key);
            if (0 <= index) {
                if (ivaluesShift != 0) {
                    return keys [ivaluesShift + index];
                }
                return 0;
            }
            return defaultValue;
        }

        /// <summary> Get integer value assigned with key.</summary>
        /// <returns> key integer value or defaultValue if key does not exist or does
        /// not have int value
        /// </returns>
        /// <throws>  RuntimeException if key does not exist </throws>
        public virtual int getExistingInt (int key)
        {
            if (key < 0)
                Context.CodeBug ();
            int index = findIndex (key);
            if (0 <= index) {
                if (ivaluesShift != 0) {
                    return keys [ivaluesShift + index];
                }
                return 0;
            }
            // Key must exist
            Context.CodeBug ();
            return 0;
        }

        /// <summary> Set object value of the key.
        /// If key does not exist, also set its int value to 0.
        /// </summary>
        public virtual void put (int key, object value)
        {
            if (key < 0)
                Context.CodeBug ();
            int index = ensureIndex (key, false);
            if (values == null) {
                values = new object [1 << power];
            }
            values [index] = value;
        }

        /// <summary> Set int value of the key.
        /// If key does not exist, also set its object value to null.
        /// </summary>
        public virtual void put (int key, int value)
        {
            if (key < 0)
                Context.CodeBug ();
            int index = ensureIndex (key, true);
            if (ivaluesShift == 0) {
                int N = 1 << power;
                // keys.length can be N * 2 after clear which set ivaluesShift to 0
                if (keys.Length != N * 2) {
                    int [] tmp = new int [N * 2];
                    Array.Copy (keys, 0, tmp, 0, N);
                    keys = tmp;
                }
                ivaluesShift = N;
            }
            keys [ivaluesShift + index] = value;
        }

        public virtual void remove (int key)
        {
            if (key < 0)
                Context.CodeBug ();
            int index = findIndex (key);
            if (0 <= index) {
                keys [index] = DELETED;
                --keyCount;
                // Allow to GC value and make sure that new key with the deleted
                // slot shall get proper default values
                if (values != null) {
                    values [index] = null;
                }
                if (ivaluesShift != 0) {
                    keys [ivaluesShift + index] = 0;
                }
            }
        }

        public virtual void clear ()
        {
            int N = 1 << power;
            if (keys != null) {
                for (int i = 0; i != N; ++i) {
                    keys [i] = EMPTY;
                }
                if (values != null) {
                    for (int i = 0; i != N; ++i) {
                        values [i] = null;
                    }
                }
            }
            ivaluesShift = 0;
            keyCount = 0;
            occupiedCount = 0;
        }

        private static int tableLookupStep (int fraction, int mask, int power)
        {
            int shift = 32 - 2 * power;
            if (shift >= 0) {
                return (((int)((uint)fraction >> shift)) & mask) | 1;
            }
            else {
                return (fraction & (int)((uint)mask >> -shift)) | 1;
            }
        }

        private int findIndex (int key)
        {
            int [] keys = this.keys;
            if (keys != null) {
                int fraction = key * A;
                int index = (int)((uint)fraction >> (32 - power));
                int entry = keys [index];
                if (entry == key) {
                    return index;
                }
                if (entry != EMPTY) {
                    // Search in table after first failed attempt
                    int mask = (1 << power) - 1;
                    int step = tableLookupStep (fraction, mask, power);
                    int n = 0;
                    do {
                        if (check) {
                            if (n >= occupiedCount)
                                Context.CodeBug ();
                            ++n;
                        }
                        index = (index + step) & mask;
                        entry = keys [index];
                        if (entry == key) {
                            return index;
                        }
                    }
                    while (entry != EMPTY);
                }
            }
            return -1;
        }

        // Insert key that is not present to table without deleted entries
        // and enough free space
        private int insertNewKey (int key)
        {
            if (check && occupiedCount != keyCount)
                Context.CodeBug ();
            if (check && keyCount == 1 << power)
                Context.CodeBug ();
            int [] keys = this.keys;
            int fraction = key * A;
            int index = (int)((uint)fraction >> (32 - power));
            if (keys [index] != EMPTY) {
                int mask = (1 << power) - 1;
                int step = tableLookupStep (fraction, mask, power);
                int firstIndex = index;
                do {
                    if (check && keys [index] == DELETED)
                        Context.CodeBug ();
                    index = (index + step) & mask;
                    if (check && firstIndex == index)
                        Context.CodeBug ();
                }
                while (keys [index] != EMPTY);
            }
            keys [index] = key;
            ++occupiedCount;
            ++keyCount;
            return index;
        }

        private void rehashTable (bool ensureIntSpace)
        {
            if (keys != null) {
                // Check if removing deleted entries would free enough space
                if (keyCount * 2 >= occupiedCount) {
                    // Need to grow: less then half of deleted entries
                    ++power;
                }
            }
            int N = 1 << power;
            int [] old = keys;
            int oldShift = ivaluesShift;
            if (oldShift == 0 && !ensureIntSpace) {
                keys = new int [N];
            }
            else {
                ivaluesShift = N;
                keys = new int [N * 2];
            }
            for (int i = 0; i != N; ++i) {
                keys [i] = EMPTY;
            }

            object [] oldValues = values;
            if (oldValues != null) {
                values = new object [N];
            }

            int oldCount = keyCount;
            occupiedCount = 0;
            if (oldCount != 0) {
                keyCount = 0;
                for (int i = 0, remaining = oldCount; remaining != 0; ++i) {
                    int key = old [i];
                    if (key != EMPTY && key != DELETED) {
                        int index = insertNewKey (key);
                        if (oldValues != null) {
                            values [index] = oldValues [i];
                        }
                        if (oldShift != 0) {
                            keys [ivaluesShift + index] = old [oldShift + i];
                        }
                        --remaining;
                    }
                }
            }
        }

        // Ensure key index creating one if necessary
        private int ensureIndex (int key, bool intType)
        {
            int index = -1;
            int firstDeleted = -1;
            int [] keys = this.keys;
            if (keys != null) {
                int fraction = key * A;
                index = (int)((uint)fraction >> (32 - power));
                int entry = keys [index];
                if (entry == key) {
                    return index;
                }
                if (entry != EMPTY) {
                    if (entry == DELETED) {
                        firstDeleted = index;
                    }
                    // Search in table after first failed attempt
                    int mask = (1 << power) - 1;
                    int step = tableLookupStep (fraction, mask, power);
                    int n = 0;
                    do {
                        if (check) {
                            if (n >= occupiedCount)
                                Context.CodeBug ();
                            ++n;
                        }
                        index = (index + step) & mask;
                        entry = keys [index];
                        if (entry == key) {
                            return index;
                        }
                        if (entry == DELETED && firstDeleted < 0) {
                            firstDeleted = index;
                        }
                    }
                    while (entry != EMPTY);
                }
            }
            // Inserting of new key
            if (check && keys != null && keys [index] != EMPTY)
                Context.CodeBug ();
            if (firstDeleted >= 0) {
                index = firstDeleted;
            }
            else {
                // Need to consume empty entry: check occupation level
                if (keys == null || occupiedCount * 4 >= (1 << power) * 3) {
                    // Too litle unused entries: rehash
                    rehashTable (intType);
                    keys = this.keys;
                    return insertNewKey (key);
                }
                ++occupiedCount;
            }
            keys [index] = key;
            ++keyCount;
            return index;
        }
        // A == golden_ratio * (1 << 32) = ((sqrt(5) - 1) / 2) * (1 << 32)
        // See Knuth etc.
        private const int A = unchecked ((int)0x9e3779b9);

        private const int EMPTY = -1;
        private const int DELETED = -2;

        // Structure of kyes and values arrays (N == 1 << power):
        // keys[0 <= i < N]: key value or EMPTY or DELETED mark
        // values[0 <= i < N]: value of key at keys[i]
        // keys[N <= i < 2N]: int values of keys at keys[i - N]


        private int [] keys;

        private object [] values;

        private int power;
        private int keyCount;

        private int occupiedCount; // == keyCount + deleted_count

        // If ivaluesShift != 0, keys[ivaluesShift + index] contains integer
        // values associated with keys

        private int ivaluesShift;

        // If true, enables consitency checks
        private static readonly bool check = false; // TODO: make me a preprocessor directive

    }
}