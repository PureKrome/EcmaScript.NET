//------------------------------------------------------------------------------
// <license file="Parser.cs">
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
using System.Collections;
using System.Text.RegularExpressions;

using EcmaScript.NET.Collections;

namespace EcmaScript.NET
{

    /// <summary> This class implements the JavaScript parser.
    /// 
    /// It is based on the C source files jsparse.c and jsparse.h
    /// in the jsref package.
    /// 
    /// </summary>	
    public class Parser
    {
        public string EncodedSource
        {
            get
            {
                return encodedSource;
            }

        }
        // TokenInformation flags : currentFlaggedToken stores them together
        // with token type
        internal const int CLEAR_TI_MASK = 0xFFFF;
        internal const int TI_AFTER_EOL = 1 << 16;
        internal const int TI_CHECK_LABEL = 1 << 17; // indicates to check for label

        internal readonly Regex SIMPLE_IDENTIFIER_NAME_PATTERN = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

        internal CompilerEnvirons compilerEnv;
        ErrorReporter errorReporter;
        string sourceURI;
        internal bool calledByCompileFunction;

        TokenStream ts;
        int currentFlaggedToken;
        int syntaxErrorCount;

        NodeFactory nf;

        int nestingOfFunction;

        Decompiler decompiler;
        string encodedSource;

        // The following are per function variables and should be saved/restored
        // during function parsing.
        // TODO: Move to separated class?
        internal ScriptOrFnNode currentScriptOrFn;
        int nestingOfWith;
        Hashtable labelSet; // map of label names into nodes
        ObjArray loopSet;
        ObjArray loopAndSwitchSet;
        // end of per function variables

        // Exception to unwind

        class ParserException : ApplicationException
        {

        }

        public Parser(CompilerEnvirons compilerEnv, ErrorReporter errorReporter)
        {
            this.compilerEnv = compilerEnv;
            this.errorReporter = errorReporter;
        }

        Decompiler CreateDecompiler(CompilerEnvirons compilerEnv)
        {
            return new Decompiler();
        }

        internal void AddWarning(string messageId, string messageArg)
        {
            string message = ScriptRuntime.GetMessage(messageId, messageArg);
            errorReporter.Warning(message, sourceURI, ts.Lineno, ts.Line, ts.Offset);
        }

        internal void AddError(string messageId)
        {
            ++syntaxErrorCount;
            string message = ScriptRuntime.GetMessage(messageId);
            errorReporter.Error(message, sourceURI, ts.Lineno, ts.Line, ts.Offset);
        }

        internal Exception ReportError(string messageId)
        {
            AddError(messageId);

            // Throw a ParserException exception to unwind the recursive descent
            // parse.
            throw new ParserException();
        }

        int peekToken()
        {
            int tt = currentFlaggedToken;
            if (tt == Token.EOF)
            {

                while ((tt = ts.Token) == Token.CONDCOMMENT || tt == Token.KEEPCOMMENT)
                {
                    if (tt == Token.CONDCOMMENT)
                    {
                        /* Support for JScript conditional comments */
                        decompiler.AddJScriptConditionalComment(ts.String);
                    }
                    else
                    {
                        /* Support for preserved comments */
                        decompiler.AddPreservedComment(ts.String);
                    }
                }

                if (tt == Token.EOL)
                {
                    do
                    {
                        tt = ts.Token;

                        if (tt == Token.CONDCOMMENT)
                        {
                            /* Support for JScript conditional comments */
                            decompiler.AddJScriptConditionalComment(ts.String);
                        }
                        else if (tt == Token.KEEPCOMMENT)
                        {
                            /* Support for preserved comments */
                            decompiler.AddPreservedComment(ts.String);
                        }

                    }
                    while (tt == Token.EOL || tt == Token.CONDCOMMENT || tt == Token.KEEPCOMMENT);
                    tt |= TI_AFTER_EOL;
                }
                currentFlaggedToken = tt;
            }
            return tt & CLEAR_TI_MASK;
        }

        int peekFlaggedToken()
        {
            peekToken();
            return currentFlaggedToken;
        }

        void consumeToken()
        {
            currentFlaggedToken = Token.EOF;
        }

        int nextToken()
        {
            int tt = peekToken();
            consumeToken();
            return tt;
        }

        int nextFlaggedToken()
        {
            peekToken();
            int ttFlagged = currentFlaggedToken;
            consumeToken();
            return ttFlagged;
        }

        bool matchToken(int toMatch)
        {
            int tt = peekToken();
            if (tt != toMatch)
            {
                return false;
            }
            consumeToken();
            return true;
        }

        int peekTokenOrEOL()
        {
            int tt = peekToken();
            // Check for last peeked token flags
            if ((currentFlaggedToken & TI_AFTER_EOL) != 0)
            {
                tt = Token.EOL;
            }
            return tt;
        }

        void setCheckForLabel()
        {
            if ((currentFlaggedToken & CLEAR_TI_MASK) != Token.NAME)
                throw Context.CodeBug();
            currentFlaggedToken |= TI_CHECK_LABEL;
        }

        void mustMatchToken(int toMatch, string messageId)
        {
            if (!matchToken(toMatch))
            {
                ReportError(messageId);
            }
        }

        void mustHaveXML()
        {
            if (!compilerEnv.isXmlAvailable())
            {
                ReportError("msg.XML.not.available");
            }
        }

        public bool Eof
        {
            get
            {
                return ts.eof();
            }
        }

        internal bool insideFunction()
        {
            return nestingOfFunction != 0;
        }

        Node enterLoop(Node loopLabel)
        {
            Node loop = nf.CreateLoopNode(loopLabel, ts.Lineno);
            if (loopSet == null)
            {
                loopSet = new ObjArray();
                if (loopAndSwitchSet == null)
                {
                    loopAndSwitchSet = new ObjArray();
                }
            }
            loopSet.push(loop);
            loopAndSwitchSet.push(loop);
            return loop;
        }

        void exitLoop()
        {
            loopSet.pop();
            loopAndSwitchSet.pop();
        }

        Node enterSwitch(Node switchSelector, int lineno, Node switchLabel)
        {
            Node switchNode = nf.CreateSwitch(switchSelector, lineno);
            if (loopAndSwitchSet == null)
            {
                loopAndSwitchSet = new ObjArray();
            }
            loopAndSwitchSet.push(switchNode);
            return switchNode;
        }

        void exitSwitch()
        {
            loopAndSwitchSet.pop();
        }

        /*
        * Build a parse tree from the given sourceString.
        *
        * @return an Object representing the parsed
        * program.  If the parse fails, null will be returned.  (The
        * parse failure will result in a call to the ErrorReporter from
        * CompilerEnvirons.)
        */
        public ScriptOrFnNode Parse(string sourceString, string sourceURI, int lineno)
        {
            this.sourceURI = sourceURI;
            this.ts = new TokenStream(this, null, sourceString, lineno);
            try
            {
                return Parse();
            }
            catch (System.IO.IOException)
            {
                // Should never happen
                throw new ApplicationException();
            }
        }

        /*
        * Build a parse tree from the given sourceString.
        *
        * @return an Object representing the parsed
        * program.  If the parse fails, null will be returned.  (The
        * parse failure will result in a call to the ErrorReporter from
        * CompilerEnvirons.)
        */
        public ScriptOrFnNode Parse(System.IO.StreamReader sourceReader, string sourceURI, int lineno)
        {
            this.sourceURI = sourceURI;
            this.ts = new TokenStream(this, sourceReader, null, lineno);
            return Parse();
        }

        ScriptOrFnNode Parse()
        {
            this.decompiler = CreateDecompiler(compilerEnv);
            this.nf = new NodeFactory(this);
            currentScriptOrFn = nf.CreateScript();
            int sourceStartOffset = decompiler.CurrentOffset;
            this.encodedSource = null;
            decompiler.AddToken(Token.SCRIPT);

            this.currentFlaggedToken = Token.EOF;
            this.syntaxErrorCount = 0;

            int baseLineno = ts.Lineno; // line number where source starts

            /* so we have something to add nodes to until
            * we've collected all the source */
            Node pn = nf.CreateLeaf(Token.BLOCK);

            for (; ; )
            {
                int tt = peekToken();

                if (tt <= Token.EOF)
                {
                    break;
                }

                Node n;
                if (tt == Token.FUNCTION)
                {
                    consumeToken();
                    try
                    {
                        n = function(calledByCompileFunction ? FunctionNode.FUNCTION_EXPRESSION : FunctionNode.FUNCTION_STATEMENT);
                    }
                    catch (ParserException)
                    {
                        break;
                    }
                }
                else
                {
                    n = statement();
                }
                nf.addChildToBack(pn, n);
            }

            if (this.syntaxErrorCount != 0)
            {
                string msg = Convert.ToString(this.syntaxErrorCount);
                msg = ScriptRuntime.GetMessage("msg.got.syntax.errors", msg);
                throw errorReporter.RuntimeError(msg, sourceURI, baseLineno, null, 0);
            }

            currentScriptOrFn.SourceName = sourceURI;
            currentScriptOrFn.BaseLineno = baseLineno;
            currentScriptOrFn.EndLineno = ts.Lineno;

            int sourceEndOffset = decompiler.CurrentOffset;
            currentScriptOrFn.setEncodedSourceBounds(sourceStartOffset, sourceEndOffset);

            nf.initScript(currentScriptOrFn, pn);

            if (compilerEnv.isGeneratingSource())
            {
                encodedSource = decompiler.EncodedSource;
            }
            this.decompiler = null; // It helps GC

            return currentScriptOrFn;
        }

        /*
        * The C version of this function takes an argument list,
        * which doesn't seem to be needed for tree generation...
        * it'd only be useful for checking argument hiding, which
        * I'm not doing anyway...
        */
        Node parseFunctionBody()
        {
            ++nestingOfFunction;
            Node pn = nf.CreateBlock(ts.Lineno);
            try
            {
                for (; ; )
                {
                    Node n;
                    int tt = peekToken();
                    switch (tt)
                    {

                        case Token.ERROR:
                        case Token.EOF:
                        case Token.RC:

                            goto bodyLoop_brk;


                        case Token.FUNCTION:
                            consumeToken();
                            n = function(FunctionNode.FUNCTION_STATEMENT);
                            break;

                        default:
                            n = statement();
                            break;

                    }
                    nf.addChildToBack(pn, n);
                }

            bodyLoop_brk:
                ;

            }
            catch (ParserException)
            {
                // Ignore it
            }
            finally
            {
                --nestingOfFunction;
            }

            return pn;
        }

        Node function(int functionType)
        {
            using (Helpers.StackOverflowVerifier sov = new Helpers.StackOverflowVerifier(1024))
            {
                int syntheticType = functionType;
                int baseLineno = ts.Lineno; // line number where source starts

                int functionSourceStart = decompiler.MarkFunctionStart(functionType);
                string name;
                Node memberExprNode = null;
                if (matchToken(Token.NAME))
                {
                    name = ts.String;
                    decompiler.AddName(name);
                    if (!matchToken(Token.LP))
                    {
                        if (compilerEnv.isAllowMemberExprAsFunctionName())
                        {
                            // Extension to ECMA: if 'function <name>' does not follow
                            // by '(', assume <name> starts memberExpr
                            Node memberExprHead = nf.CreateName(name);
                            name = "";
                            memberExprNode = memberExprTail(false, memberExprHead);
                        }
                        mustMatchToken(Token.LP, "msg.no.paren.parms");
                    }
                }
                else if (matchToken(Token.LP))
                {
                    // Anonymous function
                    name = "";
                }
                else
                {
                    name = "";
                    if (compilerEnv.isAllowMemberExprAsFunctionName())
                    {
                        // Note that memberExpr can not start with '(' like
                        // in function (1+2).toString(), because 'function (' already
                        // processed as anonymous function
                        memberExprNode = memberExpr(false);
                    }
                    mustMatchToken(Token.LP, "msg.no.paren.parms");
                }

                if (memberExprNode != null)
                {
                    syntheticType = FunctionNode.FUNCTION_EXPRESSION;
                }

                bool nested = insideFunction();

                FunctionNode fnNode = nf.CreateFunction(name);
                if (nested || nestingOfWith > 0)
                {
                    // 1. Nested functions are not affected by the dynamic scope flag
                    // as dynamic scope is already a parent of their scope.
                    // 2. Functions defined under the with statement also immune to
                    // this setup, in which case dynamic scope is ignored in favor
                    // of with object.
                    fnNode.itsIgnoreDynamicScope = true;
                }

                int functionIndex = currentScriptOrFn.addFunction(fnNode);

                int functionSourceEnd;

                ScriptOrFnNode savedScriptOrFn = currentScriptOrFn;
                currentScriptOrFn = fnNode;
                int savedNestingOfWith = nestingOfWith;
                nestingOfWith = 0;
                Hashtable savedLabelSet = labelSet;
                labelSet = null;
                ObjArray savedLoopSet = loopSet;
                loopSet = null;
                ObjArray savedLoopAndSwitchSet = loopAndSwitchSet;
                loopAndSwitchSet = null;

                Node body;
                try
                {
                    decompiler.AddToken(Token.LP);
                    if (!matchToken(Token.RP))
                    {
                        bool first = true;
                        do
                        {
                            if (!first)
                                decompiler.AddToken(Token.COMMA);
                            first = false;
                            mustMatchToken(Token.NAME, "msg.no.parm");
                            string s = ts.String;
                            if (fnNode.hasParamOrVar(s))
                            {
                                AddWarning("msg.dup.parms", s);
                            }
                            fnNode.addParam(s);
                            decompiler.AddName(s);
                        }
                        while (matchToken(Token.COMMA));

                        mustMatchToken(Token.RP, "msg.no.paren.after.parms");
                    }
                    decompiler.AddToken(Token.RP);

                    mustMatchToken(Token.LC, "msg.no.brace.body");
                    decompiler.AddEol(Token.LC);
                    body = parseFunctionBody();
                    mustMatchToken(Token.RC, "msg.no.brace.after.body");

                    decompiler.AddToken(Token.RC);
                    functionSourceEnd = decompiler.MarkFunctionEnd(functionSourceStart);
                    if (functionType != FunctionNode.FUNCTION_EXPRESSION)
                    {
                        if (compilerEnv.LanguageVersion >= Context.Versions.JS1_2)
                        {
                            // function f() {} function g() {} is not allowed in 1.2
                            // or later but for compatibility with old scripts
                            // the check is done only if language is
                            // explicitly set.
                            //  TODO: warning needed if version == VERSION_DEFAULT ?
                            int tt = peekTokenOrEOL();
                            if (tt == Token.FUNCTION)
                            {
                                ReportError("msg.no.semi.stmt");
                            }
                        }
                        // Add EOL only if function is not part of expression
                        // since it gets SEMI + EOL from Statement in that case
                        decompiler.AddToken(Token.EOL);
                    }
                }
                finally
                {
                    loopAndSwitchSet = savedLoopAndSwitchSet;
                    loopSet = savedLoopSet;
                    labelSet = savedLabelSet;
                    nestingOfWith = savedNestingOfWith;
                    currentScriptOrFn = savedScriptOrFn;
                }

                fnNode.setEncodedSourceBounds(functionSourceStart, functionSourceEnd);
                fnNode.SourceName = sourceURI;
                fnNode.BaseLineno = baseLineno;
                fnNode.EndLineno = ts.Lineno;

                Node pn = nf.initFunction(fnNode, functionIndex, body, syntheticType);
                if (memberExprNode != null)
                {
                    pn = nf.CreateAssignment(Token.ASSIGN, memberExprNode, pn);
                    if (functionType != FunctionNode.FUNCTION_EXPRESSION)
                    {
                        // TOOD: check JScript behavior: should it be createExprStatement?
                        pn = nf.CreateExprStatementNoReturn(pn, baseLineno);
                    }
                }
                return pn;
            }
        }

        Node statements()
        {
            Node pn = nf.CreateBlock(ts.Lineno);

            int tt;
            while ((tt = peekToken()) > Token.EOF && tt != Token.RC)
            {
                nf.addChildToBack(pn, statement());
            }

            return pn;
        }

        Node condition()
        {
            Node pn;
            mustMatchToken(Token.LP, "msg.no.paren.cond");
            decompiler.AddToken(Token.LP);
            pn = expr(false);
            mustMatchToken(Token.RP, "msg.no.paren.after.cond");
            decompiler.AddToken(Token.RP);

            // there's a check here in jsparse.c that corrects = to ==

            return pn;
        }

        // match a NAME; return null if no match.
        Node matchJumpLabelName()
        {
            Node label = null;

            int tt = peekTokenOrEOL();
            if (tt == Token.NAME)
            {
                consumeToken();
                string name = ts.String;
                decompiler.AddName(name);
                if (labelSet != null)
                {
                    label = (Node)labelSet[name];
                }
                if (label == null)
                {
                    ReportError("msg.undef.label");
                }
            }

            return label;
        }

        Node statement()
        {
            using (Helpers.StackOverflowVerifier sov = new Helpers.StackOverflowVerifier(512))
            {
                try
                {
                    Node pn = statementHelper(null);
                    if (pn != null)
                    {
                        return pn;
                    }
                }
                catch (ParserException)
                {
                }
            }

            // skip to end of statement
            int lineno = ts.Lineno;
            for (; ; )
            {
                int tt = peekTokenOrEOL();
                consumeToken();
                switch (tt)
                {

                    case Token.ERROR:
                    case Token.EOF:
                    case Token.EOL:
                    case Token.SEMI:

                        goto guessingStatementEnd_brk;
                }
            }

        guessingStatementEnd_brk:
            ;

            return nf.CreateExprStatement(nf.CreateName("error"), lineno);
        }

        /// <summary> Whether the "catch (e: e instanceof Exception) { ... }" syntax
        /// is implemented.
        /// </summary>

        Node statementHelper(Node statementLabel)
        {
            Node pn = null;

            int tt;

            tt = peekToken();

            switch (tt)
            {

                case Token.IF:
                    {
                        consumeToken();

                        decompiler.AddToken(Token.IF);
                        int lineno = ts.Lineno;
                        Node cond = condition();
                        decompiler.AddEol(Token.LC);
                        Node ifTrue = statement();
                        Node ifFalse = null;
                        if (matchToken(Token.ELSE))
                        {
                            decompiler.AddToken(Token.RC);
                            decompiler.AddToken(Token.ELSE);
                            decompiler.AddEol(Token.LC);
                            ifFalse = statement();
                        }
                        decompiler.AddEol(Token.RC);
                        pn = nf.CreateIf(cond, ifTrue, ifFalse, lineno);
                        return pn;
                    }


                case Token.SWITCH:
                    {
                        consumeToken();

                        decompiler.AddToken(Token.SWITCH);
                        int lineno = ts.Lineno;
                        mustMatchToken(Token.LP, "msg.no.paren.switch");
                        decompiler.AddToken(Token.LP);
                        pn = enterSwitch(expr(false), lineno, statementLabel);
                        try
                        {
                            mustMatchToken(Token.RP, "msg.no.paren.after.switch");
                            decompiler.AddToken(Token.RP);
                            mustMatchToken(Token.LC, "msg.no.brace.switch");
                            decompiler.AddEol(Token.LC);

                            bool hasDefault = false;
                            for (; ; )
                            {
                                tt = nextToken();
                                Node caseExpression;
                                switch (tt)
                                {

                                    case Token.RC:

                                        goto switchLoop_brk;


                                    case Token.CASE:
                                        decompiler.AddToken(Token.CASE);
                                        caseExpression = expr(false);
                                        mustMatchToken(Token.COLON, "msg.no.colon.case");
                                        decompiler.AddEol(Token.COLON);
                                        break;


                                    case Token.DEFAULT:
                                        if (hasDefault)
                                        {
                                            ReportError("msg.double.switch.default");
                                        }
                                        decompiler.AddToken(Token.DEFAULT);
                                        hasDefault = true;
                                        caseExpression = null;
                                        mustMatchToken(Token.COLON, "msg.no.colon.case");
                                        decompiler.AddEol(Token.COLON);
                                        break;


                                    default:
                                        ReportError("msg.bad.switch");

                                        goto switchLoop_brk;

                                }

                                Node block = nf.CreateLeaf(Token.BLOCK);
                                while ((tt = peekToken()) != Token.RC && tt != Token.CASE && tt != Token.DEFAULT && tt != Token.EOF)
                                {
                                    nf.addChildToBack(block, statement());
                                }

                                // caseExpression == null => add default lable
                                nf.addSwitchCase(pn, caseExpression, block);
                            }

                        switchLoop_brk:
                            ;

                            decompiler.AddEol(Token.RC);
                            nf.closeSwitch(pn);
                        }
                        finally
                        {
                            exitSwitch();
                        }
                        return pn;
                    }


                case Token.WHILE:
                    {
                        consumeToken();
                        decompiler.AddToken(Token.WHILE);

                        Node loop = enterLoop(statementLabel);
                        try
                        {
                            Node cond = condition();
                            decompiler.AddEol(Token.LC);
                            Node body = statement();
                            decompiler.AddEol(Token.RC);
                            pn = nf.CreateWhile(loop, cond, body);
                        }
                        finally
                        {
                            exitLoop();
                        }
                        return pn;
                    }


                case Token.DO:
                    {
                        consumeToken();
                        decompiler.AddToken(Token.DO);
                        decompiler.AddEol(Token.LC);

                        Node loop = enterLoop(statementLabel);
                        try
                        {
                            Node body = statement();
                            decompiler.AddToken(Token.RC);
                            mustMatchToken(Token.WHILE, "msg.no.while.do");
                            decompiler.AddToken(Token.WHILE);
                            Node cond = condition();
                            pn = nf.CreateDoWhile(loop, body, cond);
                        }
                        finally
                        {
                            exitLoop();
                        }
                        // Always auto-insert semicon to follow SpiderMonkey:
                        // It is required by EMAScript but is ignored by the rest of
                        // world, see bug 238945
                        matchToken(Token.SEMI);
                        decompiler.AddEol(Token.SEMI);
                        return pn;
                    }


                case Token.FOR:
                    {
                        consumeToken();
                        bool isForEach = false;
                        decompiler.AddToken(Token.FOR);

                        Node loop = enterLoop(statementLabel);
                        try
                        {

                            Node init; // Node init is also foo in 'foo in Object'
                            Node cond; // Node cond is also object in 'foo in Object'
                            Node incr = null; // to kill warning
                            Node body;

                            // See if this is a for each () instead of just a for ()
                            if (matchToken(Token.NAME))
                            {
                                decompiler.AddName(ts.String);
                                if (ts.String.Equals("each"))
                                {
                                    isForEach = true;
                                }
                                else
                                {
                                    ReportError("msg.no.paren.for");
                                }
                            }

                            mustMatchToken(Token.LP, "msg.no.paren.for");
                            decompiler.AddToken(Token.LP);
                            tt = peekToken();
                            if (tt == Token.SEMI)
                            {
                                init = nf.CreateLeaf(Token.EMPTY);
                            }
                            else
                            {
                                if (tt == Token.VAR)
                                {
                                    // set init to a var list or initial
                                    consumeToken(); // consume the 'var' token
                                    init = variables(true);
                                }
                                else
                                {
                                    init = expr(true);
                                }
                            }

                            if (matchToken(Token.IN))
                            {
                                decompiler.AddToken(Token.IN);
                                // 'cond' is the object over which we're iterating
                                cond = expr(false);
                            }
                            else
                            {
                                // ordinary for loop
                                mustMatchToken(Token.SEMI, "msg.no.semi.for");
                                decompiler.AddToken(Token.SEMI);
                                if (peekToken() == Token.SEMI)
                                {
                                    // no loop condition
                                    cond = nf.CreateLeaf(Token.EMPTY);
                                }
                                else
                                {
                                    cond = expr(false);
                                }

                                mustMatchToken(Token.SEMI, "msg.no.semi.for.cond");
                                decompiler.AddToken(Token.SEMI);
                                if (peekToken() == Token.RP)
                                {
                                    incr = nf.CreateLeaf(Token.EMPTY);
                                }
                                else
                                {
                                    incr = expr(false);
                                }
                            }

                            mustMatchToken(Token.RP, "msg.no.paren.for.ctrl");
                            decompiler.AddToken(Token.RP);
                            decompiler.AddEol(Token.LC);
                            body = statement();
                            decompiler.AddEol(Token.RC);

                            if (incr == null)
                            {
                                // cond could be null if 'in obj' got eaten
                                // by the init node.
                                pn = nf.CreateForIn(loop, init, cond, body, isForEach);
                            }
                            else
                            {
                                pn = nf.CreateFor(loop, init, cond, incr, body);
                            }
                        }
                        finally
                        {
                            exitLoop();
                        }
                        return pn;
                    }


                case Token.TRY:
                    {
                        consumeToken();
                        int lineno = ts.Lineno;

                        Node tryblock;
                        Node catchblocks = null;
                        Node finallyblock = null;

                        decompiler.AddToken(Token.TRY);
                        decompiler.AddEol(Token.LC);
                        tryblock = statement();
                        decompiler.AddEol(Token.RC);

                        catchblocks = nf.CreateLeaf(Token.BLOCK);

                        bool sawDefaultCatch = false;
                        int peek = peekToken();
                        if (peek == Token.CATCH)
                        {
                            while (matchToken(Token.CATCH))
                            {
                                if (sawDefaultCatch)
                                {
                                    ReportError("msg.catch.unreachable");
                                }
                                decompiler.AddToken(Token.CATCH);
                                mustMatchToken(Token.LP, "msg.no.paren.catch");
                                decompiler.AddToken(Token.LP);

                                mustMatchToken(Token.NAME, "msg.bad.catchcond");
                                string varName = ts.String;
                                decompiler.AddName(varName);

                                Node catchCond = null;
                                if (matchToken(Token.IF))
                                {
                                    decompiler.AddToken(Token.IF);
                                    catchCond = expr(false);
                                }
                                else
                                {
                                    sawDefaultCatch = true;
                                }

                                mustMatchToken(Token.RP, "msg.bad.catchcond");
                                decompiler.AddToken(Token.RP);
                                mustMatchToken(Token.LC, "msg.no.brace.catchblock");
                                decompiler.AddEol(Token.LC);

                                nf.addChildToBack(catchblocks, nf.CreateCatch(varName, catchCond, statements(), ts.Lineno));

                                mustMatchToken(Token.RC, "msg.no.brace.after.body");
                                decompiler.AddEol(Token.RC);
                            }
                        }
                        else if (peek != Token.FINALLY)
                        {
                            mustMatchToken(Token.FINALLY, "msg.try.no.catchfinally");
                        }

                        if (matchToken(Token.FINALLY))
                        {
                            decompiler.AddToken(Token.FINALLY);
                            decompiler.AddEol(Token.LC);
                            finallyblock = statement();
                            decompiler.AddEol(Token.RC);
                        }

                        pn = nf.CreateTryCatchFinally(tryblock, catchblocks, finallyblock, lineno);

                        return pn;
                    }


                case Token.THROW:
                    {
                        consumeToken();
                        if (peekTokenOrEOL() == Token.EOL)
                        {
                            // ECMAScript does not allow new lines before throw expression,
                            // see bug 256617
                            ReportError("msg.bad.throw.eol");
                        }

                        int lineno = ts.Lineno;
                        decompiler.AddToken(Token.THROW);
                        pn = nf.CreateThrow(expr(false), lineno);
                        break;
                    }


                case Token.BREAK:
                    {
                        consumeToken();
                        int lineno = ts.Lineno;

                        decompiler.AddToken(Token.BREAK);

                        // matchJumpLabelName only matches if there is one
                        Node breakStatement = matchJumpLabelName();
                        if (breakStatement == null)
                        {
                            if (loopAndSwitchSet == null || loopAndSwitchSet.size() == 0)
                            {
                                ReportError("msg.bad.break");
                                return null;
                            }
                            breakStatement = (Node)loopAndSwitchSet.peek();
                        }
                        pn = nf.CreateBreak(breakStatement, lineno);
                        break;
                    }


                case Token.CONTINUE:
                    {
                        consumeToken();
                        int lineno = ts.Lineno;

                        decompiler.AddToken(Token.CONTINUE);

                        Node loop;
                        // matchJumpLabelName only matches if there is one
                        Node label = matchJumpLabelName();
                        if (label == null)
                        {
                            if (loopSet == null || loopSet.size() == 0)
                            {
                                ReportError("msg.continue.outside");
                                return null;
                            }
                            loop = (Node)loopSet.peek();
                        }
                        else
                        {
                            loop = nf.getLabelLoop(label);
                            if (loop == null)
                            {
                                ReportError("msg.continue.nonloop");
                                return null;
                            }
                        }
                        pn = nf.CreateContinue(loop, lineno);
                        break;
                    }


                case Token.WITH:
                    {
                        consumeToken();

                        decompiler.AddToken(Token.WITH);
                        int lineno = ts.Lineno;
                        mustMatchToken(Token.LP, "msg.no.paren.with");
                        decompiler.AddToken(Token.LP);
                        Node obj = expr(false);
                        mustMatchToken(Token.RP, "msg.no.paren.after.with");
                        decompiler.AddToken(Token.RP);
                        decompiler.AddEol(Token.LC);

                        ++nestingOfWith;
                        Node body;
                        try
                        {
                            body = statement();
                        }
                        finally
                        {
                            --nestingOfWith;
                        }

                        decompiler.AddEol(Token.RC);

                        pn = nf.CreateWith(obj, body, lineno);
                        return pn;
                    }


                case Token.VAR:
                    {
                        consumeToken();
                        pn = variables(false);
                        break;
                    }


                case Token.RETURN:
                    {
                        if (!insideFunction())
                        {
                            ReportError("msg.bad.return");
                        }
                        consumeToken();
                        decompiler.AddToken(Token.RETURN);
                        int lineno = ts.Lineno;

                        Node retExpr;
                        /* This is ugly, but we don't want to require a semicolon. */
                        tt = peekTokenOrEOL();
                        switch (tt)
                        {

                            case Token.SEMI:
                            case Token.RC:
                            case Token.EOF:
                            case Token.EOL:
                            case Token.ERROR:
                                retExpr = null;
                                break;

                            default:
                                retExpr = expr(false);
                                break;

                        }
                        pn = nf.CreateReturn(retExpr, lineno);
                        break;
                    }

                case Token.DEBUGGER:
                    consumeToken();
                    decompiler.AddToken(Token.DEBUGGER);
                    pn = nf.CreateDebugger(ts.Lineno);
                    break;

                case Token.LC:
                    consumeToken();
                    if (statementLabel != null)
                    {
                        decompiler.AddToken(Token.LC);
                    }
                    pn = statements();
                    mustMatchToken(Token.RC, "msg.no.brace.block");
                    if (statementLabel != null)
                    {
                        decompiler.AddEol(Token.RC);
                    }
                    return pn;


                case Token.ERROR:
                // Fall thru, to have a node for error recovery to work on
                case Token.SEMI:
                    consumeToken();
                    pn = nf.CreateLeaf(Token.EMPTY);
                    return pn;


                case Token.FUNCTION:
                    {
                        consumeToken();
                        pn = function(FunctionNode.FUNCTION_EXPRESSION_STATEMENT);
                        return pn;
                    }


                case Token.DEFAULT:
                    consumeToken();
                    mustHaveXML();

                    decompiler.AddToken(Token.DEFAULT);
                    int nsLine = ts.Lineno;

                    if (!(matchToken(Token.NAME) && ts.String.Equals("xml")))
                    {
                        ReportError("msg.bad.namespace");
                    }
                    decompiler.AddName(ts.String);

                    if (!(matchToken(Token.NAME) && ts.String.Equals("namespace")))
                    {
                        ReportError("msg.bad.namespace");
                    }
                    decompiler.AddName(ts.String);

                    if (!matchToken(Token.ASSIGN))
                    {
                        ReportError("msg.bad.namespace");
                    }
                    decompiler.AddToken(Token.ASSIGN);

                    Node e = expr(false);
                    pn = nf.CreateDefaultNamespace(e, nsLine);
                    break;


                case Token.NAME:
                    {
                        int lineno = ts.Lineno;
                        string name = ts.String;
                        setCheckForLabel();
                        pn = expr(false);
                        if (pn.Type != Token.LABEL)
                        {

                            if (compilerEnv.getterAndSetterSupport)
                            {
                                tt = peekToken();
                                if (tt == Token.NAME)
                                {
                                    if (ts.String == "getter" || ts.String == "setter")
                                    {
                                        pn.Type = (ts.String[0] == 'g' ? Token.SETPROP_GETTER
                                            : Token.SETPROP_SETTER);
                                        decompiler.AddName(" " + ts.String); // HACK: Hack (whitespace) for decmpiler
                                        consumeToken();
                                        matchToken(Token.ASSIGN);
                                        decompiler.AddToken(Token.ASSIGN);
                                        matchToken(Token.FUNCTION);
                                        Node fn = function(FunctionNode.FUNCTION_EXPRESSION);
                                        pn.addChildToBack(fn);
                                    }
                                }
                            }
                            pn = nf.CreateExprStatement(pn, lineno);
                        }
                        else
                        {
                            // Parsed the label: push back token should be
                            // colon that primaryExpr left untouched.
                            if (peekToken() != Token.COLON)
                                Context.CodeBug();
                            consumeToken();
                            // depend on decompiling lookahead to guess that that
                            // last name was a label.
                            decompiler.AddName(name);
                            decompiler.AddEol(Token.COLON);

                            if (labelSet == null)
                            {
                                labelSet = Hashtable.Synchronized(new Hashtable());
                            }
                            else if (labelSet.ContainsKey(name))
                            {
                                ReportError("msg.dup.label");
                            }

                            bool firstLabel;
                            if (statementLabel == null)
                            {
                                firstLabel = true;
                                statementLabel = pn;
                            }
                            else
                            {
                                // Discard multiple label nodes and use only
                                // the first: it allows to simplify IRFactory
                                firstLabel = false;
                            }
                            labelSet[name] = statementLabel;
                            try
                            {
                                pn = statementHelper(statementLabel);
                            }
                            finally
                            {
                                labelSet.Remove(name);
                            }
                            if (firstLabel)
                            {
                                pn = nf.CreateLabeledStatement(statementLabel, pn);
                            }
                            return pn;
                        }
                        break;
                    }


                default:
                    {
                        int lineno = ts.Lineno;
                        pn = expr(false);
                        pn = nf.CreateExprStatement(pn, lineno);
                        break;
                    }

            }

            // FINDME

            int ttFlagged = peekFlaggedToken();
            switch (ttFlagged & CLEAR_TI_MASK)
            {

                case Token.SEMI:
                    // Consume ';' as a part of expression
                    consumeToken();
                    break;

                case Token.ERROR:
                case Token.EOF:
                case Token.RC:
                    // Autoinsert ;
                    break;

                default:
                    if ((ttFlagged & TI_AFTER_EOL) == 0)
                    {
                        // Report error if no EOL or autoinsert ; otherwise
                        ReportError("msg.no.semi.stmt");
                    }
                    break;

            }
            decompiler.AddEol(Token.SEMI);

            return pn;
        }

        Node variables(bool inForInit)
        {
            Node pn = nf.CreateVariables(ts.Lineno);
            bool first = true;

            decompiler.AddToken(Token.VAR);

            for (; ; )
            {
                Node name;
                Node init;
                mustMatchToken(Token.NAME, "msg.bad.var");
                string s = ts.String;

                if (!first)
                    decompiler.AddToken(Token.COMMA);
                first = false;

                decompiler.AddName(s);
                currentScriptOrFn.addVar(s);
                name = nf.CreateName(s);

                // omitted check for argument hiding

                if (matchToken(Token.ASSIGN))
                {
                    decompiler.AddToken(Token.ASSIGN);

                    init = assignExpr(inForInit);
                    nf.addChildToBack(name, init);
                }
                nf.addChildToBack(pn, name);
                if (!matchToken(Token.COMMA))
                    break;
            }
            return pn;
        }

        Node expr(bool inForInit)
        {
            Node pn = assignExpr(inForInit);
            while (matchToken(Token.COMMA))
            {
                decompiler.AddToken(Token.COMMA);
                pn = nf.CreateBinary(Token.COMMA, pn, assignExpr(inForInit));
            }
            return pn;
        }

        Node assignExpr(bool inForInit)
        {
            Node pn = condExpr(inForInit);

            int tt = peekToken();
            if (Token.FIRST_ASSIGN <= tt && tt <= Token.LAST_ASSIGN)
            {
                consumeToken();
                decompiler.AddToken(tt);
                pn = nf.CreateAssignment(tt, pn, assignExpr(inForInit));
            }

            return pn;
        }

        Node condExpr(bool inForInit)
        {
            Node ifTrue;
            Node ifFalse;

            Node pn = orExpr(inForInit);

            if (matchToken(Token.HOOK))
            {
                decompiler.AddToken(Token.HOOK);
                ifTrue = assignExpr(false);
                mustMatchToken(Token.COLON, "msg.no.colon.cond");
                decompiler.AddToken(Token.COLON);
                ifFalse = assignExpr(inForInit);
                return nf.CreateCondExpr(pn, ifTrue, ifFalse);
            }

            return pn;
        }

        Node orExpr(bool inForInit)
        {
            Node pn = andExpr(inForInit);
            if (matchToken(Token.OR))
            {
                decompiler.AddToken(Token.OR);
                pn = nf.CreateBinary(Token.OR, pn, orExpr(inForInit));
            }

            return pn;
        }

        Node andExpr(bool inForInit)
        {
            Node pn = bitOrExpr(inForInit);
            if (matchToken(Token.AND))
            {
                decompiler.AddToken(Token.AND);
                pn = nf.CreateBinary(Token.AND, pn, andExpr(inForInit));
            }

            return pn;
        }

        Node bitOrExpr(bool inForInit)
        {
            Node pn = bitXorExpr(inForInit);
            while (matchToken(Token.BITOR))
            {
                decompiler.AddToken(Token.BITOR);
                pn = nf.CreateBinary(Token.BITOR, pn, bitXorExpr(inForInit));
            }
            return pn;
        }

        Node bitXorExpr(bool inForInit)
        {
            Node pn = bitAndExpr(inForInit);
            while (matchToken(Token.BITXOR))
            {
                decompiler.AddToken(Token.BITXOR);
                pn = nf.CreateBinary(Token.BITXOR, pn, bitAndExpr(inForInit));
            }
            return pn;
        }

        Node bitAndExpr(bool inForInit)
        {
            Node pn = eqExpr(inForInit);
            while (matchToken(Token.BITAND))
            {
                decompiler.AddToken(Token.BITAND);
                pn = nf.CreateBinary(Token.BITAND, pn, eqExpr(inForInit));
            }
            return pn;
        }

        Node eqExpr(bool inForInit)
        {
            Node pn = relExpr(inForInit);
            for (; ; )
            {
                int tt = peekToken();
                switch (tt)
                {

                    case Token.EQ:
                    case Token.NE:
                    case Token.SHEQ:
                    case Token.SHNE:
                        consumeToken();
                        int decompilerToken = tt;
                        int parseToken = tt;
                        if (compilerEnv.LanguageVersion == Context.Versions.JS1_2)
                        {
                            // JavaScript 1.2 uses shallow equality for == and != .
                            // In addition, convert === and !== for decompiler into
                            // == and != since the decompiler is supposed to show
                            // canonical source and in 1.2 ===, !== are allowed
                            // only as an alias to ==, !=.
                            switch (tt)
                            {

                                case Token.EQ:
                                    parseToken = Token.SHEQ;
                                    break;

                                case Token.NE:
                                    parseToken = Token.SHNE;
                                    break;

                                case Token.SHEQ:
                                    decompilerToken = Token.EQ;
                                    break;

                                case Token.SHNE:
                                    decompilerToken = Token.NE;
                                    break;
                            }
                        }
                        decompiler.AddToken(decompilerToken);
                        pn = nf.CreateBinary(parseToken, pn, relExpr(inForInit));
                        continue;
                }
                break;
            }
            return pn;
        }

        Node relExpr(bool inForInit)
        {
            Node pn = shiftExpr();
            for (; ; )
            {
                int tt = peekToken();
                switch (tt)
                {

                    case Token.IN:
                        if (inForInit)
                            break;
                        // fall through
                        goto case Token.INSTANCEOF;

                    case Token.INSTANCEOF:
                    case Token.LE:
                    case Token.LT:
                    case Token.GE:
                    case Token.GT:
                        consumeToken();
                        decompiler.AddToken(tt);
                        pn = nf.CreateBinary(tt, pn, shiftExpr());
                        continue;
                }
                break;
            }
            return pn;
        }

        Node shiftExpr()
        {
            Node pn = addExpr();
            for (; ; )
            {
                int tt = peekToken();
                switch (tt)
                {

                    case Token.LSH:
                    case Token.URSH:
                    case Token.RSH:
                        consumeToken();
                        decompiler.AddToken(tt);
                        pn = nf.CreateBinary(tt, pn, addExpr());
                        continue;
                }
                break;
            }
            return pn;
        }

        Node addExpr()
        {
            Node pn = mulExpr();
            for (; ; )
            {
                int tt = peekToken();
                if (tt == Token.ADD || tt == Token.SUB)
                {
                    consumeToken();
                    decompiler.AddToken(tt);
                    // flushNewLines
                    pn = nf.CreateBinary(tt, pn, mulExpr());
                    continue;
                }
                break;
            }

            return pn;
        }

        Node mulExpr()
        {
            Node pn = unaryExpr();
            for (; ; )
            {
                int tt = peekToken();
                switch (tt)
                {

                    case Token.MUL:
                    case Token.DIV:
                    case Token.MOD:
                        consumeToken();
                        decompiler.AddToken(tt);
                        pn = nf.CreateBinary(tt, pn, unaryExpr());
                        continue;
                }
                break;
            }

            return pn;
        }

        Node unaryExpr()
        {
            using (Helpers.StackOverflowVerifier sov = new Helpers.StackOverflowVerifier(4096))
            {
                int tt;

                tt = peekToken();

                switch (tt)
                {

                    case Token.VOID:
                    case Token.NOT:
                    case Token.BITNOT:
                    case Token.TYPEOF:
                        consumeToken();
                        decompiler.AddToken(tt);
                        return nf.CreateUnary(tt, unaryExpr());


                    case Token.ADD:
                        consumeToken();
                        // Convert to special POS token in decompiler and parse tree
                        decompiler.AddToken(Token.POS);
                        return nf.CreateUnary(Token.POS, unaryExpr());


                    case Token.SUB:
                        consumeToken();
                        // Convert to special NEG token in decompiler and parse tree
                        decompiler.AddToken(Token.NEG);
                        return nf.CreateUnary(Token.NEG, unaryExpr());


                    case Token.INC:
                    case Token.DEC:
                        consumeToken();
                        decompiler.AddToken(tt);
                        return nf.CreateIncDec(tt, false, memberExpr(true));


                    case Token.DELPROP:
                        consumeToken();
                        decompiler.AddToken(Token.DELPROP);
                        return nf.CreateUnary(Token.DELPROP, unaryExpr());


                    case Token.ERROR:
                        consumeToken();
                        break;

                    // XML stream encountered in expression.

                    case Token.LT:
                        if (compilerEnv.isXmlAvailable())
                        {
                            consumeToken();
                            Node pn = xmlInitializer();
                            return memberExprTail(true, pn);
                        }
                        // Fall thru to the default handling of RELOP
                        goto default;


                    default:
                        {
                            Node pn = memberExpr(true);

                            // Don't look across a newline boundary for a postfix incop.
                            tt = peekTokenOrEOL();
                            if (tt == Token.INC || tt == Token.DEC)
                            {
                                consumeToken();
                                decompiler.AddToken(tt);
                                return nf.CreateIncDec(tt, true, pn);
                            }
                            return pn;
                        }

                }
                return nf.CreateName("err"); // Only reached on error.  Try to continue.
            }
        }

        Node xmlInitializer()
        {
            int tt = ts.FirstXMLToken;
            if (tt != Token.XML && tt != Token.XMLEND)
            {
                ReportError("msg.syntax");
                return null;
            }

            /* Make a NEW node to append to. */
            Node pnXML = nf.CreateLeaf(Token.NEW);
            decompiler.AddToken(Token.NEW);
            decompiler.AddToken(Token.DOT);

            string xml = ts.String;
            bool fAnonymous = xml.Trim().StartsWith("<>");

            decompiler.AddName(fAnonymous ? "XMLList" : "XML");
            Node pn = nf.CreateName(fAnonymous ? "XMLList" : "XML");
            nf.addChildToBack(pnXML, pn);

            pn = null;
            Node e;
            for (; ; tt = ts.NextXMLToken)
            {
                switch (tt)
                {

                    case Token.XML:
                        xml = ts.String;
                        decompiler.AddString(xml);
                        mustMatchToken(Token.LC, "msg.syntax");
                        decompiler.AddToken(Token.LC);
                        e = (peekToken() == Token.RC) ? nf.CreateString("") : expr(false);
                        mustMatchToken(Token.RC, "msg.syntax");
                        decompiler.AddToken(Token.RC);
                        if (pn == null)
                        {
                            pn = nf.CreateString(xml);
                        }
                        else
                        {
                            pn = nf.CreateBinary(Token.ADD, pn, nf.CreateString(xml));
                        }
                        int nodeType;
                        if (ts.XMLAttribute)
                        {
                            nodeType = Token.ESCXMLATTR;
                        }
                        else
                        {
                            nodeType = Token.ESCXMLTEXT;
                        }
                        e = nf.CreateUnary(nodeType, e);
                        pn = nf.CreateBinary(Token.ADD, pn, e);
                        break;

                    case Token.XMLEND:
                        xml = ts.String;
                        decompiler.AddString(xml);
                        if (pn == null)
                        {
                            pn = nf.CreateString(xml);
                        }
                        else
                        {
                            pn = nf.CreateBinary(Token.ADD, pn, nf.CreateString(xml));
                        }

                        nf.addChildToBack(pnXML, pn);
                        return pnXML;

                    default:
                        ReportError("msg.syntax");
                        return null;

                }
            }
        }

        void argumentList(Node listNode)
        {
            bool matched;
            matched = matchToken(Token.RP);
            if (!matched)
            {
                bool first = true;
                do
                {
                    if (!first)
                        decompiler.AddToken(Token.COMMA);
                    first = false;
                    nf.addChildToBack(listNode, assignExpr(false));
                }
                while (matchToken(Token.COMMA));

                mustMatchToken(Token.RP, "msg.no.paren.arg");
            }
            decompiler.AddToken(Token.RP);
        }

        Node memberExpr(bool allowCallSyntax)
        {
            int tt;

            Node pn;

            /* Check for new expressions. */
            tt = peekToken();
            if (tt == Token.NEW)
            {
                /* Eat the NEW token. */
                consumeToken();
                decompiler.AddToken(Token.NEW);

                /* Make a NEW node to append to. */
                pn = nf.CreateCallOrNew(Token.NEW, memberExpr(false));

                if (matchToken(Token.LP))
                {
                    decompiler.AddToken(Token.LP);
                    /* Add the arguments to pn, if any are supplied. */
                    argumentList(pn);
                }

                // TODO: there's a check in the C source against
                // TODO: "too many constructor arguments" - how many
                // TODO: do we claim to support?				

                /* Experimental syntax:  allow an object literal to follow a new expression,
                * which will mean a kind of anonymous class built with the JavaAdapter.
                * the object literal will be passed as an additional argument to the constructor.
                */
                tt = peekToken();
                if (tt == Token.LC)
                {
                    nf.addChildToBack(pn, primaryExpr());
                }
            }
            else
            {
                pn = primaryExpr();
            }

            return memberExprTail(allowCallSyntax, pn);
        }

        Node memberExprTail(bool allowCallSyntax, Node pn)
        {
            for (; ; )
            {
                int tt = peekToken();
                switch (tt)
                {


                    case Token.DOT:
                    case Token.DOTDOT:
                        {
                            int memberTypeFlags;
                            string s;
                            Match match;

                            consumeToken();
                            decompiler.AddToken(tt);
                            memberTypeFlags = 0;
                            if (tt == Token.DOTDOT)
                            {
                                mustHaveXML();
                                memberTypeFlags = Node.DESCENDANTS_FLAG;
                            }
                            if (!compilerEnv.isXmlAvailable())
                            {
                                mustMatchToken(Token.NAME, "msg.no.name.after.dot");
                                s = ts.String;
                                decompiler.AddName(s);
                                pn = nf.CreatePropertyGet(pn, null, s, memberTypeFlags);
                                break;
                            }


                            tt = nextToken();

                            switch (tt)
                            {

                                // handles: name, ns::name, ns::*, ns::[expr]
                                case Token.NAME:
                                    s = ts.String;
                                    decompiler.AddName(s);
                                    pn = propertyName(pn, s, memberTypeFlags);
                                    break;

                                // handles: *, *::name, *::*, *::[expr]

                                case Token.MUL:
                                    decompiler.AddName("*");
                                    pn = propertyName(pn, "*", memberTypeFlags);
                                    break;

                                // handles: '@attr', '@ns::attr', '@ns::*', '@ns::*',
                                //          '@::attr', '@::*', '@*', '@*::attr', '@*::*'

                                case Token.XMLATTR:
                                    decompiler.AddToken(Token.XMLATTR);
                                    pn = attributeAccess(pn, memberTypeFlags);
                                    break;


                                default:
                                    s = ts.TokenString;
                                    match = SIMPLE_IDENTIFIER_NAME_PATTERN.Match(s);
                                    if (match.Success)
                                    {
                                        decompiler.AddName(s);
                                        pn = propertyName(pn, s, memberTypeFlags);
                                        AddWarning("msg.reserved.keyword", s);
                                    } else
                                        ReportError("msg.no.name.after.dot");
                                    break;

                            }
                        }
                        break;


                    case Token.DOTQUERY:
                        consumeToken();
                        mustHaveXML();
                        decompiler.AddToken(Token.DOTQUERY);
                        pn = nf.CreateDotQuery(pn, expr(false), ts.Lineno);
                        mustMatchToken(Token.RP, "msg.no.paren");
                        decompiler.AddToken(Token.RP);
                        break;


                    case Token.LB:
                        consumeToken();
                        decompiler.AddToken(Token.LB);
                        pn = nf.CreateElementGet(pn, null, expr(false), 0);
                        mustMatchToken(Token.RB, "msg.no.bracket.index");
                        decompiler.AddToken(Token.RB);
                        break;


                    case Token.LP:
                        if (!allowCallSyntax)
                        {

                            goto tailLoop_brk;
                        }
                        consumeToken();
                        decompiler.AddToken(Token.LP);
                        pn = nf.CreateCallOrNew(Token.CALL, pn);
                        /* Add the arguments to pn, if any are supplied. */
                        argumentList(pn);
                        break;


                    default:

                        goto tailLoop_brk;

                }
            }

        tailLoop_brk:
            ;

            return pn;
        }

        /*
        * Xml attribute expression:
        *   '@attr', '@ns::attr', '@ns::*', '@ns::*', '@*', '@*::attr', '@*::*'
        */
        Node attributeAccess(Node pn, int memberTypeFlags)
        {
            memberTypeFlags |= Node.ATTRIBUTE_FLAG;
            int tt = nextToken();

            switch (tt)
            {

                // handles: @name, @ns::name, @ns::*, @ns::[expr]
                case Token.NAME:
                    {
                        string s = ts.String;
                        decompiler.AddName(s);
                        pn = propertyName(pn, s, memberTypeFlags);
                    }
                    break;

                // handles: @*, @*::name, @*::*, @*::[expr]

                case Token.MUL:
                    decompiler.AddName("*");
                    pn = propertyName(pn, "*", memberTypeFlags);
                    break;

                // handles @[expr]

                case Token.LB:
                    decompiler.AddToken(Token.LB);
                    pn = nf.CreateElementGet(pn, null, expr(false), memberTypeFlags);
                    mustMatchToken(Token.RB, "msg.no.bracket.index");
                    decompiler.AddToken(Token.RB);
                    break;


                default:
                    ReportError("msg.no.name.after.xmlAttr");
                    pn = nf.CreatePropertyGet(pn, null, "?", memberTypeFlags);
                    break;

            }

            return pn;
        }

        /// <summary> Check if :: follows name in which case it becomes qualified name</summary>
        Node propertyName(Node pn, string name, int memberTypeFlags)
        {
            string ns = null;
            if (matchToken(Token.COLONCOLON))
            {
                decompiler.AddToken(Token.COLONCOLON);
                ns = name;

                int tt = nextToken();
                switch (tt)
                {

                    // handles name::name
                    case Token.NAME:
                        name = ts.String;
                        decompiler.AddName(name);
                        break;

                    // handles name::*

                    case Token.MUL:
                        decompiler.AddName("*");
                        name = "*";
                        break;

                    // handles name::[expr]

                    case Token.LB:
                        decompiler.AddToken(Token.LB);
                        pn = nf.CreateElementGet(pn, ns, expr(false), memberTypeFlags);
                        mustMatchToken(Token.RB, "msg.no.bracket.index");
                        decompiler.AddToken(Token.RB);
                        return pn;


                    default:
                        ReportError("msg.no.name.after.coloncolon");
                        name = "?";
                        break;

                }
            }

            pn = nf.CreatePropertyGet(pn, ns, name, memberTypeFlags);
            return pn;
        }

        int currentStackIndex = 0;


        Node primaryExpr()
        {
            try
            {
                if (currentStackIndex++ > ScriptRuntime.MAXSTACKSIZE)
                {
                    currentStackIndex = 0;
                    throw Context.ReportRuntimeError(
                        ScriptRuntime.GetMessage("mag.too.deep.parser.recursion"), sourceURI, ts.Lineno, null, 0);
                }

                Node pn;

                int ttFlagged = nextFlaggedToken();
                int tt = ttFlagged & CLEAR_TI_MASK;

                switch (tt)
                {


                    case Token.FUNCTION:
                        return function(FunctionNode.FUNCTION_EXPRESSION);


                    case Token.LB:
                        {
                            ObjArray elems = new ObjArray();
                            int skipCount = 0;
                            decompiler.AddToken(Token.LB);
                            bool after_lb_or_comma = true;
                            for (; ; )
                            {
                                tt = peekToken();

                                if (tt == Token.COMMA)
                                {
                                    consumeToken();
                                    decompiler.AddToken(Token.COMMA);
                                    if (!after_lb_or_comma)
                                    {
                                        after_lb_or_comma = true;
                                    }
                                    else
                                    {
                                        elems.add((object)null);
                                        ++skipCount;
                                    }
                                }
                                else if (tt == Token.RB)
                                {
                                    consumeToken();
                                    decompiler.AddToken(Token.RB);
                                    break;
                                }
                                else
                                {
                                    if (!after_lb_or_comma)
                                    {
                                        ReportError("msg.no.bracket.arg");
                                    }
                                    elems.add(assignExpr(false));
                                    after_lb_or_comma = false;
                                }
                            }
                            return nf.CreateArrayLiteral(elems, skipCount);
                        }


                    case Token.LC:
                        {
                            ObjArray elems = new ObjArray();
                            decompiler.AddToken(Token.LC);
                            if (!matchToken(Token.RC))
                            {

                                bool first = true;
                                do
                                {
                                    object property;

                                    if (!first)
                                        decompiler.AddToken(Token.COMMA);
                                    else
                                        first = false;

                                    tt = peekToken();
                                    switch (tt)
                                    {

                                        case Token.NAME:
                                        case Token.STRING:
                                            consumeToken();
                                            if (compilerEnv.getterAndSetterSupport)
                                            {
                                                if (tt == Token.NAME)
                                                    if (CheckForGetOrSet(elems) || CheckForGetterOrSetter(elems))
                                                        goto next_prop;
                                            }


                                            // map NAMEs to STRINGs in object literal context
                                            // but tell the decompiler the proper type
                                            string s = ts.String;
                                            if (tt == Token.NAME)
                                            {
                                                decompiler.AddName(s);
                                            }
                                            else
                                            {
                                                decompiler.AddString(s);
                                            }
                                            property = ScriptRuntime.getIndexObject(s);

                                            break;


                                        case Token.NUMBER:
                                            consumeToken();
                                            double n = ts.Number;
                                            decompiler.AddNumber(n);
                                            property = ScriptRuntime.getIndexObject(n);
                                            break;


                                        case Token.RC:
                                            // trailing comma is OK.

                                            goto commaloop_brk;

                                        default:
                                            ReportError("msg.bad.prop");

                                            goto commaloop_brk;

                                    }
                                    mustMatchToken(Token.COLON, "msg.no.colon.prop");

                                    // OBJLIT is used as ':' in object literal for
                                    // decompilation to solve spacing ambiguity.
                                    decompiler.AddToken(Token.OBJECTLIT);
                                    elems.add(property);
                                    elems.add(assignExpr(false));

                                next_prop:
                                    ;
                                }
                                while (matchToken(Token.COMMA));

                            commaloop_brk:
                                ;


                                mustMatchToken(Token.RC, "msg.no.brace.prop");
                            }
                            decompiler.AddToken(Token.RC);
                            return nf.CreateObjectLiteral(elems);
                        }


                    case Token.LP:

                        /* Brendan's IR-jsparse.c makes a new node tagged with
                        * TOK_LP here... I'm not sure I understand why.  Isn't
                        * the grouping already implicit in the structure of the
                        * parse tree?  also TOK_LP is already overloaded (I
                        * think) in the C IR as 'function call.'  */
                        decompiler.AddToken(Token.LP);
                        pn = expr(false);
                        decompiler.AddToken(Token.RP);
                        mustMatchToken(Token.RP, "msg.no.paren");
                        return pn;


                    case Token.XMLATTR:
                        mustHaveXML();
                        decompiler.AddToken(Token.XMLATTR);
                        pn = attributeAccess(null, 0);
                        return pn;


                    case Token.NAME:
                        {
                            string name = ts.String;
                            if ((ttFlagged & TI_CHECK_LABEL) != 0)
                            {
                                if (peekToken() == Token.COLON)
                                {
                                    // Do not consume colon, it is used as unwind indicator
                                    // to return to statementHelper.
                                    // TODO: Better way?
                                    return nf.CreateLabel(ts.Lineno);
                                }
                            }

                            decompiler.AddName(name);
                            if (compilerEnv.isXmlAvailable())
                            {
                                pn = propertyName(null, name, 0);
                            }
                            else
                            {
                                pn = nf.CreateName(name);
                            }
                            return pn;
                        }


                    case Token.NUMBER:
                        {
                            double n = ts.Number;
                            decompiler.AddNumber(n);
                            return nf.CreateNumber(n);
                        }


                    case Token.STRING:
                        {
                            string s = ts.String;
                            decompiler.AddString(s);
                            return nf.CreateString(s);
                        }


                    case Token.DIV:
                    case Token.ASSIGN_DIV:
                        {
                            // Got / or /= which should be treated as regexp in fact
                            ts.readRegExp(tt);
                            string flags = ts.regExpFlags;
                            ts.regExpFlags = null;
                            string re = ts.String;
                            decompiler.AddRegexp(re, flags);
                            int index = currentScriptOrFn.addRegexp(re, flags);
                            return nf.CreateRegExp(index);
                        }


                    case Token.NULL:
                    case Token.THIS:
                    case Token.FALSE:
                    case Token.TRUE:
                        decompiler.AddToken(tt);
                        return nf.CreateLeaf(tt);


                    case Token.RESERVED:
                        ReportError("msg.reserved.id");
                        break;


                    case Token.ERROR:
                        /* the scanner or one of its subroutines reported the error. */
                        break;


                    case Token.EOF:
                        ReportError("msg.unexpected.eof");
                        break;


                    default:
                        ReportError("msg.syntax");
                        break;

                }
                return null; // should never reach here
            }
            finally
            {
                currentStackIndex--;
            }
        }


        /// <summary>
        /// Support for non-ecma "get"/"set" spidermonkey extension.
        /// </summary>
        /// <example>
        ///		get NAME () SCOPE
        ///		set NAME () SCOPE
        /// </example>
        bool CheckForGetOrSet(ObjArray elems)
        {
            int tt;

            string type = ts.String;
            if (type != "get" && type != "set")
            {
                return false;
            }
            tt = peekToken();
            if (tt != Token.NAME)
                return false;
            consumeToken();

            string name = ts.String;

            decompiler.AddName(name);

            Node func = function(FunctionNode.FUNCTION_EXPRESSION);
            object property = ScriptRuntime.getIndexObject(name);

            elems.add((type[0] == 'g') ? (object)new Node.GetterPropertyLiteral(property)
                : (object)new Node.SetterPropertyLiteral(property));
            elems.add(func);

            return true;
        }

        /// <summary>
        /// Support for non-ecma "get"/"set" spidermonkey extension.
        /// </summary>
        /// <example>
        ///		NAME getter: FUNCTION () SCOPE
        ///		NAME setter: FUNCTION () SCOPE
        /// </example>
        bool CheckForGetterOrSetter(ObjArray elems)
        {
            int tt;

            string name = ts.String;
            consumeToken();

            tt = peekToken();
            if (tt != Token.NAME)
                return false;
            string type = ts.String;
            if (type != "getter" && type != "setter")
            {
                return false;
            }
            consumeToken();

            matchToken(Token.COLON);
            matchToken(Token.FUNCTION);

            Node func = function(FunctionNode.FUNCTION_EXPRESSION);
            object property = ScriptRuntime.getIndexObject(name);

            elems.add((type[0] == 'g') ? (object)new Node.GetterPropertyLiteral(property)
                : (object)new Node.SetterPropertyLiteral(property));
            elems.add(func);

            return true;
        }

    }
}