//------------------------------------------------------------------------------
// <license file="NativeCliType.cs">
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
using System.Collections;
using System.Text;
using System.Reflection;

using EcmaScript.NET;
using EcmaScript.NET.Attributes;

namespace EcmaScript.NET.Types.Cli
{

    public class CliType : BaseFunction, Wrapper
    {

        private CliObject m_CliObject = null;

        public override string ClassName
        {
            get
            {
                return m_Type.FullName;
            }
        }

        public object Unwrap ()
        {
            return m_CliObject;
        }

        private Type m_Type = null;

        public Type UnderlyingType
        {
            get { return m_Type; }
        }

        private CliType (Type type)
        {
            m_Type = type;


            // Check for class attribute
            m_ClassAttribute = (EcmaScriptClassAttribute)
                CliHelper.GetCustomAttribute (m_Type, typeof (EcmaScriptClassAttribute));
        }

        private void Init ()
        {
            m_CliObject = new CliObject (m_Type, m_Type);
        }

        private static Hashtable typeCache = new Hashtable ();

        public static CliType GetNativeCliType (Type type)
        {
            if (typeCache.ContainsKey (type)) {
                return (CliType)typeCache [type];
            }
            CliType cliType = new CliType (type);
            lock (typeCache) {
                typeCache [type] = cliType;
                cliType.Init ();
            }
            return cliType;
        }

        private MemberInfo [] m_IndexGetter = null;

        public MemberInfo [] IndexGetter
        {
            get
            {
                if (m_IndexGetter == null) {
                    m_IndexGetter = m_Type.GetMember ("get_Item");
                    if (m_IndexGetter == null)
                        m_IndexGetter = new MemberInfo [0];
                }
                return m_IndexGetter;
            }
        }

        public EcmaScriptClassAttribute m_ClassAttribute = null;

        public EcmaScriptClassAttribute ClassAttribute
        {
            get { return m_ClassAttribute; }
        }

        private EcmaScriptFunctionAttribute [] m_FunctionAttributes = null;

        public EcmaScriptFunctionAttribute [] FunctionAttributes
        {
            get
            {
                if (m_FunctionAttributes != null)
                    return m_FunctionAttributes;

                ArrayList attributes = new ArrayList ();
                foreach (MethodInfo mi in m_Type.GetMethods ()) {
                    EcmaScriptFunctionAttribute funAttribute =
                        (EcmaScriptFunctionAttribute)CliHelper.GetCustomAttribute (typeof (EcmaScriptFunctionAttribute), mi);
                    if (funAttribute != null) {
                        funAttribute.MethodInfo = mi;
                        attributes.Add (funAttribute);
                    }
                }
                return (m_FunctionAttributes =
                    (EcmaScriptFunctionAttribute [])attributes.ToArray (typeof (EcmaScriptFunctionAttribute)));
            }
        }

        private Hashtable functionCache = new Hashtable ();

        public CliMethodInfo GetFunctions (string name)
        {
            if (functionCache.ContainsKey (name))
                return (CliMethodInfo)functionCache [name];

            ArrayList methods = new ArrayList ();
            foreach (MethodInfo mi in m_Type.GetMethods ()) {
                if (0 == string.Compare (name, mi.Name)) {
                    methods.Add (mi);
                }
            }


            CliMethodInfo nmi = null;
            if (methods.Count > 0) {
                nmi = new CliMethodInfo (name, (MethodInfo [])methods.ToArray (typeof (MethodInfo)), null);
            }

            lock (functionCache) {
                functionCache [name] = nmi;
            }

            return nmi;
        }

        private Hashtable functionsWithAttributeCache = new Hashtable ();

        public CliMethodInfo GetFunctionsWithAttribute (string name)
        {
            if (functionsWithAttributeCache.ContainsKey (name))
                return (CliMethodInfo)functionsWithAttributeCache [name];

            ArrayList methods = new ArrayList ();

            foreach (EcmaScriptFunctionAttribute funAttr in FunctionAttributes) {
                if (0 == string.Compare (funAttr.Name, name)) {
                    methods.Add (funAttr.MethodInfo);
                }
            }

            CliMethodInfo mis = null;
            if (methods.Count > 0) {
                mis = new CliMethodInfo (name, (MethodInfo [])methods.ToArray (typeof (MethodInfo)), null);
            }

            lock (functionsWithAttributeCache) {
                functionsWithAttributeCache [name] = mis;
            }

            return mis;
        }

        private MemberInfo [] m_IndexSetter = null;

        public MemberInfo [] IndexSetter
        {
            get
            {
                if (m_IndexSetter == null) {
                    m_IndexSetter = m_Type.GetMember ("set_Item");
                    if (m_IndexSetter == null)
                        m_IndexSetter = new MemberInfo [0];
                }
                return m_IndexSetter;
            }
        }

        public override object Call (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            try {
                return cx.Wrap (scope, Activator.CreateInstance (
                    m_Type, args), m_Type);
            }
            catch (Exception ex) {
                throw Context.ThrowAsScriptRuntimeEx (ex);
            }
        }

        public override object Get (string name, IScriptable start)
        {
            object obj = base.Get (name, start);
            if (obj == UniqueTag.NotFound)
                obj = m_CliObject.Get (name, start);
            return obj;
        }

        private Hashtable propertyCache = new Hashtable ();

        public PropertyInfo GetCachedProperty (string name)
        {
            if (propertyCache.Contains (name))
                return (PropertyInfo)propertyCache [name];
            PropertyInfo pi = m_Type.GetProperty (name);
            lock (propertyCache) {
                propertyCache [name] = pi;
            }
            return pi;
        }

        private Hashtable fieldCache = new Hashtable ();

        public FieldInfo GetCachedField (string name)
        {
            if (fieldCache.Contains (name))
                return (FieldInfo)fieldCache [name];
            FieldInfo fi = m_Type.GetField (name);
            lock (fieldCache) {
                fieldCache [name] = fi;
            }
            return fi;
        }

        internal EventInfo GetCachedEvent (string name)
        {
            return m_Type.GetEvent (name);
        }

        public override object GetDefaultValue (Type typeHint)
        {
            if (typeHint == typeof (String))
                return ToString ();
            return base.GetDefaultValue (typeHint);
        }

        public override string ToString ()
        {
            string ret = "function " + ClassName + "() \n";
            ret += "{/*\n";
            foreach (ConstructorInfo ci in m_Type.GetConstructors ()) {
                ret += CliHelper.ToSignature (ci) + "\n";
            }
            foreach (MemberInfo mi in m_Type.GetMembers (BindingFlags.Static | BindingFlags.Public)) {
                ret += CliHelper.ToSignature (mi) + "\n";
            }
            ret += "*/}";
            return ret;
        }



    }
}
