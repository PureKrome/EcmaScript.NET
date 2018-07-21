//------------------------------------------------------------------------------
// <license file="NativeCliPackage.cs">
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
using System.Reflection;

using EcmaScript.NET.Attributes;

namespace EcmaScript.NET.Types.Cli
{

    [EcmaScriptClass ("Cli")]
    public class CliPackage : CliObject
    {

        public static void Init (IScriptable scope)
        {
            CliPackage obj = new CliPackage ();

            ScriptableObject.DefineProperty (scope,
                "cli", obj, ScriptableObject.DONTENUM);

            obj.ParentScope = scope;
        }

        [EcmaScriptFunction ("load")]
        public void Load (string assembly)
        {
            try {
                ImportAssembly (
                    this.ParentScope, Assembly.LoadWithPartialName (assembly));
            }
            catch (FileNotFoundException e) {
                throw ScriptRuntime.ConstructError ("EvalError", "Failed to load assembly: " + e.Message);
            }
        }

        [EcmaScriptFunction ("using")]
        public void Using (string ns)
        {
            ImportTypes (this.ParentScope, ns);
        }

        public static void ImportAssembly (IScriptable scope, Assembly ass)
        {
            ImportAssembly (scope, ass, null);
        }

        public static void ImportAssembly (IScriptable scope, Assembly ass, string startsWith)
        {
            foreach (Type type in ass.GetTypes ()) {
                if (startsWith == null || type.FullName.StartsWith (startsWith))
                    ImportType (scope, type);
            }
        }

        public static void ImportTypes (IScriptable scope, string startsWith)
        {
            foreach (Assembly ass in Context.CurrentContext.AppDomain.GetAssemblies ()) {
                ImportAssembly (scope, ass, startsWith);
            }
        }

        public static void ImportType (IScriptable scope, Type type)
        {
            if (!type.IsPublic)
                return;

            if (ScriptRuntime.IsNativeRuntimeType (type))
                return;

            // Cannot define 'Object'
            if (type.Name == "Object")
                return;


            string [] ns = type.FullName.Split ('.');

            IScriptable parent = scope;
            for (int i = 0; i < ns.Length - 1; i++) {
                IScriptable obj = (parent.Get (ns [i], parent) as IScriptable);
                if (obj == null) {
                    obj = new BuiltinObject ();
                    parent.Put (ns [i], parent, obj);
                }
                parent = obj;
            }

            object thisObj = null;
            if (type.IsEnum) {
                thisObj = new CliEnum ((Enum)Activator.CreateInstance (type));
            }
            else {
                thisObj = CliType.GetNativeCliType (type);

            }
            // Define as toplevel object
            scope.Put (ns [ns.Length - 1], scope, thisObj);

            // Define as full qualified name
            parent.Put (ns [ns.Length - 1], parent, thisObj);
        }


    }

}
