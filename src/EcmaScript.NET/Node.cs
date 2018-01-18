//------------------------------------------------------------------------------
// <license file="Node.cs">
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

using EcmaScript.NET.Collections;

namespace EcmaScript.NET
{

    /// <summary> This class implements the root of the intermediate representation.
    /// 
    /// </summary>
    public class Node
    {

        internal class GetterPropertyLiteral
        {
            internal object Property;

            public GetterPropertyLiteral (object property)
            {
                Property = property;
            }
        }

        internal class SetterPropertyLiteral
        {
            internal object Property;

            public SetterPropertyLiteral (object property)
            {
                Property = property;
            }
        }

        public Node FirstChild
        {
            get
            {
                return first;
            }

        }
        public Node LastChild
        {
            get
            {
                return last;
            }

        }
        public Node Next
        {
            get
            {
                return next;
            }

        }
        public Node LastSibling
        {
            get
            {
                Node n = this;
                while (n.next != null) {
                    n = n.next;
                }
                return n;
            }

        }
        public int Lineno
        {
            get
            {
                return lineno;
            }

        }
        /// <summary>Can only be called when <tt>getType() == Token.NUMBER</tt> </summary>
        public double Double
        {
            get
            {
                return ((NumberNode)this).number;
            }

            set
            {
                ((NumberNode)this).number = value;
            }

        }
        /// <summary>Can only be called when node has String context. </summary>		
        public string String
        {
            get
            {
                return ((StringNode)this).str;
            }

            set
            {
                if (value == null)
                    Context.CodeBug ();
                ((StringNode)this).str = value;
            }

        }

        public const int FUNCTION_PROP = 1;
        public const int LOCAL_PROP = 2;
        public const int LOCAL_BLOCK_PROP = 3;
        public const int REGEXP_PROP = 4;
        public const int CASEARRAY_PROP = 5;
        public const int TARGETBLOCK_PROP = 6;
        public const int VARIABLE_PROP = 7;
        public const int ISNUMBER_PROP = 8;
        public const int DIRECTCALL_PROP = 9;
        public const int SPECIALCALL_PROP = 10;
        public const int SKIP_INDEXES_PROP = 11;
        public const int OBJECT_IDS_PROP = 12;
        public const int INCRDECR_PROP = 13;
        public const int CATCH_SCOPE_PROP = 14;
        public const int LABEL_ID_PROP = 15;
        public const int MEMBER_TYPE_PROP = 16;
        public const int NAME_PROP = 17;
        public const int LAST_PROP = NAME_PROP;

        // values of ISNUMBER_PROP to specify
        // which of the children are Number Types
        public const int BOTH = 0;
        public const int LEFT = 1;
        public const int RIGHT = 2;

        public const int NON_SPECIALCALL = 0;
        public const int SPECIALCALL_EVAL = 1;
        public const int SPECIALCALL_WITH = 2;

        public const int DECR_FLAG = 0x1;
        public const int POST_FLAG = 0x2;

        public const int PROPERTY_FLAG = 0x1;
        public const int ATTRIBUTE_FLAG = 0x2;
        public const int DESCENDANTS_FLAG = 0x4; // x..y or x..@i


        private class NumberNode : Node
        {
            internal NumberNode (double number)
                : base (Token.NUMBER)
            {
                this.number = number;
            }

            internal double number;
        }

        private class StringNode : Node
        {
            internal StringNode (int Type, string str)
                : base (Type)
            {
                this.str = str;
            }

            internal string str;
        }

        public class Jump : Node
        {
            public Jump JumpStatement
            {
                get
                {
                    if (!(Type == Token.BREAK || Type == Token.CONTINUE))
                        Context.CodeBug ();
                    return jumpNode;
                }

                set
                {
                    if (!(Type == Token.BREAK || Type == Token.CONTINUE))
                        Context.CodeBug ();
                    if (value == null)
                        Context.CodeBug ();
                    if (this.jumpNode != null)
                        Context.CodeBug (); //only once
                    this.jumpNode = value;
                }

            }
            public Node Default
            {
                get
                {
                    if (!(Type == Token.SWITCH))
                        Context.CodeBug ();
                    return target2;
                }

                set
                {
                    if (!(Type == Token.SWITCH))
                        Context.CodeBug ();
                    if (value.Type != Token.TARGET)
                        Context.CodeBug ();
                    if (target2 != null)
                        Context.CodeBug (); //only once
                    target2 = value;
                }

            }
            public Node Finally
            {
                get
                {
                    if (!(Type == Token.TRY))
                        Context.CodeBug ();
                    return target2;
                }

                set
                {
                    if (!(Type == Token.TRY))
                        Context.CodeBug ();
                    if (value.Type != Token.TARGET)
                        Context.CodeBug ();
                    if (target2 != null)
                        Context.CodeBug (); //only once
                    target2 = value;
                }

            }
            public Jump Loop
            {
                get
                {
                    if (!(Type == Token.LABEL))
                        Context.CodeBug ();
                    return jumpNode;
                }

                set
                {
                    if (!(Type == Token.LABEL))
                        Context.CodeBug ();
                    if (value == null)
                        Context.CodeBug ();
                    if (jumpNode != null)
                        Context.CodeBug (); //only once
                    jumpNode = value;
                }

            }
            public Node Continue
            {
                get
                {
                    if (Type != Token.LOOP)
                        Context.CodeBug ();
                    return target2;
                }

                set
                {
                    if (Type != Token.LOOP)
                        Context.CodeBug ();
                    if (value.Type != Token.TARGET)
                        Context.CodeBug ();
                    if (target2 != null)
                        Context.CodeBug (); //only once
                    target2 = value;
                }

            }
            public Jump (int Type)
                : base (Type)
            {
            }

            internal Jump (int Type, int lineno)
                : base (Type, lineno)
            {
            }

            internal Jump (int Type, Node child)
                : base (Type, child)
            {
            }

            internal Jump (int Type, Node child, int lineno)
                : base (Type, child, lineno)
            {
            }

            public Node target;
            private Node target2;
            private Jump jumpNode;
        }

        private class PropListItem
        {
            internal PropListItem next;
            internal int Type;
            internal int intValue;
            internal object objectValue;
        }


        public Node (int nodeType)
        {
            Type = nodeType;
        }

        public Node (int nodeType, Node child)
        {
            Type = nodeType;
            first = last = child;
            child.next = null;
        }

        public Node (int nodeType, Node left, Node right)
        {
            Type = nodeType;
            first = left;
            last = right;
            left.next = right;
            right.next = null;
        }

        public Node (int nodeType, Node left, Node mid, Node right)
        {
            Type = nodeType;
            first = left;
            last = right;
            left.next = mid;
            mid.next = right;
            right.next = null;
        }

        public Node (int nodeType, int line)
        {
            Type = nodeType;
            lineno = line;
        }

        public Node (int nodeType, Node child, int line)
            : this (nodeType, child)
        {
            lineno = line;
        }

        public Node (int nodeType, Node left, Node right, int line)
            : this (nodeType, left, right)
        {
            lineno = line;
        }

        public Node (int nodeType, Node left, Node mid, Node right, int line)
            : this (nodeType, left, mid, right)
        {
            lineno = line;
        }

        public static Node newNumber (double number)
        {
            return new NumberNode (number);
        }

        public static Node newString (string str)
        {
            return new StringNode (Token.STRING, str);
        }

        public static Node newString (int Type, string str)
        {
            return new StringNode (Type, str);
        }

        public bool hasChildren ()
        {
            return first != null;
        }

        public Node getChildBefore (Node child)
        {
            if (child == first)
                return null;
            Node n = first;
            while (n.next != child) {
                n = n.next;
                if (n == null)
                    throw new Exception ("node is not a child");
            }
            return n;
        }

        public void addChildToFront (Node child)
        {
            child.next = first;
            first = child;
            if (last == null) {
                last = child;
            }
        }

        public void addChildToBack (Node child)
        {
            child.next = null;
            if (last == null) {
                first = last = child;
                return;
            }
            last.next = child;
            last = child;
        }

        public void addChildrenToFront (Node children)
        {
            Node lastSib = children.LastSibling;
            lastSib.next = first;
            first = children;
            if (last == null) {
                last = lastSib;
            }
        }

        public void addChildrenToBack (Node children)
        {
            if (last != null) {
                last.next = children;
            }
            last = children.LastSibling;
            if (first == null) {
                first = children;
            }
        }

        /// <summary> Add 'child' before 'node'.</summary>
        public void addChildBefore (Node newChild, Node node)
        {
            if (newChild.next != null)
                throw new Exception ("newChild had siblings in addChildBefore");
            if (first == node) {
                newChild.next = first;
                first = newChild;
                return;
            }
            Node prev = getChildBefore (node);
            addChildAfter (newChild, prev);
        }

        /// <summary> Add 'child' after 'node'.</summary>
        public void addChildAfter (Node newChild, Node node)
        {
            if (newChild.next != null)
                throw new Exception ("newChild had siblings in addChildAfter");
            newChild.next = node.next;
            node.next = newChild;
            if (last == node)
                last = newChild;
        }

        public void removeChild (Node child)
        {
            Node prev = getChildBefore (child);
            if (prev == null)
                first = first.next;
            else
                prev.next = child.next;
            if (child == last)
                last = prev;
            child.next = null;
        }

        public void replaceChild (Node child, Node newChild)
        {
            newChild.next = child.next;
            if (child == first) {
                first = newChild;
            }
            else {
                Node prev = getChildBefore (child);
                prev.next = newChild;
            }
            if (child == last)
                last = newChild;
            child.next = null;
        }

        public void replaceChildAfter (Node prevChild, Node newChild)
        {
            Node child = prevChild.next;
            newChild.next = child.next;
            prevChild.next = newChild;
            if (child == last)
                last = newChild;
            child.next = null;
        }

        private static string propToString (int propType)
        {
            if (Token.printTrees) {
                // If Context.printTrees is false, the compiler
                // can remove all these strings.
                switch (propType) {

                    case FUNCTION_PROP:
                        return "function";

                    case LOCAL_PROP:
                        return "local";

                    case LOCAL_BLOCK_PROP:
                        return "local_block";

                    case REGEXP_PROP:
                        return "regexp";

                    case CASEARRAY_PROP:
                        return "casearray";


                    case TARGETBLOCK_PROP:
                        return "targetblock";

                    case VARIABLE_PROP:
                        return "variable";

                    case ISNUMBER_PROP:
                        return "isnumber";

                    case DIRECTCALL_PROP:
                        return "directcall";


                    case SPECIALCALL_PROP:
                        return "specialcall";

                    case SKIP_INDEXES_PROP:
                        return "skip_indexes";

                    case OBJECT_IDS_PROP:
                        return "object_ids_prop";

                    case INCRDECR_PROP:
                        return "incrdecr_prop";

                    case CATCH_SCOPE_PROP:
                        return "catch_scope_prop";

                    case LABEL_ID_PROP:
                        return "label_id_prop";

                    case MEMBER_TYPE_PROP:
                        return "member_Type_prop";

                    case NAME_PROP:
                        return "name_prop";


                    default:
                        Context.CodeBug ();
                        break;

                }
            }
            return null;
        }

        private PropListItem lookupProperty (int propType)
        {
            PropListItem x = propListHead;
            while (x != null && propType != x.Type) {
                x = x.next;
            }
            return x;
        }

        private PropListItem ensureProperty (int propType)
        {
            PropListItem item = lookupProperty (propType);
            if (item == null) {
                item = new PropListItem ();
                item.Type = propType;
                item.next = propListHead;
                propListHead = item;
            }
            return item;
        }

        public void removeProp (int propType)
        {
            PropListItem x = propListHead;
            if (x != null) {
                PropListItem prev = null;
                while (x.Type != propType) {
                    prev = x;
                    x = x.next;
                    if (x == null) {
                        return;
                    }
                }
                if (prev == null) {
                    propListHead = x.next;
                }
                else {
                    prev.next = x.next;
                }
            }
        }

        public object getProp (int propType)
        {
            PropListItem item = lookupProperty (propType);
            if (item == null) {
                return null;
            }
            return item.objectValue;
        }

        public int getIntProp (int propType, int defaultValue)
        {
            PropListItem item = lookupProperty (propType);
            if (item == null) {
                return defaultValue;
            }
            return item.intValue;
        }

        public int getExistingIntProp (int propType)
        {
            PropListItem item = lookupProperty (propType);
            if (item == null) {
                Context.CodeBug ();
            }
            return item.intValue;
        }

        public void putProp (int propType, object prop)
        {
            if (prop == null) {
                removeProp (propType);
            }
            else {
                PropListItem item = ensureProperty (propType);
                item.objectValue = prop;
            }
        }

        public void putIntProp (int propType, int prop)
        {
            PropListItem item = ensureProperty (propType);
            item.intValue = prop;
        }

        public static Node newTarget ()
        {
            return new Node (Token.TARGET);
        }

        public int labelId ()
        {
            if (Type != Token.TARGET)
                Context.CodeBug ();
            return getIntProp (LABEL_ID_PROP, -1);
        }

        public void labelId (int labelId)
        {
            if (Type != Token.TARGET)
                Context.CodeBug ();
            putIntProp (LABEL_ID_PROP, labelId);
        }

        public override string ToString ()
        {
            if (Token.printTrees) {
                System.Text.StringBuilder sb = new System.Text.StringBuilder ();
                toString (new ObjToIntMap (), sb);
                return sb.ToString ();
            }
            return Convert.ToString (Type);
        }

        private void toString (ObjToIntMap printIds, System.Text.StringBuilder sb)
        {
            if (Token.printTrees) {
                sb.Append (Token.name (this.Type));
                if (this is StringNode) {
                    sb.Append (' ');
                    sb.Append (String);
                }
                else if (this is ScriptOrFnNode) {
                    ScriptOrFnNode sof = (ScriptOrFnNode)this;
                    if (this is FunctionNode) {
                        FunctionNode fn = (FunctionNode)this;
                        sb.Append (' ');
                        sb.Append (fn.FunctionName);
                    }
                    sb.Append (" [source name: ");
                    sb.Append (sof.SourceName);
                    sb.Append ("] [encoded source length: ");
                    sb.Append (sof.EncodedSourceEnd - sof.EncodedSourceStart);
                    sb.Append ("] [base line: ");
                    sb.Append (sof.BaseLineno);
                    sb.Append ("] [end line: ");
                    sb.Append (sof.EndLineno);
                    sb.Append (']');
                }
                else if (this is Jump) {
                    Jump jump = (Jump)this;
                    if (this.Type == Token.BREAK || this.Type == Token.CONTINUE) {
                        sb.Append (" [label: ");
                        appendPrintId (jump.JumpStatement, printIds, sb);
                        sb.Append (']');
                    }
                    else if (this.Type == Token.TRY) {
                        Node catchNode = jump.target;
                        Node finallyTarget = jump.Finally;
                        if (catchNode != null) {
                            sb.Append (" [catch: ");
                            appendPrintId (catchNode, printIds, sb);
                            sb.Append (']');
                        }
                        if (finallyTarget != null) {
                            sb.Append (" [finally: ");
                            appendPrintId (finallyTarget, printIds, sb);
                            sb.Append (']');
                        }
                    }
                    else if (this.Type == Token.LABEL || this.Type == Token.LOOP || this.Type == Token.SWITCH) {
                        sb.Append (" [break: ");
                        appendPrintId (jump.target, printIds, sb);
                        sb.Append (']');
                        if (this.Type == Token.LOOP) {
                            sb.Append (" [continue: ");
                            appendPrintId (jump.Continue, printIds, sb);
                            sb.Append (']');
                        }
                    }
                    else {
                        sb.Append (" [target: ");
                        appendPrintId (jump.target, printIds, sb);
                        sb.Append (']');
                    }
                }
                else if (this.Type == Token.NUMBER) {
                    sb.Append (' ');
                    sb.Append (Double);
                }
                else if (this.Type == Token.TARGET) {
                    sb.Append (' ');
                    appendPrintId (this, printIds, sb);
                }
                if (lineno != -1) {
                    sb.Append (' ');
                    sb.Append (lineno);
                }

                for (PropListItem x = propListHead; x != null; x = x.next) {
                    int Type = x.Type;
                    sb.Append (" [");
                    sb.Append (propToString (Type));
                    sb.Append (": ");
                    string value;
                    switch (Type) {

                        case TARGETBLOCK_PROP:  // can't add this as it recurses
                            value = "target block property";
                            break;

                        case LOCAL_BLOCK_PROP:  // can't add this as it is dull
                            value = "last local block";
                            break;

                        case ISNUMBER_PROP:
                            switch (x.intValue) {

                                case BOTH:
                                    value = "both";
                                    break;

                                case RIGHT:
                                    value = "right";
                                    break;

                                case LEFT:
                                    value = "left";
                                    break;

                                default:
                                    throw Context.CodeBug ();

                            }
                            break;

                        case SPECIALCALL_PROP:
                            switch (x.intValue) {

                                case SPECIALCALL_EVAL:
                                    value = "eval";
                                    break;

                                case SPECIALCALL_WITH:
                                    value = "with";
                                    break;

                                default:
                                    // NON_SPECIALCALL should not be stored
                                    throw Context.CodeBug ();

                            }
                            break;

                        default:
                            object obj = x.objectValue;
                            if (obj != null) {
                                value = obj.ToString ();
                            }
                            else {
                                value = Convert.ToString (x.intValue);
                            }
                            break;

                    }
                    sb.Append (value);
                    sb.Append (']');
                }
            }
        }

        public string toStringTree (ScriptOrFnNode treeTop)
        {
            if (Token.printTrees) {
                System.Text.StringBuilder sb = new System.Text.StringBuilder ();
                toStringTreeHelper (treeTop, this, null, 0, sb);
                return sb.ToString ();
            }
            return null;
        }

        private static void toStringTreeHelper (ScriptOrFnNode treeTop, Node n, ObjToIntMap printIds, int level, System.Text.StringBuilder sb)
        {
            if (Token.printTrees) {
                if (printIds == null) {
                    printIds = new ObjToIntMap ();
                    generatePrintIds (treeTop, printIds);
                }
                for (int i = 0; i != level; ++i) {
                    sb.Append (" ");
                }
                n.toString (printIds, sb);
                sb.Append ('\n');
                for (Node cursor = n.FirstChild; cursor != null; cursor = cursor.Next) {
                    if (cursor.Type == Token.FUNCTION) {
                        int fnIndex = cursor.getExistingIntProp (Node.FUNCTION_PROP);
                        FunctionNode fn = treeTop.getFunctionNode (fnIndex);
                        toStringTreeHelper (fn, fn, null, level + 1, sb);
                    }
                    else {
                        toStringTreeHelper (treeTop, cursor, printIds, level + 1, sb);
                    }
                }
            }
        }

        private static void generatePrintIds (Node n, ObjToIntMap map)
        {
            if (Token.printTrees) {
                map.put (n, map.size ());
                for (Node cursor = n.FirstChild; cursor != null; cursor = cursor.Next) {
                    generatePrintIds (cursor, map);
                }
            }
        }

        private static void appendPrintId (Node n, ObjToIntMap printIds, System.Text.StringBuilder sb)
        {
            if (Token.printTrees) {
                if (n != null) {
                    int id = printIds.Get (n, -1);
                    sb.Append ('#');
                    if (id != -1) {
                        sb.Append (id + 1);
                    }
                    else {
                        sb.Append ("<not_available>");
                    }
                }
            }
        }

        internal int Type; // Type of the node; Token.NAME for example
        internal Node next; // next sibling
        private Node first; // first element of a linked list of children
        private Node last; // last element of a linked list of children
        private int lineno = -1; // encapsulated int data; depends on Type

        /// <summary> Linked list of properties. Since vast majority of nodes would have
        /// no more then 2 properties, linked list saves memory and provides
        /// fast lookup. If this does not holds, propListHead can be replaced
        /// by UintMap.
        /// </summary>
        private PropListItem propListHead;
    }
}