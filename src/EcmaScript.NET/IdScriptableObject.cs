//------------------------------------------------------------------------------
// <license file="IdScriptableObject.cs">
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
using System.Runtime.InteropServices;

namespace EcmaScript.NET
{

    /// <summary>
    /// Base class for native object implementation that uses IdFunctionObject to export its methods to script via <class-name>.prototype object.
    /// Any descendant should implement at least the following methods:
    /// findInstanceIdInfo
    /// getInstanceIdName
    /// execIdCall
    /// methodArity
    /// To define non-function properties, the descendant should override
    /// getInstanceIdValue
    /// setInstanceIdValue
    /// to get/set property value and provide its default attributes.
    /// To customize initializition of constructor and protype objects, descendant
    /// may override scopeInit or fillConstructorProperties methods.
    /// </summary>
    public abstract class IdScriptableObject : ScriptableObject, IIdFunctionCall
    {

        /// <summary>
        /// Get maximum id findInstanceIdInfo can generate.
        /// </summary>
        protected virtual internal int MaxInstanceId
        {
            get
            {
                return 0;
            }
        }

        private volatile PrototypeValues prototypeValues;

        private sealed class PrototypeValues
        {

            internal int MaxId
            {
                get
                {
                    return maxId;
                }

            }

            private const int VALUE_SLOT = 0;
            private const int NAME_SLOT = 1;
            private const int SLOT_SPAN = 2;

            private IdScriptableObject obj;
            private int maxId;
            private volatile object [] valueArray;
            private volatile short [] attributeArray;
            private volatile int lastFoundId = 1;

            // The following helps to avoid creation of valueArray during runtime
            // initialization for common case of "constructor" property
            internal int constructorId;
            private IdFunctionObject constructor;
            private short constructorAttrs;

            internal PrototypeValues (IdScriptableObject obj, int maxId)
            {
                if (obj == null)
                    throw new ArgumentNullException ("obj");
                if (maxId < 1)
                    throw new ArgumentException ("maxId may not lower than 1");
                this.obj = obj;
                this.maxId = maxId;
            }

            internal void InitValue (int id, string name, object value, int attributes)
            {
                if (!(1 <= id && id <= maxId))
                    throw new ArgumentException ();
                if (name == null)
                    throw new ArgumentException ();
                if (value == UniqueTag.NotFound)
                    throw new ArgumentException ();
                ScriptableObject.CheckValidAttributes (attributes);
                if (obj.FindPrototypeId (name) != id)
                    throw new ArgumentException (name);

                if (id == constructorId) {
                    if (!(value is IdFunctionObject)) {
                        throw new ArgumentException ("consructor should be initialized with IdFunctionObject");
                    }
                    constructor = (IdFunctionObject)value;
                    constructorAttrs = (short)attributes;
                    return;
                }

                InitSlot (id, name, value, attributes);
            }

            private void InitSlot (int id, string name, object value, int attributes)
            {
                object [] array = valueArray;
                if (array == null)
                    throw new ArgumentNullException ("array");

                if (value == null) {
                    value = UniqueTag.NullValue;
                }
                int index = (id - 1) * SLOT_SPAN;
                lock (this) {
                    object value2 = array [index + VALUE_SLOT];
                    if (value2 == null) {
                        array [index + VALUE_SLOT] = value;
                        array [index + NAME_SLOT] = name;
                        attributeArray [id - 1] = (short)attributes;
                    }
                    else {
                        if (!name.Equals (array [index + NAME_SLOT]))
                            throw new Exception ();
                    }
                }
            }

            internal IdFunctionObject createPrecachedConstructor ()
            {
                if (constructorId != 0)
                    throw new Exception ();
                constructorId = obj.FindPrototypeId ("constructor");
                if (constructorId == 0) {
                    throw new Exception ("No id for constructor property");
                }
                obj.InitPrototypeId (constructorId);
                if (constructor == null) {
                    throw new Exception (obj.GetType ().FullName + ".initPrototypeId() did not " + "initialize id=" + constructorId);
                }
                constructor.InitFunction (obj.ClassName, ScriptableObject.GetTopLevelScope (obj));
                constructor.MarkAsConstructor (obj);
                return constructor;
            }

            internal int FindId (string name)
            {
                object [] array = valueArray;
                if (array == null) {
                    return obj.FindPrototypeId (name);
                }
                int id = lastFoundId;
                if ((object)name == array [(id - 1) * SLOT_SPAN + NAME_SLOT]) {
                    return id;
                }
                id = obj.FindPrototypeId (name);
                if (id != 0) {
                    int nameSlot = (id - 1) * SLOT_SPAN + NAME_SLOT;
                    // Make cache to work!
                    array [nameSlot] = name;
                    lastFoundId = id;
                }
                return id;
            }

            internal bool Has (int id)
            {
                object [] array = valueArray;
                if (array == null) {
                    // Not yet initialized, assume all exists
                    return true;
                }
                int valueSlot = (id - 1) * SLOT_SPAN + VALUE_SLOT;
                object value = array [valueSlot];
                if (value == null) {
                    // The particular entry has not been yet initialized
                    return true;
                }
                return value != UniqueTag.NotFound;
            }

            internal object Get (int id)
            {
                object value = EnsureId (id);
                if (value == UniqueTag.NullValue) {
                    value = null;
                }
                return value;
            }

            internal void Set (int id, IScriptable start, object value)
            {
                if (value == UniqueTag.NotFound)
                    throw new ArgumentException ();
                EnsureId (id);
                int attr = attributeArray [id - 1];
                if ((attr & ScriptableObject.READONLY) == 0) {
                    if (start == obj) {
                        if (value == null) {
                            value = UniqueTag.NullValue;
                        }
                        int valueSlot = (id - 1) * SLOT_SPAN + VALUE_SLOT;
                        lock (this) {
                            valueArray [valueSlot] = value;
                        }
                    }
                    else {
                        int nameSlot = (id - 1) * SLOT_SPAN + NAME_SLOT;
                        string name = (string)valueArray [nameSlot];
                        start.Put (name, start, value);
                    }
                }
            }

            internal void Delete (int id)
            {
                EnsureId (id);
                int attr = attributeArray [id - 1];
                if ((attr & ScriptableObject.PERMANENT) == 0) {
                    int valueSlot = (id - 1) * SLOT_SPAN + VALUE_SLOT;
                    lock (this) {
                        valueArray [valueSlot] = UniqueTag.NotFound;
                        attributeArray [id - 1] = (short)(ScriptableObject.EMPTY);
                    }
                }
            }

            internal int GetAttributes (int id)
            {
                EnsureId (id);
                return attributeArray [id - 1];
            }

            internal void SetAttributes (int id, int attributes)
            {
                ScriptableObject.CheckValidAttributes (attributes);
                EnsureId (id);
                lock (this) {
                    attributeArray [id - 1] = (short)attributes;
                }
            }

            internal object [] GetNames (bool getAll, object [] extraEntries)
            {
                object [] names = null;
                int count = 0;

                for (int id = 1; id <= maxId; ++id) {

                    object value = EnsureId (id);
                    if (getAll || (attributeArray [id - 1] & ScriptableObject.DONTENUM) == 0) {
                        if (value != UniqueTag.NotFound) {
                            int nameSlot = (id - 1) * SLOT_SPAN + NAME_SLOT;
                            string name = (string)valueArray [nameSlot];
                            if (names == null) {
                                names = new object [maxId];
                            }
                            names [count++] = name;
                        }
                    }
                }
                if (count == 0) {
                    return extraEntries;
                }
                else if (extraEntries == null || extraEntries.Length == 0) {
                    if (count != names.Length) {
                        object [] tmp = new object [count];
                        Array.Copy (names, 0, tmp, 0, count);
                        names = tmp;
                    }
                    return names;
                }
                else {
                    int extra = extraEntries.Length;
                    object [] tmp = new object [extra + count];
                    Array.Copy (extraEntries, 0, tmp, 0, extra);
                    Array.Copy (names, 0, tmp, extra, count);
                    return tmp;
                }
            }

            private object EnsureId (int id)
            {
                object [] array = valueArray;
                if (array == null) {
                    lock (this) {
                        array = valueArray;
                        if (array == null) {
                            array = new object [maxId * SLOT_SPAN];
                            valueArray = array;
                            attributeArray = new short [maxId];
                        }
                    }
                }
                int valueSlot = (id - 1) * SLOT_SPAN + VALUE_SLOT;
                object value = array [valueSlot];

                if (value == null) {
                    if (id == constructorId) {
                        InitSlot (constructorId, "constructor", constructor, constructorAttrs);
                        constructor = null; // no need to refer it any longer
                    }
                    else {
                        obj.InitPrototypeId (id);
                    }
                    value = array [valueSlot];
                    if (value == null) {
                        throw new Exception (obj.GetType ().FullName + ".initPrototypeId(int id) " + "did not initialize id=" + id);
                    }
                }
                return value;
            }
        }

        public IdScriptableObject ()
        {
            ;
        }

        public IdScriptableObject (IScriptable scope, IScriptable prototype)
            : base (scope, prototype)
        {
            ;
        }

        protected internal object DefaultGet (string name)
        {
            return base.Get (name, this);
        }

        protected internal void DefaultPut (string name, object value)
        {
            base.Put (name, this, value);
        }

        public override bool Has (string name, IScriptable start)
        {
            int info = FindInstanceIdInfo (name);
            if (info != 0) {
                int attr = (int)((uint)info >> 16);
                if ((attr & PERMANENT) != 0) {
                    return true;
                }
                int id = (info & 0xFFFF);
                return UniqueTag.NotFound != GetInstanceIdValue (id);
            }
            if (prototypeValues != null) {
                int id = prototypeValues.FindId (name);
                if (id != 0) {
                    return prototypeValues.Has (id);
                }
            }
            return base.Has (name, start);
        }

        public override object Get (string name, IScriptable start)
        {
            int info = FindInstanceIdInfo (name);
            if (info != 0) {
                int id = (info & 0xFFFF);
                return GetInstanceIdValue (id);
            }
            if (prototypeValues != null) {
                int id = prototypeValues.FindId (name);
                if (id != 0) {
                    return prototypeValues.Get (id);
                }
            }
            return base.Get (name, start);
        }

        public override object Put (string name, IScriptable start, object value)
        {
            int info = FindInstanceIdInfo (name);
            if (info != 0) {
                if (start == this && Sealed) {
                    throw Context.ReportRuntimeErrorById ("msg.modify.sealed", name);
                }
                int attr = (int)((uint)info >> 16);
                if ((attr & READONLY) == 0) {
                    if (start == this) {
                        int id = (info & 0xFFFF);
                        SetInstanceIdValue (id, value);
                    }
                    else {
                        return start.Put (name, start, value);
                    }
                }
                return value;
            }
            if (prototypeValues != null) {
                int id = prototypeValues.FindId (name);
                if (id != 0) {
                    if (start == this && Sealed) {
                        throw Context.ReportRuntimeErrorById ("msg.modify.sealed", name);
                    }
                    prototypeValues.Set (id, start, value);
                    return value;
                }
            }
            return base.Put (name, start, value);
        }

        public override void Delete (string name)
        {
            int info = FindInstanceIdInfo (name);
            if (info != 0) {
                // Let the super class to throw exceptions for sealed objects
                if (!Sealed) {
                    int attr = (int)((uint)info >> 16);
                    if ((attr & PERMANENT) == 0) {
                        int id = (info & 0xFFFF);
                        SetInstanceIdValue (id, UniqueTag.NotFound);
                    }
                    return;
                }
            }
            if (prototypeValues != null) {
                int id = prototypeValues.FindId (name);
                if (id != 0) {
                    if (!Sealed) {
                        prototypeValues.Delete (id);
                    }
                    return;
                }
            }
            base.Delete (name);
        }

        public override int GetAttributes (string name)
        {
            int info = FindInstanceIdInfo (name);
            if (info != 0) {
                int attr = (int)((uint)info >> 16);
                return attr;
            }
            if (prototypeValues != null) {
                int id = prototypeValues.FindId (name);
                if (id != 0) {
                    return prototypeValues.GetAttributes (id);
                }
            }
            return base.GetAttributes (name);
        }

        public override void SetAttributes (string name, int attributes)
        {
            ScriptableObject.CheckValidAttributes (attributes);
            int info = FindInstanceIdInfo (name);
            if (info != 0) {
                int currentAttributes = (int)((uint)info >> 16);
                if (attributes != currentAttributes) {
                    throw new Exception ("Change of attributes for this id is not supported");
                }
                return;
            }
            if (prototypeValues != null) {
                int id = prototypeValues.FindId (name);
                if (id != 0) {
                    prototypeValues.SetAttributes (id, attributes);
                    return;
                }
            }
            base.SetAttributes (name, attributes);
        }

        internal override object [] GetIds (bool getAll)
        {
            object [] result = base.GetIds (getAll);

            if (prototypeValues != null) {
                result = prototypeValues.GetNames (getAll, result);
            }

            int maxInstanceId = MaxInstanceId;
            if (maxInstanceId != 0) {
                object [] ids = null;
                int count = 0;

                for (int id = maxInstanceId; id != 0; --id) {
                    string name = GetInstanceIdName (id);
                    int info = FindInstanceIdInfo (name);
                    if (info != 0) {
                        int attr = (int)((uint)info >> 16);
                        if ((attr & PERMANENT) == 0) {
                            if (UniqueTag.NotFound == GetInstanceIdValue (id)) {
                                continue;
                            }
                        }
                        if (getAll || (attr & DONTENUM) == 0) {
                            if (count == 0) {
                                // Need extra room for no more then [1..id] names
                                ids = new object [id];
                            }
                            ids [count++] = name;
                        }
                    }
                }
                if (count != 0) {
                    if (result.Length == 0 && ids.Length == count) {
                        result = ids;
                    }
                    else {
                        object [] tmp = new object [result.Length + count];
                        Array.Copy (result, 0, tmp, 0, result.Length);
                        Array.Copy (ids, 0, tmp, result.Length, count);
                        result = tmp;
                    }
                }
            }
            return result;
        }

        protected internal static int InstanceIdInfo (int attributes, int id)
        {
            return (attributes << 16) | id;
        }

        /// <summary> 
        /// Map name to id of instance property.
        /// Should return 0 if not found or the result of
        /// {@link #instanceIdInfo(int, int)}.
        /// </summary>
        protected internal virtual int FindInstanceIdInfo (string name)
        {
            return 0;
        }

        /// <summary>
        /// Map id back to property name it defines.
        /// </summary>
        protected internal virtual string GetInstanceIdName (int id)
        {
            throw new ArgumentException (Convert.ToString (id));
        }

        /// <summary>
        /// Get id value.
        /// * If id value is constant, descendant can call cacheIdValue to store
        /// * value in the permanent cache.
        /// * Default implementation creates IdFunctionObject instance for given id
        /// * and cache its value
        /// </summary>
        protected internal virtual object GetInstanceIdValue (int id)
        {
            throw new Exception (Convert.ToString (id));
        }

        /// <summary>
        /// Set or delete id value. If value == NOT_FOUND , the implementation
        /// should make sure that the following getInstanceIdValue return NOT_FOUND.
        /// </summary>
        protected internal virtual void SetInstanceIdValue (int id, object value)
        {
            throw new Exception (Convert.ToString (id));
        }

        /// <summary>'thisObj' will be null if invoked as constructor, in which case
        /// * instance of Scriptable should be returned. 
        /// </summary>
        public virtual object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            throw f.Unknown ();
        }

        public IdFunctionObject ExportAsJSClass (int maxPrototypeId, IScriptable scope, bool zealed)        
        {
            return ExportAsJSClass (maxPrototypeId, scope, zealed, ScriptableObject.DONTENUM);
        }

        public IdFunctionObject ExportAsJSClass (int maxPrototypeId, IScriptable scope, bool zealed, int attributes)
        {
            // Set scope and prototype unless this is top level scope itself
            if (scope != this && scope != null) {
                ParentScope = scope;
                SetPrototype (GetObjectPrototype (scope));
            }

            ActivatePrototypeMap (maxPrototypeId);
            IdFunctionObject ctor = prototypeValues.createPrecachedConstructor ();
            if (zealed) {
                SealObject ();
            }
            FillConstructorProperties (ctor);
            if (zealed) {
                ctor.SealObject ();
            }
            ctor.ExportAsScopeProperty (attributes);
            return ctor;
        }

        public void ActivatePrototypeMap (int maxPrototypeId)
        {
            PrototypeValues values = new PrototypeValues (this, maxPrototypeId);
            lock (this) {
                if (prototypeValues != null)
                    throw new Exception ();
                prototypeValues = values;
            }
        }

        public void InitPrototypeMethod (object tag, int id, string name, int arity)
        {
            IScriptable scope = ScriptableObject.GetTopLevelScope (this);
            IdFunctionObject f = NewIdFunction (tag, id, name, arity, scope);
            prototypeValues.InitValue (id, name, f, DONTENUM);
        }

        public void InitPrototypeConstructor (IdFunctionObject f)
        {
            int id = prototypeValues.constructorId;
            if (id == 0)
                throw new Exception ();
            if (f.MethodId != id)
                throw new ArgumentException ();
            if (Sealed) {
                f.SealObject ();
            }
            prototypeValues.InitValue (id, "constructor", f, DONTENUM);
        }

        public void InitPrototypeValue (int id, string name, object value, int attributes)
        {
            prototypeValues.InitValue (id, name, value, attributes);
        }

        protected internal virtual void InitPrototypeId (int id)
        {
            throw new Exception (Convert.ToString (id));
        }

        protected internal virtual int FindPrototypeId (string name)
        {
            throw new Exception (name);
        }

        protected internal virtual void FillConstructorProperties (IdFunctionObject ctor)
        {
        }

        protected internal virtual void AddIdFunctionProperty (IScriptable obj, object tag, int id, string name, int arity)
        {
            IScriptable scope = ScriptableObject.GetTopLevelScope (obj);
            IdFunctionObject f = NewIdFunction (tag, id, name, arity, scope);
            f.AddAsProperty (obj);
        }

        /// <summary> 
        /// Utility method to construct type error to indicate incompatible call
        /// when converting script thisObj to a particular type is not possible.
        /// Possible usage would be to have a private function like realThis:
        /// <pre>
        /// private static NativeSomething realThis(Scriptable thisObj,
        /// IdFunctionObject f)
        /// {
        /// if (!(thisObj instanceof NativeSomething))
        /// throw incompatibleCallError(f);
        /// return (NativeSomething)thisObj;
        /// }
        /// </pre>
        /// Note that although such function can be implemented universally via
        /// java.lang.Class.isInstance(), it would be much more slower.
        /// </summary>
        /// <param name="readOnly">specify if the function f does not change state of
        /// object.
        /// </param>
        /// <returns> Scriptable object suitable for a check by the instanceof
        /// operator.
        /// </returns>
        /// <throws>  RuntimeException if no more instanceof target can be found </throws>
        protected internal static EcmaScriptError IncompatibleCallError (IdFunctionObject f)
        {
            throw ScriptRuntime.TypeErrorById ("msg.incompat.call", f.FunctionName);
        }

        private IdFunctionObject NewIdFunction (object tag, int id, string name, int arity, IScriptable scope)
        {
            IdFunctionObject f = new IdFunctionObject (this, tag, id, name, arity, scope);
            if (Sealed) {
                f.SealObject ();
            }
            return f;
        }


    }
}