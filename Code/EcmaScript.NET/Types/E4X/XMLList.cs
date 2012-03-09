//------------------------------------------------------------------------------
// <license file="XMLList.cs">
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
using System.Collections;
using System.Xml;

namespace EcmaScript.NET.Types.E4X
{

    class XMLList : XMLObject, IFunction, IEnumerable
    {

        private static readonly System.Object XMLOBJECT_TAG = new System.Object ();

        public override string ClassName
        {
            get
            {
                return "XMLList";
            }
        }

        public XML this [int index]
        {
            get
            {
                return (XML)m_Nodes [index];
            }
        }

        public XMLList (XMLLib lib)
            : base (lib, lib.xmlListPrototype)
        {
            this.lib = lib;
        }

        public XMLList (XMLLib lib, object inputObject)
            : this (lib)
        {
            string frag;
            if (inputObject == null || inputObject is Undefined) {
                frag = "";
            }
            else if (inputObject is XML) {
                XML xml = (XML)inputObject;
                Add (xml);
            }
            else if (inputObject is XMLList) {
                XMLList xmll = (XMLList)inputObject;
                AddRange (xmll);
            }
            else {
                frag = ScriptConvert.ToString (inputObject).Trim ();
                if (!frag.StartsWith ("<>")) {
                    frag = "<>" + frag + "</>";
                }
                frag = "<fragment>" + frag.Substring (2);
                if (!frag.EndsWith ("</>")) {
                    throw ScriptRuntime.TypeError ("XML with anonymous tag missing end anonymous tag");
                }
                frag = frag.Substring (0, frag.Length - 3) + "</fragment>";

                XML orgXML = XML.CreateFromJS (lib, frag);

                // Now orphan the children and add them to our XMLList.
                XMLList children = (XMLList)orgXML.Children ();
                AddRange (children);
            }
        }

        public void ExportAsJSClass (bool zealed)
        {
            base.ExportAsJSClass (MAX_PROTOTYPE_ID, lib.GlobalScope, zealed);
            isPrototype = true;
        }

        #region PrototypeIds
        private const int Id_constructor = 1;
        private const int Id_attribute = 2;
        private const int Id_attributes = 3;
        private const int Id_child = 4;
        private const int Id_children = 5;
        private const int Id_comments = 6;
        private const int Id_contains = 7;
        private const int Id_copy = 8;
        private const int Id_descendants = 9;
        private const int Id_elements = 10;
        private const int Id_hasOwnProperty = 11;
        private const int Id_hasComplexContent = 12;
        private const int Id_hasSimpleContent = 13;
        private const int Id_length = 14;
        private const int Id_normalize = 15;
        private const int Id_parent = 16;
        private const int Id_processingInstructions = 17;
        private const int Id_propertyIsEnumerable = 18;
        private const int Id_text = 19;
        private const int Id_toString = 20;
        private const int Id_toXMLString = 21;
        private const int Id_valueOf = 22;
        private const int Id_domNode = 23;
        private const int Id_domNodeList = 24;
        private const int Id_xpath = 25;
        private const int Id_addNamespace = 26;
        private const int Id_appendChild = 27;
        private const int Id_childIndex = 28;
        private const int Id_inScopeNamespaces = 29;
        private const int Id_insertChildAfter = 30;
        private const int Id_insertChildBefore = 31;
        private const int Id_localName = 32;
        private const int Id_name = 33;
        private const int Id_namespace = 34;
        private const int Id_namespaceDeclarations = 35;
        private const int Id_nodeKind = 36;
        private const int Id_prependChild = 37;
        private const int Id_removeNamespace = 38;
        private const int Id_replace = 39;
        private const int Id_setChildren = 40;
        private const int Id_setLocalName = 41;
        private const int Id_setName = 42;
        private const int Id_setNamespace = 43;
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
                case Id_constructor:
                    arity = 1;
                    s = "constructor";
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
                case Id_length:
                    arity = 0;
                    s = "length";
                    break;
                case Id_processingInstructions:
                    arity = 1;
                    s = "processingInstructions";
                    break;
                case Id_propertyIsEnumerable:
                    arity = 1;
                    s = "propertyIsEnumerable";
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
                case Id_addNamespace:
                    arity = 1;
                    s = "addNamespace";
                    break;
                case Id_appendChild:
                    arity = 1;
                    s = "appendChild";
                    break;
                case Id_childIndex:
                    arity = 0;
                    s = "childIndex";
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
                default:
                    throw new System.ArgumentException (System.Convert.ToString (id));
            }
            InitPrototypeMethod (XMLOBJECT_TAG, id, s, arity);
        }

        public override object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (!f.HasTag (XMLOBJECT_TAG)) {
                return base.ExecIdCall (f, cx, scope, thisObj, args);
            }

            int id = f.MethodId;
            if (id == Id_constructor) {
                return JsConstructor (cx, thisObj == null, args);
            }

            // All XML.prototype methods require thisObj to be XML
            if (!(thisObj is XMLList))
                throw IncompatibleCallError (f);
            XMLList realThis = (XMLList)thisObj;

            XMLName xmlName;

            switch (id) {
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

                case Id_children:
                    return realThis.Children ();

                case Id_contains:
                    return realThis.Contains (GetArgSafe (args, 0));

                case Id_copy:
                    return realThis.Copy ();

                case Id_descendants: {
                        xmlName = (args.Length == 0)
                            ? XMLName.FormStar () : XMLName.Parse (lib, cx, GetArgSafe (args, 0));
                        return realThis.Descendants (xmlName);
                    }

                case Id_hasOwnProperty:
                    xmlName = XMLName.Parse (lib, cx, GetArgSafe (args, 0));
                    return realThis.HasOwnProperty (xmlName);


                case Id_hasComplexContent:
                    return realThis.HasComplexContent ();

                case Id_hasSimpleContent:
                    return realThis.HasSimpleContent ();

                case Id_length:
                    return realThis.Length ();

                case Id_normalize:
                    realThis.Normalize ();
                    return Undefined.Value;

                case Id_parent:
                    return realThis.Parent ();

                case Id_processingInstructions:
                    xmlName = (args.Length > 0) ? XMLName.Parse (lib, cx, args [0]) : XMLName.FormStar ();
                    return realThis.ProcessingInstructions (xmlName);

                case Id_propertyIsEnumerable: {
                        return realThis.PropertyIsEnumerable (GetArgSafe (args, 0));
                    }
                case Id_text:
                    return realThis.Text ();

                case Id_toString:
                    return realThis.ToString ();

                case Id_toXMLString:
                    return realThis.ToXMLString ();

                case Id_valueOf:
                    return realThis;

                case Id_addNamespace:
                    return realThis.DelegateTo ("addNamespace").AddNamespace (GetArgSafe (args, 0));

                case Id_appendChild:
                    return realThis.DelegateTo ("appendChild").AppendChild (GetArgSafe (args, 0));

                case Id_childIndex:
                    return realThis.DelegateTo ("childIndex").ChildIndex ();

                case Id_inScopeNamespaces:
                    return realThis.DelegateTo ("inScopeNamespaces").InScopeNamespaces ();

                case Id_insertChildAfter:
                    return realThis.DelegateTo ("insertChildAfter").InsertChildAfter (GetArgSafe (args, 0), GetArgSafe (args, 1));

                case Id_insertChildBefore:
                    return realThis.DelegateTo ("insertChildBefore").InsertChildBefore (GetArgSafe (args, 0), GetArgSafe (args, 1));

                case Id_localName:
                    return realThis.DelegateTo ("localName").LocalName ();

                case Id_name:
                    return realThis.DelegateTo ("name").Name ();

                case Id_namespace:
                    return realThis.DelegateTo ("namespace").Namespace (GetArgSafe (args, 0));

                case Id_namespaceDeclarations:
                    return realThis.DelegateTo ("namespaceDeclarations").NamespaceDeclarations ();

                case Id_nodeKind:
                    return realThis.DelegateTo ("nodeKind").NodeKind ();

                case Id_prependChild:
                    return realThis.DelegateTo ("prependChild").PrependChild (GetArgSafe (args, 0));

                case Id_removeNamespace:
                    return realThis.DelegateTo ("removeNamespace").RemoveNamespace (GetArgSafe (args, 0));

                case Id_replace:
                    return realThis.DelegateTo ("replace").Replace (GetArgSafe (args, 0), GetArgSafe (args, 1));

                case Id_setChildren:
                    return realThis.DelegateTo ("setChildren").SetChildren (GetArgSafe (args, 0));

                case Id_setLocalName:
                    realThis.DelegateTo ("setLocalName").SetLocalName (GetArgSafe (args, 0));
                    return Undefined.Value;

                case Id_setName:
                    realThis.DelegateTo ("setName").SetName (GetArgSafe (args, 0));
                    return Undefined.Value;

                case Id_setNamespace:
                    realThis.DelegateTo ("setNamespace").SetNamespace (GetArgSafe (args, 0));
                    return Undefined.Value;

            }

            throw new System.ArgumentException (System.Convert.ToString (id));
        }


        private XML DelegateTo (string methodName)
        {
            if (Length () != 1)
                throw ScriptRuntime.TypeError ("The " + methodName + " method works only on lists containing one item");
            return this [0];
        }

        internal ArrayList m_Nodes = new ArrayList ();

        internal void Add (XML node)
        {
            m_Nodes.Add (node);
        }

        internal void AddRange (XMLList list)
        {
            foreach (XML xml in list.m_Nodes)
                Add (xml);
        }

        internal int Length ()
        {
            return m_Nodes.Count;
        }

        public object Call (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            throw new Exception ("The method or operation is not implemented.");
        }

        public IScriptable Construct (Context cx, IScriptable scope, object [] args)
        {
            throw new Exception ("The method or operation is not implemented.");
        }

        private object JsConstructor (Context cx, bool inNewExpr, object [] args)
        {
            if (args.Length == 0) {
                return new XMLList (lib);
            }
            else {
                Object arg0 = args [0];
                if (!inNewExpr && arg0 is XMLList) {
                    // XMLList(XMLList) returns the same object.
                    return arg0;
                }
                return new XMLList (lib, arg0);
            }
        }

        protected internal override string ToXMLString ()
        {
            return ToSource (0);
        }

        private bool HasSimpleContent ()
        {
            return (Length () == 1);
        }

        private bool HasComplexContent ()
        {
            return !HasSimpleContent ();
        }


        protected override internal IScriptable GetExtraMethodSource (Context cx)
        {
            if (HasSimpleContent ())
                return (m_Nodes [0] as XML).GetExtraMethodSource (cx);
            return null;
        }

        private string ToSource (int indent)
        {
            StringBuilder sb = new StringBuilder ();
            foreach (XML node in m_Nodes)
                sb.Append (node.ToSource (indent));
            return sb.ToString ();
        }

        public override bool Has (int index, IScriptable start)
        {
            return (index >= 0 && index < Length ());
        }

        public override object Get (int index, IScriptable start)
        {
            if (!Has (index, start))
                return base.Get (index, start);
            return m_Nodes [index];
        }


        private string NodeKind ()
        {
            if (Length () == 1)
                return (m_Nodes [0] as XML).NodeKind ();
            throw ScriptRuntime.TypeError ("The nodeKind method works only on lists containing one item");

        }

        private object Namespace (string prefix)
        {
            if (Length () == 1)
                return (m_Nodes [0] as XML).Namespace (prefix);
            throw ScriptRuntime.TypeError ("The namespace method works only on lists containing one item");
        }

        /// <summary>
        /// If list is empty, return undefined, if elements have different parents return undefined,
        /// If they all have the same parent, return that parent.
        /// </summary>
        private object Parent ()
        {
            if (Length () == 0)
                return Undefined.Value;
            object parent = null;
            foreach (XML xml in m_Nodes) {
                if (parent == null)
                    parent = xml.Parent ();
                else
                    if (!parent.Equals (xml.Parent ()))
                        return Undefined.Value;
            }
            return parent;
        }

        private string Text ()
        {
            StringBuilder sb = new StringBuilder ();
            foreach (XML xml in this)
                sb.Append (xml.Text ());
            return sb.ToString ();
        }

        private XMLObject Normalize ()
        {
            foreach (XML xml in m_Nodes)
                xml.Normalize ();
            return this;
        }


        private XMLList Children ()
        {
            XMLList list = new XMLList (lib);
            foreach (XML xml in m_Nodes) {
                list.AddRange (xml.Children ());
            }
            return list;
        }



        public override object [] GetIds ()
        {
            if (isPrototype) {
                return new object [0];
            }
            else {
                object [] ret = new object [Length ()];
                for (int i = 0; i < Length (); i++)
                    ret [i] = i;
                return ret;
            }
        }



        public override object GetDefaultValue (Type typeHint)
        {
            if (m_Nodes.Count == 1) {
                return ((XML)m_Nodes [0]).GetDefaultValue (typeHint);
            }
            return ToSource (0);
        }

        private XMLList Attribute (XMLName xmlName)
        {
            XMLList list = new XMLList (lib);
            foreach (XML xml in this)
                list.AddRange (xml.Attribute (xmlName));
            return list;
        }

        private XMLList Attributes ()
        {
            XMLList list = new XMLList (lib);
            foreach (XML xml in this)
                list.AddRange (xml.Attributes ());
            return list;
        }

        protected internal override object GetXMLProperty (XMLName name)
        {
            if (isPrototype) {
                return base.Get (name.localName, this);
            }

            XMLList list = new XMLList (lib);
            foreach (XML xml in m_Nodes) {
                list.AddRange ((XMLList)xml.GetXMLProperty (name));
            }
            return list;
        }

        private XMLList Descendants (XMLName xmlName)
        {
            XMLList list = new XMLList (lib);
            foreach (XML xml in this)
                list.AddRange (xml.Descendants (xmlName));
            return list;
        }

        /// <summary>
        /// XMLList.prototype.child ( propertyName )
        /// 
        /// The child method calls the child() method of each XML object in
        /// this XMLList object and returns an XMLList containing the results
        /// in order.
        /// 
        /// See ECMA 13.5.4.4
        /// </summary>
        private XMLList Child (XMLName xmlName)
        {
            XMLList list = new XMLList (lib);
            foreach (XML xml in this)
                list.AddRange (xml.Child (xmlName));
            return list;
        }

        private XMLList Child (long index)
        {
            XMLList list = new XMLList (lib);
            foreach (XML xml in this)
                list.AddRange (xml.Child (index));
            return list;
        }


        protected internal override void PutXMLProperty (XMLName name, object value)
        {
            if (isPrototype)
                return;
            if (value == null)
                value = "null";
            else if (value is Undefined)
                value = "undefined";

            if (Length () > 1) {
                throw ScriptRuntime.TypeError ("Assignment to lists with more that one item is not supported");
            }

            throw new NotImplementedException ();
        }

        protected internal override bool EquivalentXml (object value)
        {
            if (Length () == 0 && value is Undefined)
                return true;
            if (Length () == 1) {
                return this [0].EquivalentXml (value);
            }
            if (value is XMLList) {
                return value.ToString ().Equals (ToSource (0));
            }
            return false;
        }

        private object Copy ()
        {
            XMLList list = new XMLList (lib);
            foreach (XML xml in m_Nodes) {
                list.Add ((XML)xml.Copy ());
            }
            return list;
        }

        private object [] NamespaceDeclarations ()
        {

            ArrayList namespaceDeclarations = new ArrayList ();
            foreach (XML xml in m_Nodes) {
                foreach (Namespace ns in xml.NamespaceDeclarations ()) {
                    if (!namespaceDeclarations.Contains (ns))
                        namespaceDeclarations.Add (ns);
                }
            }
            return (object [])namespaceDeclarations.ToArray ();

        }

        private bool HasOwnProperty (XMLName xmlName)
        {
            if (isPrototype) {
                return FindPrototypeId (xmlName.localName) != 0;
            }
            else {
                return GetPropertyList (xmlName).Length () > 0;
            }
        }

        internal XMLList GetPropertyList (XMLName xmlName)
        {
            XMLList list = new XMLList (lib);
            foreach (XML xml in m_Nodes) {
                list.AddRange (xml.GetPropertyList (xmlName));
            }
            return list;
        }

        private void SetNamespace (object ns)
        {

        }

        /// <summary>
        /// XMLList.prototype.contains ( value )
        /// 
        /// The contains method returns a boolean value indicating whether
        /// this XMLList object contains an XML object that compares equal
        /// to the given value.
        /// 
        /// See ECMA 13.5.4.8
        /// </summary>
        private bool Contains (object value)
        {
            // TODO: Performance
            foreach (XML xml in this) {
                if (xml.EquivalentXml (value))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// XMLList.prototype.processingInstructions ( [ name ] )
        /// 
        /// The processingInstructions method calls the processingInstructions
        /// method of each XML object in this XMLList object passing the optional
        /// parameter name (or "*" if it is omitted) and returns an XMList
        /// containing the results in order.
        /// 
        /// See ECMA 
        /// </summary>
        private XMLList ProcessingInstructions (XMLName name)
        {
            XMLList list = new XMLList (lib);
            foreach (XML xml in this) {
                list.AddRange (xml.ProcessingInstructions (name));
            }
            return list;
        }

        /// <summary>
        /// XMLList.prototype.propertyIsEnumerable ( P )
        /// 
        /// The propertyIsEnumerable method returns a Boolean value
        /// indicating whether the property P will be included in the
        /// set of properties iterated over when this XMLList object is used
        /// in a for-in statement.
        /// 
        /// See ECMA 13.5.4.19
        /// </summary>
        internal bool PropertyIsEnumerable (object p)
        {
            throw new NotImplementedException ();
        }

        /// <summary>
        /// XMLList.prototype.valueOf ( )
        /// 
        /// The valueOf method returns this XMLList object.
        /// 
        /// See ECMA 
        /// </summary>		
        private XMLObject ValueOf ()
        {
            return this;
        }

        public IEnumerator GetEnumerator ()
        {
            return m_Nodes.GetEnumerator ();
        }

    }
}
