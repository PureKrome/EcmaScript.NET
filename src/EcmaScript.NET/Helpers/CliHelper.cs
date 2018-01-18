//------------------------------------------------------------------------------
// <license file="EcmaScriptHelper.cs">
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
using System.Text;
using System.Reflection;
using System.Collections;
using System.Runtime.InteropServices;

namespace EcmaScript.NET
{

    sealed class CliHelper
    {

        private CliHelper () 
        {
            ;
        }
        
        internal static Type GetType (string className)
        {
            try {
                return Type.GetType (className);
            }
            catch {
                ;
            }
            return null;
        }

        internal static bool IsNegativeZero (double d)
        {
            if (double.IsNaN (d))
                return false;
            if (d != 0.0)
                return false;
            return (double.PositiveInfinity / d) == double.NegativeInfinity;
        }

        internal static bool IsPositiveZero (double d)
        {
            if (double.IsNaN (d))
                return false;
            if (d != 0.0)
                return false;
            return (double.PositiveInfinity / d) == double.PositiveInfinity;
        }

        internal static new bool Equals (object o1, object o2)
        {
            if (o1 == null && o2 == null)
                return true;
            if (o1 == null || o2 == null)
                return false;
            return o1.Equals (o2);
        }

        internal static string ToSignature (ConstructorInfo ci)
        {
            StringBuilder sb = new StringBuilder ();
            sb.Append (ToSignature (ci.DeclaringType));
            sb.Append (ToSignature ('(', ci.GetParameters (), ')'));
            return sb.ToString ();
        }
        internal static string ToSignature (PropertyInfo pi)
        {
            StringBuilder sb = new StringBuilder ();
            sb.Append (ToSignature (pi.PropertyType));
            sb.Append (" ");
            sb.Append (ToSignature (pi.DeclaringType));
            sb.Append (".");
            sb.Append (pi.Name);
            sb.Append (ToSignature ('[', pi.GetIndexParameters (), ']'));
            return sb.ToString ();
        }

        internal static string ToSignature (FieldInfo fi)
        {
            StringBuilder sb = new StringBuilder ();
            sb.Append (ToSignature (fi.FieldType));
            sb.Append (" ");
            sb.Append (ToSignature (fi.DeclaringType));
            sb.Append (".");
            sb.Append (fi.Name);
            return sb.ToString ();
        }

        internal static string ToSignature (object [] args)
        {
            StringBuilder sb = new StringBuilder ();
            for (int i = 0; i < args.Length; i++) {
                if (i > 0)
                    sb.Append (", ");
                sb.Append (ToSignature (args [0].GetType ()));
            }
            return sb.ToString ();
        }

        internal static string ToSignature (MemberInfo mi)
        {
            if (mi is PropertyInfo)
                return ToSignature ((PropertyInfo)mi);
            if (mi is FieldInfo)
                return ToSignature ((FieldInfo)mi);
            if (mi is ConstructorInfo)
                return ToSignature ((ConstructorInfo)mi);
            if (mi is MethodInfo)
                return ToSignature ((MethodInfo)mi);
            return "[unknown: " + mi.GetType ().FullName + "]";
        }

        internal static string ToSignature (char parenOpen, ParameterInfo [] pi, char parenClose)
        {
            StringBuilder sb = new StringBuilder ();
            sb.Append (parenOpen);
            for (int i = 0; i < pi.Length; i++) {
                if (i > 0)
                    sb.Append (", ");
                if (pi [i].IsOut)
                    sb.Append ("out ");
                if (pi [i].IsIn)
                    sb.Append ("in ");
                if (IsParamsParameter (pi [i]))
                    sb.Append ("params ");
                sb.Append (ToSignature (pi [i].ParameterType));
                sb.Append (" ");
                sb.Append (pi [i].Name);
            }
            sb.Append (parenClose);
            return sb.ToString ();
        }


        internal static string ToSignature (MethodInfo mi)
        {
            StringBuilder sb = new StringBuilder ();
            sb.Append (ToSignature (mi.ReturnType));
            sb.Append (" ");
            sb.Append (ToSignature (mi.DeclaringType));
            sb.Append (".");
            sb.Append (mi.Name);
            sb.Append (ToSignature ('(', mi.GetParameters (), ')'));
            return sb.ToString ();
        }

        internal static bool HasParamsParameter (MethodBase mb)
        {
            return HasParamsParameter (mb.GetParameters ());
        }

        internal static bool HasParamsParameter (ParameterInfo [] pis)
        {
            for (int i = 0; i < pis.Length; i++)
                if (IsParamsParameter (pis [i]))
                    return true;
            return false;
        }

        internal static bool IsParamsParameter (ParameterInfo pi)
        {
            ParamArrayAttribute attr = (ParamArrayAttribute)
                CliHelper.GetCustomAttribute (typeof (ParamArrayAttribute), pi);
            return (attr != null);
        }


        internal static string ToSignature (Type type)
        {
            if (type.IsArray) {
                string ret = ToSignature (type.GetElementType ());
                for (int i = 0; i < type.GetArrayRank (); i++)
                    ret += "[]";
                return ret;
            }
            if (type == typeof (short))
                return "short";
            if (type == typeof (ushort))
                return "ushort";
            if (type == typeof (int))
                return "int";
            if (type == typeof (uint))
                return "uint";
            if (type == typeof (ulong))
                return "ulong";
            if (type == typeof (long))
                return "long";
            if (type == typeof (void))
                return "void";
            if (type == typeof (bool))
                return "bool";
            if (type == typeof (double))
                return "double";
            if (type == typeof (decimal))
                return "decimal";
            if (type == typeof (object))
                return "object";
            return type.FullName;
        }

        internal static bool IsNumberType (Type type)
        {
            return (
                   type == typeof (Int16)
                || type == typeof (UInt16)
                || type == typeof (Int32)
                || type == typeof (UInt32)
                || type == typeof (Int64)
                || type == typeof (UInt64)
                || type == typeof (Single)
                || type == typeof (Double)
                || type == typeof (Decimal));
        }

        internal static bool IsNumber (object value)
        {
            return (
                   value is Int16
                || value is UInt16
                || value is Int32
                || value is UInt32
                || value is Int64
                || value is UInt64
                || value is Single
                || value is Double
                || value is Decimal);
        }

        internal static object CreateInstance (Type cl)
        {
            try {
                return System.Activator.CreateInstance (cl);
            }
            catch {
                ;
            }
            return null;
        }

        internal static object GetCustomAttribute (Type type, Type attribute)
        {
            object [] attributes = type.GetCustomAttributes (attribute, true);
            if (attributes.Length < 1)
                return null;
            return attributes [0];
        }

        internal static object GetCustomAttribute (Type type, MemberInfo mi)
        {
            object attribute = null;
            object [] attributes = mi.GetCustomAttributes (type, true);
            if (attributes.Length > 0)
                attribute = attributes [0];
            return attribute;
        }

        internal static object GetCustomAttribute (Type type, ParameterInfo pi)
        {
            object [] attributes = pi.GetCustomAttributes (type, true);
            if (attributes.Length < 1)
                return null;
            return attributes [0];
        }

        internal static Type [] GetParameterTypes (ParameterInfo [] parameters)
        {
            Type [] types = new Type [parameters.Length];
            for (int i = 0; i < types.Length; i++) {
                types [i] = parameters [i].ParameterType;
            }
            return types;
        }

    }

}
