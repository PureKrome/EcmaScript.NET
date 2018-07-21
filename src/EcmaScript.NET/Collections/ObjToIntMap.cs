//------------------------------------------------------------------------------
// <license file="ObjToIntMap.cs">
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

    /// <summary> Map to associate objects to integers.
    /// The map does not synchronize any of its operation, so either use
    /// it from a single thread or do own synchronization or perform all mutation
    /// operations on one thread before passing the map to others
    /// 
    /// </summary>
    public class ObjToIntMap
    {
        virtual public bool Empty
        {
            get
            {
                return keyCount == 0;
            }

        }

        // Map implementation via hashtable,
        // follows "The Art of Computer Programming" by Donald E. Knuth

        // ObjToIntMap is a copy cat of ObjToIntMap with API adjusted to object keys

        public class Iterator
        {
            virtual public object Key
            {
                get
                {
                    object key = keys [cursor];
                    if (key == UniqueTag.NullValue) {
                        key = null;
                    }
                    return key;
                }

            }
            virtual public int Value
            {
                get
                {
                    return values [cursor];
                }

                set
                {
                    values [cursor] = value;
                }

            }

            internal Iterator (ObjToIntMap master)
            {
                this.master = master;
            }

            internal void Init (object [] keys, int [] values, int keyCount)
            {
                this.keys = keys;
                this.values = values;
                this.cursor = -1;
                this.remaining = keyCount;
            }

            public virtual void start ()
            {
                master.initIterator (this);
                next ();
            }

            public virtual bool done ()
            {
                return remaining < 0;
            }

            public virtual void next ()
            {
                if (remaining == -1)
                    Context.CodeBug ();
                if (remaining == 0) {
                    remaining = -1;
                    cursor = -1;
                }
                else {
                    for (++cursor; ; ++cursor) {
                        object key = keys [cursor];
                        if (key != null && key != ObjToIntMap.DELETED) {
                            --remaining;
                            break;
                        }
                    }
                }
            }

            internal ObjToIntMap master;
            private int cursor;
            private int remaining;
            private object [] keys;
            private int [] values;
        }

        public ObjToIntMap ()
            : this (4)
        {
        }

        public ObjToIntMap (int keyCountHint)
        {
            if (keyCountHint < 0)
                Context.CodeBug ();
            // Table grow when number of stored keys >= 3/4 of max capacity
            int minimalCapacity = keyCountHint * 4 / 3;
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

        public virtual bool has (object key)
        {
            if (key == null) {
                key = UniqueTag.NullValue;
            }
            return 0 <= findIndex (key);
        }

        /// <summary> Get integer value assigned with key.</summary>
        /// <returns> key integer value or defaultValue if key is absent
        /// </returns>
        public virtual int Get (object key, int defaultValue)
        {
            if (key == null) {
                key = UniqueTag.NullValue;
            }
            int index = findIndex (key);
            if (0 <= index) {
                return values [index];
            }
            return defaultValue;
        }

        /// <summary> Get integer value assigned with key.</summary>
        /// <returns> key integer value
        /// </returns>
        /// <throws>  RuntimeException if key does not exist </throws>
        public virtual int getExisting (object key)
        {
            if (key == null) {
                key = UniqueTag.NullValue;
            }
            int index = findIndex (key);
            if (0 <= index) {
                return values [index];
            }
            // Key must exist
            Context.CodeBug ();
            return 0;
        }

        public virtual void put (object key, int value)
        {
            if (key == null) {
                key = UniqueTag.NullValue;
            }
            int index = ensureIndex (key);
            values [index] = value;
        }

        /// <summary> If table already contains a key that equals to keyArg, return that key
        /// while setting its value to zero, otherwise add keyArg with 0 value to
        /// the table and return it.
        /// </summary>
        public virtual object intern (object keyArg)
        {
            bool nullKey = false;
            if (keyArg == null) {
                nullKey = true;
                keyArg = UniqueTag.NullValue;
            }
            int index = ensureIndex (keyArg);
            values [index] = 0;
            return (nullKey) ? null : keys [index];
        }

        public virtual void remove (object key)
        {
            if (key == null) {
                key = UniqueTag.NullValue;
            }
            int index = findIndex (key);
            if (0 <= index) {
                keys [index] = DELETED;
                --keyCount;
            }
        }

        public virtual void clear ()
        {
            int i = keys.Length;
            while (i != 0) {
                keys [--i] = null;
            }
            keyCount = 0;
            occupiedCount = 0;
        }

        public virtual Iterator newIterator ()
        {
            return new Iterator (this);
        }

        // The sole purpose of the method is to avoid accessing private fields
        // from the Iterator inner class to workaround JDK 1.1 compiler bug which
        // generates code triggering VerifierError on recent JVMs
        internal void initIterator (Iterator i)
        {
            i.Init (keys, values, keyCount);
        }

        /// <summary>Return array of present keys </summary>
        public virtual object [] getKeys ()
        {
            object [] array = new object [keyCount];
            getKeys (array, 0);
            return array;
        }

        public virtual void getKeys (object [] array, int offset)
        {
            int count = keyCount;
            for (int i = 0; count != 0; ++i) {
                object key = keys [i];
                if (key != null && key != DELETED) {
                    if (key == UniqueTag.NullValue) {
                        key = null;
                    }
                    array [offset] = key;
                    ++offset;
                    --count;
                }
            }
        }

        private static int tableLookupStep (int fraction, int mask, int power)
        {
            int shift = 32 - 2 * power;
            if (shift >= 0) {
                return ((int)(((uint)fraction >> shift)) & mask) | 1;
            }
            else {
                return (fraction & (int)((uint)mask >> -shift)) | 1;
            }
        }

        private int findIndex (object key)
        {
            if (keys != null) {
                int hash = key.GetHashCode ();
                int fraction = hash * A;
                int index = (int)((uint)fraction >> (32 - power));
                object test = keys [index];
                if (test != null) {
                    int N = 1 << power;
                    if (test == key || (values [N + index] == hash && test.Equals (key))) {
                        return index;
                    }
                    // Search in table after first failed attempt
                    int mask = N - 1;
                    int step = tableLookupStep (fraction, mask, power);
                    int n = 0;
                    for (; ; ) {
                        if (check) {
                            if (n >= occupiedCount)
                                Context.CodeBug ();
                            ++n;
                        }
                        index = (index + step) & mask;
                        test = keys [index];
                        if (test == null) {
                            break;
                        }
                        if (test == key || (values [N + index] == hash && test.Equals (key))) {
                            return index;
                        }
                    }
                }
            }
            return -1;
        }

        // Insert key that is not present to table without deleted entries
        // and enough free space
        private int insertNewKey (object key, int hash)
        {
            if (check && occupiedCount != keyCount)
                Context.CodeBug ();
            if (check && keyCount == 1 << power)
                Context.CodeBug ();
            int fraction = hash * A;
            int index = (int)((uint)fraction >> (32 - power));
            int N = 1 << power;
            if (keys [index] != null) {
                int mask = N - 1;
                int step = tableLookupStep (fraction, mask, power);
                int firstIndex = index;
                do {
                    if (check && keys [index] == DELETED)
                        Context.CodeBug ();
                    index = (index + step) & mask;
                    if (check && firstIndex == index)
                        Context.CodeBug ();
                }
                while (keys [index] != null);
            }
            keys [index] = key;
            values [N + index] = hash;
            ++occupiedCount;
            ++keyCount;

            return index;
        }

        private void rehashTable ()
        {
            if (keys == null) {
                if (check && keyCount != 0)
                    Context.CodeBug ();
                if (check && occupiedCount != 0)
                    Context.CodeBug ();
                int N = 1 << power;
                keys = new object [N];
                values = new int [2 * N];
            }
            else {
                // Check if removing deleted entries would free enough space
                if (keyCount * 2 >= occupiedCount) {
                    // Need to grow: less then half of deleted entries
                    ++power;
                }
                int N = 1 << power;
                object [] oldKeys = keys;
                int [] oldValues = values;
                int oldN = oldKeys.Length;
                keys = new object [N];
                values = new int [2 * N];

                int remaining = keyCount;
                occupiedCount = keyCount = 0;
                for (int i = 0; remaining != 0; ++i) {
                    object key = oldKeys [i];
                    if (key != null && key != DELETED) {
                        int keyHash = oldValues [oldN + i];
                        int index = insertNewKey (key, keyHash);
                        values [index] = oldValues [i];
                        --remaining;
                    }
                }
            }
        }

        // Ensure key index creating one if necessary
        private int ensureIndex (object key)
        {
            int hash = key.GetHashCode ();
            int index = -1;
            int firstDeleted = -1;
            if (keys != null) {
                int fraction = hash * A;
                index = (int)((uint)fraction >> (32 - power));
                object test = keys [index];
                if (test != null) {
                    int N = 1 << power;
                    if (test == key || (values [N + index] == hash && test.Equals (key))) {
                        return index;
                    }
                    if (test == DELETED) {
                        firstDeleted = index;
                    }

                    // Search in table after first failed attempt
                    int mask = N - 1;
                    int step = tableLookupStep (fraction, mask, power);
                    int n = 0;
                    for (; ; ) {
                        if (check) {
                            if (n >= occupiedCount)
                                Context.CodeBug ();
                            ++n;
                        }
                        index = (index + step) & mask;
                        test = keys [index];
                        if (test == null) {
                            break;
                        }
                        if (test == key || (values [N + index] == hash && test.Equals (key))) {
                            return index;
                        }
                        if (test == DELETED && firstDeleted < 0) {
                            firstDeleted = index;
                        }
                    }
                }
            }
            // Inserting of new key
            if (check && keys != null && keys [index] != null)
                Context.CodeBug ();
            if (firstDeleted >= 0) {
                index = firstDeleted;
            }
            else {
                // Need to consume empty entry: check occupation level
                if (keys == null || occupiedCount * 4 >= (1 << power) * 3) {
                    // Too litle unused entries: rehash
                    rehashTable ();
                    return insertNewKey (key, hash);
                }
                ++occupiedCount;
            }
            keys [index] = key;
            values [(1 << power) + index] = hash;
            ++keyCount;
            return index;
        }

        // A == golden_ratio * (1 << 32) = ((sqrt(5) - 1) / 2) * (1 << 32)
        // See Knuth etc.
        private const int A = unchecked ((int)0x9e3779b9);

        private static readonly object DELETED = new object ();

        // Structure of kyes and values arrays (N == 1 << power):
        // keys[0 <= i < N]: key value or null or DELETED mark
        // values[0 <= i < N]: value of key at keys[i]
        // values[N <= i < 2*N]: hash code of key at keys[i-N]


        private object [] keys;

        private int [] values;

        private int power;
        private int keyCount;

        private int occupiedCount; // == keyCount + deleted_count

        // If true, enables consitency checks
        private static readonly bool check = false; // TODO: make me a preprocessor directive

    }
}