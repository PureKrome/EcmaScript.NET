//------------------------------------------------------------------------------
// <license file="FunctionNode.cs">
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

namespace EcmaScript.NET
{

    public class FunctionNode : ScriptOrFnNode
    {
        virtual public string FunctionName
        {
            get
            {
                return functionName;
            }

        }
        virtual public bool IgnoreDynamicScope
        {
            get
            {
                return itsIgnoreDynamicScope;
            }

        }
        virtual public int FunctionType
        {
            get
            {
                return itsFunctionType;
            }

        }

        public FunctionNode (string name)
            : base (Token.FUNCTION)
        {
            functionName = name;
        }

        public virtual bool RequiresActivation
        {

            get
            {
                return itsNeedsActivation;
            }
        }

        /// <summary>
        /// There are three types of functions that can be defined. The first
        /// is a function statement. This is a function appearing as a top-level
        /// statement (i.e., not nested inside some other statement) in either a
        /// script or a function.
        /// 
        /// The second is a function expression, which is a function appearing in
        /// an expression except for the third type, which is...
        /// 
        /// The third type is a function expression where the expression is the
        /// top-level expression in an expression statement.
        /// 
        /// The three types of functions have different treatment and must be
        /// distinquished.
        /// </summary>
        public const int FUNCTION_STATEMENT = 1;
        public const int FUNCTION_EXPRESSION = 2;
        public const int FUNCTION_EXPRESSION_STATEMENT = 3;

        internal string functionName;
        internal bool itsNeedsActivation;
        internal int itsFunctionType;
        internal bool itsIgnoreDynamicScope;
    }
}