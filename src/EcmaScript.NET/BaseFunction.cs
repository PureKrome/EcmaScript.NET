//------------------------------------------------------------------------------
// <license file="BaseFunction.cs">
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

    /// <summary> The base class for Function objects
    /// See ECMA 15.3.
    /// </summary>
    public class BaseFunction : IdScriptableObject, IFunction
    {
        override public string ClassName
        {
            get
            {
                return "Function";
            }

        }
        override protected internal int MaxInstanceId
        {
            get
            {
                return MAX_INSTANCE_ID;
            }

        }
        /// <summary> Make value as DontEnum, DontDelete, ReadOnly
        /// prototype property of this Function object
        /// </summary>
        virtual public object ImmunePrototypeProperty
        {
            set
            {
                if (isPrototypePropertyImmune) {
                    throw new Exception ();
                }
                prototypeProperty = (value != null) ? value : UniqueTag.NullValue;
                isPrototypePropertyImmune = true;
            }

        }
        virtual public int Arity
        {
            get
            {
                return 0;
            }

        }
        virtual public int Length
        {
            get
            {
                return 0;
            }

        }
        virtual public string FunctionName
        {
            get
            {
                return "";
            }

        }
        virtual internal object PrototypeProperty
        {
            get
            {
                object result = prototypeProperty;
                if (result == null) {
                    lock (this) {
                        result = prototypeProperty;
                        if (result == null) {
                            SetupDefaultPrototype ();
                            result = prototypeProperty;
                        }
                    }
                }
                else if (result == UniqueTag.NullValue) {
                    result = null;
                }
                return result;
            }

        }
        private object Arguments
        {
            get
            {
                // <Function name>.arguments is deprecated, so we use a slow
                // way of getting it that doesn't add to the invocation cost.
                // TODO: add warning, error based on version
                object value = DefaultGet ("arguments");
                if (value != UniqueTag.NotFound) {
                    // Should after changing <Function name>.arguments its
                    // activation still be available during Function call?
                    // This code assumes it should not:
                    // defaultGet("arguments") != NOT_FOUND
                    // means assigned arguments
                    return value;
                }
                Context cx = Context.CurrentContext;
                BuiltinCall activation = ScriptRuntime.findFunctionActivation (cx, this);
                return (activation == null) ? null : activation.Get ("arguments", activation);
            }

        }

        private static readonly object FUNCTION_TAG = new object ();

        internal static void Init (IScriptable scope, bool zealed)
        {
            BaseFunction obj = new BaseFunction ();
            obj.isPrototypePropertyImmune = true;
            obj.ExportAsJSClass (MAX_PROTOTYPE_ID, scope, zealed);
        }

        public BaseFunction ()
        {
        }

        public BaseFunction (IScriptable scope, IScriptable prototype)
            : base (scope, prototype)
        {
        }

        /// <summary> Implements the instanceof operator for JavaScript Function objects.
        /// <p>
        /// <code>
        /// foo = new Foo();<br>
        /// foo instanceof Foo;  // true<br>
        /// </code>
        ///
        /// </summary>
        /// <param name="instance">The value that appeared on the LHS of the instanceof
        /// operator
        /// </param>
        /// <returns> true if the "prototype" property of "this" appears in
        /// value's prototype chain
        ///
        /// </returns>
        public override bool HasInstance (IScriptable instance)
        {
            object protoProp = ScriptableObject.GetProperty (this, "prototype");
            if (protoProp is IScriptable) {
                return ScriptRuntime.jsDelegatesTo (instance, (IScriptable)protoProp);
            }
            throw ScriptRuntime.TypeErrorById ("msg.instanceof.bad.prototype", FunctionName);
        }


        #region InstanceIds
        private const int Id_length = 1;
        private const int Id_arity = 2;
        private const int Id_name = 3;
        private const int Id_prototype = 4;
        private const int Id_arguments = 5;
        private const int MAX_INSTANCE_ID = 5;
        #endregion

        protected internal override int FindInstanceIdInfo (string s)
        {
            int id;
            #region Generated InstanceId Switch
        L0: {
                id = 0;
                string X = null;
                int c;
            L:
                switch (s.Length) {
                    case 4:
                        X = "name";
                        id = Id_name;
                        break;
                    case 5:
                        X = "arity";
                        id = Id_arity;
                        break;
                    case 6:
                        X = "length";
                        id = Id_length;
                        break;
                    case 9:
                        c = s [0];
                        if (c == 'a') { X = "arguments"; id = Id_arguments; }
                        else if (c == 'p') { X = "prototype"; id = Id_prototype; }
                        break;
                }
                if (X != null && X != s && !X.Equals (s))
                    id = 0;
            }
        EL0:

            #endregion

            if (id == 0)
                return base.FindInstanceIdInfo (s);

            int attr;
            switch (id) {

                case Id_length:
                case Id_arity:
                case Id_name:
                    attr = DONTENUM | READONLY | PERMANENT;
                    break;

                case Id_prototype:
                    // As of ECMA 15.3.2.1 prototype will be PERMANENT if it's not with "Function"
                    attr = (isPrototypePropertyImmune) ? DONTENUM | READONLY | PERMANENT : PERMANENT;
                    break;

                case Id_arguments:
                    attr = DONTENUM | PERMANENT;
                    break;

                default:
                    throw new Exception ();

            }
            return InstanceIdInfo (attr, id);
        }

        protected internal override string GetInstanceIdName (int id)
        {
            switch (id) {

                case Id_length:
                    return "length";
                case Id_arity:
                    return "arity";
                case Id_name:
                    return "name";
                case Id_prototype:
                    return "prototype";
                case Id_arguments:
                    return "arguments";
            }
            return base.GetInstanceIdName (id);
        }


        protected internal override object GetInstanceIdValue (int id)
        {
            switch (id) {
                case Id_length:
                    return Length;
                case Id_arity:
                    return Arity;
                case Id_name:
                    return FunctionName;
                case Id_prototype:
                    return PrototypeProperty;
                case Id_arguments:
                    return Arguments;
            }
            return base.GetInstanceIdValue (id);
        }

        protected internal override void SetInstanceIdValue (int id, object value)
        {
            if (id == Id_prototype) {
                if (!isPrototypePropertyImmune) {
                    prototypeProperty = (value != null) ? value : UniqueTag.NullValue;
                }
                return;
            }
            else if (id == Id_arguments) {
                if (value == UniqueTag.NotFound) {
                    // This should not be called since "arguments" is PERMANENT
                    Context.CodeBug ();
                }
                DefaultPut ("arguments", value);
            }
            base.SetInstanceIdValue (id, value);
        }

        protected internal override void FillConstructorProperties (IdFunctionObject ctor)
        {
            // Fix up bootstrapping problem: getPrototype of the IdFunctionObject
            // can not return Function.prototype because Function object is not
            // yet defined.
            ctor.SetPrototype (this);
            base.FillConstructorProperties (ctor);
        }

        protected internal override void InitPrototypeId (int id)
        {
            string s;
            int arity;
            switch (id) {

                case Id_constructor:
                    arity = 1;
                    s = "constructor";
                    break;

                case Id_toString:
                    arity = 1;
                    s = "toString";
                    break;

                case Id_toSource:
                    arity = 1;
                    s = "toSource";
                    break;

                case Id_apply:
                    arity = 2;
                    s = "apply";
                    break;

                case Id_call:
                    arity = 1;
                    s = "call";
                    break;

                default:
                    throw new ArgumentException (Convert.ToString (id));

            }
            InitPrototypeMethod (FUNCTION_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (FUNCTION_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;
            switch (id) {

                case Id_constructor:
                    return JsConstructor (cx, scope, args);


                case Id_toString: {
                        BaseFunction realf = RealFunction (thisObj, f);
                        int indent = ScriptConvert.ToInt32 (args, 0);
                        return realf.Decompile (indent, Decompiler.TO_STRING_FLAG);
                    }


                case Id_toSource: {
                        BaseFunction realf = RealFunction (thisObj, f);
                        int indent = 0;
                        int flags = Decompiler.TO_SOURCE_FLAG;
                        if (args.Length != 0) {
                            indent = ScriptConvert.ToInt32 (args [0]);
                            if (indent >= 0) {
                                flags = 0;
                            }
                            else {
                                indent = 0;
                            }
                        }
                        return realf.Decompile (indent, flags);
                    }


                case Id_apply:
                case Id_call:
                    return ScriptRuntime.applyOrCall (id == Id_apply, cx, scope, thisObj, args);
            }
            throw new ArgumentException (Convert.ToString (id));
        }

        private BaseFunction RealFunction (IScriptable thisObj, IdFunctionObject f)
        {
            object x = thisObj.GetDefaultValue (typeof (IFunction));
            if (x is BaseFunction) {
                return (BaseFunction)x;
            }
            throw ScriptRuntime.TypeErrorById ("msg.incompat.call", f.FunctionName);
        }

        protected internal virtual IScriptable GetClassPrototype()
        {
            object protoVal = PrototypeProperty;
            if (protoVal is IScriptable) {
                return (IScriptable)protoVal;
            }
            return getClassPrototype (this, "Object");
        }

        /// <summary> Should be overridden.</summary>
        public virtual object Call (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            return Undefined.Value;
        }

        public virtual IScriptable Construct (Context cx, IScriptable scope, object [] args)
        {
            IScriptable result = CreateObject (cx, scope);
            if (result != null) {
                object val = Call (cx, scope, result, args);
                if (val is IScriptable) {
                    result = (IScriptable)val;
                }
            }
            else {
                object val = Call (cx, scope, null, args);
                if (!(val is IScriptable)) {
                    // It is program error not to return Scriptable from
                    // the call method if createObject returns null.
                    throw new Exception ("Bad implementaion of call as constructor, name=" + FunctionName + " in " + GetType ().FullName);
                }
                result = (IScriptable)val;
                if (result.GetPrototype () == null) {
                    result.SetPrototype (GetClassPrototype ());
                }
                if (result.ParentScope == null) {
                    IScriptable parent = ParentScope;
                    if (result != parent) {
                        result.ParentScope = parent;
                    }
                }
            }
            return result;
        }

        /// <summary> Creates new script object.
        /// The default implementation of {@link #construct} uses the method to
        /// to get the value for <tt>thisObj</tt> argument when invoking
        /// {@link #call}.
        /// The methos is allowed to return <tt>null</tt> to indicate that
        /// {@link #call} will create a new object itself. In this case
        /// {@link #construct} will set scope and prototype on the result
        /// {@link #call} unless they are already set.
        /// </summary>
        public virtual IScriptable CreateObject (Context cx, IScriptable scope)
        {
            IScriptable newInstance = new BuiltinObject ();
            newInstance.SetPrototype (GetClassPrototype ());
            newInstance.ParentScope = ParentScope;
            return newInstance;
        }

        /// <summary> Decompile the source information associated with this js
        /// function/script back into a string.
        ///
        /// </summary>
        /// <param name="indent">How much to indent the decompiled result.
        ///
        /// </param>
        /// <param name="flags">Flags specifying format of decompilation output.
        /// </param>
        internal virtual string Decompile (int indent, int flags)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder ();
            bool justbody = (0 != (flags & Decompiler.ONLY_BODY_FLAG));
            if (!justbody) {
                sb.Append ("function ");
                sb.Append (FunctionName);
                sb.Append ("() {\n\t");
            }
            sb.Append ("[native code, arity=");
            sb.Append (Arity);
            sb.Append ("]\n");
            if (!justbody) {
                sb.Append ("}\n");
            }
            return sb.ToString ();
        }

        private void SetupDefaultPrototype ()
        {
            BuiltinObject obj = new BuiltinObject ();
            obj.DefineProperty ("constructor", this, ScriptableObject.DONTENUM);
            // put the prototype property into the object now, then in the
            // wacky case of a user defining a function Object(), we don't
            // get an infinite loop trying to find the prototype.
            prototypeProperty = obj;
            IScriptable proto = GetObjectPrototype (this);
            if (proto != obj) {
                // not the one we just made, it must remain grounded
                obj.SetPrototype (proto);
            }
        }

        private static object JsConstructor (Context cx, IScriptable scope, object [] args)
        {
            int arglen = args.Length;
            System.Text.StringBuilder sourceBuf = new System.Text.StringBuilder ();

            sourceBuf.Append ("function ");
            /* version != 1.2 Function constructor behavior -
            * print 'anonymous' as the function name if the
            * version (under which the function was compiled) is
            * less than 1.2... or if it's greater than 1.2, because
            * we need to be closer to ECMA.
            */
            if (cx.Version != Context.Versions.JS1_2) {
                sourceBuf.Append ("anonymous");
            }
            sourceBuf.Append ('(');

            // Append arguments as coma separated strings
            for (int i = 0; i < arglen - 1; i++) {
                if (i > 0) {
                    sourceBuf.Append (',');
                }
                sourceBuf.Append (ScriptConvert.ToString (args [i]));
            }
            sourceBuf.Append (") {");
            if (arglen != 0) {
                // append function body
                string funBody = ScriptConvert.ToString (args [arglen - 1]);
                sourceBuf.Append (funBody);
            }
            sourceBuf.Append ('}');
            string source = sourceBuf.ToString ();

            int [] linep = new int [1];
            string filename = Context.GetSourcePositionFromStack (linep);
            if (filename == null) {
                filename = "<eval'ed string>";
                linep [0] = 1;
            }

            string sourceURI = ScriptRuntime.makeUrlForGeneratedScript (false, filename, linep [0]);

            IScriptable global = ScriptableObject.GetTopLevelScope (scope);

            ErrorReporter reporter;
            reporter = DefaultErrorReporter.ForEval (cx.ErrorReporter);

            // Compile with explicit interpreter instance to force interpreter
            // mode.
            return cx.CompileFunction (global, source, new Interpreter (), reporter, sourceURI, 1, (object)null);
        }

        #region PrototypeIds
        private const int Id_constructor = 1;
        private const int Id_toString = 2;
        private const int Id_toSource = 3;
        private const int Id_apply = 4;
        private const int Id_call = 5;
        private const int MAX_PROTOTYPE_ID = 5;
        #endregion

        protected internal override int FindPrototypeId (string s)
        {
            int id;
            #region Generated PrototypeId Switch
        L0: {
                id = 0;
                string X = null;
                int c;
            L:
                switch (s.Length) {
                    case 4:
                        X = "call";
                        id = Id_call;
                        break;
                    case 5:
                        X = "apply";
                        id = Id_apply;
                        break;
                    case 8:
                        c = s [3];
                        if (c == 'o') { X = "toSource"; id = Id_toSource; }
                        else if (c == 't') { X = "toString"; id = Id_toString; }
                        break;
                    case 11:
                        X = "constructor";
                        id = Id_constructor;
                        break;
                }
                if (X != null && X != s && !X.Equals (s))
                    id = 0;
            }
        EL0:

            #endregion
            return id;
        }


        private object prototypeProperty;
        private bool isPrototypePropertyImmune;


    }
}

