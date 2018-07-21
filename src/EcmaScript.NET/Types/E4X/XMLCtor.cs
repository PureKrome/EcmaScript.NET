//------------------------------------------------------------------------------
// <license file="XMLCtor.cs">
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
using System.Text;

namespace EcmaScript.NET.Types.E4X
{
    class XMLCtor : IdFunctionObject
    {

        private static readonly object XMLCTOR_TAG = new object ();

        private XMLLib lib = null;

        internal XMLCtor (XML xml, object tag, int id, int arity)
            : base (xml, tag, id, arity)
        {
            this.lib = xml.lib;
            ActivatePrototypeMap (MAX_FUNCTION_ID);
        }

        protected internal override int MaxInstanceId
        {
            get
            {
                return base.MaxInstanceId + MAX_INSTANCE_ID;
            }
        }

        #region PrototypeIds
        private const int Id_defaultSettings = 1;
        private const int Id_settings = 2;
        private const int Id_setSettings = 3;
        private const int MAX_FUNCTION_ID = 3;
        #endregion

        protected internal override int FindPrototypeId (System.String s)
        {
            int id;
            #region Generated PrototypeId Switch
        L0: {
                id = 0;
                string X = null;
                int s_length = s.Length;
                if (s_length == 8) { X = "settings"; id = Id_settings; }
                else if (s_length == 11) { X = "setSettings"; id = Id_setSettings; }
                else if (s_length == 15) { X = "defaultSettings"; id = Id_defaultSettings; }
                if (X != null && X != s && !X.Equals (s))
                    id = 0;
            }
        EL0:

            #endregion
            return id;
        }

        protected internal override void InitPrototypeId (int id)
        {
            System.String s;
            int arity;
            switch (id) {
                case Id_defaultSettings:
                    arity = 0;
                    s = "defaultSettings";
                    break;
                case Id_settings:
                    arity = 0;
                    s = "settings";
                    break;
                case Id_setSettings:
                    arity = 1;
                    s = "setSettings";
                    break;
                default:
                    throw new System.ArgumentException (System.Convert.ToString (id));
            }
            InitPrototypeMethod (XMLCTOR_TAG, id, s, arity);
        }

        public override System.Object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, System.Object [] args)
        {
            if (!f.HasTag (XMLCTOR_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;
            switch (id) {

                case Id_defaultSettings: {
                        lib.SetDefaultSettings ();
                        IScriptable obj = cx.NewObject (scope);
                        WriteSetting (obj);
                        return obj;
                    }

                case Id_settings: {
                        IScriptable obj = cx.NewObject (scope);
                        WriteSetting (obj);
                        return obj;
                    }

                case Id_setSettings: {
                        if (args.Length == 0 || args [0] == null || args [0] == Undefined.Value) {
                            lib.SetDefaultSettings ();
                        }
                        else if (args [0] is IScriptable) {
                            ReadSettings ((IScriptable)args [0]);
                        }
                        return Undefined.Value;
                    }
            }
            throw new System.ArgumentException (System.Convert.ToString (id));
        }


        #region InstanceIds
        private const int Id_ignoreComments = 1;
        private const int Id_ignoreProcessingInstructions = 2;
        private const int Id_ignoreWhitespace = 3;
        private const int Id_prettyIndent = 4;
        private const int Id_prettyPrinting = 5;
        private const int MAX_INSTANCE_ID = 5;
        #endregion


        protected internal override int FindInstanceIdInfo (System.String s)
        {
            int id;
            #region Generated InstanceId Switch
        L0: {
                id = 0;
                string X = null;
                int c;
            L:
                switch (s.Length) {
                    case 12:
                        X = "prettyIndent";
                        id = Id_prettyIndent;
                        break;
                    case 14:
                        c = s [0];
                        if (c == 'i') { X = "ignoreComments"; id = Id_ignoreComments; }
                        else if (c == 'p') { X = "prettyPrinting"; id = Id_prettyPrinting; }
                        break;
                    case 16:
                        X = "ignoreWhitespace";
                        id = Id_ignoreWhitespace;
                        break;
                    case 28:
                        X = "ignoreProcessingInstructions";
                        id = Id_ignoreProcessingInstructions;
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
                case Id_ignoreComments:
                case Id_ignoreProcessingInstructions:
                case Id_ignoreWhitespace:
                case Id_prettyIndent:
                case Id_prettyPrinting:
                    attr = PERMANENT | DONTENUM;
                    break;
                default:
                    throw new System.SystemException ();

            }
            return InstanceIdInfo (attr, base.MaxInstanceId + id);
        }

        protected internal override System.String GetInstanceIdName (int id)
        {
            switch (id - base.MaxInstanceId) {
                case Id_ignoreComments:
                    return "ignoreComments";
                case Id_ignoreProcessingInstructions:
                    return "ignoreProcessingInstructions";
                case Id_ignoreWhitespace:
                    return "ignoreWhitespace";
                case Id_prettyIndent:
                    return "prettyIndent";
                case Id_prettyPrinting:
                    return "prettyPrinting";
            }
            return base.GetInstanceIdName (id);
        }

        protected internal override System.Object GetInstanceIdValue (int id)
        {
            switch (id - base.MaxInstanceId) {

                case Id_ignoreComments:
                    return lib.ignoreComments;

                case Id_ignoreProcessingInstructions:
                    return lib.ignoreProcessingInstructions;

                case Id_ignoreWhitespace:
                    return lib.ignoreWhitespace;

                case Id_prettyIndent:
                    return lib.prettyIndent;

                case Id_prettyPrinting:
                    return lib.prettyPrinting;
            }
            return base.GetInstanceIdValue (id);
        }

        protected internal override void SetInstanceIdValue (int id, System.Object value_Renamed)
        {
            switch (id - base.MaxInstanceId) {
                case Id_ignoreComments:
                    lib.ignoreComments = ScriptConvert.ToBoolean (value_Renamed);
                    return;

                case Id_ignoreProcessingInstructions:
                    lib.ignoreProcessingInstructions = ScriptConvert.ToBoolean (value_Renamed);
                    return;

                case Id_ignoreWhitespace:
                    lib.ignoreWhitespace = ScriptConvert.ToBoolean (value_Renamed);
                    return;

                case Id_prettyIndent:
                    lib.prettyIndent = ScriptConvert.ToInt32 (value_Renamed);
                    return;

                case Id_prettyPrinting:
                    lib.prettyPrinting = ScriptConvert.ToBoolean (value_Renamed);
                    return;
            }
            base.SetInstanceIdValue (id, value_Renamed);
        }

        private void WriteSetting (IScriptable target)
        {
            for (int i = 1; i <= MAX_INSTANCE_ID; ++i) {
                int id = base.MaxInstanceId + i;
                string name = GetInstanceIdName (id);
                object value = GetInstanceIdValue (id);
                ScriptableObject.PutProperty (target, name, value);
            }
        }

        private void ReadSettings (IScriptable source)
        {
            for (int i = 1; i <= MAX_INSTANCE_ID; ++i) {
                int id = base.MaxInstanceId + i;
                string name = GetInstanceIdName (id);
                object value = ScriptableObject.GetProperty (source, name);
                if (value == UniqueTag.NotFound) {
                    continue;
                }
                switch (i) {

                    case Id_ignoreComments:
                    case Id_ignoreProcessingInstructions:
                    case Id_ignoreWhitespace:
                    case Id_prettyPrinting:
                        if (!(value is bool)) {
                            continue;
                        }
                        break;

                    case Id_prettyIndent:
                        if (!(CliHelper.IsNumber (value))) {
                            continue;
                        }
                        break;

                    default:
                        throw new System.SystemException ();

                }
                SetInstanceIdValue (id, value);
            }
        }

    }
}
