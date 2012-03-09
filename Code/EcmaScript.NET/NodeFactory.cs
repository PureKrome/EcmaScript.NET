//------------------------------------------------------------------------------
// <license file="IRFactory.cs">
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

    /// <summary> This class allows the creation of nodes, and follows the Factory pattern.
    /// 
    /// </summary>	
    sealed class NodeFactory
    {
        internal NodeFactory (Parser parser)
        {
            this.parser = parser;
        }

        internal ScriptOrFnNode CreateScript ()
        {
            return new ScriptOrFnNode (Token.SCRIPT);
        }

        /// <summary> Script (for associating file/url names with toplevel scripts.)</summary>
        internal void initScript (ScriptOrFnNode scriptNode, Node body)
        {
            Node children = body.FirstChild;
            if (children != null) {
                scriptNode.addChildrenToBack (children);
            }
        }

        /// <summary> Leaf</summary>
        internal Node CreateLeaf (int nodeType)
        {
            return new Node (nodeType);
        }

        internal Node CreateLeaf (int nodeType, int nodeOp)
        {
            return new Node (nodeType, nodeOp);
        }

        /// <summary> Statement leaf nodes.</summary>

        internal Node CreateSwitch (Node expr, int lineno)
        {
            //
            // The switch will be rewritten from:
            //
            // switch (expr) {
            //   case test1: statements1;
            //   ...
            //   default: statementsDefault;
            //   ...
            //   case testN: statementsN;
            // }
            //
            // to:
            //
            // {
            //     switch (expr) {
            //       case test1: goto label1;
            //       ...
            //       case testN: goto labelN;
            //     }
            //     goto labelDefault;
            //   label1:
            //     statements1;
            //   ...
            //   labelDefault:
            //     statementsDefault;
            //   ...
            //   labelN:
            //     statementsN;
            //   breakLabel:
            // }
            //
            // where inside switch each "break;" without label will be replaced
            // by "goto breakLabel".
            //
            // If the original switch does not have the default label, then
            // the transformed code would contain after the switch instead of
            //     goto labelDefault;
            // the following goto:
            //     goto breakLabel;
            //

            Node.Jump switchNode = new Node.Jump (Token.SWITCH, expr, lineno);
            Node block = new Node (Token.BLOCK, switchNode);
            return block;
        }

        /// <summary> If caseExpression argument is null it indicate default label.</summary>
        internal void addSwitchCase (Node switchBlock, Node caseExpression, Node statements)
        {
            if (switchBlock.Type != Token.BLOCK)
                throw Context.CodeBug ();
            Node.Jump switchNode = (Node.Jump)switchBlock.FirstChild;
            if (switchNode.Type != Token.SWITCH)
                throw Context.CodeBug ();

            Node gotoTarget = Node.newTarget ();
            if (caseExpression != null) {
                Node.Jump caseNode = new Node.Jump (Token.CASE, caseExpression);
                caseNode.target = gotoTarget;
                switchNode.addChildToBack (caseNode);
            }
            else {
                switchNode.Default = gotoTarget;
            }
            switchBlock.addChildToBack (gotoTarget);
            switchBlock.addChildToBack (statements);
        }

        internal void closeSwitch (Node switchBlock)
        {
            if (switchBlock.Type != Token.BLOCK)
                throw Context.CodeBug ();
            Node.Jump switchNode = (Node.Jump)switchBlock.FirstChild;
            if (switchNode.Type != Token.SWITCH)
                throw Context.CodeBug ();

            Node switchBreakTarget = Node.newTarget ();
            // switchNode.target is only used by NodeTransformer
            // to detect switch end
            switchNode.target = switchBreakTarget;

            Node defaultTarget = switchNode.Default;
            if (defaultTarget == null) {
                defaultTarget = switchBreakTarget;
            }

            switchBlock.addChildAfter (makeJump (Token.GOTO, defaultTarget), switchNode);
            switchBlock.addChildToBack (switchBreakTarget);
        }

        internal Node CreateVariables (int lineno)
        {
            return new Node (Token.VAR, lineno);
        }

        internal Node CreateExprStatement (Node expr, int lineno)
        {
            int type;
            if (parser.insideFunction ()) {
                type = Token.EXPR_VOID;
            }
            else {
                type = Token.EXPR_RESULT;
            }
            return new Node (type, expr, lineno);
        }

        internal Node CreateExprStatementNoReturn (Node expr, int lineno)
        {
            return new Node (Token.EXPR_VOID, expr, lineno);
        }

        internal Node CreateDefaultNamespace (Node expr, int lineno)
        {
            // default xml namespace requires activation
            setRequiresActivation ();
            Node n = CreateUnary (Token.DEFAULTNAMESPACE, expr);
            Node result = CreateExprStatement (n, lineno);
            return result;
        }

        /// <summary> Name</summary>
        internal Node CreateName (string name)
        {
            checkActivationName (name, Token.NAME);
            return Node.newString (Token.NAME, name);
        }

        /// <summary> String (for literals)</summary>
        internal Node CreateString (string str)
        {
            return Node.newString (str);
        }

        /// <summary> Number (for literals)</summary>
        internal Node CreateNumber (double number)
        {
            return Node.newNumber (number);
        }

        /// <summary> Catch clause of try/catch/finally</summary>
        /// <param name="varName">the name of the variable to bind to the exception
        /// </param>
        /// <param name="catchCond">the condition under which to catch the exception.
        /// May be null if no condition is given.
        /// </param>
        /// <param name="stmts">the statements in the catch clause
        /// </param>
        /// <param name="lineno">the starting line number of the catch clause
        /// </param>
        internal Node CreateCatch (string varName, Node catchCond, Node stmts, int lineno)
        {
            if (catchCond == null) {
                catchCond = new Node (Token.EMPTY);
            }
            return new Node (Token.CATCH, CreateName (varName), catchCond, stmts, lineno);
        }

        /// <summary> Throw</summary>
        internal Node CreateThrow (Node expr, int lineno)
        {
            return new Node (Token.THROW, expr, lineno);
        }

        /// <summary> Return</summary>
        internal Node CreateReturn (Node expr, int lineno)
        {
            return expr == null ? new Node (Token.RETURN, lineno) : new Node (Token.RETURN, expr, lineno);
        }

        /// <summary> Debugger</summary>
        internal Node CreateDebugger(int lineno)
        {
            return new Node(Token.DEBUGGER, lineno);
        }

        /// <summary> Label</summary>
        internal Node CreateLabel (int lineno)
        {
            return new Node.Jump (Token.LABEL, lineno);
        }

        internal Node getLabelLoop (Node label)
        {
            return ((Node.Jump)label).Loop;
        }

        /// <summary> Label</summary>
        internal Node CreateLabeledStatement (Node labelArg, Node statement)
        {
            Node.Jump label = (Node.Jump)labelArg;

            // Make a target and put it _after_ the statement
            // node.  And in the LABEL node, so breaks get the
            // right target.

            Node breakTarget = Node.newTarget ();
            Node block = new Node (Token.BLOCK, label, statement, breakTarget);
            label.target = breakTarget;

            return block;
        }

        /// <summary> Break (possibly labeled)</summary>
        internal Node CreateBreak (Node breakStatement, int lineno)
        {
            Node.Jump n = new Node.Jump (Token.BREAK, lineno);
            Node.Jump jumpStatement;
            int t = breakStatement.Type;
            if (t == Token.LOOP || t == Token.LABEL) {
                jumpStatement = (Node.Jump)breakStatement;
            }
            else if (t == Token.BLOCK && breakStatement.FirstChild.Type == Token.SWITCH) {
                jumpStatement = (Node.Jump)breakStatement.FirstChild;
            }
            else {
                throw Context.CodeBug ();
            }
            n.JumpStatement = jumpStatement;
            return n;
        }

        /// <summary> Continue (possibly labeled)</summary>
        internal Node CreateContinue (Node loop, int lineno)
        {
            if (loop.Type != Token.LOOP)
                Context.CodeBug ();
            Node.Jump n = new Node.Jump (Token.CONTINUE, lineno);
            n.JumpStatement = (Node.Jump)loop;
            return n;
        }

        /// <summary> Statement block
        /// Creates the empty statement block
        /// Must make subsequent calls to add statements to the node
        /// </summary>
        internal Node CreateBlock (int lineno)
        {
            return new Node (Token.BLOCK, lineno);
        }

        internal FunctionNode CreateFunction (string name)
        {
            return new FunctionNode (name);
        }

        internal Node initFunction (FunctionNode fnNode, int functionIndex, Node statements, int functionType)
        {
            fnNode.itsFunctionType = functionType;
            fnNode.addChildToBack (statements);

            int functionCount = fnNode.FunctionCount;
            if (functionCount != 0) {
                // Functions containing other functions require activation objects
                fnNode.itsNeedsActivation = true;
                for (int i = 0; i != functionCount; ++i) {
                    FunctionNode fn = fnNode.getFunctionNode (i);
                    // nested function expression statements overrides var
                    if (fn.FunctionType == FunctionNode.FUNCTION_EXPRESSION_STATEMENT) {
                        string name = fn.FunctionName;
                        if (name != null && name.Length != 0) {
                            fnNode.removeParamOrVar (name);
                        }
                    }
                }
            }

            if (functionType == FunctionNode.FUNCTION_EXPRESSION) {
                string name = fnNode.FunctionName;
                if (name != null && name.Length != 0 && !fnNode.hasParamOrVar (name)) {
                    // A function expression needs to have its name as a
                    // variable (if it isn't already allocated as a variable).
                    // See ECMA Ch. 13.  We add code to the beginning of the
                    // function to initialize a local variable of the
                    // function's name to the function value.
                    fnNode.addVar (name);
                    Node setFn = new Node (Token.EXPR_VOID, new Node (Token.SETNAME, Node.newString (Token.BINDNAME, name), new Node (Token.THISFN)));
                    statements.addChildrenToFront (setFn);
                }
            }

            // Add return to end if needed.
            Node lastStmt = statements.LastChild;
            if (lastStmt == null || lastStmt.Type != Token.RETURN) {
                statements.addChildToBack (new Node (Token.RETURN));
            }

            Node result = Node.newString (Token.FUNCTION, fnNode.FunctionName);
            result.putIntProp (Node.FUNCTION_PROP, functionIndex);
            return result;
        }

        /// <summary> Add a child to the back of the given node.  This function
        /// breaks the Factory abstraction, but it removes a requirement
        /// from implementors of Node.
        /// </summary>
        internal void addChildToBack (Node parent, Node child)
        {
            parent.addChildToBack (child);
        }

        /// <summary> Create loop node. The parser will later call
        /// CreateWhile|CreateDoWhile|CreateFor|CreateForIn
        /// to finish loop generation.
        /// </summary>
        internal Node CreateLoopNode (Node loopLabel, int lineno)
        {
            Node.Jump result = new Node.Jump (Token.LOOP, lineno);
            if (loopLabel != null) {
                ((Node.Jump)loopLabel).Loop = result;
            }
            return result;
        }

        /// <summary> While</summary>
        internal Node CreateWhile (Node loop, Node cond, Node body)
        {
            return CreateLoop ((Node.Jump)loop, LOOP_WHILE, body, cond, null, null);
        }

        /// <summary> DoWhile</summary>
        internal Node CreateDoWhile (Node loop, Node body, Node cond)
        {
            return CreateLoop ((Node.Jump)loop, LOOP_DO_WHILE, body, cond, null, null);
        }

        /// <summary> For</summary>
        internal Node CreateFor (Node loop, Node init, Node test, Node incr, Node body)
        {
            return CreateLoop ((Node.Jump)loop, LOOP_FOR, body, test, init, incr);
        }

        private Node CreateLoop (Node.Jump loop, int loopType, Node body, Node cond, Node init, Node incr)
        {
            Node bodyTarget = Node.newTarget ();
            Node condTarget = Node.newTarget ();
            if (loopType == LOOP_FOR && cond.Type == Token.EMPTY) {
                cond = new Node (Token.TRUE);
            }
            Node.Jump IFEQ = new Node.Jump (Token.IFEQ, cond);
            IFEQ.target = bodyTarget;
            Node breakTarget = Node.newTarget ();

            loop.addChildToBack (bodyTarget);
            loop.addChildrenToBack (body);
            if (loopType == LOOP_WHILE || loopType == LOOP_FOR) {
                // propagate lineno to condition
                loop.addChildrenToBack (new Node (Token.EMPTY, loop.Lineno));
            }
            loop.addChildToBack (condTarget);
            loop.addChildToBack (IFEQ);
            loop.addChildToBack (breakTarget);

            loop.target = breakTarget;
            Node continueTarget = condTarget;

            if (loopType == LOOP_WHILE || loopType == LOOP_FOR) {
                // Just add a GOTO to the condition in the do..while
                loop.addChildToFront (makeJump (Token.GOTO, condTarget));

                if (loopType == LOOP_FOR) {
                    if (init.Type != Token.EMPTY) {
                        if (init.Type != Token.VAR) {
                            init = new Node (Token.EXPR_VOID, init);
                        }
                        loop.addChildToFront (init);
                    }
                    Node incrTarget = Node.newTarget ();
                    loop.addChildAfter (incrTarget, body);
                    if (incr.Type != Token.EMPTY) {
                        incr = new Node (Token.EXPR_VOID, incr);
                        loop.addChildAfter (incr, incrTarget);
                    }
                    continueTarget = incrTarget;
                }
            }

            loop.Continue = continueTarget;

            return loop;
        }

        /// <summary> For .. In
        /// 
        /// </summary>
        internal Node CreateForIn (Node loop, Node lhs, Node obj, Node body, bool isForEach)
        {
            int type = lhs.Type;

            Node lvalue;
            if (type == Token.VAR) {
                /*
                * check that there was only one variable given.
                * we can't do this in the parser, because then the
                * parser would have to know something about the
                * 'init' node of the for-in loop.
                */
                Node lastChild = lhs.LastChild;
                if (lhs.FirstChild != lastChild) {
                    parser.ReportError ("msg.mult.index");
                }
                lvalue = Node.newString (Token.NAME, lastChild.String);
            }
            else {
                lvalue = makeReference (lhs);
                if (lvalue == null) {
                    parser.ReportError ("msg.bad.for.in.lhs");
                    return obj;
                }
            }

            Node localBlock = new Node (Token.LOCAL_BLOCK);

            int initType = (isForEach) ? Token.ENUM_INIT_VALUES : Token.ENUM_INIT_KEYS;
            Node init = new Node (initType, obj);
            init.putProp (Node.LOCAL_BLOCK_PROP, localBlock);
            Node cond = new Node (Token.ENUM_NEXT);
            cond.putProp (Node.LOCAL_BLOCK_PROP, localBlock);
            Node id = new Node (Token.ENUM_ID);
            id.putProp (Node.LOCAL_BLOCK_PROP, localBlock);

            Node newBody = new Node (Token.BLOCK);
            Node assign = simpleAssignment (lvalue, id);
            newBody.addChildToBack (new Node (Token.EXPR_VOID, assign));
            newBody.addChildToBack (body);

            loop = CreateWhile (loop, cond, newBody);
            loop.addChildToFront (init);
            if (type == Token.VAR)
                loop.addChildToFront (lhs);
            localBlock.addChildToBack (loop);

            return localBlock;
        }

        /// <summary> Try/Catch/Finally
        /// 
        /// The IRFactory tries to express as much as possible in the tree;
        /// the responsibilities remaining for Codegen are to add the Java
        /// handlers: (Either (but not both) of TARGET and FINALLY might not
        /// be defined)
        /// - a catch handler for javascript exceptions that unwraps the
        /// exception onto the stack and GOTOes to the catch target
        /// - a finally handler
        /// ... and a goto to GOTO around these handlers.
        /// </summary>
        internal Node CreateTryCatchFinally (Node tryBlock, Node catchBlocks, Node finallyBlock, int lineno)
        {
            bool hasFinally = (finallyBlock != null) && (finallyBlock.Type != Token.BLOCK || finallyBlock.hasChildren ());

            // short circuit
            if (tryBlock.Type == Token.BLOCK && !tryBlock.hasChildren () && !hasFinally) {
                return tryBlock;
            }

            bool hasCatch = catchBlocks.hasChildren ();

            // short circuit
            if (!hasFinally && !hasCatch) {
                // bc finally might be an empty block...
                return tryBlock;
            }


            Node handlerBlock = new Node (Token.LOCAL_BLOCK);
            Node.Jump pn = new Node.Jump (Token.TRY, tryBlock, lineno);
            pn.putProp (Node.LOCAL_BLOCK_PROP, handlerBlock);

            if (hasCatch) {
                // jump around catch code
                Node endCatch = Node.newTarget ();
                pn.addChildToBack (makeJump (Token.GOTO, endCatch));

                // make a TARGET for the catch that the tcf node knows about
                Node catchTarget = Node.newTarget ();
                pn.target = catchTarget;
                // mark it
                pn.addChildToBack (catchTarget);

                //
                //  Given
                //
                //   try {
                //       tryBlock;
                //   } catch (e if condition1) {
                //       something1;
                //   ...
                //
                //   } catch (e if conditionN) {
                //       somethingN;
                //   } catch (e) {
                //       somethingDefault;
                //   }
                //
                //  rewrite as
                //
                //   try {
                //       tryBlock;
                //       goto after_catch:
                //   } catch (x) {
                //       with (newCatchScope(e, x)) {
                //           if (condition1) {
                //               something1;
                //               goto after_catch;
                //           }
                //       }
                //   ...
                //       with (newCatchScope(e, x)) {
                //           if (conditionN) {
                //               somethingN;
                //               goto after_catch;
                //           }
                //       }
                //       with (newCatchScope(e, x)) {
                //           somethingDefault;
                //           goto after_catch;
                //       }
                //   }
                // after_catch:
                //
                // If there is no default catch, then the last with block
                // arround  "somethingDefault;" is replaced by "rethrow;"

                // It is assumed that catch handler generation will store
                // exeception object in handlerBlock register

                // Block with local for exception scope objects
                Node catchScopeBlock = new Node (Token.LOCAL_BLOCK);

                // expects catchblocks children to be (cond block) pairs.
                Node cb = catchBlocks.FirstChild;
                bool hasDefault = false;
                int scopeIndex = 0;
                while (cb != null) {
                    int catchLineNo = cb.Lineno;

                    Node name = cb.FirstChild;
                    Node cond = name.Next;
                    Node catchStatement = cond.Next;
                    cb.removeChild (name);
                    cb.removeChild (cond);
                    cb.removeChild (catchStatement);

                    // Add goto to the catch statement to jump out of catch
                    // but prefix it with LEAVEWITH since try..catch produces
                    // "with"code in order to limit the scope of the exception
                    // object.
                    catchStatement.addChildToBack (new Node (Token.LEAVEWITH));
                    catchStatement.addChildToBack (makeJump (Token.GOTO, endCatch));

                    // Create condition "if" when present
                    Node condStmt;
                    if (cond.Type == Token.EMPTY) {
                        condStmt = catchStatement;
                        hasDefault = true;
                    }
                    else {
                        condStmt = CreateIf (cond, catchStatement, null, catchLineNo);
                    }

                    // Generate code to Create the scope object and store
                    // it in catchScopeBlock register
                    Node catchScope = new Node (Token.CATCH_SCOPE, name, CreateUseLocal (handlerBlock));
                    catchScope.putProp (Node.LOCAL_BLOCK_PROP, catchScopeBlock);
                    catchScope.putIntProp (Node.CATCH_SCOPE_PROP, scopeIndex);
                    catchScopeBlock.addChildToBack (catchScope);

                    // Add with statement based on catch scope object
                    catchScopeBlock.addChildToBack (CreateWith (CreateUseLocal (catchScopeBlock), condStmt, catchLineNo));

                    // move to next cb
                    cb = cb.Next;
                    ++scopeIndex;
                }
                pn.addChildToBack (catchScopeBlock);
                if (!hasDefault) {
                    // Generate code to rethrow if no catch clause was executed
                    Node rethrow = new Node (Token.RETHROW);
                    rethrow.putProp (Node.LOCAL_BLOCK_PROP, handlerBlock);
                    pn.addChildToBack (rethrow);
                }

                pn.addChildToBack (endCatch);
            }

            if (hasFinally) {
                Node finallyTarget = Node.newTarget ();
                pn.Finally = finallyTarget;

                // add jsr finally to the try block
                pn.addChildToBack (makeJump (Token.JSR, finallyTarget));

                // jump around finally code
                Node finallyEnd = Node.newTarget ();
                pn.addChildToBack (makeJump (Token.GOTO, finallyEnd));

                pn.addChildToBack (finallyTarget);
                Node fBlock = new Node (Token.FINALLY, finallyBlock);
                fBlock.putProp (Node.LOCAL_BLOCK_PROP, handlerBlock);
                pn.addChildToBack (fBlock);

                pn.addChildToBack (finallyEnd);
            }
            handlerBlock.addChildToBack (pn);
            return handlerBlock;
        }

        /// <summary> Throw, Return, Label, Break and Continue are defined in ASTFactory.</summary>

        /// <summary> With</summary>
        internal Node CreateWith (Node obj, Node body, int lineno)
        {
            setRequiresActivation ();
            Node result = new Node (Token.BLOCK, lineno);
            result.addChildToBack (new Node (Token.ENTERWITH, obj));
            Node bodyNode = new Node (Token.WITH, body, lineno);
            result.addChildrenToBack (bodyNode);
            result.addChildToBack (new Node (Token.LEAVEWITH));
            return result;
        }

        /// <summary> DOTQUERY</summary>
        public Node CreateDotQuery (Node obj, Node body, int lineno)
        {
            setRequiresActivation ();
            Node result = new Node (Token.DOTQUERY, obj, body, lineno);
            return result;
        }

        internal Node CreateArrayLiteral (ObjArray elems, int skipCount)
        {
            int length = elems.size ();
            int [] skipIndexes = null;
            if (skipCount != 0) {
                skipIndexes = new int [skipCount];
            }
            Node array = new Node (Token.ARRAYLIT);
            for (int i = 0, j = 0; i != length; ++i) {
                Node elem = (Node)elems.Get (i);
                if (elem != null) {
                    array.addChildToBack (elem);
                }
                else {
                    skipIndexes [j] = i;
                    ++j;
                }
            }
            if (skipCount != 0) {
                array.putProp (Node.SKIP_INDEXES_PROP, skipIndexes);
            }
            return array;
        }

        /// <summary> Object Literals
        /// <BR> CreateObjectLiteral rewrites its argument as object
        /// creation plus object property entries, so later compiler
        /// stages don't need to know about object literals.
        /// </summary>
        internal Node CreateObjectLiteral (ObjArray elems)
        {
            int size = elems.size () / 2;
            Node obj = new Node (Token.OBJECTLIT);
            object [] properties;
            if (size == 0) {
                properties = ScriptRuntime.EmptyArgs;
            }
            else {
                properties = new object [size];
                for (int i = 0; i != size; ++i) {
                    properties [i] = elems.Get (2 * i);
                    Node value = (Node)elems.Get (2 * i + 1);
                    obj.addChildToBack (value);
                }
            }
            obj.putProp (Node.OBJECT_IDS_PROP, properties);
            return obj;
        }

        /// <summary> Regular expressions</summary>
        internal Node CreateRegExp (int regexpIndex)
        {
            Node n = new Node (Token.REGEXP);
            n.putIntProp (Node.REGEXP_PROP, regexpIndex);
            return n;
        }

        /// <summary> If statement</summary>
        internal Node CreateIf (Node cond, Node ifTrue, Node ifFalse, int lineno)
        {
            int condStatus = isAlwaysDefinedBoolean (cond);
            if (condStatus == ALWAYS_TRUE_BOOLEAN) {
                return ifTrue;
            }
            else if (condStatus == ALWAYS_FALSE_BOOLEAN) {
                if (ifFalse != null) {
                    return ifFalse;
                }
                return new Node (Token.BLOCK, lineno);
            }

            Node result = new Node (Token.BLOCK, lineno);
            Node ifNotTarget = Node.newTarget ();
            Node.Jump IFNE = new Node.Jump (Token.IFNE, cond);
            IFNE.target = ifNotTarget;

            result.addChildToBack (IFNE);
            result.addChildrenToBack (ifTrue);

            if (ifFalse != null) {
                Node endTarget = Node.newTarget ();
                result.addChildToBack (makeJump (Token.GOTO, endTarget));
                result.addChildToBack (ifNotTarget);
                result.addChildrenToBack (ifFalse);
                result.addChildToBack (endTarget);
            }
            else {
                result.addChildToBack (ifNotTarget);
            }

            return result;
        }

        internal Node CreateCondExpr (Node cond, Node ifTrue, Node ifFalse)
        {
            int condStatus = isAlwaysDefinedBoolean (cond);
            if (condStatus == ALWAYS_TRUE_BOOLEAN) {
                return ifTrue;
            }
            else if (condStatus == ALWAYS_FALSE_BOOLEAN) {
                return ifFalse;
            }
            return new Node (Token.HOOK, cond, ifTrue, ifFalse);
        }

        /// <summary> Unary</summary>
        internal Node CreateUnary (int nodeType, Node child)
        {
            int childType = child.Type;
            switch (nodeType) {

                case Token.DELPROP: {
                        Node n;
                        if (childType == Token.NAME) {
                            // Transform Delete(Name "a")
                            //  to Delete(Bind("a"), String("a"))
                            child.Type = Token.BINDNAME;
                            Node left = child;
                            Node right = Node.newString (child.String);
                            n = new Node (nodeType, left, right);
                        }
                        else if (childType == Token.GETPROP || childType == Token.GETELEM) {
                            Node left = child.FirstChild;
                            Node right = child.LastChild;
                            child.removeChild (left);
                            child.removeChild (right);
                            n = new Node (nodeType, left, right);
                        }
                        else if (childType == Token.GET_REF) {
                            Node rf = child.FirstChild;
                            child.removeChild (rf);
                            n = new Node (Token.DEL_REF, rf);
                        }
                        else {
                            n = new Node (Token.TRUE);
                        }
                        return n;
                    }

                case Token.TYPEOF:
                    if (childType == Token.NAME) {
                        child.Type = Token.TYPEOFNAME;
                        return child;
                    }
                    break;

                case Token.BITNOT:
                    if (childType == Token.NUMBER) {
                        int value = ScriptConvert.ToInt32 (child.Double);
                        child.Double = ~value;
                        return child;
                    }
                    break;

                case Token.NEG:
                    if (childType == Token.NUMBER) {
                        child.Double = -child.Double;
                        return child;
                    }
                    break;

                case Token.NOT: {
                        int status = isAlwaysDefinedBoolean (child);
                        if (status != 0) {
                            int type;
                            if (status == ALWAYS_TRUE_BOOLEAN) {
                                type = Token.FALSE;
                            }
                            else {
                                type = Token.TRUE;
                            }
                            if (childType == Token.TRUE || childType == Token.FALSE) {
                                child.Type = type;
                                return child;
                            }
                            return new Node (type);
                        }
                        break;
                    }
            }
            return new Node (nodeType, child);
        }

        internal Node CreateCallOrNew (int nodeType, Node child)
        {
            int type = Node.NON_SPECIALCALL;
            if (child.Type == Token.NAME) {
                string name = child.String;
                if (name.Equals ("eval")) {
                    type = Node.SPECIALCALL_EVAL;
                }
                else if (name.Equals ("With")) {
                    type = Node.SPECIALCALL_WITH;
                }
            }
            else if (child.Type == Token.GETPROP) {
                string name = child.LastChild.String;
                if (name.Equals ("eval")) {
                    type = Node.SPECIALCALL_EVAL;
                }
            }
            Node node = new Node (nodeType, child);
            if (type != Node.NON_SPECIALCALL) {
                // Calls to these functions require activation objects.
                setRequiresActivation ();
                node.putIntProp (Node.SPECIALCALL_PROP, type);
            }
            return node;
        }

        internal Node CreateIncDec (int nodeType, bool post, Node child)
        {
            child = makeReference (child);
            if (child == null) {
                string msg;
                if (nodeType == Token.DEC) {
                    msg = "msg.bad.decr";
                }
                else {
                    msg = "msg.bad.incr";
                }
                parser.ReportError (msg);
                return null;
            }

            int childType = child.Type;

            switch (childType) {

                case Token.NAME:
                case Token.GETPROP:
                case Token.GETELEM:
                case Token.GET_REF: {
                        Node n = new Node (nodeType, child);
                        int incrDecrMask = 0;
                        if (nodeType == Token.DEC) {
                            incrDecrMask |= Node.DECR_FLAG;
                        }
                        if (post) {
                            incrDecrMask |= Node.POST_FLAG;
                        }
                        n.putIntProp (Node.INCRDECR_PROP, incrDecrMask);
                        return n;
                    }
            }
            throw Context.CodeBug ();
        }

        internal Node CreatePropertyGet (Node target, string ns, string name, int memberTypeFlags)
        {
            if (ns == null && memberTypeFlags == 0) {
                if (target == null) {
                    return CreateName (name);
                }
                checkActivationName (name, Token.GETPROP);
                if (ScriptRuntime.isSpecialProperty (name)) {
                    Node rf = new Node (Token.REF_SPECIAL, target);
                    rf.putProp (Node.NAME_PROP, name);
                    return new Node (Token.GET_REF, rf);
                }
                return new Node (Token.GETPROP, target, CreateString (name));
            }
            Node elem = CreateString (name);
            memberTypeFlags |= Node.PROPERTY_FLAG;
            return CreateMemberRefGet (target, ns, elem, memberTypeFlags);
        }

        internal Node CreateElementGet (Node target, string ns, Node elem, int memberTypeFlags)
        {
            // OPT: could optimize to CreatePropertyGet
            // iff elem is string that can not be number
            if (ns == null && memberTypeFlags == 0) {
                // stand-alone [aaa] as primary expression is array literal
                // declaration and should not come here!
                if (target == null)
                    throw Context.CodeBug ();
                return new Node (Token.GETELEM, target, elem);
            }
            return CreateMemberRefGet (target, ns, elem, memberTypeFlags);
        }

        private Node CreateMemberRefGet (Node target, string ns, Node elem, int memberTypeFlags)
        {
            Node nsNode = null;
            if (ns != null) {
                // See 11.1.2 in ECMA 357
                if (ns.Equals ("*")) {
                    nsNode = new Node (Token.NULL);
                }
                else {
                    nsNode = CreateName (ns);
                }
            }
            Node rf;
            if (target == null) {
                if (ns == null) {
                    rf = new Node (Token.REF_NAME, elem);
                }
                else {
                    rf = new Node (Token.REF_NS_NAME, nsNode, elem);
                }
            }
            else {
                if (ns == null) {
                    rf = new Node (Token.REF_MEMBER, target, elem);
                }
                else {
                    rf = new Node (Token.REF_NS_MEMBER, target, nsNode, elem);
                }
            }
            if (memberTypeFlags != 0) {
                rf.putIntProp (Node.MEMBER_TYPE_PROP, memberTypeFlags);
            }
            return new Node (Token.GET_REF, rf);
        }

        /// <summary> Binary</summary>
        internal Node CreateBinary (int nodeType, Node left, Node right)
        {
            switch (nodeType) {


                case Token.ADD:
                    // numerical addition and string concatenation
                    if (left.Type == Token.STRING) {
                        string s2;
                        if (right.Type == Token.STRING) {
                            s2 = right.String;
                        }
                        else if (right.Type == Token.NUMBER) {
                            s2 = ScriptConvert.ToString (right.Double, 10);
                        }
                        else {
                            break;
                        }
                        string s1 = left.String;
                        left.String = string.Concat (s1, s2);
                        return left;
                    }
                    else if (left.Type == Token.NUMBER) {
                        if (right.Type == Token.NUMBER) {
                            left.Double = left.Double + right.Double;
                            return left;
                        }
                        else if (right.Type == Token.STRING) {
                            string s1, s2;
                            s1 = ScriptConvert.ToString (left.Double, 10);
                            s2 = right.String;
                            right.String = string.Concat (s1, s2);
                            return right;
                        }
                    }
                    // can't do anything if we don't know  both types - since
                    // 0 + object is supposed to call toString on the object and do
                    // string concantenation rather than addition
                    break;


                case Token.SUB:
                    // numerical subtraction
                    if (left.Type == Token.NUMBER) {
                        double ld = left.Double;
                        if (right.Type == Token.NUMBER) {
                            //both numbers
                            left.Double = ld - right.Double;
                            return left;
                        }
                        else if (ld == 0.0) {
                            // first 0: 0-x -> -x
                            return new Node (Token.NEG, right);
                        }
                    }
                    else if (right.Type == Token.NUMBER) {
                        if (right.Double == 0.0) {
                            //second 0: x - 0 -> +x
                            // can not make simply x because x - 0 must be number
                            return new Node (Token.POS, left);
                        }
                    }
                    break;


                case Token.MUL:
                    // numerical multiplication
                    if (left.Type == Token.NUMBER) {
                        double ld = left.Double;
                        if (right.Type == Token.NUMBER) {
                            //both numbers
                            left.Double = ld * right.Double;
                            return left;
                        }
                        else if (ld == 1.0) {
                            // first 1: 1 *  x -> +x
                            return new Node (Token.POS, right);
                        }
                    }
                    else if (right.Type == Token.NUMBER) {
                        if (right.Double == 1.0) {
                            //second 1: x * 1 -> +x
                            // can not make simply x because x - 0 must be number
                            return new Node (Token.POS, left);
                        }
                    }
                    // can't do x*0: Infinity * 0 gives NaN, not 0
                    break;


                case Token.DIV:
                    // number division
                    if (right.Type == Token.NUMBER) {
                        double rd = right.Double;
                        if (left.Type == Token.NUMBER) {
                            // both constants -- just divide, trust Java to handle x/0
                            left.Double = left.Double / rd;
                            return left;
                        }
                        else if (rd == 1.0) {
                            // second 1: x/1 -> +x
                            // not simply x to force number convertion
                            return new Node (Token.POS, left);
                        }
                    }
                    break;


                case Token.AND: {
                        int leftStatus = isAlwaysDefinedBoolean (left);
                        if (leftStatus == ALWAYS_FALSE_BOOLEAN) {
                            // if the first one is false, replace with FALSE
                            return new Node (Token.FALSE);
                        }
                        else if (leftStatus == ALWAYS_TRUE_BOOLEAN) {
                            // if first is true, set to second
                            return right;
                        }
                        int rightStatus = isAlwaysDefinedBoolean (right);
                        if (rightStatus == ALWAYS_FALSE_BOOLEAN) {
                            // if the second one is false, replace with FALSE
                            if (!hasSideEffects (left)) {
                                return new Node (Token.FALSE);
                            }
                        }
                        else if (rightStatus == ALWAYS_TRUE_BOOLEAN) {
                            // if second is true, set to first
                            return left;
                        }
                        break;
                    }


                case Token.OR: {
                        int leftStatus = isAlwaysDefinedBoolean (left);
                        if (leftStatus == ALWAYS_TRUE_BOOLEAN) {
                            // if the first one is true, replace with TRUE
                            return new Node (Token.TRUE);
                        }
                        else if (leftStatus == ALWAYS_FALSE_BOOLEAN) {
                            // if first is false, set to second
                            return right;
                        }
                        int rightStatus = isAlwaysDefinedBoolean (right);
                        if (rightStatus == ALWAYS_TRUE_BOOLEAN) {
                            // if the second one is true, replace with TRUE
                            if (!hasSideEffects (left)) {
                                return new Node (Token.TRUE);
                            }
                        }
                        else if (rightStatus == ALWAYS_FALSE_BOOLEAN) {
                            // if second is false, set to first
                            return left;
                        }
                        break;
                    }
            }

            return new Node (nodeType, left, right);
        }

        private Node simpleAssignment (Node left, Node right)
        {
            int nodeType = left.Type;
            switch (nodeType) {

                case Token.NAME:
                    left.Type = Token.BINDNAME;
                    return new Node (Token.SETNAME, left, right);


                case Token.GETPROP:
                case Token.GETELEM: {
                        Node obj = left.FirstChild;
                        Node id = left.LastChild;
                        int type;
                        if (nodeType == Token.GETPROP) {
                            type = Token.SETPROP;
                        }
                        else {
                            type = Token.SETELEM;
                        }
                        return new Node (type, obj, id, right);
                    }

                case Token.GET_REF: {
                        Node rf = left.FirstChild;
                        checkMutableReference (rf);
                        return new Node (Token.SET_REF, rf, right);
                    }
            }

            throw Context.CodeBug ();
        }

        private void checkMutableReference (Node n)
        {
            int memberTypeFlags = n.getIntProp (Node.MEMBER_TYPE_PROP, 0);
            if ((memberTypeFlags & Node.DESCENDANTS_FLAG) != 0) {
                parser.ReportError ("msg.bad.assign.left");
            }
        }

        internal Node CreateAssignment (int assignType, Node left, Node right)
        {
            left = makeReference (left);
            if (left == null) {
                parser.ReportError ("msg.bad.assign.left");
                return right;
            }

            int assignOp;
            switch (assignType) {

                case Token.ASSIGN:
                    return simpleAssignment (left, right);

                case Token.ASSIGN_BITOR:
                    assignOp = Token.BITOR;
                    break;

                case Token.ASSIGN_BITXOR:
                    assignOp = Token.BITXOR;
                    break;

                case Token.ASSIGN_BITAND:
                    assignOp = Token.BITAND;
                    break;

                case Token.ASSIGN_LSH:
                    assignOp = Token.LSH;
                    break;

                case Token.ASSIGN_RSH:
                    assignOp = Token.RSH;
                    break;

                case Token.ASSIGN_URSH:
                    assignOp = Token.URSH;
                    break;

                case Token.ASSIGN_ADD:
                    assignOp = Token.ADD;
                    break;

                case Token.ASSIGN_SUB:
                    assignOp = Token.SUB;
                    break;

                case Token.ASSIGN_MUL:
                    assignOp = Token.MUL;
                    break;

                case Token.ASSIGN_DIV:
                    assignOp = Token.DIV;
                    break;

                case Token.ASSIGN_MOD:
                    assignOp = Token.MOD;
                    break;

                default:
                    throw Context.CodeBug ();

            }

            int nodeType = left.Type;
            switch (nodeType) {

                case Token.NAME: {
                        string s = left.String;

                        Node opLeft = Node.newString (Token.NAME, s);
                        Node op = new Node (assignOp, opLeft, right);
                        Node lvalueLeft = Node.newString (Token.BINDNAME, s);
                        return new Node (Token.SETNAME, lvalueLeft, op);
                    }

                case Token.GETPROP:
                case Token.GETELEM: {
                        Node obj = left.FirstChild;
                        Node id = left.LastChild;

                        int type = nodeType == Token.GETPROP ? Token.SETPROP_OP : Token.SETELEM_OP;

                        Node opLeft = new Node (Token.USE_STACK);
                        Node op = new Node (assignOp, opLeft, right);
                        return new Node (type, obj, id, op);
                    }

                case Token.GET_REF: {
                        Node rf = left.FirstChild;
                        checkMutableReference (rf);
                        Node opLeft = new Node (Token.USE_STACK);
                        Node op = new Node (assignOp, opLeft, right);
                        return new Node (Token.SET_REF_OP, rf, op);
                    }
            }

            throw Context.CodeBug ();
        }

        internal Node CreateUseLocal (Node localBlock)
        {
            if (Token.LOCAL_BLOCK != localBlock.Type)
                throw Context.CodeBug ();
            Node result = new Node (Token.LOCAL_LOAD);
            result.putProp (Node.LOCAL_BLOCK_PROP, localBlock);
            return result;
        }

        private Node.Jump makeJump (int type, Node target)
        {
            Node.Jump n = new Node.Jump (type);
            n.target = target;
            return n;
        }

        private Node makeReference (Node node)
        {
            int type = node.Type;
            switch (type) {

                case Token.NAME:
                case Token.GETPROP:
                case Token.GETELEM:
                case Token.GET_REF:
                    return node;

                case Token.CALL:
                    node.Type = Token.REF_CALL;
                    return new Node (Token.GET_REF, node);
            }
            // Signal caller to report error
            return null;
        }

        // Check if Node always mean true or false in boolean context
        private static int isAlwaysDefinedBoolean (Node node)
        {
            switch (node.Type) {

                case Token.FALSE:
                case Token.NULL:
                    return ALWAYS_FALSE_BOOLEAN;

                case Token.TRUE:
                    return ALWAYS_TRUE_BOOLEAN;

                case Token.NUMBER: {
                        double num = node.Double;
                        if (!double.IsNaN (num) && num != 0.0) {
                            return ALWAYS_TRUE_BOOLEAN;
                        }
                        else {
                            return ALWAYS_FALSE_BOOLEAN;
                        }
                    }
            }
            return 0;
        }

        private static bool hasSideEffects (Node exprTree)
        {
            switch (exprTree.Type) {

                case Token.INC:
                case Token.DEC:
                case Token.SETPROP:
                case Token.SETELEM:
                case Token.SETNAME:
                case Token.CALL:
                case Token.NEW:
                    return true;

                default:
                    Node child = exprTree.FirstChild;
                    while (child != null) {
                        if (hasSideEffects (child))
                            return true;
                        child = child.Next;
                    }
                    break;

            }
            return false;
        }

        private void checkActivationName (string name, int token)
        {
            if (parser.insideFunction ()) {
                bool activation = false;
                if ("arguments".Equals (name) || (parser.compilerEnv.activationNames != null && parser.compilerEnv.activationNames.ContainsKey (name))) {
                    activation = true;
                }
                else if ("length".Equals (name)) {
                    if (token == Token.GETPROP && parser.compilerEnv.LanguageVersion == Context.Versions.JS1_2) {
                        // Use of "length" in 1.2 requires an activation object.
                        activation = true;
                    }
                }
                if (activation) {
                    setRequiresActivation ();
                }
            }
        }

        private void setRequiresActivation ()
        {
            if (parser.insideFunction ()) {
                ((FunctionNode)parser.currentScriptOrFn).itsNeedsActivation = true;
            }
        }

        private Parser parser;

        private const int LOOP_DO_WHILE = 0;
        private const int LOOP_WHILE = 1;
        private const int LOOP_FOR = 2;

        private const int ALWAYS_TRUE_BOOLEAN = 1;
        private const int ALWAYS_FALSE_BOOLEAN = -1;
    }
}