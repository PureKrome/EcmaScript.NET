using System;
using System.Collections.Generic;
using System.Text;

namespace EcmaScript.NET.Types
{
    public class BuiltinGlobalObject : IdScriptableObject
    {

        public override string ClassName
        {
            get
            {
                return "global";
            }
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            int id = f.MethodId;
            switch (id) {
                case Id_print:
                    for (int i = 0; i < args.Length; i++) {
                        if (i > 0)
                            Console.Out.Write (" ");
                        Console.Out.Write (ScriptConvert.ToString (args [i]));
                    }
                    Console.Out.WriteLine ();
                    return Undefined.Value;

                case Id_version:
                    if (args.Length > 0) {
                        if (CliHelper.IsNumber (args [0])) {
                            int newVer = (int)ScriptConvert.ToNumber (args [0]);
                            if (Context.IsValidLanguageVersion (newVer)) {
                                cx.Version = Context.ToValidLanguageVersion (newVer);
                            }
                        }
                    }
                    return (int)cx.Version;
                case Id_options:
                    StringBuilder sb = new StringBuilder ();
                    if (cx.HasFeature (Context.Features.Strict))
                        sb.Append ("strict");
                    return sb.ToString ();
                case Id_gc:
                    GC.Collect ();
                    return Undefined.Value;
            }
            throw f.Unknown ();
        }


        protected internal override void InitPrototypeId (int id)
        {
            string s;
            int arity;
            switch (id) {
                case Id_print:                    
                    arity = 0;
                    s = "print";                    
                    break;
                case Id_version:                    
                    arity = 0;
                    s = "version";                    
                    break;
                case Id_options:
                    arity = 0;
                    s = "options";
                    break;
                case Id_gc:
                    arity = 0;
                    s = "gc";
                    break;
                default:
                    throw new ArgumentException (Convert.ToString (id));

            }
            
            InitPrototypeMethod (GLOBALOBJECT_TAG, id, s, arity);               
        }

        internal override object [] GetIds (bool getAll)
        {
            return base.GetIds (getAll);
        }

        public void Init (IScriptable scope, bool zealed)
        {
            base.ActivatePrototypeMap (MAX_PROTOTYPE_ID);
        }

        private static readonly object GLOBALOBJECT_TAG = new object ();

        protected internal override int FindPrototypeId (string s)
        {
            int id;
            #region Generated PrototypeId Switch
        L0: {
                id = 0;
                string X = null;
                int c;
                int s_length = s.Length;
                if (s_length == 2) { if (s [0] == 'g' && s [1] == 'c') { id = Id_gc; goto EL0; } }
                else if (s_length == 5) { X = "print"; id = Id_print; }
                else if (s_length == 7) {
                    c = s [0];
                    if (c == 'o') { X = "options"; id = Id_options; }
                    else if (c == 'v') { X = "version"; id = Id_version; }
                }
                if (X != null && X != s && !X.Equals (s))
                    id = 0;
            }
        EL0:

            #endregion

            //id = HideIfNotSet (id);
            
            return id;
        }
        
        int HideIfNotSet (int id) {
            id = HideIfNotSet (Context.Features.NonEcmaPrintFunction, id, Id_print);
            id = HideIfNotSet (Context.Features.NonEcmaOptionsFunction, id, Id_options);
            id = HideIfNotSet (Context.Features.NonEcmaVersionFunction, id, Id_version);
            id = HideIfNotSet (Context.Features.NonEcmaGcFunction, id, Id_gc);
            return id;        
        }
        
        int HideIfNotSet (Context.Features feature, int id, int requiredId)
        {
            if (id != requiredId)
                return id;
            if (Context.CurrentContext.HasFeature (feature))
                return id;
            return 0;
        }

        #region PrototypeIds
        private const int Id_print = 1;
        private const int Id_version = 2;
        private const int Id_options = 3;
        private const int Id_gc = 4;
        private const int MAX_PROTOTYPE_ID = 4;
        #endregion

    }
}
