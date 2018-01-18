//------------------------------------------------------------------------------
// <license file="Callable.cs">
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

    /// <summary> Generic notion of callable object that can execute some script-related code
    /// upon request with specified values for script scope and this objects.
    /// </summary>
    public interface ICallable
    {
        /// <summary> Perform the call.
        /// 
        /// </summary>
        /// <param name="cx">the current Context for this thread
        /// </param>
        /// <param name="scope">the scope to use to resolve properties.
        /// </param>
        /// <param name="thisObj">the JavaScript <code>this</code> object
        /// </param>
        /// <param name="args">the array of arguments
        /// </param>
        /// <returns> the result of the call
        /// </returns>
        object Call (Context cx, IScriptable scope, IScriptable thisObj, object [] args);
    }
}