//------------------------------------------------------------------------------
// <license file="XMLObject.cs">
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

    internal abstract class XMLObject : IdScriptableObject
    {

        private static readonly System.Object XMLOBJECT_TAG = new System.Object ();

        public override string ClassName
        {
            get
            {
                return "XMLObject";
            }
        }

        internal XMLLib lib = null;
        internal bool isPrototype = false;

        public XMLObject ()
        {
            ;
        }

        public XMLObject (IScriptable scope, IScriptable prototype)
            : base (scope, prototype)
        {
            ;
        }

        protected internal XMLObject (XMLLib lib, XMLObject prototype)
            : base (lib.GlobalScope, prototype)
        {
            this.lib = lib;
        }

        /// <summary>
        /// Custom <tt>+</tt> operator.
        /// Should return {@link Scriptable#NOT_FOUND} if this object does not have
        /// custom addition operator for the given value,
        /// or the result of the addition operation.
        /// <p>
        /// The default implementation returns {@link Scriptable#NOT_FOUND}
        /// to indicate no custom addition operation.
        /// 
        /// </summary>
        /// <param name="cx">the Context object associated with the current thread.
        /// </param>
        /// <param name="thisIsLeft">if true, the object should calculate this + value
        /// if false, the object should calculate value + this.
        /// </param>
        /// <param name="value">the second argument for addition operation.
        /// </param>		
        public virtual object AddValues (Context cx, bool thisIsLeft, object value)
        {
            return UniqueTag.NotFound;
        }

        public bool EcmaHas (Context cx, object id)
        {
            throw new NotImplementedException ();
        }

        public object EcmaGet (Context cx, object id)
        {
            XMLName name = XMLName.Parse (lib, cx, id);
            if (name == null) {
                long index = ScriptRuntime.lastUint32Result (cx);
                object result = base.Get ((int)index, this);
                if (result == UniqueTag.NotFound)
                    return Undefined.Value;
                return result;
            }
            return GetXMLProperty (name);
        }

        public void EcmaPut (Context cx, object id, object value)
        {
            if (cx == null)
                cx = Context.CurrentContext;
            XMLName xmlName = XMLName.Parse (lib, cx, id);
            if (xmlName == null) {
                long index = ScriptRuntime.lastUint32Result (cx);
                // TODO Fix this cast
                Put ((int)index, this, value);
                return;
            }
            PutXMLProperty (xmlName, value);
        }


        public bool EcmaDelete (Context cx, object id)
        {
            throw new NotImplementedException ();
        }


        public IRef MemberRef (Context cx, object elem, int memberTypeFlags)
        {
            XMLName name = XMLName.Parse (lib, cx, elem);
            if ((memberTypeFlags & Node.ATTRIBUTE_FLAG) != 0)
                name.IsAttributeName = true;
            if ((memberTypeFlags & Node.DESCENDANTS_FLAG) != 0)
                name.IsDescendants = true;
            name.BindTo (this);
            return name;
        }

        public IRef MemberRef (Context cx, object ns, object elem, int memberTypeFlags)
        {
            XMLName xmlName = lib.toQualifiedName (cx, ns, elem);
            if ((memberTypeFlags & Node.ATTRIBUTE_FLAG) != 0)
                xmlName.IsAttributeName = true;
            if ((memberTypeFlags & Node.DESCENDANTS_FLAG) != 0)
                xmlName.IsDescendants = true;
            xmlName.BindTo (this);
            return xmlName;
        }

        public BuiltinWith EnterWith (IScriptable scope)
        {
            throw new NotImplementedException ();
        }

        public BuiltinWith EnterDotQuery (IScriptable scope)
        {
            throw new NotImplementedException ();
        }


        protected internal override object EquivalentValues (object value)
        {
            return EquivalentXml (value);
        }

        protected object GetArgSafe (object [] args, int index)
        {
            if (index >= 0 && index < args.Length)
                return args [index];
            return null;
        }

        protected internal abstract IScriptable GetExtraMethodSource (Context cx);
        protected internal abstract bool EquivalentXml (object value);
        protected internal abstract object GetXMLProperty (XMLName name);
        protected internal abstract void PutXMLProperty (XMLName xmlName, object value);
        protected internal abstract string ToXMLString ();

    }
}
