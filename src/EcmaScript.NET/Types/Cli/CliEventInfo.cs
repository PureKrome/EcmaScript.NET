using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace EcmaScript.NET.Types.Cli
{

    public class CliEventInfo : CliObject
    {

        EventInfo m_EventInfo = null;

        public CliEventInfo (EventInfo ei)
        {
            m_EventInfo = ei;
            base.Init (m_EventInfo, m_EventInfo.GetType ());
        }

        public override string ClassName
        {
            get
            {
                return "NativeCliEventInfo";
            }
        }

        internal object Add (object val2, Context cx)
        {
            if (!(val2 is InterpretedFunction)) {
                return this;
            }
            InterpretedFunction ifn = (InterpretedFunction)val2;

            ScriptableCallback sc = (ScriptableCallback)Activator.CreateInstance (
                GetOrCreateType (m_EventInfo.EventHandlerType));
            sc.Context = cx;
            sc.Function = ifn;
            sc.ThisObj = ParentScope;
            sc.Scope = ifn;
            m_EventInfo.AddEventHandler ((ParentScope as CliObject).Object,
                Delegate.CreateDelegate (m_EventInfo.EventHandlerType, sc, "DynMethod")
            );
            return this;
        }
        internal object Del (object val2, Context cx)
        {
            if (!(val2 is InterpretedFunction)) {
                return this;
            }
            InterpretedFunction ifn = (InterpretedFunction)val2;

            ScriptableCallback sc = (ScriptableCallback)Activator.CreateInstance (
                GetOrCreateType (m_EventInfo.EventHandlerType));
            sc.Context = cx;
            sc.Function = ifn;
            sc.ThisObj = ParentScope;
            sc.Scope = ifn;
            m_EventInfo.RemoveEventHandler ((ParentScope as CliObject).Object,
                Delegate.CreateDelegate (m_EventInfo.EventHandlerType, sc, "DynMethod")
            );
            return this;
        }


        static Hashtable m_EventHandlerTypes = Hashtable.Synchronized (new Hashtable ());

        static Type GetOrCreateType (Type eventHandlerType)
        {
            if (m_EventHandlerTypes.Contains (eventHandlerType))
                return (Type)m_EventHandlerTypes [eventHandlerType];

            MethodInfo mi = eventHandlerType.GetMethod ("Invoke");

            Type [] parameterTypes = CliHelper.GetParameterTypes (mi.GetParameters ());

            var ab = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName ("Dyn_" + eventHandlerType.FullName.Replace (".", "_")), AssemblyBuilderAccess.Run);
            ModuleBuilder mb = ab.DefineDynamicModule ("DynModule");
            TypeBuilder tp = mb.DefineType ("DynType", TypeAttributes.Public, typeof (ScriptableCallback));
            MethodBuilder db = tp.DefineMethod ("DynMethod", MethodAttributes.Public, CallingConventions.Standard,
                mi.ReturnType, parameterTypes);
            ILGenerator ilg = db.GetILGenerator ();

            ilg.DeclareLocal (typeof (object []));

            int len = mi.GetParameters ().Length;

            // 1. Array create
            ilg.Emit (OpCodes.Ldarg_0);
            ilg.Emit (OpCodes.Ldc_I4, len);
            ilg.Emit (OpCodes.Newarr, typeof (object));
            ilg.Emit (OpCodes.Stloc_0);

            // 2. Parameter pushen
            for (int i = 0; i < len; i++) {
                ilg.Emit (OpCodes.Ldloc_0);
                ilg.Emit (OpCodes.Ldc_I4, i);
                ilg.Emit (OpCodes.Ldarg, i + 1);
                ilg.Emit (OpCodes.Stelem_Ref);
            }

            ilg.Emit (OpCodes.Ldloc_0);
            ilg.EmitCall (OpCodes.Call, typeof (ScriptableCallback).GetMethod ("ScriptInvoke"), null);
            ilg.Emit (OpCodes.Pop);
            ilg.Emit (OpCodes.Ret);

            Type type = tp.CreateTypeInfo();
            m_EventHandlerTypes [eventHandlerType] = type;
            return type;
        }


        public class ScriptableCallback
        {

            public Context Context;
            public IScriptable Scope;
            public IScriptable ThisObj;
            public BuiltinFunction Function;

            public object ScriptInvoke (object [] args)
            {
                return Function.Call (Context, Scope, ThisObj, args);
            }

        }

    }


}
