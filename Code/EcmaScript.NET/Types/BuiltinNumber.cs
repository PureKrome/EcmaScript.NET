//------------------------------------------------------------------------------
// <license file="NativeNumber.cs">
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
using System.Globalization;

namespace EcmaScript.NET.Types
{

    /// <summary> This class implements the Number native object.
    /// 
    /// See ECMA 15.7.
    /// 
    /// </summary>
    sealed class BuiltinNumber : IdScriptableObject
    {

        internal const int DTOSTR_STANDARD = 0;
        internal const int DTOSTR_STANDARD_EXPONENTIAL = 1;
        internal const int DTOSTR_FIXED = 2;
        internal const int DTOSTR_EXPONENTIAL = 3;
        internal const int DTOSTR_PRECISION = 4; /* Either fixed or exponential format; <precision> significant digits */

        public const double NaN = double.NaN;

        public const double POSITIVE_INFINITY = double.PositiveInfinity;
        public const double NEGATIVE_INFINITY = double.NegativeInfinity;
        public const double MAX_VALUE = 1.7976931348623157e308;
        public const double MIN_VALUE = 5e-324;

        public static readonly double NegativeZero = BitConverter.Int64BitsToDouble (unchecked ((long)0x8000000000000000L));

        override public string ClassName
        {
            get
            {
                return "Number";
            }

        }


        private static readonly object NUMBER_TAG = new object ();

        private const int MAX_PRECISION = 100;

        internal static void Init (IScriptable scope, bool zealed)
        {
            BuiltinNumber obj = new BuiltinNumber (0.0);
            obj.ExportAsJSClass (MAX_PROTOTYPE_ID, scope, zealed
                , ScriptableObject.DONTENUM | ScriptableObject.READONLY | ScriptableObject.PERMANENT);
        }

        private BuiltinNumber (double number)
        {
            doubleValue = number;
        }

        protected internal override void FillConstructorProperties (IdFunctionObject ctor)
        {
            const int attr = ScriptableObject.DONTENUM | ScriptableObject.PERMANENT | ScriptableObject.READONLY;

            ctor.DefineProperty ("NaN", NaN, attr);
            ctor.DefineProperty ("POSITIVE_INFINITY", POSITIVE_INFINITY, attr);
            ctor.DefineProperty ("NEGATIVE_INFINITY", NEGATIVE_INFINITY, attr);
            ctor.DefineProperty ("MAX_VALUE", MAX_VALUE, attr);
            ctor.DefineProperty ("MIN_VALUE", MIN_VALUE, attr);

            base.FillConstructorProperties (ctor);
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
                    arity = 1;
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
                case Id_valueOf:
                    arity = 0;
                    s = "valueOf";
                    break;
                case Id_toFixed:
                    arity = 1;
                    s = "toFixed";
                    break;
                case Id_toExponential:
                    arity = 1;
                    s = "toExponential";
                    break;
                case Id_toPrecision:
                    arity = 1;
                    s = "toPrecision";
                    break;
                default:
                    throw new ArgumentException (Convert.ToString (id));

            }
            InitPrototypeMethod (NUMBER_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (NUMBER_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;
            if (id == Id_constructor) {
                double val = (args.Length >= 1) ? ScriptConvert.ToNumber (args [0]) : 0.0;
                if (thisObj == null) {
                    // new Number(val) creates a new Number object.
                    return new BuiltinNumber (val);
                }
                // Number(val) converts val to a number value.
                return val;
            }

            // The rest of Number.prototype methods require thisObj to be Number
            BuiltinNumber nativeNumber = (thisObj as BuiltinNumber);
            if (nativeNumber == null)
                throw IncompatibleCallError (f);
            double value = nativeNumber.doubleValue;

            int toBase = 0;
            switch (id) {


                case Id_toString:
                    toBase = (args.Length == 0) ? 10 : ScriptConvert.ToInt32 (args [0]);
                    return ScriptConvert.ToString (value, toBase);

                case Id_toLocaleString: {
                        // toLocaleString is just an alias for toString for now
                        toBase = (args.Length == 0) ? 10 : ScriptConvert.ToInt32 (args [0]);
                        return ScriptConvert.ToString (value, toBase);
                    }


                case Id_toSource:
                    return "(new Number(" + ScriptConvert.ToString (value) + "))";


                case Id_valueOf:
                    return value;


                case Id_toFixed:
                    return num_to (value, args, DTOSTR_FIXED, DTOSTR_FIXED, -20, 0);


                case Id_toExponential:
                    return num_to (value, args, DTOSTR_STANDARD_EXPONENTIAL, DTOSTR_EXPONENTIAL, 0, 1);


                case Id_toPrecision: {
                        if (args.Length < 0 || args [0] == Undefined.Value)
                            return ScriptConvert.ToString (value);
                        int precision = ScriptConvert.ToInt32 (args [0]);
                        if (precision < 0 || precision > MAX_PRECISION) {
                            throw ScriptRuntime.ConstructError ("RangeError",
                                ScriptRuntime.GetMessage ("msg.bad.precision", ScriptConvert.ToString (args [0])));
                        }
                        return value.ToString (GetFormatString (precision));
                    }


                default:
                    throw new ArgumentException (Convert.ToString (id));

            }
        }

        static string GetFormatString (int precision)
        {
            string formatString = "0.";
            for (int i = 0; i < precision; i++)
                formatString += "#";
            return formatString;
        }

        public override string ToString ()
        {
            return ScriptConvert.ToString (doubleValue, 10);
        }

        private static NumberFormatInfo m_NumberFormatter = null;
        public static NumberFormatInfo NumberFormatter
        {
            get
            {
                if (m_NumberFormatter == null) {
                    m_NumberFormatter = new NumberFormatInfo ();
                    m_NumberFormatter.PercentGroupSeparator = ",";
                    m_NumberFormatter.NumberDecimalSeparator = ".";
                }
                return m_NumberFormatter;
            }
        }

        private static string num_to (double val, object [] args, int zeroArgMode, int oneArgMode, int precisionMin, int precisionOffset)
        {
            int precision;
            if (args.Length == 0) {
                precision = 0;
                oneArgMode = zeroArgMode;
            }
            else {
                /* We allow a larger range of precision than
                ECMA requires; this is permitted by ECMA. */
                precision = ScriptConvert.ToInt32 (args [0]);
                if (precision < precisionMin || precision > MAX_PRECISION) {
                    string msg = ScriptRuntime.GetMessage ("msg.bad.precision", ScriptConvert.ToString (args [0]));
                    throw ScriptRuntime.ConstructError ("RangeError", msg);
                }
            }


            switch (zeroArgMode) {
                case DTOSTR_FIXED:
                    return val.ToString ("F" + (precision + precisionOffset), NumberFormatter);
                case DTOSTR_STANDARD_EXPONENTIAL:
                    return val.ToString ("e" + (precision + precisionOffset), NumberFormatter);
                case DTOSTR_STANDARD:
                    if (oneArgMode == DTOSTR_PRECISION) {
                        return val.ToString (precision.ToString (), NumberFormatter);
                    }
                    else {
                        return val.ToString (NumberFormatter);
                    }
            }

            Context.CodeBug ();
            return string.Empty; // Not reached
        }

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
                    case 7:
                        c = s [0];
                        if (c == 't') { X = "toFixed"; id = Id_toFixed; }
                        else if (c == 'v') { X = "valueOf"; id = Id_valueOf; }
                        break;
                    case 8:
                        c = s [3];
                        if (c == 'o') { X = "toSource"; id = Id_toSource; }
                        else if (c == 't') { X = "toString"; id = Id_toString; }
                        break;
                    case 11:
                        c = s [0];
                        if (c == 'c') { X = "constructor"; id = Id_constructor; }
                        else if (c == 't') { X = "toPrecision"; id = Id_toPrecision; }
                        break;
                    case 13:
                        X = "toExponential";
                        id = Id_toExponential;
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

        #region PrototypeIds
        private const int Id_constructor = 1;
        private const int Id_toString = 2;
        private const int Id_toLocaleString = 3;
        private const int Id_toSource = 4;
        private const int Id_valueOf = 5;
        private const int Id_toFixed = 6;
        private const int Id_toExponential = 7;
        private const int Id_toPrecision = 8;
        private const int MAX_PROTOTYPE_ID = 8;
        #endregion

        private double doubleValue;


    }
}
