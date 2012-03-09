//------------------------------------------------------------------------------
// <license file="NativeWith.cs">
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

namespace EcmaScript.NET.Types
{

    /// <summary>
    /// This class implements the object lookup required for the
    /// <code>with</code> statement.
    /// It simply delegates every action to its prototype except
    /// for operations on its parent.
    /// </summary>
    public class BuiltinWith : IScriptable, IIdFunctionCall
    {

        public virtual string ClassName
        {
            get
            {
                return "With";
            }

        }

        public virtual IScriptable ParentScope
        {
            get
            {
                return parent;
            }
            set
            {
                this.parent = value;
            }
        }

        internal static void Init (IScriptable scope, bool zealed)
        {
            BuiltinWith obj = new BuiltinWith ();

            obj.ParentScope = scope;
            obj.SetPrototype (ScriptableObject.GetObjectPrototype (scope));

            IdFunctionObject ctor = new IdFunctionObject (obj, FTAG, Id_constructor, "With", 0, scope);
            ctor.MarkAsConstructor (obj);
            if (zealed) {
                ctor.SealObject ();
            }
            ctor.ExportAsScopeProperty ();
        }

        private BuiltinWith ()
        {
            ;
        }

        internal BuiltinWith (IScriptable parent, IScriptable prototype)
        {
            this.parent = parent;
            this.prototype = prototype;
        }

        public virtual bool Has (string id, IScriptable start)
        {
            return prototype.Has (id, prototype);
        }

        public virtual bool Has (int index, IScriptable start)
        {
            return prototype.Has (index, prototype);
        }

        public virtual object Get (string id, IScriptable start)
        {
            if (start == this)
                start = prototype;
            return prototype.Get (id, start);
        }

        public virtual object Get (int index, IScriptable start)
        {
            if (start == this)
                start = prototype;
            return prototype.Get (index, start);
        }

        public virtual object Put (string id, IScriptable start, object value)
        {
            if (start == this)
                start = prototype;
            return prototype.Put (id, start, value);
        }

        public virtual object Put (int index, IScriptable start, object value)
        {
            if (start == this)
                start = prototype;
            return prototype.Put (index, start, value);
        }

        public virtual void Delete (string id)
        {
            prototype.Delete (id);
        }

        public virtual void Delete (int index)
        {
            prototype.Delete (index);
        }

        public virtual IScriptable GetPrototype ()
        {
            return prototype;
        }

        public virtual void SetPrototype (IScriptable prototype)
        {
            this.prototype = prototype;
        }

        public virtual object [] GetIds ()
        {
            return prototype.GetIds ();
        }

        public virtual object GetDefaultValue (Type typeHint)
        {
            return prototype.GetDefaultValue (typeHint);
        }

        public virtual bool HasInstance (IScriptable value)
        {
            return prototype.HasInstance (value);
        }

        /// <summary>
        /// Must return null to continue looping or the final collection result.
        /// </summary>
        protected internal virtual object UpdateDotQuery (bool value)
        {
            // NativeWith itself does not support it
            throw new ApplicationException ();
        }

        public virtual object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (f.HasTag (FTAG)) {
                if (f.MethodId == Id_constructor) {
                    throw Context.ReportRuntimeErrorById ("msg.cant.call.indirect", "With");
                }
            }
            throw f.Unknown ();
        }

        internal static bool IsWithFunction (object functionObj)
        {
            if (functionObj is IdFunctionObject) {
                IdFunctionObject f = (IdFunctionObject)functionObj;
                return f.HasTag (FTAG) && f.MethodId == Id_constructor;
            }
            return false;
        }

        internal static object NewWithSpecial (Context cx, IScriptable scope, object [] args)
        {
            ScriptRuntime.checkDeprecated (cx, "With");
            scope = ScriptableObject.GetTopLevelScope (scope);
            BuiltinWith thisObj = new BuiltinWith ();
            thisObj.SetPrototype (args.Length == 0 ? ScriptableObject.getClassPrototype (scope, "Object") : ScriptConvert.ToObject (cx, scope, args [0]));
            thisObj.ParentScope = scope;
            return thisObj;
        }

        private static readonly object FTAG = new object ();

        private const int Id_constructor = 1;

        protected internal IScriptable prototype;
        protected internal IScriptable parent;

    }
}