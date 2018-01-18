//------------------------------------------------------------------------------
// <license file="RefCallable.cs">
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

    /// <summary>
    /// Object that can allows assignments to the result of function calls.
    /// </summary>
    public interface IRefCallable : ICallable
    {
        /// <summary> Perform function call in reference context.
        /// The args array reference should not be stored in any object that is
        /// can be GC-reachable after this method returns. If this is necessary,
        /// for example, to implement {@link Ref} methods, then store args.clone(),
        /// not args array itself.
        /// 
        /// </summary>
        /// <param name="cx">the current Context for this thread
        /// </param>
        /// <param name="thisObj">the JavaScript <code>this</code> object
        /// </param>
        /// <param name="args">the array of arguments
        /// </param>
        IRef RefCall (Context cx, IScriptable thisObj, object [] args);
    }
}