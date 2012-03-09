//------------------------------------------------------------------------------
// <license file="NodeTransformer.cs">
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

    /// <summary> This class transforms a tree to a lower-level representation for codegen.
    /// 
    /// </summary>	
    public class NodeTransformer
    {

        public NodeTransformer ()
        {
        }

        public void transform (ScriptOrFnNode tree)
        {
            transformCompilationUnit (tree);
            for (int i = 0; i != tree.FunctionCount; ++i) {
                FunctionNode fn = tree.getFunctionNode (i);
                transform (fn);
            }
        }

        private void transformCompilationUnit (ScriptOrFnNode tree)
        {
            loops = new ObjArray ();
            loopEnds = new ObjArray ();
            // to save against upchecks if no finally blocks are used.
            hasFinally = false;
            
            try {
                transformCompilationUnit_r (tree, tree);
            } catch (Helpers.StackOverflowVerifierException) {
                throw Context.ReportRuntimeError (
                    ScriptRuntime.GetMessage ("mag.too.deep.parser.recursion"));
            }
        }

        private void transformCompilationUnit_r (ScriptOrFnNode tree, Node parent)
        {
            Node node = null;

            using (Helpers.StackOverflowVerifier sov = new Helpers.StackOverflowVerifier (1024)) {
                for (; ; ) {
                    Node previous = null;
                    if (node == null) {
                        node = parent.FirstChild;
                    }
                    else {
                        previous = node;
                        node = node.Next;
                    }
                    if (node == null) {
                        break;
                    }

                    int type = node.Type;

                    switch (type) {


                        case Token.LABEL:
                        case Token.SWITCH:
                        case Token.LOOP:
                            loops.push (node);
                            loopEnds.push (((Node.Jump)node).target);
                            break;


                        case Token.WITH: {
                                loops.push (node);
                                Node leave = node.Next;
                                if (leave.Type != Token.LEAVEWITH) {
                                    Context.CodeBug ();
                                }
                                loopEnds.push (leave);
                                break;
                            }


                        case Token.TRY: {
                                Node.Jump jump = (Node.Jump)node;
                                Node finallytarget = jump.Finally;
                                if (finallytarget != null) {
                                    hasFinally = true;
                                    loops.push (node);
                                    loopEnds.push (finallytarget);
                                }
                                break;
                            }


                        case Token.TARGET:
                        case Token.LEAVEWITH:
                            if (!loopEnds.Empty && loopEnds.peek () == node) {
                                loopEnds.pop ();
                                loops.pop ();
                            }
                            break;


                        case Token.RETURN: {
                                /* If we didn't support try/finally, it wouldn't be
                                * necessary to put LEAVEWITH nodes here... but as
                                * we do need a series of JSR FINALLY nodes before
                                * each RETURN, we need to ensure that each finally
                                * block gets the correct scope... which could mean
                                * that some LEAVEWITH nodes are necessary.
                                */
                                if (!hasFinally)
                                    break; // skip the whole mess.
                                Node unwindBlock = null;
                                for (int i = loops.size () - 1; i >= 0; i--) {
                                    Node n = (Node)loops.Get (i);
                                    int elemtype = n.Type;
                                    if (elemtype == Token.TRY || elemtype == Token.WITH) {
                                        Node unwind;
                                        if (elemtype == Token.TRY) {
                                            Node.Jump jsrnode = new Node.Jump (Token.JSR);
                                            Node jsrtarget = ((Node.Jump)n).Finally;
                                            jsrnode.target = jsrtarget;
                                            unwind = jsrnode;
                                        }
                                        else {
                                            unwind = new Node (Token.LEAVEWITH);
                                        }
                                        if (unwindBlock == null) {
                                            unwindBlock = new Node (Token.BLOCK, node.Lineno);
                                        }
                                        unwindBlock.addChildToBack (unwind);
                                    }
                                }
                                if (unwindBlock != null) {
                                    Node returnNode = node;
                                    Node returnExpr = returnNode.FirstChild;
                                    node = replaceCurrent (parent, previous, node, unwindBlock);
                                    if (returnExpr == null) {
                                        unwindBlock.addChildToBack (returnNode);
                                    }
                                    else {
                                        Node store = new Node (Token.EXPR_RESULT, returnExpr);
                                        unwindBlock.addChildToFront (store);
                                        returnNode = new Node (Token.RETURN_RESULT);
                                        unwindBlock.addChildToBack (returnNode);
                                        // transform return expression
                                        transformCompilationUnit_r (tree, store);
                                    }
                                    // skip transformCompilationUnit_r to avoid infinite loop

                                    goto siblingLoop;
                                }
                                break;
                            }


                        case Token.BREAK:
                        case Token.CONTINUE: {
                                Node.Jump jump = (Node.Jump)node;
                                Node.Jump jumpStatement = jump.JumpStatement;
                                if (jumpStatement == null)
                                    Context.CodeBug ();

                                for (int i = loops.size (); ; ) {
                                    if (i == 0) {
                                        // Parser/IRFactory ensure that break/continue
                                        // always has a jump statement associated with it
                                        // which should be found
                                        throw Context.CodeBug ();
                                    }
                                    --i;
                                    Node n = (Node)loops.Get (i);
                                    if (n == jumpStatement) {
                                        break;
                                    }

                                    int elemtype = n.Type;
                                    if (elemtype == Token.WITH) {
                                        Node leave = new Node (Token.LEAVEWITH);
                                        previous = addBeforeCurrent (parent, previous, node, leave);
                                    }
                                    else if (elemtype == Token.TRY) {
                                        Node.Jump tryNode = (Node.Jump)n;
                                        Node.Jump jsrFinally = new Node.Jump (Token.JSR);
                                        jsrFinally.target = tryNode.Finally;
                                        previous = addBeforeCurrent (parent, previous, node, jsrFinally);
                                    }
                                }

                                if (type == Token.BREAK) {
                                    jump.target = jumpStatement.target;
                                }
                                else {
                                    jump.target = jumpStatement.Continue;
                                }
                                jump.Type = Token.GOTO;

                                break;
                            }


                        case Token.CALL:
                            visitCall (node, tree);
                            break;


                        case Token.NEW:
                            visitNew (node, tree);
                            break;


                        case Token.VAR: {
                                Node result = new Node (Token.BLOCK);
                                for (Node cursor = node.FirstChild; cursor != null; ) {
                                    // Move cursor to next before createAssignment get chance
                                    // to change n.next
                                    Node n = cursor;
                                    if (n.Type != Token.NAME)
                                        Context.CodeBug ();
                                    cursor = cursor.Next;
                                    if (!n.hasChildren ())
                                        continue;
                                    Node init = n.FirstChild;
                                    n.removeChild (init);
                                    n.Type = Token.BINDNAME;
                                    n = new Node (Token.SETNAME, n, init);
                                    Node pop = new Node (Token.EXPR_VOID, n, node.Lineno);
                                    result.addChildToBack (pop);
                                }
                                node = replaceCurrent (parent, previous, node, result);
                                break;
                            }


                        case Token.NAME:
                        case Token.SETNAME:
                        case Token.DELPROP: {
                                // Turn name to var for faster access if possible
                                if (tree.Type != Token.FUNCTION || ((FunctionNode)tree).RequiresActivation) {
                                    break;
                                }
                                Node nameSource;
                                if (type == Token.NAME) {
                                    nameSource = node;
                                }
                                else {
                                    nameSource = node.FirstChild;
                                    if (nameSource.Type != Token.BINDNAME) {
                                        if (type == Token.DELPROP) {
                                            break;
                                        }
                                        throw Context.CodeBug ();
                                    }
                                }
                                string name = nameSource.String;
                                if (tree.hasParamOrVar (name)) {
                                    if (type == Token.NAME) {
                                        node.Type = Token.GETVAR;
                                    }
                                    else if (type == Token.SETNAME) {
                                        node.Type = Token.SETVAR;
                                        nameSource.Type = Token.STRING;
                                    }
                                    else if (type == Token.DELPROP) {
                                        // Local variables are by definition permanent
                                        Node n = new Node (Token.FALSE);
                                        node = replaceCurrent (parent, previous, node, n);
                                    }
                                    else {
                                        throw Context.CodeBug ();
                                    }
                                }
                                break;
                            }
                    }

                    transformCompilationUnit_r (tree, node);

                siblingLoop:
                    ;
                }
            }
        }

        protected internal virtual void visitNew (Node node, ScriptOrFnNode tree)
        {
        }

        protected internal virtual void visitCall (Node node, ScriptOrFnNode tree)
        {
        }

        private static Node addBeforeCurrent (Node parent, Node previous, Node current, Node toAdd)
        {
            if (previous == null) {
                if (!(current == parent.FirstChild))
                    Context.CodeBug ();
                parent.addChildToFront (toAdd);
            }
            else {
                if (!(current == previous.Next))
                    Context.CodeBug ();
                parent.addChildAfter (toAdd, previous);
            }
            return toAdd;
        }

        private static Node replaceCurrent (Node parent, Node previous, Node current, Node replacement)
        {
            if (previous == null) {
                if (!(current == parent.FirstChild))
                    Context.CodeBug ();
                parent.replaceChild (current, replacement);
            }
            else if (previous.next == current) {
                // Check cachedPrev.next == current is necessary due to possible
                // tree mutations
                parent.replaceChildAfter (previous, replacement);
            }
            else {
                parent.replaceChild (current, replacement);
            }
            return replacement;
        }

        private ObjArray loops;
        private ObjArray loopEnds;
        private bool hasFinally;
    }
}