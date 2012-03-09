//------------------------------------------------------------------------------
// <license file="ScriptRuntime.cs">
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
using System.Resources;
using System.Globalization;
using System.Text;
using System.Threading;

using EcmaScript.NET;
using EcmaScript.NET.Types;
using EcmaScript.NET.Types.RegExp;
using EcmaScript.NET.Types.E4X;
using EcmaScript.NET.Collections;

namespace EcmaScript.NET
{

    /// <summary> This is the class that implements the runtime.
    /// 
    /// </summary>
    public class ScriptRuntime
    {

        /// <summary> No instances should be created.</summary>
        protected internal ScriptRuntime ()
        {
        }

        public const int MAXSTACKSIZE = 1000;
        
        private const string XML_INIT_CLASS = "EcmaScript.NET.Xml.Impl.XMLLib";

        private static readonly object LIBRARY_SCOPE_KEY = new object ();

        public static bool IsNativeRuntimeType (Type cl)
        {
            if (cl.IsPrimitive) {
                return (cl != typeof (char));
            }
            else {
                return (cl == typeof (string) || cl == typeof (bool)
                    || CliHelper.IsNumberType (cl)
                    || typeof (IScriptable).IsAssignableFrom (cl));
            }
        }

        public static ScriptableObject InitStandardObjects (Context cx, ScriptableObject scope, bool zealed)
        {
            if (scope == null) {
                scope = new BuiltinObject ();
            }
            scope.AssociateValue (LIBRARY_SCOPE_KEY, scope);

            BaseFunction.Init (scope, zealed);
            BuiltinObject.Init (scope, zealed);

            IScriptable objectProto = ScriptableObject.GetObjectPrototype (scope);

            // Function.prototype.__proto__ should be Object.prototype
            IScriptable functionProto = ScriptableObject.GetFunctionPrototype (scope);
            functionProto.SetPrototype (objectProto);

            // Set the prototype of the object passed in if need be
            if (scope.GetPrototype () == null)
                scope.SetPrototype (objectProto);

            // must precede NativeGlobal since it's needed therein
            BuiltinError.Init (scope, zealed);
            BuiltinGlobal.Init (cx, scope, zealed);

            if (scope is BuiltinGlobalObject) {
                ((BuiltinGlobalObject)scope).Init (scope, zealed);
            }

            BuiltinArray.Init (scope, zealed);
            BuiltinString.Init (scope, zealed);
            BuiltinBoolean.Init (scope, zealed);
            BuiltinNumber.Init (scope, zealed);
            BuiltinDate.Init (scope, zealed);
            BuiltinMath.Init (scope, zealed);

            BuiltinWith.Init (scope, zealed);
            BuiltinCall.Init (scope, zealed);
            BuiltinScript.Init (scope, zealed);

            BuiltinRegExp.Init (scope, zealed);

            if (cx.HasFeature (Context.Features.E4x)) {
                Types.E4X.XMLLib.Init (scope, zealed);
            }

            Continuation.Init (scope, zealed);
        
            if (cx.HasFeature (Context.Features.NonEcmaItObject)) {
                InitItObject (cx, scope);
            }

            return scope;
        }
        
        static void InitItObject (Context cx, ScriptableObject scope) {
            BuiltinObject itObj = new BuiltinObject ();
            itObj.SetPrototype (scope);
            itObj.DefineProperty ("color", Undefined.Value, ScriptableObject.PERMANENT);
            itObj.DefineProperty ("height", Undefined.Value, ScriptableObject.PERMANENT);
            itObj.DefineProperty ("width", Undefined.Value, ScriptableObject.PERMANENT);
            itObj.DefineProperty ("funny", Undefined.Value, ScriptableObject.PERMANENT);
            itObj.DefineProperty ("array", Undefined.Value, ScriptableObject.PERMANENT);
            itObj.DefineProperty ("rdonly", Undefined.Value, ScriptableObject.READONLY);
            scope.DefineProperty ("it", itObj, ScriptableObject.PERMANENT);
        }

        public static ScriptableObject getLibraryScopeOrNull (IScriptable scope)
        {
            ScriptableObject libScope;
            libScope = (ScriptableObject)ScriptableObject.GetTopScopeValue (scope, LIBRARY_SCOPE_KEY);
            return libScope;
        }

        // It is public so NativeRegExp can access it .
        public static bool isJSLineTerminator (int c)
        {
            // Optimization for faster check for eol character:
            // they do not have 0xDFD0 bits set
            if ((c & 0xDFD0) != 0) {
                return false;
            }
            return c == '\n' || c == '\r' || c == 0x2028 || c == 0x2029;
        }


        /// <summary> Helper function for builtin objects that use the varargs form.
        /// ECMA function formal arguments are undefined if not supplied;
        /// this function pads the argument array out to the expected
        /// length, if necessary.
        /// </summary>
        public static object [] padArguments (object [] args, int count)
        {
            if (count < args.Length)
                return args;

            int i;
            object [] result = new object [count];
            for (i = 0; i < args.Length; i++) {
                result [i] = args [i];
            }

            for (; i < count; i++) {
                result [i] = Undefined.Value;
            }

            return result;
        }


        public static string escapeString (string s)
        {
            return escapeString (s, '"');
        }

        /// <summary> For escaping strings printed by object and array literals; not quite
        /// the same as 'escape.'
        /// </summary>
        public static string escapeString (string s, char escapeQuote)
        {
            if (!(escapeQuote == '"' || escapeQuote == '\''))
                Context.CodeBug ();
            System.Text.StringBuilder sb = null;

            for (int i = 0, L = s.Length; i != L; ++i) {
                int c = s [i];

                if (' ' <= c && c <= '~' && c != escapeQuote && c != '\\') {
                    // an ordinary print character (like C isprint()) and not "
                    // or \ .
                    if (sb != null) {
                        sb.Append ((char)c);
                    }
                    continue;
                }
                if (sb == null) {
                    sb = new System.Text.StringBuilder (L + 3);
                    sb.Append (s);
                    sb.Length = i;
                }

                int escape = -1;
                switch (c) {

                    case '\b':
                        escape = 'b';
                        break;

                    case '\f':
                        escape = 'f';
                        break;

                    case '\n':
                        escape = 'n';
                        break;

                    case '\r':
                        escape = 'r';
                        break;

                    case '\t':
                        escape = 't';
                        break;

                    case 0xb:
                        escape = 'v';
                        break; // Java lacks \v.

                    case ' ':
                        escape = ' ';
                        break;

                    case '\\':
                        escape = '\\';
                        break;
                }
                if (escape >= 0) {
                    // an \escaped sort of character
                    sb.Append ('\\');
                    sb.Append ((char)escape);
                }
                else if (c == escapeQuote) {
                    sb.Append ('\\');
                    sb.Append (escapeQuote);
                }
                else {
                    int hexSize;
                    if (c < 256) {
                        // 2-digit hex
                        sb.Append ("\\x");
                        hexSize = 2;
                    }
                    else {
                        // Unicode.
                        sb.Append ("\\u");
                        hexSize = 4;
                    }
                    // append hexadecimal form of c left-padded with 0
                    for (int shift = (hexSize - 1) * 4; shift >= 0; shift -= 4) {
                        int digit = 0xf & (c >> shift);
                        int hc = (digit < 10) ? '0' + digit : 'a' - 10 + digit;
                        sb.Append ((char)hc);
                    }
                }
            }
            return (sb == null) ? s : sb.ToString ();
        }

        internal static bool isValidIdentifierName (string s)
        {
            int L = s.Length;
            if (L == 0)
                return false;
            if (!(char.IsLetter (s [0]) || s [0].CompareTo ('$') == 0 || s [0].CompareTo ('_') == 0))
                return false;
            for (int i = 1; i != L; ++i) {
                if (!TokenStream.IsJavaIdentifierPart (s [i]))
                    return false;
            }
            return !TokenStream.isKeyword (s);
        }



        internal static string DefaultObjectToString (IScriptable obj)
        {
            return "[object " + obj.ClassName + ']';
        }

        internal static string uneval (Context cx, IScriptable scope, object value)
        {
            if (value == null) {
                return "null";
            }
            if (value == Undefined.Value) {
                return "undefined";
            }
            if (value is string) {
                string escaped = escapeString ((string)value);
                System.Text.StringBuilder sb = new System.Text.StringBuilder (escaped.Length + 2);
                sb.Append ('\"');
                sb.Append (escaped);
                sb.Append ('\"');
                return sb.ToString ();
            }
            if (CliHelper.IsNumber (value)) {
                double d = Convert.ToDouble (value);
                if (d == 0 && 1 / d < 0) {
                    return "-0";
                }
                return ScriptConvert.ToString (d);
            }
            if (value is bool) {
                return ScriptConvert.ToString (value);
            }
            if (value is IScriptable) {
                IScriptable obj = (IScriptable)value;
                object v = ScriptableObject.GetProperty (obj, "toSource");
                if (v is IFunction) {
                    IFunction f = (IFunction)v;
                    return ScriptConvert.ToString (f.Call (cx, scope, obj, EmptyArgs));
                }
                return ScriptConvert.ToString (value);
            }
            WarnAboutNonJSObject (value);
            return value.ToString ();
        }

        internal static string defaultObjectToSource (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            using (Helpers.StackOverflowVerifier sov = new Helpers.StackOverflowVerifier (1024)) {
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

                System.Text.StringBuilder result = new System.Text.StringBuilder (128);
                if (toplevel) {
                    result.Append ("(");
                }
                result.Append ('{');

                // Make sure cx.iterating is set to null when done
                // so we don't leak memory
                try {
                    if (!iterating) {
                        cx.iterating.intern (thisObj); // stop recursion.
                        object [] ids = thisObj.GetIds ();
                        for (int i = 0; i < ids.Length; i++) {
                            if (i > 0)
                                result.Append (", ");
                            object id = ids [i];
                            object value;
                            if (id is int) {
                                int intId = ((int)id);
                                value = thisObj.Get (intId, thisObj);
                                result.Append (intId);
                            }
                            else {
                                string strId = (string)id;
                                value = thisObj.Get (strId, thisObj);
                                if (ScriptRuntime.isValidIdentifierName (strId)) {
                                    result.Append (strId);
                                }
                                else {
                                    result.Append ('\'');
                                    result.Append (ScriptRuntime.escapeString (strId, '\''));
                                    result.Append ('\'');
                                }
                            }
                            result.Append (':');
                            result.Append (ScriptRuntime.uneval (cx, scope, value));
                        }
                    }
                }
                finally {
                    if (toplevel) {
                        cx.iterating = null;
                    }
                }

                result.Append ('}');
                if (toplevel) {
                    result.Append (')');
                }
                return result.ToString ();
            }
        }




        public static IScriptable NewObject (Context cx, IScriptable scope, string constructorName, object [] args)
        {
            scope = ScriptableObject.GetTopLevelScope (scope);
            IFunction ctor = getExistingCtor (cx, scope, constructorName);
            if (args == null) {
                args = ScriptRuntime.EmptyArgs;
            }
            return ctor.Construct (cx, scope, args);
        }


        // TODO: this is until setDefaultNamespace will learn how to store NS
        // TODO: properly and separates namespace form Scriptable.get etc.
        private const string DEFAULT_NS_TAG = "__default_namespace__";

        public static object setDefaultNamespace (object ns, Context cx)
        {
            IScriptable scope = cx.currentActivationCall;
            if (scope == null) {
                scope = getTopCallScope (cx);
            }

            XMLLib xmlLib = CurrentXMLLib (cx);
            object obj = xmlLib.ToDefaultXmlNamespace (cx, ns);

            // TODO: this should be in separated namesapce from Scriptable.get/put
            if (!scope.Has (DEFAULT_NS_TAG, scope)) {
                // TODO: this is racy of cause
                ScriptableObject.DefineProperty (scope, DEFAULT_NS_TAG, obj, ScriptableObject.PERMANENT | ScriptableObject.DONTENUM);
            }
            else {
                scope.Put (DEFAULT_NS_TAG, scope, obj);
            }

            return Undefined.Value;
        }

        public static object searchDefaultNamespace (Context cx)
        {
            IScriptable scope = cx.currentActivationCall;
            if (scope == null) {
                scope = getTopCallScope (cx);
            }
            object nsObject;
            for (; ; ) {
                IScriptable parent = scope.ParentScope;
                if (parent == null) {
                    nsObject = ScriptableObject.GetProperty (scope, DEFAULT_NS_TAG);
                    if (nsObject == UniqueTag.NotFound) {
                        return null;
                    }
                    break;
                }
                nsObject = scope.Get (DEFAULT_NS_TAG, scope);
                if (nsObject != UniqueTag.NotFound) {
                    break;
                }
                scope = parent;
            }
            return nsObject;
        }

        public static object getTopLevelProp (IScriptable scope, string id)
        {
            scope = ScriptableObject.GetTopLevelScope (scope);
            return ScriptableObject.GetProperty (scope, id);
        }

        internal static IFunction getExistingCtor (Context cx, IScriptable scope, string constructorName)
        {
            object ctorVal = ScriptableObject.GetProperty (scope, constructorName);
            if (ctorVal is IFunction) {
                return (IFunction)ctorVal;
            }
            if (ctorVal == UniqueTag.NotFound) {
                throw Context.ReportRuntimeErrorById ("msg.ctor.not.found", constructorName);
            }
            else {
                throw Context.ReportRuntimeErrorById ("msg.not.ctor", constructorName);
            }
        }

        /// <summary> Return -1L if str is not an index or the index value as lower 32
        /// bits of the result.
        /// </summary>
        private static long indexFromString (string str)
        {
            // The length of the decimal string representation of
            //  Integer.MAX_VALUE, 2147483647            
            const int MAX_VALUE_LENGTH = 10;

            int len = str.Length;
            if (len > 0) {
                int i = 0;
                bool negate = false;
                int c = str [0];
                if (c == '-') {
                    if (len > 1) {
                        c = str [1];
                        i = 1;
                        negate = true;
                    }
                }
                c -= '0';
                if (0 <= c && c <= 9 && len <= (negate ? MAX_VALUE_LENGTH + 1 : MAX_VALUE_LENGTH)) {
                    // Use negative numbers to accumulate index to handle
                    // Integer.MIN_VALUE that is greater by 1 in absolute value
                    // then Integer.MAX_VALUE
                    int index = -c;
                    int oldIndex = 0;
                    i++;
                    if (index != 0) {
                        // Note that 00, 01, 000 etc. are not indexes
                        while (i != len && 0 <= (c = str [i] - '0') && c <= 9) {
                            oldIndex = index;
                            index = 10 * index - c;
                            i++;
                        }
                    }
                    // Make sure all characters were consumed and that it couldn't
                    // have overflowed.
                    if (i == len && (oldIndex > (int.MinValue / 10) || (oldIndex == (int.MinValue / 10) && c <= (negate ? -(int.MinValue % 10) : (int.MaxValue % 10))))) {
                        return unchecked ((int)0xFFFFFFFFL) & (negate ? index : -index);
                    }
                }
            }
            return -1L;
        }

        /// <summary> If str is a decimal presentation of Uint32 value, return it as long.
        /// Othewise return -1L;
        /// </summary>
        public static long testUint32String (string str)
        {
            // The length of the decimal string representation of
            //  UINT32_MAX_VALUE, 4294967296            
            const int MAX_VALUE_LENGTH = 10;

            int len = str.Length;
            if (1 <= len && len <= MAX_VALUE_LENGTH) {
                int c = str [0];
                c -= '0';
                if (c == 0) {
                    // Note that 00,01 etc. are not valid Uint32 presentations
                    return (len == 1) ? 0L : -1L;
                }
                if (1 <= c && c <= 9) {
                    long v = c;
                    for (int i = 1; i != len; ++i) {
                        c = str [i] - '0';
                        if (!(0 <= c && c <= 9)) {
                            return -1;
                        }
                        v = 10 * v + c;
                    }
                    // Check for overflow
                    if ((ulong)v >> 32 == 0) {
                        return v;
                    }
                }
            }
            return -1;
        }

        /// <summary> If s represents index, then return index value wrapped as Integer
        /// and othewise return s.
        /// </summary>
        internal static object getIndexObject (string s)
        {
            long indexTest = indexFromString (s);
            if (indexTest >= 0) {
                return (int)indexTest;
            }
            return s;
        }

        /// <summary> If d is exact int value, return its value wrapped as Integer
        /// and othewise return d converted to String.
        /// </summary>
        internal static object getIndexObject (double d)
        {
            int i = (int)d;
            if ((double)i == d) {
                return (int)i;
            }
            return ScriptConvert.ToString (d);
        }

        /// <summary> If ScriptConvert.ToString(id) is a decimal presentation of int32 value, then id
        /// is index. In this case return null and make the index available
        /// as ScriptRuntime.lastIndexResult(cx). Otherwise return ScriptConvert.ToString(id).
        /// </summary>
        internal static string ToStringIdOrIndex (Context cx, object id)
        {
            if (CliHelper.IsNumber (id)) {
                double d = Convert.ToDouble (id);
                int index = (int)d;
                if (((double)index) == d) {
                    storeIndexResult (cx, index);
                    return null;
                }
                return ScriptConvert.ToString (id);
            }
            else {
                string s;
                if (id is string) {
                    s = ((string)id);
                }
                else {
                    s = ScriptConvert.ToString (id);
                }
                long indexTest = indexFromString (s);
                if (indexTest >= 0) {
                    storeIndexResult (cx, (int)indexTest);
                    return null;
                }
                return s;
            }
        }

        /// <summary> Call obj.[[Get]](id)</summary>
        public static object getObjectElem (object obj, object elem, Context cx)
        {
            IScriptable sobj = ScriptConvert.ToObjectOrNull (cx, obj);
            if (sobj == null) {
                throw UndefReadError (obj, elem);
            }
            return getObjectElem (sobj, elem, cx);
        }

        public static object getObjectElem (IScriptable obj, object elem, Context cx)
        {
            if (obj is XMLObject) {
                XMLObject xmlObject = (XMLObject)obj;
                return xmlObject.EcmaGet (cx, elem);
            }

            object result;

            string s = ScriptRuntime.ToStringIdOrIndex (cx, elem);
            if (s == null) {
                int index = lastIndexResult (cx);
                result = ScriptableObject.GetProperty (obj, index);
            }
            else {
                result = ScriptableObject.GetProperty (obj, s);
            }

            if (result == UniqueTag.NotFound) {
                result = Undefined.Value;
            }

            return result;
        }

        /// <summary> Version of getObjectElem when elem is a valid JS identifier name.</summary>
        public static object getObjectProp (object obj, string property, Context cx)
        {
            IScriptable sobj = ScriptConvert.ToObjectOrNull (cx, obj);
            if (sobj == null) {
                throw UndefReadError (obj, property);
            }
            return getObjectProp (sobj, property, cx);
        }

        public static object getObjectProp (IScriptable obj, string property, Context cx)
        {
            if (obj is XMLObject) {
                XMLObject xmlObject = (XMLObject)obj;
                return xmlObject.EcmaGet (cx, property);
            }

            object result = ScriptableObject.GetProperty (obj, property);
            if (result == UniqueTag.NotFound) {
                result = Undefined.Value;
            }

            return result;
        }

        /*
        * A cheaper and less general version of the above for well-known argument
        * types.
        */
        public static object getObjectIndex (object obj, double dblIndex, Context cx)
        {
            IScriptable sobj = ScriptConvert.ToObjectOrNull (cx, obj);
            if (sobj == null) {
                throw UndefReadError (obj, ScriptConvert.ToString (dblIndex));
            }
            int index = (int)dblIndex;
            if ((double)index == dblIndex) {
                return getObjectIndex (sobj, index, cx);
            }
            else {
                string s = ScriptConvert.ToString (dblIndex);
                return getObjectProp (sobj, s, cx);
            }
        }

        public static object getObjectIndex (IScriptable obj, int index, Context cx)
        {
            if (obj is XMLObject) {
                XMLObject xmlObject = (XMLObject)obj;
                return xmlObject.EcmaGet (cx, (object)index);
            }

            object result = ScriptableObject.GetProperty (obj, index);
            if (result == UniqueTag.NotFound) {
                result = Undefined.Value;
            }

            return result;
        }

        /*
        * Call obj.[[Put]](id, value)
        */
        public static object setObjectElem (object obj, object elem, object value, Context cx)
        {
            IScriptable sobj = ScriptConvert.ToObjectOrNull (cx, obj);
            if (sobj == null) {
                throw UndefWriteError (obj, elem, value);
            }
            return setObjectElem (sobj, elem, value, cx);
        }

        public static object setObjectElem (IScriptable obj, object elem, object value, Context cx)
        {
            if (obj is XMLObject) {
                XMLObject xmlObject = (XMLObject)obj;
                xmlObject.EcmaPut (cx, elem, value);
                return value;
            }

            string s = ScriptRuntime.ToStringIdOrIndex (cx, elem);
            if (s == null) {
                int index = lastIndexResult (cx);
                ScriptableObject.PutProperty (obj, index, value);
            }
            else {
                ScriptableObject.PutProperty (obj, s, value);
            }

            return value;
        }

        /// <summary> Version of setObjectElem when elem is a valid JS identifier name.</summary>
        public static object setObjectProp (object obj, string property, object value, Context cx)
        {
            IScriptable sobj = ScriptConvert.ToObjectOrNull (cx, obj);
            if (sobj == null) {
                throw UndefWriteError (obj, property, value);
            }
            return setObjectProp (sobj, property, value, cx);
        }

        public static object setObjectProp (IScriptable obj, string property, object value, Context cx)
        {
            if (obj is XMLObject) {
                XMLObject xmlObject = (XMLObject)obj;
                xmlObject.EcmaPut (cx, property, value);
            }
            else {
                return ScriptableObject.PutProperty (obj, property, value);
            }
            return value;
        }

        /*
        * A cheaper and less general version of the above for well-known argument
        * types.
        */
        public static object setObjectIndex (object obj, double dblIndex, object value, Context cx)
        {
            IScriptable sobj = ScriptConvert.ToObjectOrNull (cx, obj);
            if (sobj == null) {
                throw UndefWriteError (obj, Convert.ToString (dblIndex), value);
            }
            int index = (int)dblIndex;
            if ((double)index == dblIndex) {
                return setObjectIndex (sobj, index, value, cx);
            }
            else {
                string s = ScriptConvert.ToString (dblIndex);
                return setObjectProp (sobj, s, value, cx);
            }
        }

        public static object setObjectIndex (IScriptable obj, int index, object value, Context cx)
        {
            if (obj is XMLObject) {
                XMLObject xmlObject = (XMLObject)obj;
                xmlObject.EcmaPut (cx, (object)index, value);
            }
            else {
                return ScriptableObject.PutProperty (obj, index, value);
            }
            return value;
        }

        public static bool deleteObjectElem (IScriptable target, object elem, Context cx)
        {
            bool result;
            if (target is XMLObject) {
                XMLObject xmlObject = (XMLObject)target;
                result = xmlObject.EcmaDelete (cx, elem);
            }
            else {
                string s = ScriptRuntime.ToStringIdOrIndex (cx, elem);
                if (s == null) {
                    int index = lastIndexResult (cx);
                    result = ScriptableObject.DeleteProperty (target, index);
                }
                else {
                    result = ScriptableObject.DeleteProperty (target, s);
                }
            }
            return result;
        }

        public static bool hasObjectElem (IScriptable target, object elem, Context cx)
        {
            bool result;

            if (target is XMLObject) {
                XMLObject xmlObject = (XMLObject)target;
                result = xmlObject.EcmaHas (cx, elem);
            }
            else {
                string s = ScriptRuntime.ToStringIdOrIndex (cx, elem);
                if (s == null) {
                    int index = lastIndexResult (cx);
                    result = ScriptableObject.HasProperty (target, index);
                }
                else {
                    result = ScriptableObject.HasProperty (target, s);
                }
            }

            return result;
        }

        public static object refGet (IRef rf, Context cx)
        {
            return rf.Get (cx);
        }

        public static object refSet (IRef rf, object value, Context cx)
        {
            return rf.Set (cx, value);
        }

        public static object refDel (IRef rf, Context cx)
        {
            return rf.Delete (cx);
        }

        internal static bool isSpecialProperty (string s)
        {
            return s.Equals ("__proto__") || s.Equals ("__parent__");
        }

        public static IRef specialRef (object obj, string specialProperty, Context cx)
        {
            return SpecialRef.createSpecial (cx, obj, specialProperty);
        }

        /// <summary> The delete operator
        /// 
        /// See ECMA 11.4.1
        /// 
        /// In ECMA 0.19, the description of the delete operator (11.4.1)
        /// assumes that the [[Delete]] method returns a value. However,
        /// the definition of the [[Delete]] operator (8.6.2.5) does not
        /// define a return value. Here we assume that the [[Delete]]
        /// method doesn't return a value.
        /// </summary>
        public static object delete (object obj, object id, Context cx)
        {
            IScriptable sobj = ScriptConvert.ToObjectOrNull (cx, obj);
            if (sobj == null) {
                string idStr = (id == null) ? "null" : id.ToString ();
                throw TypeErrorById ("msg.undef.prop.delete", ScriptConvert.ToString (obj), idStr);
            }
            bool result = deleteObjectElem (sobj, id, cx);
            return result;
        }

        /// <summary> Looks up a name in the scope chain and returns its value.</summary>
        public static object name (Context cx, IScriptable scope, string name)
        {
            IScriptable parent = scope.ParentScope;
            if (parent == null) {
                object result = topScopeName (cx, scope, name);
                if (result == UniqueTag.NotFound) {
                    throw NotFoundError (scope, name);
                }
                return result;
            }

            return nameOrFunction (cx, scope, parent, name, false);
        }

        private static object nameOrFunction (Context cx, IScriptable scope, IScriptable parentScope, string name, bool asFunctionCall)
        {
            object result;
            IScriptable thisObj = scope; // It is used only if asFunctionCall==true.

            XMLObject firstXMLObject = null;
            for (; ; ) {
                if (scope is BuiltinWith) {
                    IScriptable withObj = scope.GetPrototype ();
                    if (withObj is XMLObject) {
                        XMLObject xmlObj = (XMLObject)withObj;
                        if (xmlObj.EcmaHas (cx, name)) {
                            // function this should be the target object of with
                            thisObj = xmlObj;
                            result = xmlObj.EcmaGet (cx, name);
                            break;
                        }
                        if (firstXMLObject == null) {
                            firstXMLObject = xmlObj;
                        }
                    }
                    else {
                        result = ScriptableObject.GetProperty (withObj, name);
                        if (result != UniqueTag.NotFound) {
                            // function this should be the target object of with
                            thisObj = withObj;
                            break;
                        }
                    }
                }
                else if (scope is BuiltinCall) {
                    // NativeCall does not prototype chain and Scriptable.get
                    // can be called directly.
                    result = scope.Get (name, scope);
                    if (result != UniqueTag.NotFound) {
                        if (asFunctionCall) {
                            // ECMA 262 requires that this for nested funtions
                            // should be top scope
                            thisObj = ScriptableObject.GetTopLevelScope (parentScope);
                        }
                        break;
                    }
                }
                else {
                    // Can happen if embedding decided that nested
                    // scopes are useful for what ever reasons.
                    result = ScriptableObject.GetProperty (scope, name);
                    if (result != UniqueTag.NotFound) {
                        thisObj = scope;
                        break;
                    }
                }
                scope = parentScope;
                parentScope = parentScope.ParentScope;
                if (parentScope == null) {
                    result = topScopeName (cx, scope, name);
                    if (result == UniqueTag.NotFound) {
                        if (firstXMLObject == null || asFunctionCall) {
                            throw NotFoundError (scope, name);
                        }
                        // The name was not found, but we did find an XML
                        // object in the scope chain and we are looking for name,
                        // not function. The result should be an empty XMLList
                        // in name context.
                        result = firstXMLObject.EcmaGet (cx, name);
                    }
                    // For top scope thisObj for functions is always scope itself.
                    thisObj = scope;
                    break;
                }
            }

            if (asFunctionCall) {
                if (!(result is ICallable)) {
                    throw NotFunctionError (result, name);
                }
                storeScriptable (cx, thisObj);
            }

            return result;
        }

        private static object topScopeName (Context cx, IScriptable scope, string name)
        {
            if (cx.useDynamicScope) {
                scope = checkDynamicScope (cx.topCallScope, scope);
            }
            return ScriptableObject.GetProperty (scope, name);
        }


        /// <summary> Returns the object in the scope chain that has a given property.
        /// 
        /// The order of evaluation of an assignment expression involves
        /// evaluating the lhs to a reference, evaluating the rhs, and then
        /// modifying the reference with the rhs value. This method is used
        /// to 'bind' the given name to an object containing that property
        /// so that the side effects of evaluating the rhs do not affect
        /// which property is modified.
        /// Typically used in conjunction with setName.
        /// 
        /// See ECMA 10.1.4
        /// </summary>
        public static IScriptable bind (Context cx, IScriptable scope, string id)
        {
            IScriptable firstXMLObject = null;
            IScriptable parent = scope.ParentScope;
            if (parent != null) {
                // Check for possibly nested "with" scopes first
                while (scope is BuiltinWith) {
                    IScriptable withObj = scope.GetPrototype ();
                    if (withObj is XMLObject) {
                        XMLObject xmlObject = (XMLObject)withObj;
                        if (xmlObject.EcmaHas (cx, id)) {
                            return xmlObject;
                        }
                        if (firstXMLObject == null) {
                            firstXMLObject = xmlObject;
                        }
                    }
                    else {
                        if (ScriptableObject.HasProperty (withObj, id)) {
                            return withObj;
                        }
                    }
                    scope = parent;
                    parent = parent.ParentScope;
                    if (parent == null) {

                        goto childScopesChecks_brk;
                    }
                }
                for (; ; ) {
                    if (ScriptableObject.HasProperty (scope, id)) {
                        return scope;
                    }
                    scope = parent;
                    parent = parent.ParentScope;
                    if (parent == null) {

                        goto childScopesChecks_brk;
                    }
                }
            }

        childScopesChecks_brk:
            ;

            // scope here is top scope
            if (cx.useDynamicScope) {
                scope = checkDynamicScope (cx.topCallScope, scope);
            }
            if (ScriptableObject.HasProperty (scope, id)) {
                return scope;
            }
            // Nothing was found, but since XML objects always bind
            // return one if found
            return firstXMLObject;
        }

        public static object setName (IScriptable bound, object value, Context cx, IScriptable scope, string id)
        {
            if (bound != null) {
                if (bound is XMLObject) {
                    XMLObject xmlObject = (XMLObject)bound;
                    xmlObject.EcmaPut (cx, id, value);
                }
                else {
                    ScriptableObject.PutProperty (bound, id, value);
                }
            }
            else {
                // "newname = 7;", where 'newname' has not yet
                // been defined, creates a new property in the
                // top scope unless strict mode is specified.
                if (cx.HasFeature (Context.Features.StrictVars)) {
                    throw Context.ReportRuntimeErrorById ("msg.assn.create.strict", id);
                }
                // Find the top scope by walking up the scope chain.
                bound = ScriptableObject.GetTopLevelScope (scope);
                if (cx.useDynamicScope) {
                    bound = checkDynamicScope (cx.topCallScope, bound);
                }
                bound.Put (id, bound, value);
            }
            return value;
        }






        /// <summary> Prepare for calling name(...): return function corresponding to
        /// name and make current top scope available
        /// as ScriptRuntime.lastStoredScriptable() for consumption as thisObj.
        /// The caller must call ScriptRuntime.lastStoredScriptable() immediately
        /// after calling this method.
        /// </summary>
        public static ICallable getNameFunctionAndThis (string name, Context cx, IScriptable scope)
        {
            IScriptable parent = scope.ParentScope;
            if (parent == null) {
                object result = topScopeName (cx, scope, name);
                if (!(result is ICallable)) {
                    if (result == UniqueTag.NotFound) {
                        throw NotFoundError (scope, name);
                    }
                    else {
                        throw NotFunctionError (result, name);
                    }
                }
                // Top scope is not NativeWith or NativeCall => thisObj == scope
                IScriptable thisObj = scope;
                storeScriptable (cx, thisObj);
                return (ICallable)result;
            }

            // name will call storeScriptable(cx, thisObj);
            return (ICallable)nameOrFunction (cx, scope, parent, name, true);
        }

        /// <summary> Prepare for calling obj[id](...): return function corresponding to
        /// obj[id] and make obj properly converted to Scriptable available
        /// as ScriptRuntime.lastStoredScriptable() for consumption as thisObj.
        /// The caller must call ScriptRuntime.lastStoredScriptable() immediately
        /// after calling this method.
        /// </summary>
        public static ICallable GetElemFunctionAndThis (object obj, object elem, Context cx)
        {
            string s = ScriptRuntime.ToStringIdOrIndex (cx, elem);
            if (s != null) {
                return getPropFunctionAndThis (obj, s, cx);
            }
            int index = lastIndexResult (cx);

            IScriptable thisObj = ScriptConvert.ToObjectOrNull (cx, obj);
            if (thisObj == null) {
                throw UndefCallError (obj, Convert.ToString (index));
            }

            object value;
            for (; ; ) {
                // Ignore XML lookup as requred by ECMA 357, 11.2.2.1
                value = ScriptableObject.GetProperty (thisObj, index);
                if (value != UniqueTag.NotFound) {
                    break;
                }
                if (!(thisObj is XMLObject)) {
                    break;
                }
                XMLObject xmlObject = (XMLObject)thisObj;
                IScriptable extra = xmlObject.GetExtraMethodSource (cx);
                if (extra == null) {
                    break;
                }
                thisObj = extra;
            }
            if (!(value is ICallable)) {
                throw NotFunctionError (value, elem);
            }

            storeScriptable (cx, thisObj);
            return (ICallable)value;
        }

        /// <summary> Prepare for calling obj.property(...): return function corresponding to
        /// obj.property and make obj properly converted to Scriptable available
        /// as ScriptRuntime.lastStoredScriptable() for consumption as thisObj.
        /// The caller must call ScriptRuntime.lastStoredScriptable() immediately
        /// after calling this method.
        /// </summary>
        public static ICallable getPropFunctionAndThis (object obj, string property, Context cx)
        {
            IScriptable thisObj = ScriptConvert.ToObjectOrNull (cx, obj);
            if (thisObj == null) {
                throw UndefCallError (obj, property);
            }

            object value;
            for (; ; ) {
                // Ignore XML lookup as requred by ECMA 357, 11.2.2.1
                value = ScriptableObject.GetProperty (thisObj, property);
                if (value != UniqueTag.NotFound) {
                    break;
                }
                if (!(thisObj is XMLObject)) {
                    break;
                }
                XMLObject xmlObject = (XMLObject)thisObj;
                IScriptable extra = xmlObject.GetExtraMethodSource (cx);
                if (extra == null) {
                    break;
                }
                thisObj = extra;
            }
            
            if (value == UniqueTag.NotFound) {
                //if (thisObj.Get ("__noSuchMethod__", thisObj) as ICallable != null) {
                //    return UniqueTag.NoSuchMethodMark;
                //}
            }

            if (!(value is ICallable)) {            
                throw NotFunctionError (value, property);
            }

            storeScriptable (cx, thisObj);
            return (ICallable)value;
        }

        /// <summary> Prepare for calling <expression>(...): return function corresponding to
        /// <expression> and make parent scope of the function available
        /// as ScriptRuntime.lastStoredScriptable() for consumption as thisObj.
        /// The caller must call ScriptRuntime.lastStoredScriptable() immediately
        /// after calling this method.
        /// </summary>
        public static ICallable getValueFunctionAndThis (object value, Context cx)
        {
            if (!(value is ICallable)) {
                throw NotFunctionError (value);
            }

            ICallable f = (ICallable)value;
            IScriptable thisObj;
            if (f is IScriptable) {
                thisObj = ((IScriptable)f).ParentScope;
            }
            else {
                if (cx.topCallScope == null)
                    throw new Exception ();
                thisObj = cx.topCallScope;
            }
            if (thisObj.ParentScope != null) {
                if (thisObj is BuiltinWith) {
                    // functions defined inside with should have with target
                    // as their thisObj
                }
                else if (thisObj is BuiltinCall) {
                    // nested functions should have top scope as their thisObj
                    thisObj = ScriptableObject.GetTopLevelScope (thisObj);
                }
            }
            storeScriptable (cx, thisObj);
            return f;
        }

        /// <summary> Perform function call in reference context. Should always
        /// return value that can be passed to
        /// {@link #refGet(Object)} or @link #refSet(Object, Object)}
        /// arbitrary number of times.
        /// The args array reference should not be stored in any object that is
        /// can be GC-reachable after this method returns. If this is necessary,
        /// store args.clone(), not args array itself.
        /// </summary>
        public static IRef callRef (ICallable function, IScriptable thisObj, object [] args, Context cx)
        {
            if (function is IRefCallable) {
                IRefCallable rfunction = (IRefCallable)function;
                IRef rf = rfunction.RefCall (cx, thisObj, args);
                if (rf == null) {
                    throw new ApplicationException (rfunction.GetType ().FullName + ".refCall() returned null");
                }
                return rf;
            }
            // No runtime support for now
            string msg = GetMessage ("msg.no.ref.from.function", ScriptConvert.ToString (function));
            throw ConstructError ("ReferenceError", msg);
        }

        /// <summary> Operator new.
        /// 
        /// See ECMA 11.2.2
        /// </summary>
        public static IScriptable NewObject (object fun, Context cx, IScriptable scope, object [] args)
        {
            if (!(fun is IFunction)) {
                throw NotFunctionError (fun);
            }
            IFunction function = (IFunction)fun;
            return function.Construct (cx, scope, args);
        }

        public static object callSpecial (Context cx, ICallable fun, IScriptable thisObj, object [] args, IScriptable scope, IScriptable callerThis, int callType, string filename, int lineNumber)
        {
            if (callType == Node.SPECIALCALL_EVAL) {
                if (BuiltinGlobal.isEvalFunction (fun)) {
                    return evalSpecial (cx, scope, callerThis, args, filename, lineNumber);
                }
            }
            else if (callType == Node.SPECIALCALL_WITH) {
                if (BuiltinWith.IsWithFunction (fun)) {
                    throw Context.ReportRuntimeErrorById ("msg.only.from.new", "With");
                }
            }
            else {
                throw Context.CodeBug ();
            }

            return fun.Call (cx, scope, thisObj, args);
        }

        public static object newSpecial (Context cx, object fun, object [] args, IScriptable scope, int callType)
        {
            if (callType == Node.SPECIALCALL_EVAL) {
                if (BuiltinGlobal.isEvalFunction (fun)) {
                    throw TypeErrorById ("msg.not.ctor", "eval");
                }
            }
            else if (callType == Node.SPECIALCALL_WITH) {
                if (BuiltinWith.IsWithFunction (fun)) {
                    return BuiltinWith.NewWithSpecial (cx, scope, args);
                }
            }
            else {
                throw Context.CodeBug ();
            }

            return NewObject (fun, cx, scope, args);
        }

        /// <summary> Function.prototype.apply and Function.prototype.call
        /// 
        /// See Ecma 15.3.4.[34]
        /// </summary>
        public static object applyOrCall (bool isApply, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            int L = args.Length;
            ICallable function;
            if (thisObj is ICallable) {
                function = (ICallable)thisObj;
            }
            else {
                object value = thisObj.GetDefaultValue (typeof (IFunction));
                if (!(value is ICallable)) {
                    throw ScriptRuntime.NotFunctionError (value, thisObj);
                }
                function = (ICallable)value;
            }

            IScriptable callThis = null;
            if (L != 0) {
                callThis = ScriptConvert.ToObjectOrNull (cx, args [0]);
            }
            if (callThis == null) {
                // This covers the case of args[0] == (null|undefined) as well.
                callThis = getTopCallScope (cx);
            }

            object [] callArgs;
            if (isApply) {
                // Follow Ecma 15.3.4.3
                if (L <= 1) {
                    callArgs = ScriptRuntime.EmptyArgs;
                }
                else {
                    object arg1 = args [1];
                    if (arg1 == null || arg1 == Undefined.Value) {
                        callArgs = ScriptRuntime.EmptyArgs;
                    }
                    else if (arg1 is BuiltinArray || arg1 is Arguments) {
                        callArgs = cx.GetElements ((IScriptable)arg1);
                    }
                    else {
                        throw ScriptRuntime.TypeErrorById ("msg.arg.isnt.array");
                    }
                }
            }
            else {
                // Follow Ecma 15.3.4.4
                if (L <= 1) {
                    callArgs = ScriptRuntime.EmptyArgs;
                }
                else {
                    callArgs = new object [L - 1];
                    Array.Copy (args, 1, callArgs, 0, L - 1);
                }
            }

            return function.Call (cx, scope, callThis, callArgs);
        }

        /// <summary> The eval function property of the global object.
        /// 
        /// See ECMA 15.1.2.1
        /// </summary>
        public static object evalSpecial (Context cx, IScriptable scope, object thisArg, object [] args, string filename, int lineNumber)
        {
            if (args.Length < 1)
                return Undefined.Value;
            object x = args [0];
            if (!(x is string)) {
                if (cx.HasFeature (Context.Features.StrictEval)) {
                    throw Context.ReportRuntimeErrorById ("msg.eval.nonstring.strict");
                }
                string message = ScriptRuntime.GetMessage ("msg.eval.nonstring");
                Context.ReportWarning (message);
                return x;
            }
            if (filename == null) {
                int [] linep = new int [1];
                filename = Context.GetSourcePositionFromStack (linep);
                if (filename != null) {
                    lineNumber = linep [0];
                }
                else {
                    filename = "";
                }
            }
            string sourceName = ScriptRuntime.makeUrlForGeneratedScript (true, filename, lineNumber);

            ErrorReporter reporter;
            reporter = DefaultErrorReporter.ForEval (cx.ErrorReporter);

            // Compile with explicit interpreter instance to force interpreter
            // mode.
            IScript script = cx.CompileString ((string)x, new Interpreter (), reporter, sourceName, 1, (object)null);
            ((InterpretedFunction)script).idata.evalScriptFlag = true;
            ICallable c = (ICallable)script;
            return c.Call (cx, scope, (IScriptable)thisArg, ScriptRuntime.EmptyArgs);
        }

        /// <summary> The typeof operator</summary>
        public static string Typeof (object value)
        {
            if (value == null)
                return "object";
            if (value == Undefined.Value)
                return "undefined";
            if (value is IScriptable) {
                if (value is XMLObject)
                    return "xml";

                return (value is ICallable && !(value is BuiltinRegExp)) ? "function" : "object";
            }
            if (value is string)
                return "string";
            if (value is char || CliHelper.IsNumber (value))
                return "number";
            if (value is bool)
                return "boolean";
            throw errorWithClassName ("msg.invalid.type", value);
        }


        internal static ApplicationException errorWithClassName (string msg, object val)
        {
            return Context.ReportRuntimeErrorById (msg, val.GetType ().FullName);
        }

        /// <summary> The typeof operator that correctly handles the undefined case</summary>
        public static string TypeofName (IScriptable scope, string id)
        {
            Context cx = Context.CurrentContext;
            IScriptable val = bind (cx, scope, id);
            if (val == null)
                return "undefined";
            return Typeof (getObjectProp (val, id, cx));
        }

        // neg:
        // implement the '-' operator inline in the caller
        // as "-ScriptConvert.ToNumber(val)"

        // not:
        // implement the '!' operator inline in the caller
        // as "!toBoolean(val)"

        // bitnot:
        // implement the '~' operator inline in the caller
        // as "~toInt32(val)"

        public static object Add (object val1, object val2, Context cx)
        {
            if (CliHelper.IsNumber (val1) && CliHelper.IsNumber (val2)) {
                return (double)val1 + (double)val2;
            }
            if (val1 is XMLObject) {
                object test = ((XMLObject)val1).AddValues (cx, true, val2);
                if (test != UniqueTag.NotFound) {
                    return test;
                }
            }
            if (val2 is XMLObject) {
                object test = ((XMLObject)val2).AddValues (cx, false, val1);
                if (test != UniqueTag.NotFound) {
                    return test;
                }
            }
            if (val1 is EcmaScript.NET.Types.Cli.CliEventInfo) {
                return ((EcmaScript.NET.Types.Cli.CliEventInfo)val1).Add (val2, cx);
            }
            if (val1 is IScriptable)
                val1 = ((IScriptable)val1).GetDefaultValue (null);
            if (val2 is IScriptable)
                val2 = ((IScriptable)val2).GetDefaultValue (null);
            if (!(val1 is string) && !(val2 is string))
                if ((CliHelper.IsNumber (val1)) && (CliHelper.IsNumber (val2)))
                    return (double)val1 + (double)val2;
                else
                    return ScriptConvert.ToNumber (val1) + ScriptConvert.ToNumber (val2);
            return string.Concat (ScriptConvert.ToString (val1), ScriptConvert.ToString (val2));
        }

        public static object nameIncrDecr (IScriptable scopeChain, string id, int incrDecrMask)
        {
            IScriptable target;
            object value;
            {
                do {
                    target = scopeChain;
                    do {
                        value = target.Get (id, scopeChain);
                        if (value != UniqueTag.NotFound) {

                            goto search_brk;
                        }
                        target = target.GetPrototype ();
                    }
                    while (target != null);
                    scopeChain = scopeChain.ParentScope;
                }
                while (scopeChain != null);
                throw NotFoundError (scopeChain, id);
            }

        search_brk:
            ;

            return doScriptableIncrDecr (target, id, scopeChain, value, incrDecrMask);
        }

        public static object propIncrDecr (object obj, string id, Context cx, int incrDecrMask)
        {
            IScriptable start = ScriptConvert.ToObjectOrNull (cx, obj);
            if (start == null) {
                throw UndefReadError (obj, id);
            }

            IScriptable target = start;
            object value;
            {
                do {
                    value = target.Get (id, start);
                    if (value != UniqueTag.NotFound) {

                        goto search1_brk;
                    }
                    target = target.GetPrototype ();
                }
                while (target != null);
                start.Put (id, start, double.NaN);
                return double.NaN;
            }

        search1_brk:
            ;

            return doScriptableIncrDecr (target, id, start, value, incrDecrMask);
        }

        private static object doScriptableIncrDecr (IScriptable target, string id, IScriptable protoChainStart, object value, int incrDecrMask)
        {
            bool post = ((incrDecrMask & Node.POST_FLAG) != 0);
            double number;
            if (CliHelper.IsNumber (value)) {
                number = Convert.ToDouble (value);
            }
            else {
                number = ScriptConvert.ToNumber (value);
                if (post) {
                    // convert result to number
                    value = number;
                }
            }
            if ((incrDecrMask & Node.DECR_FLAG) == 0) {
                ++number;
            }
            else {
                --number;
            }
            object result = number;
            target.Put (id, protoChainStart, result);
            if (post) {
                return value;
            }
            else {
                return result;
            }
        }

        public static object elemIncrDecr (object obj, object index, Context cx, int incrDecrMask)
        {
            object value = getObjectElem (obj, index, cx);
            bool post = ((incrDecrMask & Node.POST_FLAG) != 0);
            double number;
            if (CliHelper.IsNumber (value)) {
                number = Convert.ToDouble (value);
            }
            else {
                number = ScriptConvert.ToNumber (value);
                if (post) {
                    // convert result to number
                    value = number;
                }
            }
            if ((incrDecrMask & Node.DECR_FLAG) == 0) {
                ++number;
            }
            else {
                --number;
            }
            object result = number;
            setObjectElem (obj, index, result, cx);
            if (post) {
                return value;
            }
            else {
                return result;
            }
        }

        public static object refIncrDecr (IRef rf, Context cx, int incrDecrMask)
        {
            object value = rf.Get (cx);
            bool post = ((incrDecrMask & Node.POST_FLAG) != 0);
            double number;
            if (CliHelper.IsNumber (value)) {
                number = Convert.ToDouble (value);
            }
            else {
                number = ScriptConvert.ToNumber (value);
                if (post) {
                    // convert result to number
                    value = number;
                }
            }
            if ((incrDecrMask & Node.DECR_FLAG) == 0) {
                ++number;
            }
            else {
                --number;
            }
            rf.Set (cx, number);
            if (post) {
                return value;
            }
            else {
                return number;
            }
        }


        /// <summary> Equality
        /// 
        /// See ECMA 11.9
        /// </summary>
        public static bool eq (object x, object y)
        {
            if (x == null || x == Undefined.Value) {
                if (y == null || y == Undefined.Value) {
                    return true;
                }
                if (y is ScriptableObject) {
                    object test = ((ScriptableObject)y).EquivalentValues (x);
                    if (test != UniqueTag.NotFound) {
                        return ((bool)test);
                    }
                }
                return false;
            }
            else if (CliHelper.IsNumber (x)) {
                return eqNumber (Convert.ToDouble (x), y);
            }
            else if (x is string) {
                return eqString ((string)x, y);
            }
            else if (x is bool) {
                bool b = ((bool)x);
                if (y is bool) {
                    return b == ((bool)y);
                }
                if (y is ScriptableObject) {
                    object test = ((ScriptableObject)y).EquivalentValues (x);
                    if (test != UniqueTag.NotFound) {
                        return ((bool)test);
                    }
                }
                return eqNumber (b ? 1.0 : 0.0, y);
            }
            else if (x is IScriptable) {
                if (y is IScriptable) {
                    if (x == y) {
                        return true;
                    }
                    if (x is ScriptableObject) {
                        object test = ((ScriptableObject)x).EquivalentValues (y);
                        if (test != UniqueTag.NotFound) {
                            return ((bool)test);
                        }
                    }
                    if (y is ScriptableObject) {
                        object test = ((ScriptableObject)y).EquivalentValues (x);
                        if (test != UniqueTag.NotFound) {
                            return ((bool)test);
                        }
                    }
                    if (x is Wrapper && y is Wrapper) {
                        return ((Wrapper)x).Unwrap () == ((Wrapper)y).Unwrap ();
                    }
                    return false;
                }
                else if (y is bool) {
                    if (x is ScriptableObject) {
                        object test = ((ScriptableObject)x).EquivalentValues (y);
                        if (test != UniqueTag.NotFound) {
                            return ((bool)test);
                        }
                    }
                    double d = ((bool)y) ? 1.0 : 0.0;
                    return eqNumber (d, x);
                }
                else if (CliHelper.IsNumber (y)) {
                    return eqNumber (Convert.ToDouble (y), x);
                }
                else if (y is string) {
                    return eqString ((string)y, x);
                }
                // covers the case when y == Undefined.instance as well
                return false;
            }
            else {
                WarnAboutNonJSObject (x);
                return x == y;
            }
        }

        internal static bool eqNumber (double x, object y)
        {
            for (; ; ) {
                if (y == null || y == Undefined.Value) {
                    return false;
                }
                else if (CliHelper.IsNumber (y)) {
                    return x == Convert.ToDouble (y);
                }
                else if (y is string) {
                    return x == ScriptConvert.ToNumber (y);
                }
                else if (y is bool) {
                    return x == (((bool)y) ? 1.0 : +0.0);
                }
                else if (y is IScriptable) {
                    if (y is ScriptableObject) {
                        object xval = x;
                        object test = ((ScriptableObject)y).EquivalentValues (xval);
                        if (test != UniqueTag.NotFound) {
                            return ((bool)test);
                        }
                    }
                    y = ScriptConvert.ToPrimitive (y);
                }
                else {
                    WarnAboutNonJSObject (y);
                    return false;
                }
            }
        }

        private static bool eqString (string x, object y)
        {
            for (; ; ) {
                if (y == null || y == Undefined.Value) {
                    return false;
                }
                else if (y is string) {
                    return x.Equals (y);
                }
                else if (CliHelper.IsNumber (y)) {
                    return ScriptConvert.ToNumber (x) == Convert.ToDouble (y);
                }
                else if (y is bool) {
                    return ScriptConvert.ToNumber (x) == (((bool)y) ? 1.0 : 0.0);
                }
                else if (y is IScriptable) {
                    if (y is ScriptableObject) {
                        object test = ((ScriptableObject)y).EquivalentValues (x);
                        if (test != UniqueTag.NotFound) {
                            return ((bool)test);
                        }
                    }
                    y = ScriptConvert.ToPrimitive (y);
                    continue;
                }
                else {
                    WarnAboutNonJSObject (y);
                    return false;
                }
            }
        }
        public static bool shallowEq (object x, object y)
        {
            if (x == y) {
                if (!(CliHelper.IsNumber (x))) {
                    return true;
                }
                // double.NaN check
                double d = Convert.ToDouble (x);
                return !double.IsNaN (d);
            }
            if (x == null || x == Undefined.Value) {
                return false;
            }
            else if (CliHelper.IsNumber (x)) {
                if (CliHelper.IsNumber (y)) {
                    return Convert.ToDouble (x) == Convert.ToDouble (y);
                }
            }
            else if (x is string) {
                if (y is string) {
                    return x.Equals (y);
                }
            }
            else if (x is bool) {
                if (y is bool) {
                    return x.Equals (y);
                }
            }
            else if (x is IScriptable) {
                if (x is Wrapper && y is Wrapper) {
                    return ((Wrapper)x).Unwrap () == ((Wrapper)y).Unwrap ();
                }
            }
            else {
                WarnAboutNonJSObject (x);
                return x == y;
            }
            return false;
        }

        /// <summary> The instanceof operator.
        /// 
        /// </summary>
        /// <returns> a instanceof b
        /// </returns>
        public static bool InstanceOf (object a, object b, Context cx)
        {
            IScriptable sB = (b as IScriptable);

            // Check RHS is an object
            if (sB == null) {
                throw TypeErrorById ("msg.instanceof.not.object");
            }

            IScriptable sA = (a as IScriptable);

            // for primitive values on LHS, return false
            // TODO we may want to change this so that 5 instanceof Number == true
            if (sA == null) {                
                return false;
            }


            return sB.HasInstance (sA);
        }

        /// <summary> Delegates to
        /// 
        /// </summary>
        /// <returns> true iff rhs appears in lhs' proto chain
        /// </returns>
        protected internal static bool jsDelegatesTo (IScriptable lhs, IScriptable rhs)
        {
            IScriptable proto = lhs.GetPrototype ();

            while (proto != null) {
                if (proto.Equals (rhs))
                    return true;
                proto = proto.GetPrototype ();
            }

            return false;
        }

        /// <summary> The in operator.
        /// 
        /// This is a new JS 1.3 language feature.  The in operator mirrors
        /// the operation of the for .. in construct, and tests whether the
        /// rhs has the property given by the lhs.  It is different from the
        /// for .. in construct in that:
        /// <BR> - it doesn't perform ToObject on the right hand side
        /// <BR> - it returns true for DontEnum properties.
        /// </summary>
        /// <param name="a">the left hand operand
        /// </param>
        /// <param name="b">the right hand operand
        /// 
        /// </param>
        /// <returns> true if property name or element number a is a property of b
        /// </returns>
        public static bool In (object a, object b, Context cx)
        {
            if (!(b is IScriptable)) {
                throw TypeErrorById ("msg.instanceof.not.object");
            }

            return hasObjectElem ((IScriptable)b, a, cx);
        }

        public static bool cmp_LT (object val1, object val2)
        {
            double d1, d2;
            if (CliHelper.IsNumber (val1) && CliHelper.IsNumber (val2)) {
                d1 = Convert.ToDouble (val1);
                d2 = Convert.ToDouble (val2);
            }
            else {
                if (val1 is IScriptable)
                    val1 = ((IScriptable)val1).GetDefaultValue (typeof (long));
                if (val2 is IScriptable)
                    val2 = ((IScriptable)val2).GetDefaultValue (typeof (long));
                if (val1 is string && val2 is string) {
                    return String.CompareOrdinal (((string)val1), (string)val2) < 0;
                }
                d1 = ScriptConvert.ToNumber (val1);
                d2 = ScriptConvert.ToNumber (val2);
            }
            return d1 < d2;
        }

        public static bool cmp_LE (object val1, object val2)
        {
            double d1, d2;
            if (CliHelper.IsNumber (val1) && CliHelper.IsNumber (val2)) {
                d1 = Convert.ToDouble (val1);
                d2 = Convert.ToDouble (val2);
            }
            else {
                if (val1 is IScriptable)
                    val1 = ((IScriptable)val1).GetDefaultValue (typeof (long));
                if (val2 is IScriptable)
                    val2 = ((IScriptable)val2).GetDefaultValue (typeof (long));
                if (val1 is string && val2 is string) {
                    return String.CompareOrdinal (((string)val1), (string)val2) <= 0;
                }
                d1 = ScriptConvert.ToNumber (val1);
                d2 = ScriptConvert.ToNumber (val2);
            }
            return d1 <= d2;
        }


        public static bool hasTopCall (Context cx)
        {
            return (cx.topCallScope != null);
        }

        public static IScriptable getTopCallScope (Context cx)
        {
            IScriptable scope = cx.topCallScope;
            if (scope == null) {
                throw new ApplicationException ();
            }
            return scope;
        }

        public static object DoTopCall (ICallable callable, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (scope == null)
                throw new ArgumentException ();
            if (cx.topCallScope != null)
                throw new ApplicationException ();

            object result;
            cx.topCallScope = ScriptableObject.GetTopLevelScope (scope);
            cx.useDynamicScope = cx.HasFeature (Context.Features.DynamicScope);
            ContextFactory f = cx.Factory;
            try {
                result = f.DoTopCall (callable, cx, scope, thisObj, args);
            }
            finally {
                cx.topCallScope = null;
                // Cleanup cached references
                cx.cachedXMLLib = null;

                if (cx.currentActivationCall != null) {
                    // Function should always call exitActivationFunction
                    // if it creates activation record
                    throw new ApplicationException (
                        "ActivationCall without exitActivationFunction() invokation."
                    );
                }
            }
            return result;
        }

        /// <summary> Return <tt>possibleDynamicScope</tt> if <tt>staticTopScope</tt>
        /// is present on its prototype chain and return <tt>staticTopScope</tt>
        /// otherwise.
        /// Should only be called when <tt>staticTopScope</tt> is top scope.
        /// </summary>
        internal static IScriptable checkDynamicScope (IScriptable possibleDynamicScope, IScriptable staticTopScope)
        {
            // Return cx.topCallScope if scope
            if (possibleDynamicScope == staticTopScope) {
                return possibleDynamicScope;
            }
            IScriptable proto = possibleDynamicScope;
            for (; ; ) {
                proto = proto.GetPrototype ();
                if (proto == staticTopScope) {
                    return possibleDynamicScope;
                }
                if (proto == null) {
                    return staticTopScope;
                }
            }
        }

        public static void initScript (BuiltinFunction funObj, IScriptable thisObj, Context cx, IScriptable scope, bool evalScript)
        {
            if (cx.topCallScope == null)
                throw new ApplicationException ();

            int varCount = funObj.ParamAndVarCount;
            if (varCount != 0) {

                IScriptable varScope = scope;
                // Never define any variables from var statements inside with
                // object. See bug 38590.
                while (varScope is BuiltinWith) {
                    varScope = varScope.ParentScope;
                }

                for (int i = varCount; i-- != 0; ) {
                    string name = funObj.getParamOrVarName (i);
                    // Don't overwrite existing def if already defined in object
                    // or prototypes of object.
                    if (!ScriptableObject.HasProperty (scope, name)) {
                        if (!evalScript) {
                            // Global var definitions are supposed to be DONTDELETE
                            ScriptableObject.DefineProperty (varScope, name, Undefined.Value, ScriptableObject.PERMANENT);
                        }
                        else {
                            varScope.Put (name, varScope, Undefined.Value);
                        }
                    }
                }
            }
        }

        public static IScriptable createFunctionActivation (BuiltinFunction funObj, IScriptable scope, object [] args)
        {
            return new BuiltinCall (funObj, scope, args);
        }


        public static void enterActivationFunction (Context cx, IScriptable activation)
        {
            if (cx.topCallScope == null)
                throw new ApplicationException ();

            BuiltinCall call = (BuiltinCall)activation;
            call.parentActivationCall = cx.currentActivationCall;
            cx.currentActivationCall = call;
        }

        public static void exitActivationFunction (Context cx)
        {
            BuiltinCall call = cx.currentActivationCall;
            cx.currentActivationCall = call.parentActivationCall;
            call.parentActivationCall = null;
        }

        internal static BuiltinCall findFunctionActivation (Context cx, IFunction f)
        {
            BuiltinCall call = cx.currentActivationCall;
            while (call != null) {
                if (call.function == f)
                    return call;
                call = call.parentActivationCall;
            }
            return null;
        }

        public static IScriptable NewCatchScope (Exception t, IScriptable lastCatchScope, string exceptionName, Context cx, IScriptable scope)
        {
            object obj;
            bool cacheObj;

            if (t is EcmaScriptThrow) {
                cacheObj = false;
                obj = ((EcmaScriptThrow)t).Value;
            }
            else {
                cacheObj = true;

                // Create wrapper object unless it was associated with
                // the previous scope object

                if (lastCatchScope != null) {
                    BuiltinObject last = (BuiltinObject)lastCatchScope;
                    obj = last.GetAssociatedValue (t);
                    if (obj == null)
                        Context.CodeBug ();

                    goto getObj_brk;
                }

                EcmaScriptException re;
                string errorName;
                string errorMsg;

                Exception javaException = null;

                if (t is EcmaScriptError) {
                    EcmaScriptError ee = (EcmaScriptError)t;
                    re = ee;
                    errorName = ee.Name;
                    errorMsg = ee.ErrorMessage;
                }
                else if (t is EcmaScriptRuntimeException) {
                    re = (EcmaScriptRuntimeException)t;
                    if (t.InnerException != null) {
                        javaException = t.InnerException;
                        errorName = "JavaException";
                        errorMsg = javaException.GetType ().FullName + ": " + javaException.Message;
                    }
                    else {
                        errorName = "InternalError";
                        errorMsg = re.Message;
                    }
                }
                else {
                    // Script can catch only instances of JavaScriptException,
                    // EcmaError and EvaluatorException
                    throw Context.CodeBug ();
                }

                string sourceUri = re.SourceName;
                if (sourceUri == null) {
                    sourceUri = "";
                }
                int line = re.LineNumber;
                object [] args;
                if (line > 0) {
                    args = new object [] { errorMsg, sourceUri, (int)line };
                }
                else {
                    args = new object [] { errorMsg, sourceUri };
                }

                IScriptable errorObject = cx.NewObject (scope, errorName, args);
                ScriptableObject.PutProperty (errorObject, "name", errorName);

                if (javaException != null) {
                    object wrap = cx.Wrap (scope, javaException, null);
                    ScriptableObject.DefineProperty (errorObject, "javaException", wrap, ScriptableObject.PERMANENT | ScriptableObject.READONLY);
                }
                if (re != null) {
                    object wrap = cx.Wrap (scope, re, null);
                    ScriptableObject.DefineProperty (errorObject, "rhinoException", wrap, ScriptableObject.PERMANENT | ScriptableObject.READONLY);
                }

                obj = errorObject;
            }

        getObj_brk:
            ;



            BuiltinObject catchScopeObject = new BuiltinObject ();
            // See ECMA 12.4
            catchScopeObject.DefineProperty (exceptionName, obj, ScriptableObject.PERMANENT);
            if (cacheObj) {
                catchScopeObject.AssociateValue (t, obj);
            }
            return catchScopeObject;
        }

        public static IScriptable enterWith (object obj, Context cx, IScriptable scope)
        {
            IScriptable sobj = ScriptConvert.ToObjectOrNull (cx, obj);
            if (sobj == null) {
                throw TypeErrorById ("msg.undef.with", ScriptConvert.ToString (obj));
            }
            if (sobj is XMLObject) {
                XMLObject xmlObject = (XMLObject)sobj;
                return xmlObject.EnterWith (scope);
            }
            return new BuiltinWith (scope, sobj);
        }

        public static IScriptable leaveWith (IScriptable scope)
        {
            BuiltinWith nw = (BuiltinWith)scope;
            return nw.ParentScope;
        }

        public static IScriptable enterDotQuery (object value, IScriptable scope)
        {
            if (!(value is XMLObject)) {
                throw NotXmlError (value);
            }
            XMLObject obj = (XMLObject)value;
            return obj.EnterDotQuery (scope);
        }

        public static object updateDotQuery (bool value, IScriptable scope)
        {
            // Return null to continue looping
            BuiltinWith nw = (BuiltinWith)scope;
            return nw.UpdateDotQuery (value);
        }

        public static IScriptable leaveDotQuery (IScriptable scope)
        {
            BuiltinWith nw = (BuiltinWith)scope;
            return nw.ParentScope;
        }

        public static void setFunctionProtoAndParent (BaseFunction fn, IScriptable scope)
        {
            fn.ParentScope = scope;
            fn.SetPrototype (ScriptableObject.GetFunctionPrototype (scope));
        }

        public static void setObjectProtoAndParent (ScriptableObject obj, IScriptable scope)
        {
            // Compared with function it always sets the scope to top scope
            scope = ScriptableObject.GetTopLevelScope (scope);
            obj.ParentScope = scope;
            IScriptable proto = ScriptableObject.getClassPrototype (scope, obj.ClassName);
            obj.SetPrototype (proto);
        }

        public static void initFunction (Context cx, IScriptable scope, BuiltinFunction function, int type, bool fromEvalCode)
        {
            if (type == FunctionNode.FUNCTION_STATEMENT) {
                string name = function.FunctionName;
                if (name != null && name.Length != 0) {
                    if (!fromEvalCode) {
                        // ECMA specifies that functions defined in global and
                        // function scope outside eval should have DONTDELETE set.
                        ScriptableObject.DefineProperty (scope, name, function, ScriptableObject.PERMANENT);
                    }
                    else {
                        scope.Put (name, scope, function);
                    }
                }
            }
            else if (type == FunctionNode.FUNCTION_EXPRESSION_STATEMENT) {
                string name = function.FunctionName;
                if (name != null && name.Length != 0) {
                    // Always put function expression statements into initial
                    // activation object ignoring the with statement to follow
                    // SpiderMonkey
                    while (scope is BuiltinWith) {
                        scope = scope.ParentScope;
                    }
                    scope.Put (name, scope, function);
                }
            }
            else {
                throw Context.CodeBug ();
            }
        }

        public static IScriptable newArrayLiteral (object [] objects, int [] skipIndexces, Context cx, IScriptable scope)
        {
            int count = objects.Length;
            int skipCount = 0;
            if (skipIndexces != null) {
                skipCount = skipIndexces.Length;
            }
            int length = count + skipCount;
            int lengthObj = (int)length;
            IScriptable arrayObj;
            /*
            * If the version is 120, then new Array(4) means create a new
            * array with 4 as the first element.  In this case, we have to
            * set length property manually.
            */
            if (cx.Version == Context.Versions.JS1_2) {
                arrayObj = cx.NewObject (scope, "Array", ScriptRuntime.EmptyArgs);
                ScriptableObject.PutProperty (arrayObj, "length", (object)lengthObj);
            }
            else {
                arrayObj = cx.NewObject (scope, "Array", new object [] { lengthObj });
            }
            int skip = 0;
            for (int i = 0, j = 0; i != length; ++i) {
                if (skip != skipCount && skipIndexces [skip] == i) {
                    ++skip;
                    continue;
                }
                ScriptableObject.PutProperty (arrayObj, i, objects [j]);
                ++j;
            }
            return arrayObj;
        }

        public static IScriptable newObjectLiteral (object [] propertyIds, object [] propertyValues, Context cx, IScriptable scope)
        {
            IScriptable obj = cx.NewObject (scope);
            for (int i = 0, end = propertyIds.Length; i != end; ++i) {
                object id = propertyIds [i];
                object value = propertyValues [i];

                if (id is Node.GetterPropertyLiteral) {
                    BuiltinObject nativeObj = (BuiltinObject)obj;
                    InterpretedFunction fun = (InterpretedFunction)value;
                    nativeObj.DefineGetter ((string)((Node.GetterPropertyLiteral)id).Property, fun);
                }
                else if (id is Node.SetterPropertyLiteral) {
                    BuiltinObject nativeObj = (BuiltinObject)obj;
                    InterpretedFunction fun = (InterpretedFunction)value;
                    nativeObj.DefineSetter ((string)((Node.SetterPropertyLiteral)id).Property, fun);
                }
                else if (id is string) {
                    ScriptableObject.PutProperty (obj, (string)id, value);
                }
                else {
                    ScriptableObject.PutProperty (obj, (int)id, value);
                }
            }
            return obj;
        }

        public static bool isArrayObject (object obj)
        {
            return obj is BuiltinArray || obj is Arguments;
        }

        public static object [] getArrayElements (IScriptable obj)
        {
            Context cx = Context.CurrentContext;
            long longLen = BuiltinArray.getLengthProperty (cx, obj);
            if (longLen > int.MaxValue) {
                // arrays beyond  MAX_INT is not in Java in any case
                throw new ArgumentException ();
            }
            int len = (int)longLen;
            if (len == 0) {
                return ScriptRuntime.EmptyArgs;
            }
            else {
                object [] result = new object [len];
                for (int i = 0; i < len; i++) {
                    object elem = ScriptableObject.GetProperty (obj, i);
                    result [i] = (elem == UniqueTag.NotFound) ? Undefined.Value : elem;
                }
                return result;
            }
        }

        internal static void checkDeprecated (Context cx, string name)
        {
            Context.Versions version = cx.Version;
            if (version >= Context.Versions.JS1_4 || version == Context.Versions.Default) {
                string msg = GetMessage ("msg.deprec.ctor", name);
                if (version == Context.Versions.Default)
                    Context.ReportWarning (msg);
                else
                    throw Context.ReportRuntimeError (msg);
            }
        }


        private static ResourceManager m_ResourceManager = null;

        public static string GetMessage (string messageId, params object [] arguments)
        {
            Context cx = Context.CurrentContext;

            // Get current culture
            CultureInfo culture = null;
            if (cx != null)
                culture = cx.CurrentCulture;

            if (m_ResourceManager == null) {
                m_ResourceManager = new ResourceManager (
                    "EcmaScript.NET.Resources.Messages", typeof (ScriptRuntime).Assembly);
            }

            string formatString = m_ResourceManager.GetString (messageId, culture);
            if (formatString == null)
                throw new ApplicationException ("Missing no message resource found for message property " + messageId);

            if (arguments == null)
                arguments = new object [0];
            if (arguments.Length == 0)
                return formatString;
            return string.Format (formatString, arguments);
        }

        public static EcmaScriptError ConstructError (string error, string message)
        {
            int [] linep = new int [1];
            string filename = Context.GetSourcePositionFromStack (linep);
            return ConstructError (error, message, filename, linep [0], null, 0);
        }

        public static EcmaScriptError ConstructError (string error, string message, string sourceName, int lineNumber, string lineSource, int columnNumber)
        {
            return new EcmaScriptError (error, message, sourceName, lineNumber, lineSource, columnNumber);
        }

        public static EcmaScriptError TypeError (string message)
        {
            return ConstructError ("TypeError", message);
        }

        public static EcmaScriptError TypeErrorById (string messageId, params string [] args)
        {
            return TypeError (GetMessage (messageId, args));
        }

        public static ApplicationException UndefReadError (object obj, object id)
        {
            string idStr = (id == null) ? "null" : id.ToString ();
            return TypeErrorById ("msg.undef.prop.read", ScriptConvert.ToString (obj), idStr);
        }

        public static ApplicationException UndefCallError (object obj, object id)
        {
            string idStr = (id == null) ? "null" : id.ToString ();
            return TypeErrorById ("msg.undef.method.call", ScriptConvert.ToString (obj), idStr);
        }

        public static ApplicationException UndefWriteError (object obj, object id, object value)
        {
            string idStr = (id == null) ? "null" : id.ToString ();
            string valueStr = (value is IScriptable) ? value.ToString () : ScriptConvert.ToString (value);
            return TypeErrorById ("msg.undef.prop.write", ScriptConvert.ToString (obj), idStr, valueStr);
        }

        public static ApplicationException NotFoundError (IScriptable obj, string property)
        {
            // TODO: use object to improve the error message
            string msg = GetMessage ("msg.is.not.defined", property);
            throw ConstructError ("ReferenceError", msg);
        }

        public static ApplicationException NotFunctionError (object value)
        {
            return NotFunctionError (value, value);
        }

        public static ApplicationException NotFunctionError (object value, object messageHelper)
        {
            // TODO: Use value for better error reporting			
            string msg = (messageHelper == null) ? "null" : messageHelper.ToString ();
            if (value == UniqueTag.NotFound) {
                return TypeErrorById ("msg.function.not.found", msg);
            }
            return TypeErrorById ("msg.isnt.function", msg, value == null ? "null" : value.GetType ().FullName);
        }

        private static ApplicationException NotXmlError (object value)
        {
            throw TypeErrorById ("msg.isnt.xml.object", ScriptConvert.ToString (value));
        }

        internal static void WarnAboutNonJSObject (object nonJSObject)
        {
            string message = "+++ USAGE WARNING: Missed Context.Wrap() conversion:\n"
                + "Runtime detected object " + nonJSObject + " of class " + nonJSObject.GetType ().FullName + " where it expected String, Number, Boolean or Scriptable instance. "
                + "Please check your code for missig Context.Wrap() call.";

            Context.ReportWarning (message);
            Console.Error.WriteLine (message);
        }


        private static XMLLib CurrentXMLLib (Context cx)
        {
            // Scripts should be running to access this
            if (cx.topCallScope == null)
                throw new ApplicationException ();

            XMLLib xmlLib = cx.cachedXMLLib;
            if (xmlLib == null) {
                xmlLib = XMLLib.ExtractFromScope (cx.topCallScope);
                if (xmlLib == null)
                    throw new ApplicationException ();
                cx.cachedXMLLib = xmlLib;
            }

            return xmlLib;
        }

        /// <summary> Escapes the reserved characters in a value of an attribute
        /// 
        /// </summary>
        /// <param name="value">Unescaped text
        /// </param>
        /// <returns> The escaped text
        /// </returns>
        public static string escapeAttributeValue (object value, Context cx)
        {
            XMLLib xmlLib = CurrentXMLLib (cx);
            return xmlLib.EscapeAttributeValue (value);
        }

        /// <summary> Escapes the reserved characters in a value of a text node
        /// 
        /// </summary>
        /// <param name="value">Unescaped text
        /// </param>
        /// <returns> The escaped text
        /// </returns>
        public static string escapeTextValue (object value, Context cx)
        {
            XMLLib xmlLib = CurrentXMLLib (cx);
            return xmlLib.EscapeTextValue (value);
        }

        public static IRef memberRef (object obj, object elem, Context cx, int memberTypeFlags)
        {
            if (!(obj is XMLObject)) {
                throw NotXmlError (obj);
            }
            XMLObject xmlObject = (XMLObject)obj;
            return xmlObject.MemberRef (cx, elem, memberTypeFlags);
        }

        public static IRef memberRef (object obj, object ns, object elem, Context cx, int memberTypeFlags)
        {
            if (!(obj is XMLObject)) {
                throw NotXmlError (obj);
            }
            XMLObject xmlObject = (XMLObject)obj;
            return xmlObject.MemberRef (cx, ns, elem, memberTypeFlags);
        }

        public static IRef nameRef (object name, Context cx, IScriptable scope, int memberTypeFlags)
        {
            XMLLib xmlLib = CurrentXMLLib (cx);
            return xmlLib.NameRef (cx, name, scope, memberTypeFlags);
        }

        public static IRef nameRef (object ns, object name, Context cx, IScriptable scope, int memberTypeFlags)
        {
            XMLLib xmlLib = CurrentXMLLib (cx);
            return xmlLib.NameRef (cx, ns, name, scope, memberTypeFlags);
        }

        private static void storeIndexResult (Context cx, int index)
        {
            cx.scratchIndex = index;
        }

        internal static int lastIndexResult (Context cx)
        {
            return cx.scratchIndex;
        }

        public static void storeUint32Result (Context cx, long value)
        {
            if (((ulong)value >> 32) != 0)
                throw new ArgumentException ();
            cx.scratchUint32 = value;
        }

        public static long lastUint32Result (Context cx)
        {
            long value = cx.scratchUint32;
            if ((ulong)value >> 32 != 0)
                throw new ApplicationException ();
            return value;
        }

        private static void storeScriptable (Context cx, IScriptable value)
        {
            // The previosly stored scratchScriptable should be consumed
            if (cx.scratchScriptable != null)
                throw new ApplicationException ();
            cx.scratchScriptable = value;
        }

        public static IScriptable lastStoredScriptable (Context cx)
        {
            IScriptable result = cx.scratchScriptable;
            cx.scratchScriptable = null;
            return result;
        }

        internal static string makeUrlForGeneratedScript (bool isEval, string masterScriptUrl, int masterScriptLine)
        {
            if (isEval) {
                return masterScriptUrl + '#' + masterScriptLine + "(eval)";
            }
            else {
                return masterScriptUrl + '#' + masterScriptLine + "(Function)";
            }
        }

        internal static bool isGeneratedScript (string sourceUrl)
        {
            // ALERT: this may clash with a valid URL containing (eval) or
            // (Function)
            return sourceUrl.IndexOf ("(eval)") >= 0 || sourceUrl.IndexOf ("(Function)") >= 0;
        }


        public static readonly object [] EmptyArgs = new object [0];
        public static readonly string [] EmptyStrings = new string [0];
         
    }
}