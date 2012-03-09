//------------------------------------------------------------------------------
// <license file="DebugFrame.cs">
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
using Scriptable = EcmaScript.NET.IScriptable;
namespace EcmaScript.NET.Debugging
{

    /// <summary>Interface to implement if the application is interested in receiving debug
    /// information during execution of a particular script or function.
    /// </summary>
    public interface DebugFrame
    {

        /// <summary>Called when execution is ready to start bytecode interpretation for entered a particular function or script.</summary>
        /// <param name="cx">current Context for this thread
        /// </param>
        /// <param name="activation">the activation scope for the function or script.
        /// </param>
        /// <param name="thisObj">value of the JavaScript <code>this</code> object
        /// </param>
        /// <param name="args">the array of arguments
        /// </param>
        void OnEnter (Context cx, IScriptable activation, IScriptable thisObj, object [] args);
        /// <summary>Called when executed code reaches new line in the source.</summary>
        /// <param name="cx">current Context for this thread
        /// </param>
        /// <param name="lineNumber">current line number in the script source
        /// </param>
        void OnLineChange (Context cx, int lineNumber);

        /// <summary>Called when thrown exception is handled by the function or script.</summary>
        /// <param name="cx">current Context for this thread
        /// </param>
        /// <param name="ex">exception object
        /// </param>		
        void OnExceptionThrown (Context cx, Exception ex);

        /// <summary>Called when the function or script for this frame is about to return.</summary>
        /// <param name="cx">current Context for this thread
        /// </param>
        /// <param name="byThrow">if true function will leave by throwing exception, otherwise it
        /// will execute normal return
        /// </param>
        /// <param name="resultOrException">function result in case of normal return or
        /// exception object if about to throw exception
        /// </param>
        void OnExit (Context cx, bool byThrow, object resultOrException);

        /// <summary>
        /// Called when the function or script executes a 'debugger' statement.
        /// </summary>
        /// <param name="cx">current Context for this thread</param>
        void OnDebuggerStatement(Context cx);
    }
}