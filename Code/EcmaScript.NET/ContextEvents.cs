//------------------------------------------------------------------------------
// <license file="ContextEvents.cs">
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

    public class ContextEventArgs : EventArgs
    {

        private Context m_Context = null;

        /// <summary>
        /// The underlying context
        /// </summary>
        public Context Context
        {
            get { return m_Context; }
        }

        /// <summary>
        /// Creates a new ContextEventArgs object
        /// </summary>
        /// <param name="cx"></param>
        public ContextEventArgs (Context cx)
        {
            m_Context = cx;
        }

    }

    public class ContextScriptableEventArgs : ContextEventArgs
    {


        private IScriptable m_Scope = null;

        public ContextScriptableEventArgs (Context cx, IScriptable scope)
            : base (cx)
        {
            m_Scope = scope;
        }

        public IScriptable Scope
        {
            get { return m_Scope; }
        }

    }


    /// <summary>
    /// 
    /// </summary>
    public class ContextWrapEventArgs : ContextScriptableEventArgs
    {

        private object m_Source = null;

        public object Source
        {
            get { return m_Source; }
        }

        private object m_Target = null;

        public object Target
        {
            get { return m_Target; }
            set { m_Target = null; }
        }


        private Type m_StaticType = null;

        public Type staticType
        {
            get { return m_StaticType; }
        }

        public ContextWrapEventArgs (Context cx, IScriptable scope, object obj, Type staticType)
            : base (cx, scope)
        {
            m_Source = obj;
            m_StaticType = staticType;
        }

    }

    public delegate void ContextEventHandler (object sender, ContextEventArgs e);

    public delegate void ContextWrapHandler (object sender, ContextWrapEventArgs e);

}
