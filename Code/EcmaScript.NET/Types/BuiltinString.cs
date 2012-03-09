//------------------------------------------------------------------------------
// <license file="NativeString.cs">
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
    /// This class implements the String native object.
    /// 
    /// See ECMA 15.5.
    /// 
    /// String methods for dealing with regular expressions are
    /// ported directly from C. Latest port is from version 1.40.12.19
    /// in the JSFUN13_BRANCH.
    /// 
    /// </summary>
    internal sealed class BuiltinString : IdScriptableObject
    {

        public override string ClassName
        {
            get
            {
                return "String";
            }

        }
        override protected internal int MaxInstanceId
        {
            get
            {
                return MAX_INSTANCE_ID;
            }

        }
        internal int Length
        {
            get
            {
                return m_Value.Length;
            }

        }


        private static readonly object STRING_TAG = new object ();

        internal static void Init
            (IScriptable scope, bool zealed)
        {
            BuiltinString obj = new BuiltinString ("");
            obj.ExportAsJSClass (MAX_PROTOTYPE_ID, scope, zealed, 
                ScriptableObject.DONTENUM | ScriptableObject.READONLY | ScriptableObject.PERMANENT);
        }

        private BuiltinString (string s)
        {
            m_Value = s;
        }

        private const int Id_length = 1;
        private const int MAX_INSTANCE_ID = 1;

        protected internal override int FindInstanceIdInfo (string s)
        {
            if (s.Equals ("length")) {
                return InstanceIdInfo (DONTENUM | READONLY | PERMANENT, Id_length);
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
                return m_Value.Length;
            }
            return base.GetInstanceIdValue (id);
        }

        protected internal override void FillConstructorProperties (IdFunctionObject ctor)
        {
            AddIdFunctionProperty (ctor, STRING_TAG, ConstructorId_fromCharCode, "fromCharCode", 1);
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
                    arity = 0;
                    s = "toString";
                    break;

                case Id_toSource:
                    arity = 0;
                    s = "toSource";
                    break;

                case Id_valueOf:
                    arity = 0;
                    s = "valueOf";
                    break;

                case Id_charAt:
                    arity = 1;
                    s = "charAt";
                    break;

                case Id_charCodeAt:
                    arity = 1;
                    s = "charCodeAt";
                    break;

                case Id_indexOf:
                    arity = 1;
                    s = "indexOf";
                    break;

                case Id_lastIndexOf:
                    arity = 1;
                    s = "lastIndexOf";
                    break;

                case Id_split:
                    arity = 2;
                    s = "split";
                    break;

                case Id_substring:
                    arity = 2;
                    s = "substring";
                    break;

                case Id_toLowerCase:
                    arity = 0;
                    s = "toLowerCase";
                    break;

                case Id_toUpperCase:
                    arity = 0;
                    s = "toUpperCase";
                    break;

                case Id_substr:
                    arity = 2;
                    s = "substr";
                    break;

                case Id_concat:
                    arity = 1;
                    s = "concat";
                    break;

                case Id_slice:
                    arity = 2;
                    s = "slice";
                    break;

                case Id_bold:
                    arity = 0;
                    s = "bold";
                    break;

                case Id_italics:
                    arity = 0;
                    s = "italics";
                    break;

                case Id_fixed:
                    arity = 0;
                    s = "fixed";
                    break;

                case Id_strike:
                    arity = 0;
                    s = "strike";
                    break;

                case Id_small:
                    arity = 0;
                    s = "small";
                    break;

                case Id_big:
                    arity = 0;
                    s = "big";
                    break;

                case Id_blink:
                    arity = 0;
                    s = "blink";
                    break;

                case Id_sup:
                    arity = 0;
                    s = "sup";
                    break;

                case Id_sub:
                    arity = 0;
                    s = "sub";
                    break;

                case Id_fontsize:
                    arity = 0;
                    s = "fontsize";
                    break;

                case Id_fontcolor:
                    arity = 0;
                    s = "fontcolor";
                    break;

                case Id_link:
                    arity = 0;
                    s = "link";
                    break;

                case Id_anchor:
                    arity = 0;
                    s = "anchor";
                    break;

                case Id_equals:
                    arity = 1;
                    s = "equals";
                    break;

                case Id_equalsIgnoreCase:
                    arity = 1;
                    s = "equalsIgnoreCase";
                    break;

                case Id_match:
                    arity = 1;
                    s = "match";
                    break;

                case Id_search:
                    arity = 1;
                    s = "search";
                    break;

                case Id_replace:
                    arity = 1;
                    s = "replace";
                    break;

                default:
                    throw new ArgumentException (Convert.ToString (id));

            }
            InitPrototypeMethod (STRING_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (STRING_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;
            switch (id) {

                case ConstructorId_fromCharCode: {
                        int N = args.Length;
                        if (N < 1)
                            return "";
                        System.Text.StringBuilder sb = new System.Text.StringBuilder (N);
                        for (int i = 0; i != N; ++i) {
                            sb.Append (ScriptConvert.ToUint16 (args [i]));
                        }
                        return sb.ToString ();
                    }


                case Id_constructor: {
                        string s = (args.Length >= 1) ? ScriptConvert.ToString (args [0]) : "";
                        if (thisObj == null) {
                            // new String(val) creates a new String object.
                            return new BuiltinString (s);
                        }
                        // String(val) converts val to a string value.
                        return s;
                    }


                case Id_toString:
                case Id_valueOf:
                    // ECMA 15.5.4.2: 'the toString function is not generic.
                    return RealThis (thisObj, f).m_Value;


                case Id_toSource: {
                        string s = RealThis (thisObj, f).m_Value;
                        return "(new String(\"" + ScriptRuntime.escapeString (s) + "\"))";
                    }


                case Id_charAt:
                case Id_charCodeAt: {
                        // See ECMA 15.5.4.[4,5]
                        string target = ScriptConvert.ToString (thisObj);
                        double pos = ScriptConvert.ToInteger (args, 0);
                        if (pos < 0 || pos >= target.Length) {
                            if (id == Id_charAt)
                                return "";
                            else
                                return double.NaN;
                        }
                        char c = target [(int)pos];
                        if (id == Id_charAt)
                            return Convert.ToString (c);
                        else
                            return (int)c;
                    }


                case Id_indexOf:
                    return js_indexOf (ScriptConvert.ToString (thisObj), args);


                case Id_lastIndexOf:
                    return js_lastIndexOf (ScriptConvert.ToString (thisObj), args);


                case Id_split:
                    return ImplSplit (cx, scope, ScriptConvert.ToString (thisObj), args);


                case Id_substring:
                    return js_substring (cx, ScriptConvert.ToString (thisObj), args);


                case Id_toLowerCase:
                    // See ECMA 15.5.4.11
                    return ScriptConvert.ToString (thisObj).ToLower ();


                case Id_toUpperCase:
                    // See ECMA 15.5.4.12
                    return ScriptConvert.ToString (thisObj).ToUpper ();

                case Id_substr:
                    return js_substr (ScriptConvert.ToString (thisObj), args);


                case Id_concat:
                    return js_concat (ScriptConvert.ToString (thisObj), args);


                case Id_slice:
                    return js_slice (ScriptConvert.ToString (thisObj), args);


                case Id_bold:
                    return Tagify (thisObj, "b", null, null);


                case Id_italics:
                    return Tagify (thisObj, "i", null, null);


                case Id_fixed:
                    return Tagify (thisObj, "tt", null, null);


                case Id_strike:
                    return Tagify (thisObj, "strike", null, null);


                case Id_small:
                    return Tagify (thisObj, "small", null, null);


                case Id_big:
                    return Tagify (thisObj, "big", null, null);


                case Id_blink:
                    return Tagify (thisObj, "blink", null, null);


                case Id_sup:
                    return Tagify (thisObj, "sup", null, null);


                case Id_sub:
                    return Tagify (thisObj, "sub", null, null);


                case Id_fontsize:
                    return Tagify (thisObj, "font", "size", args);


                case Id_fontcolor:
                    return Tagify (thisObj, "font", "color", args);


                case Id_link:
                    return Tagify (thisObj, "a", "href", args);


                case Id_anchor:
                    return Tagify (thisObj, "a", "name", args);


                case Id_equals:
                case Id_equalsIgnoreCase: {
                        string s1 = ScriptConvert.ToString (thisObj);
                        string s2 = ScriptConvert.ToString (args, 0);
                        return (id == Id_equals) ? s1.Equals (s2) : s1.ToUpper ().Equals (s2.ToUpper ());
                    }


                case Id_match:
                case Id_search:
                case Id_replace: {
                        RegExpActions actionType;
                        if (id == Id_match) {
                            actionType = EcmaScript.NET.RegExpActions.Match;
                        }
                        else if (id == Id_search) {
                            actionType = EcmaScript.NET.RegExpActions.Search;
                        }
                        else {
                            actionType = EcmaScript.NET.RegExpActions.Replace;
                        }
                        return cx.regExpProxy.Perform (cx, scope, thisObj, args, actionType);
                    }
            }
            throw new ArgumentException (Convert.ToString (id));
        }

        private static BuiltinString RealThis (IScriptable thisObj, IdFunctionObject f)
        {
            if (!(thisObj is BuiltinString))
                throw IncompatibleCallError (f);
            return (BuiltinString)thisObj;
        }

        /// <summary>
        /// HTML composition aids.
        /// </summary>
        private static string Tagify (object thisObj, string tag, string attribute, object [] args)
        {
            string str = ScriptConvert.ToString (thisObj);
            System.Text.StringBuilder result = new System.Text.StringBuilder ();
            result.Append ('<');
            result.Append (tag);
            if (attribute != null) {
                result.Append (' ');
                result.Append (attribute);
                result.Append ("=\"");
                result.Append (ScriptConvert.ToString (args, 0));
                result.Append ('"');
            }
            result.Append ('>');
            result.Append (str);
            result.Append ("</");
            result.Append (tag);
            result.Append ('>');
            return result.ToString ();
        }

        public override string ToString ()
        {
            return m_Value;
        }

        /// <summary>
        /// Make array-style property lookup work for strings. 
        /// 
        /// TODO: is this ECMA?  A version check is probably needed. In js too.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="start"></param>
        /// <returns></returns>
        public override object Get (int index, IScriptable start)
        {
            if (0 <= index && index < m_Value.Length) {
                return m_Value.Substring (index, (index + 1) - (index));
            }
            return base.Get (index, start);
        }

        public override object Put (int index, IScriptable start, object value)
        {
            if (0 <= index && index < m_Value.Length) {
                return Undefined.Value;
            }
            return base.Put (index, start, value);
        }

        /*
        *
        * See ECMA 15.5.4.6.  Uses Java String.indexOf()
        * OPT to add - BMH searching from jsstr.c.
        */
        private static int js_indexOf (string target, object [] args)
        {
            string search = ScriptConvert.ToString (args, 0);
            double begin = ScriptConvert.ToInteger (args, 1);

            if (begin > target.Length) {
                return -1;
            }
            else {
                if (begin < 0)
                    begin = 0;
                return target.IndexOf (search, (int)begin);
            }
        }

        /*
        *
        * See ECMA 15.5.4.7
        *
        */
        private static int js_lastIndexOf (string target, object [] args)
        {
            string search = ScriptConvert.ToString (args, 0);
            double end = ScriptConvert.ToNumber (args, 1);

            if (double.IsNaN (end) || end > target.Length)
                end = target.Length;
            else if (end < 0)
                end = 0;

            return lastIndexOf (
                target.ToCharArray (), 0, target.Length, search.ToCharArray (), 0, search.Length, (int)end);
        }

        static int lastIndexOf (char [] source, int sourceOffset, int sourceCount,
            char [] target, int targetOffset, int targetCount,
            int fromIndex)
        {
            /*
             * Check arguments; return immediately where possible. For
             * consistency, don't check for null str.
             */
            int rightIndex = sourceCount - targetCount;
            if (fromIndex < 0) {
                return -1;
            }
            if (fromIndex > rightIndex) {
                fromIndex = rightIndex;
            }
            /* Empty string always matches. */
            if (targetCount == 0) {
                return fromIndex;
            }

            int strLastIndex = targetOffset + targetCount - 1;
            char strLastChar = target [strLastIndex];
            int min = sourceOffset + targetCount - 1;
            int i = min + fromIndex;

        startSearchForLastChar:
            while (true) {
                while (i >= min && source [i] != strLastChar) {
                    i--;
                }
                if (i < min) {
                    return -1;
                }
                int j = i - 1;
                int start = j - (targetCount - 1);
                int k = strLastIndex - 1;

                while (j > start) {
                    if (source [j--] != target [k--]) {
                        i--;
                        goto startSearchForLastChar;
                    }
                }
                return start - sourceOffset + 1;
            }
        }

        /*
        * Used by js_split to find the next split point in target,
        * starting at offset ip and looking either for the given
        * separator substring, or for the next re match.  ip and
        * matchlen must be reference variables (assumed to be arrays of
        * length 1) so they can be updated in the leading whitespace or
        * re case.
        *
        * Return -1 on end of string, >= 0 for a valid index of the next
        * separator occurrence if found, or the string length if no
        * separator is found.
        */
        private static int find_split (Context cx, IScriptable scope, string target, string separator, Context.Versions version, RegExpProxy reProxy, IScriptable re, int [] ip, int [] matchlen, bool [] matched, string [] [] parensp)
        {
            int i = ip [0];
            int length = target.Length;

            /*
            * Perl4 special case for str.split(' '), only if the user has selected
            * JavaScript1.2 explicitly.  Split on whitespace, and skip leading w/s.
            * Strange but true, apparently modeled after awk.
            */
            if (version == Context.Versions.JS1_2 && re == null && separator.Length == 1 && separator [0] == ' ') {
                /* Skip leading whitespace if at front of str. */
                if (i == 0) {
                    while (i < length && char.IsWhiteSpace (target [i]))
                        i++;
                    ip [0] = i;
                }

                /* Don't delimit whitespace at end of string. */
                if (i == length)
                    return -1;

                /* Skip over the non-whitespace chars. */
                while (i < length && !char.IsWhiteSpace (target [i]))
                    i++;

                /* Now skip the next run of whitespace. */
                int j = i;
                while (j < length && char.IsWhiteSpace (target [j]))
                    j++;

                /* Update matchlen to count delimiter chars. */
                matchlen [0] = j - i;
                return i;
            }

            /*
            * Stop if past end of string.  If at end of string, we will
            * return target length, so that
            *
            *  "ab,".split(',') => new Array("ab", "")
            *
            * and the resulting array converts back to the string "ab,"
            * for symmetry.  NB: This differs from perl, which drops the
            * trailing empty substring if the LIMIT argument is omitted.
            */
            if (i > length)
                return -1;

            /*
            * Match a regular expression against the separator at or
            * above index i.  Return -1 at end of string instead of
            * trying for a match, so we don't get stuck in a loop.
            */
            if (re != null) {
                return reProxy.FindSplit (cx, scope, target, separator, re, ip, matchlen, matched, parensp);
            }

            /*
            * Deviate from ECMA by never splitting an empty string by any separator
            * string into a non-empty array (an array of length 1 that contains the
            * empty string).
            */
            if (version != Context.Versions.Default && version < Context.Versions.JS1_3 && length == 0)
                return -1;

            /*
            * Special case: if sep is the empty string, split str into
            * one character substrings.  Let our caller worry about
            * whether to split once at end of string into an empty
            * substring.
            *
            * For 1.2 compatibility, at the end of the string, we return the length as
            * the result, and set the separator length to 1 -- this allows the caller
            * to include an additional null string at the end of the substring list.
            */
            if (separator.Length == 0) {
                if (version == Context.Versions.JS1_2) {
                    if (i == length) {
                        matchlen [0] = 1;
                        return i;
                    }
                    return i + 1;
                }
                return (i == length) ? -1 : i + 1;
            }

            /* Punt to j.l.s.indexOf; return target length if seperator is
            * not found.
            */
            if (ip [0] >= length)
                return length;

            i = target.IndexOf (separator, ip [0]);

            return (i != -1) ? i : length;
        }

        /*
        * See ECMA 15.5.4.8.  Modified to match JS 1.2 - optionally takes
        * a limit argument and accepts a regular expression as the split
        * argument.
        */
        private static object ImplSplit (Context cx, IScriptable scope, string target, object [] args)
        {
            // create an empty Array to return;
            IScriptable top = GetTopLevelScope (scope);
            IScriptable result = ScriptRuntime.NewObject (cx, top, "Array", null);

            // return an array consisting of the target if no separator given
            // don't check against undefined, because we want
            // 'fooundefinedbar'.split(void 0) to split to ['foo', 'bar']
            if (args.Length < 1) {
                result.Put (0, result, target);
                return result;
            }

            // Use the second argument as the split limit, if given.
            bool limited = (args.Length > 1) && (args [1] != Undefined.Value);
            long limit = 0; // Initialize to avoid warning.
            if (limited) {
                /* Clamp limit between 0 and 1 + string length. */
                limit = ScriptConvert.ToUint32 (args [1]);
                if (limit > target.Length)
                    limit = 1 + target.Length;
            }

            string separator = null;
            int [] matchlen = new int [1];
            IScriptable re = null;
            RegExpProxy reProxy = null;
            if (args [0] is IScriptable) {
                reProxy = cx.RegExpProxy;
                if (reProxy != null) {
                    IScriptable test = (IScriptable)args [0];
                    if (reProxy.IsRegExp (test)) {
                        re = test;
                    }
                }
            }
            if (re == null) {
                separator = ScriptConvert.ToString (args [0]);
                matchlen [0] = separator.Length;
            }

            // split target with separator or re
            int [] ip = new int [] { 0 };
            int match;
            int len = 0;
            bool [] matched = new bool [] { false };
            string [] [] parens = new string [] [] { null };
            Context.Versions version = cx.Version;
            while ((match = find_split (cx, scope, target, separator, version, reProxy, re, ip, matchlen, matched, parens)) >= 0) {
                if ((limited && len >= limit) || (match > target.Length))
                    break;

                string substr;
                if (target.Length == 0)
                    substr = target;
                else
                    substr = target.Substring (ip [0], (match) - (ip [0]));

                result.Put (len, result, substr);
                len++;
                /*
                * Imitate perl's feature of including parenthesized substrings
                * that matched part of the delimiter in the new array, after the
                * split substring that was delimited.
                */
                // CB, 02.01.2007: Don't do this, causes bug #287630
                // https://bugzilla.mozilla.org/show_bug.cgi?query_format=specific&order=relevance+desc&bug_status=__open__&id=287630
                /*
                if (re != null && matched [0] == true) {
                    int size = parens [0].Length;
                    for (int num = 0; num < size; num++) {
                        if (limited && len >= limit)
                            break;
                        result.Put (len, result, parens [0] [num]);
                        len++;
                    }
                    matched [0] = false;
                }
                 */
                ip [0] = match + matchlen [0];

                if (version < Context.Versions.JS1_3 && version != Context.Versions.Default) {
                    /*
                    * Deviate from ECMA to imitate Perl, which omits a final
                    * split unless a limit argument is given and big enough.
                    */
                    if (!limited && ip [0] == target.Length)
                        break;
                }
            }
            return result;
        }

        /*
        * See ECMA 15.5.4.15
        */
        private static string js_substring (Context cx, string target, object [] args)
        {
            int length = target.Length;
            double start = ScriptConvert.ToInteger (args, 0);
            double end;

            if (start < 0)
                start = 0;
            else if (start > length)
                start = length;

            if (args.Length <= 1 || args [1] == Undefined.Value) {
                end = length;
            }
            else {
                end = ScriptConvert.ToInteger (args [1]);
                if (end < 0)
                    end = 0;
                else if (end > length)
                    end = length;

                // swap if end < start
                if (end < start) {
                    if (cx.Version != Context.Versions.JS1_2) {
                        double temp = start;
                        start = end;
                        end = temp;
                    }
                    else {
                        // Emulate old JDK1.0 java.lang.String.substring()
                        end = start;
                    }
                }
            }
            return target.Substring ((int)start, ((int)end) - ((int)start));
        }

        /*
        * Non-ECMA methods.
        */
        private static string js_substr (string target, object [] args)
        {
            if (args.Length < 1)
                return target;

            double begin = ScriptConvert.ToInteger (args [0]);
            double end;
            int length = target.Length;

            if (begin < 0) {
                begin += length;
                if (begin < 0)
                    begin = 0;
            }
            else if (begin > length) {
                begin = length;
            }

            if (args.Length == 1) {
                end = length;
            }
            else {
                end = ScriptConvert.ToInteger (args [1]);
                if (end < 0)
                    end = 0;
                end += begin;
                if (end > length)
                    end = length;
            }

            return target.Substring ((int)begin, ((int)end) - ((int)begin));
        }

        /*
        * Python-esque sequence operations.
        */
        private static string js_concat (string target, object [] args)
        {
            int N = args.Length;
            if (N == 0) {
                return target;
            }
            else if (N == 1) {
                string arg = ScriptConvert.ToString (args [0]);
                return string.Concat (target, arg);
            }

            // Find total capacity for the final string to avoid unnecessary
            // re-allocations in StringBuffer
            int size = target.Length;
            string [] argsAsStrings = new string [N];
            for (int i = 0; i != N; ++i) {
                string s = ScriptConvert.ToString (args [i]);
                argsAsStrings [i] = s;
                size += s.Length;
            }

            System.Text.StringBuilder result = new System.Text.StringBuilder (size);
            result.Append (target);
            for (int i = 0; i != N; ++i) {
                result.Append (argsAsStrings [i]);
            }
            return result.ToString ();
        }

        private static string js_slice (string target, object [] args)
        {
            if (args.Length != 0) {
                double begin = ScriptConvert.ToInteger (args [0]);
                double end;
                int length = target.Length;
                if (begin < 0) {
                    begin += length;
                    if (begin < 0)
                        begin = 0;
                }
                else if (begin > length) {
                    begin = length;
                }

                if (args.Length == 1) {
                    end = length;
                }
                else {
                    end = ScriptConvert.ToInteger (args [1]);
                    if (end < 0) {
                        end += length;
                        if (end < 0)
                            end = 0;
                    }
                    else if (end > length) {
                        end = length;
                    }
                    if (end < begin)
                        end = begin;
                }

                return target.Substring ((int)begin, ((int)end) - ((int)begin));
            }
            return target;
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
                    case 3:
                        c = s [2];
                        if (c == 'b') { if (s [0] == 's' && s [1] == 'u') { id = Id_sub; goto EL0; } }
                        else if (c == 'g') { if (s [0] == 'b' && s [1] == 'i') { id = Id_big; goto EL0; } }
                        else if (c == 'p') { if (s [0] == 's' && s [1] == 'u') { id = Id_sup; goto EL0; } }
                        break;
                    case 4:
                        c = s [0];
                        if (c == 'b') { X = "bold"; id = Id_bold; }
                        else if (c == 'l') { X = "link"; id = Id_link; }
                        break;
                    case 5:
                        switch (s [4]) {
                            case 'd':
                                X = "fixed";
                                id = Id_fixed;
                                break;
                            case 'e':
                                X = "slice";
                                id = Id_slice;
                                break;
                            case 'h':
                                X = "match";
                                id = Id_match;
                                break;
                            case 'k':
                                X = "blink";
                                id = Id_blink;
                                break;
                            case 'l':
                                X = "small";
                                id = Id_small;
                                break;
                            case 't':
                                X = "split";
                                id = Id_split;
                                break;
                        }
                        break;
                    case 6:
                        switch (s [1]) {
                            case 'e':
                                X = "search";
                                id = Id_search;
                                break;
                            case 'h':
                                X = "charAt";
                                id = Id_charAt;
                                break;
                            case 'n':
                                X = "anchor";
                                id = Id_anchor;
                                break;
                            case 'o':
                                X = "concat";
                                id = Id_concat;
                                break;
                            case 'q':
                                X = "equals";
                                id = Id_equals;
                                break;
                            case 't':
                                X = "strike";
                                id = Id_strike;
                                break;
                            case 'u':
                                X = "substr";
                                id = Id_substr;
                                break;
                        }
                        break;
                    case 7:
                        switch (s [1]) {
                            case 'a':
                                X = "valueOf";
                                id = Id_valueOf;
                                break;
                            case 'e':
                                X = "replace";
                                id = Id_replace;
                                break;
                            case 'n':
                                X = "indexOf";
                                id = Id_indexOf;
                                break;
                            case 't':
                                X = "italics";
                                id = Id_italics;
                                break;
                        }
                        break;
                    case 8:
                        c = s [4];
                        if (c == 'r') { X = "toString"; id = Id_toString; }
                        else if (c == 's') { X = "fontsize"; id = Id_fontsize; }
                        else if (c == 'u') { X = "toSource"; id = Id_toSource; }
                        break;
                    case 9:
                        c = s [0];
                        if (c == 'f') { X = "fontcolor"; id = Id_fontcolor; }
                        else if (c == 's') { X = "substring"; id = Id_substring; }
                        break;
                    case 10:
                        X = "charCodeAt";
                        id = Id_charCodeAt;
                        break;
                    case 11:
                        switch (s [2]) {
                            case 'L':
                                X = "toLowerCase";
                                id = Id_toLowerCase;
                                break;
                            case 'U':
                                X = "toUpperCase";
                                id = Id_toUpperCase;
                                break;
                            case 'n':
                                X = "constructor";
                                id = Id_constructor;
                                break;
                            case 's':
                                X = "lastIndexOf";
                                id = Id_lastIndexOf;
                                break;
                        }
                        break;
                    case 16:
                        X = "equalsIgnoreCase";
                        id = Id_equalsIgnoreCase;
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
        private const int ConstructorId_fromCharCode = -1;
        private const int Id_constructor = 1;
        private const int Id_toString = 2;
        private const int Id_toSource = 3;
        private const int Id_valueOf = 4;
        private const int Id_charAt = 5;
        private const int Id_charCodeAt = 6;
        private const int Id_indexOf = 7;
        private const int Id_lastIndexOf = 8;
        private const int Id_split = 9;
        private const int Id_substring = 10;
        private const int Id_toLowerCase = 11;
        private const int Id_toUpperCase = 12;
        private const int Id_substr = 13;
        private const int Id_concat = 14;
        private const int Id_slice = 15;
        private const int Id_bold = 16;
        private const int Id_italics = 17;
        private const int Id_fixed = 18;
        private const int Id_strike = 19;
        private const int Id_small = 20;
        private const int Id_big = 21;
        private const int Id_blink = 22;
        private const int Id_sup = 23;
        private const int Id_sub = 24;
        private const int Id_fontsize = 25;
        private const int Id_fontcolor = 26;
        private const int Id_link = 27;
        private const int Id_anchor = 28;
        private const int Id_equals = 29;
        private const int Id_equalsIgnoreCase = 30;
        private const int Id_match = 31;
        private const int Id_search = 32;
        private const int Id_replace = 33;
        private const int MAX_PROTOTYPE_ID = 33;
        #endregion

        private string m_Value;


    }
}
