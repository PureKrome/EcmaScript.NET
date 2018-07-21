//------------------------------------------------------------------------------
// <license file="DefaultErrorReporter.cs">
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

    /// <summary> This is the default error reporter for JavaScript.
    /// 
    /// </summary>
    class DefaultErrorReporter : ErrorReporter
    {
        internal static readonly DefaultErrorReporter instance = new DefaultErrorReporter ();

        private bool forEval;
        private ErrorReporter chainedReporter;

        private DefaultErrorReporter ()
        {
        }

        internal static ErrorReporter ForEval (ErrorReporter reporter)
        {
            DefaultErrorReporter r = new DefaultErrorReporter ();
            r.forEval = true;
            r.chainedReporter = reporter;
            return r;
        }

        public virtual void Warning (string message, string sourceURI, int line, string lineText, int lineOffset)
        {
            if (chainedReporter != null) {
                chainedReporter.Warning (message, sourceURI, line, lineText, lineOffset);
            }
            else {
                Console.Error.WriteLine ("strict warning: " + message);
            }
        }

        public virtual void Error (string message, string sourceURI, int line, string lineText, int lineOffset)
        {
            if (forEval) {
                throw ScriptRuntime.ConstructError ("SyntaxError", message, sourceURI, line, lineText, lineOffset);
            }
            if (chainedReporter != null) {
                chainedReporter.Error (message, sourceURI, line, lineText, lineOffset);
            }
            else {
                throw RuntimeError (message, sourceURI, line, lineText, lineOffset);
            }
        }

        public virtual EcmaScriptRuntimeException RuntimeError (string message, string sourceURI, int line, string lineText, int lineOffset)
        {
            if (chainedReporter != null) {
                return chainedReporter.RuntimeError (message, sourceURI, line, lineText, lineOffset);
            }
            else {
                return new EcmaScriptRuntimeException (message, sourceURI, line, lineText, lineOffset);
            }
        }
    }
}