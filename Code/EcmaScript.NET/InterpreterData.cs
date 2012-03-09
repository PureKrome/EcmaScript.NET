//------------------------------------------------------------------------------
// <license file="InterpreterData.cs">
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

    internal sealed class InterpreterData : DebuggableScript
    {
        public bool TopLevel
        {
            get
            {
                return topLevel;
            }

        }
        public string FunctionName
        {
            get
            {
                return itsName;
            }

        }
        public int ParamCount
        {
            get
            {
                return argCount;
            }

        }
        public int ParamAndVarCount
        {
            get
            {
                return argNames.Length;
            }

        }
        public string SourceName
        {
            get
            {
                return itsSourceFile;
            }

        }
        public bool GeneratedScript
        {
            get
            {
                return ScriptRuntime.isGeneratedScript (itsSourceFile);
            }

        }
        public int [] LineNumbers
        {
            get
            {
                return Interpreter.getLineNumbers (this);
            }

        }
        public int FunctionCount
        {
            get
            {
                return (itsNestedFunctions == null) ? 0 : itsNestedFunctions.Length;
            }

        }
        public DebuggableScript Parent
        {
            get
            {
                return parentData;
            }

        }

        internal const int INITIAL_MAX_ICODE_LENGTH = 1024;
        internal const int INITIAL_STRINGTABLE_SIZE = 64;
        internal const int INITIAL_NUMBERTABLE_SIZE = 64;

        internal InterpreterData (Context.Versions languageVersion, string sourceFile, string encodedSource)
        {
            this.languageVersion = languageVersion;
            this.itsSourceFile = sourceFile;
            this.encodedSource = encodedSource;

            Init ();
        }

        internal InterpreterData (InterpreterData parent)
        {
            this.parentData = parent;
            this.languageVersion = parent.languageVersion;
            this.itsSourceFile = parent.itsSourceFile;
            this.encodedSource = parent.encodedSource;

            Init ();
        }

        private void Init ()
        {
            itsICode = new sbyte [INITIAL_MAX_ICODE_LENGTH];
            itsStringTable = new string [INITIAL_STRINGTABLE_SIZE];
        }

        internal string itsName;
        internal string itsSourceFile;
        internal bool itsNeedsActivation;
        internal int itsFunctionType;

        internal string [] itsStringTable;
        internal double [] itsDoubleTable;
        internal InterpreterData [] itsNestedFunctions;
        internal object [] itsRegExpLiterals;

        internal sbyte [] itsICode;

        internal int [] itsExceptionTable;

        internal int itsMaxVars;
        internal int itsMaxLocals;
        internal int itsMaxStack;
        internal int itsMaxFrameArray;

        // see comments in NativeFuncion for definition of argNames and argCount
        internal string [] argNames;
        internal int argCount;

        internal int itsMaxCalleeArgs;

        internal string encodedSource;
        internal int encodedSourceStart;
        internal int encodedSourceEnd;

        internal Context.Versions languageVersion;

        internal bool useDynamicScope;

        internal bool topLevel;

        internal object [] literalIds;

        internal UintMap longJumps;

        internal int firstLinePC = -1; // PC for the first LINE icode

        internal InterpreterData parentData;

        internal bool evalScriptFlag; // true if script corresponds to eval() code

        public bool IsFunction ()
        {
            return itsFunctionType != 0;
        }

        public string GetParamOrVarName (int index)
        {
            return argNames [index];
        }

        public DebuggableScript GetFunction (int index)
        {
            return itsNestedFunctions [index];
        }
    }
}