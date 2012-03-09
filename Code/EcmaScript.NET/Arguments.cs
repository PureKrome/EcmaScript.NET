//------------------------------------------------------------------------------
// <license file="Arguments.cs">
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

using EcmaScript.NET.Types;

namespace EcmaScript.NET
{


    /// <summary> This class implements the "arguments" object.
    /// 
    /// See ECMA 10.1.8
    /// 
    /// </summary>	
    sealed class Arguments : IdScriptableObject
    {
        override public string ClassName
        {
            get
            {
                return "Arguments";
            }

        }
        override protected internal int MaxInstanceId
        {
            get
            {
                return MAX_INSTANCE_ID;
            }

        }


        public Arguments (BuiltinCall activation)
        {
            this.activation = activation;

            IScriptable parent = activation.ParentScope;
            ParentScope = parent;
            SetPrototype (ScriptableObject.GetObjectPrototype (parent));

            args = activation.originalArgs;
            lengthObj = (int)args.Length;

            BuiltinFunction f = activation.function;
            calleeObj = f;

            Context.Versions version = f.LanguageVersion;
            if (version <= Context.Versions.JS1_3 && version != Context.Versions.Default) {
                callerObj = null;
            }
            else {
                callerObj = UniqueTag.NotFound;
            }
        }

        public override bool Has (int index, IScriptable start)
        {
            if (0 <= index && index < args.Length) {
                if (args [index] != UniqueTag.NotFound) {
                    return true;
                }
            }
            return base.Has (index, start);
        }

        public override object Get (int index, IScriptable start)
        {
            if (0 <= index && index < args.Length) {
                object value = args [index];
                if (value != UniqueTag.NotFound) {
                    if (sharedWithActivation (index)) {
                        BuiltinFunction f = activation.function;
                        string argName = f.getParamOrVarName (index);
                        value = activation.Get (argName, activation);
                        if (value == UniqueTag.NotFound)
                            Context.CodeBug ();
                    }
                    return value;
                }
            }
            return base.Get (index, start);
        }

        private bool sharedWithActivation (int index)
        {
            BuiltinFunction f = activation.function;
            int definedCount = f.ParamCount;
            if (index < definedCount) {
                // Check if argument is not hidden by later argument with the same
                // name as hidden arguments are not shared with activation
                if (index < definedCount - 1) {
                    string argName = f.getParamOrVarName (index);
                    for (int i = index + 1; i < definedCount; i++) {
                        if (argName.Equals (f.getParamOrVarName (i))) {
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public override object Put (int index, IScriptable start, object value)
        {
            if (0 <= index && index < args.Length) {
                if (args [index] != UniqueTag.NotFound) {
                    if (sharedWithActivation (index)) {
                        string argName;
                        argName = activation.function.getParamOrVarName (index);
                        return activation.Put (argName, activation, value);
                    }
                    lock (this) {
                        if (args [index] != UniqueTag.NotFound) {
                            if (args == activation.originalArgs) {
                                args = new object [args.Length];
                                args.CopyTo (args, 0);
                            }
                            args [index] = value;
                            return value;
                        }
                    }
                }
            }
            return base.Put (index, start, value);
        }

        public override void Delete (int index)
        {
            if (0 <= index && index < args.Length) {
                lock (this) {
                    if (args [index] != UniqueTag.NotFound) {
                        if (args == activation.originalArgs) {
                            args = new object [args.Length];
                            args.CopyTo (args, 0);
                        }
                        args [index] = UniqueTag.NotFound;
                        return;
                    }
                }
            }
            base.Delete (index);
        }

        #region InstanceIds
        private const int Id_callee = 1;
        private const int Id_length = 2;
        private const int Id_caller = 3;
        private const int MAX_INSTANCE_ID = 3;
        #endregion

        protected internal override int FindInstanceIdInfo (string s)
        {
            int id;
            #region Generated InstanceId Switch
        L0: {
                id = 0;
                string X = null;
                int c;
                if (s.Length == 6) {
                    c = s [5];
                    if (c == 'e') { X = "callee"; id = Id_callee; }
                    else if (c == 'h') { X = "length"; id = Id_length; }
                    else if (c == 'r') { X = "caller"; id = Id_caller; }
                }
                if (X != null && X != s && !X.Equals (s))
                    id = 0;
            }
        EL0:
            #endregion

            if (id == 0)
                return base.FindInstanceIdInfo (s);

            int attr;
            switch (id) {

                case Id_callee:
                case Id_caller:
                case Id_length:
                    attr = DONTENUM;
                    break;

                default:
                    throw new ApplicationException ();

            }
            return InstanceIdInfo (attr, id);
        }

        // #/string_id_map#

        protected internal override string GetInstanceIdName (int id)
        {
            switch (id) {

                case Id_callee:
                    return "callee";

                case Id_length:
                    return "length";

                case Id_caller:
                    return "caller";
            }
            return null;
        }

        protected internal override object GetInstanceIdValue (int id)
        {
            switch (id) {

                case Id_callee:
                    return calleeObj;

                case Id_length:
                    return lengthObj;

                case Id_caller: {
                        object value = callerObj;
                        if (value == UniqueTag.NullValue) {
                            value = null;
                        }
                        else if (value == null) {
                            BuiltinCall caller = activation.parentActivationCall;
                            if (caller != null) {
                                value = caller.Get ("arguments", caller);
                            }
                            else {
                                value = null;
                            }
                        }
                        return value;
                    }
            }
            return base.GetInstanceIdValue (id);
        }

        protected internal override void SetInstanceIdValue (int id, object value)
        {
            switch (id) {

                case Id_callee:
                    calleeObj = value;
                    return;

                case Id_length:
                    lengthObj = value;
                    return;

                case Id_caller:
                    callerObj = (value != null) ? value : UniqueTag.NullValue;
                    return;
            }
            base.SetInstanceIdValue (id, value);
        }

        internal override object [] GetIds (bool getAll)
        {
            object [] ids = base.GetIds (getAll);
            if (getAll && args.Length != 0) {
                bool [] present = null;
                int extraCount = args.Length;
                for (int i = 0; i != ids.Length; ++i) {
                    object id = ids [i];
                    if (id is int) {
                        int index = ((int)id);
                        if (0 <= index && index < args.Length) {
                            if (present == null) {
                                present = new bool [args.Length];
                            }
                            if (!present [index]) {
                                present [index] = true;
                                extraCount--;
                            }
                        }
                    }
                }
                if (extraCount != 0) {
                    object [] tmp = new object [extraCount + ids.Length];
                    Array.Copy (ids, 0, tmp, extraCount, ids.Length);
                    ids = tmp;
                    int offset = 0;
                    for (int i = 0; i != args.Length; ++i) {
                        if (present == null || !present [i]) {
                            ids [offset] = (int)i;
                            ++offset;
                        }
                    }
                    if (offset != extraCount)
                        Context.CodeBug ();
                }
            }
            return ids;
        }

        // Fields to hold caller, callee and length properties,
        // where NOT_FOUND value tags deleted properties.
        // In addition if callerObj == NullValue, it tags null for scripts, as
        // initial callerObj == null means access to caller arguments available
        // only in JS <= 1.3 scripts
        private object callerObj;
        private object calleeObj;
        private object lengthObj;

        private BuiltinCall activation;

        // Initially args holds activation.getOriginalArgs(), but any modification
        // of its elements triggers creation of a copy. If its element holds NOT_FOUND,
        // it indicates deleted index, in which case super class is queried.
        private object [] args;

    }
}
