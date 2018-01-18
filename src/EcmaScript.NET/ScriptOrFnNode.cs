//------------------------------------------------------------------------------
// <license file="ScriptOrFnNode.cs">
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

    public class ScriptOrFnNode : Node
    {
        virtual public string SourceName
        {
            get
            {
                return sourceName;
            }

            set
            {
                this.sourceName = value;
            }

        }
        virtual public int EncodedSourceStart
        {
            get
            {
                return encodedSourceStart;
            }

        }
        virtual public int EncodedSourceEnd
        {
            get
            {
                return encodedSourceEnd;
            }

        }
        virtual public int BaseLineno
        {
            get
            {
                return baseLineno;
            }

            set
            {
                // One time action
                if (value < 0 || baseLineno >= 0)
                    Context.CodeBug ();
                baseLineno = value;
            }

        }
        virtual public int EndLineno
        {
            get
            {
                return baseLineno;
            }

            set
            {
                // One time action
                if (value < 0 || endLineno >= 0)
                    Context.CodeBug ();
                endLineno = value;
            }

        }
        virtual public int FunctionCount
        {
            get
            {
                if (functions == null) {
                    return 0;
                }
                return functions.size ();
            }

        }
        virtual public int RegexpCount
        {
            get
            {
                if (regexps == null) {
                    return 0;
                }
                return regexps.size () / 2;
            }

        }
        virtual public int ParamCount
        {
            get
            {
                return varStart;
            }

        }
        virtual public int ParamAndVarCount
        {
            get
            {
                return itsVariables.size ();
            }

        }
        virtual public string [] ParamAndVarNames
        {
            get
            {
                int N = itsVariables.size ();
                if (N == 0) {
                    return ScriptRuntime.EmptyStrings;
                }
                string [] array = new string [N];
                itsVariables.ToArray (array);
                return array;
            }

        }
        virtual public object CompilerData
        {
            get
            {
                return compilerData;
            }

            set
            {
                if (value == null)
                    throw new ArgumentException ();
                // Can only call once
                if (compilerData != null)
                    throw new Exception ();
                compilerData = value;
            }

        }

        public ScriptOrFnNode (int nodeType)
            : base (nodeType)
        {
        }

        public void setEncodedSourceBounds (int start, int end)
        {
            this.encodedSourceStart = start;
            this.encodedSourceEnd = end;
        }

        public FunctionNode getFunctionNode (int i)
        {
            return (FunctionNode)functions.Get (i);
        }

        public int addFunction (FunctionNode fnNode)
        {
            if (fnNode == null)
                Context.CodeBug ();
            if (functions == null) {
                functions = new ObjArray ();
            }
            functions.add (fnNode);
            return functions.size () - 1;
        }

        public string getRegexpString (int index)
        {
            return (string)regexps.Get (index * 2);
        }

        public string getRegexpFlags (int index)
        {
            return (string)regexps.Get (index * 2 + 1);
        }

        public int addRegexp (string str, string flags)
        {
            if (str == null)
                Context.CodeBug ();
            if (regexps == null) {
                regexps = new ObjArray ();
            }
            regexps.add (str);
            regexps.add (flags);
            return regexps.size () / 2 - 1;
        }

        public bool hasParamOrVar (string name)
        {
            return itsVariableNames.has (name);
        }

        public int getParamOrVarIndex (string name)
        {
            return itsVariableNames.Get (name, -1);
        }

        public string getParamOrVarName (int index)
        {
            return (string)itsVariables.Get (index);
        }

        public void addParam (string name)
        {
            // Check addparam is not called after addLocal
            if (varStart != itsVariables.size ())
                Context.CodeBug ();
            // Allow non-unique parameter names: use the last occurrence
            int index = varStart++;
            itsVariables.add (name);
            itsVariableNames.put (name, index);
        }

        public void addVar (string name)
        {
            int vIndex = itsVariableNames.Get (name, -1);
            if (vIndex != -1) {
                // There's already a variable or parameter with this name.
                return;
            }
            int index = itsVariables.size ();
            itsVariables.add (name);
            itsVariableNames.put (name, index);
        }

        public void removeParamOrVar (string name)
        {
            int i = itsVariableNames.Get (name, -1);
            if (i != -1) {
                itsVariables.remove (i);
                itsVariableNames.remove (name);
                ObjToIntMap.Iterator iter = itsVariableNames.newIterator ();
                for (iter.start (); !iter.done (); iter.next ()) {
                    int v = iter.Value;
                    if (v > i) {
                        iter.Value = v - 1;
                    }
                }
            }
        }

        private int encodedSourceStart;
        private int encodedSourceEnd;
        private string sourceName;
        private int baseLineno = -1;
        private int endLineno = -1;

        private ObjArray functions;

        private ObjArray regexps;

        // a list of the formal parameters and local variables
        private ObjArray itsVariables = new ObjArray ();

        // mapping from name to index in list
        private ObjToIntMap itsVariableNames = new ObjToIntMap (11);

        private int varStart; // index in list of first variable

        private object compilerData;
    }
}