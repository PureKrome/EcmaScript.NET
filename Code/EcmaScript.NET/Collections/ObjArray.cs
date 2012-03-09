//------------------------------------------------------------------------------
// <license file="ObjArray.cs">
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

    /// <summary>Implementation of resizable array with focus on minimizing memory usage by storing few initial array elements in object fields. Can also be used as a stack.</summary>


    public class ObjArray
    {
        virtual public bool Sealed
        {
            get
            {
                return zealed;
            }

        }
        virtual public bool Empty
        {
            get
            {
                return m_Size == 0;
            }

        }
        virtual public int Size
        {
            set
            {
                if (value < 0)
                    throw new ArgumentException ();
                if (zealed)
                    throw onSeledMutation ();
                int N = m_Size;
                if (value < N) {
                    for (int i = value; i != N; ++i) {
                        SetImpl (i, (object)null);
                    }
                }
                else if (value > N) {
                    if (value > FIELDS_STORE_SIZE) {
                        ensureCapacity (value);
                    }
                }
                m_Size = value;
            }

        }

        public ObjArray ()
        {
        }

        public void seal ()
        {
            zealed = true;
        }

        public int size ()
        {
            return m_Size;
        }

        public object Get (int index)
        {
            if (!(0 <= index && index < m_Size))
                throw onInvalidIndex (index, m_Size);
            return GetImpl (index);
        }

        public void Set (int index, object value)
        {
            if (!(0 <= index && index < m_Size))
                throw onInvalidIndex (index, m_Size);
            if (zealed)
                throw onSeledMutation ();
            SetImpl (index, value);
        }

        private object GetImpl (int index)
        {
            switch (index) {

                case 0:
                    return f0;

                case 1:
                    return f1;

                case 2:
                    return f2;

                case 3:
                    return f3;

                case 4:
                    return f4;
            }
            return data [index - FIELDS_STORE_SIZE];
        }

        private void SetImpl (int index, object value)
        {
            switch (index) {

                case 0:
                    f0 = value;
                    break;

                case 1:
                    f1 = value;
                    break;

                case 2:
                    f2 = value;
                    break;

                case 3:
                    f3 = value;
                    break;

                case 4:
                    f4 = value;
                    break;

                default:
                    data [index - FIELDS_STORE_SIZE] = value;
                    break;

            }
        }

        public virtual int indexOf (object obj)
        {
            int N = m_Size;
            for (int i = 0; i != N; ++i) {
                object current = GetImpl (i);
                if (current == obj || (current != null && current.Equals (obj))) {
                    return i;
                }
            }
            return -1;
        }

        public virtual int lastIndexOf (object obj)
        {
            for (int i = m_Size; i != 0; ) {
                --i;
                object current = GetImpl (i);
                if (current == obj || (current != null && current.Equals (obj))) {
                    return i;
                }
            }
            return -1;
        }

        public object peek ()
        {
            int N = m_Size;
            if (N == 0)
                throw onEmptyStackTopRead ();
            return GetImpl (N - 1);
        }

        public object pop ()
        {
            if (zealed)
                throw onSeledMutation ();
            int N = m_Size;
            --N;
            object top;
            switch (N) {

                case -1:
                    throw onEmptyStackTopRead ();

                case 0:
                    top = f0;
                    f0 = null;
                    break;

                case 1:
                    top = f1;
                    f1 = null;
                    break;

                case 2:
                    top = f2;
                    f2 = null;
                    break;

                case 3:
                    top = f3;
                    f3 = null;
                    break;

                case 4:
                    top = f4;
                    f4 = null;
                    break;

                default:
                    top = data [N - FIELDS_STORE_SIZE];
                    data [N - FIELDS_STORE_SIZE] = null;
                    break;

            }
            m_Size = N;
            return top;
        }

        public void push (object value)
        {
            add (value);
        }

        public void add (object value)
        {
            if (zealed)
                throw onSeledMutation ();
            int N = m_Size;
            if (N >= FIELDS_STORE_SIZE) {
                ensureCapacity (N + 1);
            }
            m_Size = N + 1;
            SetImpl (N, value);
        }

        public void add (int index, object value)
        {
            int N = m_Size;
            if (!(0 <= index && index <= N))
                throw onInvalidIndex (index, N + 1);
            if (zealed)
                throw onSeledMutation ();
            object tmp;
            switch (index) {

                case 0:
                    if (N == 0) {
                        f0 = value;
                        break;
                    }
                    tmp = f0;
                    f0 = value;
                    value = tmp;
                    goto case 1;

                case 1:
                    if (N == 1) {
                        f1 = value;
                        break;
                    }
                    tmp = f1;
                    f1 = value;
                    value = tmp;
                    goto case 2;

                case 2:
                    if (N == 2) {
                        f2 = value;
                        break;
                    }
                    tmp = f2;
                    f2 = value;
                    value = tmp;
                    goto case 3;

                case 3:
                    if (N == 3) {
                        f3 = value;
                        break;
                    }
                    tmp = f3;
                    f3 = value;
                    value = tmp;
                    goto case 4;

                case 4:
                    if (N == 4) {
                        f4 = value;
                        break;
                    }
                    tmp = f4;
                    f4 = value;
                    value = tmp;

                    index = FIELDS_STORE_SIZE;
                    goto default;

                default:
                    ensureCapacity (N + 1);
                    if (index != N) {
                        Array.Copy (data, index - FIELDS_STORE_SIZE, data, index - FIELDS_STORE_SIZE + 1, N - index);
                    }
                    data [index - FIELDS_STORE_SIZE] = value;
                    break;

            }
            m_Size = N + 1;
        }

        public void remove (int index)
        {
            int N = m_Size;
            if (!(0 <= index && index < N))
                throw onInvalidIndex (index, N);
            if (zealed)
                throw onSeledMutation ();
            --N;
            switch (index) {

                case 0:
                    if (N == 0) {
                        f0 = null;
                        break;
                    }
                    f0 = f1;
                    goto case 1;

                case 1:
                    if (N == 1) {
                        f1 = null;
                        break;
                    }
                    f1 = f2;
                    goto case 2;

                case 2:
                    if (N == 2) {
                        f2 = null;
                        break;
                    }
                    f2 = f3;
                    goto case 3;

                case 3:
                    if (N == 3) {
                        f3 = null;
                        break;
                    }
                    f3 = f4;
                    goto case 4;

                case 4:
                    if (N == 4) {
                        f4 = null;
                        break;
                    }
                    f4 = data [0];

                    index = FIELDS_STORE_SIZE;
                    goto default;

                default:
                    if (index != N) {
                        Array.Copy (data, index - FIELDS_STORE_SIZE + 1, data, index - FIELDS_STORE_SIZE, N - index);
                    }
                    data [N - FIELDS_STORE_SIZE] = null;
                    break;

            }
            m_Size = N;
        }

        public void clear ()
        {
            if (zealed)
                throw onSeledMutation ();
            int N = m_Size;
            for (int i = 0; i != N; ++i) {
                SetImpl (i, (object)null);
            }
            m_Size = 0;
        }

        public object [] ToArray ()
        {
            object [] array = new object [m_Size];
            ToArray (array, 0);
            return array;
        }

        public void ToArray (object [] array)
        {
            ToArray (array, 0);
        }

        public void ToArray (object [] array, int offset)
        {
            int N = m_Size;
            switch (N) {

                default:
                    Array.Copy (data, 0, array, offset + FIELDS_STORE_SIZE, N - FIELDS_STORE_SIZE);
                    goto case 5;


                case 5:
                    array [offset + 4] = f4;
                    goto case 4;

                case 4:
                    array [offset + 3] = f3;
                    goto case 3;

                case 3:
                    array [offset + 2] = f2;
                    goto case 2;

                case 2:
                    array [offset + 1] = f1;
                    goto case 1;

                case 1:
                    array [offset + 0] = f0;
                    goto case 0;

                case 0:
                    break;
            }
        }

        private void ensureCapacity (int minimalCapacity)
        {
            int required = minimalCapacity - FIELDS_STORE_SIZE;
            if (required <= 0)
                throw new ArgumentException ();
            if (data == null) {
                int alloc = FIELDS_STORE_SIZE * 2;
                if (alloc < required) {
                    alloc = required;
                }
                data = new object [alloc];
            }
            else {
                int alloc = data.Length;
                if (alloc < required) {
                    if (alloc <= FIELDS_STORE_SIZE) {
                        alloc = FIELDS_STORE_SIZE * 2;
                    }
                    else {
                        alloc *= 2;
                    }
                    if (alloc < required) {
                        alloc = required;
                    }
                    object [] tmp = new object [alloc];
                    if (m_Size > FIELDS_STORE_SIZE) {
                        Array.Copy (data, 0, tmp, 0, m_Size - FIELDS_STORE_SIZE);
                    }
                    data = tmp;
                }
            }
        }

        private static ApplicationException onInvalidIndex (int index, int upperBound)
        {
            // \u2209 is "NOT ELEMENT OF"
            string msg = index + " \u2209 [0, " + upperBound + ')';
            throw new System.IndexOutOfRangeException (msg);
        }

        private static ApplicationException onEmptyStackTopRead ()
        {
            throw new ApplicationException ("Empty stack");
        }

        private static ApplicationException onSeledMutation ()
        {
            throw new ApplicationException ("Attempt to modify sealed array");
        }


        // Number of data elements
        private int m_Size;

        private bool zealed;

        private const int FIELDS_STORE_SIZE = 5;

        private object f0, f1, f2, f3, f4;

        private object [] data;
    }
}