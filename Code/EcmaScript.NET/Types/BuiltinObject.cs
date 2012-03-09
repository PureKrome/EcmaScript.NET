//------------------------------------------------------------------------------
// <license file="NativeObject.cs">
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

    /// <summary> This class implements the Object native object.
    /// See ECMA 15.2.
    /// </summary>
    public class BuiltinObject : IdScriptableObject
    {
        override public string ClassName
        {
            get
            {
                return "Object";
            }

        }

        public BuiltinObject ()
        {
            ;
        }


        private static readonly object OBJECT_TAG = new object ();

        internal static void Init (IScriptable scope, bool zealed)
        {
            BuiltinObject obj = new BuiltinObject ();
            obj.ExportAsJSClass (MAX_PROTOTYPE_ID, scope, zealed);
        }

        public override string ToString ()
        {        
            return ScriptRuntime.DefaultObjectToString (this);
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
                case Id_toLocaleString:
                    arity = 0;
                    s = "toLocaleString";
                    break;
                case Id_valueOf:
                    arity = 0;
                    s = "valueOf";
                    break;
                case Id_hasOwnProperty:
                    arity = 1;
                    s = "hasOwnProperty";
                    break;
                case Id_propertyIsEnumerable:
                    arity = 1;
                    s = "propertyIsEnumerable";
                    break;
                case Id_isPrototypeOf:
                    arity = 1;
                    s = "isPrototypeOf";
                    break;
                case Id_toSource:
                    arity = 0;
                    s = "toSource";
                    break;

                case Id___defineGetter__:
                    arity = 2;
                    s = "__defineGetter__";
                    break;
                case Id___defineSetter__:
                    arity = 2;
                    s = "__defineSetter__";
                    break;
                case Id___lookupGetter__:
                    arity = 2;
                    s = "__lookupGetter__";
                    break;
                case Id___lookupSetter__:
                    arity = 2;
                    s = "__lookupSetter__";
                    break;

                default:
                    throw new ArgumentException (Convert.ToString (id));

            }
            InitPrototypeMethod (OBJECT_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (OBJECT_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;
            switch (id) {

                case Id_constructor: {
                        if (thisObj != null) {
                            // BaseFunction.construct will set up parent, proto
                            return f.Construct (cx, scope, args);
                        }
                        if (args.Length == 0 || args [0] == null || args [0] == Undefined.Value) {
                            return new BuiltinObject ();
                        }
                        return ScriptConvert.ToObject (cx, scope, args [0]);
                    }


                case Id_toLocaleString:
                // For now just alias toString
                case Id_toString: {
                        if (cx.HasFeature (Context.Features.ToStringAsSource)) {
                            string s = ScriptRuntime.defaultObjectToSource (cx, scope, thisObj, args);
                            int L = s.Length;
                            if (L != 0 && s [0] == '(' && s [L - 1] == ')') {
                                // Strip () that surrounds toSource
                                s = s.Substring (1, (L - 1) - (1));
                            }
                            return s;
                        }
                        return ScriptRuntime.DefaultObjectToString (thisObj);
                    }


                case Id_valueOf:
                    return thisObj;


                case Id_hasOwnProperty: {
                        bool result;
                        if (args.Length == 0) {
                            result = false;
                        }
                        else {
                            string s = ScriptRuntime.ToStringIdOrIndex (cx, args [0]);
                            if (s == null) {
                                int index = ScriptRuntime.lastIndexResult (cx);
                                result = thisObj.Has (index, thisObj);
                            }
                            else {
                                result = thisObj.Has (s, thisObj);
                            }
                        }
                        return result;
                    }


                case Id_propertyIsEnumerable: {
                        bool result;
                        if (args.Length == 0) {
                            result = false;
                        }
                        else {
                            string s = ScriptRuntime.ToStringIdOrIndex (cx, args [0]);
                            if (s == null) {
                                int index = ScriptRuntime.lastIndexResult (cx);
                                result = thisObj.Has (index, thisObj);
                                if (result && thisObj is ScriptableObject) {
                                    ScriptableObject so = (ScriptableObject)thisObj;
                                    int attrs = so.GetAttributes (index);
                                    result = ((attrs & ScriptableObject.DONTENUM) == 0);
                                }
                            }
                            else {
                                result = thisObj.Has (s, thisObj);
                                if (result && thisObj is ScriptableObject) {
                                    ScriptableObject so = (ScriptableObject)thisObj;
                                    int attrs = so.GetAttributes (s);
                                    result = ((attrs & ScriptableObject.DONTENUM) == 0);
                                }
                            }
                        }
                        return result;
                    }


                case Id_isPrototypeOf: {
                        bool result = false;
                        if (args.Length != 0 && args [0] is IScriptable) {
                            IScriptable v = (IScriptable)args [0];
                            do {
                                v = v.GetPrototype ();
                                if (v == thisObj) {
                                    result = true;
                                    break;
                                }
                            }
                            while (v != null);
                        }
                        return result;
                    }


                case Id_toSource:
                    return ScriptRuntime.defaultObjectToSource (cx, scope, thisObj, args);


                case Id___lookupGetter__:
                case Id___lookupSetter__: {
                        if (args.Length < 1)
                            return Undefined.Value;

                        string name = ScriptRuntime.ToStringIdOrIndex (cx, args [0]);
                        int index = (name != null) ? name.GetHashCode () : ScriptRuntime.lastIndexResult (cx);

                        // TODO: delegate way up to prototypes?
                        ScriptableObject so = (thisObj as ScriptableObject);
                        if (so == null) {
                            throw ScriptRuntime.TypeError ("this is not a scriptable object.");
                        }

                        if (id == Id___lookupGetter__) {
                            return so.LookupGetter (name);
                        }
                        else {
                            return so.LookupSetter (name);
                        }
                    }

                case Id___defineGetter__:
                case Id___defineSetter__: {
                        if (args.Length < 2 || args.Length > 0 && !(args [1] is ICallable)) {
                            object badArg = (args.Length > 1 ? args [1] : Undefined.Value);
                            throw ScriptRuntime.NotFunctionError (badArg);
                        }

                        string name = ScriptRuntime.ToStringIdOrIndex (cx, args [0]);
                        int index = (name != null) ? name.GetHashCode () : ScriptRuntime.lastIndexResult (cx);

                        // TODO: delegate way up to prototypes?
                        ScriptableObject so = (thisObj as ScriptableObject);
                        if (so == null) {
                            throw ScriptRuntime.TypeError ("this is not a scriptable object.");
                        }
                        ICallable getterOrSetter = (ICallable)args [1];

                        if (id == Id___defineGetter__) {
                            if (name == null) {
                                so.DefineGetter (index, getterOrSetter);
                            } else {
                                so.DefineGetter (name, getterOrSetter);
                            }
                        }
                        else {
                            if (name == null) {
                                so.DefineSetter (index, getterOrSetter);
                            } else {
                                so.DefineSetter (name, getterOrSetter);
                            }
                        }

                        return Undefined.Value;
                        break;
                    }

                default:
                    throw new ArgumentException (Convert.ToString (id));

            }
        }


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
                    case 7:
                        X = "valueOf";
                        id = Id_valueOf;
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
                    case 13:
                        X = "isPrototypeOf";
                        id = Id_isPrototypeOf;
                        break;
                    case 14:
                        c = s [0];
                        if (c == 'h') { X = "hasOwnProperty"; id = Id_hasOwnProperty; }
                        else if (c == 't') { X = "toLocaleString"; id = Id_toLocaleString; }
                        break;
                    case 16:
                        c = s [2];
                        if (c == 'd') {
                            c = s [8];
                            if (c == 'G') { X = "__defineGetter__"; id = Id___defineGetter__; }
                            else if (c == 'S') { X = "__defineSetter__"; id = Id___defineSetter__; }
                        }
                        else if (c == 'l') {
                            c = s [8];
                            if (c == 'G') { X = "__lookupGetter__"; id = Id___lookupGetter__; }
                            else if (c == 'S') { X = "__lookupSetter__"; id = Id___lookupSetter__; }
                        }
                        break;
                    case 20:
                        X = "propertyIsEnumerable";
                        id = Id_propertyIsEnumerable;
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
        private const int Id_toLocaleString = 3;
        private const int Id_valueOf = 4;
        private const int Id_hasOwnProperty = 5;
        private const int Id_propertyIsEnumerable = 6;
        private const int Id_isPrototypeOf = 7;
        private const int Id_toSource = 8;
        private const int Id___defineGetter__ = 9;
        private const int Id___defineSetter__ = 10;
        private const int Id___lookupGetter__ = 11;
        private const int Id___lookupSetter__ = 12;
        private const int MAX_PROTOTYPE_ID = 12;
        #endregion


    }
}
