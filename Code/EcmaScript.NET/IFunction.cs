//------------------------------------------------------------------------------
// <license file="Function.cs">
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

    /// <summary> This is interface that all functions in JavaScript must implement.
    /// The interface provides for calling functions and constructors.
    /// 
    /// </summary>	
    public interface IFunction : IScriptable, ICallable
    {

        /// <summary> Call the function as a constructor.
        /// 
        /// This method is invoked by the runtime in order to satisfy a use
        /// of the JavaScript <code>new</code> operator.  This method is
        /// expected to create a new object and return it.
        /// 
        /// </summary>
        /// <param name="cx">the current Context for this thread
        /// </param>
        /// <param name="scope">an enclosing scope of the caller except
        /// when the function is called from a closure.
        /// </param>
        /// <param name="args">the array of arguments
        /// </param>
        /// <returns> the allocated object
        /// </returns>
        IScriptable Construct (Context cx, IScriptable scope, object [] args);
    }
}