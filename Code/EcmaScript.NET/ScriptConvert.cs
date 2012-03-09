//------------------------------------------------------------------------------
// <license file="ScriptConvert.cs">
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
using System.Globalization;

namespace EcmaScript.NET
{

    public sealed class ScriptConvert
    {


        /// <summary>
        /// If character <tt>c</tt> is a hexadecimal digit, return
        /// <tt>accumulator</tt> * 16 plus corresponding
        /// number. Otherise return -1.
        /// </summary>		
        internal static int XDigitToInt (int c, int accumulator)
        {
            {
                // Use 0..9 < A..Z < a..z
                if (c <= '9') {
                    c -= '0';
                    if (0 <= c) {

                        goto check_brk;
                    }
                }
                else if (c <= 'F') {
                    if ('A' <= c) {
                        c -= ('A' - 10);

                        goto check_brk;
                    }
                }
                else if (c <= 'f') {
                    if ('a' <= c) {
                        c -= ('a' - 10);

                        goto check_brk;
                    }
                }
                return -1;
            }

        check_brk:
            ;

            return (accumulator << 4) | c;
        }


        public static IScriptable ToObject (IScriptable scope, object val)
        {
            if (val is IScriptable) {
                return (IScriptable)val;
            }
            return ToObject (null, scope, val);
        }

        public static IScriptable ToObjectOrNull (Context cx, object obj)
        {
            if (obj is IScriptable) {
                return (IScriptable)obj;
            }
            else if (obj != null && obj != Undefined.Value) {
                return ToObject (cx, ScriptRuntime.getTopCallScope (cx), obj);
            }
            return null;
        }

        /// <summary> Convert the value to an object.
        /// 
        /// See ECMA 9.9.
        /// </summary>
        public static IScriptable ToObject (Context cx, IScriptable scope, object val)
        {
            if (val is IScriptable) {
                return (IScriptable)val;
            }
            if (val == null) {
                throw ScriptRuntime.TypeErrorById ("msg.null.to.object");
            }
            if (val == Undefined.Value) {
                throw ScriptRuntime.TypeErrorById ("msg.undef.to.object");
            }
            string className = val is string ? "String" : (CliHelper.IsNumber (val) ? "Number" : (val is bool ? "Boolean" : null));
            if (className != null) {
                object [] args = new object [] { val };
                scope = ScriptableObject.GetTopLevelScope (scope);
                return ScriptRuntime.NewObject (cx == null ? Context.CurrentContext : cx, scope, className, args);
            }

            // Extension: Wrap as a LiveConnect object.
            object wrapped = cx.Wrap (scope, val, null);
            if (wrapped is IScriptable)
                return (IScriptable)wrapped;
            throw ScriptRuntime.errorWithClassName ("msg.invalid.type", val);
        }


        /// <summary> 
        /// See ECMA 9.4.
        /// </summary>
        public static double ToInteger (object val)
        {
            return ToInteger (ToNumber (val));
        }

        // convenience method
        public static double ToInteger (double d)
        {
            // if it's double.NaN
            if (double.IsNaN (d))
                return +0.0;

            if (d == 0.0 || d == System.Double.PositiveInfinity || d == System.Double.NegativeInfinity)
                return d;

            if (d > 0.0)
                return Math.Floor (d);
            else
                return Math.Ceiling (d);
        }

        public static double ToInteger (object [] args, int index)
        {
            return (index < args.Length) ? ToInteger (args [index]) : +0.0;
        }

        /// <summary> 
        /// See ECMA 9.5.
        /// </summary>
        public static int ToInt32 (object val)
        {
            // short circuit for common integer values
            if (val is int)
                return ((int)val);

            return ToInt32 (ToNumber (val));
        }

        public static int ToInt32 (object [] args, int index)
        {
            return (index < args.Length) ? ToInt32 (args [index]) : 0;
        }

        public static int ToInt32 (double d)
        {
            int id = (int)d;
            if (id == d) {
                // This covers -0.0 as well
                return id;
            }

            if (double.IsNaN (d) || d == System.Double.PositiveInfinity || d == System.Double.NegativeInfinity) {
                return 0;
            }

            d = (d >= 0) ? Math.Floor (d) : Math.Ceiling (d);

            double two32 = 4294967296.0;
            d = Math.IEEERemainder (d, two32);
            // (double)(long)d == d should hold here

            long l = (long)d;
            // returning (int)d does not work as d can be outside int range
            // but the result must always be 32 lower bits of l
            return (int)l;
        }

        /// <summary> See ECMA 9.6.</summary>
        /// <returns> long value representing 32 bits unsigned integer
        /// </returns>
        public static long ToUint32 (double d)
        {
            long l = (long)d;
            if (l == d) {
                // This covers -0.0 as well				                           
                return l & 0xffffffffL;
            }

            if (double.IsNaN (d) || d == System.Double.PositiveInfinity || d == System.Double.NegativeInfinity) {
                return 0;
            }

            d = (d >= 0) ? Math.Floor (d) : Math.Ceiling (d);

            double two32 = 4294967296.0;
            l = (long)Math.IEEERemainder (d, two32);
            unchecked {
                return l & (int)0xffffffffL;
            }
        }

        public static long ToUint32 (object val)
        {
            return ToUint32 (ToNumber (val));
        }


        /// <summary> Convert the value to a boolean.
        /// 
        /// See ECMA 9.2.
        /// </summary>
        public static bool ToBoolean (object val)
        {
            for (; ; ) {
                if (val is bool)
                    return ((bool)val);
                if (val == null || val == Undefined.Value)
                    return false;
                if (val is string)
                    return ((string)val).Length != 0;
                if (CliHelper.IsNumber (val)) {
                    double d = Convert.ToDouble (val);
                    return (!double.IsNaN (d) && d != 0.0);
                }
                if (val is IScriptable) {
                    if (Context.CurrentContext.VersionECMA1) {
                        // pure ECMA
                        return true;
                    }
                    // ECMA extension
                    val = ((IScriptable)val).GetDefaultValue (typeof (bool));
                    if (val is IScriptable)
                        throw ScriptRuntime.errorWithClassName ("msg.primitive.expected", val);
                    continue;
                }
                ScriptRuntime.WarnAboutNonJSObject (val);
                return true;
            }
        }

        public static bool ToBoolean (object [] args, int index)
        {
            return (index < args.Length) ? ToBoolean (args [index]) : false;
        }

        /// <summary> Convert the value to a number.
        /// 
        /// See ECMA 9.3.
        /// </summary>
        public static double ToNumber (object val)
        {
            for (; ; ) {
                if (val is double)
                    return (double)val;
                if (CliHelper.IsNumber (val))
                    return Convert.ToDouble (val);
                if (val == null)
                    return +0.0;
                if (val == Undefined.Value)
                    return double.NaN;
                if (val is string)
                    return ToNumber ((string)val);
                if (val is bool)
                    return ((bool)val) ? 1 : +0.0;
                if (val is IScriptable) {
                    val = ((IScriptable)val).GetDefaultValue (typeof (long));
                    if (val is IScriptable)
                        throw ScriptRuntime.errorWithClassName ("msg.primitive.expected", val);
                    continue;
                }
                ScriptRuntime.WarnAboutNonJSObject (val);
                return double.NaN;
            }
        }

        public static double ToNumber (object [] args, int index)
        {
            return (index < args.Length) ? ToNumber (args [index]) : double.NaN;
        }

        internal static double ToNumber (string s, int start, int radix)
        {
            char digitMax = '9';
            char lowerCaseBound = 'a';
            char upperCaseBound = 'A';
            int len = s.Length;
            if (radix < 10) {
                digitMax = (char)('0' + radix - 1);
            }
            if (radix > 10) {
                lowerCaseBound = (char)('a' + radix - 10);
                upperCaseBound = (char)('A' + radix - 10);
            }
            int end;
            double sum = 0.0;
            for (end = start; end < len; end++) {
                char c = s [end];
                int newDigit;
                if ('0' <= c && c <= digitMax)
                    newDigit = c - '0';
                else if ('a' <= c && c < lowerCaseBound)
                    newDigit = c - 'a' + 10;
                else if ('A' <= c && c < upperCaseBound)
                    newDigit = c - 'A' + 10;
                else
                    break;
                sum = sum * radix + newDigit;
            }
            if (start == end) {
                return double.NaN;
            }
            if (sum >= 9007199254740992.0) {
                if (radix == 10) {
                    /* If we're accumulating a decimal number and the number
                    * is >= 2^53, then the result from the repeated multiply-add
                    * above may be inaccurate.  Call Java to get the correct
                    * answer.
                    */
                    try {
                        return System.Double.Parse (s.Substring (start, (end) - (start)));
                    }
                    catch (System.FormatException) {
                        return double.NaN;
                    }
                }
                else if (radix == 2 || radix == 4 || radix == 8 || radix == 16 || radix == 32) {
                    /* The number may also be inaccurate for one of these bases.
                    * This happens if the addition in value*radix + digit causes
                    * a round-down to an even least significant mantissa bit
                    * when the first dropped bit is a one.  If any of the
                    * following digits in the number (which haven't been added
                    * in yet) are nonzero then the correct action would have
                    * been to round up instead of down.  An example of this
                    * occurs when reading the number 0x1000000000000081, which
                    * rounds to 0x1000000000000000 instead of 0x1000000000000100.
                    */
                    int bitShiftInChar = 1;
                    int digit = 0;

                    const int SKIP_LEADING_ZEROS = 0;
                    const int FIRST_EXACT_53_BITS = 1;
                    const int AFTER_BIT_53 = 2;
                    const int ZEROS_AFTER_54 = 3;
                    const int MIXED_AFTER_54 = 4;

                    int state = SKIP_LEADING_ZEROS;
                    int exactBitsLimit = 53;
                    double factor = 0.0;
                    bool bit53 = false;
                    // bit54 is the 54th bit (the first dropped from the mantissa)
                    bool bit54 = false;

                    for (; ; ) {
                        if (bitShiftInChar == 1) {
                            if (start == end)
                                break;
                            digit = s [start++];
                            if ('0' <= digit && digit <= '9')
                                digit -= '0';
                            else if ('a' <= digit && digit <= 'z')
                                digit -= ('a' - 10);
                            else
                                digit -= ('A' - 10);
                            bitShiftInChar = radix;
                        }
                        bitShiftInChar >>= 1;
                        bool bit = (digit & bitShiftInChar) != 0;

                        switch (state) {

                            case SKIP_LEADING_ZEROS:
                                if (bit) {
                                    --exactBitsLimit;
                                    sum = 1.0;
                                    state = FIRST_EXACT_53_BITS;
                                }
                                break;

                            case FIRST_EXACT_53_BITS:
                                sum *= 2.0;
                                if (bit)
                                    sum += 1.0;
                                --exactBitsLimit;
                                if (exactBitsLimit == 0) {
                                    bit53 = bit;
                                    state = AFTER_BIT_53;
                                }
                                break;

                            case AFTER_BIT_53:
                                bit54 = bit;
                                factor = 2.0;
                                state = ZEROS_AFTER_54;
                                break;

                            case ZEROS_AFTER_54:
                                if (bit) {
                                    state = MIXED_AFTER_54;
                                }
                                // fallthrough
                                goto case MIXED_AFTER_54;

                            case MIXED_AFTER_54:
                                factor *= 2;
                                break;
                        }
                    }
                    switch (state) {

                        case SKIP_LEADING_ZEROS:
                            sum = 0.0;
                            break;

                        case FIRST_EXACT_53_BITS:
                        case AFTER_BIT_53:
                            // do nothing
                            break;

                        case ZEROS_AFTER_54:
                            // x1.1 -> x1 + 1 (round up)
                            // x0.1 -> x0 (round down)
                            if (bit54 & bit53)
                                sum += 1.0;
                            sum *= factor;
                            break;

                        case MIXED_AFTER_54:
                            // x.100...1.. -> x + 1 (round up)
                            // x.0anything -> x (round down)
                            if (bit54)
                                sum += 1.0;
                            sum *= factor;
                            break;
                    }
                }
                /* We don't worry about inaccurate numbers for any other base. */
            }
            return sum;
        }


        public static string ToString (object [] args, int index)
        {
            return (index < args.Length) ? ToString (args [index]) : "undefined";
        }

        internal static object ToPrimitive (object val)
        {
            if (!(val is IScriptable)) {
                return val;
            }
            IScriptable s = (IScriptable)val;
            object result = s.GetDefaultValue (null);
            if (result is IScriptable)
                throw ScriptRuntime.TypeErrorById ("msg.bad.default.value");
            return result;
        }

        /// <summary> Convert the value to a string.
        /// 
        /// See ECMA 9.8.
        /// </summary>
        public static string ToString (object val)
        {
            for (; ; ) {
                if (val == null) {
                    return "null";
                }
                if (val == Undefined.Value) {
                    return "undefined";
                }
                if (val is string) {
                    return (string)val;
                }
                if (val is Boolean)
                    return ((bool)val) ? "true" : "false";
                if (CliHelper.IsNumber (val)) {
                    // TODO: should we just teach NativeNumber.stringValue()
                    // TODO: about Numbers?
                    return ToString (Convert.ToDouble (val), 10);
                }
                if (val is IScriptable) {
                    val = ((IScriptable)val).GetDefaultValue (typeof (string));
                    if (val is IScriptable) {
                        throw ScriptRuntime.errorWithClassName ("msg.primitive.expected", val);
                    }
                    continue;
                }
                return val.ToString ();
            }
        }



        /// <summary> ToNumber applied to the String type
        /// 
        /// See ECMA 9.3.1
        /// </summary>
        public static double ToNumber (string input)
        {
            int len = input.Length;
            int start = 0;
            char [] chars = input.ToCharArray ();
            char startChar;
            for (; ; ) {
                if (start == len) {
                    // Empty or contains only whitespace
                    return +0.0;
                }
                startChar = chars [start];
                if (!char.IsWhiteSpace (startChar))
                    break;
                start++;
            }

            if (startChar == '0') {
                if (start + 2 < len) {
                    int c1 = chars [start + 1];
                    if (c1 == 'x' || c1 == 'X') {
                        // A hexadecimal number
                        return ToNumber (input, start + 2, 16);
                    }
                }
            }
            else if (startChar == '+' || startChar == '-') {
                if (start + 3 < len && chars [start + 1] == '0') {
                    int c2 = chars [start + 2];
                    if (c2 == 'x' || c2 == 'X') {
                        // A hexadecimal number with sign
                        double val = ToNumber (input, start + 3, 16);
                        return startChar == '-' ? -val : val;
                    }
                }
            }

            int end = len - 1;
            char endChar;
            while (char.IsWhiteSpace (endChar = chars [end]))
                end--;
            if (endChar == 'y') {
                // check for "Infinity"
                if (startChar == '+' || startChar == '-')
                    start++;
                if (start + 7 == end && String.Compare (input, start, "Infinity", 0, 8) == 0)
                    return startChar == '-' ? System.Double.NegativeInfinity : System.Double.PositiveInfinity;
                return double.NaN;
            }
            // A non-hexadecimal, non-infinity number:
            // just try a normal floating point conversion
            string sub = input.Substring (start, (end + 1) - (start));

            // MS.NET will accept non-conformant strings
            // rather than throwing a NumberFormatException
            // as it should (like with \0).
            for (int i = sub.Length - 1; i >= 0; i--) {
                char c = sub [i];
                if (('0' <= c && c <= '9') || c == '.' ||
                    c == 'e' || c == 'E' ||
                    c == '+' || c == '-')
                    continue;
                return double.NaN;
            }

            try {
                double ret = double.Parse (sub);
                if (ret == 0) {
                    // IMHO a bug in MS.NET: double.Parse("-0.0") == 0.0 so we retard the "-" sign here
                    if (sub [0] == '-')
                        ret = -ret;
                }
                return ret;
            }
            catch (OverflowException) {
                // HACK 
                if (sub [0] == '-')
                    return double.NegativeInfinity;
                else
                    return double.PositiveInfinity;
            }
            catch (Exception) {
                return double.NaN;
            }
        }

        /// <summary> 
        /// See ECMA 9.7.
        /// </summary>
        public static char ToUint16 (object val)
        {
            double d = ToNumber (val);

            int i = (int)d;
            if (i == d) {
                return (char)i;
            }

            if (double.IsNaN (d) || d == System.Double.PositiveInfinity || d == System.Double.NegativeInfinity) {
                return (char)(0);
            }

            d = (d >= 0) ? Math.Floor (d) : Math.Ceiling (d);

            int int16 = 0x10000;
            i = (int)Math.IEEERemainder (d, int16);
            return (char)i;
        }

        /// <summary> Optimized version of toString(Object) for numbers.</summary>
        public static string ToString (double val)
        {
            return ToString (val, 10);
        }

        /// <summary>
        /// See ECMA 9.8.1
        /// </summary>
        /// <param name="d"></param>
        /// <param name="toBase"></param>
        /// <returns></returns>
        public static string ToString (double d, int toBase)
        {
            if (double.IsNaN (d))
                return "NaN";
            if (d == System.Double.PositiveInfinity)
                return "Infinity";
            if (d == System.Double.NegativeInfinity)
                return "-Infinity";
            if (d == 0.0)
                return "0";

            if ((toBase < 2) || (toBase > 36)) {
                throw Context.ReportRuntimeErrorById ("msg.bad.radix", Convert.ToString (toBase));
            }

            if (double.IsNaN (d))
                return "NaN";
            else if (Double.IsPositiveInfinity (d))
                return "Infinity";
            else if (Double.IsNegativeInfinity (d))
                return "-Infinity";
            else {
                // BugFix: Item 9856 - g16 yields better results than "g".  Not perfect, but better
                string ret = d.ToString ("g16");
                // TODO: This is plain wrong, but as close as we can get
                // without converting DtoA to C#.
                return ret;
            }
        }



    }

}
