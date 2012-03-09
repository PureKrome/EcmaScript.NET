//------------------------------------------------------------------------------
// <license file="NativeFunction.cs">
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

namespace EcmaScript.NET.Types
{

    /// <summary> This class implements the Function native object.
    /// See ECMA 15.3.
    /// </summary>
    public abstract class BuiltinFunction : BaseFunction
    {
        override public int Length
        {
            get
            {
                int paramCount = ParamCount;
                if (LanguageVersion != Context.Versions.JS1_2) {
                    return paramCount;
                }
                Context cx = Context.CurrentContext;
                BuiltinCall activation = ScriptRuntime.findFunctionActivation (cx, this);
                if (activation == null) {
                    return paramCount;
                }
                return activation.originalArgs.Length;
            }

        }
        override public int Arity
        {
            get
            {
                return ParamCount;
            }

        }
        /// <summary> Get encoded source string.</summary>
        virtual public string EncodedSource
        {
            get
            {
                return null;
            }

        }
        virtual public DebuggableScript DebuggableView
        {
            get
            {
                return null;
            }

        }
        protected internal abstract Context.Versions LanguageVersion { get;}
        /// <summary> Get number of declared parameters. It should be 0 for scripts.</summary>
        protected internal abstract int ParamCount { get;}
        /// <summary> Get number of declared parameters and variables defined through var
        /// statements.
        /// </summary>
        protected internal abstract int ParamAndVarCount { get;}

        public void initScriptFunction (Context cx, IScriptable scope)
        {
            ScriptRuntime.setFunctionProtoAndParent (this, scope);
        }

        /// <param name="indent">How much to indent the decompiled result
        /// 
        /// </param>
        /// <param name="flags">Flags specifying format of decompilation output
        /// </param>
        internal override string Decompile (int indent, int flags)
        {
            string encodedSource = EncodedSource;
            if (encodedSource == null) {
                return base.Decompile (indent, flags);
            }
            else {
                UintMap properties = new UintMap (1);
                properties.put (Decompiler.INITIAL_INDENT_PROP, indent);
                return Decompiler.Decompile (encodedSource, flags, properties);
            }
        }


        /// <summary> Get parameter or variable name.
        /// If <tt>index < {@link #getParamCount()}</tt>, then return the name of the
        /// corresponding parameter. Otherwise returm the name of variable.
        /// </summary>
        protected internal abstract string getParamOrVarName (int index);


    }
}