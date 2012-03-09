//------------------------------------------------------------------------------
// <license file="NativeGlobal.cs">
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

using EcmaScript.NET.Types.E4X;

namespace EcmaScript.NET.Types
{

    /// <summary> This class implements the global native object (function and value
    /// properties only).
    /// 
    /// See ECMA 15.1.[12].
    /// 
    /// </summary>	
    public class BuiltinGlobal : IIdFunctionCall
    {

        public static void Init (Context cx, IScriptable scope, bool zealed)
        {
            BuiltinGlobal obj = new BuiltinGlobal ();

            for (int id = 1; id <= LAST_SCOPE_FUNCTION_ID; ++id) {
                string name;
                int arity = 1;
                switch (id) {

                    case Id_decodeURI:
                        name = "decodeURI";
                        break;

                    case Id_decodeURIComponent:
                        name = "decodeURIComponent";
                        break;

                    case Id_encodeURI:
                        name = "encodeURI";
                        break;

                    case Id_encodeURIComponent:
                        name = "encodeURIComponent";
                        break;

                    case Id_escape:
                        name = "escape";
                        break;

                    case Id_eval:
                        name = "eval";
                        break;

                    case Id_isFinite:
                        name = "isFinite";
                        break;

                    case Id_isNaN:
                        name = "isNaN";
                        break;

                    case Id_isXMLName:
                        name = "isXMLName";
                        break;

                    case Id_parseFloat:
                        name = "parseFloat";
                        break;

                    case Id_parseInt:
                        name = "parseInt";
                        arity = 2;
                        break;

                    case Id_unescape:
                        name = "unescape";
                        break;

                    case Id_uneval:
                        name = "uneval";
                        break;

                    default:
                        throw Context.CodeBug ();

                }
                IdFunctionObject f = new IdFunctionObject (obj, FTAG, id, name, arity, scope);
                if (zealed) {
                    f.SealObject ();
                }
                f.ExportAsScopeProperty ();
            }

            ScriptableObject.DefineProperty (scope, "NaN", (object)double.NaN, ScriptableObject.DONTENUM);
            ScriptableObject.DefineProperty (scope, "Infinity", (System.Double.PositiveInfinity), ScriptableObject.DONTENUM);
            ScriptableObject.DefineProperty (scope, "undefined", Undefined.Value, ScriptableObject.DONTENUM);

            string [] errorMethods = new string [] {
                "ConversionError",
                "EvalError",
                "RangeError",
                "ReferenceError",
                "SyntaxError",
                "TypeError",
                "URIError",
                "InternalError",
                "JavaException"
            };

            /*
            Each error constructor gets its own Error object as a prototype,
            with the 'name' property set to the name of the error.
            */
            for (int i = 0; i < errorMethods.Length; i++) {
                string name = errorMethods [i];
                IScriptable errorProto = ScriptRuntime.NewObject (cx, scope, "Error", ScriptRuntime.EmptyArgs);
                errorProto.Put ("name", errorProto, name);
                if (zealed) {
                    if (errorProto is ScriptableObject) {
                        ((ScriptableObject)errorProto).SealObject ();
                    }
                }
                IdFunctionObject ctor = new IdFunctionObject (obj, FTAG, Id_new_CommonError, name, 1, scope);
                ctor.MarkAsConstructor (errorProto);
                if (zealed) {
                    ctor.SealObject ();
                }
                ctor.ExportAsScopeProperty ();
            }
        }

        public virtual object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (f.HasTag (FTAG)) {
                int methodId = f.MethodId;
                switch (methodId) {

                    case Id_decodeURI:
                    case Id_decodeURIComponent: {
                            string str = ScriptConvert.ToString (args, 0);
                            return decode (str, methodId == Id_decodeURI);
                        }


                    case Id_encodeURI:
                    case Id_encodeURIComponent: {
                            string str = ScriptConvert.ToString (args, 0);
                            return encode (str, methodId == Id_encodeURI);
                        }


                    case Id_escape:
                        return js_escape (args);


                    case Id_eval:
                        return ImplEval (cx, scope, thisObj, args);


                    case Id_isFinite: {
                            bool result;
                            if (args.Length < 1) {
                                result = false;
                            }
                            else {
                                double d = ScriptConvert.ToNumber (args [0]);
                                result = (!double.IsNaN (d) && d != System.Double.PositiveInfinity && d != System.Double.NegativeInfinity);
                            }
                            return result;
                        }


                    case Id_isNaN: {
                            // The global method isNaN, as per ECMA-262 15.1.2.6.
                            bool result;
                            if (args.Length < 1) {
                                result = true;
                            }
                            else {
                                double d = ScriptConvert.ToNumber (args [0]);
                                result = (double.IsNaN (d));
                            }
                            return result;
                        }


                    case Id_isXMLName: {
                            object name = (args.Length == 0) ? Undefined.Value : args [0];
                            XMLLib xmlLib = XMLLib.ExtractFromScope (scope);
                            return xmlLib.IsXMLName (cx, name);
                        }


                    case Id_parseFloat:
                        return js_parseFloat (args);


                    case Id_parseInt:
                        return js_parseInt (args);


                    case Id_unescape:
                        return js_unescape (args);


                    case Id_uneval: {
                            object value = (args.Length != 0) ? args [0] : Undefined.Value;
                            return ScriptRuntime.uneval (cx, scope, value);
                        }


                    case Id_new_CommonError:
                        // The implementation of all the ECMA error constructors
                        // (SyntaxError, TypeError, etc.)
                        return BuiltinError.make (cx, scope, f, args);
                }
            }
            throw f.Unknown ();
        }

        /// <summary> The global method parseInt, as per ECMA-262 15.1.2.2.</summary>
        private object js_parseInt (object [] args)
        {
            string s = ScriptConvert.ToString (args, 0);
            int radix = ScriptConvert.ToInt32 (args, 1);

            int len = s.Length;
            if (len == 0)
                return double.NaN;

            bool negative = false;
            int start = 0;
            char c;
            do {
                c = s [start];
                if (!char.IsWhiteSpace (c))
                    break;
                start++;
            }
            while (start < len);

            if (c == '+' || (negative = (c == '-')))
                start++;

            const int NO_RADIX = -1;
            if (radix == 0) {
                radix = NO_RADIX;
            }
            else if (radix < 2 || radix > 36) {
                return double.NaN;
            }
            else if (radix == 16 && len - start > 1 && s [start] == '0') {
                c = s [start + 1];
                if (c == 'x' || c == 'X')
                    start += 2;
            }

            if (radix == NO_RADIX) {
                radix = 10;
                if (len - start > 1 && s [start] == '0') {
                    c = s [start + 1];
                    if (c == 'x' || c == 'X') {
                        radix = 16;
                        start += 2;
                    }
                    else if ('0' <= c && c <= '9') {
                        radix = 8;
                        start++;
                    }
                }
            }

            double d = ScriptConvert.ToNumber (s, start, radix);
            return (negative ? -d : d);
        }

        /// <summary> The global method parseFloat, as per ECMA-262 15.1.2.3.
        /// 
        /// </summary>
        /// <param name="cx">unused
        /// </param>
        /// <param name="thisObj">unused
        /// </param>
        /// <param name="args">the arguments to parseFloat, ignoring args[>=1]
        /// </param>
        /// <param name="funObj">unused
        /// </param>
        private object js_parseFloat (object [] args)
        {
            if (args.Length < 1)
                return double.NaN;

            string s = ScriptConvert.ToString (args [0]);
            int len = s.Length;
            int start = 0;
            // Scan forward to skip whitespace
            char c;
            for (; ; ) {
                if (start == len) {
                    return double.NaN;
                }
                c = s [start];
                if (!TokenStream.isJSSpace (c)) {
                    break;
                }
                ++start;
            }

            int i = start;
            if (c == '+' || c == '-') {
                ++i;
                if (i == len) {
                    return double.NaN;
                }
                c = s [i];
            }

            if (c == 'I') {
                // check for "Infinity"
                if (i + 8 <= len && String.Compare (s, i, "Infinity", 0, 8) == 0) {
                    double d;
                    if (s [start] == '-') {
                        d = System.Double.NegativeInfinity;
                    }
                    else {
                        d = System.Double.PositiveInfinity;
                    }
                    return (d);
                }
                return double.NaN;
            }

            // Find the end of the legal bit
            int dec = -1;
            int exponent = -1;
            for (; i < len; i++) {
                switch (s [i]) {

                    case '.':
                        if (dec != -1)
                            // Only allow a single decimal point.
                            break;
                        dec = i;
                        continue;


                    case 'e':
                    case 'E':
                        if (exponent != -1)
                            break;
                        exponent = i;
                        continue;


                    case '+':
                    case '-':
                        // Only allow '+' or '-' after 'e' or 'E'
                        if (exponent != i - 1)
                            break;
                        continue;


                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        continue;


                    default:
                        break;

                }
                break;
            }
            s = s.Substring (start, (i) - (start));
            try {
                return System.Double.Parse (s, BuiltinNumber.NumberFormatter);

            }
            catch (OverflowException) {
                // HACK 
                if (s [0] == '-')
                    return double.NegativeInfinity;
                else
                    return double.PositiveInfinity;
            }
            catch (Exception) {
                return double.NaN;
            }
        }

        /// <summary> The global method escape, as per ECMA-262 15.1.2.4.
        /// Includes code for the 'mask' argument supported by the C escape
        /// method, which used to be part of the browser imbedding.  Blame
        /// for the strange constant names should be directed there.
        /// </summary>

        private object js_escape (object [] args)
        {
            const int URL_XALPHAS = 1;
            const int URL_XPALPHAS = 2;
            const int URL_PATH = 4;

            string s = ScriptConvert.ToString (args, 0);

            int mask = URL_XALPHAS | URL_XPALPHAS | URL_PATH;
            if (args.Length > 1) {
                // the 'mask' argument.  Non-ECMA.
                double d = ScriptConvert.ToNumber (args [1]);
                if (double.IsNaN (d) || ((mask = (int)d) != d) || 0 != (mask & ~(URL_XALPHAS | URL_XPALPHAS | URL_PATH))) {
                    throw Context.ReportRuntimeErrorById ("msg.bad.esc.mask");
                }
            }

            System.Text.StringBuilder sb = null;
            for (int k = 0, L = s.Length; k != L; ++k) {
                int c = s [k];
                if (mask != 0 && ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '@' || c == '*' || c == '_' || c == '-' || c == '.' || (0 != (mask & URL_PATH) && (c == '/' || c == '+')))) {
                    if (sb != null) {
                        sb.Append ((char)c);
                    }
                }
                else {
                    if (sb == null) {
                        sb = new System.Text.StringBuilder (L + 3);
                        sb.Append (s);
                        sb.Length = k;
                    }

                    int hexSize;
                    if (c < 256) {
                        if (c == ' ' && mask == URL_XPALPHAS) {
                            sb.Append ('+');
                            continue;
                        }
                        sb.Append ('%');
                        hexSize = 2;
                    }
                    else {
                        sb.Append ('%');
                        sb.Append ('u');
                        hexSize = 4;
                    }

                    // append hexadecimal form of c left-padded with 0
                    for (int shift = (hexSize - 1) * 4; shift >= 0; shift -= 4) {
                        int digit = 0xf & (c >> shift);
                        int hc = (digit < 10) ? '0' + digit : 'A' - 10 + digit;
                        sb.Append ((char)hc);
                    }
                }
            }

            return (sb == null) ? s : sb.ToString ();
        }

        /// <summary> The global unescape method, as per ECMA-262 15.1.2.5.</summary>

        private object js_unescape (object [] args)
        {
            string s = ScriptConvert.ToString (args, 0);
            int firstEscapePos = s.IndexOf ((char)'%');
            if (firstEscapePos >= 0) {
                int L = s.Length;
                char [] buf = s.ToCharArray ();
                int destination = firstEscapePos;
                for (int k = firstEscapePos; k != L; ) {
                    char c = buf [k];
                    ++k;
                    if (c == '%' && k != L) {
                        int end, start;
                        if (buf [k] == 'u') {
                            start = k + 1;
                            end = k + 5;
                        }
                        else {
                            start = k;
                            end = k + 2;
                        }
                        if (end <= L) {
                            int x = 0;
                            for (int i = start; i != end; ++i) {
                                x = ScriptConvert.XDigitToInt (buf [i], x);
                            }
                            if (x >= 0) {
                                c = (char)x;
                                k = end;
                            }
                        }
                    }
                    buf [destination] = c;
                    ++destination;
                }
                s = new string (buf, 0, destination);
            }
            return s;
        }

        private object ImplEval (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {        
            if (cx.Version == Context.Versions.JS1_4) {
                Context.ReportWarningById ("msg.cant.call.indirect", "eval");
                return ScriptRuntime.evalSpecial (cx, scope, thisObj, args, string.Empty, 0);
            }
            throw ScriptRuntime.ConstructError ("EvalError", ScriptRuntime.GetMessage ("msg.cant.call.indirect", "eval"));
        }

        internal static bool isEvalFunction (object functionObj)
        {
            if (functionObj is IdFunctionObject) {
                IdFunctionObject function = (IdFunctionObject)functionObj;
                if (function.HasTag (FTAG) && function.MethodId == Id_eval) {
                    return true;
                }
            }
            return false;
        }



        /*
        *   ECMA 3, 15.1.3 URI Handling Function Properties
        *
        *   The following are implementations of the algorithms
        *   given in the ECMA specification for the hidden functions
        *   'Encode' and 'Decode'.
        */
        private static string encode (string str, bool fullUri)
        {
            sbyte [] utf8buf = null;
            System.Text.StringBuilder sb = null;

            for (int k = 0, length = str.Length; k != length; ++k) {
                char C = str [k];
                if (encodeUnescaped (C, fullUri)) {
                    if (sb != null) {
                        sb.Append (C);
                    }
                }
                else {
                    if (sb == null) {
                        sb = new System.Text.StringBuilder (length + 3);
                        sb.Append (str);
                        sb.Length = k;
                        utf8buf = new sbyte [6];
                    }
                    if (0xDC00 <= C && C <= 0xDFFF) {
                        throw Context.ReportRuntimeErrorById ("msg.bad.uri");
                    }
                    int V;
                    if (C < 0xD800 || 0xDBFF < C) {
                        V = C;
                    }
                    else {
                        k++;
                        if (k == length) {
                            throw Context.ReportRuntimeErrorById ("msg.bad.uri");
                        }
                        char C2 = str [k];
                        if (!(0xDC00 <= C2 && C2 <= 0xDFFF)) {
                            throw Context.ReportRuntimeErrorById ("msg.bad.uri");
                        }
                        V = ((C - 0xD800) << 10) + (C2 - 0xDC00) + 0x10000;
                    }
                    int L = oneUcs4ToUtf8Char (utf8buf, V);
                    for (int j = 0; j < L; j++) {
                        int d = 0xff & utf8buf [j];
                        sb.Append ('%');
                        sb.Append (toHexChar ((int)((uint)d >> 4)));
                        sb.Append (toHexChar (d & 0xf));
                    }
                }
            }
            return (sb == null) ? str : sb.ToString ();
        }

        private static char toHexChar (int i)
        {
            if (i >> 4 != 0)
                Context.CodeBug ();
            return (char)((i < 10) ? i + '0' : i - 10 + 'a');
        }

        private static int unHex (char c)
        {
            if ('A' <= c && c <= 'F') {
                return c - 'A' + 10;
            }
            else if ('a' <= c && c <= 'f') {
                return c - 'a' + 10;
            }
            else if ('0' <= c && c <= '9') {
                return c - '0';
            }
            else {
                return -1;
            }
        }

        private static int unHex (char c1, char c2)
        {
            int i1 = unHex (c1);
            int i2 = unHex (c2);
            if (i1 >= 0 && i2 >= 0) {
                return (i1 << 4) | i2;
            }
            return -1;
        }

        private static string decode (string str, bool fullUri)
        {
            char [] buf = null;
            int bufTop = 0;

            for (int k = 0, length = str.Length; k != length; ) {
                char C = str [k];
                if (C != '%') {
                    if (buf != null) {
                        buf [bufTop++] = C;
                    }
                    ++k;
                }
                else {
                    if (buf == null) {
                        // decode always compress so result can not be bigger then
                        // str.length()
                        buf = new char [length];
                        str.ToCharArray (0, k).CopyTo (buf, 0);
                        bufTop = k;
                    }
                    int start = k;
                    if (k + 3 > length)
                        throw Context.ReportRuntimeErrorById ("msg.bad.uri");
                    int B = unHex (str [k + 1], str [k + 2]);
                    if (B < 0)
                        throw Context.ReportRuntimeErrorById ("msg.bad.uri");
                    k += 3;
                    if ((B & 0x80) == 0) {
                        C = (char)B;
                    }
                    else {
                        // Decode UTF-8 sequence into ucs4Char and encode it into
                        // UTF-16
                        int utf8Tail, ucs4Char, minUcs4Char;
                        if ((B & 0xC0) == 0x80) {
                            // First  UTF-8 should be ouside 0x80..0xBF
                            throw Context.ReportRuntimeErrorById ("msg.bad.uri");
                        }
                        else if ((B & 0x20) == 0) {
                            utf8Tail = 1;
                            ucs4Char = B & 0x1F;
                            minUcs4Char = 0x80;
                        }
                        else if ((B & 0x10) == 0) {
                            utf8Tail = 2;
                            ucs4Char = B & 0x0F;
                            minUcs4Char = 0x800;
                        }
                        else if ((B & 0x08) == 0) {
                            utf8Tail = 3;
                            ucs4Char = B & 0x07;
                            minUcs4Char = 0x10000;
                        }
                        else if ((B & 0x04) == 0) {
                            utf8Tail = 4;
                            ucs4Char = B & 0x03;
                            minUcs4Char = 0x200000;
                        }
                        else if ((B & 0x02) == 0) {
                            utf8Tail = 5;
                            ucs4Char = B & 0x01;
                            minUcs4Char = 0x4000000;
                        }
                        else {
                            // First UTF-8 can not be 0xFF or 0xFE
                            throw Context.ReportRuntimeErrorById ("msg.bad.uri");
                        }
                        if (k + 3 * utf8Tail > length)
                            throw Context.ReportRuntimeErrorById ("msg.bad.uri");
                        for (int j = 0; j != utf8Tail; j++) {
                            if (str [k] != '%')
                                throw Context.ReportRuntimeErrorById ("msg.bad.uri");
                            B = unHex (str [k + 1], str [k + 2]);
                            if (B < 0 || (B & 0xC0) != 0x80)
                                throw Context.ReportRuntimeErrorById ("msg.bad.uri");
                            ucs4Char = (ucs4Char << 6) | (B & 0x3F);
                            k += 3;
                        }
                        // Check for overlongs and other should-not-present codes
                        if (ucs4Char < minUcs4Char || ucs4Char == 0xFFFE || ucs4Char == 0xFFFF) {
                            ucs4Char = 0xFFFD;
                        }
                        if (ucs4Char >= 0x10000) {
                            ucs4Char -= 0x10000;
                            if (ucs4Char > 0xFFFFF)
                                throw Context.ReportRuntimeErrorById ("msg.bad.uri");
                            char H = (char)(((int)((uint)ucs4Char >> 10)) + 0xD800);
                            C = (char)((ucs4Char & 0x3FF) + 0xDC00);
                            buf [bufTop++] = H;
                        }
                        else {
                            C = (char)ucs4Char;
                        }
                    }
                    if (fullUri && URI_DECODE_RESERVED.IndexOf ((char)C) >= 0) {
                        for (int x = start; x != k; x++) {
                            buf [bufTop++] = str [x];
                        }
                    }
                    else {
                        buf [bufTop++] = C;
                    }
                }
            }
            return (buf == null) ? str : new string (buf, 0, bufTop);
        }

        private static bool encodeUnescaped (char c, bool fullUri)
        {
            if (('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z') || ('0' <= c && c <= '9')) {
                return true;
            }
            if ("-_.!~*'()".IndexOf ((char)c) >= 0)
                return true;
            if (fullUri) {
                return URI_DECODE_RESERVED.IndexOf ((char)c) >= 0;
            }
            return false;
        }

        private const string URI_DECODE_RESERVED = ";/?:@&=+$,#";

        /* Convert one UCS-4 char and write it into a UTF-8 buffer, which must be
        * at least 6 bytes long.  Return the number of UTF-8 bytes of data written.
        */
        private static int oneUcs4ToUtf8Char (sbyte [] utf8Buffer, int ucs4Char)
        {
            int utf8Length = 1;

            //JS_ASSERT(ucs4Char <= 0x7FFFFFFF);
            if ((ucs4Char & ~0x7F) == 0)
                utf8Buffer [0] = (sbyte)ucs4Char;
            else {
                int i;
                int a = (int)((uint)ucs4Char >> 11);
                utf8Length = 2;
                while (a != 0) {
                    a = (int)((uint)a >> 5);
                    utf8Length++;
                }
                i = utf8Length;
                while (--i > 0) {
                    utf8Buffer [i] = (sbyte)((ucs4Char & 0x3F) | 0x80);
                    ucs4Char = (int)((uint)ucs4Char >> 6);
                }
                utf8Buffer [0] = (sbyte)(0x100 - (1 << (8 - utf8Length)) + ucs4Char);
            }
            return utf8Length;
        }

        private static readonly object FTAG = new object ();

        #region PrototypeIds
        private const int Id_decodeURI = 1;
        private const int Id_decodeURIComponent = 2;
        private const int Id_encodeURI = 3;
        private const int Id_encodeURIComponent = 4;
        private const int Id_escape = 5;
        private const int Id_eval = 6;
        private const int Id_isFinite = 7;
        private const int Id_isNaN = 8;
        private const int Id_isXMLName = 9;
        private const int Id_parseFloat = 10;
        private const int Id_parseInt = 11;
        private const int Id_unescape = 12;
        private const int Id_uneval = 13;
        private const int LAST_SCOPE_FUNCTION_ID = 13;
        private const int Id_new_CommonError = 14;
        #endregion

    }
}
