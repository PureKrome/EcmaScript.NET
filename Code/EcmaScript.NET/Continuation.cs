//------------------------------------------------------------------------------
// <license file="Continuation.cs">
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
using EcmaScript.NET;

namespace EcmaScript.NET
{


    public sealed class Continuation : IdScriptableObject, IFunction
    {
        public object Implementation
        {
            get
            {
                return implementation;
            }

        }
        override public string ClassName
        {
            get
            {
                return "Continuation";
            }

        }


        private static readonly object FTAG = new object ();

        private object implementation;

        public static void Init (IScriptable scope, bool zealed)
        {
            Continuation obj = new Continuation ();
            obj.ExportAsJSClass (MAX_PROTOTYPE_ID, scope, zealed);
        }

        public void initImplementation (object implementation)
        {
            this.implementation = implementation;
        }

        public IScriptable Construct (Context cx, IScriptable scope, object [] args)
        {
            throw Context.ReportRuntimeError ("Direct call is not supported");
        }

        public object Call (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            return Interpreter.restartContinuation (this, cx, scope, args);
        }

        public static bool IsContinuationConstructor (IdFunctionObject f)
        {
            if (f.HasTag (FTAG) && f.MethodId == Id_constructor) {
                return true;
            }
            return false;
        }

        protected internal override void InitPrototypeId (int id)
        {
            string s;
            int arity;
            switch (id) {

                case Id_constructor:
                    arity = 0;
                    s = "constructor";
                    break;

                default:
                    throw new ArgumentException (Convert.ToString (id));

            }
            InitPrototypeMethod (FTAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (FTAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;
            switch (id) {

                case Id_constructor:
                    throw Context.ReportRuntimeError ("Direct call is not supported");
            }
            throw new ArgumentException (Convert.ToString (id));
        }

        // #string_id_map#

        protected internal override int FindPrototypeId (string s)
        {
            int id;
            #region Generated PrototypeId Switch
        L0: {
                id = 0;
                string X = null;
                if (s.Length == 11) { X = "constructor"; id = Id_constructor; }
                if (X != null && X != s && !X.Equals (s))
                    id = 0;
            }
        EL0:
            #endregion
            return id;
        }

        #region PrototypeIds
        private const int Id_constructor = 1;
        private const int MAX_PROTOTYPE_ID = 1;
        #endregion

    }
}
