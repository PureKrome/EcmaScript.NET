//------------------------------------------------------------------------------
// <license file="SpecialRef.cs">
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

namespace EcmaScript.NET
{

    class SpecialRef : IRef
    {

        public enum Types
        {
            None = 0,
            Proto = 1,
            Parent = 2
        }

        private IScriptable target;
        private Types type;
        private string name;

        private SpecialRef (IScriptable target, Types type, string name)
        {
            this.target = target;
            this.type = type;
            this.name = name;
        }

        internal static IRef createSpecial (Context cx, object obj, string name)
        {
            IScriptable target = ScriptConvert.ToObjectOrNull (cx, obj);
            if (target == null) {
                throw ScriptRuntime.UndefReadError (obj, name);
            }

            Types type;
            if (name.Equals ("__proto__")) {
                type = Types.Proto;
            }
            else if (name.Equals ("__parent__")) {
                type = Types.Parent;
            }
            else {
                throw new ArgumentException (name);
            }

            if (!cx.HasFeature (Context.Features.ParentProtoProperties)) {
                // Clear special after checking for valid name!
                type = Types.None;
            }

            return new SpecialRef (target, type, name);
        }

        public object Get (Context cx)
        {
            switch (type) {

                case Types.None:
                    return ScriptRuntime.getObjectProp (target, name, cx);

                case Types.Proto:
                    return target.GetPrototype ();

                case Types.Parent:
                    return target.ParentScope;

                default:
                    throw Context.CodeBug ();

            }
        }

        public object Set (Context cx, object value)
        {
            switch (type) {

                case Types.None:
                    return ScriptRuntime.setObjectProp (target, name, value, cx);

                case Types.Proto:
                case Types.Parent: {
                        IScriptable obj = ScriptConvert.ToObjectOrNull (cx, value);
                        if (obj != null) {
                            // Check that obj does not contain on its prototype/scope
                            // chain to prevent cycles
                            IScriptable search = obj;
                            do {
                                if (search == target) {
                                    throw Context.ReportRuntimeErrorById ("msg.cyclic.value", name);
                                }
                                if (type == Types.Proto) {
                                    search = search.GetPrototype ();
                                }
                                else {
                                    search = search.ParentScope;
                                }
                            }
                            while (search != null);
                        }
                        if (type == Types.Proto) {
                            target.SetPrototype (obj);
                        }
                        else {
                            target.ParentScope = obj;
                        }
                        return obj;
                    }

                default:
                    throw Context.CodeBug ();

            }
        }

        public bool Has (Context cx)
        {
            if (type == Types.None) {
                return ScriptRuntime.hasObjectElem (target, name, cx);
            }
            return true;
        }

        public bool Delete (Context cx)
        {
            if (type == Types.None) {
                return ScriptRuntime.deleteObjectElem (target, name, cx);
            }
            return false;
        }
        
    }
    
}