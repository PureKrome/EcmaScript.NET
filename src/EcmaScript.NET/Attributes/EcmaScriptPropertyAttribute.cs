//------------------------------------------------------------------------------
// <license file="EcmaScriptPropertyAttribute.cs">
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

    [AttributeUsage (AttributeTargets.Property
        | AttributeTargets.Field)]
    public class EcmaScriptPropertyAttribute : Attribute
    {

        private string m_Name = string.Empty;

        /// <summary>
        /// Name of this property
        /// </summary>
        public string Name
        {
            get { return m_Name; }
        }

        private EcmaScriptPropertyAccess m_Access = EcmaScriptPropertyAccess.AsDeclared;

        /// <summary>
        /// Access
        /// </summary>
        public EcmaScriptPropertyAccess Access
        {
            get { return m_Access; }
        }

        public EcmaScriptPropertyAttribute (string name)
        {
            m_Name = name;
        }

        public EcmaScriptPropertyAttribute (string name, EcmaScriptPropertyAccess access)
        {
            m_Name = name;
            m_Access = access;
        }

    }

}
