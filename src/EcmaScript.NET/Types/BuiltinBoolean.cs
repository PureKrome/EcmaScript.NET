//------------------------------------------------------------------------------
// <license file="NativeBoolean.cs">
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
    /// This class implements the Boolean native object.
    /// See ECMA 15.6.
    /// </summary>
    sealed class BuiltinBoolean : IdScriptableObject
    {

        public override string ClassName
        {
            get
            {
                return "Boolean";
            }

        }

        private static readonly object BOOLEAN_TAG = new object ();

        internal static void Init (IScriptable scope, bool zealed)
        {
            BuiltinBoolean obj = new BuiltinBoolean (false);
            obj.ExportAsJSClass (MAX_PROTOTYPE_ID, scope, zealed
                , ScriptableObject.DONTENUM | ScriptableObject.READONLY | ScriptableObject.PERMANENT);
        }

        private BuiltinBoolean (bool b)
        {
            booleanValue = b;
        }

        public override object GetDefaultValue (Type typeHint)
        {
            // This is actually non-ECMA, but will be proposed
            // as a change in round 2.
            if (typeHint == typeof (bool))
                return booleanValue;
            return base.GetDefaultValue (typeHint);
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

                case Id_valueOf:
                    arity = 0;
                    s = "valueOf";
                    break;

                default:
                    throw new ArgumentException (Convert.ToString (id));

            }
            InitPrototypeMethod (BOOLEAN_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (BOOLEAN_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;

            if (id == Id_constructor) {
                bool b = ScriptConvert.ToBoolean (args, 0);
                if (thisObj == null) {
                    return new BuiltinBoolean (b);
                }
                return b;
            }

            // The rest of Boolean.prototype methods require thisObj to be Boolean

            if (!(thisObj is BuiltinBoolean))
                throw IncompatibleCallError (f);
            bool value = ((BuiltinBoolean)thisObj).booleanValue;

            switch (id) {


                case Id_toString:
                    return value ? "true" : "false";


                case Id_toSource:
                    return value ? "(new Boolean(true))" : "(new Boolean(false))";

                case Id_valueOf:
                    return value;
            }
            throw new ArgumentException (Convert.ToString (id));
        }

        #region PrototypeIds
        private const int Id_constructor = 1;
        private const int Id_toString = 2;
        private const int Id_toSource = 3;
        private const int Id_valueOf = 4;
        private const int MAX_PROTOTYPE_ID = 4;
        #endregion

        protected internal override int FindPrototypeId (string s)
        {
            int id;
            #region Generated PrototypeId Switch
        L0: {
                id = 0;
                string X = null;
                int c;
                int s_length = s.Length;
                if (s_length == 7) { X = "valueOf"; id = Id_valueOf; }
                else if (s_length == 8) {
                    c = s [3];
                    if (c == 'o') { X = "toSource"; id = Id_toSource; }
                    else if (c == 't') { X = "toString"; id = Id_toString; }
                }
                else if (s_length == 11) { X = "constructor"; id = Id_constructor; }
                if (X != null && X != s && !X.Equals (s))
                    id = 0;
            }
        EL0:

            #endregion
            return id;
        }

        private bool booleanValue;

    }

}
