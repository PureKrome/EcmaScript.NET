//------------------------------------------------------------------------------
// <license file="NativeCliArray.cs">
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

namespace EcmaScript.NET.Types.Cli
{

    /// <summary> This class reflects Java arrays into the JavaScript environment.
    /// 
    /// </summary>
    public class CliArray : CliObject
    {
        override public string ClassName
        {
            get
            {
                return "NativeCliArray";
            }

        }


        public CliArray (IScriptable scope, object array)
            : base (array)
        {
            Type cl = array.GetType ();
            if (!cl.IsArray) {
                throw new ApplicationException ("Array expected");
            }
            this.array = array;
            this.length = ((System.Array)array).Length;
            this.cls = cl.GetElementType ();
        }

        public override bool Has (string id, IScriptable start)
        {
            return id.Equals ("length") || base.Has (id, start);
        }

        public override bool Has (int index, IScriptable start)
        {
            return 0 <= index && index < length;
        }

        public override object Get (string id, IScriptable start)
        {
            if (id.Equals ("length"))
                return (int)length;
            object result = base.Get (id, start);
            if (result == UniqueTag.NotFound && !ScriptableObject.HasProperty (GetPrototype (), id)) {
                throw Context.ReportRuntimeErrorById ("msg.java.member.not.found", array.GetType ().FullName, id);
            }
            return result;
        }

        public override object Get (int index, IScriptable start)
        {
            if (0 <= index && index < length) {
                object obj = ((System.Array)array).GetValue (index);
                return Context.CurrentContext.Wrap (this, obj, cls);
            }
            return Undefined.Value;
        }

        public override object Put (string id, IScriptable start, object value)
        {
            // Ignore assignments to "length"--it's readonly.
            if (!id.Equals ("length"))
                return base.Put (id, start, value);
            return Undefined.Value;
        }

        public override object Put (int index, IScriptable start, object value)
        {
            if (0 <= index && index < length) {
                ((System.Array)array).SetValue (Context.JsToCli (value, cls), index);
                return value;
            }
            return base.Put (index, start, value);
        }

        public override object GetDefaultValue (Type hint)
        {
            if (hint == null || hint == typeof (string))
                return array.ToString ();
            if (hint == typeof (bool))
                return true;
            if (CliHelper.IsNumberType (hint))
                return BuiltinNumber.NaN;
            return this;
        }

        public override object [] GetIds ()
        {
            object [] result = new object [length];
            int i = length;
            while (--i >= 0)
                result [i] = (int)i;
            return result;
        }

        public override bool HasInstance (IScriptable value)
        {
            if (!(value is Wrapper))
                return false;
            object instance = ((Wrapper)value).Unwrap ();
            return cls.IsInstanceOfType (instance);
        }

        public override IScriptable GetPrototype ()
        {
            if (prototype == null) {
                prototype = ScriptableObject.getClassPrototype (this.ParentScope, "Array");
            }
            return prototype;
        }

        internal object array;
        internal int length;
        internal Type cls;
        internal IScriptable prototype;

    }
}