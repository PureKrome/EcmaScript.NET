//------------------------------------------------------------------------------
// <license file="ContextFactory.cs">
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

    /// <summary> Factory class that Rhino runtime use to create new {@link Context}
    /// instances or to notify about Context execution.
    /// <p>
    /// When Rhino runtime needs to create new {@link Context} instance during
    /// execution of {@link Context#enter()} or {@link Context}, it will call
    /// {@link #makeContext()} of the current global ContextFactory.
    /// See {@link #getGlobal()} and {@link #initGlobal(ContextFactory)}.
    /// <p>
    /// It is also possible to use explicit ContextFactory instances for Context
    /// creation. This is useful to have a set of independent Rhino runtime
    /// instances under single JVM. See {@link #call(ContextAction)}.
    /// <p>	
    /// </summary>

    public class ContextFactory
    {
        /// <summary> Get global ContextFactory.
        /// 
        /// </summary>		
        public static ContextFactory Global
        {
            get
            {
                return global;
            }

        }

        /// <summary> Checks if this is a sealed ContextFactory.</summary>		
        virtual public bool Sealed
        {
            get
            {
                return zealed;
            }

        }
        private static volatile bool hasCustomGlobal;
        private static ContextFactory global = new ContextFactory ();

        private volatile bool zealed;


        /// <summary> Notify about newly created {@link Context} object.</summary>
        public event ContextEventHandler OnContextCreated;

        /// <summary> Notify that the specified {@link Context} instance is no longer
        /// associated with the current thread.
        /// </summary>
        public event ContextEventHandler OnContextReleased;

        /// <summary> Check if global factory was set.
        /// Return true to indicate that {@link #initGlobal(ContextFactory)} was
        /// already called and false to indicate that the global factory was not
        /// explicitly set.
        /// 
        /// </summary>		
        public static bool HasExplicitGlobal
        {
            get
            {
                return hasCustomGlobal;
            }
        }

        /// <summary> Set global ContextFactory.
        /// The method can only be called once.
        /// 
        /// </summary>		
        public static void InitGlobal (ContextFactory factory)
        {
            if (factory == null) {
                throw new ArgumentException ();
            }
            if (hasCustomGlobal) {
                throw new Exception ();
            }
            hasCustomGlobal = true;
            global = factory;
        }


        /// <summary> Execute top call to script or function.
        /// When the runtime is about to execute a script or function that will
        /// create the first stack frame with scriptable code, it calls this method
        /// to perform the real call. In this way execution of any script
        /// happens inside this function.
        /// </summary>
        protected internal virtual object DoTopCall (ICallable callable, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            return callable.Call (cx, scope, thisObj, args);
        }

        /// <summary> Implementation of
        /// {@link Context#observeInstructionCount(int instructionCount)}.
        /// This can be used to customize {@link Context} without introducing
        /// additional subclasses.
        /// </summary>
        protected internal virtual void ObserveInstructionCount (Context cx, int instructionCount)
        {
        }

        protected internal virtual void FireOnContextCreated (Context cx)
        {
            if (OnContextCreated != null)
                OnContextCreated (this, new ContextEventArgs (cx));
        }

        protected internal virtual void FireOnContextReleased (Context cx)
        {
            if (OnContextReleased != null)
                OnContextReleased (this, new ContextEventArgs (cx));
        }


        /// <summary> Seal this ContextFactory so any attempt to modify it like to add or
        /// remove its listeners will throw an exception.
        /// </summary>		
        public void Seal ()
        {
            CheckNotSealed ();
            zealed = true;
        }

        protected internal void CheckNotSealed ()
        {
            if (zealed)
                throw new Exception ();
        }

    }
}