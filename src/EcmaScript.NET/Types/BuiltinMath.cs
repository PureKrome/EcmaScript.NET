//------------------------------------------------------------------------------
// <license file="NativeMath.cs">
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

namespace EcmaScript.NET.Types
{

    /// <summary>
    /// This class implements the Math native object.
    /// See ECMA 15.8.
    /// </summary>
    internal sealed class BuiltinMath : IdScriptableObject
    {

        public override string ClassName
        {
            get
            {
                return "Math";
            }

        }


        private const double NET_WORKAROUND_1 = 2.35619449019234;
        private const double NET_WORKAROUND_2 = -2.35619449019234;
        private const double NET_WORKAROUND_3 = 0.785398163397448;
        private const double NET_WORKAROUND_4 = -0.785398163397448;

        private static readonly object MATH_TAG = new object ();

        internal static void Init (IScriptable scope, bool zealed)
        {
            BuiltinMath obj = new BuiltinMath ();
            obj.ActivatePrototypeMap (MAX_ID);
            obj.SetPrototype (GetObjectPrototype (scope));
            obj.ParentScope = scope;
            if (zealed) {
                obj.SealObject ();
            }
            ScriptableObject.DefineProperty (scope, "Math", obj, ScriptableObject.DONTENUM
                | ScriptableObject.READONLY | ScriptableObject.PERMANENT);
        }

        private BuiltinMath ()
        {
            ;
        }

        protected internal override void InitPrototypeId (int id)
        {
            if (id <= LAST_METHOD_ID) {
                string name;
                int arity;

                switch (id) {
                    case Id_toSource:
                        arity = 0;
                        name = "toSource";
                        break;
                    case Id_abs:
                        arity = 1;
                        name = "abs";
                        break;
                    case Id_acos:
                        arity = 1;
                        name = "acos";
                        break;
                    case Id_asin:
                        arity = 1;
                        name = "asin";
                        break;
                    case Id_atan:
                        arity = 1;
                        name = "atan";
                        break;
                    case Id_atan2:
                        arity = 2;
                        name = "atan2";
                        break;
                    case Id_ceil:
                        arity = 1;
                        name = "ceil";
                        break;
                    case Id_cos:
                        arity = 1;
                        name = "cos";
                        break;
                    case Id_exp:
                        arity = 1;
                        name = "exp";
                        break;
                    case Id_floor:
                        arity = 1;
                        name = "floor";
                        break;
                    case Id_log:
                        arity = 1;
                        name = "log";
                        break;
                    case Id_max:
                        arity = 2;
                        name = "max";
                        break;
                    case Id_min:
                        arity = 2;
                        name = "min";
                        break;
                    case Id_pow:
                        arity = 2;
                        name = "pow";
                        break;
                    case Id_random:
                        arity = 0;
                        name = "random";
                        break;
                    case Id_round:
                        arity = 1;
                        name = "round";
                        break;
                    case Id_sin:
                        arity = 1;
                        name = "sin";
                        break;
                    case Id_sqrt:
                        arity = 1;
                        name = "sqrt";
                        break;
                    case Id_tan:
                        arity = 1;
                        name = "tan";
                        break;
                    default:
                        throw new Exception (Convert.ToString (id));

                }
                InitPrototypeMethod (MATH_TAG, id, name, arity);
            }
            else {
                string name;
                double x;
                switch (id) {
                    case Id_E:
                        x = Math.E;
                        name = "E";
                        break;
                    case Id_PI:
                        x = Math.PI;
                        name = "PI";
                        break;
                    case Id_LN10:
                        x = 2.302585092994046;
                        name = "LN10";
                        break;
                    case Id_LN2:
                        x = 0.6931471805599453;
                        name = "LN2";
                        break;
                    case Id_LOG2E:
                        x = 1.4426950408889634;
                        name = "LOG2E";
                        break;
                    case Id_LOG10E:
                        x = 0.4342944819032518;
                        name = "LOG10E";
                        break;
                    case Id_SQRT1_2:
                        x = 0.7071067811865476;
                        name = "SQRT1_2";
                        break;
                    case Id_SQRT2:
                        x = 1.4142135623730951;
                        name = "SQRT2";
                        break;
                    default:
                        throw new Exception (Convert.ToString (id));

                }
                InitPrototypeValue (id, name, (x), DONTENUM | READONLY | PERMANENT);
            }
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (MATH_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            double x, y;
            int methodId = f.MethodId;
            switch (methodId) {

                case Id_toSource:
                    return "Math";


                case Id_abs:
                    x = ScriptConvert.ToNumber (args, 0);
                    // abs(-0.0) should be 0.0, but -0.0 < 0.0 == false
                    x = (x == 0.0) ? 0.0 : ((x < 0.0) ? -x : x);
                    break;


                case Id_acos:
                case Id_asin:
                    x = ScriptConvert.ToNumber (args, 0);
                    if (!double.IsNaN (x) && -1.0 <= x && x <= 1.0) {
                        x = (methodId == Id_acos) ? Math.Acos (x) : Math.Asin (x);
                    }
                    else {
                        x = System.Double.NaN;
                    }
                    break;


                case Id_atan:
                    x = ScriptConvert.ToNumber (args, 0);
                    x = Math.Atan (x);
                    break;


                case Id_atan2:
                    x = ScriptConvert.ToNumber (args, 0);
                    y = ScriptConvert.ToNumber (args, 1);
                    if (x == double.PositiveInfinity && y == double.PositiveInfinity) {
                        x = NET_WORKAROUND_3;
                    }
                    else if (x == double.PositiveInfinity && y == double.NegativeInfinity) {
                        x = NET_WORKAROUND_1;
                    }
                    else if (x == double.NegativeInfinity && y == double.PositiveInfinity) {
                        x = NET_WORKAROUND_4;
                    }
                    else if (x == double.NegativeInfinity && y == double.NegativeInfinity) {
                        x = NET_WORKAROUND_2;
                    }
                    else {
                        x = Math.Atan2 (x, y);
                    }
                    break;


                case Id_ceil:
                    x = ScriptConvert.ToNumber (args, 0);
                    x = Math.Ceiling (x);
                    break;


                case Id_cos:
                    x = ScriptConvert.ToNumber (args, 0);
                    x = (x == System.Double.PositiveInfinity || x == System.Double.NegativeInfinity) ? System.Double.NaN : Math.Cos (x);
                    break;


                case Id_exp:
                    x = ScriptConvert.ToNumber (args, 0);
                    x = (x == System.Double.PositiveInfinity) ? x : ((x == System.Double.NegativeInfinity) ? 0.0 : Math.Exp (x));
                    break;


                case Id_floor:
                    x = ScriptConvert.ToNumber (args, 0);
                    x = Math.Floor (x);
                    break;


                case Id_log:
                    x = ScriptConvert.ToNumber (args, 0);
                    // Java's log(<0) = -Infinity; we need NaN
                    x = (x < 0) ? System.Double.NaN : Math.Log (x);
                    break;


                case Id_max:
                case Id_min:
                    x = (methodId == Id_max) ? System.Double.NegativeInfinity : System.Double.PositiveInfinity;
                    for (int i = 0; i != args.Length; ++i) {
                        double d = ScriptConvert.ToNumber (args [i]);
                        if (double.IsNaN (d)) {
                            x = d; // NaN
                            break;
                        }
                        if (methodId == Id_max) {
                            // if (x < d) x = d; does not work due to -0.0 >= +0.0
                            x = Math.Max (x, d);
                        }
                        else {
                            x = Math.Min (x, d);
                        }
                    }
                    break;


                case Id_pow:
                    x = ScriptConvert.ToNumber (args, 0);
                    x = js_pow (x, ScriptConvert.ToNumber (args, 1));
                    break;


                case Id_random:
                    x = (new Random ()).NextDouble ();
                    break;


                case Id_round:
                    x = ScriptConvert.ToNumber (args, 0);
                    if (!double.IsNaN (x) && x != System.Double.PositiveInfinity && x != System.Double.NegativeInfinity) {
                        long l = (long)Math.Floor (x + 0.5);
                        if (l != 0) {
                            x = l;
                        }
                        else {
                            // We must propagate the sign of d into the result
                            if (x < 0.0) {
                                x = BuiltinNumber.NegativeZero;
                            }
                            else if (x != 0.0) {
                                x = 0.0;
                            }
                        }
                    }
                    break;


                case Id_sin:
                    x = ScriptConvert.ToNumber (args, 0);
                    x = (x == System.Double.PositiveInfinity || x == System.Double.NegativeInfinity) ? System.Double.NaN : Math.Sin (x);
                    break;


                case Id_sqrt:
                    x = ScriptConvert.ToNumber (args, 0);
                    x = Math.Sqrt (x);
                    break;


                case Id_tan:
                    x = ScriptConvert.ToNumber (args, 0);
                    x = Math.Tan (x);
                    break;


                default:
                    throw new Exception (Convert.ToString (methodId));

            }
            return (x);
        }

        // See Ecma 15.8.2.13
        private double js_pow (double x, double y)
        {
            double result;
            if (double.IsNaN (y)) {
                // y is NaN, result is always NaN
                result = y;
            }
            else if (y == 0) {
                // Java's pow(NaN, 0) = NaN; we need 1
                result = 1.0;
            }
            else if (x == 0) {
                // Many dirrerences from Java's Math.pow
                if (1 / x > 0) {
                    result = (y > 0) ? 0 : System.Double.PositiveInfinity;
                }
                else {
                    // x is -0, need to check if y is an odd integer					
                    long y_long = (long)y;
                    if (y_long == y && (y_long & 0x1) != 0) {
                        result = (y > 0) ? -0.0 : System.Double.NegativeInfinity;
                    }
                    else {
                        result = (y > 0) ? 0.0 : System.Double.PositiveInfinity;
                    }
                }
            }
            else {
                result = Math.Pow (x, y);
                if (!double.IsNaN (y)) {
                    // Check for broken Java implementations that gives NaN
                    // when they should return something else
                    if (y == System.Double.PositiveInfinity) {
                        if (x < -1.0 || 1.0 < x) {
                            result = System.Double.PositiveInfinity;
                        }
                        else if (-1.0 < x && x < 1.0) {
                            result = 0;
                            // TODO: is this really necessary?
                        }
                        else if (x == 1) {
                            result = double.NaN;
                            // TODO: is this really necessary?
                        }
                        else if (x == -1) {
                            result = double.NaN;
                        }
                    }
                    else if (y == System.Double.NegativeInfinity) {
                        if (x < -1.0 || 1.0 < x) {
                            result = 0;
                        }
                        else if (-1.0 < x && x < 1.0) {
                            result = System.Double.PositiveInfinity;
                            // TODO: is this really necessary?
                        }
                        else if (x == 1) {
                            result = double.NaN;
                            // TODO: is this really necessary?
                        }
                        else if (x == -1) {
                            result = double.NaN;
                        }
                    }
                    else if (x == System.Double.PositiveInfinity) {
                        result = (y > 0) ? System.Double.PositiveInfinity : 0.0;
                    }
                    else if (x == System.Double.NegativeInfinity) {
                        long y_long = (long)y;
                        if (y_long == y && (y_long & 0x1) != 0) {
                            // y is odd integer
                            result = (y > 0) ? System.Double.NegativeInfinity : -0.0;
                        }
                        else {
                            result = (y > 0) ? System.Double.PositiveInfinity : 0.0;
                        }
                    }
                }
            }
            return result;
        }

        #region PrototypeIds
        private const int Id_toSource = 1;
        private const int Id_abs = 2;
        private const int Id_acos = 3;
        private const int Id_asin = 4;
        private const int Id_atan = 5;
        private const int Id_atan2 = 6;
        private const int Id_ceil = 7;
        private const int Id_cos = 8;
        private const int Id_exp = 9;
        private const int Id_floor = 10;
        private const int Id_log = 11;
        private const int Id_max = 12;
        private const int Id_min = 13;
        private const int Id_pow = 14;
        private const int Id_random = 15;
        private const int Id_round = 16;
        private const int Id_sin = 17;
        private const int Id_sqrt = 18;
        private const int Id_tan = 19;
        private const int LAST_METHOD_ID = Id_tan;
        private const int Id_E = 20;
        private const int Id_PI = 21;
        private const int Id_LN10 = 22;
        private const int Id_LN2 = 23;
        private const int Id_LOG2E = 24;
        private const int Id_LOG10E = 25;
        private const int Id_SQRT1_2 = 26;
        private const int Id_SQRT2 = 27;
        private const int MAX_ID = Id_SQRT2;
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
                    case 1:
                        if (s [0] == 'E') { id = Id_E; goto EL0; }
                        break;
                    case 2:
                        if (s [0] == 'P' && s [1] == 'I') { id = Id_PI; goto EL0; }
                        break;
                    case 3:
                        switch (s [0]) {
                            case 'L':
                                if (s [2] == '2' && s [1] == 'N') { id = Id_LN2; goto EL0; }
                                break;
                            case 'a':
                                if (s [2] == 's' && s [1] == 'b') { id = Id_abs; goto EL0; }
                                break;
                            case 'c':
                                if (s [2] == 's' && s [1] == 'o') { id = Id_cos; goto EL0; }
                                break;
                            case 'e':
                                if (s [2] == 'p' && s [1] == 'x') { id = Id_exp; goto EL0; }
                                break;
                            case 'l':
                                if (s [2] == 'g' && s [1] == 'o') { id = Id_log; goto EL0; }
                                break;
                            case 'm':
                                c = s [2];
                                if (c == 'n') { if (s [1] == 'i') { id = Id_min; goto EL0; } }
                                else if (c == 'x') { if (s [1] == 'a') { id = Id_max; goto EL0; } }
                                break;
                            case 'p':
                                if (s [2] == 'w' && s [1] == 'o') { id = Id_pow; goto EL0; }
                                break;
                            case 's':
                                if (s [2] == 'n' && s [1] == 'i') { id = Id_sin; goto EL0; }
                                break;
                            case 't':
                                if (s [2] == 'n' && s [1] == 'a') { id = Id_tan; goto EL0; }
                                break;
                        }
                        break;
                    case 4:
                        switch (s [1]) {
                            case 'N':
                                X = "LN10";
                                id = Id_LN10;
                                break;
                            case 'c':
                                X = "acos";
                                id = Id_acos;
                                break;
                            case 'e':
                                X = "ceil";
                                id = Id_ceil;
                                break;
                            case 'q':
                                X = "sqrt";
                                id = Id_sqrt;
                                break;
                            case 's':
                                X = "asin";
                                id = Id_asin;
                                break;
                            case 't':
                                X = "atan";
                                id = Id_atan;
                                break;
                        }
                        break;
                    case 5:
                        switch (s [0]) {
                            case 'L':
                                X = "LOG2E";
                                id = Id_LOG2E;
                                break;
                            case 'S':
                                X = "SQRT2";
                                id = Id_SQRT2;
                                break;
                            case 'a':
                                X = "atan2";
                                id = Id_atan2;
                                break;
                            case 'f':
                                X = "floor";
                                id = Id_floor;
                                break;
                            case 'r':
                                X = "round";
                                id = Id_round;
                                break;
                        }
                        break;
                    case 6:
                        c = s [0];
                        if (c == 'L') { X = "LOG10E"; id = Id_LOG10E; }
                        else if (c == 'r') { X = "random"; id = Id_random; }
                        break;
                    case 7:
                        X = "SQRT1_2";
                        id = Id_SQRT1_2;
                        break;
                    case 8:
                        X = "toSource";
                        id = Id_toSource;
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
