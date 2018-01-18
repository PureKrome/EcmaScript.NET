//------------------------------------------------------------------------------
// <license file="NativeRegExpCtor.cs">
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

    /// <summary> This class implements the RegExp constructor native object.
    /// 
    /// Revision History:
    /// Implementation in C by Brendan Eich
    /// Initial port to Java by Norris Boyd from jsregexp.c version 1.36
    /// Merged up to version 1.38, which included Unicode support.
    /// Merged bug fixes in version 1.39.
    /// Merged JSFUN13_BRANCH changes up to 1.32.2.11
    /// 
    /// </summary>
    class BuiltinRegExpCtor : BaseFunction
    {
        override public string FunctionName
        {
            get
            {
                return "RegExp";
            }

        }
        private static RegExpImpl Impl
        {
            get
            {
                Context cx = Context.CurrentContext;
                return (RegExpImpl)cx.RegExpProxy;
            }

        }
        override protected internal int MaxInstanceId
        {
            get
            {
                return base.MaxInstanceId + MAX_INSTANCE_ID;
            }

        }


        internal BuiltinRegExpCtor ()
        {
        }

        public override object Call (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (args.Length > 0 && args [0] is BuiltinRegExp && (args.Length == 1 || args [1] == Undefined.Value)) {
                return args [0];
            }
            return Construct (cx, scope, args);
        }

        public override IScriptable Construct (Context cx, IScriptable scope, object [] args)
        {
            BuiltinRegExp re = new BuiltinRegExp ();
            re.compile (cx, scope, args);
            ScriptRuntime.setObjectProtoAndParent (re, scope);
            return re;
        }

        #region InstanceIds
        private const int Id_multiline = 1;
        private const int Id_STAR = 2;
        private const int Id_input = 3;
        private const int Id_UNDERSCORE = 4;
        private const int Id_lastMatch = 5;
        private const int Id_AMPERSAND = 6;
        private const int Id_lastParen = 7;
        private const int Id_PLUS = 8;
        private const int Id_leftContext = 9;
        private const int Id_BACK_QUOTE = 10;
        private const int Id_rightContext = 11;
        private const int Id_QUOTE = 12;
        private const int DOLLAR_ID_BASE = 12;
        private const int Id_DOLLAR_1 = 13;
        private const int Id_DOLLAR_2 = 14;
        private const int Id_DOLLAR_3 = 15;
        private const int Id_DOLLAR_4 = 16;
        private const int Id_DOLLAR_5 = 17;
        private const int Id_DOLLAR_6 = 18;
        private const int Id_DOLLAR_7 = 19;
        private const int Id_DOLLAR_8 = 20;
        private const int Id_DOLLAR_9 = 21;
        private const int MAX_INSTANCE_ID = 21;
        #endregion

        protected internal override int FindInstanceIdInfo (string s)
        {
            int id;
            #region Generated InstanceId Switch
	L0: { id = 0; string X = null; int c;
	    L: switch (s.Length) {
	    case 2: switch (s[1]) {
		case '&': if (s[0]=='$') {id=Id_AMPERSAND; goto EL0;} break;
		case '\'': if (s[0]=='$') {id=Id_QUOTE; goto EL0;} break;
		case '*': if (s[0]=='$') {id=Id_STAR; goto EL0;} break;
		case '+': if (s[0]=='$') {id=Id_PLUS; goto EL0;} break;
		case '1': if (s[0]=='$') {id=Id_DOLLAR_1; goto EL0;} break;
		case '2': if (s[0]=='$') {id=Id_DOLLAR_2; goto EL0;} break;
		case '3': if (s[0]=='$') {id=Id_DOLLAR_3; goto EL0;} break;
		case '4': if (s[0]=='$') {id=Id_DOLLAR_4; goto EL0;} break;
		case '5': if (s[0]=='$') {id=Id_DOLLAR_5; goto EL0;} break;
		case '6': if (s[0]=='$') {id=Id_DOLLAR_6; goto EL0;} break;
		case '7': if (s[0]=='$') {id=Id_DOLLAR_7; goto EL0;} break;
		case '8': if (s[0]=='$') {id=Id_DOLLAR_8; goto EL0;} break;
		case '9': if (s[0]=='$') {id=Id_DOLLAR_9; goto EL0;} break;
		case '_': if (s[0]=='$') {id=Id_UNDERSCORE; goto EL0;} break;
		} break;
	    case 5: X="input";id=Id_input; break;
	    case 9: c=s[4];
		if (c=='M') { X="lastMatch";id=Id_lastMatch; }
		else if (c=='P') { X="lastParen";id=Id_lastParen; }
		else if (c=='i') { X="multiline";id=Id_multiline; }
		break;
	    case 10: X="BACK_QUOTE";id=Id_BACK_QUOTE; break;
	    case 11: X="leftContext";id=Id_leftContext; break;
	    case 12: X="rightContext";id=Id_rightContext; break;
	    }
	    if (X!=null && X!=s && !X.Equals(s)) id = 0;
	}
	EL0:

            #endregion

            if (id == 0)
                return base.FindInstanceIdInfo (s);

            int attr;
            switch (id) {

                case Id_multiline:
                case Id_STAR:
                case Id_input:
                case Id_UNDERSCORE:
                    attr = PERMANENT;
                    break;

                default:
                    attr = PERMANENT | READONLY;
                    break;

            }

            return InstanceIdInfo (attr, base.MaxInstanceId + id);
        }

        // #/string_id_map#

        protected internal override string GetInstanceIdName (int id)
        {
            int shifted = id - base.MaxInstanceId;
            if (1 <= shifted && shifted <= MAX_INSTANCE_ID) {
                switch (shifted) {

                    case Id_multiline:
                        return "multiline";

                    case Id_STAR:
                        return "$*";


                    case Id_input:
                        return "input";

                    case Id_UNDERSCORE:
                        return "$_";


                    case Id_lastMatch:
                        return "lastMatch";

                    case Id_AMPERSAND:
                        return "$&";


                    case Id_lastParen:
                        return "lastParen";

                    case Id_PLUS:
                        return "$+";


                    case Id_leftContext:
                        return "leftContext";

                    case Id_BACK_QUOTE:
                        return "$`";


                    case Id_rightContext:
                        return "rightContext";

                    case Id_QUOTE:
                        return "$'";
                }
                // Must be one of $1..$9, convert to 0..8
                int substring_number = shifted - DOLLAR_ID_BASE - 1;
                char [] buf = new char [] { '$', (char)('1' + substring_number) };
                return new string (buf);
            }
            return base.GetInstanceIdName (id);
        }

        protected internal override object GetInstanceIdValue (int id)
        {
            int shifted = id - base.MaxInstanceId;
            if (1 <= shifted && shifted <= MAX_INSTANCE_ID) {
                RegExpImpl impl = Impl;
                object stringResult;
                switch (shifted) {

                    case Id_multiline:
                    case Id_STAR:
                        return impl.multiline;


                    case Id_input:
                    case Id_UNDERSCORE:
                        stringResult = impl.input;
                        break;


                    case Id_lastMatch:
                    case Id_AMPERSAND:
                        stringResult = impl.lastMatch;
                        break;


                    case Id_lastParen:
                    case Id_PLUS:
                        stringResult = impl.lastParen;
                        break;


                    case Id_leftContext:
                    case Id_BACK_QUOTE:
                        stringResult = impl.leftContext;
                        break;


                    case Id_rightContext:
                    case Id_QUOTE:
                        stringResult = impl.rightContext;
                        break;


                    default: {
                            // Must be one of $1..$9, convert to 0..8
                            int substring_number = shifted - DOLLAR_ID_BASE - 1;
                            stringResult = impl.getParenSubString (substring_number);
                            break;
                        }

                }
                return (stringResult == null) ? "" : stringResult.ToString ();
            }
            return base.GetInstanceIdValue (id);
        }

        protected internal override void SetInstanceIdValue (int id, object value)
        {
            int shifted = id - base.MaxInstanceId;
            switch (shifted) {

                case Id_multiline:
                case Id_STAR:
                    Impl.multiline = ScriptConvert.ToBoolean (value);
                    return;


                case Id_input:
                case Id_UNDERSCORE:
                    Impl.input = ScriptConvert.ToString (value);
                    return;
            }
            base.SetInstanceIdValue (id, value);
        }


    }
}
