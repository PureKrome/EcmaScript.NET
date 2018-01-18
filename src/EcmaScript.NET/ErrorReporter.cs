//------------------------------------------------------------------------------
// <license file="ErrorReporter.cs">
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

    /// <summary> This is interface defines a protocol for the reporting of
    /// errors during JavaScript translation or execution.
    /// 
    /// </summary>

    public interface ErrorReporter
    {

        /// <summary> Report a warning.
        /// 
        /// The implementing class may choose to ignore the warning
        /// if it desires.
        /// 
        /// </summary>
        /// <param name="message">a String describing the warning
        /// </param>
        /// <param name="sourceName">a String describing the JavaScript source
        /// where the warning occured; typically a filename or URL
        /// </param>
        /// <param name="line">the line number associated with the warning
        /// </param>
        /// <param name="lineSource">the text of the line (may be null)
        /// </param>
        /// <param name="lineOffset">the offset into lineSource where problem was detected
        /// </param>
        void Warning (string message, string sourceName, int line, string lineSource, int lineOffset);

        /// <summary> Report an error.
        /// 
        /// The implementing class is free to throw an exception if
        /// it desires.
        /// 
        /// If execution has not yet begun, the JavaScript engine is
        /// free to find additional errors rather than terminating
        /// the translation. It will not execute a script that had
        /// errors, however.
        /// 
        /// </summary>
        /// <param name="message">a String describing the error
        /// </param>
        /// <param name="sourceName">a String describing the JavaScript source
        /// where the error occured; typically a filename or URL
        /// </param>
        /// <param name="line">the line number associated with the error
        /// </param>
        /// <param name="lineSource">the text of the line (may be null)
        /// </param>
        /// <param name="lineOffset">the offset into lineSource where problem was detected
        /// </param>
        void Error (string message, string sourceName, int line, string lineSource, int lineOffset);

        /// <summary> Creates an EvaluatorException that may be thrown.
        /// 
        /// runtimeErrors, unlike errors, will always terminate the
        /// current script.
        /// 
        /// </summary>
        /// <param name="message">a String describing the error
        /// </param>
        /// <param name="sourceName">a String describing the JavaScript source
        /// where the error occured; typically a filename or URL
        /// </param>
        /// <param name="line">the line number associated with the error
        /// </param>
        /// <param name="lineSource">the text of the line (may be null)
        /// </param>
        /// <param name="lineOffset">the offset into lineSource where problem was detected
        /// </param>
        /// <returns> an EvaluatorException that will be thrown.
        /// </returns>
        EcmaScriptRuntimeException RuntimeError (string message, string sourceName, int line, string lineSource, int lineOffset);
    }
}