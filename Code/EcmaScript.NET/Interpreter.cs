//------------------------------------------------------------------------------
// <license file="Interpreter.cs">
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

using EcmaScript.NET.Debugging;
using EcmaScript.NET.Collections;

namespace EcmaScript.NET
{

    public class Interpreter
    {

        // Additional interpreter-specific codes

        const int Icode_DUP = -1;
        const int Icode_DUP2 = -2;
        const int Icode_SWAP = -3;
        const int Icode_POP = -4;
        const int Icode_POP_RESULT = -5;
        const int Icode_IFEQ_POP = -6;
        const int Icode_VAR_INC_DEC = -7;
        const int Icode_NAME_INC_DEC = -8;
        const int Icode_PROP_INC_DEC = -9;
        const int Icode_ELEM_INC_DEC = -10;
        const int Icode_REF_INC_DEC = -11;
        const int Icode_SCOPE_LOAD = -12;
        const int Icode_SCOPE_SAVE = -13;
        const int Icode_TYPEOFNAME = -14;
        const int Icode_NAME_AND_THIS = -15;
        const int Icode_PROP_AND_THIS = -16;
        const int Icode_ELEM_AND_THIS = -17;
        const int Icode_VALUE_AND_THIS = -18;
        const int Icode_CLOSURE_EXPR = -19;
        const int Icode_CLOSURE_STMT = -20;
        const int Icode_CALLSPECIAL = -21;
        const int Icode_RETUNDEF = -22;
        const int Icode_GOSUB = -23;
        const int Icode_STARTSUB = -24;
        const int Icode_RETSUB = -25;
        const int Icode_LINE = -26;
        const int Icode_SHORTNUMBER = -27;
        const int Icode_INTNUMBER = -28;
        const int Icode_LITERAL_NEW = -29;
        const int Icode_LITERAL_SET = -30;
        const int Icode_SPARE_ARRAYLIT = -31;
        const int Icode_REG_IND_C0 = -32;
        const int Icode_REG_IND_C1 = -33;
        const int Icode_REG_IND_C2 = -34;
        const int Icode_REG_IND_C3 = -35;
        const int Icode_REG_IND_C4 = -36;
        const int Icode_REG_IND_C5 = -37;
        const int Icode_REG_IND1 = -38;
        const int Icode_REG_IND2 = -39;
        const int Icode_REG_IND4 = -40;
        const int Icode_REG_STR_C0 = -41;
        const int Icode_REG_STR_C1 = -42;
        const int Icode_REG_STR_C2 = -43;
        const int Icode_REG_STR_C3 = -44;
        const int Icode_REG_STR1 = -45;
        const int Icode_REG_STR2 = -46;
        const int Icode_REG_STR4 = -47;
        const int Icode_GETVAR1 = -48;
        const int Icode_SETVAR1 = -49;
        const int Icode_UNDEF = -50;
        const int Icode_ZERO = -51;
        const int Icode_ONE = -52;
        const int Icode_ENTERDQ = -53;
        const int Icode_LEAVEDQ = -54;
        const int Icode_TAIL_CALL = -55;
        const int Icode_LOCAL_CLEAR = -56;
        const int Icode_DEBUGGER = -57;

        // Last icode
        const int MIN_ICODE = -57;


        // data for parsing

        CompilerEnvirons compilerEnv;

        bool itsInFunctionFlag;

        InterpreterData itsData;
        ScriptOrFnNode scriptOrFn;
        int itsICodeTop;
        int itsStackDepth;
        int itsLineNumber;
        int itsDoubleTableTop;
        ObjToIntMap itsStrings = new ObjToIntMap (20);
        int itsLocalTop;

        const int MIN_LABEL_TABLE_SIZE = 32;
        const int MIN_FIXUP_TABLE_SIZE = 40;
        int [] itsLabelTable;
        int itsLabelTableTop;
        // itsFixupTable[i] = (label_index << 32) | fixup_site
        long [] itsFixupTable;
        int itsFixupTableTop;
        ObjArray itsLiteralIds = new ObjArray ();

        int itsExceptionTableTop;
        const int EXCEPTION_TRY_START_SLOT = 0;
        const int EXCEPTION_TRY_END_SLOT = 1;
        const int EXCEPTION_HANDLER_SLOT = 2;
        const int EXCEPTION_TYPE_SLOT = 3;
        const int EXCEPTION_LOCAL_SLOT = 4;
        const int EXCEPTION_SCOPE_SLOT = 5;
        // SLOT_SIZE: space for try start/end, handler, start, handler type,
        //            exception local and scope local
        const int EXCEPTION_SLOT_SIZE = 6;

        // ECF_ or Expression Context Flags constants: for now only TAIL is available
        const int ECF_TAIL = 1 << 0;


        internal class CallFrame : System.ICloneable
        {

            internal CallFrame parentFrame;
            // amount of stack frames before this one on the interpretation stack
            internal int frameIndex;
            // If true indicates read-only frame that is a part of continuation
            internal bool frozen;

            internal InterpretedFunction fnOrScript;
            internal InterpreterData idata;

            // Stack structure
            // stack[0 <= i < localShift]: arguments and local variables
            // stack[localShift <= i <= emptyStackTop]: used for local temporaries
            // stack[emptyStackTop < i < stack.length]: stack data
            // sDbl[i]: if stack[i] is UniqueTag.DoubleMark, sDbl[i] holds the number value

            internal object [] stack;
            internal double [] sDbl;

            internal CallFrame varSource; // defaults to this unless continuation frame
            internal int localShift;
            internal int emptyStackTop;

            internal DebugFrame debuggerFrame;
            internal bool useActivation;

            internal IScriptable thisObj;
            internal IScriptable [] scriptRegExps;

            // The values that change during interpretation

            internal object result;
            internal double resultDbl;
            internal int pc;
            internal int pcPrevBranch;
            internal int pcSourceLineStart;
            internal IScriptable scope;

            internal int savedStackTop;
            internal int savedCallOp;

            internal virtual CallFrame cloneFrozen ()
            {
                if (!frozen)
                    Context.CodeBug ();

                CallFrame copy = (CallFrame)Clone ();

                // clone stack but keep varSource to point to values
                // from this frame to share variables.

                // TODO: STACK
                copy.stack = new object [stack.Length];
                stack.CopyTo (copy.stack, 0);
                copy.sDbl = new double [sDbl.Length];
                sDbl.CopyTo (copy.sDbl, 0);

                copy.frozen = false;
                return copy;
            }

            virtual public object Clone ()
            {
                return base.MemberwiseClone ();
            }
        }


        sealed class ContinuationJump
        {

            internal CallFrame capturedFrame;
            internal CallFrame branchFrame;
            internal object result;
            internal double resultDbl;

            internal ContinuationJump (Continuation c, CallFrame current)
            {
                this.capturedFrame = (CallFrame)c.Implementation;
                if (this.capturedFrame == null || current == null) {
                    // Continuation and current execution does not share
                    // any frames if there is nothing to capture or
                    // if there is no currently executed frames
                    this.branchFrame = null;
                }
                else {
                    // Search for branch frame where parent frame chains starting
                    // from captured and current meet.
                    CallFrame chain1 = this.capturedFrame;
                    CallFrame chain2 = current;

                    // First work parents of chain1 or chain2 until the same
                    // frame depth.
                    int diff = chain1.frameIndex - chain2.frameIndex;
                    if (diff != 0) {
                        if (diff < 0) {
                            // swap to make sure that
                            // chain1.frameIndex > chain2.frameIndex and diff > 0
                            chain1 = current;
                            chain2 = this.capturedFrame;
                            diff = -diff;
                        }
                        do {
                            chain1 = chain1.parentFrame;
                        }
                        while (--diff != 0);
                        if (chain1.frameIndex != chain2.frameIndex)
                            Context.CodeBug ();
                    }

                    // Now walk parents in parallel until a shared frame is found
                    // or until the root is reached.
                    while (chain1 != chain2 && chain1 != null) {
                        chain1 = chain1.parentFrame;
                        chain2 = chain2.parentFrame;
                    }

                    this.branchFrame = chain1;
                    if (this.branchFrame != null && !this.branchFrame.frozen)
                        Context.CodeBug ();
                }
            }
        }

        static string bytecodeName (int bytecode)
        {
            if (!validBytecode (bytecode)) {
                throw new ArgumentException (Convert.ToString (bytecode));
            }

            if (!Token.printICode) {
                return Convert.ToString (bytecode);
            }

            if (ValidTokenCode (bytecode)) {
                return Token.name (bytecode);
            }

            switch (bytecode) {

                case Icode_DUP:
                    return "DUP";

                case Icode_DUP2:
                    return "DUP2";

                case Icode_SWAP:
                    return "SWAP";

                case Icode_POP:
                    return "POP";

                case Icode_POP_RESULT:
                    return "POP_RESULT";

                case Icode_IFEQ_POP:
                    return "IFEQ_POP";

                case Icode_VAR_INC_DEC:
                    return "VAR_INC_DEC";

                case Icode_NAME_INC_DEC:
                    return "NAME_INC_DEC";

                case Icode_PROP_INC_DEC:
                    return "PROP_INC_DEC";

                case Icode_ELEM_INC_DEC:
                    return "ELEM_INC_DEC";

                case Icode_REF_INC_DEC:
                    return "REF_INC_DEC";

                case Icode_SCOPE_LOAD:
                    return "SCOPE_LOAD";

                case Icode_SCOPE_SAVE:
                    return "SCOPE_SAVE";

                case Icode_TYPEOFNAME:
                    return "TYPEOFNAME";

                case Icode_NAME_AND_THIS:
                    return "NAME_AND_THIS";

                case Icode_PROP_AND_THIS:
                    return "PROP_AND_THIS";

                case Icode_ELEM_AND_THIS:
                    return "ELEM_AND_THIS";

                case Icode_VALUE_AND_THIS:
                    return "VALUE_AND_THIS";

                case Icode_CLOSURE_EXPR:
                    return "CLOSURE_EXPR";

                case Icode_CLOSURE_STMT:
                    return "CLOSURE_STMT";

                case Icode_CALLSPECIAL:
                    return "CALLSPECIAL";

                case Icode_RETUNDEF:
                    return "RETUNDEF";

                case Icode_GOSUB:
                    return "GOSUB";

                case Icode_STARTSUB:
                    return "STARTSUB";

                case Icode_RETSUB:
                    return "RETSUB";

                case Icode_LINE:
                    return "LINE";

                case Icode_SHORTNUMBER:
                    return "SHORTNUMBER";

                case Icode_INTNUMBER:
                    return "INTNUMBER";

                case Icode_LITERAL_NEW:
                    return "LITERAL_NEW";

                case Icode_LITERAL_SET:
                    return "LITERAL_SET";

                case Icode_SPARE_ARRAYLIT:
                    return "SPARE_ARRAYLIT";

                case Icode_REG_IND_C0:
                    return "REG_IND_C0";

                case Icode_REG_IND_C1:
                    return "REG_IND_C1";

                case Icode_REG_IND_C2:
                    return "REG_IND_C2";

                case Icode_REG_IND_C3:
                    return "REG_IND_C3";

                case Icode_REG_IND_C4:
                    return "REG_IND_C4";

                case Icode_REG_IND_C5:
                    return "REG_IND_C5";

                case Icode_REG_IND1:
                    return "LOAD_IND1";

                case Icode_REG_IND2:
                    return "LOAD_IND2";

                case Icode_REG_IND4:
                    return "LOAD_IND4";

                case Icode_REG_STR_C0:
                    return "REG_STR_C0";

                case Icode_REG_STR_C1:
                    return "REG_STR_C1";

                case Icode_REG_STR_C2:
                    return "REG_STR_C2";

                case Icode_REG_STR_C3:
                    return "REG_STR_C3";

                case Icode_REG_STR1:
                    return "LOAD_STR1";

                case Icode_REG_STR2:
                    return "LOAD_STR2";

                case Icode_REG_STR4:
                    return "LOAD_STR4";

                case Icode_GETVAR1:
                    return "GETVAR1";

                case Icode_SETVAR1:
                    return "SETVAR1";

                case Icode_UNDEF:
                    return "UNDEF";

                case Icode_ZERO:
                    return "ZERO";

                case Icode_ONE:
                    return "ONE";

                case Icode_ENTERDQ:
                    return "ENTERDQ";

                case Icode_LEAVEDQ:
                    return "LEAVEDQ";

                case Icode_TAIL_CALL:
                    return "TAIL_CALL";

                case Icode_LOCAL_CLEAR:
                    return "LOCAL_CLEAR";

                case Icode_DEBUGGER:
                    return "DEBUGGER";
            }

            // icode without name
            throw new ApplicationException (Convert.ToString (bytecode));
        }

        static bool validIcode (int icode)
        {
            return MIN_ICODE <= icode && icode <= -1;
        }

        static bool ValidTokenCode (int token)
        {
            return Token.FIRST_BYTECODE_TOKEN <= token && token <= Token.LAST_BYTECODE_TOKEN;
        }

        static bool validBytecode (int bytecode)
        {
            return validIcode (bytecode) || ValidTokenCode (bytecode);
        }

        public virtual object Compile (CompilerEnvirons compilerEnv, ScriptOrFnNode tree, string encodedSource, bool returnFunction)
        {
            this.compilerEnv = compilerEnv;
            new NodeTransformer ().transform (tree);

            if (Token.printTrees) {
                System.Console.Out.WriteLine (tree.toStringTree (tree));
            }

            if (returnFunction) {
                tree = tree.getFunctionNode (0);
            }

            scriptOrFn = tree;
            itsData = new InterpreterData (compilerEnv.LanguageVersion, scriptOrFn.SourceName, encodedSource);
            itsData.topLevel = true;

            if (returnFunction) {
                generateFunctionICode ();
            }
            else {
                generateICodeFromTree (scriptOrFn);
            }

            return itsData;
        }

        public virtual IScript CreateScriptObject (object bytecode, object staticSecurityDomain)
        {
            InterpreterData idata = (InterpreterData)bytecode;
            return InterpretedFunction.createScript (itsData, staticSecurityDomain);
        }

        public virtual IFunction CreateFunctionObject (Context cx, IScriptable scope, object bytecode, object staticSecurityDomain)
        {
            InterpreterData idata = (InterpreterData)bytecode;
            return InterpretedFunction.createFunction (cx, scope, itsData, staticSecurityDomain);
        }

        void generateFunctionICode ()
        {
            itsInFunctionFlag = true;

            FunctionNode theFunction = (FunctionNode)scriptOrFn;

            itsData.itsFunctionType = theFunction.FunctionType;
            itsData.itsNeedsActivation = theFunction.RequiresActivation;
            itsData.itsName = theFunction.FunctionName;
            if (!theFunction.IgnoreDynamicScope) {
                if (compilerEnv.UseDynamicScope) {
                    itsData.useDynamicScope = true;
                }
            }

            generateICodeFromTree (theFunction.LastChild);
        }

        void generateICodeFromTree (Node tree)
        {
            generateNestedFunctions ();

            generateRegExpLiterals ();

            VisitStatement (tree);
            fixLabelGotos ();
            // add RETURN_RESULT only to scripts as function always ends with RETURN
            if (itsData.itsFunctionType == 0) {
                addToken (Token.RETURN_RESULT);
            }

            if (itsData.itsICode.Length != itsICodeTop) {
                // Make itsData.itsICode length exactly itsICodeTop to save memory
                // and catch bugs with jumps beyound icode as early as possible
                sbyte [] tmp = new sbyte [itsICodeTop];
                Array.Copy (itsData.itsICode, 0, tmp, 0, itsICodeTop);
                itsData.itsICode = tmp;
            }
            if (itsStrings.size () == 0) {
                itsData.itsStringTable = null;
            }
            else {
                itsData.itsStringTable = new string [itsStrings.size ()];
                ObjToIntMap.Iterator iter = itsStrings.newIterator ();
                for (iter.start (); !iter.done (); iter.next ()) {
                    string str = (string)iter.Key;
                    int index = iter.Value;
                    if (itsData.itsStringTable [index] != null)
                        Context.CodeBug ();
                    itsData.itsStringTable [index] = str;
                }
            }
            if (itsDoubleTableTop == 0) {
                itsData.itsDoubleTable = null;
            }
            else if (itsData.itsDoubleTable.Length != itsDoubleTableTop) {
                double [] tmp = new double [itsDoubleTableTop];
                Array.Copy (itsData.itsDoubleTable, 0, tmp, 0, itsDoubleTableTop);
                itsData.itsDoubleTable = tmp;
            }
            if (itsExceptionTableTop != 0 && itsData.itsExceptionTable.Length != itsExceptionTableTop) {
                int [] tmp = new int [itsExceptionTableTop];
                Array.Copy (itsData.itsExceptionTable, 0, tmp, 0, itsExceptionTableTop);
                itsData.itsExceptionTable = tmp;
            }

            itsData.itsMaxVars = scriptOrFn.ParamAndVarCount;
            // itsMaxFrameArray: interpret method needs this amount for its
            // stack and sDbl arrays
            itsData.itsMaxFrameArray = itsData.itsMaxVars + itsData.itsMaxLocals + itsData.itsMaxStack;

            itsData.argNames = scriptOrFn.ParamAndVarNames;
            itsData.argCount = scriptOrFn.ParamCount;

            itsData.encodedSourceStart = scriptOrFn.EncodedSourceStart;
            itsData.encodedSourceEnd = scriptOrFn.EncodedSourceEnd;

            if (itsLiteralIds.size () != 0) {
                itsData.literalIds = itsLiteralIds.ToArray ();
            }

            if (Token.printICode)
                dumpICode (itsData);
        }

        void generateNestedFunctions ()
        {
            int functionCount = scriptOrFn.FunctionCount;
            if (functionCount == 0)
                return;

            InterpreterData [] array = new InterpreterData [functionCount];
            for (int i = 0; i != functionCount; i++) {
                FunctionNode def = scriptOrFn.getFunctionNode (i);
                Interpreter jsi = new Interpreter ();
                jsi.compilerEnv = compilerEnv;
                jsi.scriptOrFn = def;
                jsi.itsData = new InterpreterData (itsData);
                jsi.generateFunctionICode ();
                array [i] = jsi.itsData;
            }
            itsData.itsNestedFunctions = array;
        }

        void generateRegExpLiterals ()
        {
            int N = scriptOrFn.RegexpCount;
            if (N == 0)
                return;

            Context cx = Context.CurrentContext;
            RegExpProxy rep = cx.RegExpProxy;
            object [] array = new object [N];
            for (int i = 0; i != N; i++) {
                string str = scriptOrFn.getRegexpString (i);
                string flags = scriptOrFn.getRegexpFlags (i);
                array [i] = rep.Compile (cx, str, flags);
            }
            itsData.itsRegExpLiterals = array;
        }

        void updateLineNumber (Node node)
        {
            int lineno = node.Lineno;
            if (lineno != itsLineNumber && lineno >= 0) {
                if (itsData.firstLinePC < 0) {
                    itsData.firstLinePC = lineno;
                }
                itsLineNumber = lineno;
                addIcode (Icode_LINE);
                addUint16 (lineno & 0xFFFF);
            }
        }

        ApplicationException badTree (Node node)
        {
            throw new ApplicationException (node.ToString ());
        }

        void VisitStatement (Node node)
        {
            int type = node.Type;
            Node child = node.FirstChild;
            switch (type) {


                case Token.FUNCTION: {
                        int fnIndex = node.getExistingIntProp (Node.FUNCTION_PROP);
                        int fnType = scriptOrFn.getFunctionNode (fnIndex).FunctionType;
                        // Only function expressions or function expression
                        // statements needs closure code creating new function
                        // object on stack as function statements are initialized
                        // at script/function start
                        // In addition function expression can not present here
                        // at statement level, they must only present as expressions.
                        if (fnType == FunctionNode.FUNCTION_EXPRESSION_STATEMENT) {
                            addIndexOp (Icode_CLOSURE_STMT, fnIndex);
                        }
                        else {
                            if (fnType != FunctionNode.FUNCTION_STATEMENT) {
                                throw Context.CodeBug ();
                            }
                        }
                    }
                    break;


                case Token.SCRIPT:
                case Token.LABEL:
                case Token.LOOP:
                case Token.BLOCK:
                case Token.EMPTY:
                case Token.WITH:
                    updateLineNumber (node);
                    while (child != null) {
                        VisitStatement (child);
                        child = child.Next;
                    }
                    break;


                case Token.ENTERWITH:
                    VisitExpression (child, 0);
                    addToken (Token.ENTERWITH);
                    stackChange (-1);
                    break;


                case Token.LEAVEWITH:
                    addToken (Token.LEAVEWITH);
                    break;


                case Token.LOCAL_BLOCK: {
                        int local = allocLocal ();
                        node.putIntProp (Node.LOCAL_PROP, local);
                        updateLineNumber (node);
                        while (child != null) {
                            VisitStatement (child);
                            child = child.Next;
                        }
                        addIndexOp (Icode_LOCAL_CLEAR, local);
                        releaseLocal (local);
                    }
                    break;

                case Token.DEBUGGER:
                    updateLineNumber (node);
                    addIcode (Icode_DEBUGGER);
                    break;

                case Token.SWITCH:
                    updateLineNumber (node); {
                        // See comments in IRFactory.createSwitch() for description
                        // of SWITCH node
                        Node switchNode = (Node.Jump)node;
                        VisitExpression (child, 0);
                        for (Node.Jump caseNode = (Node.Jump)child.Next; caseNode != null; caseNode = (Node.Jump)caseNode.Next) {
                            if (caseNode.Type != Token.CASE)
                                throw badTree (caseNode);
                            Node test = caseNode.FirstChild;
                            addIcode (Icode_DUP);
                            stackChange (1);
                            VisitExpression (test, 0);
                            addToken (Token.SHEQ);
                            stackChange (-1);
                            // If true, Icode_IFEQ_POP will jump and remove case
                            // value from stack
                            addGoto (caseNode.target, Icode_IFEQ_POP);
                            stackChange (-1);
                        }
                        addIcode (Icode_POP);
                        stackChange (-1);
                    }
                    break;


                case Token.TARGET:
                    markTargetLabel (node);
                    break;


                case Token.IFEQ:
                case Token.IFNE: {
                        Node target = ((Node.Jump)node).target;
                        VisitExpression (child, 0);
                        addGoto (target, type);
                        stackChange (-1);
                    }
                    break;


                case Token.GOTO: {
                        Node target = ((Node.Jump)node).target;
                        addGoto (target, type);
                    }
                    break;


                case Token.JSR: {
                        Node target = ((Node.Jump)node).target;
                        addGoto (target, Icode_GOSUB);
                    }
                    break;


                case Token.FINALLY: {
                        // Account for incomming GOTOSUB address
                        stackChange (1);
                        int finallyRegister = getLocalBlockRef (node);
                        addIndexOp (Icode_STARTSUB, finallyRegister);
                        stackChange (-1);
                        while (child != null) {
                            VisitStatement (child);
                            child = child.Next;
                        }
                        addIndexOp (Icode_RETSUB, finallyRegister);
                    }
                    break;


                case Token.EXPR_VOID:
                case Token.EXPR_RESULT:
                    updateLineNumber (node);
                    VisitExpression (child, 0);
                    addIcode ((type == Token.EXPR_VOID) ? Icode_POP : Icode_POP_RESULT);
                    stackChange (-1);
                    break;


                case Token.TRY: {
                        Node.Jump tryNode = (Node.Jump)node;
                        int exceptionObjectLocal = getLocalBlockRef (tryNode);
                        int scopeLocal = allocLocal ();

                        addIndexOp (Icode_SCOPE_SAVE, scopeLocal);

                        int tryStart = itsICodeTop;
                        while (child != null) {
                            VisitStatement (child);
                            child = child.Next;
                        }

                        Node catchTarget = tryNode.target;
                        if (catchTarget != null) {
                            int catchStartPC = itsLabelTable [getTargetLabel (catchTarget)];
                            addExceptionHandler (tryStart, catchStartPC, catchStartPC, false, exceptionObjectLocal, scopeLocal);
                        }
                        Node finallyTarget = tryNode.Finally;
                        if (finallyTarget != null) {
                            int finallyStartPC = itsLabelTable [getTargetLabel (finallyTarget)];
                            addExceptionHandler (tryStart, finallyStartPC, finallyStartPC, true, exceptionObjectLocal, scopeLocal);
                        }

                        addIndexOp (Icode_LOCAL_CLEAR, scopeLocal);
                        releaseLocal (scopeLocal);
                    }
                    break;


                case Token.CATCH_SCOPE: {
                        int localIndex = getLocalBlockRef (node);
                        int scopeIndex = node.getExistingIntProp (Node.CATCH_SCOPE_PROP);
                        string name = child.String;
                        child = child.Next;
                        VisitExpression (child, 0); // load expression object
                        addStringPrefix (name);
                        addIndexPrefix (localIndex);
                        addToken (Token.CATCH_SCOPE);
                        addUint8 (scopeIndex != 0 ? 1 : 0);
                        stackChange (-1);
                    }
                    break;


                case Token.THROW:
                    updateLineNumber (node);
                    VisitExpression (child, 0);
                    addToken (Token.THROW);
                    addUint16 (itsLineNumber & 0xFFFF);
                    stackChange (-1);
                    break;


                case Token.RETHROW:
                    updateLineNumber (node);
                    addIndexOp (Token.RETHROW, getLocalBlockRef (node));
                    break;


                case Token.RETURN:
                    updateLineNumber (node);
                    if (child != null) {
                        VisitExpression (child, ECF_TAIL);
                        addToken (Token.RETURN);
                        stackChange (-1);
                    }
                    else {
                        addIcode (Icode_RETUNDEF);
                    }
                    break;


                case Token.RETURN_RESULT:
                    updateLineNumber (node);
                    addToken (Token.RETURN_RESULT);
                    break;


                case Token.ENUM_INIT_KEYS:
                case Token.ENUM_INIT_VALUES:
                    VisitExpression (child, 0);
                    addIndexOp (type, getLocalBlockRef (node));
                    stackChange (-1);
                    break;


                default:
                    throw badTree (node);

            }

            if (itsStackDepth != 0) {
                throw Context.CodeBug ();
            }
        }

        bool VisitExpressionOptimized (Node node, int contextFlags)
        {
            return false;
#if FALKSE
            if (node.Type == Token.ADD) {
                Node next = node.Next;
                if (next == null)
                    return false;
                switch (next.Type) { 
                    case Token.NAME:
                    case Token.STRING:
                        return true;
                }
            }
            return false;
#endif
        }

        void VisitExpression (Node node, int contextFlags)
        {
            if (VisitExpressionOptimized (node, contextFlags))
                return;

            int type = node.Type;
            Node child = node.FirstChild;
            int savedStackDepth = itsStackDepth;
            switch (type) {


                case Token.FUNCTION: {
                        int fnIndex = node.getExistingIntProp (Node.FUNCTION_PROP);
                        FunctionNode fn = scriptOrFn.getFunctionNode (fnIndex);

                        // See comments in visitStatement for Token.FUNCTION case					
                        switch (fn.FunctionType) {
                            case FunctionNode.FUNCTION_EXPRESSION:
                                addIndexOp (Icode_CLOSURE_EXPR, fnIndex);
                                break;
                            default:
                                throw Context.CodeBug ();
                        }

                        stackChange (1);
                    }
                    break;


                case Token.LOCAL_LOAD: {
                        int localIndex = getLocalBlockRef (node);
                        addIndexOp (Token.LOCAL_LOAD, localIndex);
                        stackChange (1);
                    }
                    break;


                case Token.COMMA: {
                        Node lastChild = node.LastChild;
                        while (child != lastChild) {
                            VisitExpression (child, 0);
                            addIcode (Icode_POP);
                            stackChange (-1);
                            child = child.Next;
                        }
                        // Preserve tail context flag if any
                        VisitExpression (child, contextFlags & ECF_TAIL);
                    }
                    break;


                case Token.USE_STACK:
                    // Indicates that stack was modified externally,
                    // like placed catch object
                    stackChange (1);
                    break;


                case Token.REF_CALL:
                case Token.CALL:
                case Token.NEW: {
                        if (type == Token.NEW) {
                            VisitExpression (child, 0);
                        }
                        else {
                            generateCallFunAndThis (child);
                        }
                        int argCount = 0;
                        while ((child = child.Next) != null) {
                            VisitExpression (child, 0);
                            ++argCount;
                        }
                        int callType = node.getIntProp (Node.SPECIALCALL_PROP, Node.NON_SPECIALCALL);
                        if (callType != Node.NON_SPECIALCALL) {
                            // embed line number and source filename
                            addIndexOp (Icode_CALLSPECIAL, argCount);
                            addUint8 (callType);
                            addUint8 (type == Token.NEW ? 1 : 0);
                            addUint16 (itsLineNumber & 0xFFFF);
                        }
                        else {
                            if (type == Token.CALL) {
                                if ((contextFlags & ECF_TAIL) != 0) {
                                    type = Icode_TAIL_CALL;
                                }
                            }
                            addIndexOp (type, argCount);
                        }
                        // adjust stack
                        if (type == Token.NEW) {
                            // new: f, args -> result
                            stackChange (-argCount);
                        }
                        else {
                            // call: f, thisObj, args -> result
                            // ref_call: f, thisObj, args -> ref
                            stackChange (-1 - argCount);
                        }
                        if (argCount > itsData.itsMaxCalleeArgs) {
                            itsData.itsMaxCalleeArgs = argCount;
                        }
                    }
                    break;


                case Token.AND:
                case Token.OR: {
                        VisitExpression (child, 0);
                        addIcode (Icode_DUP);
                        stackChange (1);
                        int afterSecondJumpStart = itsICodeTop;
                        int jump = (type == Token.AND) ? Token.IFNE : Token.IFEQ;
                        addGotoOp (jump);
                        stackChange (-1);
                        addIcode (Icode_POP);
                        stackChange (-1);
                        child = child.Next;
                        // Preserve tail context flag if any
                        VisitExpression (child, contextFlags & ECF_TAIL);
                        resolveForwardGoto (afterSecondJumpStart);
                    }
                    break;


                case Token.HOOK: {
                        Node ifThen = child.Next;
                        Node ifElse = ifThen.Next;
                        VisitExpression (child, 0);
                        int elseJumpStart = itsICodeTop;
                        addGotoOp (Token.IFNE);
                        ;
                        stackChange (-1);
                        // Preserve tail context flag if any
                        VisitExpression (ifThen, contextFlags & ECF_TAIL);
                        int afterElseJumpStart = itsICodeTop;
                        addGotoOp (Token.GOTO);
                        resolveForwardGoto (elseJumpStart);
                        itsStackDepth = savedStackDepth;
                        // Preserve tail context flag if any
                        VisitExpression (ifElse, contextFlags & ECF_TAIL);
                        resolveForwardGoto (afterElseJumpStart);
                    }
                    break;


                case Token.GETPROP:
                    VisitExpression (child, 0);
                    child = child.Next;
                    addStringOp (Token.GETPROP, child.String);
                    break;


                case Token.ADD:
                case Token.GETELEM:
                case Token.DELPROP:
                case Token.BITAND:
                case Token.BITOR:
                case Token.BITXOR:
                case Token.LSH:
                case Token.RSH:
                case Token.URSH:
                case Token.SUB:
                case Token.MOD:
                case Token.DIV:
                case Token.MUL:
                case Token.EQ:
                case Token.NE:
                case Token.SHEQ:
                case Token.SHNE:
                case Token.IN:
                case Token.INSTANCEOF:
                case Token.LE:
                case Token.LT:
                case Token.GE:
                case Token.GT:
                    VisitExpression (child, 0);
                    VisitExpression (child.Next, 0);
                    addToken (type);
                    stackChange (-1);
                    break;


                case Token.POS:
                case Token.NEG:
                case Token.NOT:
                case Token.BITNOT:
                case Token.TYPEOF:
                case Token.VOID:
                    VisitExpression (child, 0);
                    if (type == Token.VOID) {
                        addIcode (Icode_POP);
                        addIcode (Icode_UNDEF);
                    }
                    else {
                        addToken (type);
                    }
                    break;


                case Token.GET_REF:
                case Token.DEL_REF:
                    VisitExpression (child, 0);
                    addToken (type);
                    break;


                case Token.SETPROP:
                case Token.SETPROP_OP:
                case Token.SETPROP_GETTER:
                case Token.SETPROP_SETTER: 
                    {
                        VisitExpression (child, 0);
                        child = child.Next;
                        string property = child.String;
                        child = child.Next;
                        if (type == Token.SETPROP_OP) {
                            addIcode (Icode_DUP);
                            stackChange (1);
                            addStringOp (Token.GETPROP, property);
                            // Compensate for the following USE_STACK
                            stackChange (-1);
                        }
                        VisitExpression (child, 0);
                        addStringOp ((type == Token.SETPROP_OP) ? Token.SETPROP : type, property);
                        stackChange (-1);
                    }
                    break;


                case Token.SETELEM:
                case Token.SETELEM_OP:
                    VisitExpression (child, 0);
                    child = child.Next;
                    VisitExpression (child, 0);
                    child = child.Next;
                    if (type == Token.SETELEM_OP) {
                        addIcode (Icode_DUP2);
                        stackChange (2);
                        addToken (Token.GETELEM);
                        stackChange (-1);
                        // Compensate for the following USE_STACK
                        stackChange (-1);
                    }
                    VisitExpression (child, 0);
                    addToken (Token.SETELEM);
                    stackChange (-2);
                    break;


                case Token.SET_REF:
                case Token.SET_REF_OP:
                    VisitExpression (child, 0);
                    child = child.Next;
                    if (type == Token.SET_REF_OP) {
                        addIcode (Icode_DUP);
                        stackChange (1);
                        addToken (Token.GET_REF);
                        // Compensate for the following USE_STACK
                        stackChange (-1);
                    }
                    VisitExpression (child, 0);
                    addToken (Token.SET_REF);
                    stackChange (-1);
                    break;


                case Token.SETNAME: {
                        string name = child.String;
                        VisitExpression (child, 0);
                        child = child.Next;
                        VisitExpression (child, 0);
                        addStringOp (Token.SETNAME, name);
                        stackChange (-1);
                    }
                    break;


                case Token.TYPEOFNAME: {
                        string name = node.String;
                        int index = -1;
                        // use typeofname if an activation frame exists
                        // since the vars all exist there instead of in jregs
                        if (itsInFunctionFlag && !itsData.itsNeedsActivation)
                            index = scriptOrFn.getParamOrVarIndex (name);
                        if (index == -1) {
                            addStringOp (Icode_TYPEOFNAME, name);
                            stackChange (1);
                        }
                        else {
                            addVarOp (Token.GETVAR, index);
                            stackChange (1);
                            addToken (Token.TYPEOF);
                        }
                    }
                    break;


                case Token.BINDNAME:
                case Token.NAME:
                case Token.STRING:
                    addStringOp (type, node.String);
                    stackChange (1);
                    break;


                case Token.INC:
                case Token.DEC:
                    VisitIncDec (node, child);
                    break;


                case Token.NUMBER: {
                        double num = node.Double;
                        int inum = (int)num;
                        if (inum == num) {
                            if (inum == 0) {
                                addIcode (Icode_ZERO);
                                // Check for negative zero
                                if (1.0 / num < 0.0) {
                                    addToken (Token.NEG);
                                }
                            }
                            else if (inum == 1) {
                                addIcode (Icode_ONE);
                            }
                            else if ((short)inum == inum) {
                                addIcode (Icode_SHORTNUMBER);
                                // write short as uin16 bit pattern
                                addUint16 (inum & 0xFFFF);
                            }
                            else {
                                addIcode (Icode_INTNUMBER);
                                addInt (inum);
                            }
                        }
                        else {
                            int index = GetDoubleIndex (num);
                            addIndexOp (Token.NUMBER, index);
                        }
                        stackChange (1);
                    }
                    break;


                case Token.GETVAR: {
                        if (itsData.itsNeedsActivation)
                            Context.CodeBug ();
                        string name = node.String;
                        int index = scriptOrFn.getParamOrVarIndex (name);
                        addVarOp (Token.GETVAR, index);
                        stackChange (1);
                    }
                    break;


                case Token.SETVAR: {
                        if (itsData.itsNeedsActivation)
                            Context.CodeBug ();
                        string name = child.String;
                        child = child.Next;
                        VisitExpression (child, 0);
                        int index = scriptOrFn.getParamOrVarIndex (name);
                        addVarOp (Token.SETVAR, index);
                    }
                    break;



                case Token.NULL:
                case Token.THIS:
                case Token.THISFN:
                case Token.FALSE:
                case Token.TRUE:
                    addToken (type);
                    stackChange (1);
                    break;


                case Token.ENUM_NEXT:
                case Token.ENUM_ID:
                    addIndexOp (type, getLocalBlockRef (node));
                    stackChange (1);
                    break;


                case Token.REGEXP: {
                        int index = node.getExistingIntProp (Node.REGEXP_PROP);
                        addIndexOp (Token.REGEXP, index);
                        stackChange (1);
                    }
                    break;


                case Token.ARRAYLIT:
                case Token.OBJECTLIT:
                    VisitLiteral (node, child);
                    break;


                case Token.REF_SPECIAL:
                    VisitExpression (child, 0);
                    addStringOp (type, (string)node.getProp (Node.NAME_PROP));
                    break;


                case Token.REF_MEMBER:
                case Token.REF_NS_MEMBER:
                case Token.REF_NAME:
                case Token.REF_NS_NAME: {
                        int memberTypeFlags = node.getIntProp (Node.MEMBER_TYPE_PROP, 0);
                        // generate possible target, possible namespace and member
                        int childCount = 0;
                        do {
                            VisitExpression (child, 0);
                            ++childCount;
                            child = child.Next;
                        }
                        while (child != null);
                        addIndexOp (type, memberTypeFlags);
                        stackChange (1 - childCount);
                    }
                    break;


                case Token.DOTQUERY: {
                        int queryPC;
                        updateLineNumber (node);
                        VisitExpression (child, 0);
                        addIcode (Icode_ENTERDQ);
                        stackChange (-1);
                        queryPC = itsICodeTop;
                        VisitExpression (child.Next, 0);
                        addBackwardGoto (Icode_LEAVEDQ, queryPC);
                    }
                    break;


                case Token.DEFAULTNAMESPACE:
                case Token.ESCXMLATTR:
                case Token.ESCXMLTEXT:
                    VisitExpression (child, 0);
                    addToken (type);
                    break;

                default:
                    throw badTree (node);

            }
            //if (savedStackDepth + 1 != itsStackDepth) {
            //    EcmaScriptHelper.CodeBug();
            //}
        }

        void generateCallFunAndThis (Node left)
        {
            // Generate code to place on stack function and thisObj
            int type = left.Type;
            switch (type) {

                case Token.NAME: {
                        string name = left.String;
                        // stack: ... -> ... function thisObj
                        addStringOp (Icode_NAME_AND_THIS, name);
                        stackChange (2);
                        break;
                    }

                case Token.GETPROP:
                case Token.GETELEM: {
                        Node target = left.FirstChild;
                        VisitExpression (target, 0);
                        Node id = target.Next;
                        if (type == Token.GETPROP) {
                            string property = id.String;
                            // stack: ... target -> ... function thisObj
                            addStringOp (Icode_PROP_AND_THIS, property);
                            stackChange (1);
                        }
                        else {
                            VisitExpression (id, 0);
                            // stack: ... target id -> ... function thisObj
                            addIcode (Icode_ELEM_AND_THIS);
                        }
                        break;
                    }

                default:
                    // Including Token.GETVAR
                    VisitExpression (left, 0);
                    // stack: ... value -> ... function thisObj
                    addIcode (Icode_VALUE_AND_THIS);
                    stackChange (1);
                    break;

            }
        }

        void VisitIncDec (Node node, Node child)
        {
            int incrDecrMask = node.getExistingIntProp (Node.INCRDECR_PROP);
            int childType = child.Type;
            switch (childType) {

                case Token.GETVAR: {
                        if (itsData.itsNeedsActivation)
                            Context.CodeBug ();
                        string name = child.String;
                        int i = scriptOrFn.getParamOrVarIndex (name);
                        addVarOp (Icode_VAR_INC_DEC, i);
                        addUint8 (incrDecrMask);
                        stackChange (1);
                        break;
                    }

                case Token.NAME: {
                        string name = child.String;
                        addStringOp (Icode_NAME_INC_DEC, name);
                        addUint8 (incrDecrMask);
                        stackChange (1);
                        break;
                    }

                case Token.GETPROP: {
                        Node obj = child.FirstChild;
                        VisitExpression (obj, 0);
                        string property = obj.Next.String;
                        addStringOp (Icode_PROP_INC_DEC, property);
                        addUint8 (incrDecrMask);
                        break;
                    }

                case Token.GETELEM: {
                        Node obj = child.FirstChild;
                        VisitExpression (obj, 0);
                        Node index = obj.Next;
                        VisitExpression (index, 0);
                        addIcode (Icode_ELEM_INC_DEC);
                        addUint8 (incrDecrMask);
                        stackChange (-1);
                        break;
                    }

                case Token.GET_REF: {
                        Node rf = child.FirstChild;
                        VisitExpression (rf, 0);
                        addIcode (Icode_REF_INC_DEC);
                        addUint8 (incrDecrMask);
                        break;
                    }

                default: {
                        throw badTree (node);
                    }

            }
        }

        void VisitLiteral (Node node, Node child)
        {
            int type = node.Type;
            int count;
            object [] propertyIds = null;
            if (type == Token.ARRAYLIT) {
                count = 0;
                for (Node n = child; n != null; n = n.Next) {
                    ++count;
                }
            }
            else if (type == Token.OBJECTLIT) {
                propertyIds = (object [])node.getProp (Node.OBJECT_IDS_PROP);
                count = propertyIds.Length;
            }
            else {
                throw badTree (node);
            }
            addIndexOp (Icode_LITERAL_NEW, count);
            stackChange (1);
            while (child != null) {
                VisitExpression (child, 0);
                addIcode (Icode_LITERAL_SET);
                stackChange (-1);
                child = child.Next;
            }
            if (type == Token.ARRAYLIT) {
                int [] skipIndexes = (int [])node.getProp (Node.SKIP_INDEXES_PROP);
                if (skipIndexes == null) {
                    addToken (Token.ARRAYLIT);
                }
                else {
                    int index = itsLiteralIds.size ();
                    itsLiteralIds.add (skipIndexes);
                    addIndexOp (Icode_SPARE_ARRAYLIT, index);
                }
            }
            else {
                int index = itsLiteralIds.size ();
                itsLiteralIds.add (propertyIds);
                addIndexOp (Token.OBJECTLIT, index);
            }
        }

        int getLocalBlockRef (Node node)
        {
            Node localBlock = (Node)node.getProp (Node.LOCAL_BLOCK_PROP);
            return localBlock.getExistingIntProp (Node.LOCAL_PROP);
        }

        int getTargetLabel (Node target)
        {
            int label = target.labelId ();
            if (label != -1) {
                return label;
            }
            label = itsLabelTableTop;
            if (itsLabelTable == null || label == itsLabelTable.Length) {
                if (itsLabelTable == null) {
                    itsLabelTable = new int [MIN_LABEL_TABLE_SIZE];
                }
                else {
                    int [] tmp = new int [itsLabelTable.Length * 2];
                    Array.Copy (itsLabelTable, 0, tmp, 0, label);
                    itsLabelTable = tmp;
                }
            }
            itsLabelTableTop = label + 1;
            itsLabelTable [label] = -1;

            target.labelId (label);
            return label;
        }

        void markTargetLabel (Node target)
        {
            int label = getTargetLabel (target);
            if (itsLabelTable [label] != -1) {
                // Can mark label only once
                Context.CodeBug ();
            }
            itsLabelTable [label] = itsICodeTop;
        }

        void addGoto (Node target, int gotoOp)
        {
            int label = getTargetLabel (target);
            if (!(label < itsLabelTableTop))
                Context.CodeBug ();
            int targetPC = itsLabelTable [label];

            if (targetPC != -1) {
                addBackwardGoto (gotoOp, targetPC);
            }
            else {
                int gotoPC = itsICodeTop;
                addGotoOp (gotoOp);
                int top = itsFixupTableTop;
                if (itsFixupTable == null || top == itsFixupTable.Length) {
                    if (itsFixupTable == null) {
                        itsFixupTable = new long [MIN_FIXUP_TABLE_SIZE];
                    }
                    else {
                        long [] tmp = new long [itsFixupTable.Length * 2];
                        Array.Copy (itsFixupTable, 0, tmp, 0, top);
                        itsFixupTable = tmp;
                    }
                }
                itsFixupTableTop = top + 1;
                itsFixupTable [top] = ((long)label << 32) | (uint)gotoPC;
            }
        }

        void fixLabelGotos ()
        {
            for (int i = 0; i < itsFixupTableTop; i++) {
                long fixup = itsFixupTable [i];
                int label = (int)(fixup >> 32);
                int jumpSource = (int)fixup;
                int pc = itsLabelTable [label];
                if (pc == -1) {
                    // Unlocated label
                    throw Context.CodeBug ();
                }
                resolveGoto (jumpSource, pc);
            }
            itsFixupTableTop = 0;
        }

        void addBackwardGoto (int gotoOp, int jumpPC)
        {
            int fromPC = itsICodeTop;
            // Ensure that this is a jump backward  
            if (fromPC <= jumpPC)
                throw Context.CodeBug ();
            addGotoOp (gotoOp);
            resolveGoto (fromPC, jumpPC);
        }


        void resolveForwardGoto (int fromPC)
        {
            // Ensure that forward jump skips at least self bytecode                
            if (itsICodeTop < fromPC + 3)
                throw Context.CodeBug ();
            resolveGoto (fromPC, itsICodeTop);
        }

        void resolveGoto (int fromPC, int jumpPC)
        {
            int offset = jumpPC - fromPC;
            // Ensure that jumps do not overlap                                     
            if (0 <= offset && offset <= 2)
                throw Context.CodeBug ();
            int offsetSite = fromPC + 1;
            if (offset != (short)offset) {
                if (itsData.longJumps == null) {
                    itsData.longJumps = new UintMap ();
                }
                itsData.longJumps.put (offsetSite, jumpPC);
                offset = 0;
            }
            sbyte [] array = itsData.itsICode;
            array [offsetSite] = (sbyte)(offset >> 8);
            array [offsetSite + 1] = (sbyte)offset;
        }

        void addToken (int token)
        {
            if (!ValidTokenCode (token))
                throw Context.CodeBug ();
            addUint8 (token);
        }

        void addIcode (int icode)
        {
            if (!validIcode (icode))
                throw Context.CodeBug ();
            // Write negative icode as uint8 bits
            addUint8 (icode & 0xFF);
        }

        void addUint8 (int value)
        {
            if ((value & ~0xFF) != 0)
                throw Context.CodeBug ();
            sbyte [] array = itsData.itsICode;
            int top = itsICodeTop;
            if (top == array.Length) {
                array = increaseICodeCapasity (1);
            }
            array [top] = (sbyte)value;
            itsICodeTop = top + 1;
        }

        void addUint16 (int value)
        {
            if ((value & ~0xFFFF) != 0)
                throw Context.CodeBug ();
            sbyte [] array = itsData.itsICode;
            int top = itsICodeTop;
            if (top + 2 > array.Length) {
                array = increaseICodeCapasity (2);
            }
            array [top] = (sbyte)((uint)value >> 8);
            array [top + 1] = (sbyte)value;
            itsICodeTop = top + 2;
        }

        void addInt (int i)
        {
            sbyte [] array = itsData.itsICode;
            int top = itsICodeTop;
            if (top + 4 > array.Length) {
                array = increaseICodeCapasity (4);
            }
            array [top] = (sbyte)((uint)i >> 24);
            array [top + 1] = (sbyte)((uint)i >> 16);
            array [top + 2] = (sbyte)((uint)i >> 8);
            array [top + 3] = (sbyte)i;
            itsICodeTop = top + 4;
        }

        int GetDoubleIndex (double num)
        {
            int index = itsDoubleTableTop;
            if (index == 0) {
                itsData.itsDoubleTable = new double [64];
            }
            else if (itsData.itsDoubleTable.Length == index) {
                double [] na = new double [index * 2];
                Array.Copy (itsData.itsDoubleTable, 0, na, 0, index);
                itsData.itsDoubleTable = na;
            }
            itsData.itsDoubleTable [index] = num;
            itsDoubleTableTop = index + 1;
            return index;
        }

        void addVarOp (int op, int varIndex)
        {
            switch (op) {

                case Token.GETVAR:
                case Token.SETVAR:
                    if (varIndex < 128) {
                        addIcode (op == Token.GETVAR ? Icode_GETVAR1 : Icode_SETVAR1);
                        addUint8 (varIndex);
                        return;
                    }
                    // fallthrough
                    goto case Icode_VAR_INC_DEC;

                case Icode_VAR_INC_DEC:
                    addIndexOp (op, varIndex);
                    return;
            }
            throw Context.CodeBug ();
        }

        void addStringOp (int op, string str)
        {
            addStringPrefix (str);
            if (validIcode (op)) {
                addIcode (op);
            }
            else {
                addToken (op);
            }
        }

        void addIndexOp (int op, int index)
        {
            addIndexPrefix (index);
            if (validIcode (op)) {
                addIcode (op);
            }
            else {
                addToken (op);
            }
        }

        void addStringPrefix (string str)
        {
            int index = itsStrings.Get (str, -1);
            if (index == -1) {
                index = itsStrings.size ();
                itsStrings.put (str, index);
            }
            if (index < 4) {
                addIcode (Icode_REG_STR_C0 - index);
            }
            else if (index <= 0xFF) {
                addIcode (Icode_REG_STR1);
                addUint8 (index);
            }
            else if (index <= 0xFFFF) {
                addIcode (Icode_REG_STR2);
                addUint16 (index);
            }
            else {
                addIcode (Icode_REG_STR4);
                addInt (index);
            }
        }

        void addIndexPrefix (int index)
        {
            if (index < 0)
                Context.CodeBug ();
            if (index < 6) {
                addIcode (Icode_REG_IND_C0 - index);
            }
            else if (index <= 0xFF) {
                addIcode (Icode_REG_IND1);
                addUint8 (index);
            }
            else if (index <= 0xFFFF) {
                addIcode (Icode_REG_IND2);
                addUint16 (index);
            }
            else {
                addIcode (Icode_REG_IND4);
                addInt (index);
            }
        }

        void addExceptionHandler (int icodeStart, int icodeEnd, int handlerStart, bool isFinally, int exceptionObjectLocal, int scopeLocal)
        {
            int top = itsExceptionTableTop;
            int [] table = itsData.itsExceptionTable;
            if (table == null) {
                if (top != 0)
                    Context.CodeBug ();
                table = new int [EXCEPTION_SLOT_SIZE * 2];
                itsData.itsExceptionTable = table;
            }
            else if (table.Length == top) {
                table = new int [table.Length * 2];
                Array.Copy (itsData.itsExceptionTable, 0, table, 0, top);
                itsData.itsExceptionTable = table;
            }
            table [top + EXCEPTION_TRY_START_SLOT] = icodeStart;
            table [top + EXCEPTION_TRY_END_SLOT] = icodeEnd;
            table [top + EXCEPTION_HANDLER_SLOT] = handlerStart;
            table [top + EXCEPTION_TYPE_SLOT] = isFinally ? 1 : 0;
            table [top + EXCEPTION_LOCAL_SLOT] = exceptionObjectLocal;
            table [top + EXCEPTION_SCOPE_SLOT] = scopeLocal;

            itsExceptionTableTop = top + EXCEPTION_SLOT_SIZE;
        }

        sbyte [] increaseICodeCapasity (int extraSize)
        {
            int capacity = itsData.itsICode.Length;
            int top = itsICodeTop;
            if (top + extraSize <= capacity)
                throw Context.CodeBug ();
            capacity *= 2;
            if (top + extraSize > capacity) {
                capacity = top + extraSize;
            }
            sbyte [] array = new sbyte [capacity];
            Array.Copy (itsData.itsICode, 0, array, 0, top);
            itsData.itsICode = array;
            return array;
        }

        void stackChange (int change)
        {
            if (change <= 0) {
                itsStackDepth += change;
            }
            else {
                int newDepth = itsStackDepth + change;
                if (newDepth > itsData.itsMaxStack) {
                    itsData.itsMaxStack = newDepth;
                }
                itsStackDepth = newDepth;
            }
        }

        int allocLocal ()
        {
            int localSlot = itsLocalTop;
            ++itsLocalTop;
            if (itsLocalTop > itsData.itsMaxLocals) {
                itsData.itsMaxLocals = itsLocalTop;
            }
            return localSlot;
        }

        void releaseLocal (int localSlot)
        {
            --itsLocalTop;
            if (localSlot != itsLocalTop)
                Context.CodeBug ();
        }

        static int GetShort (sbyte [] iCode, int pc)
        {
            return (iCode [pc] << 8) | (iCode [pc + 1] & 0xFF);
        }

        static int GetIndex (sbyte [] iCode, int pc)
        {
            return ((iCode [pc] & 0xFF) << 8) | (iCode [pc + 1] & 0xFF);
        }

        static int GetInt (sbyte [] iCode, int pc)
        {
            return (iCode [pc] << 24) | ((iCode [pc + 1] & 0xFF) << 16) | ((iCode [pc + 2] & 0xFF) << 8) | (iCode [pc + 3] & 0xFF);
        }

        static int getExceptionHandler (CallFrame frame, bool onlyFinally)
        {
            int [] exceptionTable = frame.idata.itsExceptionTable;
            if (exceptionTable == null) {
                // No exception handlers
                return -1;
            }

            // Icode switch in the interpreter increments PC immediately
            // and it is necessary to subtract 1 from the saved PC
            // to point it before the start of the next instruction.
            int pc = frame.pc - 1;

            // OPT: use binary search
            int best = -1, bestStart = 0, bestEnd = 0;
            for (int i = 0; i != exceptionTable.Length; i += EXCEPTION_SLOT_SIZE) {
                int start = exceptionTable [i + EXCEPTION_TRY_START_SLOT];
                int end = exceptionTable [i + EXCEPTION_TRY_END_SLOT];
                if (!(start <= pc && pc < end)) {
                    continue;
                }
                if (onlyFinally && exceptionTable [i + EXCEPTION_TYPE_SLOT] != 1) {
                    continue;
                }
                if (best >= 0) {
                    // Since handlers always nest and they never have shared end
                    // although they can share start  it is sufficient to compare
                    // handlers ends
                    if (bestEnd < end) {
                        continue;
                    }
                    // Check the above assumption
                    if (bestStart > start)
                        Context.CodeBug (); // should be nested
                    if (bestEnd == end)
                        Context.CodeBug (); // no ens sharing
                }
                best = i;
                bestStart = start;
                bestEnd = end;
            }
            return best;
        }

        static void dumpICode (InterpreterData idata)
        {
            if (!Token.printICode) {
                return;
            }

            sbyte [] iCode = idata.itsICode;
            int iCodeLength = iCode.Length;
            string [] strings = idata.itsStringTable;

            System.IO.TextWriter sw = Console.Out;
            sw.WriteLine ("ICode dump, for " + idata.itsName + ", length = " + iCodeLength);
            sw.WriteLine ("MaxStack = " + idata.itsMaxStack);

            int indexReg = 0;
            for (int pc = 0; pc < iCodeLength; ) {
                sw.Flush ();
                sw.Write (" [" + pc + "] ");
                int token = iCode [pc];
                int icodeLength = bytecodeSpan (token);
                string tname = bytecodeName (token);
                int old_pc = pc;
                ++pc;
                switch (token) {

                    default:
                        if (icodeLength != 1)
                            Context.CodeBug ();
                        sw.WriteLine (tname);
                        break;



                    case Icode_GOSUB:
                    case Token.GOTO:
                    case Token.IFEQ:
                    case Token.IFNE:
                    case Icode_IFEQ_POP:
                    case Icode_LEAVEDQ: {
                            int newPC = pc + GetShort (iCode, pc) - 1;
                            sw.WriteLine (tname + " " + newPC);
                            pc += 2;
                            break;
                        }

                    case Icode_VAR_INC_DEC:
                    case Icode_NAME_INC_DEC:
                    case Icode_PROP_INC_DEC:
                    case Icode_ELEM_INC_DEC:
                    case Icode_REF_INC_DEC: {
                            int incrDecrType = iCode [pc];
                            sw.WriteLine (tname + " " + incrDecrType);
                            ++pc;
                            break;
                        }


                    case Icode_CALLSPECIAL: {
                            int callType = iCode [pc] & 0xFF;
                            bool isNew = (iCode [pc + 1] != 0);
                            int line = GetIndex (iCode, pc + 2);
                            sw.WriteLine (tname + " " + callType + " " + isNew + " " + indexReg + " " + line);
                            pc += 4;
                            break;
                        }


                    case Token.CATCH_SCOPE: {
                            bool afterFisrtFlag = (iCode [pc] != 0);
                            sw.WriteLine (tname + " " + afterFisrtFlag);
                            ++pc;
                        }
                        break;

                    case Token.REGEXP:
                        sw.WriteLine (tname + " " + idata.itsRegExpLiterals [indexReg]);
                        break;

                    case Token.OBJECTLIT:
                    case Icode_SPARE_ARRAYLIT:
                        sw.WriteLine (tname + " " + idata.literalIds [indexReg]);
                        break;

                    case Icode_CLOSURE_EXPR:
                    case Icode_CLOSURE_STMT:
                        sw.WriteLine (tname + " " + idata.itsNestedFunctions [indexReg]);
                        break;

                    case Token.CALL:
                    case Icode_TAIL_CALL:
                    case Token.REF_CALL:
                    case Token.NEW:
                        sw.WriteLine (tname + ' ' + indexReg);
                        break;

                    case Token.THROW: {
                            int line = GetIndex (iCode, pc);
                            sw.WriteLine (tname + " : " + line);
                            pc += 2;
                            break;
                        }

                    case Icode_SHORTNUMBER: {
                            int value = GetShort (iCode, pc);
                            sw.WriteLine (tname + " " + value);
                            pc += 2;
                            break;
                        }

                    case Icode_INTNUMBER: {
                            int value = GetInt (iCode, pc);
                            sw.WriteLine (tname + " " + value);
                            pc += 4;
                            break;
                        }

                    case Token.NUMBER: {
                            double value = idata.itsDoubleTable [indexReg];
                            sw.WriteLine (tname + " " + value);
                            pc += 2;
                            break;
                        }

                    case Icode_LINE: {
                            int line = GetIndex (iCode, pc);
                            sw.WriteLine (tname + " : " + line);
                            pc += 2;
                            break;
                        }

                    case Icode_REG_STR1: {
                            string str = strings [0xFF & iCode [pc]];
                            sw.WriteLine (tname + " \"" + str + '"');
                            ++pc;
                            break;
                        }

                    case Icode_REG_STR2: {
                            string str = strings [GetIndex (iCode, pc)];
                            sw.WriteLine (tname + " \"" + str + '"');
                            pc += 2;
                            break;
                        }

                    case Icode_REG_STR4: {
                            string str = strings [GetInt (iCode, pc)];
                            sw.WriteLine (tname + " \"" + str + '"');
                            pc += 4;
                            break;
                        }

                    case Icode_REG_IND1: {
                            indexReg = 0xFF & iCode [pc];
                            sw.WriteLine (tname + " " + indexReg);
                            ++pc;
                            break;
                        }

                    case Icode_REG_IND2: {
                            indexReg = GetIndex (iCode, pc);
                            sw.WriteLine (tname + " " + indexReg);
                            pc += 2;
                            break;
                        }

                    case Icode_REG_IND4: {
                            indexReg = GetInt (iCode, pc);
                            sw.WriteLine (tname + " " + indexReg);
                            pc += 4;
                            break;
                        }

                    case Icode_GETVAR1:
                    case Icode_SETVAR1:
                        indexReg = iCode [pc];
                        sw.WriteLine (tname + " " + indexReg);
                        ++pc;
                        break;
                }
                if (old_pc + icodeLength != pc)
                    Context.CodeBug ();
            }

            int [] table = idata.itsExceptionTable;
            if (table != null) {
                sw.WriteLine ("Exception handlers: " + table.Length / EXCEPTION_SLOT_SIZE);
                for (int i = 0; i != table.Length; i += EXCEPTION_SLOT_SIZE) {
                    int tryStart = table [i + EXCEPTION_TRY_START_SLOT];
                    int tryEnd = table [i + EXCEPTION_TRY_END_SLOT];
                    int handlerStart = table [i + EXCEPTION_HANDLER_SLOT];
                    int type = table [i + EXCEPTION_TYPE_SLOT];
                    int exceptionLocal = table [i + EXCEPTION_LOCAL_SLOT];
                    int scopeLocal = table [i + EXCEPTION_SCOPE_SLOT];

                    sw.WriteLine (" tryStart=" + tryStart + " tryEnd=" + tryEnd + " handlerStart=" + handlerStart + " type=" + (type == 0 ? "catch" : "finally") + " exceptionLocal=" + exceptionLocal);
                }
            }
            sw.Flush ();
        }

        static int bytecodeSpan (int bytecode)
        {
            switch (bytecode) {

                case Token.THROW:
                    // source line
                    return 1 + 2;


                case Icode_GOSUB:
                case Token.GOTO:
                case Token.IFEQ:
                case Token.IFNE:
                case Icode_IFEQ_POP:
                case Icode_LEAVEDQ:
                    // target pc offset
                    return 1 + 2;


                case Icode_CALLSPECIAL:
                    // call type
                    // is new
                    // line number
                    return 1 + 1 + 1 + 2;


                case Token.CATCH_SCOPE:
                    // scope flag
                    return 1 + 1;


                case Icode_VAR_INC_DEC:
                case Icode_NAME_INC_DEC:
                case Icode_PROP_INC_DEC:
                case Icode_ELEM_INC_DEC:
                case Icode_REF_INC_DEC:
                    // type of ++/--
                    return 1 + 1;


                case Icode_SHORTNUMBER:
                    // short number
                    return 1 + 2;


                case Icode_INTNUMBER:
                    // int number
                    return 1 + 4;


                case Icode_REG_IND1:
                    // ubyte index
                    return 1 + 1;


                case Icode_REG_IND2:
                    // ushort index
                    return 1 + 2;


                case Icode_REG_IND4:
                    // int index
                    return 1 + 4;


                case Icode_REG_STR1:
                    // ubyte string index
                    return 1 + 1;


                case Icode_REG_STR2:
                    // ushort string index
                    return 1 + 2;


                case Icode_REG_STR4:
                    // int string index
                    return 1 + 4;


                case Icode_GETVAR1:
                case Icode_SETVAR1:
                    // byte var index
                    return 1 + 1;


                case Icode_LINE:
                    // line number
                    return 1 + 2;
            }

            if (!validBytecode (bytecode))
                throw Context.CodeBug ();

            return 1;
        }

        internal static int [] getLineNumbers (InterpreterData data)
        {
            UintMap presentLines = new UintMap ();

            sbyte [] iCode = data.itsICode;
            int iCodeLength = iCode.Length;
            for (int pc = 0; pc != iCodeLength; ) {
                int bytecode = iCode [pc];
                int span = bytecodeSpan (bytecode);
                if (bytecode == Icode_LINE) {
                    if (span != 3)
                        Context.CodeBug ();
                    int line = GetIndex (iCode, pc + 1);
                    presentLines.put (line, 0);
                }
                pc += span;
            }

            return presentLines.Keys;
        }

        internal static void captureInterpreterStackInfo (EcmaScriptException ex)
        {
            Context cx = Context.CurrentContext;
            if (cx == null || cx.lastInterpreterFrame == null) {
                // No interpreter invocations
                ex.m_InterpreterStackInfo = null;
                ex.m_InterpreterLineData = null;
                return;
            }
            // has interpreter frame on the stack
            CallFrame [] array;
            if (cx.previousInterpreterInvocations == null || cx.previousInterpreterInvocations.size () == 0) {
                array = new CallFrame [1];
            }
            else {
                int previousCount = cx.previousInterpreterInvocations.size ();
                if (cx.previousInterpreterInvocations.peek () == cx.lastInterpreterFrame) {
                    // It can happen if exception was generated after
                    // frame was pushed to cx.previousInterpreterInvocations
                    // but before assignment to cx.lastInterpreterFrame.
                    // In this case frames has to be ignored.
                    --previousCount;
                }
                array = new CallFrame [previousCount + 1];
                cx.previousInterpreterInvocations.ToArray (array);
            }
            array [array.Length - 1] = (CallFrame)cx.lastInterpreterFrame;

            int interpreterFrameCount = 0;
            for (int i = 0; i != array.Length; ++i) {
                interpreterFrameCount += 1 + array [i].frameIndex;
            }

            int [] linePC = new int [interpreterFrameCount];
            // Fill linePC with pc positions from all interpreter frames.
            // Start from the most nested frame
            int linePCIndex = interpreterFrameCount;
            for (int i = array.Length; i != 0; ) {
                --i;
                CallFrame frame = array [i];
                while (frame != null) {
                    --linePCIndex;
                    linePC [linePCIndex] = frame.pcSourceLineStart;
                    frame = frame.parentFrame;
                }
            }
            if (linePCIndex != 0)
                Context.CodeBug ();

            ex.m_InterpreterStackInfo = array;
            ex.m_InterpreterLineData = linePC;
        }

        internal static string GetSourcePositionFromStack (Context cx, int [] linep)
        {
            CallFrame frame = (CallFrame)cx.lastInterpreterFrame;
            InterpreterData idata = frame.idata;
            if (frame.pcSourceLineStart >= 0) {
                linep [0] = GetIndex (idata.itsICode, frame.pcSourceLineStart);
            }
            else {
                linep [0] = 0;
            }
            return idata.itsSourceFile;
        }


        internal static string GetStack (EcmaScriptException ex)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder ();

            CallFrame [] array = (CallFrame [])ex.m_InterpreterStackInfo;
            if (array == null) // TODO: When does this happen?
                return sb.ToString ();

            int [] linePC = ex.m_InterpreterLineData;
            int arrayIndex = array.Length;
            int linePCIndex = linePC.Length;

            while (arrayIndex != 0) {
                --arrayIndex;

                CallFrame frame = array [arrayIndex];
                while (frame != null) {
                    if (linePCIndex == 0)
                        Context.CodeBug ();
                    --linePCIndex;
                    InterpreterData idata = frame.idata;

                    if (sb.Length > 0)
                        sb.Append (Environment.NewLine);
                    sb.Append ("\tat script");
                    if (idata.itsName != null && idata.itsName.Length != 0) {
                        sb.Append ('.');
                        sb.Append (idata.itsName);
                    }
                    sb.Append ('(');
                    sb.Append (idata.itsSourceFile);
                    int pc = linePC [linePCIndex];
                    if (pc >= 0) {
                        // Include line info only if available
                        sb.Append (':');
                        sb.Append (GetIndex (idata.itsICode, pc));
                    }
                    sb.Append (')');


                    frame = frame.parentFrame;


                }
            }

            return sb.ToString ();
        }

        internal static string getPatchedStack (EcmaScriptException ex, string nativeStackTrace)
        {
            string tag = "EcmaScript.NET.Interpreter.interpretLoop";
            System.Text.StringBuilder sb = new System.Text.StringBuilder (nativeStackTrace.Length + 1000);
            string lineSeparator = System.Environment.NewLine;

            CallFrame [] array = (CallFrame [])ex.m_InterpreterStackInfo;
            if (array == null) // TODO: when does this happen?
                return sb.ToString ();

            int [] linePC = ex.m_InterpreterLineData;
            int arrayIndex = array.Length;
            int linePCIndex = linePC.Length;
            int offset = 0;
            while (arrayIndex != 0) {
                --arrayIndex;
                int pos = nativeStackTrace.IndexOf (tag, offset);
                if (pos < 0) {
                    break;
                }

                // Skip tag length
                pos += tag.Length;
                // Skip until the end of line
                for (; pos != nativeStackTrace.Length; ++pos) {
                    char c = nativeStackTrace [pos];
                    if (c == '\n' || c == '\r') {
                        break;
                    }
                }
                sb.Append (nativeStackTrace.Substring (offset, (pos) - (offset)));
                offset = pos;

                CallFrame frame = array [arrayIndex];
                while (frame != null) {
                    if (linePCIndex == 0)
                        Context.CodeBug ();
                    --linePCIndex;
                    InterpreterData idata = frame.idata;
                    sb.Append (lineSeparator);
                    sb.Append ("\tat script");
                    if (idata.itsName != null && idata.itsName.Length != 0) {
                        sb.Append ('.');
                        sb.Append (idata.itsName);
                    }
                    sb.Append ('(');
                    sb.Append (idata.itsSourceFile);
                    int pc = linePC [linePCIndex];
                    if (pc >= 0) {
                        // Include line info only if available
                        sb.Append (':');
                        sb.Append (GetIndex (idata.itsICode, pc));
                    }
                    sb.Append (')');
                    frame = frame.parentFrame;
                }
            }
            sb.Append (nativeStackTrace.Substring (offset));

            return sb.ToString ();
        }

        internal static string GetEncodedSource (InterpreterData idata)
        {
            if (idata.encodedSource == null) {
                return null;
            }
            return idata.encodedSource.Substring (idata.encodedSourceStart, (idata.encodedSourceEnd) - (idata.encodedSourceStart));
        }

        static void initFunction (Context cx, IScriptable scope, InterpretedFunction parent, int index)
        {
            InterpretedFunction fn;
            fn = InterpretedFunction.createFunction (cx, scope, parent, index);
            ScriptRuntime.initFunction (cx, scope, fn, fn.idata.itsFunctionType, parent.idata.evalScriptFlag);
        }

        internal static object Interpret (InterpretedFunction ifun, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            using (Helpers.StackOverflowVerifier sov = new Helpers.StackOverflowVerifier (128)) {
                if (!ScriptRuntime.hasTopCall (cx))
                    Context.CodeBug ();

                if (cx.interpreterSecurityDomain != ifun.securityDomain) {
                    object savedDomain = cx.interpreterSecurityDomain;
                    cx.interpreterSecurityDomain = ifun.securityDomain;
                    try {
                        return ifun.securityController.callWithDomain (ifun.securityDomain, cx, ifun, scope, thisObj, args);
                    }
                    finally {
                        cx.interpreterSecurityDomain = savedDomain;
                    }
                }

                CallFrame frame = new CallFrame ();
                initFrame (cx, scope, thisObj, args, null, 0, args.Length, ifun, null, frame);

                return InterpretLoop (cx, frame, (object)null);
            }
        }

        public static object restartContinuation (Continuation c, Context cx, IScriptable scope, object [] args)
        {
            if (!ScriptRuntime.hasTopCall (cx)) {
                return ScriptRuntime.DoTopCall (c, cx, scope, null, args);
            }

            object arg;
            if (args.Length == 0) {
                arg = Undefined.Value;
            }
            else {
                arg = args [0];
            }

            CallFrame capturedFrame = (CallFrame)c.Implementation;
            if (capturedFrame == null) {
                // No frames to restart
                return arg;
            }

            ContinuationJump cjump = new ContinuationJump (c, null);

            cjump.result = arg;
            return InterpretLoop (cx, null, cjump);
        }

        static object InterpretLoop (Context cx, CallFrame frame, object throwable)
        {
            // throwable holds exception object to rethrow or catch
            // It is also used for continuation restart in which case
            // it holds ContinuationJump

            object DBL_MRK = UniqueTag.DoubleMark;
            object undefined = Undefined.Value;

            bool instructionCounting = (cx.instructionThreshold != 0);
            // arbitrary number to add to instructionCount when calling
            // other functions			
            const int INVOCATION_COST = 100;

            // arbitrary exception cost for instruction counting			
            const int EXCEPTION_COST = 100;

            string stringReg = null;
            int indexReg = -1;

            if (cx.lastInterpreterFrame != null) {
                // save the top frame from the previous interpreterLoop
                // invocation on the stack
                if (cx.previousInterpreterInvocations == null) {
                    cx.previousInterpreterInvocations = new ObjArray ();
                }
                cx.previousInterpreterInvocations.push (cx.lastInterpreterFrame);
            }

            // When restarting continuation throwable is not null and to jump
            // to the code that rewind continuation state indexReg should be set
            // to -1.
            // With the normal call throable == null and indexReg == -1 allows to
            // catch bugs with using indeReg to access array eleemnts before
            // initializing indexReg.

            if (throwable != null) {
                // Assert assumptions
                if (!(throwable is ContinuationJump)) {
                    // It should be continuation
                    Context.CodeBug ();
                }
            }

            object interpreterResult = null;
            double interpreterResultDbl = 0.0;

            for (; ; ) {

                try {

                    if (throwable != null) {
                        // Recovering from exception, indexReg contains
                        // the index of handler

                        if (indexReg >= 0) {
                            // Normal excepton handler, transfer
                            // control appropriately

                            if (frame.frozen) {
                                // TODO: Deal with exceptios!!!
                                frame = frame.cloneFrozen ();
                            }

                            int [] table = frame.idata.itsExceptionTable;

                            frame.pc = table [indexReg + EXCEPTION_HANDLER_SLOT];
                            if (instructionCounting) {
                                frame.pcPrevBranch = frame.pc;
                            }

                            frame.savedStackTop = frame.emptyStackTop;
                            int scopeLocal = frame.localShift + table [indexReg + EXCEPTION_SCOPE_SLOT];
                            int exLocal = frame.localShift + table [indexReg + EXCEPTION_LOCAL_SLOT];
                            frame.scope = (IScriptable)frame.stack [scopeLocal];
                            frame.stack [exLocal] = throwable;

                            throwable = null;
                        }
                        else {
                            // Continuation restoration
                            ContinuationJump cjump = (ContinuationJump)throwable;

                            // Clear throwable to indicate that execptions are OK
                            throwable = null;

                            if (cjump.branchFrame != frame)
                                Context.CodeBug ();

                            // Check that we have at least one frozen frame
                            // in the case of detached continuation restoration:
                            // unwind code ensure that
                            if (cjump.capturedFrame == null)
                                Context.CodeBug ();

                            // Need to rewind branchFrame, capturedFrame
                            // and all frames in between
                            int rewindCount = cjump.capturedFrame.frameIndex + 1;
                            if (cjump.branchFrame != null) {
                                rewindCount -= cjump.branchFrame.frameIndex;
                            }

                            int enterCount = 0;
                            CallFrame [] enterFrames = null;

                            CallFrame x = cjump.capturedFrame;
                            for (int i = 0; i != rewindCount; ++i) {
                                if (!x.frozen)
                                    Context.CodeBug ();
                                if (isFrameEnterExitRequired (x)) {
                                    if (enterFrames == null) {
                                        // Allocate enough space to store the rest
                                        // of rewind frames in case all of them
                                        // would require to enter
                                        enterFrames = new CallFrame [rewindCount - i];
                                    }
                                    enterFrames [enterCount] = x;
                                    ++enterCount;
                                }
                                x = x.parentFrame;
                            }

                            while (enterCount != 0) {
                                // execute enter: walk enterFrames in the reverse
                                // order since they were stored starting from
                                // the capturedFrame, not branchFrame
                                --enterCount;
                                x = enterFrames [enterCount];
                                EnterFrame (cx, x, ScriptRuntime.EmptyArgs);
                            }

                            // Continuation jump is almost done: capturedFrame
                            // points to the call to the function that captured
                            // continuation, so clone capturedFrame and
                            // emulate return that function with the suplied result
                            frame = cjump.capturedFrame.cloneFrozen ();
                            setCallResult (frame, cjump.result, cjump.resultDbl);
                            // restart the execution
                        }

                        // Should be already cleared
                        if (throwable != null)
                            Context.CodeBug ();
                    }
                    else {
                        if (frame.frozen)
                            Context.CodeBug ();
                    }

                    // Use local variables for constant values in frame
                    // for faster access					
                    object [] stack = frame.stack;
                    double [] sDbl = frame.sDbl;
                    object [] vars = frame.varSource.stack;
                    double [] varDbls = frame.varSource.sDbl;

                    sbyte [] iCode = frame.idata.itsICode;
                    string [] strings = frame.idata.itsStringTable;

                    // Use local for stackTop as well. Since execption handlers
                    // can only exist at statement level where stack is empty,
                    // it is necessary to save/restore stackTop only accross
                    // function calls and normal returns.
                    int stackTop = frame.savedStackTop;

                    // Store new frame in cx which is used for error reporting etc.
                    cx.lastInterpreterFrame = frame;

                    for (; ; ) {

                        // Exception handler assumes that PC is already incremented
                        // pass the instruction start when it searches the
                        // exception handler
                        int op = iCode [frame.pc++];                                                
                        {
                            switch (op) {

                                case Token.THROW: {
                                        object value = stack [stackTop];
                                        if (value == DBL_MRK)
                                            value = sDbl [stackTop];
                                        stackTop--;

                                        int sourceLine = GetIndex (iCode, frame.pc);
                                        throwable = new EcmaScriptThrow (
                                            value, frame.idata.itsSourceFile, sourceLine);
                                        goto withoutExceptions_brk;
                                    }

                                case Token.RETHROW: {
                                        indexReg += frame.localShift;
                                        throwable = stack [indexReg];
                                        break;
                                    }

                                case Token.GE:
                                case Token.LE:
                                case Token.GT:
                                case Token.LT: {
                                        --stackTop;
                                        object rhs = stack [stackTop + 1];
                                        object lhs = stack [stackTop];
                                        bool valBln;
                                        {
                                            {
                                                double rDbl, lDbl;
                                                if (rhs == DBL_MRK) {
                                                    rDbl = sDbl [stackTop + 1];
                                                    lDbl = stack_double (frame, stackTop);
                                                }
                                                else if (lhs == DBL_MRK) {
                                                    rDbl = ScriptConvert.ToNumber (rhs);
                                                    lDbl = sDbl [stackTop];
                                                }
                                                else {

                                                    goto number_compare_brk;
                                                }
                                                switch (op) {

                                                    case Token.GE:
                                                        valBln = (lDbl >= rDbl);

                                                        goto object_compare_brk;

                                                    case Token.LE:
                                                        valBln = (lDbl <= rDbl);

                                                        goto object_compare_brk;

                                                    case Token.GT:
                                                        valBln = (lDbl > rDbl);

                                                        goto object_compare_brk;

                                                    case Token.LT:
                                                        valBln = (lDbl < rDbl);

                                                        goto object_compare_brk;

                                                    default:
                                                        throw Context.CodeBug ();

                                                }
                                            }

                                        number_compare_brk:
                                            ;

                                            switch (op) {

                                                case Token.GE:
                                                    valBln = ScriptRuntime.cmp_LE (rhs, lhs);
                                                    break;

                                                case Token.LE:
                                                    valBln = ScriptRuntime.cmp_LE (lhs, rhs);
                                                    break;

                                                case Token.GT:
                                                    valBln = ScriptRuntime.cmp_LT (rhs, lhs);
                                                    break;

                                                case Token.LT:
                                                    valBln = ScriptRuntime.cmp_LT (lhs, rhs);
                                                    break;

                                                default:
                                                    throw Context.CodeBug ();

                                            }
                                        }

                                    object_compare_brk:
                                        ;

                                        stack [stackTop] = valBln;

                                        goto Loop;
                                    }
                                    goto case Token.IN;

                                case Token.IN:
                                case Token.INSTANCEOF: {
                                        object rhs = stack [stackTop];
                                        if (rhs == DBL_MRK)
                                            rhs = sDbl [stackTop];
                                        --stackTop;
                                        object lhs = stack [stackTop];
                                        if (lhs == DBL_MRK)
                                            lhs = sDbl [stackTop];
                                        bool valBln;
                                        if (op == Token.IN) {
                                            valBln = ScriptRuntime.In (lhs, rhs, cx);
                                        }
                                        else {
                                            valBln = ScriptRuntime.InstanceOf (lhs, rhs, cx);
                                        }
                                        stack [stackTop] = valBln;

                                        goto Loop;
                                    }
                                    goto case Token.EQ;

                                case Token.EQ:
                                case Token.NE: {
                                        --stackTop;
                                        bool valBln;
                                        object rhs = stack [stackTop + 1];
                                        object lhs = stack [stackTop];
                                        if (rhs == DBL_MRK) {
                                            if (lhs == DBL_MRK) {
                                                valBln = (sDbl [stackTop] == sDbl [stackTop + 1]);
                                            }
                                            else {
                                                valBln = ScriptRuntime.eqNumber (sDbl [stackTop + 1], lhs);
                                            }
                                        }
                                        else {
                                            if (lhs == DBL_MRK) {
                                                valBln = ScriptRuntime.eqNumber (sDbl [stackTop], rhs);
                                            }
                                            else {
                                                valBln = ScriptRuntime.eq (lhs, rhs);
                                            }
                                        }
                                        valBln ^= (op == Token.NE);
                                        stack [stackTop] = valBln;

                                        goto Loop;
                                    }
                                    goto case Token.SHEQ;

                                case Token.SHEQ:
                                case Token.SHNE: {
                                        --stackTop;
                                        object rhs = stack [stackTop + 1];
                                        object lhs = stack [stackTop];
                                        bool valBln;
                                        {
                                            double rdbl, ldbl;
                                            if (rhs == DBL_MRK) {
                                                rdbl = sDbl [stackTop + 1];
                                                if (lhs == DBL_MRK) {
                                                    ldbl = sDbl [stackTop];
                                                }
                                                else if (CliHelper.IsNumber (lhs)) {
                                                    ldbl = Convert.ToDouble (lhs);
                                                }
                                                else {
                                                    valBln = false;

                                                    goto shallow_compare_brk;
                                                }
                                            }
                                            else if (lhs == DBL_MRK) {
                                                ldbl = sDbl [stackTop];
                                                if (rhs == DBL_MRK) {
                                                    rdbl = sDbl [stackTop + 1];
                                                }
                                                else if (CliHelper.IsNumber (rhs)) {
                                                    rdbl = Convert.ToDouble (rhs);
                                                }
                                                else {
                                                    valBln = false;

                                                    goto shallow_compare_brk;
                                                }
                                            }
                                            else {
                                                valBln = ScriptRuntime.shallowEq (lhs, rhs);

                                                goto shallow_compare_brk;
                                            }
                                            valBln = (ldbl == rdbl);
                                        }

                                    shallow_compare_brk:
                                        ;

                                        valBln ^= (op == Token.SHNE);
                                        stack [stackTop] = valBln;

                                        goto Loop;
                                    }
                                    goto case Token.IFNE;

                                case Token.IFNE:
                                    if (stack_boolean (frame, stackTop--)) {
                                        frame.pc += 2;

                                        goto Loop;
                                    }

                                    goto jumplessRun_brk;

                                case Token.IFEQ:
                                    if (!stack_boolean (frame, stackTop--)) {
                                        frame.pc += 2;

                                        goto Loop;
                                    }

                                    goto jumplessRun_brk;

                                case Icode_IFEQ_POP:
                                    if (!stack_boolean (frame, stackTop--)) {
                                        frame.pc += 2;

                                        goto Loop;
                                    }
                                    stack [stackTop--] = null;

                                    goto jumplessRun_brk;

                                case Token.GOTO:

                                    goto jumplessRun_brk;

                                case Icode_GOSUB:
                                    ++stackTop;
                                    stack [stackTop] = DBL_MRK;
                                    sDbl [stackTop] = frame.pc + 2;

                                    goto jumplessRun_brk;

                                case Icode_STARTSUB:
                                    if (stackTop == frame.emptyStackTop + 1) {
                                        // Call from Icode_GOSUB: store return PC address in the local
                                        indexReg += frame.localShift;
                                        stack [indexReg] = stack [stackTop];
                                        sDbl [indexReg] = sDbl [stackTop];
                                        --stackTop;
                                    }
                                    else {
                                        // Call from exception handler: exception object is already stored
                                        // in the local
                                        if (stackTop != frame.emptyStackTop)
                                            Context.CodeBug ();
                                    }

                                    goto Loop;
                                    goto case Icode_RETSUB;

                                case Icode_RETSUB: {
                                        // indexReg: local to store return address
                                        if (instructionCounting) {
                                            addInstructionCount (cx, frame, 0);
                                        }
                                        indexReg += frame.localShift;
                                        object value = stack [indexReg];
                                        if (value != DBL_MRK) {
                                            // Invocation from exception handler, restore object to rethrow
                                            throwable = value;
                                            goto withoutExceptions_brk;
                                        }
                                        // Normal return from GOSUB									
                                        frame.pc = (int)sDbl [indexReg];
                                        if (instructionCounting) {
                                            frame.pcPrevBranch = frame.pc;
                                        }

                                        goto Loop;
                                    }
                                    goto case Icode_POP;

                                case Icode_POP:
                                    stack [stackTop] = null;
                                    stackTop--;

                                    goto Loop;
                                    goto case Icode_POP_RESULT;

                                case Icode_POP_RESULT:
                                    frame.result = stack [stackTop];
                                    frame.resultDbl = sDbl [stackTop];
                                    stack [stackTop] = null;
                                    --stackTop;

                                    goto Loop;
                                    goto case Icode_DUP;

                                case Icode_DUP:
                                    stack [stackTop + 1] = stack [stackTop];
                                    sDbl [stackTop + 1] = sDbl [stackTop];
                                    stackTop++;

                                    goto Loop;
                                    goto case Icode_DUP2;

                                case Icode_DUP2:
                                    stack [stackTop + 1] = stack [stackTop - 1];
                                    sDbl [stackTop + 1] = sDbl [stackTop - 1];
                                    stack [stackTop + 2] = stack [stackTop];
                                    sDbl [stackTop + 2] = sDbl [stackTop];
                                    stackTop += 2;

                                    goto Loop;
                                    goto case Icode_SWAP;

                                case Icode_SWAP: {
                                        object o = stack [stackTop];
                                        stack [stackTop] = stack [stackTop - 1];
                                        stack [stackTop - 1] = o;
                                        double d = sDbl [stackTop];
                                        sDbl [stackTop] = sDbl [stackTop - 1];
                                        sDbl [stackTop - 1] = d;

                                        goto Loop;
                                    }
                                    goto case Token.RETURN;

                                case Token.RETURN:
                                    frame.result = stack [stackTop];
                                    frame.resultDbl = sDbl [stackTop];
                                    --stackTop;

                                    goto Loop_brk;

                                case Token.RETURN_RESULT:

                                    goto Loop_brk;

                                case Icode_RETUNDEF:
                                    frame.result = undefined;

                                    goto Loop_brk;

                                case Token.BITNOT: {
                                        int rIntValue = stack_int32 (frame, stackTop);
                                        stack [stackTop] = DBL_MRK;
                                        sDbl [stackTop] = ~rIntValue;

                                        goto Loop;
                                    }
                                    goto case Token.BITAND;

                                case Token.BITAND:
                                case Token.BITOR:
                                case Token.BITXOR:
                                case Token.LSH:
                                case Token.RSH: {
                                        int rIntValue = stack_int32 (frame, stackTop);
                                        --stackTop;
                                        int lIntValue = stack_int32 (frame, stackTop);
                                        stack [stackTop] = DBL_MRK;
                                        switch (op) {

                                            case Token.BITAND:
                                                lIntValue &= rIntValue;
                                                break;

                                            case Token.BITOR:
                                                lIntValue |= rIntValue;
                                                break;

                                            case Token.BITXOR:
                                                lIntValue ^= rIntValue;
                                                break;

                                            case Token.LSH:
                                                lIntValue <<= rIntValue;
                                                break;

                                            case Token.RSH:
                                                lIntValue >>= rIntValue;
                                                break;
                                        }
                                        sDbl [stackTop] = lIntValue;

                                        goto Loop;
                                    }
                                    goto case Token.URSH;

                                case Token.URSH: {
                                        int rIntValue = stack_int32 (frame, stackTop) & 0x1F;
                                        --stackTop;
                                        double lDbl = stack_double (frame, stackTop);
                                        stack [stackTop] = DBL_MRK;
                                        uint i = (uint)ScriptConvert.ToUint32 (lDbl);
                                        sDbl [stackTop] = i >> rIntValue;

                                        goto Loop;
                                    }
                                    goto case Token.NEG;

                                case Token.NEG:
                                case Token.POS: {
                                        double rDbl = stack_double (frame, stackTop);
                                        stack [stackTop] = DBL_MRK;
                                        if (op == Token.NEG) {
                                            rDbl = -rDbl;
                                        }
                                        sDbl [stackTop] = rDbl;

                                        goto Loop;
                                    }
                                    goto case Token.ADD;

                                case Token.ADD:
                                    --stackTop;
                                    DoAdd (stack, sDbl, stackTop, cx);

                                    goto Loop;
                                    goto case Token.SUB;

                                case Token.SUB:
                                case Token.MUL:
                                case Token.DIV:
                                case Token.MOD: {
                                        double rDbl = stack_double (frame, stackTop);
                                        --stackTop;
                                        double lDbl = stack_double (frame, stackTop);
                                        stack [stackTop] = DBL_MRK;
                                        switch (op) {

                                            case Token.SUB:
                                                lDbl -= rDbl;
                                                break;

                                            case Token.MUL:
                                                lDbl *= rDbl;
                                                break;

                                            case Token.DIV:
                                                lDbl /= rDbl;
                                                break;

                                            case Token.MOD:
                                                lDbl %= rDbl;
                                                break;
                                        }
                                        sDbl [stackTop] = lDbl;

                                        goto Loop;
                                    }
                                    goto case Token.NOT;

                                case Token.NOT:
                                    stack [stackTop] = !stack_boolean (frame, stackTop);

                                    goto Loop;
                                    goto case Token.BINDNAME;

                                case Token.BINDNAME:
                                    stack [++stackTop] = ScriptRuntime.bind (cx, frame.scope, stringReg);

                                    goto Loop;
                                    goto case Token.SETNAME;

                                case Token.SETNAME: {
                                        object rhs = stack [stackTop];
                                        if (rhs == DBL_MRK)
                                            rhs = sDbl [stackTop];
                                        --stackTop;
                                        IScriptable lhs = (IScriptable)stack [stackTop];
                                        stack [stackTop] = ScriptRuntime.setName (lhs, rhs, cx, frame.scope, stringReg);

                                        goto Loop;
                                    }
                                    goto case Token.DELPROP;

                                case Token.DELPROP: {
                                        object rhs = stack [stackTop];
                                        if (rhs == DBL_MRK)
                                            rhs = sDbl [stackTop];
                                        --stackTop;
                                        object lhs = stack [stackTop];
                                        if (lhs == DBL_MRK)
                                            lhs = sDbl [stackTop];
                                        stack [stackTop] = ScriptRuntime.delete (lhs, rhs, cx);

                                        goto Loop;
                                    }
                                    goto case Token.GETPROP;

                                case Token.GETPROP: {
                                        object lhs = stack [stackTop];
                                        if (lhs == DBL_MRK)
                                            lhs = sDbl [stackTop];
                                        stack [stackTop] = ScriptRuntime.getObjectProp (lhs, stringReg, cx);

                                        goto Loop;
                                    }
                                    goto case Token.SETPROP;

                                case Token.SETPROP_GETTER:
                                case Token.SETPROP_SETTER:
                                case Token.SETPROP: {
                                        object rhs = stack [stackTop];
                                        if (rhs == DBL_MRK)
                                            rhs = sDbl [stackTop];
                                        --stackTop;
                                        object lhs = stack [stackTop];
                                        if (lhs == DBL_MRK)
                                            lhs = sDbl [stackTop];

                                        switch (op) {
                                            case Token.SETPROP_GETTER:
                                                ((ScriptableObject)lhs).DefineGetter(stringReg, ((ICallable)rhs));
                                                stack[stackTop] = rhs;
                                                break;

                                            case Token.SETPROP_SETTER:
                                                ((ScriptableObject)lhs).DefineSetter(stringReg, ((ICallable)rhs));
                                                stack[stackTop] = rhs;
                                                break;


                                            default:
                                                stack [stackTop] = ScriptRuntime.setObjectProp (lhs, stringReg, rhs, cx);
                                                break;
                                        }

                                        goto Loop;
                                    }
                                    goto case Icode_PROP_INC_DEC;

                                case Icode_PROP_INC_DEC: {
                                        object lhs = stack [stackTop];
                                        if (lhs == DBL_MRK)
                                            lhs = sDbl [stackTop];
                                        stack [stackTop] = ScriptRuntime.propIncrDecr (lhs, stringReg, cx, iCode [frame.pc]);
                                        ++frame.pc;

                                        goto Loop;
                                    }
                                    goto case Token.GETELEM;

                                case Token.GETELEM: {
                                        --stackTop;
                                        object lhs = stack [stackTop];
                                        if (lhs == DBL_MRK) {
                                            lhs = sDbl [stackTop];
                                        }
                                        object value;
                                        object id = stack [stackTop + 1];
                                        if (id != DBL_MRK) {
                                            value = ScriptRuntime.getObjectElem (lhs, id, cx);
                                        }
                                        else {
                                            double d = sDbl [stackTop + 1];
                                            value = ScriptRuntime.getObjectIndex (lhs, d, cx);
                                        }
                                        stack [stackTop] = value;

                                        goto Loop;
                                    }
                                    goto case Token.SETELEM;

                                case Token.SETELEM: {
                                        stackTop -= 2;
                                        object rhs = stack [stackTop + 2];
                                        if (rhs == DBL_MRK) {
                                            rhs = sDbl [stackTop + 2];
                                        }
                                        object lhs = stack [stackTop];
                                        if (lhs == DBL_MRK) {
                                            lhs = sDbl [stackTop];
                                        }
                                        object value;
                                        object id = stack [stackTop + 1];
                                        if (id != DBL_MRK) {
                                            value = ScriptRuntime.setObjectElem (lhs, id, rhs, cx);
                                        }
                                        else {
                                            double d = sDbl [stackTop + 1];
                                            value = ScriptRuntime.setObjectIndex (lhs, d, rhs, cx);
                                        }
                                        stack [stackTop] = value;

                                        goto Loop;
                                    }
                                    goto case Icode_ELEM_INC_DEC;

                                case Icode_ELEM_INC_DEC: {
                                        object rhs = stack [stackTop];
                                        if (rhs == DBL_MRK)
                                            rhs = sDbl [stackTop];
                                        --stackTop;
                                        object lhs = stack [stackTop];
                                        if (lhs == DBL_MRK)
                                            lhs = sDbl [stackTop];
                                        stack [stackTop] = ScriptRuntime.elemIncrDecr (lhs, rhs, cx, iCode [frame.pc]);
                                        ++frame.pc;

                                        goto Loop;
                                    }
                                    goto case Token.GET_REF;

                                case Token.GET_REF: {
                                        IRef rf = (IRef)stack [stackTop];
                                        stack [stackTop] = ScriptRuntime.refGet (rf, cx);

                                        goto Loop;
                                    }
                                    goto case Token.SET_REF;

                                case Token.SET_REF: {
                                        object value = stack [stackTop];
                                        if (value == DBL_MRK)
                                            value = sDbl [stackTop];
                                        --stackTop;
                                        IRef rf = (IRef)stack [stackTop];
                                        stack [stackTop] = ScriptRuntime.refSet (rf, value, cx);

                                        goto Loop;
                                    }
                                    goto case Token.DEL_REF;

                                case Token.DEL_REF: {
                                        IRef rf = (IRef)stack [stackTop];
                                        stack [stackTop] = ScriptRuntime.refDel (rf, cx);

                                        goto Loop;
                                    }
                                    goto case Icode_REF_INC_DEC;

                                case Icode_REF_INC_DEC: {
                                        IRef rf = (IRef)stack [stackTop];
                                        stack [stackTop] = ScriptRuntime.refIncrDecr (rf, cx, iCode [frame.pc]);
                                        ++frame.pc;

                                        goto Loop;
                                    }
                                    goto case Token.LOCAL_LOAD;

                                case Token.LOCAL_LOAD:
                                    ++stackTop;
                                    indexReg += frame.localShift;
                                    stack [stackTop] = stack [indexReg];
                                    sDbl [stackTop] = sDbl [indexReg];

                                    goto Loop;
                                    goto case Icode_LOCAL_CLEAR;

                                case Icode_LOCAL_CLEAR:
                                    indexReg += frame.localShift;
                                    stack [indexReg] = null;

                                    goto Loop;
                                    goto case Icode_NAME_AND_THIS;

                                case Icode_NAME_AND_THIS:
                                    // stringReg: name
                                    ++stackTop;
                                    stack [stackTop] = ScriptRuntime.getNameFunctionAndThis (stringReg, cx, frame.scope);
                                    ++stackTop;
                                    stack [stackTop] = ScriptRuntime.lastStoredScriptable (cx);

                                    goto Loop;
                                    goto case Icode_PROP_AND_THIS;

                                case Icode_PROP_AND_THIS: {                                        
                                        object obj = stack [stackTop];                                        
                                        if (obj == DBL_MRK)
                                            obj = sDbl [stackTop];                                        
                                        // stringReg: property
                                        stack [stackTop] = ScriptRuntime.getPropFunctionAndThis (obj, stringReg, cx);
                                        ++stackTop;
                                        stack [stackTop] = ScriptRuntime.lastStoredScriptable (cx);

                                        goto Loop;
                                    }
                                    goto case Icode_ELEM_AND_THIS;

                                case Icode_ELEM_AND_THIS: {
                                        object obj = stack [stackTop - 1];
                                        if (obj == DBL_MRK)
                                            obj = sDbl [stackTop - 1];
                                        object id = stack [stackTop];
                                        if (id == DBL_MRK)
                                            id = sDbl [stackTop];
                                        stack [stackTop - 1] = ScriptRuntime.GetElemFunctionAndThis (obj, id, cx);
                                        stack [stackTop] = ScriptRuntime.lastStoredScriptable (cx);

                                        goto Loop;
                                    }
                                    goto case Icode_VALUE_AND_THIS;

                                case Icode_VALUE_AND_THIS: {
                                        object value = stack [stackTop];
                                        if (value == DBL_MRK)
                                            value = sDbl [stackTop];
                                        stack [stackTop] = ScriptRuntime.getValueFunctionAndThis (value, cx);
                                        ++stackTop;
                                        stack [stackTop] = ScriptRuntime.lastStoredScriptable (cx);

                                        goto Loop;
                                    }
                                    goto case Icode_CALLSPECIAL;

                                case Icode_CALLSPECIAL: {
                                        if (instructionCounting) {
                                            cx.instructionCount += INVOCATION_COST;
                                        }
                                        int callType = iCode [frame.pc] & 0xFF;
                                        bool isNew = (iCode [frame.pc + 1] != 0);
                                        int sourceLine = GetIndex (iCode, frame.pc + 2);

                                        // indexReg: number of arguments
                                        if (isNew) {
                                            // stack change: function arg0 .. argN -> newResult
                                            stackTop -= indexReg;

                                            object function = stack [stackTop];
                                            if (function == DBL_MRK)
                                                function = sDbl [stackTop];
                                            object [] outArgs = GetArgsArray (stack, sDbl, stackTop + 1, indexReg);
                                            stack [stackTop] = ScriptRuntime.newSpecial (cx, function, outArgs, frame.scope, callType);
                                        }
                                        else {
                                            // stack change: function thisObj arg0 .. argN -> result
                                            stackTop -= (1 + indexReg);

                                            // Call code generation ensure that stack here
                                            // is ... Callable Scriptable
                                            IScriptable functionThis = (IScriptable)stack [stackTop + 1];
                                            ICallable function = (ICallable)stack [stackTop];
                                            object [] outArgs = GetArgsArray (stack, sDbl, stackTop + 2, indexReg);
                                            stack [stackTop] = ScriptRuntime.callSpecial (cx, function, functionThis, outArgs, frame.scope, frame.thisObj, callType, frame.idata.itsSourceFile, sourceLine);
                                        }
                                        frame.pc += 4;

                                        goto Loop;
                                    }
                                    goto case Token.CALL;

                                case Token.CALL:
                                case Icode_TAIL_CALL:
                                case Token.REF_CALL: {
                                        if (instructionCounting) {
                                            cx.instructionCount += INVOCATION_COST;
                                        }
                                        // stack change: function thisObj arg0 .. argN -> result
                                        // indexReg: number of arguments
                                        stackTop -= (1 + indexReg);

                                        // CALL generation ensures that fun and funThisObj
                                        // are already Scriptable and Callable objects respectively
                                        ICallable fun = (ICallable)stack [stackTop];
                                        IScriptable funThisObj = (IScriptable)stack [stackTop + 1];
                                        if (op == Token.REF_CALL) {
                                            object [] outArgs = GetArgsArray (stack, sDbl, stackTop + 2, indexReg);
                                            stack [stackTop] = ScriptRuntime.callRef (fun, funThisObj, outArgs, cx);

                                            goto Loop;
                                        }
                                        IScriptable calleeScope = frame.scope;
                                        if (frame.useActivation) {
                                            calleeScope = ScriptableObject.GetTopLevelScope (frame.scope);
                                        }
                                        if (fun is InterpretedFunction) {
                                            InterpretedFunction ifun = (InterpretedFunction)fun;
                                            if (frame.fnOrScript.securityDomain == ifun.securityDomain) {
                                                CallFrame callParentFrame = frame;
                                                CallFrame calleeFrame = new CallFrame ();
                                                if (op == Icode_TAIL_CALL) {
                                                    // In principle tail call can re-use the current
                                                    // frame and its stack arrays but it is hard to
                                                    // do properly. Any exceptions that can legally
                                                    // happen during frame re-initialization including
                                                    // StackOverflowException during innocent looking
                                                    // System.arraycopy may leave the current frame
                                                    // data corrupted leading to undefined behaviour
                                                    // in the catch code bellow that unwinds JS stack
                                                    // on exceptions. Then there is issue about frame release
                                                    // end exceptions there.
                                                    // To avoid frame allocation a released frame
                                                    // can be cached for re-use which would also benefit
                                                    // non-tail calls but it is not clear that this caching
                                                    // would gain in performance due to potentially
                                                    // bad iteraction with GC.
                                                    callParentFrame = frame.parentFrame;
                                                }
                                                initFrame (cx, calleeScope, funThisObj, stack, sDbl, stackTop + 2, indexReg, ifun, callParentFrame, calleeFrame);
                                                if (op == Icode_TAIL_CALL) {
                                                    // Release the parent
                                                    ExitFrame (cx, frame, (object)null);
                                                }
                                                else {
                                                    frame.savedStackTop = stackTop;
                                                    frame.savedCallOp = op;
                                                }
                                                frame = calleeFrame;

                                                goto StateLoop;
                                            }
                                        }

                                        if (fun is Continuation) {
                                            // Jump to the captured continuation
                                            ContinuationJump cjump;
                                            cjump = new ContinuationJump ((Continuation)fun, frame);

                                            // continuation result is the first argument if any
                                            // of contination call
                                            if (indexReg == 0) {
                                                cjump.result = undefined;
                                            }
                                            else {
                                                cjump.result = stack [stackTop + 2];
                                                cjump.resultDbl = sDbl [stackTop + 2];
                                            }

                                            // Start the real unwind job
                                            throwable = cjump;
                                            break;
                                        }

                                        if (fun is IdFunctionObject) {
                                            IdFunctionObject ifun = (IdFunctionObject)fun;
                                            if (Continuation.IsContinuationConstructor (ifun)) {
                                                captureContinuation (cx, frame, stackTop);

                                                goto Loop;
                                            }
                                        }

                                        object [] outArgs2 = GetArgsArray (stack, sDbl, stackTop + 2, indexReg);
                                        stack [stackTop] = fun.Call (cx, calleeScope, funThisObj, outArgs2);


                                        goto Loop;
                                    }
                                    goto case Token.NEW;

                                case Token.NEW: {
                                        if (instructionCounting) {
                                            cx.instructionCount += INVOCATION_COST;
                                        }
                                        // stack change: function arg0 .. argN -> newResult
                                        // indexReg: number of arguments
                                        stackTop -= indexReg;

                                        object lhs = stack [stackTop];
                                        if (lhs is InterpretedFunction) {
                                            InterpretedFunction f = (InterpretedFunction)lhs;
                                            if (frame.fnOrScript.securityDomain == f.securityDomain) {
                                                IScriptable newInstance = f.CreateObject (cx, frame.scope);
                                                CallFrame calleeFrame = new CallFrame ();
                                                initFrame (cx, frame.scope, newInstance, stack, sDbl, stackTop + 1, indexReg, f, frame, calleeFrame);

                                                stack [stackTop] = newInstance;
                                                frame.savedStackTop = stackTop;
                                                frame.savedCallOp = op;
                                                frame = calleeFrame;

                                                goto StateLoop;
                                            }
                                        }
                                        if (!(lhs is IFunction)) {
                                            if (lhs == DBL_MRK)
                                                lhs = sDbl [stackTop];
                                            throw ScriptRuntime.NotFunctionError (lhs);
                                        }
                                        IFunction fun = (IFunction)lhs;

                                        if (fun is IdFunctionObject) {
                                            IdFunctionObject ifun = (IdFunctionObject)fun;
                                            if (Continuation.IsContinuationConstructor (ifun)) {
                                                captureContinuation (cx, frame, stackTop);

                                                goto Loop;
                                            }
                                        }

                                        object [] outArgs = GetArgsArray (stack, sDbl, stackTop + 1, indexReg);
                                        stack [stackTop] = fun.Construct (cx, frame.scope, outArgs);

                                        goto Loop;
                                    }
                                    goto case Token.TYPEOF;

                                case Token.TYPEOF: {
                                        object lhs = stack [stackTop];
                                        if (lhs == DBL_MRK)
                                            lhs = sDbl [stackTop];
                                        stack [stackTop] = ScriptRuntime.Typeof (lhs);

                                        goto Loop;
                                    }
                                    goto case Icode_TYPEOFNAME;

                                case Icode_TYPEOFNAME:
                                    stack [++stackTop] = ScriptRuntime.TypeofName (frame.scope, stringReg);

                                    goto Loop;
                                    goto case Token.STRING;

                                case Token.STRING:
                                    stack [++stackTop] = stringReg;

                                    goto Loop;
                                    goto case Icode_SHORTNUMBER;

                                case Icode_SHORTNUMBER:
                                    ++stackTop;
                                    stack [stackTop] = DBL_MRK;
                                    sDbl [stackTop] = GetShort (iCode, frame.pc);
                                    frame.pc += 2;

                                    goto Loop;
                                    goto case Icode_INTNUMBER;

                                case Icode_INTNUMBER:
                                    ++stackTop;
                                    stack [stackTop] = DBL_MRK;
                                    sDbl [stackTop] = GetInt (iCode, frame.pc);
                                    frame.pc += 4;

                                    goto Loop;
                                    goto case Token.NUMBER;

                                case Token.NUMBER:
                                    ++stackTop;
                                    stack [stackTop] = DBL_MRK;
                                    sDbl [stackTop] = frame.idata.itsDoubleTable [indexReg];

                                    goto Loop;
                                    goto case Token.NAME;

                                case Token.NAME:
                                    stack [++stackTop] = ScriptRuntime.name (cx, frame.scope, stringReg);

                                    goto Loop;
                                    goto case Icode_NAME_INC_DEC;

                                case Icode_NAME_INC_DEC:
                                    stack [++stackTop] = ScriptRuntime.nameIncrDecr (frame.scope, stringReg, iCode [frame.pc]);
                                    ++frame.pc;

                                    goto Loop;
                                    goto case Icode_SETVAR1;

                                case Icode_SETVAR1:
                                    indexReg = iCode [frame.pc++];
                                    // fallthrough
                                    goto case Token.SETVAR;

                                case Token.SETVAR:
                                    if (!frame.useActivation) {
                                        vars [indexReg] = stack [stackTop];
                                        varDbls [indexReg] = sDbl [stackTop];
                                    }
                                    else {
                                        object val = stack [stackTop];
                                        if (val == DBL_MRK)
                                            val = sDbl [stackTop];
                                        stringReg = frame.idata.argNames [indexReg];
                                        frame.scope.Put (stringReg, frame.scope, val);
                                    }

                                    goto Loop;
                                    goto case Icode_GETVAR1;

                                case Icode_GETVAR1:
                                    indexReg = iCode [frame.pc++];
                                    // fallthrough
                                    goto case Token.GETVAR;

                                case Token.GETVAR:
                                    ++stackTop;
                                    if (!frame.useActivation) {
                                        stack [stackTop] = vars [indexReg];
                                        sDbl [stackTop] = varDbls [indexReg];
                                    }
                                    else {
                                        stringReg = frame.idata.argNames [indexReg];
                                        stack [stackTop] = frame.scope.Get (stringReg, frame.scope);
                                    }

                                    goto Loop;
                                    goto case Icode_VAR_INC_DEC;

                                case Icode_VAR_INC_DEC: {
                                        // indexReg : varindex
                                        ++stackTop;
                                        int incrDecrMask = iCode [frame.pc];
                                        if (!frame.useActivation) {
                                            stack [stackTop] = DBL_MRK;
                                            object varValue = vars [indexReg];
                                            double d;
                                            if (varValue == DBL_MRK) {
                                                d = varDbls [indexReg];
                                            }
                                            else {
                                                d = ScriptConvert.ToNumber (varValue);
                                                vars [indexReg] = DBL_MRK;
                                            }
                                            double d2 = ((incrDecrMask & Node.DECR_FLAG) == 0) ? d + 1.0 : d - 1.0;
                                            varDbls [indexReg] = d2;
                                            sDbl [stackTop] = ((incrDecrMask & Node.POST_FLAG) == 0) ? d2 : d;
                                        }
                                        else {
                                            string varName = frame.idata.argNames [indexReg];
                                            stack [stackTop] = ScriptRuntime.nameIncrDecr (frame.scope, varName, incrDecrMask);
                                        }
                                        ++frame.pc;

                                        goto Loop;
                                    }
                                    goto case Icode_ZERO;

                                case Icode_ZERO:
                                    ++stackTop;
                                    stack [stackTop] = DBL_MRK;
                                    sDbl [stackTop] = 0;

                                    goto Loop;
                                    goto case Icode_ONE;

                                case Icode_ONE:
                                    ++stackTop;
                                    stack [stackTop] = DBL_MRK;
                                    sDbl [stackTop] = 1;

                                    goto Loop;
                                    goto case Token.NULL;

                                case Token.NULL:
                                    stack [++stackTop] = null;

                                    goto Loop;
                                    goto case Token.THIS;

                                case Token.THIS:
                                    stack [++stackTop] = frame.thisObj;

                                    goto Loop;
                                    goto case Token.THISFN;

                                case Token.THISFN:
                                    stack [++stackTop] = frame.fnOrScript;

                                    goto Loop;
                                    goto case Token.FALSE;

                                case Token.FALSE:
                                    stack [++stackTop] = false;

                                    goto Loop;
                                    goto case Token.TRUE;

                                case Token.TRUE:
                                    stack [++stackTop] = true;

                                    goto Loop;
                                    goto case Icode_UNDEF;

                                case Icode_UNDEF:
                                    stack [++stackTop] = undefined;

                                    goto Loop;
                                    goto case Token.ENTERWITH;

                                case Token.ENTERWITH: {
                                        object lhs = stack [stackTop];
                                        if (lhs == DBL_MRK)
                                            lhs = sDbl [stackTop];
                                        --stackTop;
                                        frame.scope = ScriptRuntime.enterWith (lhs, cx, frame.scope);

                                        goto Loop;
                                    }
                                    goto case Token.LEAVEWITH;

                                case Token.LEAVEWITH:
                                    frame.scope = ScriptRuntime.leaveWith (frame.scope);

                                    goto Loop;
                                    goto case Token.CATCH_SCOPE;

                                case Token.CATCH_SCOPE: {
                                        // stack top: exception object
                                        // stringReg: name of exception variable
                                        // indexReg: local for exception scope
                                        --stackTop;
                                        indexReg += frame.localShift;

                                        bool afterFirstScope = (frame.idata.itsICode [frame.pc] != 0);

                                        Exception caughtException = (Exception)stack [stackTop + 1];
                                        IScriptable lastCatchScope;
                                        if (!afterFirstScope) {
                                            lastCatchScope = null;
                                        }
                                        else {
                                            lastCatchScope = (IScriptable)stack [indexReg];
                                        }
                                        stack [indexReg] = ScriptRuntime.NewCatchScope (caughtException, lastCatchScope, stringReg, cx, frame.scope);
                                        ++frame.pc;

                                        goto Loop;
                                    }
                                    goto case Token.ENUM_INIT_KEYS;

                                case Token.ENUM_INIT_KEYS:
                                case Token.ENUM_INIT_VALUES: {
                                        object lhs = stack [stackTop];
                                        if (lhs == DBL_MRK)
                                            lhs = sDbl [stackTop];
                                        --stackTop;
                                        indexReg += frame.localShift;

                                        if (lhs is IIdEnumerable) {
                                            stack [indexReg] = ((IIdEnumerable)lhs).GetEnumeration (cx, (op == Token.ENUM_INIT_VALUES));
                                        }
                                        else {
                                            stack [indexReg] = new IdEnumeration (lhs, cx, (op == Token.ENUM_INIT_VALUES));
                                        }


                                        goto Loop;
                                    }
                                    goto case Token.ENUM_NEXT;

                                case Token.ENUM_NEXT:
                                case Token.ENUM_ID: {
                                        indexReg += frame.localShift;
                                        IdEnumeration val = (IdEnumeration)stack [indexReg];
                                        ++stackTop;
                                        stack [stackTop] = (op == Token.ENUM_NEXT) ? val.MoveNext () : val.Current (cx);

                                        goto Loop;
                                    }
                                    goto case Token.REF_SPECIAL;

                                case Token.REF_SPECIAL: {
                                        //stringReg: name of special property
                                        object obj = stack [stackTop];
                                        if (obj == DBL_MRK)
                                            obj = sDbl [stackTop];
                                        stack [stackTop] = ScriptRuntime.specialRef (obj, stringReg, cx);

                                        goto Loop;
                                    }
                                    goto case Token.REF_MEMBER;

                                case Token.REF_MEMBER: {
                                        //indexReg: flags
                                        object elem = stack [stackTop];
                                        if (elem == DBL_MRK)
                                            elem = sDbl [stackTop];
                                        --stackTop;
                                        object obj = stack [stackTop];
                                        if (obj == DBL_MRK)
                                            obj = sDbl [stackTop];
                                        stack [stackTop] = ScriptRuntime.memberRef (obj, elem, cx, indexReg);

                                        goto Loop;
                                    }
                                    goto case Token.REF_NS_MEMBER;

                                case Token.REF_NS_MEMBER: {
                                        //indexReg: flags
                                        object elem = stack [stackTop];
                                        if (elem == DBL_MRK)
                                            elem = sDbl [stackTop];
                                        --stackTop;
                                        object ns = stack [stackTop];
                                        if (ns == DBL_MRK)
                                            ns = sDbl [stackTop];
                                        --stackTop;
                                        object obj = stack [stackTop];
                                        if (obj == DBL_MRK)
                                            obj = sDbl [stackTop];
                                        stack [stackTop] = ScriptRuntime.memberRef (obj, ns, elem, cx, indexReg);

                                        goto Loop;
                                    }
                                    goto case Token.REF_NAME;

                                case Token.REF_NAME: {
                                        //indexReg: flags
                                        object name = stack [stackTop];
                                        if (name == DBL_MRK)
                                            name = sDbl [stackTop];
                                        stack [stackTop] = ScriptRuntime.nameRef (name, cx, frame.scope, indexReg);

                                        goto Loop;
                                    }
                                    goto case Token.REF_NS_NAME;

                                case Token.REF_NS_NAME: {
                                        //indexReg: flags
                                        object name = stack [stackTop];
                                        if (name == DBL_MRK)
                                            name = sDbl [stackTop];
                                        --stackTop;
                                        object ns = stack [stackTop];
                                        if (ns == DBL_MRK)
                                            ns = sDbl [stackTop];
                                        stack [stackTop] = ScriptRuntime.nameRef (ns, name, cx, frame.scope, indexReg);

                                        goto Loop;
                                    }
                                    goto case Icode_SCOPE_LOAD;

                                case Icode_SCOPE_LOAD:
                                    indexReg += frame.localShift;
                                    frame.scope = (IScriptable)stack [indexReg];

                                    goto Loop;
                                    goto case Icode_SCOPE_SAVE;

                                case Icode_SCOPE_SAVE:
                                    indexReg += frame.localShift;
                                    stack [indexReg] = frame.scope;

                                    goto Loop;
                                    goto case Icode_CLOSURE_EXPR;

                                case Icode_CLOSURE_EXPR: {
                                        InterpretedFunction fun = InterpretedFunction.createFunction (cx, frame.scope, frame.fnOrScript, indexReg);
                                        stack [++stackTop] = fun;
                                    }
                                    goto Loop;
                                    goto case Icode_CLOSURE_STMT;

                                case Icode_CLOSURE_STMT:
                                    initFunction (cx, frame.scope, frame.fnOrScript, indexReg);

                                    goto Loop;
                                    goto case Token.REGEXP;

                                case Token.REGEXP:
                                    stack [++stackTop] = frame.scriptRegExps [indexReg];

                                    goto Loop;
                                    goto case Icode_LITERAL_NEW;

                                case Icode_LITERAL_NEW:
                                    // indexReg: number of values in the literal
                                    ++stackTop;
                                    stack [stackTop] = new object [indexReg];
                                    sDbl [stackTop] = 0;

                                    goto Loop;
                                    goto case Icode_LITERAL_SET;

                                case Icode_LITERAL_SET: {
                                        object value = stack [stackTop];
                                        if (value == DBL_MRK)
                                            value = sDbl [stackTop];
                                        --stackTop;
                                        int i = (int)sDbl [stackTop];
                                        ((object [])stack [stackTop]) [i] = value;
                                        sDbl [stackTop] = i + 1;

                                        goto Loop;
                                    }
                                    goto case Token.ARRAYLIT;

                                case Token.ARRAYLIT:
                                case Icode_SPARE_ARRAYLIT:
                                case Token.OBJECTLIT: {
                                        object [] data = (object [])stack [stackTop];
                                        object val;
                                        if (op == Token.OBJECTLIT) {
                                            object [] ids = (object [])frame.idata.literalIds [indexReg];
                                            val = ScriptRuntime.newObjectLiteral (ids, data, cx, frame.scope);
                                        }
                                        else {
                                            int [] skipIndexces = null;
                                            if (op == Icode_SPARE_ARRAYLIT) {
                                                skipIndexces = (int [])frame.idata.literalIds [indexReg];
                                            }
                                            val = ScriptRuntime.newArrayLiteral (data, skipIndexces, cx, frame.scope);
                                        }
                                        stack [stackTop] = val;

                                        goto Loop;
                                    }
                                    goto case Icode_ENTERDQ;

                                case Icode_ENTERDQ: {
                                        object lhs = stack [stackTop];
                                        if (lhs == DBL_MRK)
                                            lhs = sDbl [stackTop];
                                        --stackTop;
                                        frame.scope = ScriptRuntime.enterDotQuery (lhs, frame.scope);

                                        goto Loop;
                                    }
                                    goto case Icode_LEAVEDQ;

                                case Icode_LEAVEDQ: {
                                        bool valBln = stack_boolean (frame, stackTop);
                                        object x = ScriptRuntime.updateDotQuery (valBln, frame.scope);
                                        if (x != null) {
                                            stack [stackTop] = x;
                                            frame.scope = ScriptRuntime.leaveDotQuery (frame.scope);
                                            frame.pc += 2;

                                            goto Loop;
                                        }
                                        // reset stack and PC to code after ENTERDQ
                                        --stackTop;

                                        goto jumplessRun_brk;
                                    }

                                case Token.DEFAULTNAMESPACE: {
                                        object value = stack [stackTop];
                                        if (value == DBL_MRK)
                                            value = sDbl [stackTop];
                                        stack [stackTop] = ScriptRuntime.setDefaultNamespace (value, cx);

                                        goto Loop;
                                    }
                                    goto case Token.ESCXMLATTR;

                                case Token.ESCXMLATTR: {
                                        object value = stack [stackTop];
                                        if (value != DBL_MRK) {
                                            stack [stackTop] = ScriptRuntime.escapeAttributeValue (value, cx);
                                        }

                                        goto Loop;
                                    }
                                    goto case Token.ESCXMLTEXT;

                                case Token.ESCXMLTEXT: {
                                        object value = stack [stackTop];
                                        if (value != DBL_MRK) {
                                            stack [stackTop] = ScriptRuntime.escapeTextValue (value, cx);
                                        }

                                        goto Loop;
                                    }
                                    goto case Icode_LINE;

                                case Icode_DEBUGGER: {
                                        if (frame.debuggerFrame != null) {
                                            frame.debuggerFrame.OnDebuggerStatement(cx);
                                        }
                                        break;
                                    }

                                case Icode_LINE:
                                    frame.pcSourceLineStart = frame.pc;
                                    if (frame.debuggerFrame != null) {
                                        int line = GetIndex (iCode, frame.pc);
                                        frame.debuggerFrame.OnLineChange (cx, line);
                                    }
                                    frame.pc += 2;

                                    goto Loop;
                                    goto case Icode_REG_IND_C0;

                                case Icode_REG_IND_C0:
                                    indexReg = 0;

                                    goto Loop;
                                    goto case Icode_REG_IND_C1;

                                case Icode_REG_IND_C1:
                                    indexReg = 1;

                                    goto Loop;
                                    goto case Icode_REG_IND_C2;

                                case Icode_REG_IND_C2:
                                    indexReg = 2;

                                    goto Loop;
                                    goto case Icode_REG_IND_C3;

                                case Icode_REG_IND_C3:
                                    indexReg = 3;

                                    goto Loop;
                                    goto case Icode_REG_IND_C4;

                                case Icode_REG_IND_C4:
                                    indexReg = 4;

                                    goto Loop;
                                    goto case Icode_REG_IND_C5;

                                case Icode_REG_IND_C5:
                                    indexReg = 5;

                                    goto Loop;
                                    goto case Icode_REG_IND1;

                                case Icode_REG_IND1:
                                    indexReg = 0xFF & iCode [frame.pc];
                                    ++frame.pc;

                                    goto Loop;
                                    goto case Icode_REG_IND2;

                                case Icode_REG_IND2:
                                    indexReg = GetIndex (iCode, frame.pc);
                                    frame.pc += 2;

                                    goto Loop;
                                    goto case Icode_REG_IND4;

                                case Icode_REG_IND4:
                                    indexReg = GetInt (iCode, frame.pc);
                                    frame.pc += 4;

                                    goto Loop;
                                    goto case Icode_REG_STR_C0;

                                case Icode_REG_STR_C0:
                                    stringReg = strings [0];

                                    goto Loop;
                                    goto case Icode_REG_STR_C1;

                                case Icode_REG_STR_C1:
                                    stringReg = strings [1];

                                    goto Loop;
                                    goto case Icode_REG_STR_C2;

                                case Icode_REG_STR_C2:
                                    stringReg = strings [2];

                                    goto Loop;
                                    goto case Icode_REG_STR_C3;

                                case Icode_REG_STR_C3:
                                    stringReg = strings [3];

                                    goto Loop;
                                    goto case Icode_REG_STR1;

                                case Icode_REG_STR1:
                                    stringReg = strings [0xFF & iCode [frame.pc]];
                                    ++frame.pc;

                                    goto Loop;
                                    goto case Icode_REG_STR2;

                                case Icode_REG_STR2:
                                    stringReg = strings [GetIndex (iCode, frame.pc)];
                                    frame.pc += 2;

                                    goto Loop;
                                    goto case Icode_REG_STR4;

                                case Icode_REG_STR4:
                                    stringReg = strings [GetInt (iCode, frame.pc)];
                                    frame.pc += 4;

                                    goto Loop;
                                    goto default;

                                default:
                                    dumpICode (frame.idata);
                                    throw new ApplicationException ("Unknown icode : " + op + " @ pc : " + (frame.pc - 1));

                            } // end of interpreter switch
                        }

                    jumplessRun_brk:
                        ;
                        // end of jumplessRun label block

                        // This should be reachable only for jump implementation
                        // when pc points to encoded target offset
                        if (instructionCounting) {
                            addInstructionCount (cx, frame, 2);
                        }
                        int offset = GetShort (iCode, frame.pc);
                        if (offset != 0) {
                            // -1 accounts for pc pointing to jump opcode + 1
                            frame.pc += offset - 1;
                        }
                        else {
                            frame.pc = frame.idata.longJumps.getExistingInt (frame.pc);
                        }
                        if (instructionCounting) {
                            frame.pcPrevBranch = frame.pc;
                        }

                        goto Loop;

                    Loop:
                        ;
                    }

                Loop_brk:
                    ;
                    // end of Loop: for

                    ExitFrame (cx, frame, (object)null);
                    interpreterResult = frame.result;
                    interpreterResultDbl = frame.resultDbl;
                    if (frame.parentFrame != null) {
                        frame = frame.parentFrame;
                        if (frame.frozen) {
                            frame = frame.cloneFrozen ();
                        }
                        setCallResult (frame, interpreterResult, interpreterResultDbl);
                        interpreterResult = null; // Help GC

                        goto StateLoop;
                    }

                    goto StateLoop_brk;
                }
                // end of interpreter withoutExceptions: try					
                catch (Exception ex) {
                    if (throwable != null) {
                        // This is serious bug and it is better to track it ASAP
                        throw new ApplicationException ();
                    }
                    throwable = ex;
                }

            withoutExceptions_brk:

                // This should be reachable only after above catch or from
                // finally when it needs to propagate exception or from
                // explicit throw
                if (throwable == null)
                    Context.CodeBug ();

                // Exception type				
                const int EX_CATCH_STATE = 2; // Can execute JS catch				
                const int EX_FINALLY_STATE = 1; // Can execute JS finally				
                const int EX_NO_JS_STATE = 0; // Terminate JS execution

                int exState;
                ContinuationJump cjump2 = null;

                if (throwable is EcmaScriptThrow) {
                    exState = EX_CATCH_STATE;
                }
                else if (throwable is EcmaScriptError) {
                    // an offical ECMA error object,
                    exState = EX_CATCH_STATE;
                }
                else if (throwable is EcmaScriptRuntimeException) {
                    exState = EX_CATCH_STATE;
                }
                else if (throwable is EcmaScriptException) {
                    exState = EX_FINALLY_STATE;
                }
                else if (throwable is Exception) {
                    exState = EX_NO_JS_STATE;
                }
                else {
                    // It must be ContinuationJump
                    exState = EX_FINALLY_STATE;
                    cjump2 = (ContinuationJump)throwable;
                }

                if (instructionCounting) {
                    try {
                        addInstructionCount (cx, frame, EXCEPTION_COST);
                    }
                    catch (ApplicationException ex) {
                        // Error from instruction counting
                        //     => unconditionally terminate JS
                        throwable = ex;
                        cjump2 = null;
                        exState = EX_NO_JS_STATE;
                    }
                }
                if (frame.debuggerFrame != null && throwable is ApplicationException) {
                    // Call debugger only for RuntimeException
                    ApplicationException rex = (ApplicationException)throwable;
                    try {
                        frame.debuggerFrame.OnExceptionThrown (cx, rex);
                    }
                    catch (Exception ex) {
                        // Any exception from debugger
                        //     => unconditionally terminate JS
                        throwable = ex;
                        cjump2 = null;
                        exState = EX_NO_JS_STATE;
                    }
                }

                for (; ; ) {
                    if (exState != EX_NO_JS_STATE) {
                        bool onlyFinally = (exState != EX_CATCH_STATE);
                        indexReg = getExceptionHandler (frame, onlyFinally);
                        if (indexReg >= 0) {
                            // We caught an exception, restart the loop
                            // with exception pending the processing at the loop
                            // start

                            goto StateLoop;
                        }
                    }
                    // No allowed execption handlers in this frame, unwind
                    // to parent and try to look there

                    ExitFrame (cx, frame, throwable);

                    frame = frame.parentFrame;
                    if (frame == null) {
                        break;
                    }
                    if (cjump2 != null && cjump2.branchFrame == frame) {
                        // Continuation branch point was hit,
                        // restart the state loop to reenter continuation
                        indexReg = -1;

                        goto StateLoop;
                    }
                }

                // No more frames, rethrow the exception or deal with continuation
                if (cjump2 != null) {
                    if (cjump2.branchFrame != null) {
                        // The above loop should locate the top frame
                        Context.CodeBug ();
                    }
                    if (cjump2.capturedFrame != null) {
                        // Restarting detached continuation
                        indexReg = -1;

                        goto StateLoop;
                    }
                    // Return continuation result to the caller
                    interpreterResult = cjump2.result;
                    interpreterResultDbl = cjump2.resultDbl;
                    throwable = null;
                }

                goto StateLoop_brk;

            StateLoop:
                ;
            }

        StateLoop_brk:
            ;
            // end of StateLoop: for(;;)

            // Do cleanups/restorations before the final return or throw

            if (cx.previousInterpreterInvocations != null && cx.previousInterpreterInvocations.size () != 0) {
                cx.lastInterpreterFrame = cx.previousInterpreterInvocations.pop ();
            }
            else {
                // It was the last interpreter frame on the stack
                cx.lastInterpreterFrame = null;
                // Force GC of the value cx.previousInterpreterInvocations
                cx.previousInterpreterInvocations = null;
            }

            if (throwable != null) {
                if (throwable is Helpers.StackOverflowVerifierException) {
                    throw Context.ReportRuntimeError (
                        ScriptRuntime.GetMessage ("mag.too.deep.parser.recursion"));
                }
                throw (Exception)throwable;                
            }

            return (interpreterResult != DBL_MRK) ? interpreterResult :
            interpreterResultDbl;
        }

        static void initFrame (Context cx, IScriptable callerScope, IScriptable thisObj, object [] args, double [] argsDbl, int argShift, int argCount, InterpretedFunction fnOrScript, CallFrame parentFrame, CallFrame frame)
        {
            InterpreterData idata = fnOrScript.idata;

            bool useActivation = idata.itsNeedsActivation;
            DebugFrame debuggerFrame = null;
            if (cx.m_Debugger != null) {
                debuggerFrame = cx.m_Debugger.GetFrame (cx, idata);
                if (debuggerFrame != null) {
                    useActivation = true;
                }
            }

            if (useActivation) {
                // Copy args to new array to pass to enterActivationFunction
                // or debuggerFrame.onEnter
                if (argsDbl != null) {
                    args = GetArgsArray (args, argsDbl, argShift, argCount);
                }
                argShift = 0;
                argsDbl = null;
            }

            IScriptable scope;
            if (idata.itsFunctionType != 0) {
                if (!idata.useDynamicScope) {
                    scope = fnOrScript.ParentScope;
                }
                else {
                    scope = callerScope;
                }

                if (useActivation) {
                    scope = ScriptRuntime.createFunctionActivation (fnOrScript, scope, args);
                }
            }
            else {
                scope = callerScope;
                ScriptRuntime.initScript (fnOrScript, thisObj, cx, scope, fnOrScript.idata.evalScriptFlag);
            }

            if (idata.itsNestedFunctions != null) {
                if (idata.itsFunctionType != 0 && !idata.itsNeedsActivation)
                    Context.CodeBug ();
                for (int i = 0; i < idata.itsNestedFunctions.Length; i++) {
                    InterpreterData fdata = idata.itsNestedFunctions [i];
                    if (fdata.itsFunctionType == FunctionNode.FUNCTION_STATEMENT) {
                        initFunction (cx, scope, fnOrScript, i);
                    }
                }
            }

            IScriptable [] scriptRegExps = null;
            if (idata.itsRegExpLiterals != null) {
                // Wrapped regexps for functions are stored in
                // InterpretedFunction
                // but for script which should not contain references to scope
                // the regexps re-wrapped during each script execution
                if (idata.itsFunctionType != 0) {
                    scriptRegExps = fnOrScript.functionRegExps;
                }
                else {
                    scriptRegExps = fnOrScript.createRegExpWraps (cx, scope);
                }
            }

            // Initialize args, vars, locals and stack

            int emptyStackTop = idata.itsMaxVars + idata.itsMaxLocals - 1;
            int maxFrameArray = idata.itsMaxFrameArray;
            if (maxFrameArray != emptyStackTop + idata.itsMaxStack + 1)
                Context.CodeBug ();

            object [] stack;
            double [] sDbl;
            bool stackReuse;
            if (frame.stack != null && maxFrameArray <= frame.stack.Length) {
                // Reuse stacks from old frame
                stackReuse = true;
                stack = frame.stack;
                sDbl = frame.sDbl;
            }
            else {
                stackReuse = false;
                stack = new object [maxFrameArray];
                sDbl = new double [maxFrameArray];
            }

            int definedArgs = idata.argCount;
            if (definedArgs > argCount) {
                definedArgs = argCount;
            }

            // Fill the frame structure

            frame.parentFrame = parentFrame;
            frame.frameIndex = (parentFrame == null) ? 0 : parentFrame.frameIndex + 1;
            if (frame.frameIndex > cx.MaximumInterpreterStackDepth)
                throw ScriptRuntime.TypeErrorById ("msg.stackoverflow");
            frame.frozen = false;

            frame.fnOrScript = fnOrScript;
            frame.idata = idata;

            frame.stack = stack;
            frame.sDbl = sDbl;
            frame.varSource = frame;
            frame.localShift = idata.itsMaxVars;
            frame.emptyStackTop = emptyStackTop;

            frame.debuggerFrame = debuggerFrame;
            frame.useActivation = useActivation;

            frame.thisObj = thisObj;
            frame.scriptRegExps = scriptRegExps;

            // Initialize initial values of variables that change during
            // interpretation.
            frame.result = Undefined.Value;
            frame.pc = 0;
            frame.pcPrevBranch = 0;
            frame.pcSourceLineStart = idata.firstLinePC;
            frame.scope = scope;

            frame.savedStackTop = emptyStackTop;
            frame.savedCallOp = 0;

            Array.Copy (args, argShift, stack, 0, definedArgs);
            if (argsDbl != null) {
                Array.Copy (argsDbl, argShift, sDbl, 0, definedArgs);
            }
            for (int i = definedArgs; i != idata.itsMaxVars; ++i) {
                stack [i] = Undefined.Value;
            }
            if (stackReuse) {
                // Clean the stack part and space beyond stack if any
                // of the old array to allow to GC objects there
                for (int i = emptyStackTop + 1; i != stack.Length; ++i) {
                    stack [i] = null;
                }
            }

            EnterFrame (cx, frame, args);
        }

        static bool isFrameEnterExitRequired (CallFrame frame)
        {
            return frame.debuggerFrame != null || frame.idata.itsNeedsActivation;
        }

        static void EnterFrame (Context cx, CallFrame frame, object [] args)
        {
            if (frame.debuggerFrame != null) {
                frame.debuggerFrame.OnEnter (cx, frame.scope, frame.thisObj, args);
            }
            if (frame.idata.itsNeedsActivation) {
                // Enter activation only when itsNeedsActivation true, not when
                // useActivation holds since debugger should not interfere
                // with activation chaining
                ScriptRuntime.enterActivationFunction (cx, frame.scope);
            }
        }

        static void ExitFrame (Context cx, CallFrame frame, object throwable)
        {
            if (frame.idata.itsNeedsActivation) {
                ScriptRuntime.exitActivationFunction (cx);
            }

            if (frame.debuggerFrame != null) {
                try {
                    if (throwable is Exception) {
                        frame.debuggerFrame.OnExit (cx, true, throwable);
                    }
                    else {
                        object result;
                        ContinuationJump cjump = (ContinuationJump)throwable;
                        if (cjump == null) {
                            result = frame.result;
                        }
                        else {
                            result = cjump.result;
                        }
                        if (result == UniqueTag.DoubleMark) {
                            double resultDbl;
                            if (cjump == null) {
                                resultDbl = frame.resultDbl;
                            }
                            else {
                                resultDbl = cjump.resultDbl;
                            }
                            result = resultDbl;
                        }
                        frame.debuggerFrame.OnExit (cx, false, result);
                    }
                }
                catch (Exception ex) {
                    Console.Error.WriteLine ("USAGE WARNING: onExit terminated with exception");
                    Console.Error.WriteLine (ex.ToString ());
                }
            }
        }

        static void setCallResult (CallFrame frame, object callResult, double callResultDbl)
        {
            if (frame.savedCallOp == Token.CALL) {
                frame.stack [frame.savedStackTop] = callResult;
                frame.sDbl [frame.savedStackTop] = callResultDbl;
            }
            else if (frame.savedCallOp == Token.NEW) {
                // If construct returns scriptable,
                // then it replaces on stack top saved original instance
                // of the object.
                if (callResult is IScriptable) {
                    frame.stack [frame.savedStackTop] = callResult;
                }
            }
            else {
                Context.CodeBug ();
            }
            frame.savedCallOp = 0;
        }

        static void captureContinuation (Context cx, CallFrame frame, int stackTop)
        {
            Continuation c = new Continuation ();
            ScriptRuntime.setObjectProtoAndParent (c, ScriptRuntime.getTopCallScope (cx));

            // Make sure that all frames upstack frames are frozen
            CallFrame x = frame.parentFrame;
            while (x != null && !x.frozen) {
                x.frozen = true;
                // Allow to GC unused stack space
                for (int i = x.savedStackTop + 1; i != x.stack.Length; ++i) {
                    // Allow to GC unused stack space
                    x.stack [i] = null;
                }
                if (x.savedCallOp == Token.CALL) {
                    // the call will always overwrite the stack top with the result
                    x.stack [x.savedStackTop] = null;
                }
                else {
                    if (x.savedCallOp != Token.NEW)
                        Context.CodeBug ();
                    // the new operator uses stack top to store the constructed
                    // object so it shall not be cleared: see comments in
                    // setCallResult
                }
                x = x.parentFrame;
            }

            c.initImplementation (frame.parentFrame);
            frame.stack [stackTop] = c;
        }

        static int stack_int32 (CallFrame frame, int i)
        {
            object x = frame.stack [i];
            double value;
            if (x == UniqueTag.DoubleMark) {
                value = frame.sDbl [i];
            }
            else {
                value = ScriptConvert.ToNumber (x);
            }
            return ScriptConvert.ToInt32 (value);
        }

        static double stack_double (CallFrame frame, int i)
        {
            object x = frame.stack [i];
            if (x != UniqueTag.DoubleMark) {
                return ScriptConvert.ToNumber (x);
            }
            else {
                return frame.sDbl [i];
            }
        }

        static bool stack_boolean (CallFrame frame, int i)
        {
            object x = frame.stack [i];
            if (x is bool) {
                return (bool)x;
            }
            else if (x == UniqueTag.DoubleMark) {
                double d = frame.sDbl [i];
                return !double.IsNaN (d) && d != 0.0;
            }
            else if (x == null || x == Undefined.Value) {
                return false;
            }
            else if (CliHelper.IsNumber (x)) {
                double d = Convert.ToDouble (x);
                return (!double.IsNaN (d) && d != 0.0);
            }
            else {
                return ScriptConvert.ToBoolean (x);
            }
        }

        static void DoAdd (object [] stack, double [] sDbl, int stackTop, Context cx)
        {
            object rhs = stack [stackTop + 1];
            object lhs = stack [stackTop];
            double d;
            bool leftRightOrder;
            if (rhs == UniqueTag.DoubleMark) {
                d = sDbl [stackTop + 1];
                if (lhs == UniqueTag.DoubleMark) {
                    sDbl [stackTop] += d;
                    return;
                }
                leftRightOrder = true;
                // fallthrough to object + number code
            }
            else if (lhs == UniqueTag.DoubleMark) {
                d = sDbl [stackTop];
                lhs = rhs;
                leftRightOrder = false;
                // fallthrough to object + number code
            }
            else {
                if (lhs is IScriptable || rhs is IScriptable) {
                    stack [stackTop] = ScriptRuntime.Add (lhs, rhs, cx);
                }
                else if (lhs is string) {
                    string lstr = (string)lhs;
                    string rstr = ScriptConvert.ToString (rhs);
                    stack [stackTop] = string.Concat (lstr, rstr);
                }
                else if (rhs is string) {
                    string lstr = ScriptConvert.ToString (lhs);
                    string rstr = (string)rhs;
                    stack [stackTop] = string.Concat (lstr, rstr);
                }
                else {
                    double lDbl = (CliHelper.IsNumber (lhs)) ? Convert.ToDouble (lhs) : ScriptConvert.ToNumber (lhs);
                    double rDbl = (CliHelper.IsNumber (rhs)) ? Convert.ToDouble (rhs) : ScriptConvert.ToNumber (rhs);
                    stack [stackTop] = UniqueTag.DoubleMark;
                    sDbl [stackTop] = lDbl + rDbl;
                }
                return;
            }

            // handle object(lhs) + number(d) code
            if (lhs is IScriptable) {
                rhs = d;
                if (!leftRightOrder) {
                    object tmp = lhs;
                    lhs = rhs;
                    rhs = tmp;
                }
                stack [stackTop] = ScriptRuntime.Add (lhs, rhs, cx);
            }
            else if (lhs is string) {
                string lstr = (string)lhs;
                string rstr = ScriptConvert.ToString (d);
                if (leftRightOrder) {
                    stack [stackTop] = string.Concat (lstr, rstr);
                }
                else {
                    stack [stackTop] = string.Concat (rstr, lstr);
                }
            }
            else {
                double lDbl = (CliHelper.IsNumber (lhs)) ? Convert.ToDouble (lhs) : ScriptConvert.ToNumber (lhs);
                stack [stackTop] = UniqueTag.DoubleMark;
                sDbl [stackTop] = lDbl + d;
            }
        }

        void addGotoOp (int gotoOp)
        {
            sbyte [] array = itsData.itsICode;
            int top = itsICodeTop;
            if (top + 3 > array.Length) {
                array = increaseICodeCapasity (3);
            }
            array [top] = (sbyte)gotoOp;
            // Offset would written later
            itsICodeTop = top + 1 + 2;
        }


        static object [] GetArgsArray (object [] stack, double [] sDbl, int shift, int count)
        {
            if (count == 0) {
                return ScriptRuntime.EmptyArgs;
            }
            object [] args = new object [count];
            for (int i = 0; i != count; ++i, ++shift) {
                object val = stack [shift];
                if (val == UniqueTag.DoubleMark) {
                    val = sDbl [shift];
                }
                args [i] = val;
            }
            return args;
        }

        static void addInstructionCount (Context cx, CallFrame frame, int extra)
        {
            cx.instructionCount += frame.pc - frame.pcPrevBranch + extra;
            if (cx.instructionCount > cx.instructionThreshold) {
                cx.ObserveInstructionCount (cx.instructionCount);
                cx.instructionCount = 0;
            }
        }

    }
}