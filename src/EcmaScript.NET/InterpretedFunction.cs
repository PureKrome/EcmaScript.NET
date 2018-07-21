//------------------------------------------------------------------------------
// <license file="InterpretedFunction.cs">
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
using EcmaScript.NET.Types;

namespace EcmaScript.NET
{


    sealed class InterpretedFunction : BuiltinFunction, IScript
    {
        override public string FunctionName
        {
            get
            {
                return (idata.itsName == null) ? "" : idata.itsName;
            }

        }

        override public string EncodedSource
        {
            get
            {
                return Interpreter.GetEncodedSource (idata);
            }

        }
        override public DebuggableScript DebuggableView
        {
            get
            {
                return idata;
            }

        }
        override protected internal Context.Versions LanguageVersion
        {
            get
            {
                return idata.languageVersion;
            }

        }
        override protected internal int ParamCount
        {
            get
            {
                return idata.argCount;
            }

        }
        override protected internal int ParamAndVarCount
        {
            get
            {
                return idata.argNames.Length;
            }

        }

        internal InterpreterData idata;
        internal SecurityController securityController;
        internal object securityDomain;
        internal IScriptable [] functionRegExps;

        private InterpretedFunction (InterpreterData idata, object staticSecurityDomain)
        {
            this.idata = idata;

            // Always get Context from the current thread to
            // avoid security breaches via passing mangled Context instances
            // with bogus SecurityController
            Context cx = Context.CurrentContext;
            SecurityController sc = cx.SecurityController;
            object dynamicDomain;
            if (sc != null) {
                dynamicDomain = sc.getDynamicSecurityDomain (staticSecurityDomain);
            }
            else {
                if (staticSecurityDomain != null) {
                    throw new ArgumentException ();
                }
                dynamicDomain = null;
            }

            this.securityController = sc;
            this.securityDomain = dynamicDomain;
        }

        private InterpretedFunction (InterpretedFunction parent, int index)
        {
            this.idata = parent.idata.itsNestedFunctions [index];
            this.securityController = parent.securityController;
            this.securityDomain = parent.securityDomain;
        }

        /// <summary> Create script from compiled bytecode.</summary>
        internal static InterpretedFunction createScript (InterpreterData idata, object staticSecurityDomain)
        {
            InterpretedFunction f;
            f = new InterpretedFunction (idata, staticSecurityDomain);
            return f;
        }

        /// <summary> Create function compiled from Function(...) constructor.</summary>
        internal static InterpretedFunction createFunction (Context cx, IScriptable scope, InterpreterData idata, object staticSecurityDomain)
        {
            InterpretedFunction f;
            f = new InterpretedFunction (idata, staticSecurityDomain);
            f.initInterpretedFunction (cx, scope);
            return f;
        }

        /// <summary> Create function embedded in script or another function.</summary>
        internal static InterpretedFunction createFunction (Context cx, IScriptable scope, InterpretedFunction parent, int index)
        {
            InterpretedFunction f = new InterpretedFunction (parent, index);
            f.initInterpretedFunction (cx, scope);
            return f;
        }

        internal IScriptable [] createRegExpWraps (Context cx, IScriptable scope)
        {
            if (idata.itsRegExpLiterals == null)
                Context.CodeBug ();

            RegExpProxy rep = cx.RegExpProxy;
            int N = idata.itsRegExpLiterals.Length;
            IScriptable [] array = new IScriptable [N];
            for (int i = 0; i != N; ++i) {
                array [i] = rep.Wrap (cx, scope, idata.itsRegExpLiterals [i]);
            }
            return array;
        }

        private void initInterpretedFunction (Context cx, IScriptable scope)
        {
            initScriptFunction (cx, scope);
            if (idata.itsRegExpLiterals != null) {
                functionRegExps = createRegExpWraps (cx, scope);
            }
        }

        public override object Call (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!ScriptRuntime.hasTopCall (cx)) {
                return ScriptRuntime.DoTopCall (this, cx, scope, thisObj, args);
            }
            return Interpreter.Interpret (this, cx, scope, thisObj, args);
        }

        public object Exec (Context cx, IScriptable scope)
        {
            if (idata.itsFunctionType != 0) {
                // Can only be applied to scripts
                throw new Exception ();
            }
            if (!ScriptRuntime.hasTopCall (cx)) {
                // It will go through "call" path. but they are equivalent
                return ScriptRuntime.DoTopCall (this, cx, scope, scope, ScriptRuntime.EmptyArgs);
            }
            return Interpreter.Interpret (this, cx, scope, scope, ScriptRuntime.EmptyArgs);
        }

        protected internal override string getParamOrVarName (int index)
        {
            return idata.argNames [index];
        }


    }
}