//------------------------------------------------------------------------------
// <license file="QName.cs">
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

    /// <summary> Class QName
    /// 
    /// </summary>
    [Serializable]
    sealed class QName : IdScriptableObject
    {

        override public string ClassName
        {
            get
            {
                return "QName";
            }

        }
        protected override internal int MaxInstanceId
        {
            get
            {
                return base.MaxInstanceId + MAX_INSTANCE_ID;
            }

        }

        private static readonly object QNAME_TAG = new object ();

        internal XMLLib lib;
        private string prefix;
        private string localName;
        private string uri;

        internal QName (XMLLib lib)
            : base (lib.GlobalScope, lib.qnamePrototype)
        {
            this.lib = lib;
        }

        internal QName (XMLLib lib, QName qname)
            :
            this (lib, qname.Uri, qname.LocalName, qname.Prefix)
        {
            ;
        }

        internal QName (XMLLib lib, string uri, string localName, string prefix)
            : base (lib.GlobalScope, lib.qnamePrototype)
        {
            if (localName == null)
                throw new System.ArgumentException ();
            this.lib = lib;
            this.uri = uri;
            this.prefix = prefix;
            this.localName = localName;
        }

        internal void ExportAsJSClass (bool zealed)
        {
            ExportAsJSClass (MAX_PROTOTYPE_ID, lib.GlobalScope, zealed);
        }

        /// <summary> </summary>
        /// <returns>
        /// </returns>
        public override string ToString ()
        {
            string result;

            if (uri == null) {
                result = string.Concat ("*::", localName);
            }
            else if (uri.Length == 0) {
                result = localName;
            }
            else {
                result = uri + "::" + localName;
            }

            return result;
        }

        public string LocalName
        {
            get
            {
                return localName;
            }
        }

        internal string Prefix
        {
            get
            {
                return (prefix == null) ? prefix : "";
            }
        }

        internal string Uri
        {
            get
            {
                return uri;
            }
        }

        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }

        public override bool Equals (object obj)
        {
            QName qName = (obj as QName);
            if (qName == null)
                return false;
            return Equals (qName);
        }

        protected internal override object EquivalentValues (object value)
        {
            QName qName = (value as QName);
            if (qName == null)
                return UniqueTag.NotFound;
            return Equals (qName);
        }

        private bool Equals (QName q)
        {
            return CliHelper.Equals (LocalName, q.LocalName)
                && CliHelper.Equals (Uri, q.Uri);
        }

        /// <summary> </summary>
        /// <param name="">hint
        /// </param>
        /// <returns>
        /// </returns>
        public override object GetDefaultValue (System.Type hint)
        {
            return ToString ();
        }

        #region InstanceIds
        private const int Id_localName = 1;
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
                else if (s_length == 9) { X = "localName"; id = Id_localName; }
                if (X != null && X != s && !X.Equals (s))
                    id = 0;
            }
        EL0:

            #endregion

            if (id == 0)
                return base.FindInstanceIdInfo (s);

            int attr;
            switch (id) {

                case Id_localName:
                case Id_uri:
                    attr = PERMANENT | READONLY;
                    break;

                default:
                    throw new System.SystemException ();

            }
            return InstanceIdInfo (attr, base.MaxInstanceId + id);
        }

        protected internal override string GetInstanceIdName (int id)
        {
            switch (id - base.MaxInstanceId) {

                case Id_localName:
                    return "localName";

                case Id_uri:
                    return "uri";
            }
            return base.GetInstanceIdName (id);
        }

        protected internal override object GetInstanceIdValue (int id)
        {
            switch (id - base.MaxInstanceId) {

                case Id_localName:
                    return localName;

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
                    throw new ArgumentException (System.Convert.ToString (id));

            }
            InitPrototypeMethod (QNAME_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (QNAME_TAG)) {
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
            throw new ArgumentException (System.Convert.ToString (id));
        }

        private QName realThis (IScriptable thisObj, IdFunctionObject f)
        {
            if (!(thisObj is QName))
                throw IncompatibleCallError (f);
            return (QName)thisObj;
        }

        private object jsConstructor (Context cx, bool inNewExpr, object [] args)
        {
            if (!inNewExpr && args.Length == 1) {
                return QName.Parse (lib, cx, args [0]);
            }
            if (args.Length == 0) {
                return QName.Parse (lib, cx, Undefined.Value);
            }
            else if (args.Length == 1) {
                return QName.Parse (lib, cx, args [0]);
            }
            else {
                return QName.Parse (lib, cx, args [0], args [1]);
            }
        }

        internal static QName Parse (XMLLib lib, Context cx, object value)
        {
            QName result;

            if (value is QName) {
                QName qname = (QName)value;
                result = new QName (lib, qname.Uri, qname.LocalName,
                    qname.Prefix);
            }
            else {
                result = Parse (lib, cx, ScriptConvert.ToString (value));
            }

            return result;
        }

        internal static QName Parse (XMLLib lib, Context cx, string localName)
        {
            if (localName == null)
                throw new ArgumentNullException ("localName");

            String uri;
            String prefix;

            if ("*".Equals (localName)) {
                uri = null;
                prefix = null;
            }
            else {
                Namespace ns = lib.GetDefaultNamespace (cx);
                uri = ns.Uri;
                prefix = ns.Prefix;
            }

            return new QName (lib, uri, localName, prefix);
        }
        internal static QName Parse (XMLLib lib, Context cx, object namespaceValue, object nameValue)
        {
            String uri;
            String localName;
            String prefix;

            if (nameValue is QName) {
                QName qname = (QName)nameValue;
                localName = qname.LocalName;
            }
            else {
                localName = ScriptConvert.ToString (nameValue);
            }

            Namespace ns;
            if (namespaceValue == Undefined.Value) {
                if ("*".Equals (localName)) {
                    ns = null;
                }
                else {
                    ns = lib.GetDefaultNamespace (cx);
                }
            }
            else if (namespaceValue == null) {
                ns = null;
            }
            else if (namespaceValue is Namespace) {
                ns = (Namespace)namespaceValue;
            }
            else {
                ns = Namespace.Parse (lib, cx, namespaceValue);
            }

            if (ns == null) {
                uri = null;
                prefix = null;
            }
            else {
                uri = ns.Uri;
                prefix = ns.Prefix;
            }

            return new QName (lib, uri, localName, prefix);
        }

        private string js_toSource ()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder ();
            sb.Append ('(');
            toSourceImpl (uri, localName, prefix, sb);
            sb.Append (')');
            return sb.ToString ();
        }

        private static void toSourceImpl (string uri, string localName, string prefix, System.Text.StringBuilder sb)
        {
            sb.Append ("new QName(");
            if (uri == null && prefix == null) {
                if (!"*".Equals (localName)) {
                    sb.Append ("null, ");
                }
            }
            else {
                Namespace.toSourceImpl (prefix, uri, sb);
                sb.Append (", ");
            }
            sb.Append ('\'');
            sb.Append (ScriptRuntime.escapeString (localName, '\''));
            sb.Append ("')");
        }

    }
}
