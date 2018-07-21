//------------------------------------------------------------------------------
// <license file="EcmaScriptError.cs">
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

    /// <summary> The class of exceptions raised by the engine as described in
    /// ECMA edition 3. See section 15.11.6 in particular.
    /// </summary>	
    public class EcmaScriptError : EcmaScriptException
    {
        /// <summary> Gets the name of the error.
        /// 
        /// ECMA edition 3 defines the following
        /// errors: EvalError, RangeError, ReferenceError,
        /// SyntaxError, TypeError, and URIError. Additional error names
        /// may be added in the future.
        /// 
        /// See ECMA edition 3, 15.11.7.9.
        /// 
        /// </summary>
        /// <returns> the name of the error.
        /// </returns>
        public virtual string Name
        {
            get
            {
                return m_ErrorName;
            }

        }

        public override string Message
        {
            get
            {
                return string.Format (
                    "\"{0}\", {1} at line {2}: {3}",
                        base.SourceName, Name, base.LineNumber, ErrorMessage);
            }
        }

        /// <summary> Gets the message corresponding to the error.
        /// 
        /// See ECMA edition 3, 15.11.7.10.
        /// 
        /// </summary>
        /// <returns> an implemenation-defined string describing the error.
        /// </returns>
        public virtual string ErrorMessage
        {
            get
            {
                return m_ErrorMessage;
            }

        }

        string m_ErrorName;
        string m_ErrorMessage;

        /// <summary> Create an exception with the specified detail message.
        /// 
        /// Errors internal to the JavaScript engine will simply throw a
        /// RuntimeException.
        /// 
        /// </summary>
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
        internal EcmaScriptError (string errorName, string errorMessage, string sourceName, int lineNumber, string lineSource, int columnNumber)
        {
            RecordErrorOrigin (sourceName, lineNumber, lineSource, columnNumber);
            this.m_ErrorName = errorName;
            this.m_ErrorMessage = errorMessage;
        }

    }

}