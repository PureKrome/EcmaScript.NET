//------------------------------------------------------------------------------
// <license file="NativeCliObject.cs">
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
using System.Reflection;
using System.Collections;

using EcmaScript.NET.Types;
using EcmaScript.NET.Attributes;

namespace EcmaScript.NET.Types.Cli
{

    public class CliObject : ScriptableObject, Wrapper, IIdEnumerable
    {

        private object m_Object = null;

        public object Object
        {
            get { return m_Object; }
        }

        public object Unwrap ()
        {
            return m_Object;
        }

        private CliType m_Type = null;



        protected CliObject ()
        {
            Init (this, this.GetType ());
        }

        public CliObject (object obj)
        {
            Init (obj, obj.GetType ());
        }

        public CliObject (object obj, Type type)
        {
            Init (obj, type);
        }

        protected void Init (object obj, Type type)
        {
            m_Object = obj;
            m_Type = CliType.GetNativeCliType (type);
        }

        public override string ClassName
        {
            get
            {
                return "NativeCliObject";
            }
        }

        public override object GetDefaultValue (Type hint)
        {
            object value;
            if (hint == null || hint == typeof (String)) {
                value = m_Object.ToString ();
            }
            else {
                string converterName;
                if (hint == typeof (bool)) {
                    converterName = "booleanValue";
                }
                else if (CliHelper.IsNumberType (hint)) {
                    converterName = "doubleValue";
                }
                else {
                    throw Context.ReportRuntimeErrorById ("msg.default.value");
                }
                object converterObject = Get (converterName, this);
                if (converterObject is IFunction) {
                    IFunction f = (IFunction)converterObject;
                    value = f.Call (Context.CurrentContext, f.ParentScope, this, ScriptRuntime.EmptyArgs);
                }
                else {
                    if (CliHelper.IsNumberType (hint) && m_Object is bool) {
                        bool b = (bool)m_Object;
                        value = b ? 1.0 : 0.0;
                    }
                    else {
                        value = m_Object.ToString ();
                    }
                }
            }
            return value;
        }

        public override object Put (int index, IScriptable start, object value)
        {
            if (value is IdFunctionObject) {
                return base.Put (index, start, value);
            }


            // 1. Index Setter
            foreach (MethodInfo mi in m_Type.IndexSetter) {
                ParameterInfo [] pis = mi.GetParameters ();
                if (pis.Length == 2 && CliHelper.IsNumberType (pis [0].ParameterType)) {
                    mi.Invoke (m_Object, new object [] { index, value });
                    return value;
                }
            }

            // 2. Base
            return base.Put (index, start, value);
        }

        public override object [] GetIds ()
        {
            if (m_Object is ICollection) {
                ICollection col = (ICollection)m_Object;

                object [] result = new object [col.Count];
                int i = col.Count;
                while (--i >= 0)
                    result [i] = (int)i;
                return result;
            }

            return base.GetIds ();
        }

        public override bool Has (int index, IScriptable start)
        {
            if (m_Object is ICollection) {
                ICollection col = (ICollection)m_Object;

                return (index >= 0 && index < col.Count);
            }
            return base.Has (index, start);
        }



        public override object Put (string name, IScriptable start, object value)
        {
            if (value is IdFunctionObject) {
                return base.Put (name, start, value);
            }

            // 1. Search froperty
            PropertyInfo pi = m_Type.GetCachedProperty (name);
            if (pi != null) {
                if (!pi.CanWrite)
                    throw Context.ReportRuntimeErrorById ("msg.undef.prop.write", name, ClassName, value);
                pi.SetValue (m_Object, Convert.ChangeType (value, pi.PropertyType), null);
                return value;
            }

            // 2. Search field
            FieldInfo fi = m_Type.GetCachedField (name);
            if (fi != null) {
                if (!fi.IsPublic)
                    throw Context.ReportRuntimeErrorById ("msg.undef.prop.write", name, ClassName);
                fi.SetValue (m_Object, value);
                return value;
            }

            // 3. Indexer
            foreach (MethodInfo mi in m_Type.IndexSetter) {
                ParameterInfo [] pis = mi.GetParameters ();
                if (pis.Length == 2 && pis [0].ParameterType == typeof (string)) {
                    mi.Invoke (m_Object, new object [] { name, value });
                    return value;
                }
            }


            return base.Put (name, start, value);
        }



        public override object Get (int index, IScriptable start)
        {
            foreach (MethodInfo mi in m_Type.IndexGetter) {
                ParameterInfo [] pis = mi.GetParameters ();
                if (pis.Length == 1 && CliHelper.IsNumberType (pis [0].ParameterType)) {
                    return mi.Invoke (m_Object, new object [] { index });
                }
            }

            object result = base.Get (index, start);
            if (result != UniqueTag.NotFound)
                return result;

            return UniqueTag.NotFound;
        }



        public override object Get (string name, IScriptable start)
        {
            object result = base.Get (name, start);
            if (result != UniqueTag.NotFound || name == "Object")
                return result;

            if (m_Type.ClassAttribute != null) {
                CliMethodInfo mi = m_Type.GetFunctionsWithAttribute (name);
                if (mi != null)
                    return mi;
                return UniqueTag.NotFound;
            }

            // Compatiblity with Microsoft.JScript
            if (typeof (IReflect).IsAssignableFrom (m_Type.UnderlyingType)) {
                MemberInfo [] mis = ((IReflect)m_Object).GetMember (name, BindingFlags.Default);
                if (mis.Length > 0) {
                    if (mis [0] is PropertyInfo) {
                        return ((PropertyInfo)mis [0]).GetValue (m_Object, null);
                    }
                    else if (mis [0] is FieldInfo) {
                        return ((FieldInfo)mis [0]).GetValue (m_Object);
                    }
                    else {
                        return new CliMethodInfo (name, mis, null);
                    }
                }

                return UniqueTag.NotFound;
            }

            // 1. Search froperty
            PropertyInfo pi = m_Type.GetCachedProperty (name);
            if (pi != null) {
                if (!pi.CanRead)
                    throw Context.ReportRuntimeErrorById ("msg.undef.prop.read", name, ClassName);
                return pi.GetValue (m_Object, null);
            }

            // 2. Search field
            FieldInfo fi = m_Type.GetCachedField (name);
            if (fi != null) {
                if (!fi.IsPublic)
                    throw Context.ReportRuntimeErrorById ("msg.undef.prop.read", name, ClassName);
                return fi.GetValue (m_Object);
            }

            // 3. Search function
            CliMethodInfo nmi = m_Type.GetFunctions (name);
            if (nmi != null)
                return nmi;

            // 4. Indexer
            foreach (MethodInfo mi in m_Type.IndexGetter) {
                ParameterInfo [] pis = mi.GetParameters ();
                if (pis.Length == 1 && pis [0].ParameterType == typeof (string)) {
                    return mi.Invoke (m_Object, new object [] { name });
                }
            }

            // 4. Event
            EventInfo ei = m_Type.GetCachedEvent (name);
            if (ei != null) {
                CliEventInfo ncei = new CliEventInfo (ei);
                ncei.ParentScope = this;
                return ncei;
            }

            return UniqueTag.NotFound;
        }

        private const int JSTYPE_UNDEFINED = 0; // undefined type
        private const int JSTYPE_NULL = 1; // null
        private const int JSTYPE_BOOLEAN = 2; // boolean
        private const int JSTYPE_NUMBER = 3; // number
        private const int JSTYPE_STRING = 4; // string
        private const int JSTYPE_CLI_CLASS = 5; // CLI Class
        private const int JSTYPE_CLI_OBJECT = 6; // CLI Object
        private const int JSTYPE_CLI_ARRAY = 7; // CLI Array
        private const int JSTYPE_OBJECT = 8; // Scriptable

        internal const sbyte CONVERSION_TRIVIAL = 1;
        internal const sbyte CONVERSION_NONTRIVIAL = 0;
        internal const sbyte CONVERSION_NONE = 99;

        public static bool CanConvert (object fromObj, Type to)
        {
            return (GetConversionWeight (fromObj, to) < CONVERSION_NONE);
        }

        /// <summary> Derive a ranking based on how "natural" the conversion is.
        /// The special value CONVERSION_NONE means no conversion is possible,
        /// and CONVERSION_NONTRIVIAL signals that more type conformance testing
        /// is required.
        /// Based on
        /// <a href="http://www.mozilla.org/js/liveconnect/lc3_method_overloading.html">
        /// "preferred method conversions" from Live Connect 3</a>
        /// </summary>
        internal static int GetConversionWeight (System.Object fromObj, System.Type to)
        {
            int fromCode = GetJSTypeCode (fromObj);

            switch (fromCode) {
                case JSTYPE_UNDEFINED:
                    if (to == typeof (string) || to == typeof (object)) {
                        return 1;
                    }
                    break;
                case JSTYPE_NULL:
                    if (!to.IsPrimitive) {
                        return 1;
                    }
                    break;
                case JSTYPE_BOOLEAN:
                    if (to == typeof (bool)) {
                        return 1;
                    }
                    else if (to == typeof (object)) {
                        return 2;
                    }
                    else if (to == typeof (string)) {
                        return 3;
                    }
                    break;


                case JSTYPE_NUMBER:
                    if (to.IsPrimitive) {
                        if (to == typeof (double)) {
                            return 1;
                        }
                        else if (to != typeof (bool)) {
                            return 1 + GetSizeRank (to);
                        }
                    }
                    else {
                        if (to == typeof (string)) {
                            // native numbers are #1-8
                            return 9;
                        }
                        else if (to == typeof (object)) {
                            return 10;
                        }
                        else if (CliHelper.IsNumberType (to)) {
                            // "double" is #1
                            return 2;
                        }
                    }
                    break;


                case JSTYPE_STRING:
                    if (to == typeof (string)) {
                        return 1;
                    }
                    else if (to.IsInstanceOfType (fromObj)) {
                        return 2;
                    }
                    else if (to.IsPrimitive) {
                        if (to == typeof (char)) {
                            return 3;
                        }
                        else if (to != typeof (bool)) {
                            return 4;
                        }
                    }
                    break;


                case JSTYPE_CLI_CLASS:
                    if (to == typeof (Type)) {
                        return 1;
                    }
                    else if (to == typeof (object)) {
                        return 3;
                    }
                    else if (to == typeof (string)) {
                        return 4;
                    }
                    break;


                case JSTYPE_CLI_OBJECT:
                case JSTYPE_CLI_ARRAY:
                    object cliObj = fromObj;
                    if (cliObj is Wrapper) {
                        cliObj = ((Wrapper)cliObj).Unwrap ();
                    }
                    if (to.IsInstanceOfType (cliObj)) {
                        return CONVERSION_NONTRIVIAL;
                    }
                    if (to == typeof (string)) {
                        return 2;
                    }
                    else if (to.IsPrimitive && to != typeof (bool)) {
                        return (fromCode == JSTYPE_CLI_ARRAY) ? CONVERSION_NONTRIVIAL : 2 + GetSizeRank (to);
                    }
                    break;


                case JSTYPE_OBJECT:
                    // Other objects takes #1-#3 spots
                    if (to == fromObj.GetType ()) {
                        // No conversion required
                        return 1;
                    }
                    if (to.IsArray) {
                        if (fromObj is BuiltinArray) {
                            // This is a native array conversion to a java array
                            // Array conversions are all equal, and preferable to object
                            // and string conversion, per LC3.
                            return 1;
                        }
                    }
                    else if (to == typeof (object)) {
                        return 2;
                    }
                    else if (to == typeof (string)) {
                        return 3;
                    }
                    else if (to == typeof (DateTime)) {
                        if (fromObj is BuiltinDate) {
                            // This is a native date to java date conversion
                            return 1;
                        }
                    }
                    else if (to.IsInterface) {
                        if (fromObj is IFunction) {
                            // See comments in coerceType
                            if (to.GetMethods ().Length == 1) {
                                return 1;
                            }
                        }
                        return 11;
                    }
                    else if (to.IsPrimitive && to != typeof (bool)) {
                        return 3 + GetSizeRank (to);
                    }
                    break;
            }

            return CONVERSION_NONE;
        }

        private static int GetSizeRank (System.Type aType)
        {
            if (aType == typeof (double)) {
                return 1;
            }
            else if (aType == typeof (Single)) {
                return 2;
            }
            else if (aType == typeof (long)) {
                return 3;
            }
            else if (aType == typeof (int)) {
                return 4;
            }
            else if (aType == typeof (short)) {
                return 5;
            }
            else if (aType == typeof (char)) {
                return 6;
            }
            else if (aType == typeof (sbyte)) {
                return 7;
            }
            else if (aType == typeof (bool)) {
                return CONVERSION_NONE;
            }
            else {
                return 8;
            }
        }

        private static int GetJSTypeCode (System.Object value)
        {
            if (value == null) {
                return JSTYPE_NULL;
            }
            else if (value == Undefined.Value) {
                return JSTYPE_UNDEFINED;
            }
            else if (value is string) {
                return JSTYPE_STRING;
            }
            else if (CliHelper.IsNumber (value)) {
                return JSTYPE_NUMBER;
            }
            else if (value is bool) {
                return JSTYPE_BOOLEAN;
            }
            else if (value is IScriptable) {
                if (value is CliType) {
                    return JSTYPE_CLI_CLASS;
                }
                else if (value is CliArray) {
                    return JSTYPE_CLI_ARRAY;
                }
                else if (value is Wrapper) {
                    return JSTYPE_CLI_OBJECT;
                }
                else {
                    return JSTYPE_OBJECT;
                }
            }
            else if (value is Type) {
                return JSTYPE_CLI_CLASS;
            }
            else {
                if (value.GetType ().IsArray) {
                    return JSTYPE_CLI_ARRAY;
                }
                else {
                    return JSTYPE_CLI_OBJECT;
                }
            }
        }

        /// <summary> Type-munging for field setting and method invocation.
        /// Conforms to LC3 specification
        /// </summary>
        internal static System.Object CoerceType (System.Type type, System.Object value)
        {
            if (value != null && value.GetType () == type) {
                return value;
            }

            switch (GetJSTypeCode (value)) {


                case JSTYPE_NULL:
                    // raise error if type.isPrimitive()
                    if (type.IsPrimitive) {
                        reportConversionError (value, type);
                    }
                    return null;


                case JSTYPE_UNDEFINED:
                    if (type == typeof (string) || type == typeof (object)) {
                        return "undefined";
                    }
                    else {
                        reportConversionError ("undefined", type);
                    }
                    break;


                case JSTYPE_BOOLEAN:
                    // Under LC3, only JS Booleans can be coerced into a Boolean value
                    if (type == typeof (bool) || type == typeof (bool) || type == typeof (object)) {
                        return value;
                    }
                    else if (type == typeof (string)) {
                        return value.ToString ();
                    }
                    else {
                        reportConversionError (value, type);
                    }
                    break;


                case JSTYPE_NUMBER:
                    if (type == typeof (string)) {
                        return ScriptConvert.ToString (value);
                    }
                    else if (type == typeof (object)) {
                        return CoerceToNumber (typeof (double), value);
                    }
                    else if ((type.IsPrimitive && type != typeof (bool)) || CliHelper.IsNumberType (type)) {
                        return CoerceToNumber (type, value);
                    }
                    else {
                        reportConversionError (value, type);
                    }
                    break;


                case JSTYPE_STRING:
                    if (type == typeof (string) || type.IsInstanceOfType (value)) {
                        return value;
                    }
                    else if (type == typeof (char)) {
                        // Special case for converting a single char string to a
                        // character
                        // Placed here because it applies *only* to JS strings,
                        // not other JS objects converted to strings
                        if (((System.String)value).Length == 1) {
                            return ((System.String)value) [0];
                        }
                        else {
                            return CoerceToNumber (type, value);
                        }
                    }
                    else if ((type.IsPrimitive && type != typeof (bool)) || CliHelper.IsNumberType (type)) {
                        return CoerceToNumber (type, value);
                    }
                    else {
                        reportConversionError (value, type);
                    }
                    break;


                case JSTYPE_CLI_CLASS:
                    if (value is Wrapper) {
                        value = ((Wrapper)value).Unwrap ();
                    }

                    if (type == typeof (Type) || type == typeof (object)) {
                        return value;
                    }
                    else if (type == typeof (string)) {
                        return value.ToString ();
                    }
                    else {
                        reportConversionError (value, type);
                    }
                    break;


                case JSTYPE_CLI_OBJECT:
                case JSTYPE_CLI_ARRAY:
                    if (type.IsPrimitive) {
                        if (type == typeof (bool)) {
                            reportConversionError (value, type);
                        }
                        return CoerceToNumber (type, value);
                    }
                    else {
                        if (value is Wrapper) {
                            value = ((Wrapper)value).Unwrap ();
                        }
                        if (type == typeof (string)) {
                            return value.ToString ();
                        }
                        else {
                            if (type.IsInstanceOfType (value)) {
                                return value;
                            }
                            else {
                                reportConversionError (value, type);
                            }
                        }
                    }
                    break;


                case JSTYPE_OBJECT:
                    if (type == typeof (string)) {
                        return ScriptConvert.ToString (value);
                    }
                    else if (type.IsPrimitive) {
                        if (type == typeof (bool)) {
                            reportConversionError (value, type);
                        }
                        return CoerceToNumber (type, value);
                    }
                    else if (type.IsInstanceOfType (value)) {
                        return value;
                    }
                    else if (type == typeof (DateTime) && value is BuiltinDate) {
                        double time = ((BuiltinDate)value).JSTimeValue;
                        // TODO: This will replace NaN by 0						
                        return BuiltinDate.FromMilliseconds ((long)time);
                    }
                    else if (type.IsArray && value is BuiltinArray) {
                        // Make a new java array, and coerce the JS array components
                        // to the target (component) type.
                        BuiltinArray array = (BuiltinArray)value;
                        long length = array.getLength ();
                        System.Type arrayType = type.GetElementType ();
                        System.Object Result = System.Array.CreateInstance (arrayType, (int)length);
                        for (int i = 0; i < length; ++i) {
                            try {
                                ((System.Array)Result).SetValue (CoerceType (arrayType, array.Get (i, array)), i);
                            }
                            catch (EcmaScriptException) {
                                reportConversionError (value, type);
                            }
                        }

                        return Result;
                    }
                    else if (value is Wrapper) {
                        value = ((Wrapper)value).Unwrap ();
                        if (type.IsInstanceOfType (value))
                            return value;
                        reportConversionError (value, type);
                    }
                    else {
                        reportConversionError (value, type);
                    }
                    break;
            }

            return value;
        }

        private static System.Object CoerceToNumber (System.Type type, System.Object value)
        {
            System.Type valueClass = value.GetType ();

            // Character
            if (type == typeof (char)) {
                if (valueClass == typeof (char)) {
                    return value;
                }
                return (char)toInteger (value, typeof (char), (double)System.Char.MinValue, (double)System.Char.MaxValue);
            }

            // Double, Float
            if (type == typeof (object) || type == typeof (double)) {
                return valueClass == typeof (double) ? value : (double)toDouble (value);
            }

            if (type == typeof (float) || type == typeof (Single)) {
                if (valueClass == typeof (float)) {
                    return value;
                }
                else {
                    double number = toDouble (value);
                    if (System.Double.IsInfinity (number) || System.Double.IsNaN (number) || number == 0.0) {
                        return (float)number;
                    }
                    else {
                        double absNumber = Math.Abs (number);
                        if (absNumber < (double)System.Single.Epsilon) {
                            return (float)((number > 0.0) ? +0.0 : -0.0);
                        }
                        else {
                            if (absNumber > (double)System.Single.MaxValue) {
                                return (float)((number > 0.0) ? System.Single.PositiveInfinity : System.Single.NegativeInfinity);
                            }
                            else {
                                return (float)number;
                            }
                        }
                    }
                }
            }

            // Integer, Long, Short, Byte
            if (type == typeof (int)) {
                if (valueClass == typeof (int)) {
                    return value;
                }
                else {
                    return (System.Int32)toInteger (value, typeof (int), (double)System.Int32.MinValue, (double)System.Int32.MaxValue);
                }
            }

            if (type == typeof (long)) {
                if (valueClass == typeof (long)) {
                    return value;
                }
                else {
                    return (long)toInteger (value, typeof (long), long.MinValue, long.MaxValue);
                }
            }

            if (type == typeof (short)) {
                if (valueClass == typeof (short)) {
                    return value;
                }
                else {
                    return (short)toInteger (value, typeof (short), (double)System.Int16.MinValue, (double)System.Int16.MaxValue);
                }
            }

            if (type == typeof (byte) || type == typeof (sbyte)) {
                if (valueClass == typeof (byte)) {
                    return value;
                }
                else {
                    return (sbyte)toInteger (value, typeof (byte), (double)System.SByte.MinValue, (double)System.SByte.MaxValue);
                }
            }

            return (double)toDouble (value);
        }

        private static double toDouble (System.Object value)
        {
            if (value is System.ValueType) {
                return Convert.ToDouble (value);
            }
            else if (value is System.String) {
                return ScriptConvert.ToNumber ((System.String)value);
            }
            else if (value is IScriptable) {
                if (value is Wrapper) {
                    // TODO: optimize tail-recursion?
                    return toDouble (((Wrapper)value).Unwrap ());
                }
                else {
                    return ScriptConvert.ToNumber (value);
                }
            }
            else {
                System.Reflection.MethodInfo meth;
                try {
                    meth = value.GetType ().GetMethod ("doubleValue", new System.Type [0]);
                }
                catch (System.MethodAccessException) {
                    meth = null;
                }
                catch (System.Security.SecurityException) {
                    meth = null;
                }
                if (meth != null) {
                    try {
                        return Convert.ToDouble (meth.Invoke (value, (System.Object [])null));
                    }
                    catch (System.UnauthorizedAccessException) {
                        // TODO: ignore, or error message?
                        reportConversionError (value, typeof (double));
                    }
                    catch (System.Reflection.TargetInvocationException) {
                        // TODO: ignore, or error message?
                        reportConversionError (value, typeof (double));
                    }
                }
                return ScriptConvert.ToNumber (value.ToString ());
            }
        }

        private static long toInteger (System.Object value, System.Type type, double min, double max)
        {
            double d = toDouble (value);

            if (System.Double.IsInfinity (d) || System.Double.IsNaN (d)) {
                // Convert to string first, for more readable message
                reportConversionError (ScriptConvert.ToString (value), type);
            }

            if (d > 0.0) {
                d = Math.Floor (d);
            }
            else {
                d = Math.Ceiling (d);
            }

            if (d < min || d > max) {
                // Convert to string first, for more readable message
                reportConversionError (ScriptConvert.ToString (value), type);
            }

            return (long)d;
        }

        internal static void reportConversionError (System.Object value, System.Type type)
        {
            // It uses String.valueOf(value), not value.toString() since
            // value can be null, bug 282447.
            throw Context.ReportRuntimeErrorById ("msg.conversion.not.allowed", Convert.ToString (value), CliHelper.ToSignature (type));
        }

        public override string ToString ()
        {
            return "[object " + ClassName + "]";
        }

        public IdEnumeration GetEnumeration (Context cx, bool enumValues)
        {
            if (m_Object is IEnumerable)
                return new EnumeratorBasedIdEnumeration (((IEnumerable)m_Object).GetEnumerator ());
            return new IdEnumeration (this, cx, enumValues);
        }

        private class EnumeratorBasedIdEnumeration : IdEnumeration
        {

            private IEnumerator enumerator = null;

            public EnumeratorBasedIdEnumeration (IEnumerator enumerator)
            {
                this.enumerator = enumerator;
            }

            public override object Current (Context cx)
            {
                return Context.CliToJS (cx, enumerator.Current, cx.topCallScope);
            }

            public override bool MoveNext ()
            {
                return enumerator.MoveNext ();
            }


        }


    }


}
