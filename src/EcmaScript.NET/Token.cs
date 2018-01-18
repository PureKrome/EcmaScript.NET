//------------------------------------------------------------------------------
// <license file="Token.cs">
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

namespace EcmaScript.NET
{

    /// <summary> This class implements the JavaScript scanner.
    /// 
    /// It is based on the C source files jsscan.c and jsscan.h
    /// in the jsref package.
    /// 
    /// </summary>	
    public class Token
    {

        // debug flags
        internal static readonly bool printTrees = false; // TODO: make me a preprocessor directive

        internal static readonly bool printICode = false; // TODO: make me a preprocessor directive

        internal static readonly bool printNames = printTrees || printICode;

        /// <summary>
        /// Token types.
        /// 
        /// These values correspond to JSTokenType values in
        /// jsscan.c.
        /// </summary>
        public const int ERROR = -1;				/* well-known as the only code < EOF */
        public const int EOF = 0;					/* end of file */
        public const int EOL = 1;					/* end of line */
        public const int FIRST_BYTECODE_TOKEN = 2;
        public const int ENTERWITH = 2;
        public const int LEAVEWITH = 3;
        public const int RETURN = 4;
        public const int GOTO = 5;
        public const int IFEQ = 6;
        public const int IFNE = 7;
        public const int SETNAME = 8;
        public const int BITOR = 9;
        public const int BITXOR = 10;
        public const int BITAND = 11;
        public const int EQ = 12;
        public const int NE = 13;
        public const int LT = 14;
        public const int LE = 15;
        public const int GT = 16;
        public const int GE = 17;
        public const int LSH = 18;
        public const int RSH = 19;
        public const int URSH = 20;
        public const int ADD = 21;
        public const int SUB = 22;
        public const int MUL = 23;
        public const int DIV = 24;
        public const int MOD = 25;
        public const int NOT = 26;
        public const int BITNOT = 27;
        public const int POS = 28;
        public const int NEG = 29;
        public const int NEW = 30;
        public const int DELPROP = 31;
        public const int TYPEOF = 32;
        public const int GETPROP = 33;
        public const int SETPROP = 34;
        public const int GETELEM = 35;
        public const int SETELEM = 36;
        public const int CALL = 37;
        public const int NAME = 38;
        public const int NUMBER = 39;
        public const int STRING = 40;
        public const int NULL = 41;
        public const int THIS = 42;
        public const int FALSE = 43;
        public const int TRUE = 44;
        public const int SHEQ = 45;
        public const int SHNE = 46;
        public const int REGEXP = 47;
        public const int BINDNAME = 48;
        public const int THROW = 49;
        public const int RETHROW = 50;
        public const int IN = 51;
        public const int INSTANCEOF = 52;
        public const int LOCAL_LOAD = 53;
        public const int GETVAR = 54;
        public const int SETVAR = 55;
        public const int CATCH_SCOPE = 56;
        public const int ENUM_INIT_KEYS = 57;
        public const int ENUM_INIT_VALUES = 58;
        public const int ENUM_NEXT = 59;
        public const int ENUM_ID = 60;
        public const int THISFN = 61;
        public const int RETURN_RESULT = 62;
        public const int ARRAYLIT = 63;
        public const int OBJECTLIT = 64;
        public const int GET_REF = 65;
        public const int SET_REF = 66;
        public const int DEL_REF = 67;
        public const int REF_CALL = 68;
        public const int REF_SPECIAL = 69;
        public const int DEFAULTNAMESPACE = 70;
        public const int ESCXMLATTR = 71;
        public const int ESCXMLTEXT = 72;
        public const int REF_MEMBER = 73;
        public const int REF_NS_MEMBER = 74;
        public const int REF_NAME = 75;
        public const int REF_NS_NAME = 76; // Reference for ns::y, @ns::y@[y] etc.		
        public const int SETPROP_GETTER = 77;
        public const int SETPROP_SETTER = 78;

        // End of interpreter bytecodes		
        public const int LAST_BYTECODE_TOKEN = SETPROP_SETTER;

        public const int TRY = LAST_BYTECODE_TOKEN + 1;
        public const int SEMI = LAST_BYTECODE_TOKEN + 2;
        public const int LB = LAST_BYTECODE_TOKEN + 3;
        public const int RB = LAST_BYTECODE_TOKEN + 4;
        public const int LC = LAST_BYTECODE_TOKEN + 5;
        public const int RC = LAST_BYTECODE_TOKEN + 6;
        public const int LP = LAST_BYTECODE_TOKEN + 7;
        public const int RP = LAST_BYTECODE_TOKEN + 8;
        public const int COMMA = LAST_BYTECODE_TOKEN + 9;
        public const int ASSIGN = LAST_BYTECODE_TOKEN + 10;
        public const int ASSIGN_BITOR = LAST_BYTECODE_TOKEN + 11;
        public const int ASSIGN_BITXOR = LAST_BYTECODE_TOKEN + 12;
        public const int ASSIGN_BITAND = LAST_BYTECODE_TOKEN + 13;
        public const int ASSIGN_LSH = LAST_BYTECODE_TOKEN + 14;
        public const int ASSIGN_RSH = LAST_BYTECODE_TOKEN + 15;
        public const int ASSIGN_URSH = LAST_BYTECODE_TOKEN + 16;
        public const int ASSIGN_ADD = LAST_BYTECODE_TOKEN + 17;
        public const int ASSIGN_SUB = LAST_BYTECODE_TOKEN + 18;
        public const int ASSIGN_MUL = LAST_BYTECODE_TOKEN + 19;
        public const int ASSIGN_DIV = LAST_BYTECODE_TOKEN + 20;
        public const int ASSIGN_MOD = LAST_BYTECODE_TOKEN + 21; // %=

        public const int FIRST_ASSIGN = ASSIGN;
        public const int LAST_ASSIGN = ASSIGN_MOD;

        public const int HOOK = LAST_BYTECODE_TOKEN + 22;
        public const int COLON = LAST_BYTECODE_TOKEN + 23;
        public const int OR = LAST_BYTECODE_TOKEN + 24;
        public const int AND = LAST_BYTECODE_TOKEN + 25;
        public const int INC = LAST_BYTECODE_TOKEN + 26;
        public const int DEC = LAST_BYTECODE_TOKEN + 27;
        public const int DOT = LAST_BYTECODE_TOKEN + 28;
        public const int FUNCTION = LAST_BYTECODE_TOKEN + 29;
        public const int EXPORT = LAST_BYTECODE_TOKEN + 30;
        public const int IMPORT = LAST_BYTECODE_TOKEN + 31;
        public const int IF = LAST_BYTECODE_TOKEN + 32;
        public const int ELSE = LAST_BYTECODE_TOKEN + 33;
        public const int SWITCH = LAST_BYTECODE_TOKEN + 34;
        public const int CASE = LAST_BYTECODE_TOKEN + 35;
        public const int DEFAULT = LAST_BYTECODE_TOKEN + 36;
        public const int WHILE = LAST_BYTECODE_TOKEN + 37;
        public const int DO = LAST_BYTECODE_TOKEN + 38;
        public const int FOR = LAST_BYTECODE_TOKEN + 39;
        public const int BREAK = LAST_BYTECODE_TOKEN + 40;
        public const int CONTINUE = LAST_BYTECODE_TOKEN + 41;
        public const int VAR = LAST_BYTECODE_TOKEN + 42;
        public const int WITH = LAST_BYTECODE_TOKEN + 43;
        public const int CATCH = LAST_BYTECODE_TOKEN + 44;
        public const int FINALLY = LAST_BYTECODE_TOKEN + 45;
        public const int VOID = LAST_BYTECODE_TOKEN + 46;
        public const int RESERVED = LAST_BYTECODE_TOKEN + 47;
        public const int EMPTY = LAST_BYTECODE_TOKEN + 48;
        public const int BLOCK = LAST_BYTECODE_TOKEN + 49;
        public const int LABEL = LAST_BYTECODE_TOKEN + 50;
        public const int TARGET = LAST_BYTECODE_TOKEN + 51;
        public const int LOOP = LAST_BYTECODE_TOKEN + 52;
        public const int EXPR_VOID = LAST_BYTECODE_TOKEN + 53;
        public const int EXPR_RESULT = LAST_BYTECODE_TOKEN + 54;
        public const int JSR = LAST_BYTECODE_TOKEN + 55;
        public const int SCRIPT = LAST_BYTECODE_TOKEN + 56;
        public const int TYPEOFNAME = LAST_BYTECODE_TOKEN + 57;
        public const int USE_STACK = LAST_BYTECODE_TOKEN + 58;
        public const int SETPROP_OP = LAST_BYTECODE_TOKEN + 59;
        public const int SETELEM_OP = LAST_BYTECODE_TOKEN + 60;
        public const int LOCAL_BLOCK = LAST_BYTECODE_TOKEN + 61;
        public const int SET_REF_OP = LAST_BYTECODE_TOKEN + 62;
        public const int DOTDOT = LAST_BYTECODE_TOKEN + 63;
        public const int COLONCOLON = LAST_BYTECODE_TOKEN + 64;
        public const int XML = LAST_BYTECODE_TOKEN + 65;
        public const int DOTQUERY = LAST_BYTECODE_TOKEN + 66;
        public const int XMLATTR = LAST_BYTECODE_TOKEN + 67;
        public const int XMLEND = LAST_BYTECODE_TOKEN + 68;
        public const int TO_OBJECT = LAST_BYTECODE_TOKEN + 69;
        public const int TO_DOUBLE = LAST_BYTECODE_TOKEN + 70;

        public const int GET = LAST_BYTECODE_TOKEN + 71;  // JS 1.5 get pseudo keyword
        public const int SET = LAST_BYTECODE_TOKEN + 72;  // JS 1.5 set pseudo keyword
        public const int CONST = LAST_BYTECODE_TOKEN + 73;
        public const int SETCONST = LAST_BYTECODE_TOKEN + 74;
        public const int SETCONSTVAR = LAST_BYTECODE_TOKEN + 75;
        public const int CONDCOMMENT = LAST_BYTECODE_TOKEN + 76; // JScript conditional comment
        public const int KEEPCOMMENT = LAST_BYTECODE_TOKEN + 77; // /*! ... */ comment
        public const int DEBUGGER = LAST_BYTECODE_TOKEN + 78;

        public const int LAST_TOKEN = LAST_BYTECODE_TOKEN + 79;

        public static string name (int token)
        {
            //if (!printNames) {
            //    return Convert.ToString (token);
            //}
            switch (token) {

                case ERROR:
                    return "ERROR";
                case EOF:
                    return "EOF";
                case EOL:
                    return "EOL";

                case ENTERWITH:
                    return "ENTERWITH";

                case LEAVEWITH:
                    return "LEAVEWITH";

                case RETURN:
                    return "RETURN";

                case GOTO:
                    return "GOTO";

                case IFEQ:
                    return "IFEQ";

                case IFNE:
                    return "IFNE";

                case SETNAME:
                    return "SETNAME";

                case BITOR:
                    return "BITOR";

                case BITXOR:
                    return "BITXOR";

                case BITAND:
                    return "BITAND";

                case EQ:
                    return "EQ";

                case NE:
                    return "NE";

                case LT:
                    return "LT";

                case LE:
                    return "LE";

                case GT:
                    return "GT";

                case GE:
                    return "GE";

                case LSH:
                    return "LSH";

                case RSH:
                    return "RSH";

                case URSH:
                    return "URSH";

                case ADD:
                    return "ADD";

                case SUB:
                    return "SUB";

                case MUL:
                    return "MUL";

                case DIV:
                    return "DIV";

                case MOD:
                    return "MOD";

                case NOT:
                    return "NOT";

                case BITNOT:
                    return "BITNOT";

                case POS:
                    return "POS";

                case NEG:
                    return "NEG";

                case NEW:
                    return "NEW";

                case DELPROP:
                    return "DELPROP";

                case TYPEOF:
                    return "TYPEOF";

                case GETPROP:
                    return "GETPROP";

                case SETPROP:
                    return "SETPROP";

                case GETELEM:
                    return "GETELEM";

                case SETELEM:
                    return "SETELEM";

                case CALL:
                    return "CALL";

                case NAME:
                    return "NAME";

                case NUMBER:
                    return "NUMBER";

                case STRING:
                    return "STRING";

                case NULL:
                    return "NULL";

                case THIS:
                    return "THIS";

                case FALSE:
                    return "FALSE";

                case TRUE:
                    return "TRUE";

                case SHEQ:
                    return "SHEQ";

                case SHNE:
                    return "SHNE";

                case REGEXP:
                    return "OBJECT";

                case BINDNAME:
                    return "BINDNAME";

                case THROW:
                    return "THROW";

                case RETHROW:
                    return "RETHROW";

                case IN:
                    return "IN";

                case INSTANCEOF:
                    return "INSTANCEOF";

                case LOCAL_LOAD:
                    return "LOCAL_LOAD";

                case GETVAR:
                    return "GETVAR";

                case SETVAR:
                    return "SETVAR";

                case CATCH_SCOPE:
                    return "CATCH_SCOPE";

                case ENUM_INIT_KEYS:
                    return "ENUM_INIT_KEYS";

                case ENUM_INIT_VALUES:
                    return "ENUM_INIT_VALUES";

                case ENUM_NEXT:
                    return "ENUM_NEXT";

                case ENUM_ID:
                    return "ENUM_ID";

                case THISFN:
                    return "THISFN";

                case RETURN_RESULT:
                    return "RETURN_RESULT";

                case ARRAYLIT:
                    return "ARRAYLIT";

                case OBJECTLIT:
                    return "OBJECTLIT";

                case GET_REF:
                    return "GET_REF";

                case SET_REF:
                    return "SET_REF";

                case DEL_REF:
                    return "DEL_REF";

                case REF_CALL:
                    return "REF_CALL";

                case REF_SPECIAL:
                    return "REF_SPECIAL";

                case DEFAULTNAMESPACE:
                    return "DEFAULTNAMESPACE";

                case ESCXMLTEXT:
                    return "ESCXMLTEXT";

                case ESCXMLATTR:
                    return "ESCXMLATTR";

                case REF_MEMBER:
                    return "REF_MEMBER";

                case REF_NS_MEMBER:
                    return "REF_NS_MEMBER";

                case REF_NAME:
                    return "REF_NAME";

                case REF_NS_NAME:
                    return "REF_NS_NAME";

                case TRY:
                    return "TRY";

                case SEMI:
                    return "SEMI";

                case LB:
                    return "LB";

                case RB:
                    return "RB";

                case LC:
                    return "LC";

                case RC:
                    return "RC";

                case LP:
                    return "LP";

                case RP:
                    return "RP";

                case COMMA:
                    return "COMMA";

                case ASSIGN:
                    return "ASSIGN";

                case ASSIGN_BITOR:
                    return "ASSIGN_BITOR";

                case ASSIGN_BITXOR:
                    return "ASSIGN_BITXOR";

                case ASSIGN_BITAND:
                    return "ASSIGN_BITAND";

                case ASSIGN_LSH:
                    return "ASSIGN_LSH";

                case ASSIGN_RSH:
                    return "ASSIGN_RSH";

                case ASSIGN_URSH:
                    return "ASSIGN_URSH";

                case ASSIGN_ADD:
                    return "ASSIGN_ADD";

                case ASSIGN_SUB:
                    return "ASSIGN_SUB";

                case ASSIGN_MUL:
                    return "ASSIGN_MUL";

                case ASSIGN_DIV:
                    return "ASSIGN_DIV";

                case ASSIGN_MOD:
                    return "ASSIGN_MOD";

                case HOOK:
                    return "HOOK";

                case COLON:
                    return "COLON";

                case OR:
                    return "OR";

                case AND:
                    return "AND";

                case INC:
                    return "INC";

                case DEC:
                    return "DEC";

                case DOT:
                    return "DOT";

                case FUNCTION:
                    return "FUNCTION";

                case EXPORT:
                    return "EXPORT";

                case IMPORT:
                    return "IMPORT";

                case IF:
                    return "IF";

                case ELSE:
                    return "ELSE";

                case SWITCH:
                    return "SWITCH";

                case CASE:
                    return "CASE";

                case DEFAULT:
                    return "DEFAULT";

                case WHILE:
                    return "WHILE";

                case DO:
                    return "DO";

                case FOR:
                    return "FOR";

                case BREAK:
                    return "BREAK";

                case CONTINUE:
                    return "CONTINUE";

                case VAR:
                    return "VAR";

                case WITH:
                    return "WITH";

                case CATCH:
                    return "CATCH";

                case FINALLY:
                    return "FINALLY";

                case RESERVED:
                    return "RESERVED";

                case EMPTY:
                    return "EMPTY";

                case BLOCK:
                    return "BLOCK";

                case LABEL:
                    return "LABEL";

                case TARGET:
                    return "TARGET";

                case LOOP:
                    return "LOOP";

                case EXPR_VOID:
                    return "EXPR_VOID";

                case EXPR_RESULT:
                    return "EXPR_RESULT";

                case JSR:
                    return "JSR";

                case SCRIPT:
                    return "SCRIPT";

                case TYPEOFNAME:
                    return "TYPEOFNAME";

                case USE_STACK:
                    return "USE_STACK";

                case SETPROP_OP:
                    return "SETPROP_OP";

                case SETELEM_OP:
                    return "SETELEM_OP";

                case LOCAL_BLOCK:
                    return "LOCAL_BLOCK";

                case SET_REF_OP:
                    return "SET_REF_OP";

                case DOTDOT:
                    return "DOTDOT";

                case COLONCOLON:
                    return "COLONCOLON";

                case XML:
                    return "XML";

                case DOTQUERY:
                    return "DOTQUERY";

                case XMLATTR:
                    return "XMLATTR";

                case XMLEND:
                    return "XMLEND";

                case TO_OBJECT: return "TO_OBJECT";
                case TO_DOUBLE: return "TO_DOUBLE";
                case GET: return "GET";
                case SET: return "SET";
                case CONST: return "CONST";
                case SETCONST: return "SETCONST";
                case SETCONSTVAR: return "SETCONSTVAR";
                case CONDCOMMENT: return "CONDCOMMENT";
                case KEEPCOMMENT: return "KEEPCOMMENT";
                case DEBUGGER: return "DEBUGGER";
                default: return "UNKNOWN Token Type";
            }

            // Token without name
            throw new Exception ("Unknown token: " + Convert.ToString (token));
        }
    }
}