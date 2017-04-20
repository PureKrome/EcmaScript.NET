//------------------------------------------------------------------------------
// <license file="TokenStream.cs">
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

using EcmaScript.NET.Types;
using EcmaScript.NET.Collections;

namespace EcmaScript.NET
{

    /// <summary> This class implements the JavaScript scanner.
    /// 
    /// It is based on the C source files jsscan.c and jsscan.h
    /// in the jsref package.
    /// 
    /// </summary>	
    internal class TokenStream
    {
        internal int Lineno
        {
            get
            {
                return lineno;
            }

        }
        internal string String
        {
            get
            {
                return str;
            }

        }

        internal string TokenString
        {
            get
            {
                return tokenstr;
            }
        }
        internal double Number
        {
            get
            {
                return dNumber;
            }

        }
        internal int Token
        {
            get
            {
                int c;


                for (; ; )
                {
                    // Eat whitespace, possibly sensitive to newlines.
                    for (; ; )
                    {
                        c = Char;
                        if (c == EOF_CHAR)
                        {
                            return EcmaScript.NET.Token.EOF;
                        }
                        else if (c == '\n')
                        {
                            dirtyLine = false;
                            return EcmaScript.NET.Token.EOL;
                        }
                        else if (!isJSSpace(c))
                        {
                            if (c != '-')
                            {
                                dirtyLine = true;
                            }
                            break;
                        }
                    }

                    if (c == '@')
                        return EcmaScript.NET.Token.XMLATTR;

                    // identifier/keyword/instanceof?
                    // watch out for starting with a <backslash>
                    bool identifierStart;
                    bool isUnicodeEscapeStart = false;
                    if (c == '\\')
                    {
                        c = Char;
                        if (c == 'u')
                        {
                            identifierStart = true;
                            isUnicodeEscapeStart = true;
                            stringBufferTop = 0;
                        }
                        else
                        {
                            identifierStart = false;
                            ungetChar(c);
                            c = '\\';
                        }
                    }
                    else
                    {
                        char ch = (char)c;
                        identifierStart = char.IsLetter(ch)
                            || ch == '$'
                            || ch == '_';
                        if (identifierStart)
                        {
                            stringBufferTop = 0;
                            addToString(c);
                        }
                    }

                    if (identifierStart)
                    {
                        bool containsEscape = isUnicodeEscapeStart;
                        for (; ; )
                        {
                            if (isUnicodeEscapeStart)
                            {
                                // strictly speaking we should probably push-back
                                // all the bad characters if the <backslash>u
                                // sequence is malformed. But since there isn't a
                                // correct context(is there?) for a bad Unicode
                                // escape sequence in an identifier, we can report
                                // an error here.
                                int escapeVal = 0;
                                for (int i = 0; i != 4; ++i)
                                {
                                    c = Char;
                                    escapeVal = ScriptConvert.XDigitToInt(c, escapeVal);
                                    // Next check takes care about c < 0 and bad escape
                                    if (escapeVal < 0)
                                    {
                                        break;
                                    }
                                }
                                if (escapeVal < 0)
                                {
                                    parser.AddError("msg.invalid.escape");
                                    return EcmaScript.NET.Token.ERROR;
                                }
                                addToString(escapeVal);
                                isUnicodeEscapeStart = false;
                            }
                            else
                            {
                                c = Char;
                                if (c == '\\')
                                {
                                    c = Char;
                                    if (c == 'u')
                                    {
                                        isUnicodeEscapeStart = true;
                                        containsEscape = true;
                                    }
                                    else
                                    {
                                        parser.AddError("msg.illegal.character");
                                        return EcmaScript.NET.Token.ERROR;
                                    }
                                }
                                else
                                {
                                    if (c == EOF_CHAR || !IsJavaIdentifierPart((char)c))
                                    {
                                        break;
                                    }
                                    addToString(c);
                                }
                            }
                        }
                        ungetChar(c);

                        string str = StringFromBuffer;
                        this.tokenstr = str;

                        if (!containsEscape)
                        {
                            // OPT we shouldn't have to make a string (object!) to
                            // check if it's a keyword.

                            // Return the corresponding token if it's a keyword
                            int result = stringToKeyword(str);
                            if (result != EcmaScript.NET.Token.EOF)
                            {
                                if (result != EcmaScript.NET.Token.RESERVED)
                                {
                                    return result;
                                }
                                else if (!parser.compilerEnv.isReservedKeywordAsIdentifier())
                                {
                                    return result;
                                }
                                else
                                {
                                    // If implementation permits to use future reserved
                                    // keywords in violation with the EcmaScript,
                                    // treat it as name but issue warning
                                    parser.AddWarning("msg.reserved.keyword", str);
                                }
                            }
                        }
                        this.str = ((string)allStrings.intern(str));
                        return EcmaScript.NET.Token.NAME;
                    }

                    // is it a number?
                    if (isDigit(c) || (c == '.' && isDigit(peekChar())))
                    {

                        stringBufferTop = 0;
                        int toBase = 10;

                        if (c == '0')
                        {
                            c = Char;
                            if (c == 'x' || c == 'X')
                            {
                                toBase = 16;
                                c = Char;
                            }
                            else if (isDigit(c))
                            {
                                toBase = 8;
                            }
                            else
                            {
                                addToString('0');
                            }
                        }

                        if (toBase == 16)
                        {
                            while (0 <= ScriptConvert.XDigitToInt(c, 0))
                            {
                                addToString(c);
                                c = Char;
                            }
                        }
                        else
                        {
                            while ('0' <= c && c <= '9')
                            {
                                /*
                                * We permit 08 and 09 as decimal numbers, which
                                * makes our behavior a superset of the ECMA
                                * numeric grammar.  We might not always be so
                                * permissive, so we warn about it.
                                */
                                if (toBase == 8 && c >= '8')
                                {
                                    parser.AddWarning("msg.bad.octal.literal", c == '8' ? "8" : "9");
                                    toBase = 10;
                                }
                                addToString(c);
                                c = Char;
                            }
                        }

                        bool isInteger = true;

                        if (toBase == 10 && (c == '.' || c == 'e' || c == 'E'))
                        {
                            isInteger = false;
                            if (c == '.')
                            {
                                do
                                {
                                    addToString(c);
                                    c = Char;
                                }
                                while (isDigit(c));
                            }
                            if (c == 'e' || c == 'E')
                            {
                                addToString(c);
                                c = Char;
                                if (c == '+' || c == '-')
                                {
                                    addToString(c);
                                    c = Char;
                                }
                                if (!isDigit(c))
                                {
                                    parser.AddError("msg.missing.exponent");
                                    return EcmaScript.NET.Token.ERROR;
                                }
                                do
                                {
                                    addToString(c);
                                    c = Char;
                                }
                                while (isDigit(c));
                            }
                        }
                        ungetChar(c);
                        string numString = StringFromBuffer;

                        double dval;
                        if (toBase == 10 && !isInteger)
                        {
                            try
                            {
                                // Use Java conversion to number from string...
                                dval = System.Double.Parse(numString, BuiltinNumber.NumberFormatter);
                            }
                            catch (OverflowException)
                            {
                                // HACK 
                                if (numString[0] == '-')
                                    dval = double.NegativeInfinity;
                                else
                                    dval = double.PositiveInfinity;
                            }
                            catch (Exception)
                            {
                                parser.AddError("msg.caught.nfe");
                                return EcmaScript.NET.Token.ERROR;
                            }
                        }
                        else
                        {
                            dval = ScriptConvert.ToNumber(numString, 0, toBase);
                        }

                        this.dNumber = dval;
                        return EcmaScript.NET.Token.NUMBER;
                    }

                    // is it a string?
                    if (c == '"' || c == '\'')
                    {
                        // We attempt to accumulate a string the fast way, by
                        // building it directly out of the reader.  But if there
                        // are any escaped characters in the string, we revert to
                        // building it out of a StringBuffer.

                        int quoteChar = c;
                        stringBufferTop = 0;

                        c = Char;

                        while (c != quoteChar)
                        {
                            if (c == '\n' || c == EOF_CHAR)
                            {
                                ungetChar(c);
                                parser.AddError("msg.unterminated.string.lit");
                                return EcmaScript.NET.Token.ERROR;
                            }

                            if (c == '\\')
                            {
                                // We've hit an escaped character

                                c = Char;
                                switch (c)
                                {

                                    case '\\': // backslash
                                    case 'b':  // backspace
                                    case 'f':  // form feed
                                    case 'n':  // line feed
                                    case 'r':  // carriage return
                                    case 't':  // horizontal tab
                                    case 'v':  // vertical tab
                                    case 'd':  // octal sequence
                                    case 'u':  // unicode sequence
                                    case 'x':  // hexadecimal sequence
                                        // Only keep the '\' character for those
                                        // characters that need to be escaped...
                                        // Don't escape quoting characters...
                                        addToString('\\');
                                        addToString(c);
                                        break;

                                    case '\n':
                                        // Remove line terminator after escape
                                        break;


                                    default:
                                        if (isDigit(c))
                                        {
                                            // Octal representation of a character.
                                            // Preserve the escaping (see Y! bug #1637286)
                                            addToString('\\');
                                        }
                                        addToString(c);
                                        break;
                                        break;

                                }
                            }
                            else
                            {
                                addToString(c);
                            }

                            c = Char;
                        }

                        string str = StringFromBuffer;
                        this.str = ((string)allStrings.intern(str));
                        return EcmaScript.NET.Token.STRING;
                    }

                    switch (c)
                    {

                        case ';':
                            return EcmaScript.NET.Token.SEMI;

                        case '[':
                            return EcmaScript.NET.Token.LB;

                        case ']':
                            return EcmaScript.NET.Token.RB;

                        case '{':
                            return EcmaScript.NET.Token.LC;

                        case '}':
                            return EcmaScript.NET.Token.RC;

                        case '(':
                            return EcmaScript.NET.Token.LP;

                        case ')':
                            return EcmaScript.NET.Token.RP;

                        case ',':
                            return EcmaScript.NET.Token.COMMA;

                        case '?':
                            return EcmaScript.NET.Token.HOOK;

                        case ':':
                            if (matchChar(':'))
                            {
                                return EcmaScript.NET.Token.COLONCOLON;
                            }
                            else
                            {
                                return EcmaScript.NET.Token.COLON;
                            }

                        case '.':
                            if (matchChar('.'))
                            {
                                return EcmaScript.NET.Token.DOTDOT;
                            }
                            else if (matchChar('('))
                            {
                                return EcmaScript.NET.Token.DOTQUERY;
                            }
                            else
                            {
                                return EcmaScript.NET.Token.DOT;
                            }

                        case '|':
                            if (matchChar('|'))
                            {
                                return EcmaScript.NET.Token.OR;
                            }
                            else if (matchChar('='))
                            {
                                return EcmaScript.NET.Token.ASSIGN_BITOR;
                            }
                            else
                            {
                                return EcmaScript.NET.Token.BITOR;
                            }


                        case '^':
                            if (matchChar('='))
                            {
                                return EcmaScript.NET.Token.ASSIGN_BITXOR;
                            }
                            else
                            {
                                return EcmaScript.NET.Token.BITXOR;
                            }


                        case '&':
                            if (matchChar('&'))
                            {
                                return EcmaScript.NET.Token.AND;
                            }
                            else if (matchChar('='))
                            {
                                return EcmaScript.NET.Token.ASSIGN_BITAND;
                            }
                            else
                            {
                                return EcmaScript.NET.Token.BITAND;
                            }


                        case '=':
                            if (matchChar('='))
                            {
                                if (matchChar('='))
                                    return EcmaScript.NET.Token.SHEQ;
                                else
                                    return EcmaScript.NET.Token.EQ;
                            }
                            else
                            {
                                return EcmaScript.NET.Token.ASSIGN;
                            }


                        case '!':
                            if (matchChar('='))
                            {
                                if (matchChar('='))
                                    return EcmaScript.NET.Token.SHNE;
                                else
                                    return EcmaScript.NET.Token.NE;
                            }
                            else
                            {
                                return EcmaScript.NET.Token.NOT;
                            }


                        case '<':
                            /* NB:treat HTML begin-comment as comment-till-eol */
                            if (matchChar('!'))
                            {
                                if (matchChar('-'))
                                {
                                    if (matchChar('-'))
                                    {
                                        skipLine();

                                        goto retry;
                                    }
                                    ungetChar('-');
                                }
                                ungetChar('!');
                            }
                            if (matchChar('<'))
                            {
                                if (matchChar('='))
                                {
                                    return EcmaScript.NET.Token.ASSIGN_LSH;
                                }
                                else
                                {
                                    return EcmaScript.NET.Token.LSH;
                                }
                            }
                            else
                            {
                                if (matchChar('='))
                                {
                                    return EcmaScript.NET.Token.LE;
                                }
                                else
                                {
                                    return EcmaScript.NET.Token.LT;
                                }
                            }


                        case '>':
                            if (matchChar('>'))
                            {
                                if (matchChar('>'))
                                {
                                    if (matchChar('='))
                                    {
                                        return EcmaScript.NET.Token.ASSIGN_URSH;
                                    }
                                    else
                                    {
                                        return EcmaScript.NET.Token.URSH;
                                    }
                                }
                                else
                                {
                                    if (matchChar('='))
                                    {
                                        return EcmaScript.NET.Token.ASSIGN_RSH;
                                    }
                                    else
                                    {
                                        return EcmaScript.NET.Token.RSH;
                                    }
                                }
                            }
                            else
                            {
                                if (matchChar('='))
                                {
                                    return EcmaScript.NET.Token.GE;
                                }
                                else
                                {
                                    return EcmaScript.NET.Token.GT;
                                }
                            }


                        case '*':
                            if (matchChar('='))
                            {
                                return EcmaScript.NET.Token.ASSIGN_MUL;
                            }
                            else
                            {
                                return EcmaScript.NET.Token.MUL;
                            }


                        case '/':
                            // is it a // comment?
                            if (matchChar('/'))
                            {
                                skipLine();

                                goto retry;
                            }
                            if (matchChar('*'))
                            {
                                bool lookForSlash = false;
                                StringBuilder sb = new StringBuilder();
                                for (; ; )
                                {
                                    c = Char;
                                    if (c == EOF_CHAR)
                                    {
                                        parser.AddError("msg.unterminated.comment");
                                        return EcmaScript.NET.Token.ERROR;
                                    }
                                    sb.Append((char)c);
                                    if (c == '*')
                                    {
                                        lookForSlash = true;
                                    }
                                    else if (c == '/')
                                    {
                                        if (lookForSlash)
                                        {
                                            sb.Remove(sb.Length - 2, 2);
                                            string s1 = sb.ToString();
                                            string s2 = s1.Trim();
                                            if (s1.StartsWith("!"))
                                            {
                                                // Remove the leading '!'
                                                this.str = s1.Substring(1);
                                                return NET.Token.KEEPCOMMENT;
                                            }
                                            else if (s2.StartsWith("@cc_on") ||
                                                s2.StartsWith("@if") ||
                                                s2.StartsWith("@elif") ||
                                                s2.StartsWith("@else") ||
                                                s2.StartsWith("@end"))
                                            {
                                                this.str = s1;
                                                return NET.Token.CONDCOMMENT;
                                            }
                                            else
                                            {
                                                goto retry;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        lookForSlash = false;
                                    }
                                }
                            }

                            if (matchChar('='))
                            {
                                return EcmaScript.NET.Token.ASSIGN_DIV;
                            }
                            else
                            {
                                return EcmaScript.NET.Token.DIV;
                            }


                        case '%':
                            if (matchChar('='))
                            {
                                return EcmaScript.NET.Token.ASSIGN_MOD;
                            }
                            else
                            {
                                return EcmaScript.NET.Token.MOD;
                            }


                        case '~':
                            return EcmaScript.NET.Token.BITNOT;


                        case '+':
                            if (matchChar('='))
                            {
                                return EcmaScript.NET.Token.ASSIGN_ADD;
                            }
                            else if (matchChar('+'))
                            {
                                return EcmaScript.NET.Token.INC;
                            }
                            else
                            {
                                return EcmaScript.NET.Token.ADD;
                            }


                        case '-':
                            if (matchChar('='))
                            {
                                c = EcmaScript.NET.Token.ASSIGN_SUB;
                            }
                            else if (matchChar('-'))
                            {
                                if (!dirtyLine)
                                {
                                    // treat HTML end-comment after possible whitespace
                                    // after line start as comment-utill-eol
                                    if (matchChar('>'))
                                    {
                                        skipLine();

                                        goto retry;
                                    }
                                }
                                c = EcmaScript.NET.Token.DEC;
                            }
                            else
                            {
                                c = EcmaScript.NET.Token.SUB;
                            }
                            dirtyLine = true;
                            return c;


                        default:
                            parser.AddError("msg.illegal.character");
                            return EcmaScript.NET.Token.ERROR;

                    }

                retry:
                    ;
                }
            }

        }
        internal bool XMLAttribute
        {
            get
            {
                return xmlIsAttribute;
            }

        }
        internal int FirstXMLToken
        {
            get
            {
                xmlOpenTagsCount = 0;
                xmlIsAttribute = false;
                xmlIsTagContent = false;
                ungetChar('<');
                return NextXMLToken;
            }

        }
        internal int NextXMLToken
        {
            get
            {
                stringBufferTop = 0; // remember the XML

                for (int c = Char; c != EOF_CHAR; c = Char)
                {
                    if (xmlIsTagContent)
                    {
                        switch (c)
                        {

                            case '>':
                                addToString(c);
                                xmlIsTagContent = false;
                                xmlIsAttribute = false;
                                break;

                            case '/':
                                addToString(c);
                                if (peekChar() == '>')
                                {
                                    c = Char;
                                    addToString(c);
                                    xmlIsTagContent = false;
                                    xmlOpenTagsCount--;
                                }
                                break;

                            case '{':
                                ungetChar(c);
                                this.str = StringFromBuffer;
                                return EcmaScript.NET.Token.XML;

                            case '\'':
                            case '"':
                                addToString(c);
                                if (!readQuotedString(c))
                                    return EcmaScript.NET.Token.ERROR;
                                break;

                            case '=':
                                addToString(c);
                                xmlIsAttribute = true;
                                break;

                            case ' ':
                            case '\t':
                            case '\r':
                            case '\n':
                                addToString(c);
                                break;

                            default:
                                addToString(c);
                                xmlIsAttribute = false;
                                break;

                        }

                        if (!xmlIsTagContent && xmlOpenTagsCount == 0)
                        {
                            this.str = StringFromBuffer;
                            return EcmaScript.NET.Token.XMLEND;
                        }
                    }
                    else
                    {
                        switch (c)
                        {

                            case '<':
                                addToString(c);
                                c = peekChar();
                                switch (c)
                                {

                                    case '!':
                                        c = Char; // Skip !
                                        addToString(c);
                                        c = peekChar();
                                        switch (c)
                                        {

                                            case '-':
                                                c = Char; // Skip -
                                                addToString(c);
                                                c = Char;
                                                if (c == '-')
                                                {
                                                    addToString(c);
                                                    if (!readXmlComment())
                                                        return EcmaScript.NET.Token.ERROR;
                                                }
                                                else
                                                {
                                                    // throw away the string in progress
                                                    stringBufferTop = 0;
                                                    this.str = null;
                                                    parser.AddError("msg.XML.bad.form");
                                                    return EcmaScript.NET.Token.ERROR;
                                                }
                                                break;

                                            case '[':
                                                c = Char; // Skip [
                                                addToString(c);
                                                if (Char == 'C' && Char == 'D' && Char == 'A' && Char == 'T' && Char == 'A' && Char == '[')
                                                {
                                                    addToString('C');
                                                    addToString('D');
                                                    addToString('A');
                                                    addToString('T');
                                                    addToString('A');
                                                    addToString('[');
                                                    if (!readCDATA())
                                                        return EcmaScript.NET.Token.ERROR;
                                                }
                                                else
                                                {
                                                    // throw away the string in progress
                                                    stringBufferTop = 0;
                                                    this.str = null;
                                                    parser.AddError("msg.XML.bad.form");
                                                    return EcmaScript.NET.Token.ERROR;
                                                }
                                                break;

                                            default:
                                                if (!readEntity())
                                                    return EcmaScript.NET.Token.ERROR;
                                                break;

                                        }
                                        break;

                                    case '?':
                                        c = Char; // Skip ?
                                        addToString(c);
                                        if (!readPI())
                                            return EcmaScript.NET.Token.ERROR;
                                        break;

                                    case '/':
                                        // End tag
                                        c = Char; // Skip /
                                        addToString(c);
                                        if (xmlOpenTagsCount == 0)
                                        {
                                            // throw away the string in progress
                                            stringBufferTop = 0;
                                            this.str = null;
                                            parser.AddError("msg.XML.bad.form");
                                            return EcmaScript.NET.Token.ERROR;
                                        }
                                        xmlIsTagContent = true;
                                        xmlOpenTagsCount--;
                                        break;

                                    default:
                                        // Start tag
                                        xmlIsTagContent = true;
                                        xmlOpenTagsCount++;
                                        break;

                                }
                                break;

                            case '{':
                                ungetChar(c);
                                this.str = StringFromBuffer;
                                return EcmaScript.NET.Token.XML;

                            default:
                                addToString(c);
                                break;

                        }
                    }
                }


                stringBufferTop = 0; // throw away the string in progress
                this.str = null;
                parser.AddError("msg.XML.bad.form");
                return EcmaScript.NET.Token.ERROR;
            }

        }
        private string StringFromBuffer
        {
            get
            {
                return new string(stringBuffer, 0, stringBufferTop);
            }

        }
        private int Char
        {
            get
            {
                if (ungetCursor != 0)
                {
                    return ungetBuffer[--ungetCursor];
                }

                for (; ; )
                {
                    int c;
                    if (sourceString != null)
                    {
                        if (sourceCursor == sourceEnd)
                        {
                            hitEOF = true;
                            return EOF_CHAR;
                        }
                        c = sourceString[sourceCursor++];
                    }
                    else
                    {
                        if (sourceCursor == sourceEnd)
                        {
                            if (!fillSourceBuffer())
                            {
                                hitEOF = true;
                                return EOF_CHAR;
                            }
                        }
                        c = sourceBuffer[sourceCursor++];
                    }

                    if (lineEndChar >= 0)
                    {
                        if (lineEndChar == '\r' && c == '\n')
                        {
                            lineEndChar = '\n';
                            continue;
                        }
                        lineEndChar = -1;
                        lineStart = sourceCursor - 1;
                        lineno++;
                    }

                    if (c <= 127)
                    {
                        if (c == '\n' || c == '\r')
                        {
                            lineEndChar = c;
                            c = '\n';
                        }
                    }
                    else
                    {
                        if (isJSFormatChar(c))
                        {
                            continue;
                        }
                        if (ScriptRuntime.isJSLineTerminator(c))
                        {
                            lineEndChar = c;
                            c = '\n';
                        }
                    }
                    return c;
                }
            }

        }
        internal int Offset
        {
            get
            {
                int n = sourceCursor - lineStart;
                if (lineEndChar >= 0)
                {
                    --n;
                }
                return n;
            }

        }
        internal string Line
        {
            get
            {
                if (sourceString != null)
                {
                    // String case
                    int lineEnd = sourceCursor;
                    if (lineEndChar >= 0)
                    {
                        --lineEnd;
                    }
                    else
                    {
                        for (; lineEnd != sourceEnd; ++lineEnd)
                        {
                            int c = sourceString[lineEnd];
                            if (ScriptRuntime.isJSLineTerminator(c))
                            {
                                break;
                            }
                        }
                    }
                    return sourceString.Substring(lineStart, (lineEnd) - (lineStart));
                }
                else
                {
                    // Reader case
                    int lineLength = sourceCursor - lineStart;
                    if (lineEndChar >= 0)
                    {
                        --lineLength;
                    }
                    else
                    {
                        // Read until the end of line
                        for (; ; ++lineLength)
                        {
                            int i = lineStart + lineLength;
                            if (i == sourceEnd)
                            {
                                try
                                {
                                    if (!fillSourceBuffer())
                                    {
                                        break;
                                    }
                                }
                                catch (System.IO.IOException)
                                {
                                    // ignore it, we're already displaying an error...
                                    break;
                                }
                                // i recalculuation as fillSourceBuffer can move saved
                                // line buffer and change lineStart
                                i = lineStart + lineLength;
                            }
                            int c = sourceBuffer[i];
                            if (ScriptRuntime.isJSLineTerminator(c))
                            {
                                break;
                            }
                        }
                    }
                    return new string(sourceBuffer, lineStart, lineLength);
                }
            }

        }
        /*
        * For chars - because we need something out-of-range
        * to check.  (And checking EOF by exception is annoying.)
        * Note distinction from EOF token type!
        */
        private const int EOF_CHAR = -1;


        internal TokenStream(Parser parser, System.IO.StreamReader sourceReader, string sourceString, int lineno)
        {
            this.parser = parser;
            this.lineno = lineno;
            if (sourceReader != null)
            {
                if (sourceString != null)
                    Context.CodeBug();
                this.sourceReader = sourceReader;
                this.sourceBuffer = new char[512];
                this.sourceEnd = 0;
            }
            else
            {
                if (sourceString == null)
                    Context.CodeBug();
                this.sourceString = sourceString;
                this.sourceEnd = sourceString.Length;
            }
            this.sourceCursor = 0;
        }

        /* This function uses the cached op, string and number fields in
        * TokenStream; if getToken has been called since the passed token
        * was scanned, the op or string printed may be incorrect.
        */
        internal string tokenToString(int token)
        {
            if (EcmaScript.NET.Token.printTrees)
            {
                string name = EcmaScript.NET.Token.name(token);

                switch (token)
                {

                    case EcmaScript.NET.Token.STRING:
                    case EcmaScript.NET.Token.REGEXP:
                    case EcmaScript.NET.Token.NAME:
                        return name + " `" + this.str + "'";


                    case EcmaScript.NET.Token.NUMBER:
                        return "NUMBER " + this.dNumber;
                }

                return name;
            }
            return "";
        }

        internal static bool isKeyword(string s)
        {
            return EcmaScript.NET.Token.EOF != stringToKeyword(s);
        }

        #region Ids
        private const int Id_break = EcmaScript.NET.Token.BREAK;
        private const int Id_case = EcmaScript.NET.Token.CASE;
        private const int Id_continue = EcmaScript.NET.Token.CONTINUE;
        private const int Id_default = EcmaScript.NET.Token.DEFAULT;
        private const int Id_delete = EcmaScript.NET.Token.DELPROP;
        private const int Id_do = EcmaScript.NET.Token.DO;
        private const int Id_else = EcmaScript.NET.Token.ELSE;
        private const int Id_export = EcmaScript.NET.Token.EXPORT;
        private const int Id_false = EcmaScript.NET.Token.FALSE;
        private const int Id_for = EcmaScript.NET.Token.FOR;
        private const int Id_function = EcmaScript.NET.Token.FUNCTION;
        private const int Id_if = EcmaScript.NET.Token.IF;
        private const int Id_in = EcmaScript.NET.Token.IN;
        private const int Id_new = EcmaScript.NET.Token.NEW;
        private const int Id_null = EcmaScript.NET.Token.NULL;
        private const int Id_return = EcmaScript.NET.Token.RETURN;
        private const int Id_switch = EcmaScript.NET.Token.SWITCH;
        private const int Id_this = EcmaScript.NET.Token.THIS;
        private const int Id_true = EcmaScript.NET.Token.TRUE;
        private const int Id_typeof = EcmaScript.NET.Token.TYPEOF;
        private const int Id_var = EcmaScript.NET.Token.VAR;
        private const int Id_void = EcmaScript.NET.Token.VOID;
        private const int Id_while = EcmaScript.NET.Token.WHILE;
        private const int Id_with = EcmaScript.NET.Token.WITH;
        private const int Id_abstract = EcmaScript.NET.Token.RESERVED;
        private const int Id_boolean = EcmaScript.NET.Token.RESERVED;
        private const int Id_byte = EcmaScript.NET.Token.RESERVED;
        private const int Id_catch = EcmaScript.NET.Token.CATCH;
        private const int Id_char = EcmaScript.NET.Token.RESERVED;
        private const int Id_class = EcmaScript.NET.Token.RESERVED;
        private const int Id_const = EcmaScript.NET.Token.RESERVED;
        private const int Id_debugger = EcmaScript.NET.Token.DEBUGGER;
        private const int Id_double = EcmaScript.NET.Token.RESERVED;
        private const int Id_enum = EcmaScript.NET.Token.RESERVED;
        private const int Id_extends = EcmaScript.NET.Token.RESERVED;
        private const int Id_final = EcmaScript.NET.Token.RESERVED;
        private const int Id_finally = EcmaScript.NET.Token.FINALLY;
        private const int Id_float = EcmaScript.NET.Token.RESERVED;
        private const int Id_goto = EcmaScript.NET.Token.RESERVED;
        private const int Id_implements = EcmaScript.NET.Token.RESERVED;
        private const int Id_import = EcmaScript.NET.Token.IMPORT;
        private const int Id_instanceof = EcmaScript.NET.Token.INSTANCEOF;
        private const int Id_int = EcmaScript.NET.Token.RESERVED;
        private const int Id_interface = EcmaScript.NET.Token.RESERVED;
        private const int Id_long = EcmaScript.NET.Token.RESERVED;
        private const int Id_native = EcmaScript.NET.Token.RESERVED;
        private const int Id_package = EcmaScript.NET.Token.RESERVED;
        private const int Id_private = EcmaScript.NET.Token.RESERVED;
        private const int Id_protected = EcmaScript.NET.Token.RESERVED;
        private const int Id_public = EcmaScript.NET.Token.RESERVED;
        private const int Id_short = EcmaScript.NET.Token.RESERVED;
        private const int Id_static = EcmaScript.NET.Token.RESERVED;
        private const int Id_super = EcmaScript.NET.Token.RESERVED;
        private const int Id_synchronized = EcmaScript.NET.Token.RESERVED;
        private const int Id_throw = EcmaScript.NET.Token.THROW;
        private const int Id_throws = EcmaScript.NET.Token.RESERVED;
        private const int Id_transient = EcmaScript.NET.Token.RESERVED;
        private const int Id_try = EcmaScript.NET.Token.TRY;
        private const int Id_volatile = EcmaScript.NET.Token.RESERVED;
        #endregion

        private static int stringToKeyword(string name)
        {
            // The following assumes that EcmaScript.NET.Token.EOF == 0			
            int id;
            string s = name;
            #region Generated Id Switch
        L0:
            {
                id = 0;
                string X = null;
                int c;
            L:
                switch (s.Length)
                {
                    case 2:
                        c = s[1];
                        if (c == 'f') { if (s[0] == 'i') { id = Id_if; goto EL0; } }
                        else if (c == 'n') { if (s[0] == 'i') { id = Id_in; goto EL0; } }
                        else if (c == 'o') { if (s[0] == 'd') { id = Id_do; goto EL0; } }
                        break;
                    case 3:
                        switch (s[0])
                        {
                            case 'f':
                                if (s[2] == 'r' && s[1] == 'o') { id = Id_for; goto EL0; }
                                break;
                            case 'i':
                                if (s[2] == 't' && s[1] == 'n') { id = Id_int; goto EL0; }
                                break;
                            case 'n':
                                if (s[2] == 'w' && s[1] == 'e') { id = Id_new; goto EL0; }
                                break;
                            case 't':
                                if (s[2] == 'y' && s[1] == 'r') { id = Id_try; goto EL0; }
                                break;
                            case 'v':
                                if (s[2] == 'r' && s[1] == 'a') { id = Id_var; goto EL0; }
                                break;
                        }
                        break;
                    case 4:
                        switch (s[0])
                        {
                            case 'b':
                                X = "byte";
                                id = Id_byte;
                                break;
                            case 'c':
                                c = s[3];
                                if (c == 'e') { if (s[2] == 's' && s[1] == 'a') { id = Id_case; goto EL0; } }
                                else if (c == 'r') { if (s[2] == 'a' && s[1] == 'h') { id = Id_char; goto EL0; } }
                                break;
                            case 'e':
                                c = s[3];
                                if (c == 'e') { if (s[2] == 's' && s[1] == 'l') { id = Id_else; goto EL0; } }
                                else if (c == 'm') { if (s[2] == 'u' && s[1] == 'n') { id = Id_enum; goto EL0; } }
                                break;
                            case 'g':
                                X = "goto";
                                id = Id_goto;
                                break;
                            case 'l':
                                X = "long";
                                id = Id_long;
                                break;
                            case 'n':
                                X = "null";
                                id = Id_null;
                                break;
                            case 't':
                                c = s[3];
                                if (c == 'e') { if (s[2] == 'u' && s[1] == 'r') { id = Id_true; goto EL0; } }
                                else if (c == 's') { if (s[2] == 'i' && s[1] == 'h') { id = Id_this; goto EL0; } }
                                break;
                            case 'v':
                                X = "void";
                                id = Id_void;
                                break;
                            case 'w':
                                X = "with";
                                id = Id_with;
                                break;
                        }
                        break;
                    case 5:
                        switch (s[2])
                        {
                            case 'a':
                                X = "class";
                                id = Id_class;
                                break;
                            case 'e':
                                X = "break";
                                id = Id_break;
                                break;
                            case 'i':
                                X = "while";
                                id = Id_while;
                                break;
                            case 'l':
                                X = "false";
                                id = Id_false;
                                break;
                            case 'n':
                                c = s[0];
                                if (c == 'c') { X = "const"; id = Id_const; }
                                else if (c == 'f') { X = "final"; id = Id_final; }
                                break;
                            case 'o':
                                c = s[0];
                                if (c == 'f') { X = "float"; id = Id_float; }
                                else if (c == 's') { X = "short"; id = Id_short; }
                                break;
                            case 'p':
                                X = "super";
                                id = Id_super;
                                break;
                            case 'r':
                                X = "throw";
                                id = Id_throw;
                                break;
                            case 't':
                                X = "catch";
                                id = Id_catch;
                                break;
                        }
                        break;
                    case 6:
                        switch (s[1])
                        {
                            case 'a':
                                X = "native";
                                id = Id_native;
                                break;
                            case 'e':
                                c = s[0];
                                if (c == 'd') { X = "delete"; id = Id_delete; }
                                else if (c == 'r') { X = "return"; id = Id_return; }
                                break;
                            case 'h':
                                X = "throws";
                                id = Id_throws;
                                break;
                            case 'm':
                                X = "import";
                                id = Id_import;
                                break;
                            case 'o':
                                X = "double";
                                id = Id_double;
                                break;
                            case 't':
                                X = "static";
                                id = Id_static;
                                break;
                            case 'u':
                                X = "public";
                                id = Id_public;
                                break;
                            case 'w':
                                X = "switch";
                                id = Id_switch;
                                break;
                            case 'x':
                                X = "export";
                                id = Id_export;
                                break;
                            case 'y':
                                X = "typeof";
                                id = Id_typeof;
                                break;
                        }
                        break;
                    case 7:
                        switch (s[1])
                        {
                            case 'a':
                                X = "package";
                                id = Id_package;
                                break;
                            case 'e':
                                X = "default";
                                id = Id_default;
                                break;
                            case 'i':
                                X = "finally";
                                id = Id_finally;
                                break;
                            case 'o':
                                X = "boolean";
                                id = Id_boolean;
                                break;
                            case 'r':
                                X = "private";
                                id = Id_private;
                                break;
                            case 'x':
                                X = "extends";
                                id = Id_extends;
                                break;
                        }
                        break;
                    case 8:
                        switch (s[0])
                        {
                            case 'a':
                                X = "abstract";
                                id = Id_abstract;
                                break;
                            case 'c':
                                X = "continue";
                                id = Id_continue;
                                break;
                            case 'd':
                                X = "debugger";
                                id = Id_debugger;
                                break;
                            case 'f':
                                X = "function";
                                id = Id_function;
                                break;
                            case 'v':
                                X = "volatile";
                                id = Id_volatile;
                                break;
                        }
                        break;
                    case 9:
                        c = s[0];
                        if (c == 'i') { X = "interface"; id = Id_interface; }
                        else if (c == 'p') { X = "protected"; id = Id_protected; }
                        else if (c == 't') { X = "transient"; id = Id_transient; }
                        break;
                    case 10:
                        c = s[1];
                        if (c == 'm') { X = "implements"; id = Id_implements; }
                        else if (c == 'n') { X = "instanceof"; id = Id_instanceof; }
                        break;
                    case 12:
                        X = "synchronized";
                        id = Id_synchronized;
                        break;
                }
                if (X != null && X != s && !X.Equals(s))
                    id = 0;
            }
        EL0:

            #endregion
            if (id == 0)
            {
                return EcmaScript.NET.Token.EOF;
            }
            return id & 0xff;
        }

        internal bool eof()
        {
            return hitEOF;
        }

        private static bool isAlpha(int c)
        {
            // Use 'Z' < 'a'
            if (c <= 'Z')
            {
                return 'A' <= c;
            }
            else
            {
                return 'a' <= c && c <= 'z';
            }
        }

        internal static bool isDigit(int c)
        {
            return '0' <= c && c <= '9';
        }

        /* As defined in ECMA.  jsscan.c uses C isspace() (which allows
        * \v, I think.)  note that code in getChar() implicitly accepts
        * '\r' == 
        as well.
        */
        internal static bool isJSSpace(int c)
        {
            if (c <= 127)
            {
                return c == 0x20 || c == 0x9 || c == 0xC || c == 0xB;
            }
            else
            {
                return c == 0xA0 || (int)char.GetUnicodeCategory((char)c) == (sbyte)System.Globalization.UnicodeCategory.SpaceSeparator;
            }
        }

        private static bool isJSFormatChar(int c)
        {
            return c > 127 && (int)char.GetUnicodeCategory((char)c) == (sbyte)System.Globalization.UnicodeCategory.Format;
        }

        /// <summary> Parser calls the method when it gets / or /= in literal context.</summary>
        internal void readRegExp(int startToken)
        {
            stringBufferTop = 0;
            if (startToken == EcmaScript.NET.Token.ASSIGN_DIV)
            {
                // Miss-scanned /=
                addToString('=');
            }
            else
            {
                if (startToken != EcmaScript.NET.Token.DIV)
                    Context.CodeBug();
            }

            int c;
            bool inClass = false;
            while ((c = Char) != '/' || inClass)
            {
                if (c == '\n' || c == EOF_CHAR)
                {
                    ungetChar(c);
                    throw parser.ReportError("msg.unterminated.re.lit");
                }
                if (c == '\\')
                {
                    addToString(c);
                    c = Char;
                }
                else if (c == '[')
                {
                    inClass = true;
                }
                else if (c == ']')
                {
                    inClass = false;
                }

                addToString(c);
            }

            int reEnd = stringBufferTop;

            while (true)
            {
                if (matchChar('g'))
                    addToString('g');
                else if (matchChar('i'))
                    addToString('i');
                else if (matchChar('m'))
                    addToString('m');
                else
                    break;
            }

            if (isAlpha(peekChar()))
            {
                throw parser.ReportError("msg.invalid.re.flag");
            }

            this.str = new string(stringBuffer, 0, reEnd);
            this.regExpFlags = new string(stringBuffer, reEnd, stringBufferTop - reEnd);
        }

        /// <summary> </summary>
        private bool readQuotedString(int quote)
        {
            for (int c = Char; c != EOF_CHAR; c = Char)
            {
                addToString(c);
                if (c == quote)
                    return true;
            }

            stringBufferTop = 0; // throw away the string in progress
            this.str = null;
            parser.AddError("msg.XML.bad.form");
            return false;
        }

        /// <summary> </summary>
        private bool readXmlComment()
        {
            for (int c = Char; c != EOF_CHAR; )
            {
                addToString(c);
                if (c == '-' && peekChar() == '-')
                {
                    c = Char;
                    addToString(c);
                    if (peekChar() == '>')
                    {
                        c = Char; // Skip >
                        addToString(c);
                        return true;
                    }
                    else
                    {
                        continue;
                    }
                }
                c = Char;
            }

            stringBufferTop = 0; // throw away the string in progress
            this.str = null;
            parser.AddError("msg.XML.bad.form");
            return false;
        }

        /// <summary> </summary>
        private bool readCDATA()
        {
            for (int c = Char; c != EOF_CHAR; )
            {
                addToString(c);
                if (c == ']' && peekChar() == ']')
                {
                    c = Char;
                    addToString(c);
                    if (peekChar() == '>')
                    {
                        c = Char; // Skip >
                        addToString(c);
                        return true;
                    }
                    else
                    {
                        continue;
                    }
                }
                c = Char;
            }

            stringBufferTop = 0; // throw away the string in progress
            this.str = null;
            parser.AddError("msg.XML.bad.form");
            return false;
        }

        /// <summary> </summary>
        private bool readEntity()
        {
            int declTags = 1;
            for (int c = Char; c != EOF_CHAR; c = Char)
            {
                addToString(c);
                switch (c)
                {

                    case '<':
                        declTags++;
                        break;

                    case '>':
                        declTags--;
                        if (declTags == 0)
                            return true;
                        break;
                }
            }

            stringBufferTop = 0; // throw away the string in progress
            this.str = null;
            parser.AddError("msg.XML.bad.form");
            return false;
        }

        /// <summary> </summary>
        private bool readPI()
        {
            for (int c = Char; c != EOF_CHAR; c = Char)
            {
                addToString(c);
                if (c == '?' && peekChar() == '>')
                {
                    c = Char; // Skip >
                    addToString(c);
                    return true;
                }
            }

            stringBufferTop = 0; // throw away the string in progress
            this.str = null;
            parser.AddError("msg.XML.bad.form");
            return false;
        }

        private void addToString(int c)
        {
            int N = stringBufferTop;
            if (N == stringBuffer.Length)
            {
                char[] tmp = new char[stringBuffer.Length * 4];
                Array.Copy(stringBuffer, 0, tmp, 0, N);
                stringBuffer = tmp;
            }
            stringBuffer[N] = (char)c;
            stringBufferTop = N + 1;
        }

        private void ungetChar(int c)
        {
            // can not unread past across line boundary
            if (ungetCursor != 0 && ungetBuffer[ungetCursor - 1] == '\n')
                Context.CodeBug();
            ungetBuffer[ungetCursor++] = c;
        }

        private bool matchChar(int test)
        {
            int c = Char;
            if (c == test)
            {
                return true;
            }
            else
            {
                ungetChar(c);
                return false;
            }
        }

        private int peekChar()
        {
            int c = Char;
            ungetChar(c);
            return c;
        }

        private void skipLine()
        {
            // skip to end of line
            int c;
            while ((c = Char) != EOF_CHAR && c != '\n')
            {
            }
            ungetChar(c);
        }

        private bool fillSourceBuffer()
        {
            if (sourceString != null)
                Context.CodeBug();
            if (sourceEnd == sourceBuffer.Length)
            {
                if (lineStart != 0)
                {
                    Array.Copy(sourceBuffer, lineStart, sourceBuffer, 0, sourceEnd - lineStart);
                    sourceEnd -= lineStart;
                    sourceCursor -= lineStart;
                    lineStart = 0;
                }
                else
                {
                    char[] tmp = new char[sourceBuffer.Length * 2];
                    Array.Copy(sourceBuffer, 0, tmp, 0, sourceEnd);
                    sourceBuffer = tmp;
                }
            }
            int n = sourceReader.Read(sourceBuffer, sourceEnd, sourceBuffer.Length - sourceEnd);
            if (n <= 0)
            {
                return false;
            }
            sourceEnd += n;
            return true;
        }

        // stuff other than whitespace since start of line
        private bool dirtyLine;

        internal string regExpFlags;


        // Set this to an inital non-null value so that the Parser has
        // something to retrieve even if an error has occured and no
        // string is found.  Fosters one class of error, but saves lots of
        // code.
        private string str = "";
        private string tokenstr = "";
        private double dNumber;


        private char[] stringBuffer = new char[128];
        private int stringBufferTop;
        private ObjToIntMap allStrings = new ObjToIntMap(50);

        // Room to backtrace from to < on failed match of the last - in <!--		
        private int[] ungetBuffer = new int[3];
        private int ungetCursor;

        private bool hitEOF = false;

        private int lineStart = 0;
        private int lineno;
        private int lineEndChar = -1;

        private string sourceString;
        private System.IO.StreamReader sourceReader;
        private char[] sourceBuffer;
        private int sourceEnd;
        private int sourceCursor;

        // for xml tokenizer
        private bool xmlIsAttribute;
        private bool xmlIsTagContent;
        private int xmlOpenTagsCount;

        private Parser parser;

        // FIXME: we don't check for combining mark yet
        internal static bool IsJavaIdentifierPart(char c)
        {
            if (char.IsLetterOrDigit(c))
                return true;
            UnicodeCategory unicode_category = char.GetUnicodeCategory(c);
            return unicode_category == UnicodeCategory.CurrencySymbol ||
                unicode_category == UnicodeCategory.ConnectorPunctuation ||
                unicode_category == UnicodeCategory.LetterNumber ||
                unicode_category == UnicodeCategory.NonSpacingMark || IsIdentifierIgnorable(c);
        }

        internal static bool IsIdentifierIgnorable(char c)
        {
            return (c >= '\u0000' && c <= '\u0008') || (c >= '\u000E' && c <= '\u001B') ||
                (c >= '\u007F' && c <= '\u009F') || char.GetUnicodeCategory(c) == UnicodeCategory.Format;
        }

        internal static bool IsJavaIdentifierStart(char c)
        {
            if (char.IsLetter(c))
            {
                return true;
            }

            UnicodeCategory unicode_category = char.GetUnicodeCategory(c);
            return unicode_category == UnicodeCategory.LetterNumber ||
                unicode_category == UnicodeCategory.CurrencySymbol ||
                unicode_category == UnicodeCategory.ConnectorPunctuation;
        }
    }

}
