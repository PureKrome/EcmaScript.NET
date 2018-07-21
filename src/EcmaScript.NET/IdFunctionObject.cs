//------------------------------------------------------------------------------
// <license file="IdFunctionObject.cs">
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


    public class IdFunctionObject : BaseFunction
    {

        public IdFunctionObject ()
        {
            ;
        }

        override public int Arity
        {
            get
            {
                return arity;
            }

        }
        override public int Length
        {
            get
            {
                return Arity;
            }

        }
        override public string FunctionName
        {
            get
            {
                return (functionName == null) ? "" : functionName;
            }
        }

        public IdFunctionObject (IIdFunctionCall idcall, object tag, int id, int arity)
        {
            if (arity < 0)
                throw new ArgumentException ();

            this.idcall = idcall;
            this.tag = tag;
            this.m_MethodId = id;
            this.arity = arity;
            if (arity < 0)
                throw new ArgumentException ();
        }

        public IdFunctionObject (IIdFunctionCall idcall, object tag, int id, string name, int arity, IScriptable scope)
            : base (scope, null)
        {

            if (arity < 0)
                throw new ArgumentException ();
            if (name == null)
                throw new ArgumentException ();

            this.idcall = idcall;
            this.tag = tag;
            this.m_MethodId = id;
            this.arity = arity;
            this.functionName = name;
        }

        public virtual void InitFunction (string name, IScriptable scope)
        {
            if (name == null)
                throw new ArgumentException ();
            if (scope == null)
                throw new ArgumentException ();
            this.functionName = name;
            ParentScope = scope;
        }

        public bool HasTag (object tag)
        {
            return this.tag == tag;
        }

        public int MethodId
        {
            get
            {
                return m_MethodId;
            }
        }

        public void MarkAsConstructor (IScriptable prototypeProperty)
        {
            useCallAsConstructor = true;
            ImmunePrototypeProperty = prototypeProperty;
        }

        public void AddAsProperty (IScriptable target)
        {
            AddAsProperty (target, ScriptableObject.DONTENUM);
        }

        public void AddAsProperty (IScriptable target, int attributes)
        {
            ScriptableObject.DefineProperty (target, functionName, this, attributes);
        }        

        public virtual void ExportAsScopeProperty ()
        {
            AddAsProperty (ParentScope);
        }

        public virtual void ExportAsScopeProperty (int attributes)
        {
            AddAsProperty (ParentScope, attributes);
        }

        public override IScriptable GetPrototype ()
        {
            // Lazy initialization of prototype: for native functions this
            // may not be called at all
            IScriptable proto = base.GetPrototype ();
            if (proto == null) {
                proto = GetFunctionPrototype (ParentScope);
                SetPrototype (proto);
            }
            return proto;
        }

        public override object Call (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            return idcall.ExecIdCall (this, cx, scope, thisObj, args);
        }

        public override IScriptable CreateObject (Context cx, IScriptable scope)
        {
            if (useCallAsConstructor) {
                return null;
            }
            // Throw error if not explicitly coded to be used as constructor,
            // to satisfy ECMAScript standard (see bugzilla 202019).
            // To follow current (2003-05-01) SpiderMonkey behavior, change it to:
            // return super.createObject(cx, scope);
            throw ScriptRuntime.TypeErrorById ("msg.not.ctor", functionName);
        }

        internal override string Decompile (int indent, int flags)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder ();
            bool justbody = (0 != (flags & Decompiler.ONLY_BODY_FLAG));
            if (!justbody) {
                sb.Append ("function ");
                sb.Append (FunctionName);
                sb.Append ("() { ");
            }
            sb.Append ("[native code for ");
            if (idcall is IScriptable) {
                IScriptable sobj = (IScriptable)idcall;
                sb.Append (sobj.ClassName);
                sb.Append ('.');
            }
            sb.Append (FunctionName);
            sb.Append (", arity=");
            sb.Append (Arity);
            sb.Append (justbody ? "]\n" : "] }\n");
            return sb.ToString ();
        }

        public Exception Unknown ()
        {
            // It is program error to call id-like methods for unknown function			
            return new Exception ("BAD FUNCTION ID=" + m_MethodId + " MASTER=" + idcall);
        }

        private IIdFunctionCall idcall;
        private object tag;
        private int m_MethodId;
        private int arity;
        private bool useCallAsConstructor;
        private string functionName;


    }
}