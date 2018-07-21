//------------------------------------------------------------------------------
// <license file="IdEnumeration.cs">
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

    /// <summary>
    /// This is the enumeration needed by the for..in statement.
    /// 
    /// See ECMA 12.6.3.
    /// 
    /// IdEnumeration maintains a ObjToIntMap to make sure a given
    /// id is enumerated only once across multiple objects in a
    /// prototype chain.
    /// 
    /// ECMA delete doesn't hide properties in the prototype,
    /// but js/ref does. This means that the js/ref for..in can
    /// avoid maintaining a hash table and instead perform lookups
    /// to see if a given property has already been enumerated.
    /// 
    /// </summary>
    public class IdEnumeration
    {

        protected IdEnumeration ()
        {
            ;
        }

        public IdEnumeration (object value, Context cx, bool enumValues)
        {
            obj = ScriptConvert.ToObjectOrNull (cx, value);
            if (obj != null) {
                // null or undefined do not cause errors but rather lead to empty
                // "for in" loop
                this.enumValues = enumValues;

                // enumInit should read all initial ids before returning
                // or "for (a.i in a)" would wrongly enumerate i in a as well
                ChangeObject ();
            }
        }

        private IScriptable obj;
        private object [] ids;
        private int index;
        private ObjToIntMap used;
        private string currentId;
        private bool enumValues;

        public virtual bool MoveNext ()
        {
            // OPT this could be more efficient
            bool result;
            for (; ; ) {
                if (obj == null) {
                    result = false;
                    break;
                }
                if (index == ids.Length) {
                    obj = obj.GetPrototype ();
                    this.ChangeObject ();
                    continue;
                }
                object id = ids [index++];
                if (used != null && used.has (id)) {
                    continue;
                }
                if (id is string) {
                    string strId = (string)id;
                    if (!obj.Has (strId, obj))
                        continue; // must have been deleted
                    currentId = strId;
                }
                else {
                    int intId = Convert.ToInt32 (id);
                    if (!obj.Has (intId, obj))
                        continue; // must have been deleted
                    currentId = Convert.ToString (intId);
                }
                result = true;
                break;
            }
            return result;
        }

        public virtual object Current (Context cx)
        {
            if (!enumValues)
                return currentId;

            object result;

            string s = ScriptRuntime.ToStringIdOrIndex (cx, currentId);
            if (s == null) {
                int index = ScriptRuntime.lastIndexResult (cx);
                result = obj.Get (index, obj);
            }
            else {
                result = obj.Get (s, obj);
            }

            return result;
        }

        private void ChangeObject ()
        {
            object [] ids = null;
            while (obj != null) {
                ids = obj.GetIds ();
                if (ids.Length != 0) {
                    break;
                }
                obj = obj.GetPrototype ();
            }
            if (obj != null && this.ids != null) {
                object [] previous = this.ids;
                int L = previous.Length;
                if (used == null) {
                    used = new ObjToIntMap (L);
                }
                for (int i = 0; i != L; ++i) {
                    used.intern (previous [i]);
                }
            }
            this.ids = ids;
            this.index = 0;
        }

    }
}
