//------------------------------------------------------------------------------
// <license file="EcmaScriptRuntimeException.cs">
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

    /// <summary> The class of exceptions thrown by the JavaScript engine.</summary>

    public class EcmaScriptRuntimeException : EcmaScriptException
    {

        public EcmaScriptRuntimeException (Exception innerException)
            : base (innerException.Message, innerException)
        {
            int [] linep = new int [] { 0 };
            string sourceName = Context.GetSourcePositionFromStack (linep);
            int lineNumber = linep [0];
            if (sourceName != null) {
                InitSourceName (sourceName);
            }
            if (lineNumber != 0) {
                InitLineNumber (lineNumber);
            }
        }

        public EcmaScriptRuntimeException (string detail)
            : base (detail)
        {
        }

        /// <summary> Create an exception with the specified detail message.
        /// 
        /// Errors internal to the JavaScript engine will simply throw a
        /// RuntimeException.
        /// 
        /// </summary>
        /// <param name="detail">the error message
        /// </param>
        /// <param name="sourceName">the name of the source reponsible for the error
        /// </param>
        /// <param name="lineNumber">the line number of the source
        /// </param>
        public EcmaScriptRuntimeException (string detail, string sourceName, int lineNumber)
            : this (detail, sourceName, lineNumber, null, 0)
        {
        }

        /// <summary> Create an exception with the specified detail message.
        /// 
        /// Errors internal to the JavaScript engine will simply throw a
        /// RuntimeException.
        /// 
        /// </summary>
        /// <param name="detail">the error message
        /// </param>
        /// <param name="sourceName">the name of the source reponsible for the error
        /// </param>
        /// <param name="lineNumber">the line number of the source
        /// </param>
        /// <param name="columnNumber">the columnNumber of the source (may be zero if
        /// unknown)
        /// </param>
        /// <param name="lineSource">the source of the line containing the error (may be
        /// null if unknown)
        /// </param>
        public EcmaScriptRuntimeException (string detail, string sourceName, int lineNumber, string lineSource, int columnNumber)
            : base (detail)
        {
            RecordErrorOrigin (sourceName, lineNumber, lineSource, columnNumber);
        }
    }
}