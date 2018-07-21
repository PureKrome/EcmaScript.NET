//------------------------------------------------------------------------------
// <license file="EcmaScriptClassAttribute.cs">
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
using System.Text;

namespace EcmaScript.NET.Attributes
{

    [AttributeUsage (AttributeTargets.Class)]
    public class EcmaScriptClassAttribute : Attribute
    {

        private string m_Name = string.Empty;

        /// <summary>
        /// Name of this class
        /// </summary>
        public string Name
        {
            get { return m_Name; }
        }

        public EcmaScriptClassAttribute (string name)
        {
            m_Name = name;
        }

    }
}
