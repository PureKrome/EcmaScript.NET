//------------------------------------------------------------------------------
// <license file="XML.cs">
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
using System.IO;
using System.Xml;
using System.Text;
using System.Collections;

namespace EcmaScript.NET.Types.E4X
{
    class XML : XMLObject
    {

        private static readonly System.Object XMLOBJECT_TAG = new System.Object ();

        private XmlNode underlyingNode = null;

        protected virtual XmlNode UnderlyingNode
        {
            get
            {
                return underlyingNode;
            }
            set
            {
                underlyingNode = value;
            }
        }

        public override string ClassName
        {
            get
            {
                return "XML";
            }
        }

        public XML (XMLLib lib)
            : base (lib, lib.xmlPrototype)
        {
            this.lib = lib;
        }

        public XML (XMLLib lib, XmlNode underlyingNode)
            : base (lib, lib.xmlPrototype)
        {
            this.lib = lib;
            this.underlyingNode = underlyingNode;
        }

        public void ExportAsJSClass (bool zealed)
        {
            base.ExportAsJSClass (MAX_PROTOTYPE_ID, lib.GlobalScope, zealed);
            isPrototype = true;
        }

        #region PrototypeIds
        private const int Id_constructor = 1;
        private const int Id_addNamespace = 2;
        private const int Id_appendChild = 3;
        private const int Id_attribute = 4;
        private const int Id_attributes = 5;
        private const int Id_child = 6;
        private const int Id_childIndex = 7;
        private const int Id_children = 8;
        private const int Id_comments = 9;
        private const int Id_contains = 10;
        private const int Id_copy = 11;
        private const int Id_descendants = 12;
        private const int Id_elements = 13;
        private const int Id_hasOwnProperty = 14;
        private const int Id_hasComplexContent = 15;
        private const int Id_hasSimpleContent = 16;
        private const int Id_inScopeNamespaces = 17;
        private const int Id_insertChildAfter = 18;
        private const int Id_insertChildBefore = 19;
        private const int Id_length = 20;
        private const int Id_localName = 21;
        private const int Id_name = 22;
        private const int Id_namespace = 23;
        private const int Id_namespaceDeclarations = 24;
        private const int Id_nodeKind = 25;
        private const int Id_normalize = 26;
        private const int Id_parent = 27;
        private const int Id_processingInstructions = 28;
        private const int Id_prependChild = 29;
        private const int Id_propertyIsEnumerable = 30;
        private const int Id_removeNamespace = 31;
        private const int Id_replace = 32;
        private const int Id_setChildren = 33;
        private const int Id_setLocalName = 34;
        private const int Id_setName = 35;
        private const int Id_setNamespace = 36;
        private const int Id_text = 37;
        private const int Id_toString = 38;
        private const int Id_toXMLString = 39;
        private const int Id_valueOf = 40;
        private const int Id_domNode = 41;
        private const int Id_domNodeList = 42;
        private const int Id_xpath = 43;
        private const int MAX_PROTOTYPE_ID = 43;
        #endregion


        protected internal override int FindPrototypeId (System.String s)
        {
            int id;
            #region Generated PrototypeId Switch
        L0: {
                id = 0;
                string X = null;
                int c;
            L:
                switch (s.Length) {
                    case 4:
                        c = s [0];
                        if (c == 'c') { X = "copy"; id = Id_copy; }
                        else if (c == 'n') { X = "name"; id = Id_name; }
                        else if (c == 't') { X = "text"; id = Id_text; }
                        break;
                    case 5:
                        c = s [0];
                        if (c == 'c') { X = "child"; id = Id_child; }
                        else if (c == 'x') { X = "xpath"; id = Id_xpath; }
                        break;
                    case 6:
                        c = s [0];
                        if (c == 'l') { X = "length"; id = Id_length; }
                        else if (c == 'p') { X = "parent"; id = Id_parent; }
                        break;
                    case 7:
                        switch (s [0]) {
                            case 'd':
                                X = "domNode";
                                id = Id_domNode;
                                break;
                            case 'r':
                                X = "replace";
                                id = Id_replace;
                                break;
                            case 's':
                                X = "setName";
                                id = Id_setName;
                                break;
                            case 'v':
                                X = "valueOf";
                                id = Id_valueOf;
                                break;
                        }
                        break;
                    case 8:
                        switch (s [2]) {
                            case 'S':
                                X = "toString";
                                id = Id_toString;
                                break;
                            case 'd':
                                X = "nodeKind";
                                id = Id_nodeKind;
                                break;
                            case 'e':
                                X = "elements";
                                id = Id_elements;
                                break;
                            case 'i':
                                X = "children";
                                id = Id_children;
                                break;
                            case 'm':
                                X = "comments";
                                id = Id_comments;
                                break;
                            case 'n':
                                X = "contains";
                                id = Id_contains;
                                break;
                        }
                        break;
                    case 9:
                        switch (s [2]) {
                            case 'c':
                                X = "localName";
                                id = Id_localName;
                                break;
                            case 'm':
                                X = "namespace";
                                id = Id_namespace;
                                break;
                            case 'r':
                                X = "normalize";
                                id = Id_normalize;
                                break;
                            case 't':
                                X = "attribute";
                                id = Id_attribute;
                                break;
                        }
                        break;
                    case 10:
                        c = s [0];
                        if (c == 'a') { X = "attributes"; id = Id_attributes; }
                        else if (c == 'c') { X = "childIndex"; id = Id_childIndex; }
                        break;
                    case 11:
                        switch (s [2]) {
                            case 'X':
                                X = "toXMLString";
                                id = Id_toXMLString;
                                break;
                            case 'm':
                                X = "domNodeList";
                                id = Id_domNodeList;
                                break;
                            case 'n':
                                X = "constructor";
                                id = Id_constructor;
                                break;
                            case 'p':
                                X = "appendChild";
                                id = Id_appendChild;
                                break;
                            case 's':
                                X = "descendants";
                                id = Id_descendants;
                                break;
                            case 't':
                                X = "setChildren";
                                id = Id_setChildren;
                                break;
                        }
                        break;
                    case 12:
                        c = s [0];
                        if (c == 'a') { X = "addNamespace"; id = Id_addNamespace; }
                        else if (c == 'p') { X = "prependChild"; id = Id_prependChild; }
                        else if (c == 's') {
                            c = s [3];
                            if (c == 'L') { X = "setLocalName"; id = Id_setLocalName; }
                            else if (c == 'N') { X = "setNamespace"; id = Id_setNamespace; }
                        }
                        break;
                    case 14:
                        X = "hasOwnProperty";
                        id = Id_hasOwnProperty;
                        break;
                    case 15:
                        X = "removeNamespace";
                        id = Id_removeNamespace;
                        break;
                    case 16:
                        c = s [0];
                        if (c == 'h') { X = "hasSimpleContent"; id = Id_hasSimpleContent; }
                        else if (c == 'i') { X = "insertChildAfter"; id = Id_insertChildAfter; }
                        break;
                    case 17:
                        c = s [3];
                        if (c == 'C') { X = "hasComplexContent"; id = Id_hasComplexContent; }
                        else if (c == 'c') { X = "inScopeNamespaces"; id = Id_inScopeNamespaces; }
                        else if (c == 'e') { X = "insertChildBefore"; id = Id_insertChildBefore; }
                        break;
                    case 20:
                        X = "propertyIsEnumerable";
                        id = Id_propertyIsEnumerable;
                        break;
                    case 21:
                        X = "namespaceDeclarations";
                        id = Id_namespaceDeclarations;
                        break;
                    case 22:
                        X = "processingInstructions";
                        id = Id_processingInstructions;
                        break;
                }
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
                case Id_constructor: {
                        IdFunctionObject ctor;
                        if (this is XML) {
                            ctor = new XMLCtor ((XML)this, XMLOBJECT_TAG, id, 1);
                        }
                        else {
                            ctor = new IdFunctionObject (this, XMLOBJECT_TAG, id, 1);
                        }
                        InitPrototypeConstructor (ctor);
                        return;
                    }
                case Id_addNamespace:
                    arity = 1;
                    s = "addNamespace";
                    break;
                case Id_appendChild:
                    arity = 1;
                    s = "appendChild";
                    break;
                case Id_attribute:
                    arity = 1;
                    s = "attribute";
                    break;
                case Id_attributes:
                    arity = 0;
                    s = "attributes";
                    break;
                case Id_child:
                    arity = 1;
                    s = "child";
                    break;
                case Id_childIndex:
                    arity = 0;
                    s = "childIndex";
                    break;
                case Id_children:
                    arity = 0;
                    s = "children";
                    break;
                case Id_comments:
                    arity = 0;
                    s = "comments";
                    break;
                case Id_contains:
                    arity = 1;
                    s = "contains";
                    break;
                case Id_copy:
                    arity = 0;
                    s = "copy";
                    break;
                case Id_descendants:
                    arity = 1;
                    s = "descendants";
                    break;
                case Id_hasComplexContent:
                    arity = 0;
                    s = "hasComplexContent";
                    break;
                case Id_hasOwnProperty:
                    arity = 1;
                    s = "hasOwnProperty";
                    break;
                case Id_hasSimpleContent:
                    arity = 0;
                    s = "hasSimpleContent";
                    break;
                case Id_inScopeNamespaces:
                    arity = 0;
                    s = "inScopeNamespaces";
                    break;
                case Id_insertChildAfter:
                    arity = 2;
                    s = "insertChildAfter";
                    break;
                case Id_insertChildBefore:
                    arity = 2;
                    s = "insertChildBefore";
                    break;
                case Id_length:
                    arity = 0;
                    s = "length";
                    break;
                case Id_localName:
                    arity = 0;
                    s = "localName";
                    break;
                case Id_name:
                    arity = 0;
                    s = "name";
                    break;
                case Id_namespace:
                    arity = 1;
                    s = "namespace";
                    break;
                case Id_namespaceDeclarations:
                    arity = 0;
                    s = "namespaceDeclarations";
                    break;
                case Id_nodeKind:
                    arity = 0;
                    s = "nodeKind";
                    break;
                case Id_normalize:
                    arity = 0;
                    s = "normalize";
                    break;
                case Id_parent:
                    arity = 0;
                    s = "parent";
                    break;
                case Id_prependChild:
                    arity = 1;
                    s = "prependChild";
                    break;
                case Id_processingInstructions:
                    arity = 1;
                    s = "processingInstructions";
                    break;
                case Id_propertyIsEnumerable:
                    arity = 1;
                    s = "propertyIsEnumerable";
                    break;
                case Id_removeNamespace:
                    arity = 1;
                    s = "removeNamespace";
                    break;
                case Id_replace:
                    arity = 2;
                    s = "replace";
                    break;
                case Id_setChildren:
                    arity = 1;
                    s = "setChildren";
                    break;
                case Id_setLocalName:
                    arity = 1;
                    s = "setLocalName";
                    break;
                case Id_setName:
                    arity = 1;
                    s = "setName";
                    break;
                case Id_setNamespace:
                    arity = 1;
                    s = "setNamespace";
                    break;
                case Id_text:
                    arity = 0;
                    s = "text";
                    break;
                case Id_toString:
                    arity = 0;
                    s = "toString";
                    break;
                case Id_toXMLString:
                    arity = 1;
                    s = "toXMLString";
                    break;
                case Id_valueOf:
                    arity = 0;
                    s = "valueOf";
                    break;
                case Id_domNode:
                    arity = 0;
                    s = "domNode";
                    break;
                case Id_domNodeList:
                    arity = 0;
                    s = "domNodeList";
                    break;
                case Id_xpath:
                    arity = 0;
                    s = "xpath";
                    break;
                default:
                    throw new System.ArgumentException (System.Convert.ToString (id));
            }
            InitPrototypeMethod (XMLOBJECT_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, System.Object [] args)
        {
            if (!f.HasTag (XMLOBJECT_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }
            int id = f.MethodId;
            if (id == Id_constructor) {
                return JsConstructor (cx, thisObj == null, args);
            }

            // All XML.prototype methods require thisObj to be XML
            if (!(thisObj is XML))
                throw IncompatibleCallError (f);
            XML realThis = (XML)thisObj;

            XMLName xmlName;
            switch (id) {

                case Id_addNamespace: {
                        return realThis.AddNamespace (GetArgSafe (args, 0));
                    }

                case Id_appendChild:
                    return realThis.AppendChild (GetArgSafe (args, 0));

                case Id_attribute:
                    xmlName = XMLName.Parse (lib, cx, GetArgSafe (args, 0));
                    return realThis.Attribute (xmlName);

                case Id_attributes:
                    return realThis.Attributes ();

                case Id_child:
                    xmlName = XMLName.Parse (lib, cx, GetArgSafe (args, 0));
                    if (xmlName == null) {
                        long index = ScriptRuntime.lastUint32Result (cx);
                        return realThis.Child (index);
                    }
                    else {
                        return realThis.Child (xmlName);
                    }

                case Id_childIndex:
                    return realThis.ChildIndex ();

                case Id_children:
                    return realThis.Children ();

                case Id_comments:
                    return realThis.Comments ();

                case Id_contains:
                    return realThis.Contains (GetArgSafe (args, 0));

                case Id_copy:
                    return realThis.Copy ();

                case Id_descendants: {
                        xmlName = (args.Length == 0)
                            ? XMLName.FormStar () : XMLName.Parse (lib, cx, GetArgSafe (args, 0));
                        return realThis.Descendants (xmlName);
                    }

                case Id_inScopeNamespaces: {
                        object [] array = realThis.InScopeNamespaces ();
                        return cx.NewArray (scope, array);
                    }


                case Id_insertChildAfter:
                    return realThis.InsertChildAfter (GetArgSafe (args, 0), GetArgSafe (args, 1));

                case Id_insertChildBefore:
                    return realThis.InsertChildBefore (GetArgSafe (args, 0), GetArgSafe (args, 1));

                case Id_hasOwnProperty:
                    xmlName = XMLName.Parse (lib, cx, GetArgSafe (args, 0));
                    return realThis.HasOwnProperty (xmlName);


                case Id_hasComplexContent:
                    return realThis.HasComplexContent ();

                case Id_hasSimpleContent:
                    return realThis.HasSimpleContent ();

                case Id_length:
                    return realThis.Length ();

                case Id_localName:
                    return realThis.LocalName ();

                case Id_name:
                    return realThis.Name ();

                case Id_namespace:
                    return realThis.Namespace (GetArgSafe (args, 0));

                case Id_namespaceDeclarations:
                    return cx.NewArray (scope, realThis.NamespaceDeclarations ());

                case Id_nodeKind:
                    return realThis.NodeKind ();

                case Id_normalize:
                    realThis.Normalize ();
                    return Undefined.Value;

                case Id_parent:
                    return realThis.Parent ();

                case Id_prependChild:
                    return realThis.PrependChild (GetArgSafe (args, 0));

                case Id_processingInstructions:
                    xmlName = (args.Length > 0) ? XMLName.Parse (lib, cx, args [0]) : XMLName.FormStar ();
                    return realThis.ProcessingInstructions (xmlName);

                case Id_propertyIsEnumerable: {
                        return realThis.PropertyIsEnumerable (GetArgSafe (args, 0));
                    }

                case Id_removeNamespace: {
                        Namespace ns = E4X.Namespace.Parse (lib, cx, GetArgSafe (args, 0));
                        return realThis.RemoveNamespace (ns);
                    }

                case Id_replace: {
                        xmlName = XMLName.Parse (lib, cx, GetArgSafe (args, 0));
                        object arg1 = GetArgSafe (args, 1);
                        if (xmlName == null) {
                            long index = ScriptRuntime.lastUint32Result (cx);
                            return realThis.Replace (index, arg1);
                        }
                        else {
                            return realThis.Replace (xmlName, arg1);
                        }
                    }

                case Id_setChildren:
                    return realThis.SetChildren (GetArgSafe (args, 0));

                case Id_setLocalName: {
                        string localName;
                        object arg = GetArgSafe (args, 0);
                        if (arg is QName) {
                            localName = ((QName)arg).LocalName;
                        }
                        else {
                            localName = ScriptConvert.ToString (arg);
                        }
                        realThis.SetLocalName (localName);
                        return Undefined.Value;
                    }

                case Id_setName: {
                        object arg = (args.Length != 0) ? args [0] : Undefined.Value;
                        QName qname;
                        if (arg is QName) {
                            qname = (QName)arg;
                            if (qname.Uri == null) {
                                qname = QName.Parse (lib, cx, qname.LocalName);
                            }
                            else {
                                // E4X 13.4.4.35 requires to always construct QName
                                qname = QName.Parse (lib, cx, qname);
                            }
                        }
                        else {
                            qname = QName.Parse (lib, cx, arg);
                        }
                        realThis.SetName (qname);
                        return Undefined.Value;
                    }

                case Id_setNamespace: {
                        Namespace ns = E4X.Namespace.Parse (lib, cx, GetArgSafe (args, 0));
                        realThis.SetNamespace (ns);
                        return Undefined.Value;
                    }

                case Id_text:
                    return realThis.Text ();


                case Id_toString:
                    return realThis.ToString ();

                case Id_toXMLString:
                    return realThis.ToXMLString ();

                case Id_valueOf:
                    return realThis;

            }

            throw new System.ArgumentException (System.Convert.ToString (id));
        }


        /// <summary>
        /// XML.prototype.addNamespace ( namespace )
        /// 
        /// The addNamespace method adds a namespace declaration to the in scope
        /// namespaces for this XML object and returns this XML object. If the
        /// in scope namespaces for the XML object already contains a namespace
        /// with a prefix matching that of the given parameter, the prefix of the
        /// existing namespace is set to undefined.
        /// 
        /// See ECMA 13.4.4.2
        /// </summary>
        internal XMLObject AddNamespace (object value)
        {
            if (value == null || value is Undefined)
                throw ScriptRuntime.TypeErrorById ("value may be not be null or undefined.");

            Namespace ns = (value as Namespace);
            if (ns == null) {
                throw ScriptRuntime.TypeErrorById ("value may be a Namespace, not {0}",
                    ScriptRuntime.Typeof (value));
            }

            throw new NotImplementedException ();
        }

        private object JsConstructor (Context cx, bool inNewExpr, object [] args)
        {
            if (args.Length == 0)
                return CreateFromJS (lib, string.Empty);
            else {
                object arg0 = args [0];
                if (!inNewExpr && arg0 is XML) {
                    // XML(XML) returns the same object.
                    return arg0;
                }
                return CreateFromJS (lib, arg0);
            }
        }

        internal static XML CreateFromJS (XMLLib lib, Object inputObject)
        {
            string frag = "";

            if (inputObject == null || inputObject == Undefined.Value) {
                frag = "";
            }
            else if (inputObject is XMLObject) {
                frag = ((XMLObject)inputObject).ToXMLString ();
            }
            else {
                frag = ScriptConvert.ToString (inputObject);
            }

            if (frag.Trim ().StartsWith ("<>")) {
                throw ScriptRuntime.TypeError ("Invalid use of XML object anonymous tags <></>.");
            }

            XmlDocument doc = null;
            if (frag.IndexOf ("<") == -1) {
                // Must be solo text node
                doc = new XmlDocument ();
                XmlNode node = doc.CreateTextNode (frag);
                return new XML (lib, node);
            }

            doc = new XmlDocument ();
            try {
                using (StringReader sr = new StringReader (frag)) {
                    XmlTextReader xr = new XMLTextReader (lib, sr);
                    doc.Load (xr);

                }
            }
            catch (XmlException e) {
                throw ScriptRuntime.TypeError (e.Message);
            }

            return new XML (lib, doc);
        }

        private class XMLTextReader : XmlTextReader
        {

            private XMLLib lib;

            public XMLTextReader (XMLLib lib, StringReader sr)
                : base (sr)
            {
                this.lib = lib;
            }

            public override bool Read ()
            {
                if (!base.Read ())
                    return false;

                if (lib.ignoreComments && NodeType == XmlNodeType.Comment)
                    return Read ();

                if (lib.ignoreProcessingInstructions && NodeType == XmlNodeType.ProcessingInstruction)
                    return Read ();

                if (lib.ignoreWhitespace && (
                    NodeType == XmlNodeType.Whitespace
                    || NodeType == XmlNodeType.SignificantWhitespace)
                    ) {
                    return Read ();
                }

                return true;
            }

        }

        protected override internal IScriptable GetExtraMethodSource (Context cx)
        {
            if (HasSimpleContent ()) {
                return ScriptConvert.ToObjectOrNull (cx, ToString ());
            }
            return null;
        }

        internal static XML CreateEmptyXml (XMLLib impl)
        {
            return new XML (impl);
        }

        /// <summary>
        /// XML.prototype.toXMLString ( )
        /// 
        /// The toXMLString() method returns an XML encoded string
        /// representation of this XML object per the ToXMLString
        /// conversion operator described in section 10.2. Unlike
        /// the toString method, toXMLString provides no special treatment
        /// for XML objects that contain only XML text nodes (i.e., primitive values).
        /// The toXMLString method always includes the start tag, attributes and end
        /// tag of the XML object regardless of its content. It is provided for cases
        /// when the default XML to string conversion rules are not desired.
        /// 
        /// See ECMA 13.4.4.39
        /// </summary>
        protected internal override string ToXMLString ()
        {
            using (StringWriter sw = new StringWriter ()) {
                XmlTextWriter xw = new XmlTextWriter (sw);
                UnderlyingNode.WriteTo (xw);
                xw.Flush ();
                xw.Close ();

                return sw.ToString ();
            }
        }

        /// <summary>
        /// XML.prototype.valueOf ( )
        /// 
        /// The valueOf method returns this XML object.
        /// 
        /// See ECMA 13.4.4.40
        /// </summary>
        /// <returns></returns>
        internal XMLObject ValueOf ()
        {
            return this;
        }

        /// <summary>
        /// XML.prototype.hasSimpleContent( )
        /// 
        /// The hasSimpleContent method returns a Boolean value indicating whether
        /// this XML object contains simple content. An XML object is considered to
        /// contain simple content if it represents a text node, represents an attribute
        /// node or if it represents an XML element that has no child elements. XML
        /// objects representing comments and processing instructions do not have
        /// simple content. The existence of attributes, comments, processing instructions
        /// and text nodes within an XML object is not significant in determining if it
        /// has simple content.
        /// 
        /// See ECMA 13.4.4.16
        /// </summary>
        internal bool HasSimpleContent ()
        {
            if (UnderlyingNode is XmlAttribute)
                return true;
            if (UnderlyingNode is XmlText)
                return true;
            if (UnderlyingNode.SelectNodes ("*").Count < 1)
                return true;
            return false;

        }

        /// <summary>
        /// XML.prototype.hasComplexContent( )
        /// 
        /// The hasComplexContent method returns a Boolean value
        /// indicating whether this XML object contains complex
        /// content. An XML object is considered to contain complex content
        /// if it represents an XML element that has child elements. XML
        /// objects representing attributes, comments, processing instructions 
        /// and text nodes do not have complex content. The existence of attributes,
        /// comments, processing instructions and text nodes within an XML object
        /// is not significant in determining if it has complex content.
        /// 
        /// See ECMA 13.4.4.15
        /// </summary>
        internal bool HasComplexContent ()
        {
            return !HasSimpleContent ();
        }

        /// <summary>
        /// XML.prototype.inScopeNamespaces( )
        /// 
        /// The inScopeNamespaces method returns an Array of Namespace objects
        /// representing the namespaces in scope for this XML object in the
        /// context of its parent. If the parent of this XML object is modified,
        /// the associated namespace declarations may change. The set of namespaces
        /// returned by this method may be a super set of the namespaces used by this value.
        /// 
        /// See ECMA 13.4.4.17
        /// </summary>
        internal object [] InScopeNamespaces ()
        {
            // TODO: I don't get the difference between those two functions
            return NamespaceDeclarations ();
        }



        /// <summary>
        /// XML.prototype.length ( )
        /// 
        /// The length method always returns the integer 1 for XML objects.
        /// This treatment intentionally blurs the distinction between a single
        /// XML object and an XMLList containing only one value.
        /// 
        /// See ECMA 13.4.4.20
        /// </summary>		
        internal int Length ()
        {
            return 1;
        }

        public override object GetDefaultValue (Type typeHint)
        {
            if (HasSimpleContent ())
                return UnderlyingNode.InnerText;
            return ToSource (0);
        }

        internal string ToSource (int indent)
        {
            return ToXMLString ();
        }

        /// <summary>
        /// XML.prototype.namespace ( [ prefix ] )
        /// 
        /// If no prefix is specified, the namespace method returns the 
        /// Namespace associated with the qualified name of this XML object.
        /// 
        /// If a prefix is specified, the namespace method looks for a namespace 
        /// in scope for this XML object with the given prefix and, if found,
        /// returns it. If no such namespace is found, the namespace method
        /// returns undefined.		
        /// 
        /// See ECMA 13.4.4.23
        /// </summary>		
        internal object Namespace (object value)
        {
            string prefix = ScriptConvert.ToString (value);
            string ns = UnderlyingNode.GetNamespaceOfPrefix (prefix);
            if (ns == null)
                return Undefined.Value;
            return new Namespace (lib, prefix, ns);
        }

        /// <summary>
        /// XML.prototype.parent ( )
        /// 
        /// The parent method returns the parent of this XML object.
        /// 
        /// See ECMA 13.4.4.27
        /// </summary>		
        internal object Parent ()
        {
            if (UnderlyingNode.ParentNode == null)
                return null;
            return new XML (lib, UnderlyingNode.ParentNode);
        }

        /// <summary>
        /// XML.prototype.childIndex ( )
        /// 
        /// The childIndex method returns a Number representing the 
        /// ordinal position of this XML object within the context of its parent.
        /// 
        /// See ECMA 13.4.4.7
        /// </summary>
        internal int ChildIndex ()
        {
            if (UnderlyingNode.ParentNode == null)
                return -1;
            for (int i = 0; i < UnderlyingNode.ParentNode.ChildNodes.Count; i++)
                if (UnderlyingNode.ParentNode.ChildNodes [i] == UnderlyingNode)
                    return i;
            return -1;
        }

        /// <summary>
        /// XML.prototype.children ( )
        /// 
        /// The children method returns an XMLList containing all the
        /// properties of this XML object in order.
        /// 
        /// See ECMA 13.4.4.8
        /// </summary>
        internal XMLList Children ()
        {
            XMLList list = new XMLList (lib);
            foreach (XmlNode node in UnderlyingNode.ChildNodes)
                list.Add (new XML (lib, node));
            return list;
        }

        /// <summary>
        /// XML.prototype.comments ( )
        /// 
        /// The comments method returns an XMLList containing the properties of this
        /// XML object that represent XML comments.
        /// 
        /// See ECMA 13.4.4.9
        /// </summary>
        private XMLList Comments ()
        {
            XMLList list = new XMLList (lib);
            foreach (XmlNode child in UnderlyingNode.ChildNodes) {
                if (child is XmlComment)
                    list.Add (new XML (lib, child));
            }
            return list;
        }

        /// <summary>
        /// XML.prototype.processingInstructions ( [ name ] )
        /// 
        /// When the processingInstructions method is called with one 
        /// parameter name, it returns an XMLList containing all the 
        /// children of this XML object that are processing-instructions 
        /// with the given name. When the processingInstructions method 
        /// is called with no parameters, it returns an XMLList containing 
        /// all the children of this XML object that are processing-instructions
        /// regardless of their name.
        /// 
        /// See ECMA 13.4.4.28
        /// </summary>
        /// <returns></returns>
        internal XMLList ProcessingInstructions (XMLName xmlName)
        {
            XMLList list = new XMLList (lib);
            foreach (XmlNode child in UnderlyingNode.ChildNodes) {
                if (child is XmlProcessingInstruction) {
                    if (xmlName == null || xmlName.Matches (child))
                        list.Add (new XML (lib, child));
                }
            }
            return list;
        }


        /// <summary>
        /// XML.prototype.contains
        /// 
        /// The contains method returns the result of comparing this XML object 
        /// with the given value. This treatment intentionally blurs the 
        /// distinction between a single XML object and an XMLList containing only one value.
        /// 
        /// See ECMA 13.4.4.10
        /// </summary>
        private bool Contains (object value)
        {
            // TODO: Performance
            foreach (XML xml in Children ()) {
                if (xml.EquivalentXml (value))
                    return true;
            }
            return false;
        }

        protected internal override bool EquivalentXml (object value)
        {
            object lhs = GetDefaultValue (typeof (string));

            if (value is XML) {
                XML xml = (XML)value;
                return lhs.Equals (xml.GetDefaultValue (typeof (string)));
            }

            if (value is XMLList) {
                XMLList xmlList = (XMLList)value;
                if (xmlList.Length () == 1) {
                    return EquivalentXml (xmlList [0]);
                }
            }

            if (HasSimpleContent ()) {
                return lhs.Equals (value.ToString ());
            }

            return false;
        }



        public override bool Equals (object obj)
        {
            XML xml = (obj as XML);
            if (xml == null)
                return false;
            if (xml.UnderlyingNode == null && this.UnderlyingNode == null)
                return true;
            if (xml.UnderlyingNode == null || this.UnderlyingNode == null)
                return false;
            return xml.UnderlyingNode.Equals (this.UnderlyingNode);
        }

        public override int GetHashCode ()
        {
            if (this.UnderlyingNode == null)
                return base.GetHashCode ();
            return this.UnderlyingNode.GetHashCode ();
        }

        /// <summary>
        /// XML.prototype.attribute ( attributeName )
        /// 
        /// The attribute method returns an XMLList
        /// containing zero or one XML attributes associated with 
        /// this XML object that have the given attributeName.
        /// 
        /// See ECMA 13.4.4.4
        /// </summary>
        /// <param name="xmlName"></param>
        /// <returns></returns>
        internal XMLList Attribute (XMLName xmlName)
        {
            XMLList list = new XMLList (lib);
            MatchAttributes (list, xmlName, UnderlyingNode, false);
            return list;
        }

        internal void MatchAttributes (XMLList list, XMLName xmlName, XmlNode parent, bool recursive)
        {
            if (parent is XmlDocument)
                parent = ((XmlDocument)parent).DocumentElement;

            if (!(parent is XmlElement))
                return;

            foreach (XmlAttribute attr in parent.Attributes) {
                if (xmlName == null || xmlName.Matches (attr))
                    list.Add (new XML (lib, attr));
            }

            if (recursive) {
                foreach (XmlNode node in parent.ChildNodes)
                    MatchAttributes (list, xmlName, node, recursive);
            }
        }

        /// <summary>
        /// XML.prototype.attributes ( )
        /// 
        /// The attributes method returns an XMLList containing the XML attributes of this object.
        /// 
        /// See ECMA 13.4.4.5
        /// </summary>
        /// <returns></returns>
        internal XMLList Attributes ()
        {
            XMLList list = new XMLList (lib);
            MatchAttributes (list, null, UnderlyingNode, false);
            return list;
        }

        /// <summary>
        /// XML.prototype.descendants ( [ name ] )
        /// 
        /// The descendants method returns all the XML valued descendants 
        /// (children, grandchildren, great-grandchildren, etc.) of this XML
        /// object with the given name. If the name parameter is omitted,
        /// it returns all descendants of this XML object.
        /// 
        /// See ECMA 13.4.4.12
        /// </summary>
        /// <param name="xmlName"></param>
        /// <returns></returns>
        internal XMLList Descendants (XMLName xmlName)
        {
            XMLList list = new XMLList (lib);
            if (xmlName.IsAttributeName) {
                MatchAttributes (list, xmlName, UnderlyingNode, true);
            }
            else {
                MatchChildren (list, xmlName, UnderlyingNode, true);
            }
            return list;
        }

        /// <summary>
        /// XML.prototype.elements ( [ name ] )
        /// 
        /// When the elements method is called with one parameter name,
        /// it returns an XMLList containing all the children of this
        /// XML object that are XML elements with the given name. When
        /// the elements method is called with no parameters, it returns
        /// an XMLList containing all the children of this XML object that
        /// are XML elements regardless of their name.
        /// 
        /// See ECMA 13.4.4.13
        /// </summary>
        private XMLList Elements (XMLName xmlName)
        {
            if (xmlName == null)
                return Children ();

            XMLList list = new XMLList (lib);
            MatchChildren (list, xmlName, UnderlyingNode, false);
            return list;
        }



        public void MatchChildren (XMLList list, XMLName xmlName, XmlNode parent, bool recursive)
        {
            foreach (XmlNode child in parent.ChildNodes) {
                if (xmlName.Matches (child))
                    list.Add (new XML (lib, child));
                if (recursive)
                    MatchChildren (list, xmlName, child, recursive);
            }
        }

        public override object [] GetIds ()
        {
            if (isPrototype) {
                return new object [0];
            }
            else {
                return new object [] { 0 };
            }
        }

        /// <summary>
        /// XML.prototype.name ( )
        /// 
        /// The name method returns the qualified name associated with this XML object.
        /// 
        /// See ECMA 13.4.4.22
        /// </summary>
        /// <returns></returns>
        internal QName Name ()
        {
            if (UnderlyingNode is XmlText)
                return null; // .NET returns here "#text", but we need null instead
            return new QName (lib, UnderlyingNode.NamespaceURI, UnderlyingNode.LocalName, UnderlyingNode.Prefix);

        }

        /// <summary>
        /// XML.prototype.localName ( )
        /// 
        /// The localName method returns the local name portion of the qualified name of
        /// this XML object.
        /// 
        /// See ECMA 13.4.4.21
        /// </summary>
        /// <returns></returns>
        internal string LocalName ()
        {
            return UnderlyingNode.LocalName;
        }

        /// <summary>
        /// XML.prototype.nodeKind ( )
        /// 
        /// The nodeKind method returns a string representing the [[Class]] of this XML object.
        /// 
        /// See ECMA 13.4.4.25
        /// </summary>		
        internal string NodeKind ()
        {
            if (UnderlyingNode is XmlElement)
                return "element";
            if (UnderlyingNode is XmlAttribute)
                return "attribute";
            if (UnderlyingNode is XmlText)
                return "text";
            if (UnderlyingNode is XmlComment)
                return "comment";
            if (UnderlyingNode is XmlProcessingInstruction)
                return "processing-instruction";
            if (UnderlyingNode is XmlDocument)
                return "element"; // TODO: is this correct?				
            return "text";
        }

        /// <summary>
        /// XML.prototype.child ( propertyName )
        /// 
        /// The child method returns the list of children in this 
        /// XML object matching the given propertyName. If propertyName is a numeric
        /// index, the child method returns a list containing the child at the
        /// ordinal position identified by propertyName.
        /// 
        /// See ECMA 13.4.4.6 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal XMLList Child (long index)
        {
            XMLList list = new XMLList (lib);
            if (index > 0 || index < UnderlyingNode.ChildNodes.Count)
                list.Add (new XML (lib, UnderlyingNode.ChildNodes [(int)index]));
            return list;
        }

        /// <summary>
        /// XML.prototype.child ( propertyName )
        /// 
        /// The child method returns the list of children in this 
        /// XML object matching the given propertyName. If propertyName is a numeric
        /// index, the child method returns a list containing the child at the
        /// ordinal position identified by propertyName.
        /// 
        /// See ECMA 13.4.4.6 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        internal XMLList Child (XMLName xmlName)
        {
            XMLList list = new XMLList (lib);
            MatchChildren (list, xmlName, UnderlyingNode, false);
            return list;
        }

        protected internal override object GetXMLProperty (XMLName name)
        {
            if (isPrototype) {
                return base.Get (name.localName, this);
            }
            if (name.IsDescendants) {
                return Descendants (name);
            }
            if (name.IsAttributeName) {
                return Attribute (name);
            }
            return Child (name);
        }

        /// <summary>
        /// XML.prototype.text ( )
        /// 
        /// The text method returns an XMLList containing all XML
        /// properties of this XML object that represent XML text
        /// nodes.
        /// 
        /// See ECMA 13.4.4.37
        /// </summary>
        /// <returns></returns>
        internal string Text ()
        {
            return UnderlyingNode.InnerText;
        }

        /// <summary>
        /// XML.prototype.normalize ( )
        /// 
        /// The normalize method puts all text nodes in this and all
        /// descendant XML objects into a normal form by merging adjacent
        /// text nodes and eliminating empty text nodes. It returns
        /// this XML object.
        /// 
        /// See ECMA 13.4.4.26
        /// </summary>
        internal XMLObject Normalize ()
        {
            UnderlyingNode.Normalize ();
            return this;
        }


        /// <summary>
        /// XML.prototype.copy ( )
        /// 
        /// The copy method returns a deep copy of this XML object with the 
        /// internal [[Parent]] property set to null.
        /// 
        /// See ECMA 13.4.4.11
        /// </summary>
        internal object Copy ()
        {
            XML xml = new XML (lib, UnderlyingNode.Clone ());
            return xml;
        }

        /// <summary>
        /// XML.prototype.namespaceDeclarations ( )
        /// 
        /// The namespaceDeclarations method returns an Array of Namespace objects
        /// representing the namespace declarations associated with this XML
        /// object in the context of its parent. If the parent of this XML object 
        /// is modified, the associated namespace declarations may change.
        /// 
        /// See ECMA 13.4.4.24
        /// </summary>		
        internal object [] NamespaceDeclarations ()
        {
            ArrayList namespaceDeclarations = new ArrayList ();
            foreach (XmlAttribute attr in UnderlyingNode.Attributes) {
                if (attr.LocalName == "xmlns") {
                    Namespace ns = new Namespace (lib, attr.Prefix, attr.InnerText);
                    if (!namespaceDeclarations.Contains (ns))
                        namespaceDeclarations.Add (ns);
                }
            }
            return (object [])namespaceDeclarations.ToArray ();
        }

        /// <summary>
        /// XML.prototype.hasOwnProperty ( P )
        /// 
        /// The hasOwnProperty method returns a Boolean value indicating whether
        /// this object has the property specified by P. For all XML objects except
        /// the XML prototype object, this is the same result returned by the
        /// internal method [[HasProperty]]. For the XML prototype object,
        /// hasOwnProperty also examines the list of local properties to
        /// determine if there is a method property with the given name.
        /// 
        /// See ECMA 13.4.4.14
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal bool HasOwnProperty (XMLName xmlName)
        {
            if (isPrototype) {
                return FindPrototypeId (xmlName.localName) != 0;
            }
            else {
                return GetPropertyList (xmlName).Length () > 0;
            }
        }

        protected internal override void PutXMLProperty (XMLName xmlName, object value)
        {
            if (isPrototype)
                return;
            if (value == null)
                value = "null";
            else if (value is Undefined)
                value = "undefined";

            if (xmlName.IsAttributeName) {
                SetAttribute (xmlName, value);
            }
            else {

                if (value is XMLList) {
                    XMLList xmlList = (value as XMLList);

                    XMLList matches = GetPropertyList (xmlName);
                    if (matches.Length () == 0) {
                        foreach (XML xml in xmlList)
                            AppendChild (xml);
                    }
                    else {
                        for (int i = 1; i < matches.Length (); i++) {
                            UnderlyingNode.RemoveChild (matches [i].UnderlyingNode);
                        }
                        for (int i = 1; i < xmlList.Length (); i++) {
                            UnderlyingNode.InsertAfter (
                                ImportNode (xmlList [i].UnderlyingNode), matches [0].UnderlyingNode);
                        }
                        UnderlyingNode.ReplaceChild (
                            ImportNode (xmlList [0].UnderlyingNode), matches [0].UnderlyingNode);
                    }
                }
                else {
                    XML xml = (value as XML);
                    if (xml == null) {
                        xml = XML.CreateFromJS (lib, value);
                    }

                    XMLList matches = GetPropertyList (xmlName);
                    if (matches.Length () == 0) {
                        AppendChild (xml);
                    }
                    else {
                        for (int i = 1; i < matches.Length (); i++) {
                            UnderlyingNode.RemoveChild (matches [i].UnderlyingNode);
                        }
                        if (xml.UnderlyingNode is XmlText) {
                            matches [0].RemoveAllChildren ();
                            matches [0].AppendChild (xml);
                        }
                        else {
                            UnderlyingNode.ReplaceChild (
                                ImportNode (xml.UnderlyingNode), matches [0].UnderlyingNode);
                        }
                    }
                }
            }
        }

        internal void SetAttribute (XMLName xmlName, object value)
        {
            if (xmlName.uri == null &&
                xmlName.localName.Equals ("*")) {
                throw ScriptRuntime.TypeError ("@* assignment not supported.");
            }

            XmlNode target = UnderlyingNode;
            if (target is XmlDocument)
                target = ((XmlDocument)target).DocumentElement;

            if (target is XmlElement) {
                ((XmlElement)target).SetAttribute (
                    xmlName.localName, xmlName.uri,
                    ScriptConvert.ToString (value));
            }

        }

        /// <summary>
        /// XML.prototype.appendChild ( child )
        /// 
        /// The appendChild method appends the given child to the end of
        /// this XML objects properties and returns this XML object.
        /// 
        /// See ECMA 13.4.4.3
        /// </summary>
        internal XML AppendChild (object child)
        {
            if (underlyingNode is XmlDocument) {
                // For now a single document with just a <?pi?> cannot exist, so
                // at least one root element must be given. But appending to root
                // will raise an error so we'll "guess" that the user meant to append
                // the documentElement instead of the document.
                // HACK
                XML xml = new XML (lib, ((XmlDocument)underlyingNode).DocumentElement);
                xml.AppendChild (child);
                return this;
            }
            return InsertChildBefore (child, null);
        }

        /// <summary>
        /// XML.prototype.prependChild ( value )
        /// 
        /// The prependChild method inserts the given child into this object prior to
        /// its existing XML properties and returns this XML object
        /// 
        /// See ECMA 13.4.4.29
        /// </summary>
        internal XML PrependChild (object child)
        {
            // Spec says prepending is the same as inserting after with null
            // as refChild
            return InsertChildAfter (child, null);
        }

        /// <summary>
        /// XML.prototype.propertyIsEnumerable ( P )
        /// 
        /// The propertyIsEnumerable method returns a Boolean value indicating
        /// whether the property P will be included in the set of properties
        /// iterated over when this XML object is used in a for-in statement.
        /// This method returns true when ToString(P) is "0"; otherwise, it returns
        /// false. This treatment intentionally blurs the distinction between a
        /// single XML object and an XMLList containing only one value.
        /// 
        /// See ECMA 13.4.4.30
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        internal bool PropertyIsEnumerable (object p)
        {
            throw new NotImplementedException ();
        }

        /// <summary>
        /// XML.prototype.removeNamespace ( namespace )
        /// 
        /// The removeNamespace method removes the given namespace from the
        /// in scope namespaces of this object and all its descendents,
        /// then returns a copy of this XML object. The removeNamespaces method
        /// will not remove a namespace from an object where it is referenced
        /// by that objects QName or the QNames of that objects attributes.
        /// 
        /// See ECMA 13.4.4.31
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        internal XMLObject RemoveNamespace (object ns)
        {
            throw new NotImplementedException ();
        }


        /// <summary>
        /// XML.prototype.replace ( propertyName , value )
        /// 
        /// The replace method replaces the XML properties of this XML object
        /// specified by propertyName with value and returns this XML object. If
        /// this XML object contains no properties that match propertyName, the
        /// replace method returns without modifying this XML object. The propertyName
        /// parameter may be a numeric property name, an unqualified name for a set
        /// of XML elements, a qualified name for a set of XML elements or the
        /// properties wildcard *. When the propertyName parameter is an unqualified
        /// name, it identifies XML elements in the default namespace. The value
        /// parameter may be an XML object, XMLList object or any value that may be
        /// converted to a String with ToString().
        /// 
        /// See ECMA 13.4.4.32
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal XMLObject Replace (object name, object value)
        {
            throw new NotImplementedException ();
        }

        /// <summary>
        /// XML.prototype.setChildren ( value )
        /// 
        /// The setChildren method replaces the XML properties of this
        /// XML object with a new set of XML properties from value. value
        /// may be a single XML object or an XMLList. setChildren returns
        /// this XML object.
        /// 
        /// See ECMA 13.4.4.33
        /// </summary>
        internal XMLObject SetChildren (object value)
        {
            if (!(value is XML) && !(value is XMLList)) {
                throw ScriptRuntime.TypeErrorById ("value may be a single XML object or an XMLList, not {0}",
                    ScriptRuntime.Typeof (value));
            }

            // Remove all children
            foreach (XmlNode child in UnderlyingNode.ChildNodes)
                child.ParentNode.RemoveChild (child);

            XML xml = (value as XML);
            if (xml != null) {
                UnderlyingNode.AppendChild (
                    ImportNode (xml.UnderlyingNode));
            }
            else {
                XMLList xmlList = (XMLList)value;
                foreach (XML xmlListItem in xmlList)
                    UnderlyingNode.AppendChild (
                        ImportNode (xmlListItem.UnderlyingNode));
            }

            return this;
        }


        /// <summary>
        /// XML.prototype.setLocalName ( name )
        /// 
        /// The setLocalName method replaces the local name of this
        /// XML object with a string constructed from the given name.
        /// 
        /// See ECMA 13.4.4.34
        /// </summary>		
        internal void SetLocalName (object name)
        {
            if (name == null || name is Undefined)
                throw ScriptRuntime.TypeError ("name may not be null or undefined");

            XmlQualifiedName qName = null;
            try {
                qName = new XmlQualifiedName (ScriptConvert.ToString (name));
            }
            catch (XmlException ex) {
                throw ScriptRuntime.TypeErrorById ("invalid name: {0}", ex.Message);
            }

            UnderlyingNode =
                Rename (UnderlyingNode, UnderlyingNode.Prefix,
                    qName.Name, UnderlyingNode.NamespaceURI);
        }

        /// <summary>
        /// XML.prototype.setName( name )
        /// 
        /// The setName method replaces the name of this XML object with
        /// a QName or AttributeName constructed from the given name.
        /// 
        /// See ECMA 13.4.4.35
        /// </summary>
        /// <param name="name"></param>
        internal void SetName (object name)
        {
            if (name == null || name is Undefined)
                throw ScriptRuntime.TypeError ("name may not be null or undefined");

            string prefix = UnderlyingNode.Prefix;
            string namespaceUri = UnderlyingNode.NamespaceURI;
            string localName = UnderlyingNode.LocalName;

            if (name is QName) {
                QName qName = (QName)name;

                UnderlyingNode =
                    Rename (UnderlyingNode, qName.Prefix,
                        qName.LocalName, qName.Uri);
            }
            else {
                SetLocalName (name);
            }
        }

        /// <summary>
        /// XML.prototype.setNamespace ( ns )
        /// 
        /// The setNamespace method replaces the namespace associated with
        /// the name of this XML object with the given namespace.
        /// 
        /// See ECMA 13.4.4.36
        /// </summary>
        /// <param name="ns"></param>
        internal void SetNamespace (object ns)
        {
            if (ns == null || ns is Undefined)
                throw ScriptRuntime.TypeError ("name may not be null or undefined");

            if (!(ns is Namespace))
                throw ScriptRuntime.TypeErrorById ("name must be typeof Namespace, not {0}",
                    ScriptRuntime.Typeof (ns));

            Namespace nsObj = (Namespace)ns;
            UnderlyingNode = Rename (
                UnderlyingNode, nsObj.Prefix, UnderlyingNode.LocalName,
                    nsObj.Prefix);
        }

        /// <summary>
        /// XML.prototype.insertChildAfter ( child1 , child2)
        /// 
        /// The insertChildAfter method inserts the given child2 after the given
        /// child1 in this XML object and returns this XML object. If child1 is null,
        /// the insertChildAfter method inserts child2 before all children of this XML
        /// object (i.e., after none of them). If child1 does not exist in this XML
        /// object, it returns without modifying this XML object.
        /// 
        /// See ECMA 13.4.4.18
        /// </summary>
        /// <param name="newChild"></param>
        /// <param name="refChild"></param>
        /// <returns></returns>
        internal XML InsertChildAfter (object newChild, object refChild)
        {
            XML refChildXml = lib.ToXML (refChild);
            XML newChildXml = lib.ToXML (newChild);

            if (newChildXml != null) {
                XmlNode newChildXmlNode = newChildXml.UnderlyingNode;
                if (UnderlyingNode.OwnerDocument != newChildXml.UnderlyingNode.OwnerDocument) {
                    newChildXml.UnderlyingNode =
                        UnderlyingNode.OwnerDocument.ImportNode (newChildXml.UnderlyingNode, true);
                }
                newChildXmlNode = newChildXml.UnderlyingNode;

                XmlNode refChildXmlNode = null;
                if (refChildXml != null) {
                    if (refChildXml.UnderlyingNode.OwnerDocument == UnderlyingNode.OwnerDocument) {
                        if (UnderlyingNode.OwnerDocument != refChildXml.UnderlyingNode.OwnerDocument) {
                            refChildXml.UnderlyingNode =
                                UnderlyingNode.OwnerDocument.ImportNode (refChildXml.UnderlyingNode, true);
                        }
                        refChildXmlNode = refChildXml.UnderlyingNode;
                    }
                }

                UnderlyingNode.InsertAfter (
                    newChildXmlNode, refChildXmlNode);
            }

            return this;
        }

        /// <summary>
        /// XML.prototype.insertChildBefore ( child1 , child2 )
        /// 
        /// The insertChildBefore method inserts the given child2 before the given
        /// child1 in this XML object and returns this XML object. If child1 is null,
        /// the insertChildBefore method inserts child2 after all children in this
        /// XML object (i.e., before none of them). If child1 does not exist in this
        /// XML object, it returns without modifying this XML object.
        /// 
        /// See ECMA 13.4.4.19
        /// </summary>		
        internal XML InsertChildBefore (object newChild, object refChild)
        {
            XML refChildXml = lib.ToXML (refChild);
            XML newChildXml = lib.ToXML (newChild);

            if (newChildXml != null) {
                XmlNode newChildXmlNode = newChildXml.UnderlyingNode;
                if (newChildXml.UnderlyingNode.OwnerDocument == UnderlyingNode.OwnerDocument) {
                    if (UnderlyingNode.OwnerDocument != newChildXml.UnderlyingNode.OwnerDocument) {
                        newChildXml.UnderlyingNode =
                            UnderlyingNode.OwnerDocument.ImportNode (newChildXml.UnderlyingNode, true);
                    }
                    newChildXmlNode = newChildXml.UnderlyingNode;
                }

                XmlNode refChildXmlNode = null;
                if (refChildXml != null) {
                    refChildXmlNode = refChildXml.UnderlyingNode;
                }

                UnderlyingNode.InsertBefore (
                    ImportNode (newChildXml.UnderlyingNode), refChildXmlNode);
            }

            return this;
        }



        internal XMLList GetPropertyList (XMLName name)
        {
            XMLList result;

            // Get the named property
            if (name.IsDescendants) {
                result = Descendants (name);
            }
            else if (name.IsAttributeName) {
                result = Attribute (name);
            }
            else {
                result = Child (name);
            }

            return result;
        }


        /// <summary>
        /// XML.prototype.toString ( )
        /// 
        /// The toString method returns a string representation of this XML object
        /// per the ToString conversion operator described in section 10.1.
        /// 
        /// See ECMA 13.4.4.38
        /// </summary>
        /// <returns></returns>
        public override string ToString ()
        {
            return (string)GetDefaultValue (typeof (string));
        }

        private XmlNode ImportNode (XmlNode node)
        {
            if (node.OwnerDocument != UnderlyingNode.OwnerDocument) {
                if (node is XmlDocument) {
                    node = node.SelectSingleNode ("*");
                }
                return UnderlyingNode.OwnerDocument.ImportNode (node, true);
            }
            return node;
        }

        private XmlNode Rename (XmlNode node, string prefix, string localName, string namespaceUri)
        {
            if (node is XmlElement)
                return Rename ((XmlElement)node, prefix, localName, namespaceUri);
            throw ScriptRuntime.TypeErrorById ("Renaming of xml node type {0} is not supported.",
                node.NodeType.ToString ());
        }

        private XmlNode Rename (XmlElement source, string prefix, string localName, string namespaceUri)
        {
            XmlElement target = source.OwnerDocument.CreateElement (
                prefix, localName, namespaceUri);
            foreach (XmlAttribute attr in source.Attributes)
                target.Attributes.Append (attr);
            foreach (XmlNode child in source.ChildNodes)
                target.AppendChild (child);
            source.ParentNode.ReplaceChild (target, source);
            return target;
        }

        private void RemoveAllChildren ()
        {
            foreach (XmlNode child in underlyingNode.ChildNodes)
                underlyingNode.RemoveChild (child);
        }
    }
}
