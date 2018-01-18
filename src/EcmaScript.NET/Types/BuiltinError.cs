//------------------------------------------------------------------------------
// <license file="NativeError.cs">
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
    /// The class of error objects
    /// 
    /// ECMA 15.11
    /// </summary>

    sealed class BuiltinError : IdScriptableObject
    {
        public BuiltinError () {
        }
        
        override public string ClassName
        {
            get
            {
                return "Error";
            }

        }

        private static readonly object ERROR_TAG = new object ();

        internal static void Init (IScriptable scope, bool zealed)
        {
            BuiltinError obj = new BuiltinError ();
            
            ScriptableObject.PutProperty (obj, "name", "Error");
            ScriptableObject.PutProperty (obj, "message", "");
            ScriptableObject.PutProperty (obj, "fileName", "");
            ScriptableObject.PutProperty (obj, "lineNumber", 0);
            
            // TODO: Implement as non-ecma feature
            ScriptableObject.PutProperty (obj, "stack", "NOT IMPLEMENTED");

            obj.ExportAsJSClass (MAX_PROTOTYPE_ID, scope, zealed
                , ScriptableObject.DONTENUM | ScriptableObject.READONLY | ScriptableObject.PERMANENT);
        }

        internal static BuiltinError make (Context cx, IScriptable scope, IdFunctionObject ctorObj, object [] args)
        {
            IScriptable proto = (IScriptable)(ctorObj.Get ("prototype", ctorObj));

            BuiltinError obj = new BuiltinError ();
            obj.SetPrototype (proto);
            obj.ParentScope = scope;

            if (args.Length >= 1) {
                ScriptableObject.PutProperty (obj, "message", ScriptConvert.ToString (args [0]));
                if (args.Length >= 2) {
                    ScriptableObject.PutProperty (obj, "fileName", args [1]);
                    if (args.Length >= 3) {
                        int line = ScriptConvert.ToInt32 (args [2]);
                        ScriptableObject.PutProperty (obj, "lineNumber", (object)line);
                    }
                }
            }
            return obj;
        }

        public override string ToString ()
        {
            return js_toString (this);
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

                case Id_toSource:
                    arity = 0;
                    s = "toSource";
                    break;

                default:
                    throw new ArgumentException (Convert.ToString (id));

            }
            InitPrototypeMethod (ERROR_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (ERROR_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;
            switch (id) {

                case Id_constructor:
                    return make (cx, scope, f, args);


                case Id_toString:
                    return js_toString (thisObj);


                case Id_toSource:
                    return js_toSource (cx, scope, thisObj);
            }
            throw new ArgumentException (Convert.ToString (id));
        }

        private static string js_toString (IScriptable thisObj)
        {
            return getString (thisObj, "name") + ": " + getString (thisObj, "message");
        }

        private static string js_toSource (Context cx, IScriptable scope, IScriptable thisObj)
        {
            // Emulation of SpiderMonkey behavior
            object name = ScriptableObject.GetProperty (thisObj, "name");
            object message = ScriptableObject.GetProperty (thisObj, "message");
            object fileName = ScriptableObject.GetProperty (thisObj, "fileName");
            object lineNumber = ScriptableObject.GetProperty (thisObj, "lineNumber");

            System.Text.StringBuilder sb = new System.Text.StringBuilder ();
            sb.Append ("(new ");
            if (name == UniqueTag.NotFound) {
                name = Undefined.Value;
            }
            sb.Append (ScriptConvert.ToString (name));
            sb.Append ("(");
            if (message != UniqueTag.NotFound || fileName != UniqueTag.NotFound || lineNumber != UniqueTag.NotFound) {
                if (message == UniqueTag.NotFound) {
                    message = "";
                }
                sb.Append (ScriptRuntime.uneval (cx, scope, message));
                if (fileName != UniqueTag.NotFound || lineNumber != UniqueTag.NotFound) {
                    sb.Append (", ");
                    if (fileName == UniqueTag.NotFound) {
                        fileName = "";
                    }
                    sb.Append (ScriptRuntime.uneval (cx, scope, fileName));
                    if (lineNumber != UniqueTag.NotFound) {
                        int line = ScriptConvert.ToInt32 (lineNumber);
                        if (line != 0) {
                            sb.Append (", ");
                            sb.Append (ScriptConvert.ToString (line));
                        }
                    }
                }
            }
            sb.Append ("))");
            return sb.ToString ();
        }

        private static string getString (IScriptable obj, string id)
        {
            object value = ScriptableObject.GetProperty (obj, id);
            if (value == UniqueTag.NotFound)
                return "";
            return ScriptConvert.ToString (value);
        }


        #region PrototypeIds
        private const int Id_constructor = 1;
        private const int Id_toString = 2;
        private const int Id_toSource = 3;
        private const int MAX_PROTOTYPE_ID = 3;
        #endregion

        protected internal override int FindPrototypeId (string s)
        {
            int id;
            #region Generated PrototypeId Switch
	L0: { id = 0; string X = null; int c;
	    int s_length = s.Length;
	    if (s_length==8) {
		c=s[3];
		if (c=='o') { X="toSource";id=Id_toSource; }
		else if (c=='t') { X="toString";id=Id_toString; }
	    }
	    else if (s_length==11) { X="constructor";id=Id_constructor; }
	    if (X!=null && X!=s && !X.Equals(s)) id = 0;
	}
	EL0:

            #endregion
            return id;
        }





    }
}
