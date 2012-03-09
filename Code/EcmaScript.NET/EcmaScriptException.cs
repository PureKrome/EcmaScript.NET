//------------------------------------------------------------------------------
// <license file="EcmaScriptException.cs">
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
using System.IO;
using System.Text;

namespace EcmaScript.NET
{

    /// <summary>
    /// The class of exceptions thrown by the JavaScript engine.
    /// </summary>	
    public abstract class EcmaScriptException : ApplicationException
    {

        public override string Message
        {
            get
            {
                string details = base.Message;
                if (m_SourceName == null || m_LineNumber <= 0) {
                    return details;
                }
                StringBuilder buf = new StringBuilder (details);
                buf.Append (" (");
                if (m_SourceName != null) {
                    buf.Append (m_SourceName);
                }
                if (m_LineNumber > 0) {
                    buf.Append ('#');
                    buf.Append (m_LineNumber);
                }
                buf.Append (')');
                return buf.ToString ();
            }

        }

        internal EcmaScriptException ()
        {
            Interpreter.captureInterpreterStackInfo (this);
        }

        internal EcmaScriptException (string details)
            : base (details)
        {
            Interpreter.captureInterpreterStackInfo (this);
        }


        internal EcmaScriptException (string details, Exception innerException)
            : base (details, innerException)
        {
            Interpreter.captureInterpreterStackInfo (this);
        }
        /// <summary> Get the uri of the script source containing the error, or null
        /// if that information is not available.
        /// </summary>
        public virtual string SourceName
        {
            get
            {
                return m_SourceName;
            }
        }

        /// <summary> Initialize the uri of the script source containing the error.
        /// 
        /// </summary>
        /// <param name="sourceName">the uri of the script source reponsible for the error.
        /// It should not be <tt>null</tt>.
        /// 
        /// </param>
        /// <throws>  IllegalStateException if the method is called more then once. </throws>
        public void InitSourceName (string sourceName)
        {
            if (sourceName == null)
                throw new ArgumentException ();
            if (this.m_SourceName != null)
                throw new ApplicationException ();
            this.m_SourceName = sourceName;
        }

        /// <summary> Returns the line number of the statement causing the error,
        /// or zero if not available.
        /// </summary>
        public int LineNumber
        {
            get
            {
                return m_LineNumber;
            }
        }

        /// <summary> Initialize the line number of the script statement causing the error.
        /// 
        /// </summary>
        /// <param name="lineNumber">the line number in the script source.
        /// It should be positive number.
        /// 
        /// </param>
        /// <throws>  IllegalStateException if the method is called more then once. </throws>
        public void InitLineNumber (int lineNumber)
        {
            if (lineNumber <= 0)
                throw new ArgumentException (Convert.ToString (lineNumber));
            if (this.m_LineNumber > 0)
                throw new ApplicationException ();
            this.m_LineNumber = lineNumber;
        }

        /// <summary> The column number of the location of the error, or zero if unknown.</summary>
        public int ColumnNumber
        {
            get
            {
                return m_ColumnNumber;
            }
        }

        /// <summary> Initialize the column number of the script statement causing the error.
        /// 
        /// </summary>
        /// <param name="columnNumber">the column number in the script source.
        /// It should be positive number.
        /// 
        /// </param>
        /// <throws>  IllegalStateException if the method is called more then once. </throws>
        public void InitColumnNumber (int columnNumber)
        {
            if (columnNumber <= 0)
                throw new ArgumentException (Convert.ToString (columnNumber));
            if (this.m_ColumnNumber > 0)
                throw new ApplicationException ();
            this.m_ColumnNumber = columnNumber;
        }

        /// <summary> The source text of the line causing the error, or null if unknown.</summary>
        public string LineSource
        {
            get
            {
                return m_LineSource;
            }
        }

        /// <summary> Initialize the text of the source line containing the error.
        /// 
        /// </summary>
        /// <param name="lineSource">the text of the source line reponsible for the error.
        /// It should not be <tt>null</tt>.
        /// 
        /// </param>
        /// <throws>  IllegalStateException if the method is called more then once. </throws>
        public void InitLineSource (string lineSource)
        {
            if (lineSource == null)
                throw new ArgumentException ();
            if (this.m_LineSource != null)
                throw new ApplicationException ();
            this.m_LineSource = lineSource;
        }

        internal void RecordErrorOrigin (string sourceName, int lineNumber, string lineSource, int columnNumber)
        {
            if (sourceName != null) {
                InitSourceName (sourceName);
            }
            if (lineNumber != 0) {
                InitLineNumber (lineNumber);
            }
            if (lineSource != null) {
                InitLineSource (lineSource);
            }
            if (columnNumber != 0) {
                InitColumnNumber (columnNumber);
            }
            InitScriptStackTrace ();
        }

        private string m_SourceName;
        private int m_LineNumber;
        private string m_LineSource;
        private int m_ColumnNumber;

        internal object m_InterpreterStackInfo;
        internal int [] m_InterpreterLineData;

        private void InitScriptStackTrace () {
            m_ScriptStackTrace = Interpreter.GetStack (this);
        }
        private string m_ScriptStackTrace = null;
        public string ScriptStackTrace
        {
            get
            {

                return m_ScriptStackTrace;
            }
        }
        
        public override string ToString ()
        {
            if (this.StackTrace != null)
                return Interpreter.getPatchedStack (this, this.StackTrace.ToString ());
            return ScriptStackTrace;
        }

    }
}