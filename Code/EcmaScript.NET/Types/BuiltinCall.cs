//------------------------------------------------------------------------------
// <license file="NativeCall.cs">
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
using System.Runtime.InteropServices;

namespace EcmaScript.NET.Types
{

    /// <summary> This class implements the activation object.
    /// 
    /// See ECMA 10.1.6
    /// 
    /// </summary>	
    public sealed class BuiltinCall : IdScriptableObject
    {
        override public string ClassName
        {
            get
            {
                return "Call";
            }

        }


        private static readonly object CALL_TAG = new object ();

        internal static void Init (IScriptable scope, bool zealed)
        {
            BuiltinCall obj = new BuiltinCall ();
            obj.ExportAsJSClass (MAX_PROTOTYPE_ID, scope, zealed);
        }

        internal BuiltinCall ()
        {
        }

        internal BuiltinCall (BuiltinFunction function, IScriptable scope, object [] args)
        {
            this.function = function;

            ParentScope = scope;
            // leave prototype null

            this.originalArgs = (args == null) ? ScriptRuntime.EmptyArgs : args;

            // initialize values of arguments
            int paramAndVarCount = function.ParamAndVarCount;
            int paramCount = function.ParamCount;
            if (paramAndVarCount != 0) {
                for (int i = 0; i != paramCount; ++i) {
                    string name = function.getParamOrVarName (i);
                    object val = i < args.Length ? args [i] : Undefined.Value;
                    DefineProperty (name, val, PERMANENT);
                }
            }

            // initialize "arguments" property but only if it was not overriden by
            // the parameter with the same name
            if (!base.Has ("arguments", this)) {
                DefineProperty ("arguments", new Arguments (this), PERMANENT);
            }

            if (paramAndVarCount != 0) {
                for (int i = paramCount; i != paramAndVarCount; ++i) {
                    string name = function.getParamOrVarName (i);
                    if (!base.Has (name, this)) {
                        DefineProperty (name, Undefined.Value, PERMANENT);
                    }
                }
            }
        }

        protected internal override int FindPrototypeId (string s)
        {
            return s.Equals ("constructor") ? Id_constructor : 0;
        }

        #region PrototypeIds
        private const int Id_constructor = 1;
        private const int MAX_PROTOTYPE_ID = 1;
        #endregion

        protected internal override void InitPrototypeId (int id)
        {
            string s;
            int arity;
            if (id == Id_constructor) {
                arity = 1;
                s = "constructor";
            }
            else {
                throw new ArgumentException (Convert.ToString (id));
            }
            InitPrototypeMethod (CALL_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (CALL_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;
            if (id == Id_constructor) {
                if (thisObj != null) {
                    throw Context.ReportRuntimeErrorById ("msg.only.from.new", "Call");
                }
                ScriptRuntime.checkDeprecated (cx, "Call");
                BuiltinCall result = new BuiltinCall ();
                result.SetPrototype (GetObjectPrototype (scope));
                return result;
            }
            throw new ArgumentException (Convert.ToString (id));
        }



        internal BuiltinFunction function;
        internal object [] originalArgs;


        internal BuiltinCall parentActivationCall;


    }
}
