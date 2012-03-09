//------------------------------------------------------------------------------
// <license file="SecurityController.cs">
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

    /// <summary> This class describes the support needed to implement security.
    /// <p>
    /// Three main pieces of functionality are required to implement
    /// security for JavaScript. First, it must be possible to define
    /// classes with an associated security domain. (This security
    /// domain may be any object incorporating notion of access
    /// restrictions that has meaning to an embedding; for a client-side
    /// JavaScript embedding this would typically be
    /// java.security.ProtectionDomain or similar object depending on an
    /// origin URL and/or a digital certificate.)
    /// Next it must be possible to get a security domain object that
    /// allows a particular action only if all security domains
    /// associated with code on the current Java stack allows it. And
    /// finally, it must be possible to execute script code with
    /// associated security domain injected into Java stack.
    /// <p>
    /// These three pieces of functionality are encapsulated in the
    /// SecurityController class.
    /// 
    /// </summary>	
    public abstract class SecurityController
    {

        private class AnonymousClassScript : IScript
        {
            public AnonymousClassScript (EcmaScript.NET.ICallable callable, EcmaScript.NET.IScriptable thisObj, object [] args, SecurityController enclosingInstance)
            {
                InitBlock (callable, thisObj, args, enclosingInstance);
            }
            private void InitBlock (EcmaScript.NET.ICallable callable, EcmaScript.NET.IScriptable thisObj, object [] args, SecurityController enclosingInstance)
            {
                this.callable = callable;
                this.thisObj = thisObj;
                this.args = args;
                this.enclosingInstance = enclosingInstance;
            }

            private EcmaScript.NET.ICallable callable;

            private EcmaScript.NET.IScriptable thisObj;

            private object [] args;
            private SecurityController enclosingInstance;
            public SecurityController Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }
            public virtual object Exec (Context cx, IScriptable scope)
            {
                return callable.Call (cx, scope, thisObj, args);
            }
        }
        private static SecurityController m_Global;

        // The method must NOT be public or protected
        internal static SecurityController Global
        {
            get
            {
                return m_Global;
            }
        }

        /// <summary> Check if global {@link SecurityController} was already installed.</summary>		
        public static bool HasGlobal ()
        {
            return m_Global != null;
        }

        /// <summary> Initialize global controller that will be used for all
        /// security-related operations. The global controller takes precedence
        /// over already installed {@link Context}-specific controllers and cause
        /// any subsequent call to
        /// {@link Context#setSecurityController(SecurityController)}
        /// to throw an exception.
        /// <p>
        /// The method can only be called once.
        /// 
        /// </summary>		
        public static void initGlobal (SecurityController controller)
        {
            if (controller == null)
                throw new ArgumentException ();
            if (m_Global != null) {
                throw new System.Security.SecurityException ("Cannot overwrite already installed global SecurityController");
            }
            m_Global = controller;
        }


        /// <summary> Get dynamic security domain that allows an action only if it is allowed
        /// by the current Java stack and <i>securityDomain</i>. If
        /// <i>securityDomain</i> is null, return domain representing permissions
        /// allowed by the current stack.
        /// </summary>
        public abstract object getDynamicSecurityDomain (object securityDomain);

        /// <summary> Call {@link
        /// Callable#call(Context cx, Scriptable scope, Scriptable thisObj,
        /// Object[] args)}
        /// of <i>callable</i> under restricted security domain where an action is
        /// allowed only if it is allowed according to the Java stack on the
        /// moment of the <i>execWithDomain</i> call and <i>securityDomain</i>.
        /// Any call to {@link #getDynamicSecurityDomain(Object)} during
        /// execution of <tt>callable.call(cx, scope, thisObj, args)</tt>
        /// should return a domain incorporate restrictions imposed by
        /// <i>securityDomain</i> and Java stack on the moment of callWithDomain
        /// invocation.
        /// <p>
        /// </summary>
        public abstract object callWithDomain (object securityDomain, Context cx, ICallable callable, IScriptable scope, IScriptable thisObj, object [] args);


    }
}