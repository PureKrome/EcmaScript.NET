//------------------------------------------------------------------------------
// <license file="NativeRegExp.cs">
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

using EcmaScript.NET;

namespace EcmaScript.NET.Types.RegExp
{

    /// <summary> This class implements the RegExp native object.
    /// 
    /// Revision History:
    /// Implementation in C by Brendan Eich
    /// Initial port to Java by Norris Boyd from jsregexp.c version 1.36
    /// Merged up to version 1.38, which included Unicode support.
    /// Merged bug fixes in version 1.39.
    /// Merged JSFUN13_BRANCH changes up to 1.32.2.13
    /// 
    /// </summary>				
    public class BuiltinRegExp : IdScriptableObject, IFunction
    {
        override public string ClassName
        {
            get
            {
                return "RegExp";
            }

        }
        virtual internal int Flags
        {
            get
            {
                return re.flags;
            }

        }
        override protected internal int MaxInstanceId
        {
            get
            {
                return MAX_INSTANCE_ID;
            }

        }


        private static readonly object REGEXP_TAG = new object ();

        public const int JSREG_GLOB = 0x1; // 'g' flag: global
        public const int JSREG_FOLD = 0x2; // 'i' flag: fold
        public const int JSREG_MULTILINE = 0x4; // 'm' flag: multiline

        //type of match to perform
        public const int TEST = 0;
        public const int MATCH = 1;
        public const int PREFIX = 2;

        private static bool debug = false; // TODO: make preprocessor directive

        static string DebugNameOp (sbyte op)
        {
            foreach (System.Reflection.FieldInfo fi in typeof (BuiltinRegExp).GetFields
            ()) {
                if (fi.Name.StartsWith ("REOP_")) {
                    sbyte val = (sbyte)fi.GetValue (typeof (BuiltinRegExp));
                    if (val == op)
                        return fi.Name;
                }

            }
            return "<undefined>";
        }

        public const sbyte REOP_EMPTY = 0; /* match rest of input against rest of r.e. */
        public const sbyte REOP_ALT = 1; /* alternative subexpressions in kid and next */
        public const sbyte REOP_BOL = 2; /* beginning of input (or line if multiline) */
        public const sbyte REOP_EOL = 3; /* end of input (or line if multiline) */
        public const sbyte REOP_WBDRY = 4; /* match "" at word boundary */
        public const sbyte REOP_WNONBDRY = 5; /* match "" at word non-boundary */
        public const sbyte REOP_QUANT = 6; /* quantified atom: atom{1,2} */
        public const sbyte REOP_STAR = 7; /* zero or more occurrences of kid */
        public const sbyte REOP_PLUS = 8; /* one or more occurrences of kid */
        public const sbyte REOP_OPT = 9; /* optional subexpression in kid */
        public const sbyte REOP_LPAREN = 10; /* left paren bytecode: kid is u.num'th sub-regexp */
        public const sbyte REOP_RPAREN = 11; /* right paren bytecode */
        public const sbyte REOP_DOT = 12; /* stands for any character */
        public const sbyte REOP_CCLASS = 13; /* character class: [a-f] */
        public const sbyte REOP_DIGIT = 14; /* match a digit char: [0-9] */
        public const sbyte REOP_NONDIGIT = 15; /* match a non-digit char: [^0-9] */
        public const sbyte REOP_ALNUM = 16; /* match an alphanumeric char: [0-9a-z_A-Z] */
        public const sbyte REOP_NONALNUM = 17; /* match a non-alphanumeric char: [^0-9a-z_A-Z] */
        public const sbyte REOP_SPACE = 18; /* match a whitespace char */
        public const sbyte REOP_NONSPACE = 19; /* match a non-whitespace char */
        public const sbyte REOP_BACKREF = 20; /* back-reference (e.g., \1) to a parenthetical */
        public const sbyte REOP_FLAT = 21; /* match a flat string */
        public const sbyte REOP_FLAT1 = 22; /* match a single char */
        public const sbyte REOP_JUMP = 23; /* for deoptimized closure loops */
        public const sbyte REOP_DOTSTAR = 24; /* optimize .* to use a single opcode */
        public const sbyte REOP_ANCHOR = 25; /* like .* but skips left context to unanchored r.e. */
        public const sbyte REOP_EOLONLY = 26; /* $ not preceded by any pattern */
        public const sbyte REOP_UCFLAT = 27; /* flat Unicode string; len immediate counts chars */
        public const sbyte REOP_UCFLAT1 = 28; /* single Unicode char */
        public const sbyte REOP_UCCLASS = 29; /* Unicode character class, vector of chars to match */
        public const sbyte REOP_NUCCLASS = 30; /* negated Unicode character class */
        public const sbyte REOP_BACKREFi = 31; /* case-independent REOP_BACKREF */
        public const sbyte REOP_FLATi = 32; /* case-independent REOP_FLAT */
        public const sbyte REOP_FLAT1i = 33; /* case-independent REOP_FLAT1 */
        public const sbyte REOP_UCFLATi = 34; /* case-independent REOP_UCFLAT */
        public const sbyte REOP_UCFLAT1i = 35; /* case-independent REOP_UCFLAT1 */
        public const sbyte REOP_ANCHOR1 = 36; /* first-char discriminating REOP_ANCHOR */
        public const sbyte REOP_NCCLASS = 37; /* negated 8-bit character class */
        public const sbyte REOP_DOTSTARMIN = 38; /* ungreedy version of REOP_DOTSTAR */
        public const sbyte REOP_LPARENNON = 39; /* non-capturing version of REOP_LPAREN */
        public const sbyte REOP_RPARENNON = 40; /* non-capturing version of REOP_RPAREN */
        public const sbyte REOP_ASSERT = 41; /* zero width positive lookahead assertion */
        public const sbyte REOP_ASSERT_NOT = 42; /* zero width negative lookahead assertion */
        public const sbyte REOP_ASSERTTEST = 43; /* sentinel at end of assertion child */
        public const sbyte REOP_ASSERTNOTTEST = 44; /* sentinel at end of !assertion child */
        public const sbyte REOP_MINIMALSTAR = 45; /* non-greedy version of * */
        public const sbyte REOP_MINIMALPLUS = 46; /* non-greedy version of + */
        public const sbyte REOP_MINIMALOPT = 47; /* non-greedy version of ? */
        public const sbyte REOP_MINIMALQUANT = 48; /* non-greedy version of {} */
        public const sbyte REOP_ENDCHILD = 49; /* sentinel at end of quantifier child */
        public const sbyte REOP_CLASS = 50; /* character class with index */
        public const sbyte REOP_REPEAT = 51; /* directs execution of greedy quantifier */
        public const sbyte REOP_MINIMALREPEAT = 52; /* directs execution of non-greedy quantifier */
        public const sbyte REOP_END = 53;


        public static void Init (IScriptable scope, bool zealed)
        {

            BuiltinRegExp proto = new BuiltinRegExp ();
            proto.re = (RECompiled)compileRE ("", null, false);
            proto.ActivatePrototypeMap (MAX_PROTOTYPE_ID);
            proto.ParentScope = scope;
            proto.SetPrototype (GetObjectPrototype (scope));

            BuiltinRegExpCtor ctor = new BuiltinRegExpCtor ();

            ScriptRuntime.setFunctionProtoAndParent (ctor, scope);

            ctor.ImmunePrototypeProperty = proto;

            if (zealed) {
                proto.SealObject ();
                ctor.SealObject ();
            }

            DefineProperty (scope, "RegExp", ctor, ScriptableObject.DONTENUM);
        }

        internal BuiltinRegExp (IScriptable scope, object regexpCompiled)
        {
            this.re = (RECompiled)regexpCompiled;
            this.lastIndex = 0;
            ScriptRuntime.setObjectProtoAndParent (this, scope);
        }

        public virtual object Call (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            return execSub (cx, scope, args, MATCH);
        }

        public virtual IScriptable Construct (Context cx, IScriptable scope, object [] args)
        {
            return (IScriptable)execSub (cx, scope, args, MATCH);
        }

        internal virtual IScriptable compile (Context cx, IScriptable scope, object [] args)
        {
            if (args.Length > 0 && args [0] is BuiltinRegExp) {
                if (args.Length > 1 && args [1] != Undefined.Value) {
                    // report error
                    throw ScriptRuntime.TypeErrorById ("msg.bad.regexp.compile");
                }
                BuiltinRegExp thatObj = (BuiltinRegExp)args [0];
                this.re = thatObj.re;
                this.lastIndex = thatObj.lastIndex;
                return this;
            }
            string s = args.Length == 0 ? "" : ScriptConvert.ToString (args [0]);
            string global = args.Length > 1 && args [1] != Undefined.Value ? ScriptConvert.ToString (args [1]) : null;
            this.re = (RECompiled)compileRE (s, global, false);
            this.lastIndex = 0;
            return this;
        }

        public override string ToString ()
        {
            System.Text.StringBuilder buf = new System.Text.StringBuilder ();
            buf.Append ('/');
            if (re.source.Length != 0) {
                buf.Append (re.source);
            }
            else {
                // See bugzilla 226045
                buf.Append ("(?:)");
            }
            buf.Append ('/');
            if ((re.flags & JSREG_GLOB) != 0)
                buf.Append ('g');
            if ((re.flags & JSREG_FOLD) != 0)
                buf.Append ('i');
            if ((re.flags & JSREG_MULTILINE) != 0)
                buf.Append ('m');
            return buf.ToString ();
        }

        internal BuiltinRegExp ()
        {
        }

        private static RegExpImpl getImpl (Context cx)
        {
            return (RegExpImpl)cx.RegExpProxy;
        }

        private object execSub (Context cx, IScriptable scopeObj, object [] args, int matchType)
        {
            RegExpImpl reImpl = getImpl (cx);
            string str;
            if (args.Length == 0) {
                str = reImpl.input;
                if (str == null) {
                    reportError ("msg.no.re.input.for", ToString ());
                }
            }
            else {
                str = ScriptConvert.ToString (args [0]);
            }
            double d = ((re.flags & JSREG_GLOB) != 0) ? lastIndex : 0;

            object rval;
            if (d < 0 || str.Length < d) {
                lastIndex = 0;
                rval = null;
            }
            else {

                int [] indexp = new int [] { (int)d };
                rval = executeRegExp (cx, scopeObj, reImpl, str, indexp, matchType);
                if ((re.flags & JSREG_GLOB) != 0) {
                    lastIndex = (rval == null || rval == Undefined.Value) ? 0 : indexp [0];
                }
            }
            return rval;
        }

        internal static object compileRE (string str, string global, bool flat)
        {
            RECompiled regexp = new RECompiled ();
            regexp.source = str.ToCharArray ();
            int length = str.Length;

            int flags = 0;
            if (global != null) {
                for (int i = 0; i < global.Length; i++) {
                    char c = global [i];
                    if (c == 'g') {
                        flags |= JSREG_GLOB;
                    }
                    else if (c == 'i') {
                        flags |= JSREG_FOLD;
                    }
                    else if (c == 'm') {
                        flags |= JSREG_MULTILINE;
                    }
                    else {
                        reportError ("msg.invalid.re.flag", Convert.ToString (c));
                    }
                }
            }
            regexp.flags = flags;

            CompilerState state = new CompilerState (regexp.source, length, flags);
            if (flat && length > 0) {
                if (debug) {
                    System.Console.Out.WriteLine ("flat = \"" + str + "\"");
                }
                state.result = new RENode (REOP_FLAT);
                state.result.chr = state.cpbegin [0];
                state.result.length = length;
                state.result.flatIndex = 0;
                state.progLength += 5;
            }
            else if (!parseDisjunction (state))
                return null;

            regexp.program = new sbyte [state.progLength + 1];
            if (state.classCount != 0) {
                regexp.classList = new RECharSet [state.classCount];
                regexp.classCount = state.classCount;
            }
            int endPC = emitREBytecode (state, regexp, 0, state.result);
            regexp.program [endPC++] = REOP_END;

            if (debug) {
                System.Console.Out.WriteLine ("Prog. length = " + endPC);
                for (int i = 0; i < endPC; i++) {
                    System.Console.Out.Write (DebugNameOp ((sbyte)regexp.program [i]));
                    if (i < (endPC - 1))
                        System.Console.Out.Write (", ");
                }
                System.Console.Out.WriteLine ();
            }
            regexp.parenCount = state.parenCount;

            // If re starts with literal, init anchorCh accordingly
            switch (regexp.program [0]) {

                case REOP_UCFLAT1:
                case REOP_UCFLAT1i:
                    regexp.anchorCh = (char)getIndex (regexp.program, 1);
                    break;

                case REOP_FLAT1:
                case REOP_FLAT1i:
                    regexp.anchorCh = (char)(regexp.program [1] & 0xFF);
                    break;

                case REOP_FLAT:
                case REOP_FLATi:
                    int k = getIndex (regexp.program, 1);
                    regexp.anchorCh = regexp.source [k];
                    break;
            }

            if (debug) {
                if (regexp.anchorCh >= 0) {
                    System.Console.Out.WriteLine ("Anchor ch = '" + (char)regexp.anchorCh + "'");
                }
            }
            return regexp;
        }

        internal static bool isDigit (char c)
        {
            return '0' <= c && c <= '9';
        }

        private static bool isWord (char c)
        {
            return char.IsLetter (c) || isDigit (c) || c == '_';
        }

        private static bool isLineTerm (char c)
        {
            return ScriptRuntime.isJSLineTerminator (c);
        }

        private static bool isREWhiteSpace (int c)
        {
            return (c == '\u0020' || c == '\u0009' || c == '\n' || c == '\r' || c == 0x2028 || c == 0x2029 || c == '\u000C' || c == '\u000B' || c == '\u00A0' || (int)char.GetUnicodeCategory ((char)c) == (sbyte)System.Globalization.UnicodeCategory.SpaceSeparator);
        }

        /*
        *
        * 1. If IgnoreCase is false, return ch.
        * 2. Let u be ch converted to upper case as if by calling
        *    String.prototype.toUpperCase on the one-character string ch.
        * 3. If u does not consist of a single character, return ch.
        * 4. Let cu be u's character.
        * 5. If ch's code point value is greater than or equal to decimal 128 and cu's
        *    code point value is less than decimal 128, then return ch.
        * 6. Return cu.
        */
        private static char upcase (char ch)
        {
            if (ch < 128) {
                if ('a' <= ch && ch <= 'z') {
                    return (char)(ch + ('A' - 'a'));
                }
                return ch;
            }
            char cu = char.ToUpper (ch);
            if ((ch >= 128) && (cu < 128))
                return ch;
            return cu;
        }

        private static char downcase (char ch)
        {
            if (ch < 128) {
                if ('A' <= ch && ch <= 'Z') {
                    return (char)(ch + ('a' - 'A'));
                }
                return ch;
            }
            char cl = char.ToLower (ch);
            if ((ch >= 128) && (cl < 128))
                return ch;
            return cl;
        }

        /*
        * Validates and converts hex ascii value.
        */
        private static int toASCIIHexDigit (int c)
        {
            if (c < '0')
                return -1;
            if (c <= '9') {
                return c - '0';
            }
            c |= 0x20;
            if ('a' <= c && c <= 'f') {
                return c - 'a' + 10;
            }
            return -1;
        }

        /*
        * Top-down regular expression grammar, based closely on Perl4.
        *
        *  regexp:     altern                  A regular expression is one or more
        *              altern '|' regexp       alternatives separated by vertical bar.
        */
        private static bool parseDisjunction (CompilerState state)
        {
            using (Helpers.StackOverflowVerifier sov = new Helpers.StackOverflowVerifier (1024)) {
                if (!parseAlternative (state))
                    return false;
                char [] source = state.cpbegin;
                int index = state.cp;
                if (index != source.Length && source [index] == '|') {
                    RENode altResult;
                    ++state.cp;
                    altResult = new RENode (REOP_ALT);
                    altResult.kid = state.result;
                    if (!parseDisjunction (state))
                        return false;
                    altResult.kid2 = state.result;
                    state.result = altResult;
                    /* ALT, <next>, ..., JUMP, <end> ... JUMP <end> */
                    state.progLength += 9;
                }
                return true;
            }
        }

        /*
        *  altern:     item                    An alternative is one or more items,
        *              item altern             concatenated together.
        */
        private static bool parseAlternative (CompilerState state)
        {
            RENode headTerm = null;
            RENode tailTerm = null;
            char [] source = state.cpbegin;
            while (true) {
                if (state.cp == state.cpend || source [state.cp] == '|' || (state.parenNesting != 0 && source [state.cp] == ')')) {
                    if (headTerm == null) {
                        state.result = new RENode (REOP_EMPTY);
                    }
                    else
                        state.result = headTerm;
                    return true;
                }
                if (!parseTerm (state))
                    return false;
                if (headTerm == null)
                    headTerm = state.result;
                else {
                    if (tailTerm == null) {
                        headTerm.next = state.result;
                        tailTerm = state.result;
                        while (tailTerm.next != null)
                            tailTerm = tailTerm.next;
                    }
                    else {
                        tailTerm.next = state.result;
                        tailTerm = tailTerm.next;
                        while (tailTerm.next != null)
                            tailTerm = tailTerm.next;
                    }
                }
            }
        }

        /* calculate the total size of the bitmap required for a class expression */
        private static bool calculateBitmapSize (CompilerState state, RENode target, char [] src, int index, int end)
        {
            char rangeStart = (char)(0);
            char c;
            int n;
            int nDigits;
            int i;
            int max = 0;
            bool inRange = false;

            target.bmsize = 0;

            if (index == end)
                return true;

            if (src [index] == '^')
                ++index;

            while (index != end) {
                int localMax = 0;
                nDigits = 2;
                switch (src [index]) {

                    case '\\':
                        ++index;
                        c = src [index++];
                        switch (c) {

                            case 'b':
                                localMax = 0x8;
                                break;

                            case 'f':
                                localMax = 0xC;
                                break;

                            case 'n':
                                localMax = 0xA;
                                break;

                            case 'r':
                                localMax = 0xD;
                                break;

                            case 't':
                                localMax = 0x9;
                                break;

                            case 'v':
                                localMax = 0xB;
                                break;

                            case 'c':
                                if (((index + 1) < end) && char.IsLetter (src [index + 1]))
                                    localMax = (char)(src [index++] & 0x1F);
                                else
                                    localMax = '\\';
                                break;

                            case 'u':
                                nDigits += 2;
                                // fall thru...
                                goto case 'x';

                            case 'x':
                                n = 0;
                                for (i = 0; (i < nDigits) && (index < end); i++) {
                                    c = src [index++];
                                    n = ScriptConvert.XDigitToInt (c, n);
                                    if (n < 0) {
                                        // Back off to accepting the original
                                        // '\' as a literal
                                        index -= (i + 1);
                                        n = '\\';
                                        break;
                                    }
                                }
                                localMax = n;
                                break;

                            case 'd':
                                if (inRange) {
                                    reportError ("msg.bad.range", "");
                                    return false;
                                }
                                localMax = '9';
                                break;

                            case 'D':
                            case 's':
                            case 'S':
                            case 'w':
                            case 'W':
                                if (inRange) {
                                    reportError ("msg.bad.range", "");
                                    return false;
                                }
                                target.bmsize = 65535;
                                return true;

                            case '0':
                            case '1':
                            case '2':
                            case '3':
                            case '4':
                            case '5':
                            case '6':
                            case '7':
                                /*
                                *  This is a non-ECMA extension - decimal escapes (in this
                                *  case, octal!) are supposed to be an error inside class
                                *  ranges, but supported here for backwards compatibility.
                                *
                                */
                                n = (c - '0');
                                c = src [index];
                                if ('0' <= c && c <= '7') {
                                    index++;
                                    n = 8 * n + (c - '0');
                                    c = src [index];
                                    if ('0' <= c && c <= '7') {
                                        index++;
                                        i = 8 * n + (c - '0');
                                        if (i <= 255)
                                            n = i;
                                        else
                                            index--;
                                    }
                                }
                                localMax = n;
                                break;


                            default:
                                localMax = c;
                                break;

                        }
                        break;

                    default:
                        localMax = src [index++];
                        break;

                }
                if (inRange) {
                    if (rangeStart > localMax) {
                        reportError ("msg.bad.range", "");
                        return false;
                    }
                    inRange = false;
                }
                else {
                    if (index < (end - 1)) {
                        if (src [index] == '-') {
                            ++index;
                            inRange = true;
                            rangeStart = (char)localMax;
                            continue;
                        }
                    }
                }
                if ((state.flags & JSREG_FOLD) != 0) {
                    char cu = upcase ((char)localMax);
                    char cd = downcase ((char)localMax);
                    localMax = (cu >= cd) ? cu : cd;
                }
                if (localMax > max)
                    max = localMax;
            }
            target.bmsize = max;
            return true;
        }

        /*
        *  item:       assertion               An item is either an assertion or
        *              quantatom               a quantified atom.
        *
        *  assertion:  '^'                     Assertions match beginning of string
        *                                      (or line if the class static property
        *                                      RegExp.multiline is true).
        *              '$'                     End of string (or line if the class
        *                                      static property RegExp.multiline is
        *                                      true).
        *              '\b'                    Word boundary (between \w and \W).
        *              '\B'                    Word non-boundary.
        *
        *  quantatom:  atom                    An unquantified atom.
        *              quantatom '{' n ',' m '}'
        *                                      Atom must occur between n and m times.
        *              quantatom '{' n ',' '}' Atom must occur at least n times.
        *              quantatom '{' n '}'     Atom must occur exactly n times.
        *              quantatom '*'           Zero or more times (same as {0,}).
        *              quantatom '+'           One or more times (same as {1,}).
        *              quantatom '?'           Zero or one time (same as {0,1}).
        *
        *              any of which can be optionally followed by '?' for ungreedy
        *
        *  atom:       '(' regexp ')'          A parenthesized regexp (what matched
        *                                      can be addressed using a backreference,
        *                                      see '\' n below).
        *              '.'                     Matches any char except '\n'.
        *              '[' classlist ']'       A character class.
        *              '[' '^' classlist ']'   A negated character class.
        *              '\f'                    Form Feed.
        *              '\n'                    Newline (Line Feed).
        *              '\r'                    Carriage Return.
        *              '\t'                    Horizontal Tab.
        *              '\v'                    Vertical Tab.
        *              '\d'                    A digit (same as [0-9]).
        *              '\D'                    A non-digit.
        *              '\w'                    A word character, [0-9a-z_A-Z].
        *              '\W'                    A non-word character.
        *              '\s'                    A whitespace character, [ \b\f\n\r\t\v].
        *              '\S'                    A non-whitespace character.
        *              '\' n                   A backreference to the nth (n decimal
        *                                      and positive) parenthesized expression.
        *              '\' octal               An octal escape sequence (octal must be
        *                                      two or three digits long, unless it is
        *                                      0 for the null character).
        *              '\x' hex                A hex escape (hex must be two digits).
        *              '\c' ctrl               A control character, ctrl is a letter.
        *              '\' literalatomchar     Any character except one of the above
        *                                      that follow '\' in an atom.
        *              otheratomchar           Any character not first among the other
        *                                      atom right-hand sides.
        */

        private static void doFlat (CompilerState state, char c)
        {
            state.result = new RENode (REOP_FLAT);
            state.result.chr = c;
            state.result.length = 1;
            state.result.flatIndex = -1;
            state.progLength += 3;
        }

        private static int getDecimalValue (char c, CompilerState state, int maxValue, string overflowMessageId)
        {
            bool overflow = false;
            int start = state.cp;
            char [] src = state.cpbegin;
            int value = c - '0';
            for (; state.cp != state.cpend; ++state.cp) {
                c = src [state.cp];
                if (!isDigit (c)) {
                    break;
                }
                if (!overflow) {
                    int digit = c - '0';
                    if (value < (maxValue - digit) / 10) {
                        value = value * 10 + digit;
                    }
                    else {
                        overflow = true;
                        value = maxValue;
                    }
                }
            }
            if (overflow) {
                reportError (overflowMessageId, new string (src, start, state.cp - start));
            }
            return value;
        }

        private static bool parseTerm (CompilerState state)
        {
            char [] src = state.cpbegin;
            char c = src [state.cp++];
            int nDigits = 2;
            int parenBaseCount = state.parenCount;
            int num, tmp;
            RENode term;
            int termStart;
            int ocp = state.cp;

            switch (c) {

                /* assertions and atoms */
                case '^':
                    state.result = new RENode (REOP_BOL);
                    state.progLength++;
                    return true;

                case '$':
                    state.result = new RENode (REOP_EOL);
                    state.progLength++;
                    return true;

                case '\\':
                    if (state.cp < state.cpend) {
                        c = src [state.cp++];
                        switch (c) {

                            /* assertion escapes */
                            case 'b':
                                state.result = new RENode (REOP_WBDRY);
                                state.progLength++;
                                return true;

                            case 'B':
                                state.result = new RENode (REOP_WNONBDRY);
                                state.progLength++;
                                return true;
                            /* Decimal escape */

                            case '0':
                                // Under 'strict' ECMA 3, we interpret \0 as NUL and don't accept octal.
                                // However (and since Rhino doesn't have a 'strict' mode) we'll just
                                // behave the old way for compatibility reasons.
                                // (see http://bugzilla.mozilla.org/show_bug.cgi?id=141078)
                                // TODO: Use strict mode

                                /* octal escape */
                                num = 0;
                                while (state.cp < state.cpend) {
                                    c = src [state.cp];
                                    if ((c >= '0') && (c <= '7')) {
                                        state.cp++;
                                        tmp = 8 * num + (c - '0');
                                        if (tmp > 255)
                                            break;
                                        num = tmp;
                                    }
                                    else
                                        break;
                                }
                                c = (char)(num);
                                doFlat (state, c);
                                break;

                            case '1':
                            case '2':
                            case '3':
                            case '4':
                            case '5':
                            case '6':
                            case '7':
                            case '8':
                            case '9':
                                termStart = state.cp - 1;
                                num = getDecimalValue (c, state, 0xFFFF, "msg.overlarge.backref");
                                /*
                                * n > 9 and > count of parentheses,
                                * then treat as octal instead.
                                */
                                if ((num > 9) && (num > state.parenCount)) {
                                    state.cp = termStart;
                                    num = 0;
                                    while (state.cp < state.cpend) {
                                        c = src [state.cp];
                                        if ((c >= '0') && (c <= '7')) {
                                            state.cp++;
                                            tmp = 8 * num + (c - '0');
                                            if (tmp > 255)
                                                break;
                                            num = tmp;
                                        }
                                        else
                                            break;
                                    }
                                    c = (char)(num);
                                    doFlat (state, c);
                                    break;
                                }
                                /* otherwise, it's a back-reference */
                                state.result = new RENode (REOP_BACKREF);
                                state.result.parenIndex = num - 1;
                                state.progLength += 3;
                                break;
                            /* Control escape */

                            case 'f':
                                c = (char)(0xC);
                                doFlat (state, c);
                                break;

                            case 'n':
                                c = (char)(0xA);
                                doFlat (state, c);
                                break;

                            case 'r':
                                c = (char)(0xD);
                                doFlat (state, c);
                                break;

                            case 't':
                                c = (char)(0x9);
                                doFlat (state, c);
                                break;

                            case 'v':
                                c = (char)(0xB);
                                doFlat (state, c);
                                break;
                            /* Control letter */

                            case 'c':
                                if ((state.cp < state.cpend) && char.IsLetter (src [state.cp]))
                                    c = (char)(src [state.cp++] & 0x1F);
                                else {
                                    /* back off to accepting the original '\' as a literal */
                                    --state.cp;
                                    c = '\\';
                                }
                                doFlat (state, c);
                                break;
                            /* UnicodeEscapeSequence */

                            case 'u':
                                nDigits += 2;
                                // fall thru...
                                /* HexEscapeSequence */
                                goto case 'x';

                            case 'x': {
                                    int n = 0;
                                    int i;
                                    for (i = 0; (i < nDigits) && (state.cp < state.cpend); i++) {
                                        c = src [state.cp++];
                                        n = ScriptConvert.XDigitToInt (c, n);
                                        if (n < 0) {
                                            // Back off to accepting the original
                                            // 'u' or 'x' as a literal
                                            state.cp -= (i + 2);
                                            n = src [state.cp++];
                                            break;
                                        }
                                    }
                                    c = (char)(n);
                                }
                                doFlat (state, c);
                                break;
                            /* Character class escapes */

                            case 'd':
                                state.result = new RENode (REOP_DIGIT);
                                state.progLength++;
                                break;

                            case 'D':
                                state.result = new RENode (REOP_NONDIGIT);
                                state.progLength++;
                                break;

                            case 's':
                                state.result = new RENode (REOP_SPACE);
                                state.progLength++;
                                break;

                            case 'S':
                                state.result = new RENode (REOP_NONSPACE);
                                state.progLength++;
                                break;

                            case 'w':
                                state.result = new RENode (REOP_ALNUM);
                                state.progLength++;
                                break;

                            case 'W':
                                state.result = new RENode (REOP_NONALNUM);
                                state.progLength++;
                                break;
                            /* IdentityEscape */

                            default:
                                state.result = new RENode (REOP_FLAT);
                                state.result.chr = c;
                                state.result.length = 1;
                                state.result.flatIndex = state.cp - 1;
                                state.progLength += 3;
                                break;

                        }
                        break;
                    }
                    else {
                        /* a trailing '\' is an error */
                        reportError ("msg.trail.backslash", "");
                        return false;
                    }

                case '(': {
                        RENode result = null;
                        termStart = state.cp;
                        if (state.cp + 1 < state.cpend && src [state.cp] == '?' && ((c = src [state.cp + 1]) == '=' || c == '!' || c == ':')) {
                            state.cp += 2;
                            if (c == '=') {
                                result = new RENode (REOP_ASSERT);
                                /* ASSERT, <next>, ... ASSERTTEST */
                                state.progLength += 4;
                            }
                            else if (c == '!') {
                                result = new RENode (REOP_ASSERT_NOT);
                                /* ASSERTNOT, <next>, ... ASSERTNOTTEST */
                                state.progLength += 4;
                            }
                        }
                        else {
                            result = new RENode (REOP_LPAREN);
                            /* LPAREN, <index>, ... RPAREN, <index> */
                            state.progLength += 6;
                            result.parenIndex = state.parenCount++;
                        }
                        ++state.parenNesting;
                        if (!parseDisjunction (state))
                            return false;
                        if (state.cp == state.cpend || src [state.cp] != ')') {
                            reportError ("msg.unterm.paren", "");
                            return false;
                        }
                        ++state.cp;
                        --state.parenNesting;
                        if (result != null) {
                            result.kid = state.result;
                            state.result = result;
                        }
                        break;
                    }

                case ')':
                    reportError ("msg.re.unmatched.right.paren", "");
                    return false;

                case '[':
                    state.result = new RENode (REOP_CLASS);
                    termStart = state.cp;
                    state.result.startIndex = termStart;
                    while (true) {
                        if (state.cp >= state.cpend) {
                            reportError ("msg.unterm.class", "");
                            return false;
                        }
                        if (src [state.cp] == '\\')
                            state.cp++;
                        else {
                            if (src [state.cp] == ']') {
                                state.result.kidlen = state.cp - termStart;
                                break;
                            }
                        }
                        state.cp++;
                    }
                    state.result.index = state.classCount++;
                    /*
                    * Call calculateBitmapSize now as we want any errors it finds
                    * to be reported during the parse phase, not at execution.
                    */
                    if (!calculateBitmapSize (state, state.result, src, termStart, state.cp++))
                        return false;
                    state.progLength += 3; /* CLASS, <index> */
                    break;


                case '.':
                    state.result = new RENode (REOP_DOT);
                    state.progLength++;
                    break;

                case '*':
                case '+':
                case '?':
                    reportError ("msg.bad.quant", Convert.ToString (src [state.cp - 1]));
                    return false;

                default:
                    state.result = new RENode (REOP_FLAT);
                    state.result.chr = c;
                    state.result.length = 1;
                    state.result.flatIndex = state.cp - 1;
                    state.progLength += 3;
                    break;

            }

            term = state.result;
            if (state.cp == state.cpend) {
                return true;
            }
            bool hasQ = false;
            switch (src [state.cp]) {

                case '+':
                    state.result = new RENode (REOP_QUANT);
                    state.result.min = 1;
                    state.result.max = -1;
                    /* <PLUS>, <parencount>, <parenindex>, <next> ... <ENDCHILD> */
                    state.progLength += 8;
                    hasQ = true;
                    break;

                case '*':
                    state.result = new RENode (REOP_QUANT);
                    state.result.min = 0;
                    state.result.max = -1;
                    /* <STAR>, <parencount>, <parenindex>, <next> ... <ENDCHILD> */
                    state.progLength += 8;
                    hasQ = true;
                    break;

                case '?':
                    state.result = new RENode (REOP_QUANT);
                    state.result.min = 0;
                    state.result.max = 1;
                    /* <OPT>, <parencount>, <parenindex>, <next> ... <ENDCHILD> */
                    state.progLength += 8;
                    hasQ = true;
                    break;

                case '{':  /* balance '}' */ {
                        int min = 0;
                        int max = -1;
                        int leftCurl = state.cp;

                        /* For Perl etc. compatibility, if quntifier does not match
                        * \{\d+(,\d*)?\} exactly back off from it
                        * being a quantifier, and chew it up as a literal
                        * atom next time instead.
                        */

                        c = src [++state.cp];
                        if (isDigit (c)) {
                            ++state.cp;
                            min = getDecimalValue (c, state, 0xFFFF, "msg.overlarge.min");
                            c = src [state.cp];
                            if (c == ',') {
                                c = src [++state.cp];
                                if (isDigit (c)) {
                                    ++state.cp;
                                    max = getDecimalValue (c, state, 0xFFFF, "msg.overlarge.max");
                                    c = src [state.cp];
                                    if (min > max) {
                                        reportError ("msg.max.lt.min", Convert.ToString (src [state.cp]));
                                        return false;
                                    }
                                }
                            }
                            else {
                                max = min;
                            }
                            /* balance '{' */
                            if (c == '}') {
                                state.result = new RENode (REOP_QUANT);
                                state.result.min = min;
                                state.result.max = max;
                                // QUANT, <min>, <max>, <parencount>,
                                // <parenindex>, <next> ... <ENDCHILD>
                                state.progLength += 12;
                                hasQ = true;

                                if (state.cp + 1 != state.cpend) {
                                    char nc = src [state.cp + 1];
                                    if (nc == '{') {
                                        string quant = string.Empty;
                                        for (int i = 2; state.cp + i != state.cpend; i++) {
                                            if (src [state.cp + i] == '}')
                                                break;                                           
                                            quant += src [state.cp + i];
                                        }
                                        reportError ("msg.bad.quant", quant);
                                    }
                                }
                            }
                        }
                        if (!hasQ) {
                            state.cp = leftCurl;
                        }
                        break;
                    }
            }
            if (!hasQ)
                return true;

            ++state.cp;
            state.result.kid = term;
            state.result.parenIndex = parenBaseCount;
            state.result.parenCount = state.parenCount - parenBaseCount;
            if ((state.cp < state.cpend) && (src [state.cp] == '?')) {
                ++state.cp;
                state.result.greedy = false;
            }
            else
                state.result.greedy = true;
            return true;
        }

        private static void resolveForwardJump (sbyte [] array, int from, int pc)
        {
            if (from > pc)
                throw Context.CodeBug ();
            addIndex (array, from, pc - from);
        }

        private static int getOffset (sbyte [] array, int pc)
        {
            return getIndex (array, pc);
        }

        private static int addIndex (sbyte [] array, int pc, int index)
        {
            if (index < 0)
                throw Context.CodeBug ();
            if (index > 0xFFFF)
                throw Context.ReportRuntimeError ("Too complex regexp");
            array [pc] = (sbyte)(index >> 8);
            array [pc + 1] = (sbyte)(index);
            return pc + 2;
        }

        private static int getIndex (sbyte [] array, int pc)
        {
            return ((array [pc] & 0xFF) << 8) | (array [pc + 1] & 0xFF);
        }

        private const int OFFSET_LEN = 2;
        private const int INDEX_LEN = 2;

        private static int emitREBytecode (CompilerState state, RECompiled re, int pc, RENode t)
        {
            RENode nextAlt;
            int nextAltFixup, nextTermFixup;
            sbyte [] program = re.program;

            while (t != null) {
                program [pc++] = t.op;
                switch (t.op) {

                    case REOP_EMPTY:
                        --pc;
                        break;

                    case REOP_ALT:
                        nextAlt = t.kid2;
                        nextAltFixup = pc; /* address of next alternate */
                        pc += OFFSET_LEN;
                        pc = emitREBytecode (state, re, pc, t.kid);
                        program [pc++] = REOP_JUMP;
                        nextTermFixup = pc; /* address of following term */
                        pc += OFFSET_LEN;
                        resolveForwardJump (program, nextAltFixup, pc);
                        pc = emitREBytecode (state, re, pc, nextAlt);

                        program [pc++] = REOP_JUMP;
                        nextAltFixup = pc;
                        pc += OFFSET_LEN;

                        resolveForwardJump (program, nextTermFixup, pc);
                        resolveForwardJump (program, nextAltFixup, pc);
                        break;

                    case REOP_FLAT:
                        /*
                        * Consecutize FLAT's if possible.
                        */
                        if (t.flatIndex != -1) {
                            while ((t.next != null) && (t.next.op == REOP_FLAT) && ((t.flatIndex + t.length) == t.next.flatIndex)) {
                                t.length += t.next.length;
                                t.next = t.next.next;
                            }
                        }
                        if ((t.flatIndex != -1) && (t.length > 1)) {
                            if ((state.flags & JSREG_FOLD) != 0)
                                program [pc - 1] = REOP_FLATi;
                            else
                                program [pc - 1] = REOP_FLAT;
                            pc = addIndex (program, pc, t.flatIndex);
                            pc = addIndex (program, pc, t.length);
                        }
                        else {
                            if (t.chr < 256) {
                                if ((state.flags & JSREG_FOLD) != 0)
                                    program [pc - 1] = REOP_FLAT1i;
                                else
                                    program [pc - 1] = REOP_FLAT1;
                                program [pc++] = (sbyte)(t.chr);
                            }
                            else {
                                if ((state.flags & JSREG_FOLD) != 0)
                                    program [pc - 1] = REOP_UCFLAT1i;
                                else
                                    program [pc - 1] = REOP_UCFLAT1;
                                pc = addIndex (program, pc, t.chr);
                            }
                        }
                        break;

                    case REOP_LPAREN:
                        pc = addIndex (program, pc, t.parenIndex);
                        pc = emitREBytecode (state, re, pc, t.kid);
                        program [pc++] = REOP_RPAREN;
                        pc = addIndex (program, pc, t.parenIndex);
                        break;

                    case REOP_BACKREF:
                        pc = addIndex (program, pc, t.parenIndex);
                        break;

                    case REOP_ASSERT:
                        nextTermFixup = pc;
                        pc += OFFSET_LEN;
                        pc = emitREBytecode (state, re, pc, t.kid);
                        program [pc++] = REOP_ASSERTTEST;
                        resolveForwardJump (program, nextTermFixup, pc);
                        break;

                    case REOP_ASSERT_NOT:
                        nextTermFixup = pc;
                        pc += OFFSET_LEN;
                        pc = emitREBytecode (state, re, pc, t.kid);
                        program [pc++] = REOP_ASSERTNOTTEST;
                        resolveForwardJump (program, nextTermFixup, pc);
                        break;

                    case REOP_QUANT:
                        if ((t.min == 0) && (t.max == -1))
                            program [pc - 1] = (t.greedy) ? REOP_STAR : REOP_MINIMALSTAR;
                        else if ((t.min == 0) && (t.max == 1))
                            program [pc - 1] = (t.greedy) ? REOP_OPT : REOP_MINIMALOPT;
                        else if ((t.min == 1) && (t.max == -1))
                            program [pc - 1] = (t.greedy) ? REOP_PLUS : REOP_MINIMALPLUS;
                        else {
                            if (!t.greedy)
                                program [pc - 1] = REOP_MINIMALQUANT;
                            pc = addIndex (program, pc, t.min);
                            // max can be -1 which addIndex does not accept
                            pc = addIndex (program, pc, t.max + 1);
                        }
                        pc = addIndex (program, pc, t.parenCount);
                        pc = addIndex (program, pc, t.parenIndex);
                        nextTermFixup = pc;
                        pc += OFFSET_LEN;
                        pc = emitREBytecode (state, re, pc, t.kid);
                        program [pc++] = REOP_ENDCHILD;
                        resolveForwardJump (program, nextTermFixup, pc);
                        break;

                    case REOP_CLASS:
                        pc = addIndex (program, pc, t.index);
                        re.classList [t.index] = new RECharSet (t.bmsize, t.startIndex, t.kidlen);
                        break;

                    default:
                        break;

                }
                t = t.next;
            }
            return pc;
        }

        private static void pushProgState (REGlobalData gData, int min, int max, REBackTrackData backTrackLastToSave, int continuation_pc, int continuation_op)
        {
            gData.stateStackTop = new REProgState (gData.stateStackTop, min, max, gData.cp, backTrackLastToSave, continuation_pc, continuation_op);
        }

        private static REProgState popProgState (REGlobalData gData)
        {
            REProgState state = gData.stateStackTop;
            gData.stateStackTop = state.previous;
            return state;
        }

        private static void pushBackTrackState (REGlobalData gData, sbyte op, int target)
        {
            gData.backTrackStackTop = new REBackTrackData (gData, op, target);
        }

        /*
        *   Consecutive literal characters.
        */
        private static bool flatNMatcher (REGlobalData gData, int matchChars, int length, char [] chars, int end)
        {
            if ((gData.cp + length) > end)
                return false;
            for (int i = 0; i < length; i++) {
                if (gData.regexp.source [matchChars + i] != chars [gData.cp + i]) {
                    return false;
                }
            }
            gData.cp += length;
            return true;
        }

        private static bool flatNIMatcher (REGlobalData gData, int matchChars, int length, char [] chars, int end)
        {
            if ((gData.cp + length) > end)
                return false;
            for (int i = 0; i < length; i++) {
                if (upcase (gData.regexp.source [matchChars + i]) != upcase (chars [gData.cp + i])) {
                    return false;
                }
            }
            gData.cp += length;
            return true;
        }

        /*
        1. Evaluate DecimalEscape to obtain an EscapeValue E.
        2. If E is not a character then go to step 6.
        3. Let ch be E's character.
        4. Let A be a one-element RECharSet containing the character ch.
        5. Call CharacterSetMatcher(A, false) and return its Matcher result.
        6. E must be an integer. Let n be that integer.
        7. If n=0 or n>NCapturingParens then throw a SyntaxError exception.
        8. Return an internal Matcher closure that takes two arguments, a State x
        and a Continuation c, and performs the following:
        1. Let cap be x's captures internal array.
        2. Let s be cap[n].
        3. If s is undefined, then call c(x) and return its result.
        4. Let e be x's endIndex.
        5. Let len be s's length.
        6. Let f be e+len.
        7. If f>InputLength, return failure.
        8. If there exists an integer i between 0 (inclusive) and len (exclusive)
        such that Canonicalize(s[i]) is not the same character as
        Canonicalize(Input [e+i]), then return failure.
        9. Let y be the State (f, cap).
        10. Call c(y) and return its result.
        */
        private static bool backrefMatcher (REGlobalData gData, int parenIndex, char [] chars, int end)
        {
            int len;
            int i;
            int parenContent = gData.parens_index (parenIndex);
            if (parenContent == -1)
                return true;

            len = gData.parens_length (parenIndex);
            if ((gData.cp + len) > end)
                return false;

            if ((gData.regexp.flags & JSREG_FOLD) != 0) {
                for (i = 0; i < len; i++) {
                    if (upcase (chars [parenContent + i]) != upcase (chars [gData.cp + i]))
                        return false;
                }
            }
            else {
                for (i = 0; i < len; i++) {
                    if (chars [parenContent + i] != chars [gData.cp + i])
                        return false;
                }
            }
            gData.cp += len;
            return true;
        }


        /* Add a single character to the RECharSet */
        private static void addCharacterToCharSet (RECharSet cs, char c)
        {
            int byteIndex = (int)(c / 8);
            if (c > cs.length)
                throw new ApplicationException ();
            cs.bits [byteIndex] |= (sbyte)(1 << (c & 0x7));
        }


        /* Add a character range, c1 to c2 (inclusive) to the RECharSet */
        private static void addCharacterRangeToCharSet (RECharSet cs, char c1, char c2)
        {
            int i;

            int byteIndex1 = (int)(c1 / 8);
            int byteIndex2 = (int)(c2 / 8);

            if ((c2 > cs.length) || (c1 > c2))
                throw new ApplicationException ();

            c1 &= (char)(0x7);
            c2 &= (char)(0x7);

            if (byteIndex1 == byteIndex2) {
                cs.bits [byteIndex1] |= (sbyte)(((int)(0xFF) >> (int)(7 - (c2 - c1))) << (int)c1);
            }
            else {
                cs.bits [byteIndex1] |= (sbyte)(0xFF << (int)c1);
                for (i = byteIndex1 + 1; i < byteIndex2; i++)
                    cs.bits [i] = unchecked ((sbyte)0xFF);
                cs.bits [byteIndex2] |= (sbyte)((int)(0xFF) >> (int)(7 - c2));
            }
        }



        /* Compile the source of the class into a RECharSet */
        private static void processCharSet (REGlobalData gData, RECharSet charSet)
        {
            lock (charSet) {
                if (!charSet.converted) {
                    processCharSetImpl (gData, charSet);
                    charSet.converted = true;
                }
            }
        }


        private static void processCharSetImpl (REGlobalData gData, RECharSet charSet)
        {
            int src = charSet.startIndex;
            int end = src + charSet.strlength;

            char rangeStart = (char)(0), thisCh;
            int byteLength;
            char c;
            int n;
            int nDigits;
            int i;
            bool inRange = false;

            charSet.sense = true;
            byteLength = (charSet.length / 8) + 1;
            charSet.bits = new sbyte [byteLength];

            if (src == end)
                return;

            if (gData.regexp.source [src] == '^') {
                charSet.sense = false;
                ++src;
            }

            while (src != end) {
                nDigits = 2;
                switch (gData.regexp.source [src]) {

                    case '\\':
                        ++src;
                        c = gData.regexp.source [src++];
                        switch (c) {

                            case 'b':
                                thisCh = (char)(0x8);
                                break;

                            case 'f':
                                thisCh = (char)(0xC);
                                break;

                            case 'n':
                                thisCh = (char)(0xA);
                                break;

                            case 'r':
                                thisCh = (char)(0xD);
                                break;

                            case 't':
                                thisCh = (char)(0x9);
                                break;

                            case 'v':
                                thisCh = (char)(0xB);
                                break;

                            case 'c':
                                if (((src + 1) < end) && isWord (gData.regexp.source [src + 1]))
                                    thisCh = (char)(gData.regexp.source [src++] & 0x1F);
                                else {
                                    --src;
                                    thisCh = '\\';
                                }
                                break;

                            case 'u':
                                nDigits += 2;
                                // fall thru
                                goto case 'x';

                            case 'x':
                                n = 0;
                                for (i = 0; (i < nDigits) && (src < end); i++) {
                                    c = gData.regexp.source [src++];
                                    int digit = toASCIIHexDigit (c);
                                    if (digit < 0) {
                                        /* back off to accepting the original '\'
                                        * as a literal
                                        */
                                        src -= (i + 1);
                                        n = '\\';
                                        break;
                                    }
                                    n = (n << 4) | digit;
                                }
                                thisCh = (char)(n);
                                break;

                            case '0':
                            case '1':
                            case '2':
                            case '3':
                            case '4':
                            case '5':
                            case '6':
                            case '7':
                                /*
                                *  This is a non-ECMA extension - decimal escapes (in this
                                *  case, octal!) are supposed to be an error inside class
                                *  ranges, but supported here for backwards compatibility.
                                *
                                */
                                n = (c - '0');
                                c = gData.regexp.source [src];
                                if ('0' <= c && c <= '7') {
                                    src++;
                                    n = 8 * n + (c - '0');
                                    c = gData.regexp.source [src];
                                    if ('0' <= c && c <= '7') {
                                        src++;
                                        i = 8 * n + (c - '0');
                                        if (i <= 255)
                                            n = i;
                                        else
                                            src--;
                                    }
                                }
                                thisCh = (char)(n);
                                break;


                            case 'd':
                                addCharacterRangeToCharSet (charSet, '0', '9');
                                continue; /* don't need range processing */

                            case 'D':
                                addCharacterRangeToCharSet (charSet, (char)0, (char)('0' - 1));
                                addCharacterRangeToCharSet (charSet, (char)('9' + 1), (char)(charSet.length));
                                continue;

                            case 's':
                                for (i = (int)(charSet.length); i >= 0; i--)
                                    if (isREWhiteSpace (i))
                                        addCharacterToCharSet (charSet, (char)(i));
                                continue;

                            case 'S':
                                for (i = (int)(charSet.length); i >= 0; i--)
                                    if (!isREWhiteSpace (i))
                                        addCharacterToCharSet (charSet, (char)(i));
                                continue;

                            case 'w':
                                for (i = (int)(charSet.length); i >= 0; i--)
                                    if (isWord ((char)i))
                                        addCharacterToCharSet (charSet, (char)(i));
                                continue;

                            case 'W':
                                for (i = (int)(charSet.length); i >= 0; i--)
                                    if (!isWord ((char)i))
                                        addCharacterToCharSet (charSet, (char)(i));
                                continue;

                            default:
                                thisCh = c;
                                break;

                        }
                        break;


                    default:
                        thisCh = gData.regexp.source [src++];
                        break;

                }
                if (inRange) {
                    if ((gData.regexp.flags & JSREG_FOLD) != 0) {
                        addCharacterRangeToCharSet (charSet, upcase (rangeStart), upcase (thisCh));
                        addCharacterRangeToCharSet (charSet, downcase (rangeStart), downcase (thisCh));
                    }
                    else {
                        addCharacterRangeToCharSet (charSet, rangeStart, thisCh);
                    }
                    inRange = false;
                }
                else {
                    if ((gData.regexp.flags & JSREG_FOLD) != 0) {
                        addCharacterToCharSet (charSet, upcase (thisCh));
                        addCharacterToCharSet (charSet, downcase (thisCh));
                    }
                    else {
                        addCharacterToCharSet (charSet, thisCh);
                    }
                    if (src < (end - 1)) {
                        if (gData.regexp.source [src] == '-') {
                            ++src;
                            inRange = true;
                            rangeStart = thisCh;
                        }
                    }
                }
            }
        }


        /*
        *   Initialize the character set if it this is the first call.
        *   Test the bit - if the ^ flag was specified, non-inclusion is a success
        */
        private static bool classMatcher (REGlobalData gData, RECharSet charSet, char ch)
        {
            if (!charSet.converted) {
                processCharSet (gData, charSet);
            }

            int byteIndex = ch / 8;
            if (charSet.sense) {
                if ((charSet.length == 0) || ((ch > charSet.length) || ((charSet.bits [byteIndex] & (1 << (ch & 0x7))) == 0)))
                    return false;
            }
            else {
                if (!((charSet.length == 0) || ((ch > charSet.length) || ((charSet.bits [byteIndex] & (1 << (ch & 0x7))) == 0))))
                    return false;
            }
            return true;
        }

        private static bool executeREBytecode (REGlobalData gData, char [] chars, int end)
        {
            int pc = 0;
            sbyte [] program = gData.regexp.program;
            int currentContinuation_op;
            int currentContinuation_pc;
            bool result = false;

            currentContinuation_pc = 0;
            currentContinuation_op = REOP_END;
            if (debug) {
                System.Console.Out.WriteLine ("Input = \"" + new string (chars) + "\", start at " + gData.cp);
            }
            int op = program [pc++];
            for (; ; ) {
                if (debug) {
                    System.Console.Out.WriteLine ("Testing at " + gData.cp + ", op = " + op);
                }
                switch (op) {

                    case REOP_EMPTY:
                        result = true;
                        break;

                    case REOP_BOL:
                        if (gData.cp != 0) {
                            if (gData.multiline || ((gData.regexp.flags & JSREG_MULTILINE) != 0)) {
                                if (!isLineTerm (chars [gData.cp - 1])) {
                                    result = false;
                                    break;
                                }
                            }
                            else {
                                result = false;
                                break;
                            }
                        }
                        result = true;
                        break;

                    case REOP_EOL:
                        if (gData.cp != end) {
                            if (gData.multiline || ((gData.regexp.flags & JSREG_MULTILINE) != 0)) {
                                if (!isLineTerm (chars [gData.cp])) {
                                    result = false;
                                    break;
                                }
                            }
                            else {
                                result = false;
                                break;
                            }
                        }
                        result = true;
                        break;

                    case REOP_WBDRY:
                        result = ((gData.cp == 0 || !isWord (chars [gData.cp - 1])) ^ !((gData.cp < end) && isWord (chars [gData.cp])));
                        break;

                    case REOP_WNONBDRY:
                        result = ((gData.cp == 0 || !isWord (chars [gData.cp - 1])) ^ ((gData.cp < end) && isWord (chars [gData.cp])));
                        break;

                    case REOP_DOT:
                        result = (gData.cp != end && !isLineTerm (chars [gData.cp]));
                        if (result) {
                            gData.cp++;
                        }
                        break;

                    case REOP_DIGIT:
                        result = (gData.cp != end && isDigit (chars [gData.cp]));
                        if (result) {
                            gData.cp++;
                        }
                        break;

                    case REOP_NONDIGIT:
                        result = (gData.cp != end && !isDigit (chars [gData.cp]));
                        if (result) {
                            gData.cp++;
                        }
                        break;

                    case REOP_SPACE:
                        result = (gData.cp != end && isREWhiteSpace (chars [gData.cp]));
                        if (result) {
                            gData.cp++;
                        }
                        break;

                    case REOP_NONSPACE:
                        result = (gData.cp != end && !isREWhiteSpace (chars [gData.cp]));
                        if (result) {
                            gData.cp++;
                        }
                        break;

                    case REOP_ALNUM:
                        result = (gData.cp != end && isWord (chars [gData.cp]));
                        if (result) {
                            gData.cp++;
                        }
                        break;

                    case REOP_NONALNUM:
                        result = (gData.cp != end && !isWord (chars [gData.cp]));
                        if (result) {
                            gData.cp++;
                        }
                        break;

                    case REOP_FLAT: {
                            int offset = getIndex (program, pc);
                            pc += INDEX_LEN;
                            int length = getIndex (program, pc);
                            pc += INDEX_LEN;
                            result = flatNMatcher (gData, offset, length, chars, end);
                        }
                        break;

                    case REOP_FLATi: {
                            int offset = getIndex (program, pc);
                            pc += INDEX_LEN;
                            int length = getIndex (program, pc);
                            pc += INDEX_LEN;
                            result = flatNIMatcher (gData, offset, length, chars, end);
                        }
                        break;

                    case REOP_FLAT1: {
                            char matchCh = (char)(program [pc++] & 0xFF);
                            result = (gData.cp != end && chars [gData.cp] == matchCh);
                            if (result) {
                                gData.cp++;
                            }
                        }
                        break;

                    case REOP_FLAT1i: {
                            char matchCh = (char)(program [pc++] & 0xFF);
                            result = (gData.cp != end && upcase (chars [gData.cp]) == upcase (matchCh));
                            if (result) {
                                gData.cp++;
                            }
                        }
                        break;

                    case REOP_UCFLAT1: {
                            char matchCh = (char)getIndex (program, pc);
                            pc += INDEX_LEN;
                            result = (gData.cp != end && chars [gData.cp] == matchCh);
                            if (result) {
                                gData.cp++;
                            }
                        }
                        break;

                    case REOP_UCFLAT1i: {
                            char matchCh = (char)getIndex (program, pc);
                            pc += INDEX_LEN;
                            result = (gData.cp != end && upcase (chars [gData.cp]) == upcase (matchCh));
                            if (result) {
                                gData.cp++;
                            }
                        }
                        break;

                    case REOP_ALT: {
                            int nextpc;
                            sbyte nextop;
                            pushProgState (gData, 0, 0, null, currentContinuation_pc, currentContinuation_op);
                            nextpc = pc + getOffset (program, pc);
                            nextop = program [nextpc++];
                            pushBackTrackState (gData, nextop, nextpc);
                            pc += INDEX_LEN;
                            op = program [pc++];
                        }
                        continue;


                    case REOP_JUMP: {
                            int offset;
                            REProgState state = popProgState (gData);
                            currentContinuation_pc = state.continuation_pc;
                            currentContinuation_op = state.continuation_op;
                            offset = getOffset (program, pc);
                            pc += offset;
                            op = program [pc++];
                        }
                        continue;



                    case REOP_LPAREN: {
                            int parenIndex = getIndex (program, pc);
                            pc += INDEX_LEN;
                            gData.set_parens (parenIndex, gData.cp, 0);
                            op = program [pc++];
                        }
                        continue;

                    case REOP_RPAREN: {
                            int cap_index;
                            int parenIndex = getIndex (program, pc);
                            pc += INDEX_LEN;
                            cap_index = gData.parens_index (parenIndex);
                            gData.set_parens (parenIndex, cap_index, gData.cp - cap_index);
                            if (parenIndex > gData.lastParen)
                                gData.lastParen = parenIndex;
                            op = program [pc++];
                        }
                        continue;

                    case REOP_BACKREF: {
                            int parenIndex = getIndex (program, pc);
                            pc += INDEX_LEN;
                            result = backrefMatcher (gData, parenIndex, chars, end);
                        }
                        break;


                    case REOP_CLASS: {
                            int index = getIndex (program, pc);
                            pc += INDEX_LEN;
                            if (gData.cp != end) {
                                if (classMatcher (gData, gData.regexp.classList [index], chars [gData.cp])) {
                                    gData.cp++;
                                    result = true;
                                    break;
                                }
                            }
                            result = false;
                        }
                        break;


                    case REOP_ASSERT:
                    case REOP_ASSERT_NOT: {
                            sbyte testOp;
                            pushProgState (gData, 0, 0, gData.backTrackStackTop, currentContinuation_pc, currentContinuation_op);
                            if (op == REOP_ASSERT) {
                                testOp = REOP_ASSERTTEST;
                            }
                            else {
                                testOp = REOP_ASSERTNOTTEST;
                            }
                            pushBackTrackState (gData, testOp, pc + getOffset (program, pc));
                            pc += INDEX_LEN;
                            op = program [pc++];
                        }
                        continue;


                    case REOP_ASSERTTEST:
                    case REOP_ASSERTNOTTEST: {
                            REProgState state = popProgState (gData);
                            gData.cp = state.index;
                            gData.backTrackStackTop = state.backTrack;
                            currentContinuation_pc = state.continuation_pc;
                            currentContinuation_op = state.continuation_op;
                            if (result) {
                                if (op == REOP_ASSERTTEST) {
                                    result = true;
                                }
                                else {
                                    result = false;
                                }
                            }
                            else {
                                if (op == REOP_ASSERTTEST) {
                                    // Do nothing
                                }
                                else {
                                    result = true;
                                }
                            }
                        }
                        break;


                    case REOP_STAR:
                    case REOP_PLUS:
                    case REOP_OPT:
                    case REOP_QUANT:
                    case REOP_MINIMALSTAR:
                    case REOP_MINIMALPLUS:
                    case REOP_MINIMALOPT:
                    case REOP_MINIMALQUANT: {
                            int min, max;
                            bool greedy = false;
                            switch (op) {

                                case REOP_STAR:
                                    greedy = true;
                                    // fallthrough
                                    goto case REOP_MINIMALSTAR;

                                case REOP_MINIMALSTAR:
                                    min = 0;
                                    max = -1;
                                    break;

                                case REOP_PLUS:
                                    greedy = true;
                                    // fallthrough
                                    goto case REOP_MINIMALPLUS;

                                case REOP_MINIMALPLUS:
                                    min = 1;
                                    max = -1;
                                    break;

                                case REOP_OPT:
                                    greedy = true;
                                    // fallthrough
                                    goto case REOP_MINIMALOPT;

                                case REOP_MINIMALOPT:
                                    min = 0;
                                    max = 1;
                                    break;

                                case REOP_QUANT:
                                    greedy = true;
                                    // fallthrough
                                    goto case REOP_MINIMALQUANT;

                                case REOP_MINIMALQUANT:
                                    min = getOffset (program, pc);
                                    pc += INDEX_LEN;
                                    // See comments in emitREBytecode for " - 1" reason
                                    max = getOffset (program, pc) - 1;
                                    pc += INDEX_LEN;
                                    break;

                                default:
                                    throw Context.CodeBug ();

                            }
                            pushProgState (gData, min, max, null, currentContinuation_pc, currentContinuation_op);
                            if (greedy) {
                                currentContinuation_op = REOP_REPEAT;
                                currentContinuation_pc = pc;
                                pushBackTrackState (gData, REOP_REPEAT, pc);
                                /* Step over <parencount>, <parenindex> & <next> */
                                pc += 3 * INDEX_LEN;
                                op = program [pc++];
                            }
                            else {
                                if (min != 0) {
                                    currentContinuation_op = REOP_MINIMALREPEAT;
                                    currentContinuation_pc = pc;
                                    /* <parencount> <parenindex> & <next> */
                                    pc += 3 * INDEX_LEN;
                                    op = program [pc++];
                                }
                                else {
                                    pushBackTrackState (gData, REOP_MINIMALREPEAT, pc);
                                    popProgState (gData);
                                    pc += 2 * INDEX_LEN; // <parencount> & <parenindex>
                                    pc = pc + getOffset (program, pc);
                                    op = program [pc++];
                                }
                            }
                        }
                        continue;


                    case REOP_ENDCHILD:
                        // Use the current continuation.
                        pc = currentContinuation_pc;
                        op = currentContinuation_op;
                        continue;


                    case REOP_REPEAT: {
                            REProgState state = popProgState (gData);
                            if (!result) {
                                //
                                // There's been a failure, see if we have enough
                                // children.
                                //
                                if (state.min == 0)
                                    result = true;
                                currentContinuation_pc = state.continuation_pc;
                                currentContinuation_op = state.continuation_op;
                                pc += 2 * INDEX_LEN; /* <parencount> & <parenindex> */
                                pc = pc + getOffset (program, pc);
                                break;
                            }
                            else {
                                if (state.min == 0 && gData.cp == state.index) {
                                    // matched an empty string, that'll get us nowhere
                                    result = false;
                                    currentContinuation_pc = state.continuation_pc;
                                    currentContinuation_op = state.continuation_op;
                                    pc += 2 * INDEX_LEN;
                                    pc = pc + getOffset (program, pc);
                                    break;
                                }
                                int new_min = state.min, new_max = state.max;
                                if (new_min != 0)
                                    new_min--;
                                if (new_max != -1)
                                    new_max--;
                                if (new_max == 0) {
                                    result = true;
                                    currentContinuation_pc = state.continuation_pc;
                                    currentContinuation_op = state.continuation_op;
                                    pc += 2 * INDEX_LEN;
                                    pc = pc + getOffset (program, pc);
                                    break;
                                }
                                pushProgState (gData, new_min, new_max, null, state.continuation_pc, state.continuation_op);
                                currentContinuation_op = REOP_REPEAT;
                                currentContinuation_pc = pc;
                                pushBackTrackState (gData, REOP_REPEAT, pc);
                                int parenCount = getIndex (program, pc);
                                pc += INDEX_LEN;
                                int parenIndex = getIndex (program, pc);
                                pc += 2 * INDEX_LEN;
                                op = program [pc++];
                                for (int k = 0; k < parenCount; k++) {
                                    gData.set_parens (parenIndex + k, -1, 0);
                                }
                            }
                        }
                        continue;


                    case REOP_MINIMALREPEAT: {
                            REProgState state = popProgState (gData);
                            if (!result) {
                                //
                                // Non-greedy failure - try to consume another child.
                                //
                                if (state.max == -1 || state.max > 0) {
                                    pushProgState (gData, state.min, state.max, null, state.continuation_pc, state.continuation_op);
                                    currentContinuation_op = REOP_MINIMALREPEAT;
                                    currentContinuation_pc = pc;
                                    int parenCount = getIndex (program, pc);
                                    pc += INDEX_LEN;
                                    int parenIndex = getIndex (program, pc);
                                    pc += 2 * INDEX_LEN;
                                    for (int k = 0; k < parenCount; k++) {
                                        gData.set_parens (parenIndex + k, -1, 0);
                                    }
                                    op = program [pc++];
                                    continue;
                                }
                                else {
                                    // Don't need to adjust pc since we're going to pop.
                                    currentContinuation_pc = state.continuation_pc;
                                    currentContinuation_op = state.continuation_op;
                                    break;
                                }
                            }
                            else {
                                if (state.min == 0 && gData.cp == state.index) {
                                    // Matched an empty string, that'll get us nowhere.
                                    result = false;
                                    currentContinuation_pc = state.continuation_pc;
                                    currentContinuation_op = state.continuation_op;
                                    break;
                                }
                                int new_min = state.min, new_max = state.max;
                                if (new_min != 0)
                                    new_min--;
                                if (new_max != -1)
                                    new_max--;
                                pushProgState (gData, new_min, new_max, null, state.continuation_pc, state.continuation_op);
                                if (new_min != 0) {
                                    currentContinuation_op = REOP_MINIMALREPEAT;
                                    currentContinuation_pc = pc;
                                    int parenCount = getIndex (program, pc);
                                    pc += INDEX_LEN;
                                    int parenIndex = getIndex (program, pc);
                                    pc += 2 * INDEX_LEN;
                                    for (int k = 0; k < parenCount; k++) {
                                        gData.set_parens (parenIndex + k, -1, 0);
                                    }
                                    op = program [pc++];
                                }
                                else {
                                    currentContinuation_pc = state.continuation_pc;
                                    currentContinuation_op = state.continuation_op;
                                    pushBackTrackState (gData, REOP_MINIMALREPEAT, pc);
                                    popProgState (gData);
                                    pc += 2 * INDEX_LEN;
                                    pc = pc + getOffset (program, pc);
                                    op = program [pc++];
                                }
                                continue;
                            }
                        }


                    case REOP_END:
                        return true;


                    default:
                        throw Context.CodeBug ();

                }
                /*
                *  If the match failed and there's a backtrack option, take it.
                *  Otherwise this is a complete and utter failure.
                */
                if (!result) {
                    REBackTrackData backTrackData = gData.backTrackStackTop;
                    if (backTrackData != null) {
                        gData.backTrackStackTop = backTrackData.previous;

                        gData.lastParen = backTrackData.lastParen;

                        // TODO: If backTrackData will no longer be used, then
                        // TODO: there is no need to clone backTrackData.parens
                        if (backTrackData.parens != null) {
                            gData.parens = new long [backTrackData.parens.Length];
                            backTrackData.parens.CopyTo (gData.parens, 0);
                        }

                        gData.cp = backTrackData.cp;

                        gData.stateStackTop = backTrackData.stateStackTop;

                        currentContinuation_op = gData.stateStackTop.continuation_op;
                        currentContinuation_pc = gData.stateStackTop.continuation_pc;
                        pc = backTrackData.continuation_pc;
                        op = backTrackData.continuation_op;
                        continue;
                    }
                    else
                        return false;
                }

                op = program [pc++];
            }
        }

        private static bool matchRegExp (REGlobalData gData, RECompiled re, char [] chars, int start, int end, bool multiline)
        {
            if (re.parenCount != 0) {
                gData.parens = new long [re.parenCount];
            }
            else {
                gData.parens = null;
            }

            gData.backTrackStackTop = null;

            gData.stateStackTop = null;

            gData.multiline = multiline;
            gData.regexp = re;
            gData.lastParen = 0;

            int anchorCh = gData.regexp.anchorCh;
            //
            // have to include the position beyond the last character
            //  in order to detect end-of-input/line condition
            //
            for (int i = start; i <= end; ++i) {
                //
                // If the first node is a literal match, step the index into
                // the string until that match is made, or fail if it can't be
                // found at all.
                //
                if (anchorCh >= 0) {
                    for (; ; ) {
                        if (i == end) {
                            return false;
                        }
                        char matchCh = chars [i];
                        if (matchCh == anchorCh || ((gData.regexp.flags & JSREG_FOLD) != 0 && upcase (matchCh) == upcase ((char)anchorCh))) {
                            break;
                        }
                        ++i;
                    }
                }
                gData.cp = i;
                for (int j = 0; j < re.parenCount; j++) {
                    gData.set_parens (j, -1, 0);
                }
                bool result = executeREBytecode (gData, chars, end);

                gData.backTrackStackTop = null;
                gData.stateStackTop = null;
                if (result) {
                    gData.skipped = i - start;
                    return true;
                }
            }
            return false;
        }

        /*
        * indexp is assumed to be an array of length 1
        */
        internal virtual object executeRegExp (Context cx, IScriptable scopeObj, RegExpImpl res, string str, int [] indexp, int matchType)
        {
            REGlobalData gData = new REGlobalData ();

            int start = indexp [0];
            char [] charArray = str.ToCharArray ();
            int end = charArray.Length;
            if (start > end)
                start = end;
            //
            // Call the recursive matcher to do the real work.
            //
            bool matches = matchRegExp (gData, re, charArray, start, end, res.multiline);
            if (!matches) {
                if (matchType != PREFIX)
                    return null;
                return Undefined.Value;
            }
            int index = gData.cp;
            int i = index;
            indexp [0] = i;
            int matchlen = i - (start + gData.skipped);
            int ep = index;
            index -= matchlen;
            object result;
            IScriptable obj;

            if (matchType == TEST) {
                /*
                * Testing for a match and updating cx.regExpImpl: don't allocate
                * an array object, do return true.
                */
                result = true;
                obj = null;
            }
            else {
                /*
                * The array returned on match has element 0 bound to the matched
                * string, elements 1 through re.parenCount bound to the paren
                * matches, an index property telling the length of the left context,
                * and an input property referring to the input string.
                */
                IScriptable scope = GetTopLevelScope (scopeObj);
                result = ScriptRuntime.NewObject (cx, scope, "Array", null);
                obj = (IScriptable)result;

                string matchstr = new string (charArray, index, matchlen);
                obj.Put (0, obj, matchstr);
            }

            if (re.parenCount == 0) {
                res.parens = null;
                res.lastParen = SubString.EmptySubString;
            }
            else {
                SubString parsub = null;
                int num;
                res.parens = new SubString [re.parenCount];
                for (num = 0; num < re.parenCount; num++) {
                    int cap_index = gData.parens_index (num);
                    string parstr;
                    if (cap_index != -1) {
                        int cap_length = gData.parens_length (num);
                        parsub = new SubString (charArray, cap_index, cap_length);
                        res.parens [num] = parsub;
                        if (matchType == TEST)
                            continue;
                        parstr = parsub.ToString ();
                        obj.Put (num + 1, obj, parstr);
                    }
                    else {
                        if (matchType != TEST)
                            obj.Put (num + 1, obj, Undefined.Value);
                    }
                }
                res.lastParen = parsub;
            }

            if (!(matchType == TEST)) {
                /*
                * Define the index and input properties last for better for/in loop
                * order (so they come after the elements).
                */
                obj.Put ("index", obj, (object)(start + gData.skipped));
                obj.Put ("input", obj, str);
            }

            if (res.lastMatch == null) {
                res.lastMatch = new SubString ();
                res.leftContext = new SubString ();
                res.rightContext = new SubString ();
            }
            res.lastMatch.charArray = charArray;
            res.lastMatch.index = index;
            res.lastMatch.length = matchlen;

            res.leftContext.charArray = charArray;
            if (cx.Version == Context.Versions.JS1_2) {
                /*
                * JS1.2 emulated Perl4.0.1.8 (patch level 36) for global regexps used
                * in scalar contexts, and unintentionally for the string.match "list"
                * psuedo-context.  On "hi there bye", the following would result:
                *
                * Language     while(/ /g){print("$`");}   s/ /$`/g
                * perl4.036    "hi", "there"               "hihitherehi therebye"
                * perl5        "hi", "hi there"            "hihitherehi therebye"
                * js1.2        "hi", "there"               "hihitheretherebye"
                *
                * Insofar as JS1.2 always defined $` as "left context from the last
                * match" for global regexps, it was more consistent than perl4.
                */
                res.leftContext.index = start;
                res.leftContext.length = gData.skipped;
            }
            else {
                /*
                * For JS1.3 and ECMAv2, emulate Perl5 exactly:
                *
                * js1.3        "hi", "hi there"            "hihitherehi therebye"
                */
                res.leftContext.index = 0;
                res.leftContext.length = start + gData.skipped;
            }

            res.rightContext.charArray = charArray;
            res.rightContext.index = ep;
            res.rightContext.length = end - ep;

            return result;
        }

        private static void reportError (string messageId, string arg)
        {
            string msg = ScriptRuntime.GetMessage (messageId, arg);
            throw ScriptRuntime.ConstructError ("SyntaxError", msg);
        }

        #region InstanceIds
        private const int Id_lastIndex = 1;
        private const int Id_source = 2;
        private const int Id_global = 3;
        private const int Id_ignoreCase = 4;
        private const int Id_multiline = 5;
        private const int MAX_INSTANCE_ID = 5;
        #endregion

        protected internal override int FindInstanceIdInfo (string s)
        {
            int id;
            #region Generated InstanceId Switch
        L0: {
                id = 0;
                string X = null;
                int c;
                int s_length = s.Length;
                if (s_length == 6) {
                    c = s [0];
                    if (c == 'g') { X = "global"; id = Id_global; }
                    else if (c == 's') { X = "source"; id = Id_source; }
                }
                else if (s_length == 9) {
                    c = s [0];
                    if (c == 'l') { X = "lastIndex"; id = Id_lastIndex; }
                    else if (c == 'm') { X = "multiline"; id = Id_multiline; }
                }
                else if (s_length == 10) { X = "ignoreCase"; id = Id_ignoreCase; }
                if (X != null && X != s && !X.Equals (s))
                    id = 0;
            }
        EL0:

            #endregion
            // #/string_id_map#

            if (id == 0)
                return base.FindInstanceIdInfo (s);

            int attr;
            switch (id) {

                case Id_lastIndex:
                    attr = PERMANENT | DONTENUM;
                    break;

                case Id_source:
                case Id_global:
                case Id_ignoreCase:
                case Id_multiline:
                    attr = PERMANENT | READONLY | DONTENUM;
                    break;

                default:
                    throw new ApplicationException ();

            }
            return InstanceIdInfo (attr, id);
        }

        protected internal override string GetInstanceIdName (int id)
        {
            switch (id) {

                case Id_lastIndex:
                    return "lastIndex";

                case Id_source:
                    return "source";

                case Id_global:
                    return "global";

                case Id_ignoreCase:
                    return "ignoreCase";

                case Id_multiline:
                    return "multiline";
            }
            return base.GetInstanceIdName (id);
        }

        protected internal override object GetInstanceIdValue (int id)
        {
            switch (id) {

                case Id_lastIndex:
                    return (lastIndex);

                case Id_source:
                    return new string (re.source);

                case Id_global:
                    return (re.flags & JSREG_GLOB) != 0;

                case Id_ignoreCase:
                    return (re.flags & JSREG_FOLD) != 0;

                case Id_multiline:
                    return (re.flags & JSREG_MULTILINE) != 0;
            }
            return base.GetInstanceIdValue (id);
        }

        protected internal override void SetInstanceIdValue (int id, object value)
        {
            if (id == Id_lastIndex) {
                lastIndex = ScriptConvert.ToNumber (value);
                return;
            }
            base.SetInstanceIdValue (id, value);
        }

        protected internal override void InitPrototypeId (int id)
        {
            string s;
            int arity;
            switch (id) {

                case Id_compile:
                    arity = 1;
                    s = "compile";
                    break;

                case Id_toString:
                    arity = 0;
                    s = "toString";
                    break;

                case Id_toSource:
                    arity = 0;
                    s = "toSource";
                    break;

                case Id_exec:
                    arity = 1;
                    s = "exec";
                    break;

                case Id_test:
                    arity = 1;
                    s = "test";
                    break;

                case Id_prefix:
                    arity = 1;
                    s = "prefix";
                    break;

                default:
                    throw new ArgumentException (Convert.ToString (id));

            }
            InitPrototypeMethod (REGEXP_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (REGEXP_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;
            switch (id) {

                case Id_compile:
                    return realThis (thisObj, f).compile (cx, scope, args);


                case Id_toString:
                case Id_toSource:
                    return realThis (thisObj, f).ToString ();


                case Id_exec:
                    return realThis (thisObj, f).execSub (cx, scope, args, MATCH);


                case Id_test: {
                        object x = realThis (thisObj, f).execSub (cx, scope, args, TEST);
                        return true.Equals (x) ? true : false;
                    }


                case Id_prefix:
                    return realThis (thisObj, f).execSub (cx, scope, args, PREFIX);
            }
            throw new ArgumentException (Convert.ToString (id));
        }

        private static BuiltinRegExp realThis (IScriptable thisObj, IdFunctionObject f)
        {
            if (!(thisObj is BuiltinRegExp))
                throw IncompatibleCallError (f);
            return (BuiltinRegExp)thisObj;
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
                    case 4:
                        c = s [0];
                        if (c == 'e') { X = "exec"; id = Id_exec; }
                        else if (c == 't') { X = "test"; id = Id_test; }
                        break;
                    case 6:
                        X = "prefix";
                        id = Id_prefix;
                        break;
                    case 7:
                        X = "compile";
                        id = Id_compile;
                        break;
                    case 8:
                        c = s [3];
                        if (c == 'o') { X = "toSource"; id = Id_toSource; }
                        else if (c == 't') { X = "toString"; id = Id_toString; }
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
        private const int Id_compile = 1;
        private const int Id_toString = 2;
        private const int Id_toSource = 3;
        private const int Id_exec = 4;
        private const int Id_test = 5;
        private const int Id_prefix = 6;
        private const int MAX_PROTOTYPE_ID = 6;
        #endregion

        private RECompiled re;
        internal double lastIndex; /* index after last match, for //g iterator */

    }
    // class NativeRegExp


    class RECompiled
    {


        internal char [] source; /* locked source string, sans // */
        internal int parenCount; /* number of parenthesized submatches */
        internal int flags; /* flags  */
        internal sbyte [] program; /* regular expression bytecode */
        internal int classCount; /* count [...] bitmaps */
        internal RECharSet [] classList; /* list of [...] bitmaps */
        internal int anchorCh = -1; /* if >= 0, then re starts with this literal char */
    }

    class RENode
    {

        internal RENode (sbyte op)
        {
            this.op = op;
        }

        internal sbyte op; /* r.e. op bytecode */
        internal RENode next; /* next in concatenation order */
        internal RENode kid; /* first operand */

        internal RENode kid2; /* second operand */
        internal int parenIndex; /* or a parenthesis index */

        /* or a range */
        internal int min;
        internal int max;
        internal int parenCount;
        internal bool greedy;

        /* or a character class */
        internal int startIndex;
        internal int kidlen; /* length of string at kid, in chars */
        internal int bmsize; /* bitmap size, based on max char code */
        internal int index; /* index into class list */

        /* or a literal sequence */
        internal char chr; /* of one character */
        internal int length; /* or many (via the index) */
        internal int flatIndex; /* which is -1 if not sourced */

        public override string ToString ()
        {
            return "[RENode: Op: " + op + "]";
        }
    }

    class CompilerState
    {

        internal CompilerState (char [] source, int length, int flags)
        {
            this.cpbegin = source;
            this.cp = 0;
            this.cpend = length;
            this.flags = flags;
            this.parenCount = 0;
            this.classCount = 0;
            this.progLength = 0;
        }

        internal char [] cpbegin;
        internal int cpend;
        internal int cp;
        internal int flags;
        internal int parenCount;
        internal int parenNesting;
        internal int classCount; /* number of [] encountered */
        internal int progLength; /* estimated bytecode length */
        internal RENode result;
    }

    class REProgState
    {
        internal REProgState (REProgState previous, int min, int max, int index, REBackTrackData backTrack, int continuation_pc, int continuation_op)
        {
            this.previous = previous;
            this.min = min;
            this.max = max;
            this.index = index;
            this.continuation_op = continuation_op;
            this.continuation_pc = continuation_pc;
            this.backTrack = backTrack;
        }

        internal REProgState previous; // previous state in stack

        internal int min; /* current quantifier min */
        internal int max; /* current quantifier max */
        internal int index; /* progress in text */
        internal int continuation_op;
        internal int continuation_pc;
        internal REBackTrackData backTrack; // used by ASSERT_  to recover state
    }

    class REBackTrackData
    {

        internal REBackTrackData (REGlobalData gData, int op, int pc)
        {
            previous = gData.backTrackStackTop;
            continuation_op = op;
            continuation_pc = pc;
            lastParen = gData.lastParen;
            if (gData.parens != null) {
                parens = new long [gData.parens.Length];
                gData.parens.CopyTo (parens, 0);
            }
            cp = gData.cp;
            stateStackTop = gData.stateStackTop;
        }

        internal REBackTrackData previous;

        internal int continuation_op; /* where to backtrack to */
        internal int continuation_pc;
        internal int lastParen;
        internal long [] parens; /* parenthesis captures */
        internal int cp; /* char buffer index */
        internal REProgState stateStackTop; /* state of op that backtracked */
    }

    class REGlobalData
    {
        internal bool multiline;
        internal RECompiled regexp; /* the RE in execution */
        internal int lastParen; /* highest paren set so far */
        internal int skipped; /* chars skipped anchoring this r.e. */

        internal int cp; /* char buffer index */
        internal long [] parens; /* parens captures */

        internal REProgState stateStackTop; /* stack of state of current ancestors */

        internal REBackTrackData backTrackStackTop; /* last matched-so-far position */


        /// <summary> Get start of parenthesis capture contents, -1 for empty.</summary>
        internal virtual int parens_index (int i)
        {
            return (int)(parens [i]);
        }

        /// <summary> Get length of parenthesis capture contents.</summary>
        internal virtual int parens_length (int i)
        {
            return (int)((ulong)parens [i] >> 32);
        }

        internal virtual void set_parens (int i, int index, int length)
        {
            parens [i] = ((long)index & unchecked ((int)0xffffffffL)) | ((long)length << 32);
        }
    }

    /*
    * This struct holds a bitmap representation of a class from a regexp.
    * There's a list of these referenced by the classList field in the NativeRegExp
    * struct below. The initial state has startIndex set to the offset in the
    * original regexp source of the beginning of the class contents. The first
    * use of the class converts the source representation into a bitmap.
    *
    */

    sealed class RECharSet
    {


        internal RECharSet (int length, int startIndex, int strlength)
        {
            this.length = length;
            this.startIndex = startIndex;
            this.strlength = strlength;
        }

        internal int length;
        internal int startIndex;
        internal int strlength;


        internal volatile bool converted;

        internal volatile bool sense;

        internal volatile sbyte [] bits;
    }
}
