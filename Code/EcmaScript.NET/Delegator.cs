//------------------------------------------------------------------------------
// <license file="Delegator.cs">
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

using EcmaScript.NET.Types;

namespace EcmaScript.NET
{

    /// <summary> This is a helper class for implementing wrappers around Scriptable
    /// objects. It implements the Function interface and delegates all
    /// invocations to a delegee Scriptable object. The normal use of this
    /// class involves creating a sub-class and overriding one or more of
    /// the methods.
    /// 
    /// A useful application is the implementation of interceptors,
    /// pre/post conditions, debugging.
    /// 
    /// </summary>	
    public class Delegator : IFunction
    {

        /// <summary> Retrieve the delegee.
        /// 
        /// </summary>
        /// <returns> the delegee
        /// </returns>
        /// <summary> Set the delegee.
        /// 
        /// </summary>
        /// <param name="obj">the delegee
        /// </param>		
        virtual public IScriptable Delegee
        {
            get
            {
                return obj;
            }

            set
            {
                this.obj = value;
            }

        }

        virtual public string ClassName
        {
            get
            {
                return obj.ClassName;
            }

        }


        virtual public IScriptable ParentScope
        {
            get
            {
                return obj.ParentScope;
            }

            set
            {
                obj.ParentScope = value;
            }

        }

        protected internal IScriptable obj = null;

        /// <summary> Create a Delegator prototype.
        /// 
        /// This constructor should only be used for creating prototype
        /// objects of Delegator.
        /// 
        /// </summary>

        public Delegator ()
        {
        }

        /// <summary> Create a new Delegator that forwards requests to a delegee
        /// Scriptable object.
        /// 
        /// </summary>
        /// <param name="obj">the delegee
        /// </param>		
        public Delegator (IScriptable obj)
        {
            this.obj = obj;
        }

        /// <summary> Crete new Delegator instance.
        /// The default implementation calls this.getClass().newInstance().
        /// 
        /// </summary>		
        protected internal virtual Delegator NewInstance ()
        {
            try {
                return (Delegator)System.Activator.CreateInstance (this.GetType ());
            }
            catch (Exception ex) {
                throw Context.ThrowAsScriptRuntimeEx (ex);
            }
        }

        public virtual object Get (string name, IScriptable start)
        {
            return obj.Get (name, start);
        }

        public virtual object Get (int index, IScriptable start)
        {
            return obj.Get (index, start);
        }

        public virtual bool Has (string name, IScriptable start)
        {
            return obj.Has (name, start);
        }

        public virtual bool Has (int index, IScriptable start)
        {
            return obj.Has (index, start);
        }

        public virtual object Put (string name, IScriptable start, object value)
        {
            return obj.Put (name, start, value);
        }

        public virtual object Put (int index, IScriptable start, object value)
        {
            return obj.Put (index, start, value);
        }

        public virtual void Delete (string name)
        {
            obj.Delete (name);
        }

        public virtual void Delete (int index)
        {
            obj.Delete (index);
        }

        public virtual IScriptable GetPrototype ()
        {
            return obj.GetPrototype ();
        }

        public virtual void SetPrototype (IScriptable prototype)
        {
            obj.SetPrototype (prototype);
        }

        public virtual object [] GetIds ()
        {
            return obj.GetIds ();
        }
        /// <summary> Note that this method does not get forwarded to the delegee if
        /// the <code>hint</code> parameter is null,
        /// <code>typeof(Scriptable)</code> or
        /// <code>typeof(Function)</code>. Instead the object
        /// itself is returned.
        /// 
        /// </summary>
        /// <param name="hint">the type hint
        /// </param>
        /// <returns> the default value
        /// 
        /// </returns>		
        public virtual object GetDefaultValue (Type hint)
        {
            return (hint == null
                    || hint == typeof (IScriptable)
                    || hint == typeof (IFunction))
                    ? this : obj.GetDefaultValue (hint);
        }

        public virtual bool HasInstance (IScriptable instance)
        {
            return obj.HasInstance (instance);
        }

        public virtual object Call (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            return ((IFunction)obj).Call (cx, scope, thisObj, args);
        }

        /// <summary> Note that if the <code>delegee</code> is <code>null</code>,
        /// this method creates a new instance of the Delegator itself
        /// rathert than forwarding the call to the
        /// <code>delegee</code>. This permits the use of Delegator
        /// prototypes.
        /// 
        /// </summary>
        /// <param name="cx">the current Context for this thread
        /// </param>
        /// <param name="scope">an enclosing scope of the caller except
        /// when the function is called from a closure.
        /// </param>
        /// <param name="args">the array of arguments
        /// </param>
        /// <returns> the allocated object
        /// 
        /// </returns>

        public virtual IScriptable Construct (Context cx, IScriptable scope, object [] args)
        {
            if (obj == null) {
                //this little trick allows us to declare prototype objects for
                //Delegators
                Delegator n = NewInstance ();
                IScriptable delegee;
                if (args.Length == 0) {
                    delegee = new BuiltinObject ();
                }
                else {
                    delegee = ScriptConvert.ToObject (cx, scope, args [0]);
                }
                n.Delegee = delegee;
                return n;
            }
            else {
                return ((IFunction)obj).Construct (cx, scope, args);
            }
        }
    }
}