//------------------------------------------------------------------------------
// <license file="NativeCliMethodInfo.cs">
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
using System.Reflection;

namespace EcmaScript.NET.Types.Cli
{

    public class CliMethodInfo : BaseFunction
    {

        private string m_Name = string.Empty;

        public override string ClassName
        {
            get
            {
                return m_Name;
            }
        }

        public override string FunctionName
        {
            get
            {
                return m_Name;
            }
        }

        private MethodInfo [] m_MethodInfos = null;

        private object m_Target = null;

        private bool [] paramsParameters = null;

        public CliMethodInfo (object target, string methodName)
        {
            Init (methodName, new MemberInfo [] { 
                target.GetType().GetMethod(methodName) }, target);
        }

        public CliMethodInfo (string name, MemberInfo methodInfo, object target)
        {
            Init (name, new MemberInfo [] { methodInfo }, target);
        }

        public CliMethodInfo (string name, MemberInfo [] methodInfos, object target)
        {
            Init (name, methodInfos, target);
        }


        void Init (string name, MemberInfo [] methodInfos, object target)
        {
            m_Name = name;
            m_MethodInfos = new MethodInfo [methodInfos.Length];
            Array.Copy (
                methodInfos, 0, m_MethodInfos, 0, m_MethodInfos.Length);
            m_Target = target;

            // Cache paramsParameters attribute
            paramsParameters = new bool [methodInfos.Length];
            for (int i = 0; i < methodInfos.Length; i++) {
                paramsParameters [i] = CliHelper.HasParamsParameter (
                    m_MethodInfos [i]);
            }
        }


        public override object Call (Context cx, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (m_MethodInfos.Length == 0) {
                throw new ApplicationException ("No methods defined for call");
            }

            int index = FindFunction (cx, m_MethodInfos, args, paramsParameters);
            if (index < 0) {
                Type c = m_MethodInfos [0].DeclaringType;
                string sig = c.FullName + '.' + FunctionName + '(' + ScriptSignature (args) + ')';
                throw Context.ReportRuntimeErrorById ("msg.java.no_such_method", sig);
            }

            MethodBase meth = (MethodBase)m_MethodInfos [index];
            ParameterInfo [] pis = meth.GetParameters ();

            // First, we marshall the args.
            object [] origArgs = args;
            for (int i = 0; i < args.Length; i++) {
                object arg = args [i];

                if (paramsParameters [index] && i >= pis.Length - 1) {
                    // params[] arg is always an array type
                    Type arrayType = pis [pis.Length - 1].ParameterType;
                    Type arrayElementType = arrayType.GetElementType ();

                    object [] dummyArg = new object [args.Length - i];
                    for (int e = i; e < args.Length; e++) {
                        dummyArg [e - i] = Context.JsToCli (arg, arrayElementType);
                    }
                    args = new object [i + 1];
                    args.CopyTo (args, 0);
                    args [i] = dummyArg;
                }
                else {
                    Type argType = pis [i].ParameterType;
                    object coerced = Context.JsToCli (arg, argType);
                    if (coerced != arg) {
                        if (origArgs == args) {
                            args = new object [args.Length];
                            args.CopyTo (args, 0);
                        }
                        args [i] = coerced;
                    }
                }
            }
            object cliObject;
            if (meth.IsStatic) {
                cliObject = null; // don't need an object
            }
            else {
                IScriptable o = thisObj;
                Type c = meth.DeclaringType;
                for (; ; ) {
                    if (o == null) {
                        throw Context.ReportRuntimeErrorById ("msg.nonjava.method", FunctionName, ScriptConvert.ToString (thisObj), c.FullName);
                    }
                    if (o is Wrapper) {
                        cliObject = ((Wrapper)o).Unwrap ();
                        if (c.IsInstanceOfType (cliObject)) {
                            break;
                        }
                    }
                    o = o.GetPrototype ();
                }
            }

            object retval = null;
            try {
                retval = (meth as MethodBase).Invoke (cliObject, args);
            }
            catch (Exception ex) {
                Context.ThrowAsScriptRuntimeEx (ex);
            }

            Type staticType = meth.DeclaringType;
            if (meth is MethodInfo)
                staticType = ((MethodInfo)meth).ReturnType;

            object wrapped = cx.Wrap (scope, retval, staticType);
            if (wrapped == null && staticType == Type.GetType ("System.Void")) {
                wrapped = Undefined.Value;
            }
            return wrapped;
        }


        /// <summary> Find the index of the correct function to call given the set of methods
        /// or constructors and the arguments.
        /// If no function can be found to call, return -1.
        /// </summary>
        internal static int FindFunction (Context cx, MethodBase [] methodsOrCtors, object [] args, bool [] paramsParameter)
        {
            if (methodsOrCtors.Length == 0) {
                return -1;
            }
            else if (methodsOrCtors.Length == 1) {
                MethodBase member = methodsOrCtors [0];
                ParameterInfo [] pis = member.GetParameters ();
                int alength = pis.Length;

                if (alength != args.Length) {
                    if (!paramsParameter [0]) {
                        return -1;
                    }
                }

                for (int j = 0; j != alength; ++j) {
                    object arg = args [j];
                    Type argType = null;

                    if (paramsParameter [0] && j >= pis.Length - 1) {
                        // params[] arg is always an array type
                        argType = pis [pis.Length - 1].ParameterType.GetElementType ();
                    }
                    else {
                        argType = pis [j].ParameterType;
                    }

                    if (!CliObject.CanConvert (arg, argType)) {
                        //if (debug)
                        //	printDebug("Rejecting (args can't convert) ", member, args);
                        return -1;
                    }
                }
                //if (debug)
                //	printDebug("Found ", member, args);
                return 0;
            }

            int firstBestFit = -1;
            int [] extraBestFits = null;
            int extraBestFitsCount = 0;

            for (int i = 0; i < methodsOrCtors.Length; i++) {
                MethodBase member = methodsOrCtors [i];
                ParameterInfo [] pis = member.GetParameters ();
                if (pis.Length != args.Length) {
                    goto search;
                }
                for (int j = 0; j < pis.Length; j++) {
                    if (!CliObject.CanConvert (args [j], pis [j].ParameterType)) {
                        //if (debug)
                        //	printDebug("Rejecting (args can't convert) ", member, args);						
                        goto search;
                    }
                }
                if (firstBestFit < 0) {
                    //if (debug)
                    //	printDebug("Found first applicable ", member, args);
                    firstBestFit = i;
                }
                else {
                    // Compare with all currently fit methods.
                    // The loop starts from -1 denoting firstBestFit and proceed
                    // until extraBestFitsCount to avoid extraBestFits allocation
                    // in the most common case of no ambiguity
                    int betterCount = 0; // number of times member was prefered over
                    // best fits
                    int worseCount = 0; // number of times best fits were prefered
                    // over member
                    for (int j = -1; j != extraBestFitsCount; ++j) {
                        int bestFitIndex;
                        if (j == -1) {
                            bestFitIndex = firstBestFit;
                        }
                        else {
                            bestFitIndex = extraBestFits [j];
                        }
                        MethodBase bestFit = methodsOrCtors [bestFitIndex];
                        int preference = PreferSignature (args, pis, bestFit.GetParameters ());
                        if (preference == PREFERENCE_AMBIGUOUS) {
                            break;
                        }
                        else if (preference == PREFERENCE_FIRST_ARG) {
                            ++betterCount;
                        }
                        else if (preference == PREFERENCE_SECOND_ARG) {
                            ++worseCount;
                        }
                        else {
                            if (preference != PREFERENCE_EQUAL)
                                Context.CodeBug ();
                            // This should not happen in theory
                            // but on some JVMs, Class.getMethods will return all
                            // static methods of the class heirarchy, even if
                            // a derived class's parameters match exactly.
                            // We want to call the dervied class's method.
                            if (bestFit.IsStatic && bestFit.DeclaringType.IsAssignableFrom (member.DeclaringType)) {
                                // On some JVMs, Class.getMethods will return all
                                // static methods of the class heirarchy, even if
                                // a derived class's parameters match exactly.
                                // We want to call the dervied class's method.
                                //if (debug)
                                //	printDebug("Substituting (overridden static)", member, args);
                                if (j == -1) {
                                    firstBestFit = i;
                                }
                                else {
                                    extraBestFits [j] = i;
                                }
                            }
                            else {
                                //if (debug)
                                //	printDebug("Ignoring same signature member ", member, args);
                            }
                            goto search;
                        }
                    }
                    if (betterCount == 1 + extraBestFitsCount) {
                        // member was prefered over all best fits
                        //if (debug)
                        //	printDebug("New first applicable ", member, args);
                        firstBestFit = i;
                        extraBestFitsCount = 0;
                    }
                    else if (worseCount == 1 + extraBestFitsCount) {
                        // all best fits were prefered over member, ignore it
                        //if (debug)
                        //	printDebug("Rejecting (all current bests better) ", member, args);
                    }
                    else {
                        // some ambiguity was present, add member to best fit set
                        //if (debug)
                        //	printDebug("Added to best fit set ", member, args);
                        if (extraBestFits == null) {
                            // Allocate maximum possible array
                            extraBestFits = new int [methodsOrCtors.Length - 1];
                        }
                        extraBestFits [extraBestFitsCount] = i;
                        ++extraBestFitsCount;
                    }
                }
            //UPGRADE_NOTE: Label 'search' was moved. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1014_3"'

            search:
                ;
            }

            if (firstBestFit < 0) {
                // Nothing was found
                return -1;
            }
            else if (extraBestFitsCount == 0) {
                // single best fit
                return firstBestFit;
            }

            // report remaining ambiguity
            System.Text.StringBuilder buf = new System.Text.StringBuilder ();
            for (int j = -1; j != extraBestFitsCount; ++j) {
                int bestFitIndex;
                if (j == -1) {
                    bestFitIndex = firstBestFit;
                }
                else {
                    bestFitIndex = extraBestFits [j];
                }
                buf.Append ("\n    ");
                buf.Append (CliHelper.ToSignature (methodsOrCtors [bestFitIndex]));
            }

            MethodBase firstFitMember = methodsOrCtors [firstBestFit];
            string memberName = firstFitMember.Name;
            //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Class.getName' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043_3"'
            string memberClass = firstFitMember.DeclaringType.FullName;

            if (methodsOrCtors [0] is MethodInfo) {
                throw Context.ReportRuntimeErrorById ("msg.constructor.ambiguous", memberName, ScriptSignature (args), buf.ToString ());
            }
            else {
                throw Context.ReportRuntimeErrorById ("msg.method.ambiguous", memberClass, memberName, ScriptSignature (args), buf.ToString ());
            }
        }

        internal static string ScriptSignature (object [] values)
        {
            System.Text.StringBuilder sig = new System.Text.StringBuilder ();
            for (int i = 0; i != values.Length; ++i) {
                object value = values [i];

                string s;
                if (value == null) {
                    s = "null";
                }
                else if (value is System.Boolean) {
                    s = "boolean";
                }
                else if (value is string) {
                    s = "string";
                }
                else if (CliHelper.IsNumber (value)) {
                    s = "number";
                }
                else if (value is IScriptable) {
                    if (value is Undefined) {
                        s = "undefined";
                    }
                    else if (value is Wrapper) {
                        object wrapped = ((Wrapper)value).Unwrap ();
                        //UPGRADE_TODO: The equivalent in .NET for method 'java.lang.Class.getName' may return a different value. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1043_3"'
                        s = wrapped.GetType ().FullName;
                    }
                    else if (value is IFunction) {
                        s = "function";
                    }
                    else {
                        s = "object";
                    }
                }
                else {
                    s = CliHelper.ToSignature (value.GetType ());
                }

                if (i != 0) {
                    sig.Append (',');
                }
                sig.Append (s);
            }
            return sig.ToString ();
        }

        /// <summary>Types are equal </summary>
        private const int PREFERENCE_EQUAL = 0;
        private const int PREFERENCE_FIRST_ARG = 1;
        private const int PREFERENCE_SECOND_ARG = 2;
        /// <summary>No clear "easy" conversion </summary>
        private const int PREFERENCE_AMBIGUOUS = 3;

        /// <summary> Determine which of two signatures is the closer fit.
        /// Returns one of PREFERENCE_EQUAL, PREFERENCE_FIRST_ARG,
        /// PREFERENCE_SECOND_ARG, or PREFERENCE_AMBIGUOUS.
        /// </summary>
        private static int PreferSignature (object [] args, ParameterInfo [] sig1, ParameterInfo [] sig2)
        {
            int totalPreference = 0;
            for (int j = 0; j < args.Length; j++) {
                Type type1 = sig1 [j].ParameterType;
                Type type2 = sig2 [j].ParameterType;
                if (type1 == type2) {
                    continue;
                }
                object arg = args [j];

                // Determine which of type1, type2 is easier to convert from arg.

                int rank1 = CliObject.GetConversionWeight (arg, type1);
                int rank2 = CliObject.GetConversionWeight (arg, type2);

                int preference;
                if (rank1 < rank2) {
                    preference = PREFERENCE_FIRST_ARG;
                }
                else if (rank1 > rank2) {
                    preference = PREFERENCE_SECOND_ARG;
                }
                else {
                    // Equal ranks
                    if (rank1 == CliObject.CONVERSION_NONTRIVIAL) {
                        if (type1.IsAssignableFrom (type2)) {
                            preference = PREFERENCE_SECOND_ARG;
                        }
                        else if (type2.IsAssignableFrom (type1)) {
                            preference = PREFERENCE_FIRST_ARG;
                        }
                        else {
                            preference = PREFERENCE_AMBIGUOUS;
                        }
                    }
                    else {
                        preference = PREFERENCE_AMBIGUOUS;
                    }
                }

                totalPreference |= preference;

                if (totalPreference == PREFERENCE_AMBIGUOUS) {
                    break;
                }
            }
            return totalPreference;
        }

        public override object GetDefaultValue (Type typeHint)
        {
            if (typeHint == typeof (String))
                return ToString ();
            return base.GetDefaultValue (typeHint);
        }

        public override string ToString ()
        {
            string ret = "function " + m_Name + "() \n";
            ret += "{/*\n";
            foreach (MethodInfo mi in m_MethodInfos) {
                ret += CliHelper.ToSignature (mi) + "\n";
            }
            ret += "*/}";
            return ret;
        }



    }
}
