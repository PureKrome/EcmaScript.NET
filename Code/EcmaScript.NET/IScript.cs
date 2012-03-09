//------------------------------------------------------------------------------
// <license file="Script.cs">
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

    /// <summary> All compiled scripts implement this interface.
    /// <p>
    /// This class encapsulates script execution relative to an
    /// object scope.
    /// </summary>	
    public interface IScript
    {

        /// <summary> Execute the script.
        /// <p>
        /// The script is executed in a particular runtime Context, which
        /// must be associated with the current thread.
        /// The script is executed relative to a scope--definitions and
        /// uses of global top-level variables and functions will access
        /// properties of the scope object. For compliant ECMA
        /// programs, the scope must be an object that has been initialized
        /// as a global object using <code>Context.initStandardObjects</code>.
        /// <p>
        /// 
        /// </summary>
        /// <param name="cx">the Context associated with the current thread
        /// </param>
        /// <param name="scope">the scope to execute relative to
        /// </param>
        /// <returns> the result of executing the script
        /// </returns>		
        object Exec (Context cx, IScriptable scope);
    }
}