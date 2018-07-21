//------------------------------------------------------------------------------
// <license file="EcmaScriptThrow.cs">
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

    /// <summary> Java reflection of JavaScript exceptions.
    /// Instances of this class are thrown by the JavaScript 'throw' keyword.
    /// 
    /// </summary>

    public class EcmaScriptThrow : EcmaScriptException
    {

        /// <returns> the value wrapped by this exception
        /// </returns>
        public virtual object Value
        {
            get
            {
                return value;
            }

        }

        /// <summary>
        /// Create a JavaScript exception wrapping the given JavaScript value
        /// </summary>
        /// <param name="value">the JavaScript value thrown.</param>
        public EcmaScriptThrow (object value, string sourceName, int lineNumber)
        {
            RecordErrorOrigin (sourceName, lineNumber, null, 0);
            this.value = value;
        }

        public override string Message
        {
            get
            {
                IScriptable scriptable = (value as IScriptable);
                if (scriptable != null) {
                    // to prevent potential of evaluation and throwing more exceptions
                    return ScriptRuntime.DefaultObjectToString (scriptable);
                }
                return ScriptConvert.ToString (value);
            }
        }

        private object value;
    }
}