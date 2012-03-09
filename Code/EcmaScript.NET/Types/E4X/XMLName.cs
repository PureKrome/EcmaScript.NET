//------------------------------------------------------------------------------
// <license file="XMLName.cs">
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
    internal class XMLName : IRef
    {

        internal String uri;
        internal String localName;
        private XMLObject xmlObject;
        internal bool IsAttributeName;
        internal bool IsDescendants;

        public XMLName (string uri, string localName)
        {
            this.uri = uri;
            this.localName = localName;
        }

        public void BindTo (XMLObject obj)
        {
            if (obj == null)
                throw new ArgumentNullException ("obj");
            if (xmlObject != null)
                throw new ArgumentException ("Already bound to an xml object.");
            this.xmlObject = obj;
        }

        public object Get (Context cx)
        {
            if (xmlObject == null)
                throw ScriptRuntime.UndefReadError (Undefined.Value, ToString ());
            return xmlObject.GetXMLProperty (this);
        }

        public object Set (Context cx, object value)
        {
            if (xmlObject == null)
                throw ScriptRuntime.UndefWriteError (this, ToString (), value);
            xmlObject.PutXMLProperty (this, value);
            return value;
        }

        public bool Has (Context cx)
        {
            throw new Exception ("The method or operation is not implemented.");
        }

        public bool Delete (Context cx)
        {
            throw new Exception ("The method or operation is not implemented.");
        }

        internal bool Matches (XmlNode node)
        {
            if (uri != null && !uri.Equals (node.NamespaceURI))
                return false;
            if (localName != null
                && localName != "*"
                && !localName.Equals (node.LocalName))
                return false;
            return true;
        }

        public override string ToString ()
        {
            StringBuilder buff = new StringBuilder ();
            if (IsDescendants)
                buff.Append ("..");
            if (IsAttributeName)
                buff.Append ('@');
            if (uri == null) {
                buff.Append ('*');
                if (localName.Equals ("*")) {
                    return buff.ToString ();
                }
            }
            else {
                buff.Append ('"').Append (uri).Append ('"');
            }
            buff.Append (':').Append (localName);
            return buff.ToString ();
        }

        internal static XMLName Parse (XMLLib lib, Context cx, Object value)
        {
            XMLName result;

            if (value is XMLName) {
                result = (XMLName)value;
            }
            else if (value is String) {
                String str = (String)value;
                long test = ScriptRuntime.testUint32String (str);
                if (test >= 0) {
                    ScriptRuntime.storeUint32Result (cx, test);
                    result = null;
                }
                else {
                    result = Parse (lib, cx, str);
                }
            }
            else if (CliHelper.IsNumber (value)) {
                double d = ScriptConvert.ToNumber (value);
                long l = (long)d;
                if (l == d && 0 <= l && l <= 0xFFFFFFFFL) {
                    ScriptRuntime.storeUint32Result (cx, l);
                    result = null;
                }
                else {
                    throw XMLLib.BadXMLName (value);
                }
            }
            else if (value is QName) {
                QName qname = (QName)value;
                String uri = qname.Uri;
                bool number = false;
                result = null;
                if (uri != null && uri.Length == 0) {
                    // Only in this case qname.toString() can resemble uint32
                    long test = ScriptRuntime.testUint32String (uri);
                    if (test >= 0) {
                        ScriptRuntime.storeUint32Result (cx, test);
                        number = true;
                    }
                }
                if (!number) {
                    result = XMLName.FormProperty (uri, qname.LocalName);
                }
            }
            else if (value is Boolean
                     || value == Undefined.Value
                     || value == null) {
                throw XMLLib.BadXMLName (value);
            }
            else {
                String str = ScriptConvert.ToString (value);
                long test = ScriptRuntime.testUint32String (str);
                if (test >= 0) {
                    ScriptRuntime.storeUint32Result (cx, test);
                    result = null;
                }
                else {
                    result = Parse (lib, cx, str);
                }
            }

            return result;
        }

        internal static XMLName FormStar ()
        {
            return new XMLName (null, "*");
        }

        internal static XMLName FormProperty (string uri, string localName)
        {
            return new XMLName (uri, localName);
        }

        internal static XMLName Parse (XMLLib lib, Context cx, string name)
        {
            if (name == null)
                throw new ArgumentNullException ("name");

            int l = name.Length;
            if (l != 0) {
                char firstChar = name [0];
                if (firstChar == '*') {
                    if (l == 1) {
                        return FormStar ();
                    }
                }
                else if (firstChar == '@') {
                    XMLName xmlName = FormProperty ("", name.Substring (1));
                    xmlName.IsAttributeName = true;
                    return xmlName;
                }
            }

            String uri = lib.GetDefaultNamespaceURI (cx);

            return XMLName.FormProperty (uri, name);
        }

    }
}
