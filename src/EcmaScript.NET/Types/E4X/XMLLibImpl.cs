//------------------------------------------------------------------------------
// <license file="XMLLib.cs">
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
using System.Xml;
using System.Text;

namespace EcmaScript.NET.Types.E4X
{

    class XMLLib
    {

        private IScriptable globalScope;

        public IScriptable GlobalScope
        {
            get { return globalScope; }
        }

        private XMLLib (IScriptable globalScope)
        {
            this.globalScope = globalScope;
        }

        // Environment settings
        internal bool ignoreComments;
        internal bool ignoreProcessingInstructions;
        internal bool ignoreWhitespace;
        internal bool prettyPrinting;
        internal int prettyIndent;

        // prototypes
        internal XML xmlPrototype;
        internal XMLList xmlListPrototype;
        internal Namespace namespacePrototype;
        internal QName qnamePrototype;

        internal void SetDefaultSettings ()
        {
            ignoreComments = true;
            ignoreProcessingInstructions = true;
            ignoreWhitespace = true;
            prettyPrinting = true;
            prettyIndent = 2;
        }

        private static readonly object XML_LIB_KEY = new object ();

        public static XMLLib ExtractFromScopeOrNull (IScriptable scope)
        {
            ScriptableObject so = ScriptRuntime.getLibraryScopeOrNull (scope);
            if (so == null) {
                return null;
            }

            return (XMLLib)so.GetAssociatedValue (XML_LIB_KEY);
        }

        public static XMLLib ExtractFromScope (IScriptable scope)
        {
            XMLLib lib = ExtractFromScopeOrNull (scope);
            if (lib != null) {
                return lib;
            }
            string msg = ScriptRuntime.GetMessage ("msg.XML.not.available");
            throw Context.ReportRuntimeError (msg);
        }

        internal XMLLib BindToScope (IScriptable scope)
        {
            ScriptableObject so = ScriptRuntime.getLibraryScopeOrNull (scope);
            if (so == null) {
                // standard library should be initialized at this point
                throw new Exception ();
            }
            return (XMLLib)so.AssociateValue (XML_LIB_KEY, this);
        }

        public static void Init (IScriptable scope, bool zealed)
        {
            XMLLib impl = new XMLLib (scope);
            impl.SetDefaultSettings ();
            impl.BindToScope (scope);

            impl.xmlPrototype = XML.CreateEmptyXml (impl);
            impl.xmlPrototype.ExportAsJSClass (zealed);

            impl.xmlListPrototype = new XMLList (impl);
            impl.xmlListPrototype.ExportAsJSClass (zealed);


            impl.qnamePrototype = new QName (impl);
            impl.qnamePrototype.ExportAsJSClass (zealed);

            impl.namespacePrototype = new Namespace (impl);
            impl.namespacePrototype.ExportAsJSClass (zealed);
        }

        /**
             * See E4X 13.1.2.1.
             */
        public bool IsXMLName (Context cx, object value)
        {
            string name;
            try {
                name = ScriptConvert.ToString (value);
            }
            catch (EcmaScriptError ee) {
                if ("TypeError".Equals (ee.Name)) {
                    return false;
                }
                throw ee;
            }

            // See http://w3.org/TR/xml-names11/#NT-NCName
            int length = name.Length;
            if (length != 0) {
                if (IsNCNameStartChar (name [0])) {
                    for (int i = 1; i != length; ++i) {
                        if (!IsNCNameChar (name [i])) {
                            return false;
                        }
                    }
                    return true;
                }
            }

            return false;
        }

        private static bool IsNCNameStartChar (int c)
        {
            if ((c & ~0x7F) == 0) {
                // Optimize for ASCII and use A..Z < _ < a..z
                if (c >= 'a') {
                    return c <= 'z';
                }
                else if (c >= 'A') {
                    if (c <= 'Z') {
                        return true;
                    }
                    return c == '_';
                }
            }
            else if ((c & ~0x1FFF) == 0) {
                return (0xC0 <= c && c <= 0xD6)
                    || (0xD8 <= c && c <= 0xF6)
                    || (0xF8 <= c && c <= 0x2FF)
                    || (0x370 <= c && c <= 0x37D)
                    || 0x37F <= c;
            }
            return (0x200C <= c && c <= 0x200D)
                || (0x2070 <= c && c <= 0x218F)
                || (0x2C00 <= c && c <= 0x2FEF)
                || (0x3001 <= c && c <= 0xD7FF)
                || (0xF900 <= c && c <= 0xFDCF)
                || (0xFDF0 <= c && c <= 0xFFFD)
                || (0x10000 <= c && c <= 0xEFFFF);
        }

        private static bool IsNCNameChar (int c)
        {
            if ((c & ~0x7F) == 0) {
                // Optimize for ASCII and use - < . < 0..9 < A..Z < _ < a..z
                if (c >= 'a') {
                    return c <= 'z';
                }
                else if (c >= 'A') {
                    if (c <= 'Z') {
                        return true;
                    }
                    return c == '_';
                }
                else if (c >= '0') {
                    return c <= '9';
                }
                else {
                    return c == '-' || c == '.';
                }
            }
            else if ((c & ~0x1FFF) == 0) {
                return IsNCNameStartChar (c) || c == 0xB7
                    || (0x300 <= c && c <= 0x36F);
            }
            return IsNCNameStartChar (c) || (0x203F <= c && c <= 0x2040);
        }

        public String GetDefaultNamespaceURI (Context cx)
        {
            String uri = "";
            if (cx == null) {
                cx = Context.CurrentContext;
            }
            if (cx != null) {
                Object ns = ScriptRuntime.searchDefaultNamespace (cx);
                if (ns != null) {
                    if (ns is Namespace) {
                        uri = ((Namespace)ns).Uri;
                    }
                    else {
                        // Should not happen but for now it could
                        // due to bad searchDefaultNamespace implementation.
                    }
                }
            }
            return uri;
        }

        public Namespace GetDefaultNamespace (Context cx)
        {
            if (cx == null) {
                cx = Context.CurrentContext;
                if (cx == null) {
                    return namespacePrototype;
                }
            }

            Namespace result;
            Object ns = ScriptRuntime.searchDefaultNamespace (cx);
            if (ns == null) {
                result = namespacePrototype;
            }
            else {
                if (ns is Namespace) {
                    result = (Namespace)ns;
                }
                else {
                    // Should not happen but for now it could
                    // due to bad searchDefaultNamespace implementation.
                    result = namespacePrototype;
                }
            }
            return result;
        }

        public IRef NameRef (Context cx, object name, IScriptable scope, int memberTypeFlags)
        {
            XMLName nameRef = XMLName.Parse (this, cx, name);
            if (nameRef == null)
                return null;
            return nameRef;
        }

        public IRef NameRef (Context cx, object ns, object name, IScriptable scope, int memberTypeFlags)
        {
            throw new NotImplementedException ();
        }

        public string EscapeAttributeValue (object value)
        {
            throw new NotImplementedException ();
        }

        public string EscapeTextValue (object value)
        {
            throw new NotImplementedException ();
        }

        public object ToDefaultXmlNamespace (Context cx, object uriValue)
        {
            return Namespace.Parse (this, cx, uriValue);
        }

        internal static EcmaScriptError BadXMLName (object value)
        {
            String msg;
            if (CliHelper.IsNumber (value)) {
                msg = "Can not construct XML name from number: ";
            }
            else if (value is Boolean) {
                msg = "Can not construct XML name from boolean: ";
            }
            else if (value == Undefined.Value || value == null) {
                msg = "Can not construct XML name from ";
            }
            else {
                throw new ArgumentException (value.ToString ());
            }
            return ScriptRuntime.TypeError (msg + ScriptConvert.ToString (value));
        }


        internal XMLName toQualifiedName (Context cx, Object namespaceValue,
            Object nameValue)
        {
            // This is duplication of constructQName(cx, namespaceValue, nameValue)
            // but for XMLName

            String uri;
            String localName;

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
                    ns = GetDefaultNamespace (cx);
                }
            }
            else if (namespaceValue == null) {
                ns = null;
            }
            else if (namespaceValue is Namespace) {
                ns = (Namespace)namespaceValue;
            }
            else {
                ns = Namespace.Parse (this, cx, namespaceValue);
            }

            if (ns == null) {
                uri = null;
            }
            else {
                uri = ns.Uri;
            }

            return XMLName.FormProperty (uri, localName);
        }

        internal XMLList ToXMLList (object value)
        {
            if (value == null || value is Undefined)
                return null;
            if (value is XMLList)
                return (XMLList)value;
            if (value is XML)
                return null;
            if (value is XmlNode)
                return null;
            return new XMLList (this, value);
        }

        internal XML ToXML (object value)
        {
            if (value == null || value is Undefined)
                return null;
            if (value is XML)
                return (XML)value;
            if (value is XMLList)
                return null;
            if (value is XmlNode)
                return new XML (this, (XmlNode)value);
            return XML.CreateFromJS (this, value);
        }
    }
}
