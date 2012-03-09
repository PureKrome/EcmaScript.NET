//------------------------------------------------------------------------------
// <license file="RegExpImpl.cs">
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

using EcmaScript.NET;

namespace EcmaScript.NET.Types.RegExp
{

    /// <summary> </summary>
    public class RegExpImpl : RegExpProxy
    {

        public virtual bool IsRegExp (IScriptable obj)
        {
            return obj is BuiltinRegExp;
        }

        public virtual object Compile (Context cx, string source, string flags)
        {
            return BuiltinRegExp.compileRE (source, flags, false);
        }

        public virtual IScriptable Wrap (Context cx, IScriptable scope, object compiled)
        {
            return new BuiltinRegExp (scope, compiled);
        }

        public virtual object Perform (Context cx, IScriptable scope, IScriptable thisObj, object [] args, RegExpActions actionType)
        {
            GlobData data = new GlobData ();
            data.mode = actionType;

            switch ( (RegExpActions)actionType) {

                case EcmaScript.NET.RegExpActions.Match: {
                        object rval;
                        data.optarg = 1;
                        rval = matchOrReplace (cx, scope, thisObj, args, this, data, false);
                        return data.arrayobj == null ? rval : data.arrayobj;
                    }


                case EcmaScript.NET.RegExpActions.Search:
                    data.optarg = 1;
                    return matchOrReplace (cx, scope, thisObj, args, this, data, false);


                case EcmaScript.NET.RegExpActions.Replace: {
                        object arg1 = args.Length < 2 ? Undefined.Value : args [1];
                        string repstr = null;
                        IFunction lambda = null;
                        if (arg1 is IFunction) {
                            lambda = (IFunction)arg1;
                        }
                        else {
                            repstr = ScriptConvert.ToString (arg1);
                        }

                        data.optarg = 2;
                        data.lambda = lambda;
                        data.repstr = repstr;
                        data.dollar = repstr == null ? -1 : repstr.IndexOf ((char)'$');
                        data.charBuf = null;
                        data.leftIndex = 0;
                        object val = matchOrReplace (cx, scope, thisObj, args, this, data, true);
                        SubString rc = this.rightContext;

                        if (data.charBuf == null) {
                            if (data.global || val == null || !val.Equals (true)) {
                                /* Didn't match even once. */
                                return data.str;
                            }
                            SubString lc = this.leftContext;
                            replace_glob (data, cx, scope, this, lc.index, lc.length);
                        }
                        data.charBuf.Append (rc.charArray, rc.index, rc.length);
                        return data.charBuf.ToString ();
                    }


                default:
                    throw Context.CodeBug ();

            }
        }

        /// <summary> Analog of C match_or_replace.</summary>
        private static object matchOrReplace (Context cx, IScriptable scope, IScriptable thisObj, object [] args, RegExpImpl reImpl, GlobData data, bool forceFlat)
        {
            BuiltinRegExp re;

            string str = ScriptConvert.ToString (thisObj);
            data.str = str;
            IScriptable topScope = ScriptableObject.GetTopLevelScope (scope);

            if (args.Length == 0) {
                object compiled = BuiltinRegExp.compileRE ("", "", false);
                re = new BuiltinRegExp (topScope, compiled);
            }
            else if (args [0] is BuiltinRegExp) {
                re = (BuiltinRegExp)args [0];
            }
            else {
                string src = ScriptConvert.ToString (args [0]);
                string opt;
                if (data.optarg < args.Length) {
                    args [0] = src;
                    opt = ScriptConvert.ToString (args [data.optarg]);
                }
                else {
                    opt = null;
                }
                object compiled = BuiltinRegExp.compileRE (src, opt, forceFlat);
                re = new BuiltinRegExp (topScope, compiled);
            }
            data.regexp = re;

            data.global = (re.Flags & BuiltinRegExp.JSREG_GLOB) != 0;
            int [] indexp = new int [] { 0 };
            object result = null;
            if (data.mode == EcmaScript.NET.RegExpActions.Search) {
                result = re.executeRegExp (cx, scope, reImpl, str, indexp, BuiltinRegExp.TEST);
                if (result != null && result.Equals (true))
                    result = (int)reImpl.leftContext.length;
                else
                    result = -1;
            }
            else if (data.global) {
                re.lastIndex = 0;
                for (int count = 0; indexp [0] <= str.Length; count++) {
                    result = re.executeRegExp (cx, scope, reImpl, str, indexp, BuiltinRegExp.TEST);
                    if (result == null || !result.Equals (true))
                        break;
                    if (data.mode == EcmaScript.NET.RegExpActions.Match) {
                        match_glob (data, cx, scope, count, reImpl);
                    }
                    else {
                        if (data.mode != EcmaScript.NET.RegExpActions.Replace)
                            Context.CodeBug ();
                        SubString lastMatch = reImpl.lastMatch;
                        int leftIndex = data.leftIndex;
                        int leftlen = lastMatch.index - leftIndex;
                        data.leftIndex = lastMatch.index + lastMatch.length;
                        replace_glob (data, cx, scope, reImpl, leftIndex, leftlen);
                    }
                    if (reImpl.lastMatch.length == 0) {
                        if (indexp [0] == str.Length)
                            break;
                        indexp [0]++;
                    }
                }
            }
            else {
                result = re.executeRegExp (cx, scope, reImpl, str, indexp, ((data.mode == EcmaScript.NET.RegExpActions.Replace) ? BuiltinRegExp.TEST : BuiltinRegExp.MATCH));
            }

            return result;
        }



        public virtual int FindSplit (Context cx, IScriptable scope, string target, string separator, IScriptable reObj, int [] ip, int [] matchlen, bool [] matched, string [] [] parensp)
        {
            int i = ip [0];
            int length = target.Length;
            int result;

            Context.Versions version = cx.Version;
            BuiltinRegExp re = (BuiltinRegExp)reObj;

            while (true) {
                // imitating C label
                /* JS1.2 deviated from Perl by never matching at end of string. */
                int ipsave = ip [0]; // reuse ip to save object creation
                ip [0] = i;
                object ret = re.executeRegExp (cx, scope, this, target, ip, BuiltinRegExp.TEST);
                if (ret == null || !ret.Equals (true)) {
                    // Mismatch: ensure our caller advances i past end of string.
                    ip [0] = ipsave;
                    matchlen [0] = 1;
                    matched [0] = false;
                    return length;
                }
                i = ip [0];
                ip [0] = ipsave;
                matched [0] = true;

                SubString sep = this.lastMatch;
                matchlen [0] = sep.length;
                if (matchlen [0] == 0) {
                    /*
                    * Empty string match: never split on an empty
                    * match at the start of a find_split cycle.  Same
                    * rule as for an empty global match in
                    * match_or_replace.
                    */
                    if (i == ip [0]) {
                        /*
                        * "Bump-along" to avoid sticking at an empty
                        * match, but don't bump past end of string --
                        * our caller must do that by adding
                        * sep->length to our return value.
                        */
                        if (i == length) {
                            if (version == Context.Versions.JS1_2) {
                                matchlen [0] = 1;
                                result = i;
                            }
                            else
                                result = -1;
                            break;
                        }
                        i++;

                        goto again; // imitating C goto
                    }
                }
                // PR_ASSERT((size_t)i >= sep->length);
                result = i - matchlen [0];
                break;


            again:
                ;
            }
            int size = (parens == null) ? 0 : parens.Length;
            parensp [0] = new string [size];
            for (int num = 0; num < size; num++) {
                SubString parsub = getParenSubString (num);
                parensp [0] [num] = parsub.ToString ();
            }
            return result;
        }

        /// <summary> Analog of REGEXP_PAREN_SUBSTRING in C jsregexp.h.
        /// Assumes zero-based; i.e., for $3, i==2
        /// </summary>
        internal virtual SubString getParenSubString (int i)
        {
            if (parens != null && i < parens.Length) {
                SubString parsub = parens [i];
                if (parsub != null) {
                    return parsub;
                }
            }
            return SubString.EmptySubString;
        }

        /*
        * Analog of match_glob() in jsstr.c
        */
        private static void match_glob (GlobData mdata, Context cx, IScriptable scope, int count, RegExpImpl reImpl)
        {
            if (mdata.arrayobj == null) {
                IScriptable s = ScriptableObject.GetTopLevelScope (scope);
                mdata.arrayobj = ScriptRuntime.NewObject (cx, s, "Array", null);
            }
            SubString matchsub = reImpl.lastMatch;
            string matchstr = matchsub.ToString ();
            mdata.arrayobj.Put (count, mdata.arrayobj, matchstr);
        }

        /*
        * Analog of replace_glob() in jsstr.c
        */
        private static void replace_glob (GlobData rdata, Context cx, IScriptable scope, RegExpImpl reImpl, int leftIndex, int leftlen)
        {
            int replen;
            string lambdaStr;
            if (rdata.lambda != null) {
                // invoke lambda function with args lastMatch, $1, $2, ... $n,
                // leftContext.length, whole string.
                SubString [] parens = reImpl.parens;
                int parenCount = (parens == null) ? 0 : parens.Length;
                object [] args = new object [parenCount + 3];
                args [0] = reImpl.lastMatch.ToString ();
                for (int i = 0; i < parenCount; i++) {
                    SubString sub = parens [i];
                    if (sub != null) {
                        args [i + 1] = sub.ToString ();
                    }
                    else {
                        args [i + 1] = Undefined.Value;
                    }
                }
                args [parenCount + 1] = (int)reImpl.leftContext.length;
                args [parenCount + 2] = rdata.str;
                // This is a hack to prevent expose of reImpl data to
                // JS function which can run new regexps modifing
                // regexp that are used later by the engine.
                // TODO: redesign is necessary
                if (reImpl != cx.RegExpProxy)
                    Context.CodeBug ();
                RegExpImpl re2 = new RegExpImpl ();
                re2.multiline = reImpl.multiline;
                re2.input = reImpl.input;
                cx.RegExpProxy = re2;
                try {
                    IScriptable parent = ScriptableObject.GetTopLevelScope (scope);
                    object result = rdata.lambda.Call (cx, parent, parent, args);
                    lambdaStr = ScriptConvert.ToString (result);
                }
                finally {
                    cx.RegExpProxy = reImpl;
                }
                replen = lambdaStr.Length;
            }
            else {
                lambdaStr = null;
                replen = rdata.repstr.Length;
                if (rdata.dollar >= 0) {
                    int [] skip = new int [1];
                    int dp = rdata.dollar;
                    do {
                        SubString sub = interpretDollar (cx, reImpl, rdata.repstr, dp, skip);
                        if (sub != null) {
                            replen += sub.length - skip [0];
                            dp += skip [0];
                        }
                        else {
                            ++dp;
                        }
                        dp = rdata.repstr.IndexOf ((char)'$', dp);
                    }
                    while (dp >= 0);
                }
            }

            int growth = leftlen + replen + reImpl.rightContext.length;
            System.Text.StringBuilder charBuf = rdata.charBuf;
            if (charBuf == null) {
                charBuf = new System.Text.StringBuilder (growth);
                rdata.charBuf = charBuf;
            }
            else {
                charBuf.EnsureCapacity (rdata.charBuf.Length + growth);
            }

            charBuf.Append (reImpl.leftContext.charArray, leftIndex, leftlen);
            if (rdata.lambda != null) {
                charBuf.Append (lambdaStr);
            }
            else {
                do_replace (rdata, cx, reImpl);
            }
        }

        private static SubString interpretDollar (Context cx, RegExpImpl res, string da, int dp, int [] skip)
        {
            char dc;
            int num, tmp;

            if (da [dp] != '$')
                Context.CodeBug ();

            /* Allow a real backslash (literal "\\") to escape "$1" etc. */
            Context.Versions version = cx.Version;
            if (version != Context.Versions.Default && version <= Context.Versions.JS1_4) {
                if (dp > 0 && da [dp - 1] == '\\')
                    return null;
            }
            int daL = da.Length;
            if (dp + 1 >= daL)
                return null;
            /* Interpret all Perl match-induced dollar variables. */
            dc = da [dp + 1];
            if (BuiltinRegExp.isDigit (dc)) {
                int cp;
                if (version != Context.Versions.Default && version <= Context.Versions.JS1_4) {
                    if (dc == '0')
                        return null;
                    /* Check for overflow to avoid gobbling arbitrary decimal digits. */
                    num = 0;
                    cp = dp;
                    while (++cp < daL && BuiltinRegExp.isDigit (dc = da [cp])) {
                        tmp = 10 * num + (dc - '0');
                        if (tmp < num)
                            break;
                        num = tmp;
                    }
                }
                else {
                    /* ECMA 3, 1-9 or 01-99 */
                    int parenCount = (res.parens == null) ? 0 : res.parens.Length;
                    num = dc - '0';
                    if (num > parenCount)
                        return null;
                    cp = dp + 2;
                    if ((dp + 2) < daL) {
                        dc = da [dp + 2];
                        if (BuiltinRegExp.isDigit (dc)) {
                            tmp = 10 * num + (dc - '0');
                            if (tmp <= parenCount) {
                                cp++;
                                num = tmp;
                            }
                        }
                    }
                    if (num == 0)
                        return null; /* $0 or $00 is not valid */
                }
                /* Adjust num from 1 $n-origin to 0 array-index-origin. */
                num--;
                skip [0] = cp - dp;
                return res.getParenSubString (num);
            }

            skip [0] = 2;
            switch (dc) {

                case '$':
                    return new SubString ("$");

                case '&':
                    return res.lastMatch;

                case '+':
                    return res.lastParen;

                case '`':
                    if (version == Context.Versions.JS1_2) {
                        /*
                        * JS1.2 imitated the Perl4 bug where left context at each step
                        * in an iterative use of a global regexp started from last match,
                        * not from the start of the target string.  But Perl4 does start
                        * $` at the beginning of the target string when it is used in a
                        * substitution, so we emulate that special case here.
                        */
                        res.leftContext.index = 0;
                        res.leftContext.length = res.lastMatch.index;
                    }
                    return res.leftContext;

                case '\'':
                    return res.rightContext;
            }
            return null;
        }

        /// <summary> Analog of do_replace in jsstr.c</summary>
        private static void do_replace (GlobData rdata, Context cx, RegExpImpl regExpImpl)
        {
            System.Text.StringBuilder charBuf = rdata.charBuf;
            int cp = 0;
            string da = rdata.repstr;
            int dp = rdata.dollar;
            if (dp != -1) {
                int [] skip = new int [1];
                do {
                    int len = dp - cp;
                    charBuf.Append (da.Substring (cp, (dp) - (cp)));
                    cp = dp;
                    SubString sub = interpretDollar (cx, regExpImpl, da, dp, skip);
                    if (sub != null) {
                        len = sub.length;
                        if (len > 0) {
                            charBuf.Append (sub.charArray, sub.index, len);
                        }
                        cp += skip [0];
                        dp += skip [0];
                    }
                    else {
                        ++dp;
                    }
                    dp = da.IndexOf ((char)'$', dp);
                }
                while (dp >= 0);
            }
            int daL = da.Length;
            if (daL > cp) {
                charBuf.Append (da.Substring (cp, (daL) - (cp)));
            }
        }

        internal string input; /* input string to match (perl $_, GC root) */
        internal bool multiline; /* whether input contains newlines (perl $*) */
        internal SubString [] parens; /* Vector of SubString; last set of parens
		matched (perl $1, $2) */
        internal SubString lastMatch; /* last string matched (perl $&) */
        internal SubString lastParen; /* last paren matched (perl $+) */
        internal SubString leftContext; /* input to left of last match (perl $`) */
        internal SubString rightContext; /* input to right of last match (perl $') */
    }


    sealed class GlobData
    {
        internal RegExpActions mode = RegExpActions.None; /* input: return index, match object, or void */
        internal int optarg; /* input: index of optional flags argument */
        internal bool global; /* output: whether regexp was global */
        internal string str; /* output: 'this' parameter object as string */
        internal BuiltinRegExp regexp; /* output: regexp parameter object private data */

        // match-specific data

        internal IScriptable arrayobj;

        // replace-specific data

        internal IFunction lambda; /* replacement function object or null */
        internal string repstr; /* replacement string */
        internal int dollar = -1; /* -1 or index of first $ in repstr */
        internal System.Text.StringBuilder charBuf; /* result characters, null initially */
        internal int leftIndex; /* leftContext index, always 0 for JS1.2 */
    }
}