//------------------------------------------------------------------------------
// <license file="NativeScript.cs">
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
    /// The JavaScript Script object.
    /// 
    /// Note that the C version of the engine uses XDR as the format used
    /// by freeze and thaw. Since this depends on the internal format of
    /// structures in the C runtime, we cannot duplicate it.
    /// 
    /// Since we cannot replace 'this' as a result of the compile method,
    /// will forward requests to execute to the nonnull 'script' field.
    /// 
    /// </summary>	
    class BuiltinScript : BaseFunction
    {
        /// <summary> Returns the name of this JavaScript class, "Script".</summary>
        override public string ClassName
        {
            get
            {
                return "Script";
            }

        }
        override public int Length
        {
            get
            {
                return 0;
            }

        }
        override public int Arity
        {
            get
            {
                return 0;
            }

        }


        private static readonly object SCRIPT_TAG = new object ();

        internal static new void Init (IScriptable scope, bool zealed)
        {
            BuiltinScript obj = new BuiltinScript (null);
            obj.ExportAsJSClass (MAX_PROTOTYPE_ID, scope, zealed);
        }

        private BuiltinScript (IScript script)
        {
            this.script = script;
        }

        public override object Call (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (script != null) {
                return script.Exec (cx, scope);
            }
            return Undefined.Value;
        }

        public override IScriptable Construct (Context cx, IScriptable scope, object [] args)
        {
            throw Context.ReportRuntimeErrorById ("msg.script.is.not.constructor");
        }

        internal override string Decompile (int indent, int flags)
        {
            if (script is BuiltinFunction) {
                return ((BuiltinFunction)script).Decompile (indent, flags);
            }
            return base.Decompile (indent, flags);
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
                    arity = 0;
                    s = "toString";
                    break;

                case Id_exec:
                    arity = 0;
                    s = "exec";
                    break;

                case Id_compile:
                    arity = 1;
                    s = "compile";
                    break;

                default:
                    throw new ArgumentException (Convert.ToString (id));

            }
            InitPrototypeMethod (SCRIPT_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (SCRIPT_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;
            switch (id) {

                case Id_constructor: {
                        string source = (args.Length == 0) ? "" : ScriptConvert.ToString (args [0]);
                        IScript script = compile (cx, source);
                        BuiltinScript nscript = new BuiltinScript (script);
                        ScriptRuntime.setObjectProtoAndParent (nscript, scope);
                        return nscript;
                    }


                case Id_toString: {
                        BuiltinScript real = realThis (thisObj, f);
                        IScript realScript = real.script;
                        if (realScript == null) {
                            return "";
                        }
                        return cx.DecompileScript (realScript, 0);
                    }


                case Id_exec: {
                        throw Context.ReportRuntimeErrorById ("msg.cant.call.indirect", "exec");
                    }


                case Id_compile: {
                        BuiltinScript real = realThis (thisObj, f);
                        string source = ScriptConvert.ToString (args, 0);
                        real.script = compile (cx, source);
                        return real;
                    }
            }
            throw new ArgumentException (Convert.ToString (id));
        }

        private static BuiltinScript realThis (IScriptable thisObj, IdFunctionObject f)
        {
            if (!(thisObj is BuiltinScript))
                throw IncompatibleCallError (f);
            return (BuiltinScript)thisObj;
        }

        private static IScript compile (Context cx, string source)
        {
            int [] linep = new int [] { 0 };
            string filename = Context.GetSourcePositionFromStack (linep);
            if (filename == null) {
                filename = "<Script object>";
                linep [0] = 1;
            }
            ErrorReporter reporter;
            reporter = DefaultErrorReporter.ForEval (cx.ErrorReporter);
            return cx.CompileString (source, null, reporter, filename, linep [0], (object)null);
        }


        protected internal override int FindPrototypeId (string s)
        {
            int id;
            #region Generated PrototypeId Switch
        L0: {
                id = 0;
                string X = null;
            L:
                switch (s.Length) {
                    case 4:
                        X = "exec";
                        id = Id_exec;
                        break;
                    case 7:
                        X = "compile";
                        id = Id_compile;
                        break;
                    case 8:
                        X = "toString";
                        id = Id_toString;
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

        #region PrototypeIds
        private const int Id_constructor = 1;
        private const int Id_toString = 2;
        private const int Id_compile = 3;
        private const int Id_exec = 4;
        private const int MAX_PROTOTYPE_ID = 4;
        #endregion

        private IScript script;



    }
}
