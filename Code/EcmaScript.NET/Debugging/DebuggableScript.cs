//------------------------------------------------------------------------------
// <license file="DebuggableScript.cs">
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
namespace EcmaScript.NET.Debugging
{

    /// <summary> This interface exposes debugging information from executable
    /// code (either functions or top-level scripts).
    /// </summary>
    public interface DebuggableScript
    {
        bool TopLevel
        {
            get;

        }
        /// <summary> Get name of the function described by this script.
        /// Return null or an empty string if this script is not function.
        /// </summary>
        string FunctionName
        {
            get;

        }
        /// <summary> Get number of declared parameters in function.
        /// Return 0 if this script is not function.
        /// 
        /// </summary>		
        int ParamCount
        {
            get;

        }
        /// <summary> Get number of declared parameters and local variables.
        /// Return number of declared global variables if this script is not
        /// function.
        /// 
        /// </summary>		
        int ParamAndVarCount
        {
            get;

        }
        /// <summary> Get the name of the source (usually filename or URL)
        /// of the script.
        /// </summary>
        string SourceName
        {
            get;

        }
        /// <summary> Returns true if this script or function were runtime-generated
        /// from JavaScript using <tt>eval</tt> function or <tt>Function</tt>
        /// or <tt>Script</tt> constructors.
        /// </summary>
        bool GeneratedScript
        {
            get;

        }
        /// <summary> Get array containing the line numbers that
        /// that can be passed to <code>DebugFrame.onLineChange()<code>.
        /// Note that line order in the resulting array is arbitrary
        /// </summary>
        int [] LineNumbers
        {
            get;

        }
        int FunctionCount
        {
            get;

        }
        DebuggableScript Parent
        {
            get;

        }

        /// <summary> Returns true if this is a function, false if it is a script.</summary>
        bool IsFunction ();

        /// <summary> Get name of a declared parameter or local variable.
        /// <tt>index</tt> should be less then {@link #getParamAndVarCount()}.
        /// If <tt>index&nbsp;&lt;&nbsp;{@link #getParamCount()}</tt>, return
        /// the name of the corresponding parameter, otherwise return the name
        /// of variable.
        /// If this script is not function, return the name of the declared
        /// global variable.
        /// </summary>
        string GetParamOrVarName (int index);

        DebuggableScript GetFunction (int index);
    }
}