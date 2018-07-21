//------------------------------------------------------------------------------
// <license file="ScriptableObject.cs">
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

// API class
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections;

using EcmaScript.NET.Debugging;
using EcmaScript.NET.Attributes;
using EcmaScript.NET.Collections;

namespace EcmaScript.NET
{

    /// <summary> This is the default implementation of the Scriptable interface. This
    /// class provides convenient default behavior that makes it easier to
    /// define host objects.
    /// <p>
    /// Various properties and methods of JavaScript objects can be conveniently
    /// defined using methods of ScriptableObject.
    /// <p>
    /// Classes extending ScriptableObject must define the getClassName method.
    /// 
    /// </summary>    
    public abstract class ScriptableObject : IScriptable, DebuggableObject
    {
        /// <summary> Return the name of the class.
        /// 
        /// This is typically the same name as the constructor.
        /// Classes extending ScriptableObject must implement this abstract
        /// method.
        /// </summary>
        public abstract string ClassName { get;}


        /// <summary> Returns the parent (enclosing) scope of the object.</summary>
        /// <summary> Sets the parent (enclosing) scope of the object.</summary>
        public IScriptable ParentScope
        {
            get
            {
                return parentScopeObject;
            }

            set
            {
                parentScopeObject = value;
            }

        }
        /// <summary> Returns an array of ids for the properties of the object.
        /// 
        /// <p>All properties, even those with attribute DONTENUM, are listed. <p>
        /// 
        /// </summary>
        /// <returns> an array of java.lang.Objects with an entry for every
        /// listed property. Properties accessed via an integer index will
        /// have a corresponding
        /// Integer entry in the returned array. Properties accessed by
        /// a String will have a String entry in the returned array.
        /// </returns>
        virtual public object [] AllIds
        {
            get
            {
                return GetIds (true);
            }

        }
        /// <summary> Return true if this object is sealed.
        /// 
        /// It is an error to attempt to add or remove properties to
        /// a sealed object.
        /// 
        /// </summary>
        /// <returns> true if sealed, false otherwise.
        /// </returns>
        public bool Sealed
        {
            get
            {
                return count < 0;
            }

        }

        /// <summary> The empty property attribute.
        /// 
        /// Used by getAttributes() and setAttributes().
        /// 
        /// </summary>        
        public const int EMPTY = 0x00;

        /// <summary> Property attribute indicating assignment to this property is ignored.
        /// 
        /// </summary>        
        public const int READONLY = 0x01;

        /// <summary> Property attribute indicating property is not enumerated.
        /// 
        /// Only enumerated properties will be returned by getIds().
        /// 
        /// </summary>        
        public const int DONTENUM = 0x02;

        /// <summary> Property attribute indicating property cannot be deleted.
        /// 
        /// </summary>        
        public const int PERMANENT = 0x04;

        internal static void CheckValidAttributes (int attributes)
        {
            const int mask = READONLY | DONTENUM | PERMANENT;
            if ((attributes & ~mask) != 0) {
                throw new ArgumentException (Convert.ToString (attributes));
            }
        }

        public ScriptableObject ()
        {
        }

        public ScriptableObject (IScriptable scope, IScriptable prototype)
        {
            if (scope == null)
                throw new ArgumentException ();

            parentScopeObject = scope;
            prototypeObject = prototype;
        }

        /// <summary> Returns true if the named property is defined.
        /// 
        /// </summary>
        /// <param name="name">the name of the property
        /// </param>
        /// <param name="start">the object in which the lookup began
        /// </param>
        /// <returns> true if and only if the property was found in the object
        /// </returns>
        public virtual bool Has (string name, IScriptable start)
        {
            return null != GetNamedSlot (name);
        }

        /// <summary> Returns true if the property index is defined.
        /// 
        /// </summary>
        /// <param name="index">the numeric index for the property
        /// </param>
        /// <param name="start">the object in which the lookup began
        /// </param>
        /// <returns> true if and only if the property was found in the object
        /// </returns>
        public virtual bool Has (int index, IScriptable start)
        {
            return null != GetSlot (null, index);
        }

        /// <summary> Returns the value of the named property or NOT_FOUND.
        /// 
        /// If the property was created using defineProperty, the
        /// appropriate getter method is called.
        /// 
        /// </summary>
        /// <param name="name">the name of the property
        /// </param>
        /// <param name="start">the object in which the lookup began
        /// </param>
        /// <returns> the value of the property (may be null), or NOT_FOUND
        /// </returns>
        public virtual object Get (string name, IScriptable start)
        {
            Slot slot = GetNamedSlot (name);
            if (slot == null) {
                return UniqueTag.NotFound;
            }
            return slot.GetValue (null, start, start);
        }

        /// <summary> Returns the value of the indexed property or NOT_FOUND.
        /// 
        /// </summary>
        /// <param name="index">the numeric index for the property
        /// </param>
        /// <param name="start">the object in which the lookup began
        /// </param>
        /// <returns> the value of the property (may be null), or NOT_FOUND
        /// </returns>
        public virtual object Get (int index, IScriptable start)
        {
            Slot slot = GetSlot (null, index);
            if (slot == null) {
                return UniqueTag.NotFound;
            }
            return slot.GetValue (null, start, start);
        }

        /// <summary> Sets the value of the named property, creating it if need be.
        /// 
        /// If the property was created using defineProperty, the
        /// appropriate setter method is called. <p>
        /// 
        /// If the property's attributes include READONLY, no action is
        /// taken.
        /// This method will actually set the property in the start
        /// object.
        /// 
        /// </summary>
        /// <param name="name">the name of the property
        /// </param>
        /// <param name="start">the object whose property is being set
        /// </param>
        /// <param name="value">value to set the property to
        /// </param>
        public virtual object Put (string name, IScriptable start, object value)
        {
            Slot slot = lastAccess; // Get local copy
            if ((object)name != (object)slot.stringKey || slot.wasDeleted != 0) {
                int hash = name.GetHashCode ();
                slot = GetSlot (name, hash);
                if (slot == null) {
                    if (start != this) {
                        start.Put (name, start, value);
                        return value;
                    }
                    slot = AddSlot (name, hash, null);
                }
                // Note: cache is not updated in put
            }
            if (start == this && Sealed) {
                throw Context.ReportRuntimeErrorById ("msg.modify.sealed", name);
            }
            if ((slot.attributes & ScriptableObject.READONLY) != 0) {
                // FINDME                
                Context cx = Context.CurrentContext;
                if (cx.Version == Context.Versions.JS1_2) {
                    throw Context.ReportRuntimeErrorById ("msg.read-only", name);
                } else {
                    if (cx.HasFeature (Context.Features.Strict)) {
                        Context.ReportWarningById ("msg.read-only", name);                        
                    }
                }
                return value;
            }
            if (this == start) {
                return slot.SetValue (null, start, start, value);
            }
            else {
                if (slot.setter != null) {
                    Slot newSlot = (Slot)slot.Clone ();
                    ((ScriptableObject)start).AddSlotImpl (newSlot.stringKey, newSlot.intKey, newSlot);
                    return newSlot.SetValue (null, start, start, value);
                }
                else {
                    return start.Put (name, start, value);
                }
            }
            return value;
        }

        /// <summary> Sets the value of the indexed property, creating it if need be.
        /// 
        /// </summary>
        /// <param name="index">the numeric index for the property
        /// </param>
        /// <param name="start">the object whose property is being set
        /// </param>
        /// <param name="value">value to set the property to
        /// </param>
        public virtual object Put (int index, IScriptable start, object value)
        {
            Slot slot = GetSlot (null, index);
            if (slot == null) {
                if (start != this) {
                    return start.Put (index, start, value);
                }
                slot = AddSlot (null, index, null);
            }
            if (start == this && Sealed) {
                throw Context.ReportRuntimeErrorById ("msg.modify.sealed", Convert.ToString (index));
            }
            if ((slot.attributes & ScriptableObject.READONLY) != 0) {
                return slot.GetValue (null, start, start); // TODO: ???
            }
            if (this == start) {
                return slot.SetValue (null, start, start, value);
            }
            else {
                return start.Put (index, start, value);
            }
        }

        /// <summary> Removes a named property from the object.
        /// 
        /// If the property is not found, or it has the PERMANENT attribute,
        /// no action is taken.
        /// 
        /// </summary>
        /// <param name="name">the name of the property
        /// </param>
        public virtual void Delete (string name)
        {
            RemoveSlot (name, name.GetHashCode ());
        }

        /// <summary> Removes the indexed property from the object.
        /// 
        /// If the property is not found, or it has the PERMANENT attribute,
        /// no action is taken.
        /// 
        /// </summary>
        /// <param name="index">the numeric index for the property
        /// </param>
        public virtual void Delete (int index)
        {
            RemoveSlot (null, index);
        }





        /// <summary> Get the attributes of a named property.
        /// 
        /// The property is specified by <code>name</code>
        /// as defined for <code>has</code>.<p>
        /// 
        /// </summary>
        /// <param name="name">the identifier for the property
        /// </param>
        /// <returns> the bitset of attributes
        /// </returns>
        /// <exception cref=""> EvaluatorException if the named property is not found
        /// </exception>        
        public virtual int GetAttributes (string name)
        {
            Slot slot = GetNamedSlot (name);
            if (slot == null) {
                throw Context.ReportRuntimeErrorById ("msg.prop.not.found", name);
            }
            return slot.attributes;
        }

        /// <summary> Get the attributes of an indexed property.
        /// 
        /// </summary>
        /// <param name="index">the numeric index for the property
        /// </param>
        /// <exception cref=""> EvaluatorException if the named property is not found
        /// is not found
        /// </exception>
        /// <returns> the bitset of attributes
        /// </returns>        
        public virtual int GetAttributes (int index)
        {
            Slot slot = GetSlot (null, index);
            if (slot == null) {
                throw Context.ReportRuntimeErrorById ("msg.prop.not.found", Convert.ToString (index));
            }
            return slot.attributes;
        }

        /// <summary> Set the attributes of a named property.
        /// 
        /// The property is specified by <code>name</code>
        /// as defined for <code>has</code>.<p>
        /// 
        /// The possible attributes are READONLY, DONTENUM,
        /// and PERMANENT. Combinations of attributes
        /// are expressed by the bitwise OR of attributes.
        /// EMPTY is the state of no attributes set. Any unused
        /// bits are reserved for future use.
        /// 
        /// </summary>
        /// <param name="name">the name of the property
        /// </param>
        /// <param name="attributes">the bitset of attributes
        /// </param>
        /// <exception cref=""> EvaluatorException if the named property is not found
        /// </exception>        
        public virtual void SetAttributes (string name, int attributes)
        {
            CheckValidAttributes (attributes);
            Slot slot = GetNamedSlot (name);
            if (slot == null) {
                throw Context.ReportRuntimeErrorById ("msg.prop.not.found", name);
            }
            slot.attributes = (short)attributes;
        }

        /// <summary> Set the attributes of an indexed property.
        /// 
        /// </summary>
        /// <param name="index">the numeric index for the property
        /// </param>
        /// <param name="attributes">the bitset of attributes
        /// </param>
        /// <exception cref=""> EvaluatorException if the named property is not found
        /// </exception>        
        public virtual void SetAttributes (int index, int attributes)
        {
            CheckValidAttributes (attributes);
            Slot slot = GetSlot (null, index);
            if (slot == null) {
                throw Context.ReportRuntimeErrorById ("msg.prop.not.found", Convert.ToString (index));
            }
            slot.attributes = (short)attributes;
        }

        /// <summary> Returns the prototype of the object.</summary>
        public virtual IScriptable GetPrototype ()
        {
            return prototypeObject;
        }

        /// <summary> Sets the prototype of the object.</summary>
        public virtual void SetPrototype (IScriptable m)
        {
            prototypeObject = m;
        }

        /// <summary> Returns an array of ids for the properties of the object.
        /// 
        /// <p>Any properties with the attribute DONTENUM are not listed. <p>
        /// 
        /// </summary>
        /// <returns> an array of java.lang.Objects with an entry for every
        /// listed property. Properties accessed via an integer index will
        /// have a corresponding
        /// Integer entry in the returned array. Properties accessed by
        /// a String will have a String entry in the returned array.
        /// </returns>
        public virtual object [] GetIds ()
        {
            return GetIds (false);
        }

        /// <summary> Implements the [[DefaultValue]] internal method.
        /// 
        /// <p>Note that the toPrimitive conversion is a no-op for
        /// every type other than Object, for which [[DefaultValue]]
        /// is called. See ECMA 9.1.<p>
        /// 
        /// A <code>hint</code> of null means "no hint".
        /// 
        /// </summary>
        /// <param name="typeHint">the type hint
        /// </param>
        /// <returns> the default value for the object
        /// 
        /// See ECMA 8.6.2.6.
        /// </returns>
        public virtual object GetDefaultValue (Type typeHint)
        {
            Context cx = null;
            for (int i = 0; i < 2; i++) {
                bool tryToString;
                if (typeHint == typeof (string)) {
                    tryToString = (i == 0);
                }
                else {
                    tryToString = (i == 1);
                }

                string methodName;
                object [] args;
                if (tryToString) {
                    methodName = "toString";
                    args = ScriptRuntime.EmptyArgs;
                }
                else {
                    methodName = "valueOf";
                    args = new object [1];
                    string hint;
                    if (typeHint == null) {
                        hint = "undefined";
                    }
                    else if (typeHint == typeof (string)) {
                        hint = "string";
                    }
                    else if (typeHint == typeof (IScriptable)) {
                        hint = "object";
                    }
                    else if (typeHint == typeof (IFunction)) {
                        hint = "function";
                    }
                    else if (typeHint == typeof (bool) || typeHint == typeof (bool)) {
                        hint = "boolean";
                    }
                    else if (CliHelper.IsNumberType (typeHint) || typeHint == typeof (byte) || typeHint == typeof (sbyte)) {
                        hint = "number";
                    }
                    else {
                        throw Context.ReportRuntimeErrorById ("msg.invalid.type", typeHint.ToString ());
                    }
                    args [0] = hint;
                }
                object v = GetProperty (this, methodName);
                if (!(v is IFunction))
                    continue;
                IFunction fun = (IFunction)v;
                if (cx == null)
                    cx = Context.CurrentContext;
                v = fun.Call (cx, fun.ParentScope, this, args);
                if (v != null) {
                    if (!(v is IScriptable)) {
                        return v;
                    }
                    if (typeHint == typeof (IScriptable) || typeHint == typeof (IFunction)) {
                        return v;
                    }
                    if (tryToString && v is Wrapper) {
                        // Let a wrapped java.lang.String pass for a primitive
                        // string.
                        object u = ((Wrapper)v).Unwrap ();
                        if (u is string)
                            return u;
                    }
                }
            }
            // fall through to error            
            string arg = (typeHint == null) ? "undefined" : typeHint.FullName;
            throw ScriptRuntime.TypeErrorById ("msg.default.value", arg);
        }

        /// <summary> Implements the instanceof operator.
        /// 
        /// <p>This operator has been proposed to ECMA.
        /// 
        /// </summary>
        /// <param name="instance">The value that appeared on the LHS of the instanceof
        /// operator
        /// </param>
        /// <returns> true if "this" appears in value's prototype chain
        /// 
        /// </returns>
        public virtual bool HasInstance (IScriptable instance)
        {

            // According to specs -- section 11.8.6 of ECMA-262 -- instanceof operator
            // on objects NOT implementing [[HasInstance]] internal method should 
            // throw a TypeError exception. Hence, in the following script the
            // catch block must get executed, (since Math object does not implement
            // [[HasInstance]] method).
            throw ScriptRuntime.TypeError ("msg.bad.instanceof.rhs");


            // Default for JS objects (other than Function) is to do prototype
            // chasing.  This will be overridden in NativeFunction and non-JS
            // objects.
            //return ScriptRuntime.jsDelegatesTo(instance, this);
        }

        /// <summary> Custom <tt>==</tt> operator.
        /// Must return {@link Scriptable#NOT_FOUND} if this object does not
        /// have custom equality operator for the given value,
        /// <tt>Boolean.TRUE</tt> if this object is equivalent to <tt>value</tt>,
        /// <tt>Boolean.FALSE</tt> if this object is not equivalent to
        /// <tt>value</tt>.
        /// <p>
        /// The default implementation returns Boolean.TRUE
        /// if <tt>this == value</tt> or {@link Scriptable#NOT_FOUND} otherwise.
        /// It indicates that by default custom equality is available only if
        /// <tt>value</tt> is <tt>this</tt> in which case true is returned.
        /// </summary>
        protected internal virtual object EquivalentValues (object value)
        {
            return (this == value) ? (object)true : UniqueTag.NotFound;
        }

        /// <summary> Define a JavaScript property.
        /// 
        /// Creates the property with an initial value and sets its attributes.
        /// 
        /// </summary>
        /// <param name="propertyName">the name of the property to define.
        /// </param>
        /// <param name="value">the initial value of the property
        /// </param>
        /// <param name="attributes">the attributes of the JavaScript property
        /// </param>        
        public virtual void DefineProperty (string propertyName, object value, int attributes)
        {
            Put (propertyName, this, value);
            SetAttributes (propertyName, attributes);
        }

        /// <summary> Utility method to add properties to arbitrary Scriptable object.
        /// If destination is instance of ScriptableObject, calls
        /// defineProperty there, otherwise calls put in destination
        /// ignoring attributes
        /// </summary>
        public static void DefineProperty (IScriptable destination, string propertyName, object value, int attributes)
        {
            if (!(destination is ScriptableObject)) {
                destination.Put (propertyName, destination, value);
                return;
            }
            ScriptableObject so = (ScriptableObject)destination;
            so.DefineProperty (propertyName, value, attributes);
        }



        /// <summary> Get the Object.prototype property.
        /// See ECMA 15.2.4.
        /// </summary>
        public static IScriptable GetObjectPrototype (IScriptable scope)
        {
            return getClassPrototype (scope, "Object");
        }

        /// <summary> Get the Function.prototype property.
        /// See ECMA 15.3.4.
        /// </summary>
        public static IScriptable GetFunctionPrototype (IScriptable scope)
        {
            return getClassPrototype (scope, "Function");
        }

        /// <summary> Get the prototype for the named class.
        /// 
        /// For example, <code>getClassPrototype(s, "Date")</code> will first
        /// walk up the parent chain to find the outermost scope, then will
        /// search that scope for the Date constructor, and then will
        /// return Date.prototype. If any of the lookups fail, or
        /// the prototype is not a JavaScript object, then null will
        /// be returned.
        /// 
        /// </summary>
        /// <param name="scope">an object in the scope chain
        /// </param>
        /// <param name="className">the name of the constructor
        /// </param>
        /// <returns> the prototype for the named class, or null if it
        /// cannot be found.
        /// </returns>
        public static IScriptable getClassPrototype (IScriptable scope, string className)
        {
            scope = GetTopLevelScope (scope);
            object ctor = GetProperty (scope, className);
            object proto;
            if (ctor is BaseFunction) {
                proto = ((BaseFunction)ctor).PrototypeProperty;
            }
            else if (ctor is IScriptable) {
                IScriptable ctorObj = (IScriptable)ctor;
                proto = ctorObj.Get ("prototype", ctorObj);
            }
            else {
                return null;
            }
            if (proto is IScriptable) {
                return (IScriptable)proto;
            }
            return null;
        }

        /// <summary> Get the global scope.
        /// 
        /// <p>Walks the parent scope chain to find an object with a null
        /// parent scope (the global object).
        /// 
        /// </summary>
        /// <param name="obj">a JavaScript object
        /// </param>
        /// <returns> the corresponding global scope
        /// </returns>
        public static IScriptable GetTopLevelScope (IScriptable obj)
        {
            for (; ; ) {
                IScriptable parent = obj.ParentScope;
                if (parent == null) {
                    return obj;
                }
                obj = parent;
            }
        }

        /// <summary> Seal this object.
        /// 
        /// A sealed object may not have properties added or removed. Once
        /// an object is sealed it may not be unsealed.
        /// 
        /// </summary>
        public virtual void SealObject ()
        {
            lock (this) {
                if (count >= 0) {
                    count = -1 - count;
                }
            }
        }

        /// <summary> Gets a named property from an object or any object in its prototype chain.
        /// <p>
        /// Searches the prototype chain for a property named <code>name</code>.
        /// <p>
        /// </summary>
        /// <param name="obj">a JavaScript object
        /// </param>
        /// <param name="name">a property name
        /// </param>
        /// <returns> the value of a property with name <code>name</code> found in
        /// <code>obj</code> or any object in its prototype chain, or
        /// <code>Scriptable.NOT_FOUND</code> if not found
        /// </returns>
        public static object GetProperty (IScriptable obj, string name)
        {
            IScriptable start = obj;
            object result;
            do {
                result = obj.Get (name, start);
                if (result != UniqueTag.NotFound)
                    break;
                obj = obj.GetPrototype ();
            }
            while (obj != null);
            return result;
        }

        /// <summary> Gets an indexed property from an object or any object in its prototype chain.
        /// <p>
        /// Searches the prototype chain for a property with integral index
        /// <code>index</code>. Note that if you wish to look for properties with numerical
        /// but non-integral indicies, you should use getProperty(Scriptable,String) with
        /// the string value of the index.
        /// <p>
        /// </summary>
        /// <param name="obj">a JavaScript object
        /// </param>
        /// <param name="index">an integral index
        /// </param>
        /// <returns> the value of a property with index <code>index</code> found in
        /// <code>obj</code> or any object in its prototype chain, or
        /// <code>Scriptable.NOT_FOUND</code> if not found
        /// </returns>
        public static object GetProperty (IScriptable obj, int index)
        {
            IScriptable start = obj;
            object result;
            do {
                result = obj.Get (index, start);
                if (result != UniqueTag.NotFound)
                    break;
                obj = obj.GetPrototype ();
            }
            while (obj != null);
            return result;
        }

        /// <summary> Returns whether a named property is defined in an object or any object
        /// in its prototype chain.
        /// <p>
        /// Searches the prototype chain for a property named <code>name</code>.
        /// <p>
        /// </summary>
        /// <param name="obj">a JavaScript object
        /// </param>
        /// <param name="name">a property name
        /// </param>
        /// <returns> the true if property was found
        /// </returns>
        public static bool HasProperty (IScriptable obj, string name)
        {
            return null != GetBase (obj, name);
        }

        /// <summary> Returns whether an indexed property is defined in an object or any object
        /// in its prototype chain.
        /// <p>
        /// Searches the prototype chain for a property with index <code>index</code>.
        /// <p>
        /// </summary>
        /// <param name="obj">a JavaScript object
        /// </param>
        /// <param name="index">a property index
        /// </param>
        /// <returns> the true if property was found
        /// </returns>
        public static bool HasProperty (IScriptable obj, int index)
        {
            return null != GetBase (obj, index);
        }

        /// <summary> Puts a named property in an object or in an object in its prototype chain.
        /// <p>
        /// Seaches for the named property in the prototype chain. If it is found,
        /// the value of the property is changed. If it is not found, a new
        /// property is added in <code>obj</code>.
        /// </summary>
        /// <param name="obj">a JavaScript object
        /// </param>
        /// <param name="name">a property name
        /// </param>
        /// <param name="value">any JavaScript value accepted by Scriptable.put
        /// </param>
        public static object PutProperty (IScriptable obj, string name, object value)
        {
            IScriptable toBase = GetBase (obj, name);
            if (toBase == null)
                toBase = obj;
            return toBase.Put (name, obj, value);
        }

        /// <summary> Puts an indexed property in an object or in an object in its prototype chain.
        /// <p>
        /// Seaches for the indexed property in the prototype chain. If it is found,
        /// the value of the property is changed. If it is not found, a new
        /// property is added in <code>obj</code>.
        /// </summary>
        /// <param name="obj">a JavaScript object
        /// </param>
        /// <param name="index">a property index
        /// </param>
        /// <param name="value">any JavaScript value accepted by Scriptable.put
        /// </param>
        public static object PutProperty (IScriptable obj, int index, object value)
        {
            IScriptable toBase = GetBase (obj, index);
            if (toBase == null)
                toBase = obj;
            return toBase.Put (index, obj, value);
        }

        /// <summary> Removes the property from an object or its prototype chain.
        /// <p>
        /// Searches for a property with <code>name</code> in obj or
        /// its prototype chain. If it is found, the object's delete
        /// method is called.
        /// </summary>
        /// <param name="obj">a JavaScript object
        /// </param>
        /// <param name="name">a property name
        /// </param>
        /// <returns> true if the property doesn't exist or was successfully removed
        /// </returns>
        public static bool DeleteProperty (IScriptable obj, string name)
        {
            IScriptable toBase = GetBase (obj, name);
            if (toBase == null)
                return true;
            toBase.Delete (name);
            return !toBase.Has (name, obj);
        }

        /// <summary> Removes the property from an object or its prototype chain.
        /// <p>
        /// Searches for a property with <code>index</code> in obj or
        /// its prototype chain. If it is found, the object's delete
        /// method is called.
        /// </summary>
        /// <param name="obj">a JavaScript object
        /// </param>
        /// <param name="index">a property index
        /// </param>
        /// <returns> true if the property doesn't exist or was successfully removed
        /// </returns>
        public static bool DeleteProperty (IScriptable obj, int index)
        {
            IScriptable toBase = GetBase (obj, index);
            if (toBase == null)
                return true;
            toBase.Delete (index);
            return !toBase.Has (index, obj);
        }

        /// <summary> Returns an array of all ids from an object and its prototypes.
        /// <p>
        /// </summary>
        /// <param name="obj">a JavaScript object
        /// </param>
        /// <returns> an array of all ids from all object in the prototype chain.
        /// If a given id occurs multiple times in the prototype chain,
        /// it will occur only once in this list.
        /// </returns>
        public static object [] GetPropertyIds (IScriptable obj)
        {
            if (obj == null) {
                return ScriptRuntime.EmptyArgs;
            }
            object [] result = obj.GetIds ();
            ObjToIntMap map = null;
            for (; ; ) {
                obj = obj.GetPrototype ();
                if (obj == null) {
                    break;
                }
                object [] ids = obj.GetIds ();
                if (ids.Length == 0) {
                    continue;
                }
                if (map == null) {
                    if (result.Length == 0) {
                        result = ids;
                        continue;
                    }
                    map = new ObjToIntMap (result.Length + ids.Length);
                    for (int i = 0; i != result.Length; ++i) {
                        map.intern (result [i]);
                    }
                    result = null; // Allow to GC the result
                }
                for (int i = 0; i != ids.Length; ++i) {
                    map.intern (ids [i]);
                }
            }
            if (map != null) {
                result = map.getKeys ();
            }
            return result;
        }

        /// <summary> Call a method of an object.</summary>
        /// <param name="obj">the JavaScript object
        /// </param>
        /// <param name="methodName">the name of the function property
        /// </param>
        /// <param name="args">the arguments for the call
        /// 
        /// </param>        
        public static object CallMethod (IScriptable obj, string methodName, object [] args)
        {
            return CallMethod (null, obj, methodName, args);
        }

        /// <summary> Call a method of an object.</summary>
        /// <param name="cx">the Context object associated with the current thread.
        /// </param>
        /// <param name="obj">the JavaScript object
        /// </param>
        /// <param name="methodName">the name of the function property
        /// </param>
        /// <param name="args">the arguments for the call
        /// </param>
        public static object CallMethod (Context cx, IScriptable obj, string methodName, object [] args)
        {
            object funObj = GetProperty (obj, methodName);
            if (!(funObj is IFunction)) {
                throw ScriptRuntime.NotFunctionError (obj, methodName);
            }
            IFunction fun = (IFunction)funObj;
            // TODO: What should be the scope when calling funObj?
            // The following favor scope stored in the object on the assumption
            // that is more useful especially under dynamic scope setup.
            // An alternative is to check for dynamic scope flag
            // and use ScriptableObject.getTopLevelScope(fun) if the flag is not
            // set. But that require access to Context and messy code
            // so for now it is not checked.
            IScriptable scope = ScriptableObject.GetTopLevelScope (obj);
            if (cx != null) {
                return fun.Call (cx, scope, obj, args);
            }
            else {
                return Context.Call (null, fun, scope, obj, args);
            }
        }

        private static IScriptable GetBase (IScriptable obj, string name)
        {
            do {
                if (obj.Has (name, obj))
                    break;
                obj = obj.GetPrototype ();
            }
            while (obj != null);
            return obj;
        }

        private static IScriptable GetBase (IScriptable obj, int index)
        {
            do {
                if (obj.Has (index, obj))
                    break;
                obj = obj.GetPrototype ();
            }
            while (obj != null);
            return obj;
        }

        /// <summary> Get arbitrary application-specific value associated with this object.</summary>
        /// <param name="key">key object to select particular value.
        /// </param>        
        public object GetAssociatedValue (object key)
        {
            Hashtable h = associatedValues;
            if (h == null)
                return null;
            return h [key];
        }

        /// <summary> Get arbitrary application-specific value associated with the top scope
        /// of the given scope.
        /// The method first calls {@link #getTopLevelScope(Scriptable scope)}
        /// and then searches the prototype chain of the top scope for the first
        /// object containing the associated value with the given key.
        /// 
        /// </summary>
        /// <param name="scope">the starting scope.
        /// </param>
        /// <param name="key">key object to select particular value.
        /// </param>        
        public static object GetTopScopeValue (IScriptable scope, object key)
        {
            scope = ScriptableObject.GetTopLevelScope (scope);
            for (; ; ) {
                if (scope is ScriptableObject) {
                    ScriptableObject so = (ScriptableObject)scope;
                    object value = so.GetAssociatedValue (key);
                    if (value != null) {
                        return value;
                    }
                }
                scope = scope.GetPrototype ();
                if (scope == null) {
                    return null;
                }
            }
        }

        /// <summary> Associate arbitrary application-specific value with this object.
        /// Value can only be associated with the given object and key only once.
        /// The method ignores any subsequent attempts to change the already
        /// associated value.
        /// <p> The associated values are not serilized.
        /// </summary>
        /// <param name="key">key object to select particular value.
        /// </param>
        /// <param name="value">the value to associate
        /// </param>
        /// <returns> the passed value if the method is called first time for the
        /// given key or old value for any subsequent calls.
        /// </returns>        
        public object AssociateValue (object key, object value)
        {
            if (value == null)
                throw new ArgumentException ();
            Hashtable h = associatedValues;
            if (h == null) {
                lock (this) {
                    h = associatedValues;
                    if (h == null) {
                        h = Hashtable.Synchronized (new Hashtable ());
                        associatedValues = h;
                    }
                }
            }
            return InitHash (h, key, value);
        }

        private object InitHash (Hashtable h, object key, object initialValue)
        {
            lock (h.SyncRoot) {
                object current = h [key];
                if (current == null) {
                    h [key] = initialValue;
                }
                else {
                    initialValue = current;
                }
            }
            return initialValue;
        }

        private Slot GetNamedSlot (string name)
        {
            // Query last access cache and check that it was not deleted
            Slot slot = lastAccess;
            if ((object)name == (object)slot.stringKey && slot.wasDeleted == 0) {
                return slot;
            }
            int hash = name.GetHashCode ();
            Slot [] slots = this.slots; // Get stable local reference
            int i = GetSlotPosition (slots, name, hash);
            if (i < 0) {
                return null;
            }
            slot = slots [i];
            // Update cache - here stringKey.equals(name) holds, but it can be
            // that slot.stringKey != name. To make last name cache work, need
            // to change the key
            slot.stringKey = name;
            lastAccess = slot;
            return slot;
        }

        private Slot GetSlot (string id, int index)
        {
            Slot [] slots = this.slots; // Get local copy
            int i = GetSlotPosition (slots, id, index);
            return (i < 0) ? null : slots [i];
        }

        private static int GetSlotPosition (Slot [] slots, string id, int index)
        {
            if (slots != null) {
                int start = (index & 0x7fffffff) % slots.Length;
                int i = start;
                do {
                    Slot slot = slots [i];
                    if (slot == null)
                        break;
                    if (slot != REMOVED && slot.intKey == index && ((object)slot.stringKey == (object)id || (id != null && id.Equals (slot.stringKey)))) {
                        return i;
                    }
                    if (++i == slots.Length)
                        i = 0;
                }
                while (i != start);
            }
            return -1;
        }

        /// <summary> Add a new slot to the hash table.
        /// 
        /// This method must be synchronized since it is altering the hash
        /// table itself. Note that we search again for the slot to set
        /// since another thread could have added the given property or
        /// caused the table to grow while this thread was searching.
        /// </summary>        
        private Slot AddSlot (string id, int index, Slot newSlot)
        {
            lock (this) {
                if (Sealed) {
                    string str = (id != null) ? id : Convert.ToString (index);
                    throw Context.ReportRuntimeErrorById ("msg.add.sealed", str);
                }



                return AddSlotImpl (id, index, newSlot);
            }
        }

        // Must be inside synchronized (this)
        private Slot AddSlotImpl (string id, int index, Slot newSlot)
        {
            if (slots == null) {
                slots = new Slot [5];
            }
            int start = (index & 0x7fffffff) % slots.Length;
            int i = start;
            for (; ; ) {
                Slot slot = slots [i];
                if (slot == null || slot == REMOVED) {
                    if ((4 * (count + 1)) > (3 * slots.Length)) {
                        Grow ();
                        return AddSlotImpl (id, index, newSlot);
                    }
                    slot = (newSlot == null) ? new Slot () : newSlot;
                    slot.stringKey = id;
                    slot.intKey = index;
                    slots [i] = slot;
                    count++;
                    return slot;
                }
                if (slot.intKey == index && ((object)slot.stringKey == (object)id || (id != null && id.Equals (slot.stringKey)))) {
                    return slot;
                }
                if (++i == slots.Length)
                    i = 0;
                if (i == start) {
                    // slots should never be full or bug in grow code
                    throw new Exception ();
                }
            }
        }

        /// <summary> Remove a slot from the hash table.
        /// 
        /// This method must be synchronized since it is altering the hash
        /// table itself. We might be able to optimize this more, but
        /// deletes are not common.
        /// </summary>        
        private void RemoveSlot (string name, int index)
        {
            lock (this) {
                if (Sealed) {
                    string str = (name != null) ? name : Convert.ToString (index);
                    throw Context.ReportRuntimeErrorById ("msg.remove.sealed", str);
                }

                int i = GetSlotPosition (slots, name, index);
                if (i >= 0) {
                    Slot slot = slots [i];
                    if ((slot.attributes & PERMANENT) == 0) {
                        // Mark the slot as removed to handle a case when
                        // another thread manages to put just removed slot
                        // into lastAccess cache.
                        slot.wasDeleted = (sbyte)1;
                        if (slot == lastAccess) {
                            lastAccess = REMOVED;
                        }
                        count--;
                        if (count != 0) {
                            slots [i] = REMOVED;
                        }
                        else {
                            // With no slots it is OK to mark with null.
                            slots [i] = null;
                        }
                    }
                }
            }
        }

        // Grow the hash table to accommodate new entries.
        //
        // Note that by assigning the new array back at the end we
        // can continue reading the array from other threads.
        // Must be inside synchronized (this)
        private void Grow ()
        {
            Slot [] newSlots = new Slot [slots.Length * 2 + 1];
            for (int j = slots.Length - 1; j >= 0; j--) {
                Slot slot = slots [j];
                if (slot == null || slot == REMOVED)
                    continue;
                int k = (slot.intKey & 0x7fffffff) % newSlots.Length;
                while (newSlots [k] != null)
                    if (++k == newSlots.Length)
                        k = 0;
                // The end of the "synchronized" statement will cause the memory
                // writes to be propagated on a multiprocessor machine. We want
                // to make sure that the new table is prepared to be read.
                // TODO: causes the 'this' pointer to be null in calling stack frames
                // on the MS JVM
                //synchronized (slot) { }
                newSlots [k] = slot;
            }
            slots = newSlots;
        }

        internal virtual object [] GetIds (bool getAll)
        {
            Slot [] s = slots;
            object [] a = ScriptRuntime.EmptyArgs;
            if (s == null)
                return a;
            int c = 0;
            for (int i = 0; i < s.Length; i++) {
                Slot slot = s [i];
                if (slot == null || slot == REMOVED)
                    continue;
                if (getAll || (slot.attributes & DONTENUM) == 0) {
                    if (c == 0)
                        a = new object [s.Length - i];
                    a [c++] = slot.stringKey != null ? (object)slot.stringKey : (int)slot.intKey;
                }
            }
            if (c == a.Length)
                return a;
            object [] result = new object [c];
            Array.Copy (a, 0, result, 0, c);
            return result;
        }


        /// <summary> The prototype of this object.</summary>
        private IScriptable prototypeObject;

        /// <summary> The parent scope of this object.</summary>
        private IScriptable parentScopeObject;

        private static readonly object HAS_STATIC_ACCESSORS = typeof (void);
        private static readonly Slot REMOVED = new Slot ();


        private Slot [] slots;
        // If count >= 0, it gives number of keys or if count < 0,
        // it indicates sealed object where -1 - count gives number of keys
        private int count;

        // cache; may be removed for smaller memory footprint

        private Slot lastAccess = REMOVED;

        // associated values are not serialized

        private volatile Hashtable associatedValues;

        public virtual void DefineSetter (string name, ICallable setter)
        {
            Slot slot = GetSlot (name, name.GetHashCode ());
            if (slot == null) {
                slot = new Slot ();
                AddSlot (name, name.GetHashCode (), slot);
            }
            slot.setter = setter;
        }

        public virtual void DefineSetter (int index, ICallable setter)
        {
            Slot slot = GetSlot (null, index);
            if (slot == null) {
                slot = new Slot ();
                AddSlot (null, index, slot);
            }
            slot.setter = setter;
        }
        
        public virtual void DefineGetter (int index, ICallable getter)
        {
            Slot slot = GetSlot (null, index);
            if (slot == null) {
                slot = new Slot ();
                AddSlot (null, index, slot);
            }
            slot.getter = getter;        
        }
        
        public virtual void DefineGetter (string name, ICallable getter)
        {
            Slot slot = GetSlot (name, name.GetHashCode ());
            if (slot == null) {
                slot = new Slot ();
                AddSlot (name, name.GetHashCode (), slot);
            }
            slot.getter = getter;
        }

        public virtual object LookupGetter (string name)
        {
            Slot slot = GetSlot (name, name.GetHashCode ());
            if (slot == null || slot.getter == null)
                return Undefined.Value;
            return slot.getter;
        }

        public virtual object LookupSetter (string name)
        {
            Slot slot = GetSlot (name, name.GetHashCode ());
            if (slot == null || slot.setter == null)
                return Undefined.Value;
            return slot.setter;
        }

        private class Slot : ICloneable
        {

            internal int intKey;
            internal string stringKey;
            internal object value;
            internal short attributes;

            internal sbyte wasDeleted;

            internal ICallable getter;
            internal ICallable setter;

            public object Clone ()
            {
                Slot clone = new Slot ();
                clone.intKey = intKey;
                clone.stringKey = stringKey;
                clone.value = value;
                clone.attributes = attributes;
                clone.wasDeleted = wasDeleted;
                clone.getter = getter;
                clone.setter = setter;
                return clone;
            }

            internal object GetValue (Context cx, IScriptable scope, IScriptable thisObj)
            {
                if (getter == null) {
                    return value;
                }
                else {
                    if (cx == null)
                        cx = Context.CurrentContext;
                    return getter.Call (cx, scope, thisObj,
                        ScriptRuntime.EmptyArgs);
                }
            }

            internal object SetValue (Context cx, IScriptable scope, IScriptable thisObj, object value)
            {
                if (setter == null) {
                    if (getter == null) {
                        return (this.value = value);
                    }
                    else {
                        throw ScriptRuntime.TypeError ("setting a property that has only a getter");
                    }
                }
                else {
                    if (cx == null)
                        cx = Context.CurrentContext;
                    return setter.Call (cx, scope, thisObj, new object [] { value });
                }
            }

        }

    }
}