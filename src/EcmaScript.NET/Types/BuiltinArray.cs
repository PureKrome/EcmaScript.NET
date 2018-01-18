//------------------------------------------------------------------------------
// <license file="NativeArray.cs">
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

using EcmaScript.NET.Collections;

namespace EcmaScript.NET.Types
{

    /// <summary>
    /// This class implements the Array native object.
    /// </summary>		
    public class BuiltinArray : IdScriptableObject
    {

        private long length;
        private object [] dense;
        private const int maximumDenseLength = 10000;

        public override string ClassName
        {
            get
            {
                return "Array";
            }

        }

        protected override internal int MaxInstanceId
        {
            get
            {
                return MAX_INSTANCE_ID;
            }
        }

        /*
        * Optimization possibilities and open issues:
        * - Long vs. double schizophrenia.  I suspect it might be better
        * to use double throughout.
		
        * - Most array operations go through getElem or setElem (defined
        * in this file) to handle the full 2^32 range; it might be faster
        * to have versions of most of the loops in this file for the
        * (infinitely more common) case of indices < 2^31.
		
        * - Functions that need a new Array call "new Array" in the
        * current scope rather than using a hardwired constructor;
        * "Array" could be redefined.  It turns out that js calls the
        * equivalent of "new Array" in the current scope, except that it
        * always gets at least an object back, even when Array == null.
        */
        private static readonly object ARRAY_TAG = new object ();

        internal static void Init (IScriptable scope, bool zealed)
        {
            BuiltinArray obj = new BuiltinArray ();
            obj.ExportAsJSClass (MAX_PROTOTYPE_ID, scope, zealed
                ,ScriptableObject.DONTENUM | ScriptableObject.READONLY | ScriptableObject.PERMANENT);
        }

        /// <summary> Zero-parameter constructor: just used to create Array.prototype</summary>
        private BuiltinArray ()
        {
            dense = null;
            this.length = 0;
        }

        public BuiltinArray (long length)
        {
            int intLength = (int)length;
            if (intLength == length && intLength > 0) {
                if (intLength > maximumDenseLength)
                    intLength = maximumDenseLength;
                dense = new object [intLength];
                for (int i = 0; i < intLength; i++)
                    dense [i] = UniqueTag.NotFound;
            }
            this.length = length;
        }

        public BuiltinArray (object [] array)
        {
            dense = array;
            this.length = array.Length;
        }

        #region InstanceIds
        private const int Id_length = 1;
        private const int MAX_INSTANCE_ID = 1;
        #endregion

        protected internal override int FindInstanceIdInfo (string s)
        {
            if (s.Equals ("length")) {
                return InstanceIdInfo (DONTENUM | PERMANENT, Id_length);
            }
            return base.FindInstanceIdInfo (s);
        }

        protected internal override string GetInstanceIdName (int id)
        {
            if (id == Id_length) {
                return "length";
            }
            return base.GetInstanceIdName (id);
        }

        protected internal override object GetInstanceIdValue (int id)
        {
            if (id == Id_length) {
                return (length);
            }
            return base.GetInstanceIdValue (id);
        }

        protected internal override void SetInstanceIdValue (int id, object value)
        {
            if (id == Id_length) {
                setLength (value);
                return;
            }
            base.SetInstanceIdValue (id, value);
        }


        protected internal override void InitPrototypeId (int id)
        {
            string s;
            int arity;

            switch (id) {
                case Id_constructor:
                    arity = 1;
                    s = "constructor";
                    break;
                case Id_toString:
                    arity = 0;
                    s = "toString";
                    break;
                case Id_toLocaleString:
                    arity = 1;
                    s = "toLocaleString";
                    break;
                case Id_toSource:
                    arity = 0;
                    s = "toSource";
                    break;
                case Id_join:
                    arity = 1;
                    s = "join";
                    break;
                case Id_reverse:
                    arity = 0;
                    s = "reverse";
                    break;
                case Id_sort:
                    arity = 1;
                    s = "sort";
                    break;
                case Id_push:
                    arity = 1;
                    s = "push";
                    break;
                case Id_pop:
                    arity = 1;
                    s = "pop";
                    break;
                case Id_shift:
                    arity = 1;
                    s = "shift";
                    break;
                case Id_unshift:
                    arity = 1;
                    s = "unshift";
                    break;
                case Id_splice:
                    arity = 1;
                    s = "splice";
                    break;
                case Id_concat:
                    arity = 1;
                    s = "concat";
                    break;
                case Id_slice:
                    arity = 1;
                    s = "slice";
                    break;
                default:
                    throw new ArgumentException (Convert.ToString (id));
            }
            InitPrototypeMethod (ARRAY_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (ARRAY_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;
            switch (id) {

                case Id_constructor: {
                        bool inNewExpr = (thisObj == null);
                        if (!inNewExpr) {
                            // IdFunctionObject.construct will set up parent, proto
                            return f.Construct (cx, scope, args);
                        }
                        return ImplCtor (cx, scope, args);
                    }


                case Id_toString:
                    return toStringHelper (cx, scope, thisObj,
                        cx.HasFeature (Context.Features.ToStringAsSource),
                            false);

                case Id_toLocaleString:
                    return toStringHelper (cx, scope, thisObj, false, true);

                case Id_toSource:
                    return toStringHelper (cx, scope, thisObj, true, false);

                case Id_join:
                    return ImplJoin (cx, thisObj, args);

                case Id_reverse:
                    return ImplReverse (cx, thisObj, args);

                case Id_sort:
                    return ImplSort (cx, scope, thisObj, args);

                case Id_push:
                    return ImplPush (cx, thisObj, args);

                case Id_pop:
                    return ImplPop (cx, thisObj, args);

                case Id_shift:
                    return ImplShift (cx, thisObj, args);

                case Id_unshift:
                    return ImplUnshift (cx, thisObj, args);

                case Id_splice:
                    return ImplSplice (cx, scope, thisObj, args);

                case Id_concat:
                    return ImplConcat (cx, scope, thisObj, args);

                case Id_slice:
                    return ImplSlice (cx, thisObj, args);
            }
            throw new ArgumentException (Convert.ToString (id));
        }

        public override object Get (int index, IScriptable start)
        {
            if (dense != null && 0 <= index && index < dense.Length)
                return dense [index];
            return base.Get (index, start);
        }

        public override bool Has (int index, IScriptable start)
        {
            if (dense != null && 0 <= index && index < dense.Length)
                return dense [index] != UniqueTag.NotFound;
            return base.Has (index, start);
        }

        // if id is an array index (ECMA 15.4.0), return the number,
        // otherwise return -1L
        private static long toArrayIndex (string id)
        {
            double d = ScriptConvert.ToNumber (id);
            if (!double.IsNaN (d)) {
                long index = ScriptConvert.ToUint32 (d);
                if (index == d && index != 4294967295L) {
                    // Assume that ScriptConvert.ToString(index) is the same
                    // as java.lang.Long.toString(index) for long
                    if (Convert.ToString (index).Equals (id)) {
                        return index;
                    }
                }
            }
            return -1;
        }

        public override object Put (string id, IScriptable start, object value)
        {
            object ret = base.Put (id, start, value);
            if (start == this) {
                // If the object is sealed, super will throw exception
                long index = toArrayIndex (id);
                if (index >= length) {
                    length = index + 1;
                }
            }
            return ret;
        }

        public override object Put (int index, IScriptable start, object value)
        {
            object ret = value;
            if (start == this && !Sealed && dense != null && 0 <= index && index < dense.Length) {
                // If start == this && sealed, super will throw exception
                dense [index] = value;
            }
            else {
                ret = base.Put (index, start, value);
            }
            if (start == this) {
                // only set the array length if given an array index (ECMA 15.4.0)
                if (this.length <= index) {
                    // avoid overflowing index!
                    this.length = (long)index + 1;
                }
            }
            return ret;
        }

        public override void Delete (int index)
        {
            if (!Sealed && dense != null && 0 <= index && index < dense.Length) {
                dense [index] = UniqueTag.NotFound;
            }
            else {
                base.Delete (index);
            }
        }

        public override object [] GetIds ()
        {
            object [] superIds = base.GetIds ();
            if (dense == null) {
                return superIds;
            }
            int N = dense.Length;
            long currentLength = length;
            if (N > currentLength) {
                N = (int)currentLength;
            }
            if (N == 0) {
                return superIds;
            }
            int superLength = superIds.Length;
            object [] ids = new object [N + superLength];
            // Make a copy of dense to be immune to removing
            // of array elems from other thread when calculating presentCount
            Array.Copy (dense, 0, ids, 0, N);
            int presentCount = 0;
            for (int i = 0; i != N; ++i) {
                // Replace existing elements by their indexes
                if (ids [i] != UniqueTag.NotFound) {
                    ids [presentCount] = (int)i;
                    ++presentCount;
                }
            }
            if (presentCount != N) {
                // dense contains deleted elems, need to shrink the result
                object [] tmp = new object [presentCount + superLength];
                Array.Copy (ids, 0, tmp, 0, presentCount);
                ids = tmp;
            }
            Array.Copy (superIds, 0, ids, presentCount, superLength);
            return ids;
        }

        public override object GetDefaultValue (Type hint)
        {
            if (CliHelper.IsNumberType (hint)) {
                Context cx = Context.CurrentContext;
                if (cx.Version == Context.Versions.JS1_2)
                    return (long)length;
            }
            return base.GetDefaultValue (hint);
        }

        /// <summary> See ECMA 15.4.1,2</summary>
        private static object ImplCtor (Context cx, IScriptable scope, object [] args)
        {
            if (args.Length == 0)
                return new BuiltinArray ();

            // Only use 1 arg as first element for version 1.2; for
            // any other version (including 1.3) follow ECMA and use it as
            // a length.
            if (cx.Version == Context.Versions.JS1_2) {
                return new BuiltinArray (args);
            }
            else {
                object arg0 = args [0];
                if (args.Length > 1 || !(CliHelper.IsNumber (arg0))) {
                    return new BuiltinArray (args);
                }
                else {
                    return new BuiltinArray (VerifyOutOfRange (arg0));
                }
            }
        }

        static long VerifyOutOfRange (long newLen)
        {
            long len = ScriptConvert.ToUint32 (newLen);
            if (len < 0 || len != (long)ScriptConvert.ToNumber (newLen))
                throw ScriptRuntime.ConstructError ("RangeError",
                        ScriptRuntime.GetMessage ("msg.arraylength.bad"));
            return len;
        }

        static long VerifyOutOfRange (object newLen)
        {
            long len = ScriptConvert.ToUint32 (newLen);
            if (len < 0 || len != (long)ScriptConvert.ToNumber (newLen))
                throw ScriptRuntime.ConstructError ("RangeError",
                        ScriptRuntime.GetMessage ("msg.arraylength.bad"));
            return len;
        }

        public virtual long getLength ()
        {
            return length;
        }


        private void setLength (object val)
        {
            // TODO do we satisfy this?
            // 15.4.5.1 [[Put]](P, V):
            // 1. Call the [[CanPut]] method of A with name P.
            // 2. If Result(1) is false, return.
            // ?									
            long longVal = VerifyOutOfRange (val);
            if (longVal < length) {
                // remove all properties between longVal and length
                if (length - longVal > 0x1000) {
                    // assume that the representation is sparse
                    object [] e = GetIds (); // will only find in object itself
                    for (int i = 0; i < e.Length; i++) {
                        object id = e [i];
                        if (id is string) {
                            // > MAXINT will appear as string
                            string strId = (string)id;
                            long index = toArrayIndex (strId);
                            if (index >= longVal)
                                Delete (strId);
                        }
                        else {
                            int index = ((int)id);
                            if (index >= longVal)
                                Delete (index);
                        }
                    }
                }
                else {
                    // assume a dense representation
                    for (long i = longVal; i < length; i++) {
                        deleteElem (this, i);
                    }
                }
            }
            length = longVal;
        }

        /* Support for generic Array-ish objects.  Most of the Array
        * functions try to be generic; anything that has a length
        * property is assumed to be an array.
        * getLengthProperty returns 0 if obj does not have the length property
        * or its value is not convertible to a number.
        */
        internal static long getLengthProperty (Context cx, IScriptable obj)
        {
            // These will both give numeric lengths within Uint32 range.
            if (obj is BuiltinString) {
                return ((BuiltinString)obj).Length;
            }
            else if (obj is BuiltinArray) {
                return ((BuiltinArray)obj).getLength ();
            }
            return ScriptConvert.ToUint32 (ScriptRuntime.getObjectProp (obj, "length", cx));
        }

        private static object setLengthProperty (Context cx, IScriptable target, long length)
        {
            return ScriptRuntime.setObjectProp (target, "length", (length), cx);
        }

        /* Utility functions to encapsulate index > Integer.MAX_VALUE
        * handling.  Also avoids unnecessary object creation that would
        * be necessary to use the general ScriptRuntime.get/setElem
        * functions... though this is probably premature optimization.
        */
        private static void deleteElem (IScriptable target, long index)
        {
            int i = (int)index;
            if (i == index) {
                target.Delete (i);
            }
            else {
                target.Delete (Convert.ToString (index));
            }
        }

        private static object getElem (Context cx, IScriptable target, long index)
        {
            if (index > int.MaxValue) {
                string id = Convert.ToString (index);
                return ScriptRuntime.getObjectProp (target, id, cx);
            }
            else {
                return ScriptRuntime.getObjectIndex (target, (int)index, cx);
            }
        }

        private static void setElem (Context cx, IScriptable target, long index, object value)
        {
            if (index > int.MaxValue) {
                string id = Convert.ToString (index);
                ScriptRuntime.setObjectProp (target, id, value, cx);
            }
            else {
                ScriptRuntime.setObjectIndex (target, (int)index, value, cx);
            }
        }

        class StringBuilder
        {
            int m_TopIdx = 0;
            int m_InnerIdx = 0;

            string [] [] m_Buffer = null;

            public StringBuilder (long length)
            {
                int idxSize = 32000;
                int topSize = Math.Max ((int)(length / idxSize), 1);

                m_Buffer = new string [topSize] [];
                for (int i = 0; i < topSize; i++) {
                    int thisSize = (int)Math.Min (length, (long)idxSize);
                    m_Buffer [i] = new string [thisSize];
                    length -= thisSize;
                }
            }

            public void Append (string value)
            {
                string [] tmp = m_Buffer [m_TopIdx];
                if (m_InnerIdx > tmp.Length) {
                    m_TopIdx++;
                    Append (value);
                    return;
                }
                tmp [m_InnerIdx++] = value;
            }

            public string ToString (string seperator)
            {
                string result = string.Empty;
                foreach (string [] tmp in m_Buffer) {
                    if (result != string.Empty)
                        result += seperator;
                    result += string.Join (seperator, tmp);
                }
                return result;
            }

        }

#if FALSE
        private static string toStringHelper (Context cx, IScriptable scope, IScriptable thisObj, bool toSource, bool toLocale)
        {
            /* It's probably redundant to handle long lengths in this
            * function; StringBuffers are limited to 2^31 in java.
            */

            long length = getLengthProperty (cx, thisObj);

            StringBuilder result = new StringBuilder (length);


            long i = 0;

            bool toplevel, iterating;
            if (cx.iterating == null) {
                toplevel = true;
                iterating = false;
                cx.iterating = new ObjToIntMap (31);
            }
            else {
                toplevel = false;
                iterating = cx.iterating.has (thisObj);
            }

            // Make sure cx.iterating is set to null when done
            // so we don't leak memory
            try {
                if (!iterating) {
                    cx.iterating.put (thisObj, 0); // stop recursion.
                    for (i = 0; i < length; i++) {
                        object elem = getElem (cx, thisObj, i);
                        if (elem == null || elem == Undefined.Value) {
                            continue;
                        }

                        if (toSource) {
                            result.Append (ScriptRuntime.uneval (cx, scope, elem));
                        }
                        else if (elem is string) {
                            string s = (string)elem;
                            if (toSource) {
                                result.Append (
                                      '\"'
                                    + ScriptRuntime.escapeString (s)
                                    + '\"');
                            }
                            else {
                                result.Append (s);
                            }
                        }
                        else {
                            if (toLocale && elem != Undefined.Value && elem != null) {
                                ICallable fun;
                                IScriptable funThis;
                                fun = ScriptRuntime.getPropFunctionAndThis (elem, "toLocaleString", cx);
                                funThis = ScriptRuntime.lastStoredScriptable (cx);
                                elem = fun.Call (cx, scope, funThis, ScriptRuntime.EmptyArgs);
                            }
                            result.Append (ScriptConvert.ToString (elem));
                        }
                    }
                }
            }
            finally {
                if (toplevel) {
                    cx.iterating = null;
                }
            }

            string sep = (toSource) ? "," : ", ";
            string tmp = result.ToString (sep);            
            if (!toSource) 
                return tmp;
            else                
                return "[" + tmp + "]";            
        }
#endif

        private static string toStringHelper (Context cx, IScriptable scope, IScriptable thisObj, bool toSource, bool toLocale)
        {
            /* It's probably redundant to handle long lengths in this
            * function; StringBuffers are limited to 2^31 in java.
            */

            long length = getLengthProperty (cx, thisObj);

            System.Text.StringBuilder result = new System.Text.StringBuilder (256);

            // whether to return '4,unquoted,5' or '[4, "quoted", 5]'
            string separator;

            if (toSource) {
                result.Append ('[');
                separator = ", ";
            }
            else {
                separator = ",";
            }

            bool haslast = false;
            long i = 0;

            bool toplevel, iterating;
            if (cx.iterating == null) {
                toplevel = true;
                iterating = false;
                cx.iterating = new ObjToIntMap (31);
            }
            else {
                toplevel = false;
                iterating = cx.iterating.has (thisObj);
            }

            // Make sure cx.iterating is set to null when done
            // so we don't leak memory
            try {
                if (!iterating) {
                    cx.iterating.put (thisObj, 0); // stop recursion.
                    for (i = 0; i < length; i++) {
                        if (i > 0)
                            result.Append (separator);
                        object elem = getElem (cx, thisObj, i);
                        if (elem == null || elem == Undefined.Value) {
                            haslast = false;
                            continue;
                        }
                        haslast = true;

                        if (toSource) {
                            result.Append (ScriptRuntime.uneval (cx, scope, elem));
                        }
                        else if (elem is string) {
                            string s = (string)elem;
                            if (toSource) {
                                result.Append ('\"');
                                result.Append (ScriptRuntime.escapeString (s));
                                result.Append ('\"');
                            }
                            else {
                                result.Append (s);
                            }
                        }
                        else {
                            if (toLocale && elem != Undefined.Value && elem != null) {
                                ICallable funCall;
                                IScriptable funThis;
                                funCall = ScriptRuntime.getPropFunctionAndThis (elem, "toLocaleString", cx) as ICallable;
                                funThis = ScriptRuntime.lastStoredScriptable (cx);
                                elem = funCall.Call (cx, scope, funThis, ScriptRuntime.EmptyArgs);
                            }
                            result.Append (ScriptConvert.ToString (elem));
                        }
                    }
                }
            }
            finally {
                if (toplevel) {
                    cx.iterating = null;
                }
            }

            if (toSource) {
                //for [,,].length behavior; we want toString to be symmetric.
                if (!haslast && i > 0)
                    result.Append (", ]");
                else
                    result.Append (']');
            }
            return result.ToString ();
        }     

        /// <summary> See ECMA 15.4.4.3</summary>
        private static string ImplJoin (Context cx, IScriptable thisObj, object [] args)
        {
            string separator;

            long llength = getLengthProperty (cx, thisObj);
            int length = (int)llength;
            if (llength != length) {
                throw Context.ReportRuntimeErrorById ("msg.arraylength.too.big", Convert.ToString (llength));
            }
            // if no args, use "," as separator
            if (args.Length < 1 || args [0] == Undefined.Value) {
                separator = ",";
            }
            else {
                separator = ScriptConvert.ToString (args [0]);
            }
            if (length == 0) {
                return "";
            }
            string [] buf = new string [length];
            int total_size = 0;
            for (int i = 0; i != length; i++) {
                object temp = getElem (cx, thisObj, i);
                if (temp != null && temp != Undefined.Value) {
                    string str = ScriptConvert.ToString (temp);
                    total_size += str.Length;
                    buf [i] = str;
                }
            }
            total_size += (length - 1) * separator.Length;
            System.Text.StringBuilder sb = new System.Text.StringBuilder (total_size);
            for (int i = 0; i != length; i++) {
                if (i != 0) {
                    sb.Append (separator);
                }
                string str = buf [i];
                if (str != null) {
                    // str == null for undefined or null
                    sb.Append (str);
                }
            }
            return sb.ToString ();
        }

        /// <summary> See ECMA 15.4.4.4</summary>
        private static IScriptable ImplReverse (Context cx, IScriptable thisObj, object [] args)
        {
            long len = getLengthProperty (cx, thisObj);

            long half = len / 2;
            for (long i = 0; i < half; i++) {
                long j = len - i - 1;
                object temp1 = getElem (cx, thisObj, i);
                object temp2 = getElem (cx, thisObj, j);
                setElem (cx, thisObj, i, temp2);
                setElem (cx, thisObj, j, temp1);
            }
            return thisObj;
        }

        /// <summary> See ECMA 15.4.4.5</summary>
        private static IScriptable ImplSort (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            long length = getLengthProperty (cx, thisObj);

            if (length <= 1) {
                return thisObj;
            }

            object compare;
            object [] cmpBuf;

            if (args.Length > 0 && Undefined.Value != args [0]) {
                // sort with given compare function
                compare = args [0];
                cmpBuf = new object [2]; // Buffer for cmp arguments
            }
            else {
                // sort with default compare
                compare = null;
                cmpBuf = null;
            }

            // Should we use the extended sort function, or the faster one?
            if (length >= int.MaxValue) {
                heapsort_extended (cx, scope, thisObj, length, compare, cmpBuf);
            }
            else {
                int ilength = (int)length;
                // copy the JS array into a working array, so it can be
                // sorted cheaply.
                object [] working = new object [ilength];
                for (int i = 0; i != ilength; ++i) {
                    working [i] = getElem (cx, thisObj, i);
                }

                heapsort (cx, scope, working, ilength, compare, cmpBuf);

                // copy the working array back into thisObj
                for (int i = 0; i != ilength; ++i) {
                    setElem (cx, thisObj, i, working [i]);
                }
            }
            return thisObj;
        }

        // Return true only if x > y
        private static bool IsBigger (Context cx, IScriptable scope, object x, object y, object cmp, object [] cmpBuf)
        {
            if (cmp == null) {
                if (cmpBuf != null)
                    Context.CodeBug ();
            }
            else {
                if (cmpBuf == null || cmpBuf.Length != 2)
                    Context.CodeBug ();
            }

            object undef = Undefined.Value;

            // sort undefined to end
            if (undef == y) {
                return false; // x can not be bigger then undef
            }
            else if (undef == x) {
                return true; // y != undef here, so x > y
            }

            if (cmp == null) {
                // if no cmp function supplied, sort lexicographically
                string a = ScriptConvert.ToString (x);
                string b = ScriptConvert.ToString (y);
                return String.CompareOrdinal (a, b) > 0;
            }
            else {
                // assemble args and call supplied JS cmp function
                cmpBuf [0] = x;
                cmpBuf [1] = y;
                ICallable fun = ScriptRuntime.getValueFunctionAndThis (cmp, cx);
                IScriptable funThis = ScriptRuntime.lastStoredScriptable (cx);

                object ret = fun.Call (cx, scope, funThis, cmpBuf);
                double d = ScriptConvert.ToNumber (ret);

                // TODO what to do when cmp function returns NaN? 
                // ECMA states
                // that it's then not a 'consistent compararison function'... but
                // then what do we do?  Back out and start over with the generic
                // cmp function when we see a NaN?  Throw an error?				

                // for now, just ignore it:				
                return d > 0;

            }
        }

        /// <summary>Heapsort implementation.
        /// See "Introduction to Algorithms" by Cormen, Leiserson, Rivest for details.
        /// Adjusted for zero based indexes.
        /// </summary>
        private static void heapsort (Context cx, IScriptable scope, object [] array, int length, object cmp, object [] cmpBuf)
        {
            if (length <= 1)
                Context.CodeBug ();

            // Build heap
            for (int i = length / 2; i != 0; ) {
                --i;
                object pivot = array [i];
                heapify (cx, scope, pivot, array, i, length, cmp, cmpBuf);
            }

            // Sort heap
            for (int i = length; i != 1; ) {
                --i;
                object pivot = array [i];
                array [i] = array [0];
                heapify (cx, scope, pivot, array, 0, i, cmp, cmpBuf);
            }
        }

        /// <summary>pivot and child heaps of i should be made into heap starting at i,
        /// original array[i] is never used to have less array access during sorting.
        /// </summary>
        private static void heapify (Context cx, IScriptable scope, object pivot, object [] array, int i, int end, object cmp, object [] cmpBuf)
        {
            for (; ; ) {
                int child = i * 2 + 1;
                if (child >= end) {
                    break;
                }
                object childVal = array [child];
                if (child + 1 < end) {
                    object nextVal = array [child + 1];
                    if (IsBigger (cx, scope, nextVal, childVal, cmp, cmpBuf)) {
                        ++child;
                        childVal = nextVal;
                    }
                }
                if (!IsBigger (cx, scope, childVal, pivot, cmp, cmpBuf)) {
                    break;
                }
                array [i] = childVal;
                i = child;
            }
            array [i] = pivot;
        }

        /// <summary>Version of heapsort that call getElem/setElem on target to query/assign
        /// array elements instead of Java array access
        /// </summary>
        private static void heapsort_extended (Context cx, IScriptable scope, IScriptable target, long length, object cmp, object [] cmpBuf)
        {
            if (length <= 1)
                Context.CodeBug ();

            // Build heap
            for (long i = length / 2; i != 0; ) {
                --i;
                object pivot = getElem (cx, target, i);
                heapify_extended (cx, scope, pivot, target, i, length, cmp, cmpBuf);
            }

            // Sort heap
            for (long i = length; i != 1; ) {
                --i;
                object pivot = getElem (cx, target, i);
                setElem (cx, target, i, getElem (cx, target, 0));
                heapify_extended (cx, scope, pivot, target, 0, i, cmp, cmpBuf);
            }
        }

        private static void heapify_extended (Context cx, IScriptable scope, object pivot, IScriptable target, long i, long end, object cmp, object [] cmpBuf)
        {
            for (; ; ) {
                long child = i * 2 + 1;
                if (child >= end) {
                    break;
                }
                object childVal = getElem (cx, target, child);
                if (child + 1 < end) {
                    object nextVal = getElem (cx, target, child + 1);
                    if (IsBigger (cx, scope, nextVal, childVal, cmp, cmpBuf)) {
                        ++child;
                        childVal = nextVal;
                    }
                }
                if (!IsBigger (cx, scope, childVal, pivot, cmp, cmpBuf)) {
                    break;
                }
                setElem (cx, target, i, childVal);
                i = child;
            }
            setElem (cx, target, i, pivot);
        }

        /// <summary> Non-ECMA methods.</summary>

        private static object ImplPush (Context cx, IScriptable thisObj, object [] args)
        {
            long length = getLengthProperty (cx, thisObj);
            for (int i = 0; i < args.Length; i++) {
                setElem (cx, thisObj, length + i, args [i]);
            }

            length += args.Length;
            object lengthObj = setLengthProperty (cx, thisObj, length);

            /*
            * If JS1.2, follow Perl4 by returning the last thing pushed.
            * Otherwise, return the new array length.
            */
            if (cx.Version == Context.Versions.JS1_2)
                // if JS1.2 && no arguments, return undefined.
                return args.Length == 0 ? Undefined.Value : args [args.Length - 1];
            else
                return lengthObj;
        }

        private static object ImplPop (Context cx, IScriptable thisObj, object [] args)
        {
            object result;
            long length = getLengthProperty (cx, thisObj);
            if (length > 0) {
                length--;

                // Get the to-be-deleted property's value.
                result = getElem (cx, thisObj, length);

                // We don't need to delete the last property, because
                // setLength does that for us.
            }
            else {
                result = Undefined.Value;
            }
            // necessary to match js even when length < 0; js pop will give a
            // length property to any target it is called on.
            setLengthProperty (cx, thisObj, length);

            return result;
        }

        private static object ImplShift (Context cx, IScriptable thisObj, object [] args)
        {
            object result;
            long length = getLengthProperty (cx, thisObj);
            if (length > 0) {
                long i = 0;
                length--;

                // Get the to-be-deleted property's value.
                result = getElem (cx, thisObj, i);

                /*
                * Slide down the array above the first element.  Leave i
                * set to point to the last element.
                */
                if (length > 0) {
                    for (i = 1; i <= length; i++) {
                        object temp = getElem (cx, thisObj, i);
                        setElem (cx, thisObj, i - 1, temp);
                    }
                }
                // We don't need to delete the last property, because
                // setLength does that for us.
            }
            else {
                result = Undefined.Value;
            }
            setLengthProperty (cx, thisObj, length);
            return result;
        }

        private static object ImplUnshift (Context cx, IScriptable thisObj, object [] args)
        {
            long length = getLengthProperty (cx, thisObj);
            int argc = args.Length;

            VerifyOutOfRange (length + args.Length);

            if (args.Length > 0) {
                /*  Slide up the array to make room for args at the bottom */
                if (length > 0) {
                    for (long last = length - 1; last >= 0; last--) {
                        object temp = getElem (cx, thisObj, last);
                        setElem (cx, thisObj, last + argc, temp);
                    }
                }

                /* Copy from argv to the bottom of the array. */
                for (int i = 0; i < args.Length; i++) {
                    setElem (cx, thisObj, i, args [i]);
                }

                /* Follow Perl by returning the new array length. */
                length += args.Length;
                return setLengthProperty (cx, thisObj, length);
            }
            return (length);
        }

        private static object ImplSplice (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            /* create an empty Array to return. */
            scope = GetTopLevelScope (scope);
            object result = ScriptRuntime.NewObject (cx, scope, "Array", null);
            int argc = args.Length;
            if (argc == 0)
                return result;
            long length = getLengthProperty (cx, thisObj);

            /* Convert the first argument into a starting index. */
            long begin = toSliceIndex (ScriptConvert.ToInteger (args [0]), length);
            argc--;

            /* Convert the second argument into count */
            long count;
            if (args.Length == 1) {
                count = length - begin;
            }
            else {
                double dcount = ScriptConvert.ToInteger (args [1]);
                if (dcount < 0) {
                    count = 0;
                }
                else if (dcount > (length - begin)) {
                    count = length - begin;
                }
                else {
                    count = (long)dcount;
                }
                argc--;
            }

            long end = begin + count;

            /* If there are elements to remove, put them into the return value. */
            if (count != 0) {
                if (count == 1 && (cx.Version == Context.Versions.JS1_2)) {
                    /*
                    * JS lacks "list context", whereby in Perl one turns the
                    * single scalar that's spliced out into an array just by
                    * assigning it to @single instead of $single, or by using it
                    * as Perl push's first argument, for instance.
                    *
                    * JS1.2 emulated Perl too closely and returned a non-Array for
                    * the single-splice-out case, requiring callers to test and
                    * wrap in [] if necessary.  So JS1.3, default, and other
                    * versions all return an array of length 1 for uniformity.
                    */
                    result = getElem (cx, thisObj, begin);
                }
                else {
                    for (long last = begin; last != end; last++) {
                        IScriptable resultArray = (IScriptable)result;
                        object temp = getElem (cx, thisObj, last);
                        setElem (cx, resultArray, last - begin, temp);
                    }
                }
            }
            else if (count == 0 && cx.Version == Context.Versions.JS1_2) {
                /* Emulate C JS1.2; if no elements are removed, return undefined. */
                result = Undefined.Value;
            }

            /* Find the direction (up or down) to copy and make way for argv. */
            long delta = argc - count;
            VerifyOutOfRange (length + delta);

            if (delta > 0) {
                for (long last = length - 1; last >= end; last--) {
                    object temp = getElem (cx, thisObj, last);
                    setElem (cx, thisObj, last + delta, temp);
                }
            }
            else if (delta < 0) {
                for (long last = end; last < length; last++) {
                    object temp = getElem (cx, thisObj, last);
                    setElem (cx, thisObj, last + delta, temp);
                }
            }

            /* Copy from argv into the hole to complete the splice. */
            int argoffset = args.Length - argc;
            for (int i = 0; i < argc; i++) {
                setElem (cx, thisObj, begin + i, args [i + argoffset]);
            }

            /* Update length in case we deleted elements from the end. */
            setLengthProperty (cx, thisObj, length + delta);
            return result;
        }

        /*
        * See Ecma 262v3 15.4.4.4
        */
        private static IScriptable ImplConcat (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            // create an empty Array to return.
            scope = GetTopLevelScope (scope);
            IFunction ctor = ScriptRuntime.getExistingCtor (cx, scope, "Array");
            IScriptable result = ctor.Construct (cx, scope, ScriptRuntime.EmptyArgs);
            long length;
            long slot = 0;

            // Range Check
            long sourceLength = getLengthProperty (cx, thisObj);
            long targetLength = 0;
            for (int i = 0; i < args.Length; i++) {
                if (ScriptRuntime.InstanceOf (args [i], ctor, cx)) {
                    IScriptable arg = (IScriptable)args [i];
                    length = getLengthProperty (cx, arg);
                    targetLength += length;
                }
                else {
                    targetLength++;
                }
            }

            VerifyOutOfRange (sourceLength + targetLength);

            /* Put the target in the result array; only add it as an array
            * if it looks like one.
            */
            if (ScriptRuntime.InstanceOf (thisObj, ctor, cx)) {
                length = getLengthProperty (cx, thisObj);

                // Copy from the target object into the result
                for (slot = 0; slot < length; slot++) {
                    object temp = getElem (cx, thisObj, slot);
                    setElem (cx, result, slot, temp);
                }
            }
            else {
                setElem (cx, result, slot++, thisObj);
            }

            /* Copy from the arguments into the result.  If any argument
            * has a numeric length property, treat it as an array and add
            * elements separately; otherwise, just copy the argument.
            */
            for (int i = 0; i < args.Length; i++) {
                if (ScriptRuntime.InstanceOf (args [i], ctor, cx)) {
                    // ScriptRuntime.instanceOf => instanceof Scriptable
                    IScriptable arg = (IScriptable)args [i];
                    length = getLengthProperty (cx, arg);
                    for (long j = 0; j < length; j++, slot++) {
                        object temp = getElem (cx, arg, j);
                        setElem (cx, result, slot, temp);
                    }
                }
                else {
                    setElem (cx, result, slot++, args [i]);
                }
            }
            return result;
        }

        private IScriptable ImplSlice (Context cx, IScriptable thisObj, object [] args)
        {
            IScriptable scope = GetTopLevelScope (this);
            IScriptable result = ScriptRuntime.NewObject (cx, scope, "Array", null);
            long length = getLengthProperty (cx, thisObj);

            long begin, end;
            if (args.Length == 0) {
                begin = 0;
                end = length;
            }
            else {
                begin = toSliceIndex (ScriptConvert.ToInteger (args [0]), length);
                if (args.Length == 1) {
                    end = length;
                }
                else {
                    end = toSliceIndex (ScriptConvert.ToInteger (args [1]), length);
                }
            }

            for (long slot = begin; slot < end; slot++) {
                object temp = getElem (cx, thisObj, slot);
                setElem (cx, result, slot - begin, temp);
            }

            return result;
        }

        private static long toSliceIndex (double value, long length)
        {
            long result;
            if (value < 0.0) {
                if (value + length < 0.0) {
                    result = 0;
                }
                else {
                    result = (long)(value + length);
                }
            }
            else if (value > length) {
                result = length;
            }
            else {
                result = (long)value;
            }
            return result;
        }

        #region PrototypeIds
        private const int Id_constructor = 1;
        private const int Id_toString = 2;
        private const int Id_toLocaleString = 3;
        private const int Id_toSource = 4;
        private const int Id_join = 5;
        private const int Id_reverse = 6;
        private const int Id_sort = 7;
        private const int Id_push = 8;
        private const int Id_pop = 9;
        private const int Id_shift = 10;
        private const int Id_unshift = 11;
        private const int Id_splice = 12;
        private const int Id_concat = 13;
        private const int Id_slice = 14;
        private const int MAX_PROTOTYPE_ID = 14;
        #endregion

        protected internal override int FindPrototypeId (string s)
        {
            int id;
            #region Generated PrototypeId Switch
        L0: {
                id = 0;
                string X = null;
                int c;
            L:
                switch (s.Length) {
                    case 3:
                        X = "pop";
                        id = Id_pop;
                        break;
                    case 4:
                        c = s [0];
                        if (c == 'j') { X = "join"; id = Id_join; }
                        else if (c == 'p') { X = "push"; id = Id_push; }
                        else if (c == 's') { X = "sort"; id = Id_sort; }
                        break;
                    case 5:
                        c = s [1];
                        if (c == 'h') { X = "shift"; id = Id_shift; }
                        else if (c == 'l') { X = "slice"; id = Id_slice; }
                        break;
                    case 6:
                        c = s [0];
                        if (c == 'c') { X = "concat"; id = Id_concat; }
                        else if (c == 's') { X = "splice"; id = Id_splice; }
                        break;
                    case 7:
                        c = s [0];
                        if (c == 'r') { X = "reverse"; id = Id_reverse; }
                        else if (c == 'u') { X = "unshift"; id = Id_unshift; }
                        break;
                    case 8:
                        c = s [3];
                        if (c == 'o') { X = "toSource"; id = Id_toSource; }
                        else if (c == 't') { X = "toString"; id = Id_toString; }
                        break;
                    case 11:
                        X = "constructor";
                        id = Id_constructor;
                        break;
                    case 14:
                        X = "toLocaleString";
                        id = Id_toLocaleString;
                        break;
                }
                if (X != null && X != s && !X.Equals (s))
                    id = 0;
            }
        EL0:

            #endregion
            return id;
        }

    }
}
