//------------------------------------------------------------------------------
// <license file="Namespace.cs">
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
using EcmaScript.NET.Types;

namespace EcmaScript.NET.Types.E4X
{


    internal class Namespace : IdScriptableObject
    {
        /// <summary> </summary>
        /// <returns>
        /// </returns>
        override public string ClassName
        {
            get
            {
                return "Namespace";
            }

        }
        override protected internal int MaxInstanceId
        {
            get
            {
                return base.MaxInstanceId + MAX_INSTANCE_ID;
            }
        }

        private static readonly object NAMESPACE_TAG = new object ();

        private XMLLib lib;
        private string prefix;
        private string uri;

        internal Namespace (XMLLib lib)
            : base (lib.GlobalScope, lib.namespacePrototype)
        {
            this.lib = lib;
        }

        public Namespace (XMLLib lib, string uri)
            : base (lib.GlobalScope, lib.namespacePrototype)
        {

            if (uri == null)
                throw new System.ArgumentException ();

            this.lib = lib;
            this.prefix = (uri.Length == 0) ? "" : null;
            this.uri = uri;
        }


        public Namespace (XMLLib lib, string prefix, string uri)
            : base (lib.GlobalScope, lib.namespacePrototype)
        {

            if (uri == null)
                throw new System.ArgumentException ();
            if (uri.Length == 0) {
                // prefix should be "" for empty uri
                if (prefix == null)
                    throw new System.ArgumentException ();
                if (prefix.Length != 0)
                    throw new System.ArgumentException ();
            }

            this.lib = lib;
            this.prefix = prefix;
            this.uri = uri;
        }

        public virtual void ExportAsJSClass (bool sealed_Renamed)
        {
            ExportAsJSClass (MAX_PROTOTYPE_ID, lib.GlobalScope, sealed_Renamed);
        }

        public virtual string Uri
        {
            get
            {
                return uri;
            }
        }

        /// <summary> </summary>
        /// <returns>
        /// </returns>
        public virtual string Prefix
        {
            get
            {
                return prefix;
            }
        }

        /// <summary> </summary>
        /// <returns>
        /// </returns>
        public override string ToString ()
        {
            return Uri;
        }

        /// <summary> </summary>
        /// <returns>
        /// </returns>
        public virtual string toLocaleString ()
        {
            return ToString ();
        }

        public override bool Equals (object obj)
        {
            if (!(obj is Namespace))
                return false;
            return equals ((Namespace)obj);
        }

        protected internal override object EquivalentValues (object value_Renamed)
        {
            if (!(value_Renamed is Namespace))
                return UniqueTag.NotFound;
            bool result = equals ((Namespace)value_Renamed);
            return result ? true : false;
        }

        private bool equals (Namespace n)
        {
            return Uri.Equals (n.Uri);
        }

        /// <summary> </summary>
        /// <param name="">hint
        /// </param>
        /// <returns>
        /// </returns>
        public override object GetDefaultValue (System.Type hint)
        {
            return Uri;
        }

        #region InstanceIds
        private const int Id_prefix = 1;
        private const int Id_uri = 2;
        private const int MAX_INSTANCE_ID = 2;
        #endregion

        protected internal override int FindInstanceIdInfo (string s)
        {
            int id;
            #region Generated InstanceId Switch
        L0: {
                id = 0;
                string X = null;
                int s_length = s.Length;
                if (s_length == 3) { X = "uri"; id = Id_uri; }
                else if (s_length == 6) { X = "prefix"; id = Id_prefix; }
                if (X != null && X != s && !X.Equals (s))
                    id = 0;
            }
        EL0:

            #endregion

            if (id == 0)
                return base.FindInstanceIdInfo (s);

            int attr;
            switch (id) {

                case Id_prefix:
                case Id_uri:
                    attr = PERMANENT | READONLY;
                    break;

                default:
                    throw new System.SystemException ();

            }
            return InstanceIdInfo (attr, base.MaxInstanceId + id);
        }
        // #/string_id_map#

        protected internal override string GetInstanceIdName (int id)
        {
            switch (id - base.MaxInstanceId) {

                case Id_prefix:
                    return "prefix";

                case Id_uri:
                    return "uri";
            }
            return base.GetInstanceIdName (id);
        }

        protected internal override object GetInstanceIdValue (int id)
        {
            switch (id - base.MaxInstanceId) {

                case Id_prefix:
                    if (prefix == null)
                        return Undefined.Value;
                    return prefix;

                case Id_uri:
                    return uri;
            }
            return base.GetInstanceIdValue (id);
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
        L0: {
                id = 0;
                string X = null;
                int c;
                int s_length = s.Length;
                if (s_length == 8) {
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

        protected internal override void InitPrototypeId (int id)
        {
            string s;
            int arity;
            switch (id) {

                case Id_constructor:
                    arity = 2;
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
                    throw new System.ArgumentException (System.Convert.ToString (id));

            }
            InitPrototypeMethod (NAMESPACE_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (NAMESPACE_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;
            switch (id) {

                case Id_constructor:
                    return jsConstructor (cx, (thisObj == null), args);

                case Id_toString:
                    return realThis (thisObj, f).ToString ();

                case Id_toSource:
                    return realThis (thisObj, f).js_toSource ();
            }
            throw new System.ArgumentException (System.Convert.ToString (id));
        }

        private Namespace realThis (IScriptable thisObj, IdFunctionObject f)
        {
            if (!(thisObj is Namespace))
                throw IncompatibleCallError (f);
            return (Namespace)thisObj;
        }

        private object jsConstructor (Context cx, bool inNewExpr, object [] args)
        {
            if (!inNewExpr && args.Length == 1) {
                return Namespace.Parse (lib, cx, args [0]);
            }

            if (args.Length == 0) {
                return Namespace.Parse (lib, cx);
            }
            else if (args.Length == 1) {
                return Namespace.Parse (lib, cx, args [0]);
            }
            else {
                return Namespace.Parse (lib, cx, args [0], args [1]);
            }
        }

        private string js_toSource ()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder ();
            sb.Append ('(');
            toSourceImpl (prefix, uri, sb);
            sb.Append (')');
            return sb.ToString ();
        }

        internal static void toSourceImpl (string prefix, string uri, System.Text.StringBuilder sb)
        {
            sb.Append ("new Namespace(");
            if (uri.Length == 0) {
                if (!"".Equals (prefix))
                    throw new System.ArgumentException (prefix);
            }
            else {
                sb.Append ('\'');
                if (prefix != null) {
                    sb.Append (ScriptRuntime.escapeString (prefix, '\''));
                    sb.Append ("', '");
                }
                sb.Append (ScriptRuntime.escapeString (uri, '\''));
                sb.Append ('\'');
            }
            sb.Append (')');
        }

        public static Namespace Parse (XMLLib lib, Context cx, Object prefixValue,
            Object uriValue)
        {
            String prefix;
            String uri;

            if (uriValue is QName) {
                QName qname = (QName)uriValue;
                uri = qname.Uri;
                if (uri == null) {
                    uri = qname.ToString ();
                }
            }
            else {
                uri = ScriptConvert.ToString (uriValue);
            }

            if (uri.Length == 0) {
                if (prefixValue == Undefined.Value) {
                    prefix = "";
                }
                else {
                    prefix = ScriptConvert.ToString (prefixValue);
                    if (prefix.Length != 0) {
                        throw ScriptRuntime.TypeError (
                            "Illegal prefix '" + prefix + "' for 'no namespace'.");
                    }
                }
            }
            else if (prefixValue == Undefined.Value) {
                prefix = "";
            }
            else if (!lib.IsXMLName (cx, prefixValue)) {
                prefix = "";
            }
            else {
                prefix = ScriptConvert.ToString (prefixValue);
            }

            return new Namespace (lib, prefix, uri);
        }

        internal static Namespace Parse (XMLLib lib, Context cx)
        {
            return new Namespace (lib, "", "");
        }
        internal static Namespace Parse (XMLLib lib, Context cx, Object uriValue)
        {
            String prefix;
            String uri;

            if (uriValue is Namespace) {
                Namespace ns = (Namespace)uriValue;
                prefix = ns.Prefix;
                uri = ns.Uri;
            }
            else if (uriValue is QName) {
                QName qname = (QName)uriValue;
                uri = qname.Uri;
                if (uri != null) {
                    prefix = qname.Prefix;
                }
                else {
                    uri = qname.ToString ();
                    prefix = null;
                }
            }
            else {
                uri = ScriptConvert.ToString (uriValue);
                prefix = (uri.Length == 0) ? "" : null;
            }

            return new Namespace (lib, prefix, uri);
        }

        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }

    }
}
