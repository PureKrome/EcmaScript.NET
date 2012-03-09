//------------------------------------------------------------------------------
// <license file="Decompiler.cs">
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

using EcmaScript.NET.Collections;

namespace EcmaScript.NET
{

    /// <summary> The following class save decompilation information about the source.
    /// Source information is returned from the parser as a String
    /// associated with function nodes and with the toplevel script.  When
    /// saved in the constant pool of a class, this string will be UTF-8
    /// encoded, and token values will occupy a single byte.
    /// Source is saved (mostly) as token numbers.  The tokens saved pretty
    /// much correspond to the token stream of a 'canonical' representation
    /// of the input program, as directed by the parser.  (There were a few
    /// cases where tokens could have been left out where decompiler could
    /// easily reconstruct them, but I left them in for clarity).  (I also
    /// looked adding source collection to TokenStream instead, where I
    /// could have limited the changes to a few lines in getToken... but
    /// this wouldn't have saved any space in the resulting source
    /// representation, and would have meant that I'd have to duplicate
    /// parser logic in the decompiler to disambiguate situations where
    /// newlines are important.)  The function decompile expands the
    /// tokens back into their string representations, using simple
    /// lookahead to correct spacing and indentation.
    /// 
    /// Assignments are saved as two-token pairs (Token.ASSIGN, op). Number tokens
    /// are stored inline, as a NUMBER token, a character representing the type, and
    /// either 1 or 4 characters representing the bit-encoding of the number.  String
    /// types NAME, STRING and OBJECT are currently stored as a token type,
    /// followed by a character giving the length of the string (assumed to
    /// be less than 2^16), followed by the characters of the string
    /// inlined into the source string.  Changing this to some reference to
    /// to the string in the compiled class' constant pool would probably
    /// save a lot of space... but would require some method of deriving
    /// the final constant pool entry from information available at parse
    /// time.
    /// </summary>
    public class Decompiler
    {
        internal string EncodedSource
        {
            get
            {
                return SourceToString(0);
            }

        }
        internal int CurrentOffset
        {
            get
            {
                return sourceTop;
            }

        }
        /// <summary> Flag to indicate that the decompilation should omit the
        /// function header and trailing brace.
        /// </summary>
        public const int ONLY_BODY_FLAG = 1 << 0;

        /// <summary> Flag to indicate that the decompilation generates toSource result.</summary>
        public const int TO_SOURCE_FLAG = 1 << 1;

        public const int TO_STRING_FLAG = 1 << 2;

        /// <summary> Decompilation property to specify initial ident value.</summary>
        public const int INITIAL_INDENT_PROP = 1;

        /// <summary> Decompilation property to specify default identation offset.</summary>
        public const int INDENT_GAP_PROP = 2;

        /// <summary> Decompilation property to specify identation offset for case labels.</summary>
        public const int CASE_GAP_PROP = 3;

        // Marker to denote the last RC of function so it can be distinguished from
        // the last RC of object literals in case of function expressions		
        private const int FUNCTION_END = 147;

        internal int MarkFunctionStart(int functionType)
        {
            int savedOffset = CurrentOffset;
            AddToken(Token.FUNCTION);
            Append((char)functionType);
            return savedOffset;
        }

        internal int MarkFunctionEnd(int functionStart)
        {
            int offset = CurrentOffset;
            Append((char)FUNCTION_END);
            return offset;
        }

        internal void AddToken(int token)
        {
            if (!(0 <= token && token <= Token.LAST_TOKEN))
                throw new ArgumentException();

            Append((char)token);
        }

        internal void AddEol(int token)
        {
            if (!(0 <= token && token <= Token.LAST_TOKEN))
                throw new ArgumentException();

            Append((char)token);
            Append((char)Token.EOL);
        }

        internal void AddName(string str)
        {
            AddToken(Token.NAME);
            AppendString(str);
        }

        internal void AddString(string str)
        {
            AddToken(Token.STRING);
            AppendString(str);
        }

        internal void AddRegexp(string regexp, string flags)
        {
            AddToken(Token.REGEXP);
            AppendString('/' + regexp + '/' + flags);
        }

        internal void AddJScriptConditionalComment(String str)
        {
            AddToken(Token.CONDCOMMENT);
            AppendString(str);
        }

        internal void AddPreservedComment(String str)
        {
            AddToken(Token.KEEPCOMMENT);
            AppendString(str);
        }

        internal void AddNumber(double n)
        {
            AddToken(Token.NUMBER);

            /* encode the number in the source stream.
            * Save as NUMBER type (char | char char char char)
            * where type is
            * 'D' - double, 'S' - short, 'J' - long.
			
            * We need to retain float vs. integer type info to keep the
            * behavior of liveconnect type-guessing the same after
            * decompilation.  (Liveconnect tries to present 1.0 to Java
            * as a float/double)
            * OPT: This is no longer true. We could compress the format.
			
            * This may not be the most space-efficient encoding;
            * the chars created below may take up to 3 bytes in
            * constant pool UTF-8 encoding, so a Double could take
            * up to 12 bytes.
            */

            long lbits = (long)n;
            if (lbits != n)
            {
                // if it's floating point, save as a Double bit pattern.
                // (12/15/97 our scanner only returns Double for f.p.)				
                lbits = BitConverter.DoubleToInt64Bits(n);
                Append('D');
                Append((char)(lbits >> 48));
                Append((char)(lbits >> 32));
                Append((char)(lbits >> 16));
                Append((char)lbits);
            }
            else
            {
                // we can ignore negative values, bc they're already prefixed
                // by NEG
                if (lbits < 0)
                    Context.CodeBug();

                // will it fit in a char?
                // this gives a short encoding for integer values up to 2^16.
                if (lbits <= char.MaxValue)
                {
                    Append('S');
                    Append((char)lbits);
                }
                else
                {
                    // Integral, but won't fit in a char. Store as a long.
                    Append('J');
                    Append((char)(lbits >> 48));
                    Append((char)(lbits >> 32));
                    Append((char)(lbits >> 16));
                    Append((char)lbits);
                }
            }
        }

        private void AppendString(string str)
        {
            int L = str.Length;
            int lengthEncodingSize = 1;
            if (L >= 0x8000)
            {
                lengthEncodingSize = 2;
            }
            int nextTop = sourceTop + lengthEncodingSize + L;
            if (nextTop > sourceBuffer.Length)
            {
                IncreaseSourceCapacity(nextTop);
            }
            if (L >= 0x8000)
            {
                // Use 2 chars to encode strings exceeding 32K, were the highest
                // bit in the first char indicates presence of the next byte
                sourceBuffer[sourceTop] = (char)(0x8000 | (int)((uint)L >> 16));
                ++sourceTop;
            }
            sourceBuffer[sourceTop] = (char)L;
            ++sourceTop;
            str.ToCharArray(0, L).CopyTo(sourceBuffer, sourceTop);
            sourceTop = nextTop;
        }

        private void Append(char c)
        {
            if (sourceTop == sourceBuffer.Length)
            {
                IncreaseSourceCapacity(sourceTop + 1);
            }
            sourceBuffer[sourceTop] = c;
            ++sourceTop;
        }

        private void IncreaseSourceCapacity(int minimalCapacity)
        {
            // Call this only when capacity increase is must
            if (minimalCapacity <= sourceBuffer.Length)
                Context.CodeBug();
            int newCapacity = sourceBuffer.Length * 2;
            if (newCapacity < minimalCapacity)
            {
                newCapacity = minimalCapacity;
            }
            char[] tmp = new char[newCapacity];
            Array.Copy(sourceBuffer, 0, tmp, 0, sourceTop);
            sourceBuffer = tmp;
        }

        private string SourceToString(int offset)
        {
            if (offset < 0 || sourceTop < offset)
                Context.CodeBug();
            return new string(sourceBuffer, offset, sourceTop - offset);
        }

        /// <summary> Decompile the source information associated with this js
        /// function/script back into a string.  For the most part, this
        /// just means translating tokens back to their string
        /// representations; there's a little bit of lookahead logic to
        /// decide the proper spacing/indentation.  Most of the work in
        /// mapping the original source to the prettyprinted decompiled
        /// version is done by the parser.
        /// 
        /// </summary>
        /// <param name="source">encoded source tree presentation
        /// 
        /// </param>
        /// <param name="flags">flags to select output format
        /// 
        /// </param>
        /// <param name="properties">indentation properties
        /// 
        /// </param>
        public static string Decompile(string source, int flags, UintMap properties)
        {
            int length = source.Length;
            if (length == 0)
            {
                return "";
            }

            int indent = properties.getInt(INITIAL_INDENT_PROP, 0);
            if (indent < 0)
                throw new ArgumentException();
            int indentGap = properties.getInt(INDENT_GAP_PROP, 4);
            if (indentGap < 0)
                throw new ArgumentException();
            int caseGap = properties.getInt(CASE_GAP_PROP, 2);
            if (caseGap < 0)
                throw new ArgumentException();

            System.Text.StringBuilder result = new System.Text.StringBuilder();
            bool justFunctionBody = (0 != (flags & Decompiler.ONLY_BODY_FLAG));
            bool toSource = (0 != (flags & Decompiler.TO_SOURCE_FLAG));
            bool toString = (0 != (flags & Decompiler.TO_STRING_FLAG));

            // Spew tokens in source, for debugging.
            // as TYPE number char
            if (printSource)
            {
                System.Console.Error.WriteLine("length:" + length);
                for (int i = 0; i < length; ++i)
                {
                    // Note that tokenToName will fail unless Context.printTrees
                    // is true.
                    string tokenname = null;
                    if (Token.printNames)
                    {
                        tokenname = Token.name(source[i]);
                    }
                    if (tokenname == null)
                    {
                        tokenname = "---";
                    }
                    string pad = tokenname.Length > 7 ? "\t" : "\t\t";
                    System.Console.Error.WriteLine(tokenname + pad + (int)source[i] + "\t'" + ScriptRuntime.escapeString(source.Substring(i, (i + 1) - (i))) + "'");
                }
                System.Console.Error.WriteLine();
            }

            int braceNesting = 0;
            bool afterFirstEOL = false;
            int i2 = 0;
            int topFunctionType;
            if (source[i2] == Token.SCRIPT)
            {
                ++i2;
                topFunctionType = -1;
            }
            else
            {
                topFunctionType = source[i2 + 1];
            }

            if (!toSource)
            {
                if (!toString)
                {
                    // add an initial newline to exactly match js.
                    result.Append('\n');
                }
                for (int j = 0; j < indent; j++)
                    result.Append(' ');
            }
            else
            {
                if (topFunctionType == FunctionNode.FUNCTION_EXPRESSION)
                {
                    result.Append('(');
                }
            }

            while (i2 < length)
            {
                switch (source[i2])
                {

                    case (char)(Token.NAME):
                    case (char)(Token.REGEXP):  // re-wrapped in '/'s in parser...
                        i2 = PrintSourceString(source, i2 + 1, false, result);
                        continue;


                    case (char)(Token.STRING):
                        i2 = PrintSourceString(source, i2 + 1, true, result);
                        continue;


                    case (char)(Token.NUMBER):
                        i2 = PrintSourceNumber(source, i2 + 1, result);
                        continue;


                    case (char)(Token.TRUE):
                        result.Append("true");
                        break;


                    case (char)(Token.FALSE):
                        result.Append("false");
                        break;


                    case (char)(Token.NULL):
                        result.Append("null");
                        break;


                    case (char)(Token.THIS):
                        result.Append("this");
                        break;


                    case (char)(Token.FUNCTION):
                        ++i2; // skip function type
                        result.Append("function ");
                        break;


                    case (char)(FUNCTION_END):
                        // Do nothing
                        break;


                    case (char)(Token.COMMA):
                        result.Append(", ");
                        break;


                    case (char)(Token.LC):
                        ++braceNesting;
                        if (Token.EOL == GetNext(source, length, i2))
                            indent += indentGap;
                        result.Append('{');
                        break;


                    case (char)(Token.RC):
                        {
                            --braceNesting;
                            /* don't print the closing RC if it closes the
                            * toplevel function and we're called from
                            * decompileFunctionBody.
                            */
                            if (justFunctionBody && braceNesting == 0)
                                break;

                            result.Append('}');
                            switch (GetNext(source, length, i2))
                            {

                                case Token.EOL:
                                case FUNCTION_END:
                                    indent -= indentGap;
                                    break;

                                case Token.WHILE:
                                case Token.ELSE:
                                    indent -= indentGap;
                                    result.Append(' ');
                                    break;
                            }
                            break;
                        }

                    case (char)(Token.LP):
                        result.Append('(');
                        break;


                    case (char)(Token.RP):
                        result.Append(')');
                        if (Token.LC == GetNext(source, length, i2))
                            result.Append(' ');
                        break;


                    case (char)(Token.LB):
                        result.Append('[');
                        break;


                    case (char)(Token.RB):
                        result.Append(']');
                        break;


                    case (char)(Token.EOL):
                        {
                            if (toSource)
                                break;
                            bool newLine = true;
                            if (!afterFirstEOL)
                            {
                                afterFirstEOL = true;
                                if (justFunctionBody)
                                {
                                    /* throw away just added 'function name(...) {'
                                    * and restore the original indent
                                    */
                                    result.Length = 0;
                                    indent -= indentGap;
                                    newLine = false;
                                }
                            }
                            if (newLine)
                            {
                                result.Append('\n');
                            }

                            /* add indent if any tokens remain,
                            * less setback if next token is
                            * a label, case or default.
                            */
                            if (i2 + 1 < length)
                            {
                                int less = 0;
                                int nextToken = source[i2 + 1];
                                if (nextToken == Token.CASE || nextToken == Token.DEFAULT)
                                {
                                    less = indentGap - caseGap;
                                }
                                else if (nextToken == Token.RC)
                                {
                                    less = indentGap;
                                }
                                /* elaborate check against label... skip past a
                                * following inlined NAME and look for a COLON.
                                */
                                else if (nextToken == Token.NAME)
                                {
                                    int afterName = GetSourceStringEnd(source, i2 + 2);
                                    if (source[afterName] == Token.COLON)
                                        less = indentGap;
                                }

                                for (; less < indent; less++)
                                    result.Append(' ');
                            }
                            break;
                        }

                    case (char)(Token.DOT):
                        result.Append('.');
                        break;


                    case (char)(Token.NEW):
                        result.Append("new ");
                        break;


                    case (char)(Token.DELPROP):
                        result.Append("delete ");
                        break;


                    case (char)(Token.IF):
                        result.Append("if ");
                        break;


                    case (char)(Token.ELSE):
                        result.Append("else ");
                        break;


                    case (char)(Token.FOR):
                        result.Append("for ");
                        break;


                    case (char)(Token.IN):
                        result.Append(" in ");
                        break;


                    case (char)(Token.WITH):
                        result.Append("with ");
                        break;


                    case (char)(Token.WHILE):
                        result.Append("while ");
                        break;


                    case (char)(Token.DO):
                        result.Append("do ");
                        break;


                    case (char)(Token.TRY):
                        result.Append("try ");
                        break;


                    case (char)(Token.CATCH):
                        result.Append("catch ");
                        break;


                    case (char)(Token.FINALLY):
                        result.Append("finally ");
                        break;


                    case (char)(Token.THROW):
                        result.Append("throw ");
                        break;


                    case (char)(Token.SWITCH):
                        result.Append("switch ");
                        break;


                    case (char)(Token.BREAK):
                        result.Append("break");
                        if (Token.NAME == GetNext(source, length, i2))
                            result.Append(' ');
                        break;


                    case (char)(Token.CONTINUE):
                        result.Append("continue");
                        if (Token.NAME == GetNext(source, length, i2))
                            result.Append(' ');
                        break;


                    case (char)(Token.CASE):
                        result.Append("case ");
                        break;


                    case (char)(Token.DEFAULT):
                        result.Append("default");
                        break;


                    case (char)(Token.RETURN):
                        result.Append("return");
                        if (Token.SEMI != GetNext(source, length, i2))
                            result.Append(' ');
                        break;


                    case (char)(Token.VAR):
                        result.Append("var ");
                        break;


                    case (char)(Token.SEMI):
                        result.Append(';');
                        if (Token.EOL != GetNext(source, length, i2))
                        {
                            // separators in FOR
                            result.Append(' ');
                        }
                        break;


                    case (char)(Token.ASSIGN):
                        result.Append(" = ");
                        break;


                    case (char)(Token.ASSIGN_ADD):
                        result.Append(" += ");
                        break;


                    case (char)(Token.ASSIGN_SUB):
                        result.Append(" -= ");
                        break;


                    case (char)(Token.ASSIGN_MUL):
                        result.Append(" *= ");
                        break;


                    case (char)(Token.ASSIGN_DIV):
                        result.Append(" /= ");
                        break;


                    case (char)(Token.ASSIGN_MOD):
                        result.Append(" %= ");
                        break;


                    case (char)(Token.ASSIGN_BITOR):
                        result.Append(" |= ");
                        break;


                    case (char)(Token.ASSIGN_BITXOR):
                        result.Append(" ^= ");
                        break;


                    case (char)(Token.ASSIGN_BITAND):
                        result.Append(" &= ");
                        break;


                    case (char)(Token.ASSIGN_LSH):
                        result.Append(" <<= ");
                        break;


                    case (char)(Token.ASSIGN_RSH):
                        result.Append(" >>= ");
                        break;


                    case (char)(Token.ASSIGN_URSH):
                        result.Append(" >>>= ");
                        break;


                    case (char)(Token.HOOK):
                        result.Append(" ? ");
                        break;


                    case (char)(Token.OBJECTLIT):
                        // pun OBJECTLIT to mean colon in objlit property
                        // initialization.
                        // This needs to be distinct from COLON in the general case
                        // to distinguish from the colon in a ternary... which needs
                        // different spacing.
                        result.Append(':');
                        break;


                    case (char)(Token.COLON):
                        if (Token.EOL == GetNext(source, length, i2))
                            // it's the end of a label
                            result.Append(':');
                        // it's the middle part of a ternary
                        else
                            result.Append(" : ");
                        break;


                    case (char)(Token.OR):
                        result.Append(" || ");
                        break;


                    case (char)(Token.AND):
                        result.Append(" && ");
                        break;


                    case (char)(Token.BITOR):
                        result.Append(" | ");
                        break;


                    case (char)(Token.BITXOR):
                        result.Append(" ^ ");
                        break;


                    case (char)(Token.BITAND):
                        result.Append(" & ");
                        break;


                    case (char)(Token.SHEQ):
                        result.Append(" === ");
                        break;


                    case (char)(Token.SHNE):
                        result.Append(" !== ");
                        break;


                    case (char)(Token.EQ):
                        result.Append(" == ");
                        break;


                    case (char)(Token.NE):
                        result.Append(" != ");
                        break;


                    case (char)(Token.LE):
                        result.Append(" <= ");
                        break;


                    case (char)(Token.LT):
                        result.Append(" < ");
                        break;


                    case (char)(Token.GE):
                        result.Append(" >= ");
                        break;


                    case (char)(Token.GT):
                        result.Append(" > ");
                        break;


                    case (char)(Token.INSTANCEOF):
                        result.Append(" instanceof ");
                        break;


                    case (char)(Token.LSH):
                        result.Append(" << ");
                        break;


                    case (char)(Token.RSH):
                        result.Append(" >> ");
                        break;


                    case (char)(Token.URSH):
                        result.Append(" >>> ");
                        break;


                    case (char)(Token.TYPEOF):
                        result.Append("typeof ");
                        break;


                    case (char)(Token.VOID):
                        result.Append("void ");
                        break;


                    case (char)(Token.NOT):
                        result.Append('!');
                        break;


                    case (char)(Token.BITNOT):
                        result.Append('~');
                        break;


                    case (char)(Token.POS):
                        result.Append('+');
                        break;


                    case (char)(Token.NEG):
                        result.Append('-');
                        break;


                    case (char)(Token.INC):
                        result.Append("++");
                        break;


                    case (char)(Token.DEC):
                        result.Append("--");
                        break;


                    case (char)(Token.ADD):
                        result.Append(" + ");
                        break;


                    case (char)(Token.SUB):
                        result.Append(" - ");
                        break;


                    case (char)(Token.MUL):
                        result.Append(" * ");
                        break;


                    case (char)(Token.DIV):
                        result.Append(" / ");
                        break;


                    case (char)(Token.MOD):
                        result.Append(" % ");
                        break;


                    case (char)(Token.COLONCOLON):
                        result.Append("::");
                        break;


                    case (char)(Token.DOTDOT):
                        result.Append("..");
                        break;


                    case (char)(Token.DOTQUERY):
                        result.Append(".(");
                        break;


                    case (char)(Token.XMLATTR):
                        result.Append('@');
                        break;


                    default:
                        // If we don't know how to decompile it, raise an exception.
                        throw new ApplicationException();

                }
                ++i2;
            }

            if (!toSource)
            {
                // add that trailing newline if it's an outermost function.
                if (!justFunctionBody && !toString)
                    result.Append('\n');
            }
            else
            {
                if (topFunctionType == FunctionNode.FUNCTION_EXPRESSION)
                {
                    result.Append(')');
                }
            }

            return result.ToString();
        }

        private static int GetNext(string source, int length, int i)
        {
            return (i + 1 < length) ? source[i + 1] : Token.EOF;
        }

        private static int GetSourceStringEnd(string source, int offset)
        {
            return PrintSourceString(source, offset, false, null);
        }

        private static int PrintSourceString(string source, int offset, bool asQuotedString, System.Text.StringBuilder sb)
        {
            int length = source[offset];
            ++offset;
            if ((0x8000 & length) != 0)
            {
                length = ((0x7FFF & length) << 16) | source[offset];
                ++offset;
            }
            if (sb != null)
            {
                string str = source.Substring(offset, (offset + length) - (offset));
                if (!asQuotedString)
                {
                    sb.Append(str);
                }
                else
                {
                    sb.Append('"');
                    sb.Append(ScriptRuntime.escapeString(str));
                    sb.Append('"');
                }
            }
            return offset + length;
        }

        private static int PrintSourceNumber(string source, int offset, System.Text.StringBuilder sb)
        {
            double number = 0.0;
            char type = source[offset];
            ++offset;
            if (type == 'S')
            {
                if (sb != null)
                {
                    int ival = source[offset];
                    number = ival;
                }
                ++offset;
            }
            else if (type == 'J' || type == 'D')
            {
                if (sb != null)
                {
                    long lbits;
                    lbits = (long)source[offset] << 48;
                    lbits |= (long)source[offset + 1] << 32;
                    lbits |= (long)source[offset + 2] << 16;
                    lbits |= (long)source[offset + 3];
                    if (type == 'J')
                    {
                        number = lbits;
                    }
                    else
                    {
                        number = BitConverter.Int64BitsToDouble(lbits);
                    }
                }
                offset += 4;
            }
            else
            {
                // Bad source
                throw new ApplicationException();
            }
            if (sb != null)
            {
                sb.Append(ScriptConvert.ToString(number, 10));
            }
            return offset;
        }

        private char[] sourceBuffer = new char[128];

        // Per script/function source buffer top: parent source does not include a
        // nested functions source and uses function index as a reference instead.
        private int sourceTop;

        // whether to do a debug print of the source information, when decompiling.
        private static bool printSource = false; // TODO: make preprocessor directive

    }
}