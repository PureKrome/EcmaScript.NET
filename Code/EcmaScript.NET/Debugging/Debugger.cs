//------------------------------------------------------------------------------
// <license file="Debugger.cs">
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

// API class
using System;
using Context = EcmaScript.NET.Context;
namespace EcmaScript.NET.Debugging
{

    /// <summary>Interface to implement if the application is interested in receiving debug
    /// information.
    /// </summary>
    public interface Debugger
    {

        /// <summary>Called when compilation of a particular function or script into internal
        /// bytecode is done.
        /// </summary>
        /// <param name="cx">current Context for this thread
        /// </param>
        /// <param name="fnOrScript">object describing the function or script
        /// </param>
        /// <param name="source">the function or script source
        /// </param>
        void HandleCompilationDone (Context cx, DebuggableScript fnOrScript, string source);

        /// <summary>Called when execution entered a particular function or script.</summary>
        /// <returns> implementation of DebugFrame which receives debug information during
        /// the function or script execution or null otherwise
        /// </returns>
        DebugFrame GetFrame (Context cx, DebuggableScript fnOrScript);
    }
}