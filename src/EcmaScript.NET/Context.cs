//------------------------------------------------------------------------------
// <license file="Context.cs">
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
using System.Threading;
using System.Globalization;
using System.Reflection;
using System.Collections;

using EcmaScript.NET.Debugging;
using EcmaScript.NET.Collections;
using EcmaScript.NET.Types;
using EcmaScript.NET.Types.Cli;
using EcmaScript.NET.Types.E4X;

namespace EcmaScript.NET
{

    /// <summary> 
    /// This class represents the runtime context of an executing script.
    /// 
    /// Before executing a script, an instance of Context must be created
    /// and associated with the thread that will be executing the script.
    /// The Context will be used to store information about the executing
    /// of the script such as the call stack. Contexts are associated with
    /// the current thread  using the {@link #call(ContextAction)}
    /// or {@link #enter()} methods.<p>
    /// 
    /// Different forms of script execution are supported. Scripts may be
    /// evaluated from the source directly, or first compiled and then later
    /// executed. Interactive execution is also supported.<p>
    /// 
    /// Some aspects of script execution, such as type conversions and
    /// object creation, may be accessed directly through methods of
    /// Context.
    /// 
    /// </summary>    
    public class Context : IDisposable
    {

        public enum Features
        {
            /// <summary>
            /// No features at all
            /// </summary>
            None = 0,

            /// <summary>
            /// Support for E4X
            /// </summary>
            E4x = 1 << 1,

            /// <summary>
            /// Support for get and set
            /// </summary>
            GetterAndSetter = 1 << 2,

            /// <summary>
            /// 
            /// </summary>
            NonEcmaGetYear = 1 << 3,

            /// <summary> Control if dynamic scope should be used for name access.
            /// If hasFeature(FEATURE_DYNAMIC_SCOPE) returns true, then the name lookup
            /// during name resolution will use the top scope of the script or function
            /// which is at the top of JS execution stack instead of the top scope of the
            /// script or function from the current stack frame if the top scope of
            /// the top stack frame contains the top scope of the current stack frame
            /// on its prototype chain.
            /// <p>
            /// This is useful to define shared scope containing functions that can
            /// be called from scripts and functions using private scopes.
            /// <p>
            /// By default {@link #hasFeature(int)} returns false.
            /// </summary>        
            DynamicScope = 1 << 4,
            /// <summary> Control if member expression as function name extension is available.
            /// If <tt>hasFeature(FEATURE_MEMBER_EXPR_AS_FUNCTION_NAME)</tt> returns
            /// true, allow <tt>function memberExpression(args) { body }</tt> to be
            /// syntax sugar for <tt>memberExpression = function(args) { body }</tt>,
            /// when memberExpression is not a simple identifier.
            /// See ECMAScript-262, section 11.2 for definition of memberExpression.
            /// By default {@link #hasFeature(int)} returns false.
            /// </summary>
            MemberExprAsFunctionName = 1 << 5,

            /// <summary> Control if reserved keywords are treated as identifiers.
            /// If <tt>hasFeature(RESERVED_KEYWORD_AS_IDENTIFIER)</tt> returns true,
            /// treat future reserved keyword (see  Ecma-262, section 7.5.3) as ordinary
            /// identifiers but warn about this usage.
            /// 
            /// By default {@link #hasFeature(int)} returns false.
            /// </summary>
            ReservedKeywordAsIdentifier = 1 << 6,

            /// <summary> Control if <tt>toString()</tt> should returns the same result
            /// as  <tt>toSource()</tt> when applied to objects and arrays.
            /// If <tt>hasFeature(FEATURE_TO_STRING_AS_SOURCE)</tt> returns true,
            /// calling <tt>toString()</tt> on JS objects gives the same result as
            /// calling <tt>toSource()</tt>. That is it returns JS source with code
            /// to create an object with all enumeratable fields of the original object
            /// instead of printing <tt>[object <i>result of
            /// {@link Scriptable#getClassName()}</i>]</tt>.
            /// <p>
            /// By default {@link #hasFeature(int)} returns true only if
            /// the current JS version is set to {@link #Versions.JS1_2}.
            /// </summary>
            ToStringAsSource = 1 << 7,

            /// <summary> Control if properties <tt>__proto__</tt> and <tt>__parent__</tt>
            /// are treated specially.
            /// If <tt>hasFeature(FEATURE_PARENT_PROTO_PROPRTIES)</tt> returns true,
            /// treat <tt>__parent__</tt> and <tt>__proto__</tt> as special properties.
            /// <p>
            /// The properties allow to query and set scope and prototype chains for the
            /// objects. The special meaning of the properties is available
            /// only when they are used as the right hand side of the dot operator.
            /// For example, while <tt>x.__proto__ = y</tt> changes the prototype
            /// chain of the object <tt>x</tt> to point to <tt>y</tt>,
            /// <tt>x["__proto__"] = y</tt> simply assigns a new value to the property
            /// <tt>__proto__</tt> in <tt>x</tt> even when the feature is on.
            /// 
            /// By default {@link #hasFeature(int)} returns true.
            /// </summary>
            ParentProtoProperties = 1 << 8,

            /// <summary> Control if strict variable mode is enabled.
            /// When the feature is on Rhino reports runtime errors if assignment
            /// to a global variable that does not exist is executed. When the feature
            /// is off such assignments creates new variable in the global scope  as
            /// required by ECMA 262.
            /// <p>
            /// By default {@link #hasFeature(int)} returns false.
            /// </summary>
            StrictVars = 1 << 9,

            /// <summary> Control if strict eval mode is enabled.
            /// When the feature is on Rhino reports runtime errors if non-string
            /// argument is passed to the eval function. When the feature is off
            /// eval simply return non-string argument as is without performing any
            /// evaluation as required by ECMA 262.
            /// <p>
            /// By default {@link #hasFeature(int)} returns false.
            /// </summary>        
            StrictEval = 1 << 10,

            /// <summary>
            /// Controls if the non-ecma function 'print' is available or not.
            /// </summary>
            NonEcmaPrintFunction = 1 << 11,

            /// <summary>
            /// Controls if the non-ecma function 'version' is available or not
            /// </summary>
            NonEcmaVersionFunction = 1 << 12,

            /// <summary>
            /// Controls if the non-ecma function 'options' is available or not
            /// </summary>
            NonEcmaOptionsFunction = 1 << 13,

            Strict = 1 << 14,

            /// <summary>
            /// Controls if the non-ecma function 'options' is available or not
            /// </summary>
            NonEcmaGcFunction = 1 << 14,
            
            /// <summary>
            /// The famous 'it' object (@see js.c:2315)
            /// </summary>
            NonEcmaItObject = 1 << 15,

        }

        public enum Versions
        {
            Unknown = -1,
            Default = 0,
            JS1_0 = 100,
            JS1_1 = 110,
            JS1_2 = 120,
            JS1_3 = 130,
            JS1_4 = 140,
            JS1_5 = 150,
            JS1_6 = 160
        }

        /// <summary>
        /// 
        /// </summary>
        public event ContextWrapHandler OnWrap;

        private int m_MaximumInterpreterStackDepth = 8092;

        /// <summary>
        /// 
        /// </summary>
        public int MaximumInterpreterStackDepth
        {
            get
            {
                return m_MaximumInterpreterStackDepth;
            }
            set
            {
                if (Sealed)
                    OnSealedMutation ();
                m_MaximumInterpreterStackDepth = value;
            }
        }

        private AppDomain m_AppDomain = null;

        /// <summary>
        /// Associated app domain
        /// </summary>
        public AppDomain AppDomain
        {
            get { return m_AppDomain; }
        }

        private static LocalDataStoreSlot LocalSlot
        {
            get
            {
                LocalDataStoreSlot slot = Thread.GetNamedDataSlot ("Context");
                if (slot == null) {
                    slot = Thread.AllocateNamedDataSlot ("Context");
                }
                return slot;
            }
        }

        /// <summary> Get the current Context.
        /// 
        /// The current Context is per-thread; this method looks up
        /// the Context associated with the current thread. <p>
        /// 
        /// </summary>
        /// <returns> the Context associated with the current thread, or
        /// null if no context is associated with the current
        /// thread.
        /// </returns>        
        public static Context CurrentContext
        {
            get
            {
                Context cx = (Context)Thread.GetData (LocalSlot);
                return cx;
            }
        }

        /// <summary> Return {@link ContextFactory} instance used to create this Context
        /// or the result of {@link ContextFactory#getGlobal()} if no factory
        /// was used for Context creation.
        /// </summary>
        public ContextFactory Factory
        {
            get
            {
                ContextFactory result = factory;
                if (result == null) {
                    result = ContextFactory.Global;
                }
                return result;
            }

        }
        /// <summary> Checks if this is a sealed Context. A sealed Context instance does not
        /// allow to modify any of its properties and will throw an exception
        /// on any such attempt.
        /// </summary>        
        public bool Sealed
        {
            get
            {
                return m_Sealed;
            }

        }
        /// <summary> Get the implementation version.
        /// 
        /// <p>
        /// The implementation version is of the form
        /// <pre>
        /// "<i>name langVer</i> <code>release</code> <i>relNum date</i>"
        /// </pre>
        /// where <i>name</i> is the name of the product, <i>langVer</i> is
        /// the language version, <i>relNum</i> is the release number, and
        /// <i>date</i> is the release date for that specific
        /// release in the form "yyyy mm dd".
        /// 
        /// </summary>
        /// <returns> a string that encodes the product, language version, release
        /// number, and date.
        /// </returns>
        public string ImplementationVersion
        {
            get
            {
                // TODO: Probably it would be better to embed this directly into source
                // TODO: with special build preprocessing but that would require some ant
                // TODO: tweaking and then replacing token in resource files was simpler
                if (implementationVersion == null) {
                    implementationVersion = ScriptRuntime.GetMessage ("implementation.version");
                }
                return implementationVersion;
            }

        }
        /// <summary> Get the singleton object that represents the JavaScript Undefined value.</summary>
        public static object UndefinedValue
        {
            get
            {
                return Undefined.Value;
            }

        }
        /// <summary> Specify whether or not debug information should be generated.
        /// <p>
        /// Setting the generation of debug information on will set the
        /// optimization level to zero.
        /// </summary>
        public bool GeneratingDebug
        {
            get
            {
                return generatingDebug;
            }

            set
            {
                if (m_Sealed)
                    OnSealedMutation ();
                generatingDebugChanged = true;
                if (value && OptimizationLevel > 0)
                    OptimizationLevel = 0;
                this.generatingDebug = value;
            }

        }

        /// <summary> Specify whether or not source information should be generated.
        /// <p>
        /// Without source information, evaluating the "toString" method
        /// on JavaScript functions produces only "[native code]" for
        /// the body of the function.
        /// Note that code generated without source is not fully ECMA
        /// conformant.
        /// </summary>
        public bool GeneratingSource
        {
            get
            {
                return generatingSource;
            }
            set
            {
                if (m_Sealed)
                    OnSealedMutation ();
                this.generatingSource = value;
            }

        }
        /// <summary> Set the current optimization level.
        /// <p>
        /// The optimization level is expected to be an integer between -1 and
        /// 9. Any negative values will be interpreted as -1, and any values
        /// greater than 9 will be interpreted as 9.
        /// An optimization level of -1 indicates that interpretive mode will
        /// always be used. Levels 0 through 9 indicate that class files may
        /// be generated. Higher optimization levels trade off compile time
        /// performance for runtime performance.
        /// The optimizer level can't be set greater than -1 if the optimizer
        /// package doesn't exist at run time.
        /// </summary>
        /// <param name="optimizationLevel">an integer indicating the level of
        /// optimization to perform
        /// </param>
        public int OptimizationLevel
        {
            get
            {
                return m_OptimizationLevel;
            }

            set
            {
                if (m_Sealed)
                    OnSealedMutation ();
                if (value == -2) {
                    // To be compatible with Cocoon fork
                    value = -1;
                }
                CheckOptimizationLevel (value);
                this.m_OptimizationLevel = value;
            }

        }

        /// <summary> Return the debugger context data associated with current context.</summary>
        /// <returns> the debugger data, or null if debugger is not attached
        /// </returns>
        public object DebuggerContextData
        {
            get
            {
                return debuggerData;
            }

        }

        public object Wrap (IScriptable scope, object obj, Type staticType)
        {
            if (obj == null || obj is IScriptable)
                return obj;
            if (staticType == null)
                staticType = obj.GetType ();

            if (staticType.IsArray)
                return new CliArray (scope, obj as Array);

            if (staticType.IsPrimitive)
                return obj;

            if (OnWrap != null) {
                ContextWrapEventArgs e = new ContextWrapEventArgs (this, scope, obj, staticType);
                OnWrap (this, e);
                obj = e.Target;
            }

            return new CliObject (obj);
        }

        /// <summary> Get/Set threshold of executed instructions counter that triggers call to
        /// <code>observeInstructionCount()</code>.
        /// When the threshold is zero, instruction counting is disabled,
        /// otherwise each time the run-time executes at least the threshold value
        /// of script instructions, <code>observeInstructionCount()</code> will
        /// be called.
        /// </summary>
        public int InstructionObserverThreshold
        {
            get
            {
                return instructionThreshold;
            }

            set
            {
                if (m_Sealed)
                    OnSealedMutation ();
                if (value < 0)
                    throw new ArgumentException ();
                instructionThreshold = value;
            }

        }

        internal RegExpProxy RegExpProxy
        {
            get
            {
                if (regExpProxy == null) {
                    regExpProxy = new Types.RegExp.RegExpImpl ();
                }
                return regExpProxy;
            }
            set {
                regExpProxy = value;
            }
        }
        internal bool VersionECMA1
        {
            get
            {
                return m_Version == Versions.Default || m_Version >= Versions.JS1_3;
            }

        }
        public bool GeneratingDebugChanged
        {
            get
            {
                return generatingDebugChanged;
            }

        }
        /// <summary> Language versions.
        /// 
        /// All integral values are reserved for future version numbers.
        /// </summary>











        public const string languageVersionProperty = "language version";
        public const string errorReporterProperty = "error reporter";

        /// <summary> Convinient value to use as zero-length array of objects.</summary>
        public static readonly object [] EmptyArgs;

        /// <summary> Create a new Context.
        /// 
        /// Note that the Context must be associated with a thread before
        /// it can be used to execute a script.
        /// 
        /// </summary>        
        public Context (AppDomain appDomain)
        {
            if (appDomain == null)
                throw new ArgumentNullException ("appDomain");
            Version = Versions.Default;
            m_AppDomain = appDomain;
        }

        /// <summary> Get a context associated with the current thread, creating
        /// one if need be.
        /// 
        /// The Context stores the execution state of the JavaScript
        /// engine, so it is required that the context be entered
        /// before execution may begin. Once a thread has entered
        /// a Context, then getCurrentContext() may be called to find
        /// the context that is associated with the current thread.
        /// <p>
        /// Calling <code>enter()</code> will
        /// return either the Context currently associated with the
        /// thread, or will create a new context and associate it
        /// with the current thread. Each call to <code>enter()</code>
        /// must have a matching call to <code>exit()</code>. For example,
        /// <pre>
        /// Context cx = Context.enter();
        /// try {
        /// ...
        /// cx.evaluateString(...);
        /// } finally {
        /// Context.exit();
        /// }
        /// </pre>
        /// Instead of using <tt>enter()</tt>, <tt>exit()</tt> pair consider using
        /// {@link #call(ContextAction)} which guarantees proper
        /// association of Context instances with the current thread and is faster.
        /// With this method the above example becomes:
        /// <pre>
        /// Context.call(new ContextAction() {
        /// public Object run(Context cx) {
        /// ...
        /// cx.evaluateString(...);
        /// return null;
        /// }
        /// });
        /// </pre>
        /// 
        /// </summary>
        /// <returns> a Context associated with the current thread
        /// </returns>        
        public static Context Enter ()
        {
            return Enter (null, AppDomain.CurrentDomain);
        }

        public static Context Enter (AppDomain appDomain)
        {
            return Enter (null, appDomain);
        }

        /// <summary> Get a Context associated with the current thread, using
        /// the given Context if need be.
        /// <p>
        /// The same as <code>enter()</code> except that <code>cx</code>
        /// is associated with the current thread and returned if
        /// the current thread has no associated context and <code>cx</code>
        /// is not associated with any other thread.
        /// </summary>
        /// <param name="cx">a Context to associate with the thread if possible
        /// </param>
        /// <returns> a Context associated with the current thread
        /// 
        /// </returns>
        public static Context Enter (Context cx)
        {
            return Enter (cx, AppDomain.CurrentDomain);
        }

        public static Context Enter (Context cx, AppDomain appDomain)
        {
            Context old = CurrentContext;
            if (old != null) {
                if (cx != null && cx != old && cx.enterCount != 0) {
                    // The suplied context must be the context for
                    // the current thread if it is already entered
                    throw new ArgumentException ("Cannot enter Context active on another thread");
                }
                if (old.factory != null) {
                    // Context with associated factory will be released
                    // automatically and does not need to change enterCount
                    return old;
                }
                if (old.m_Sealed)
                    OnSealedMutation ();
                cx = old;
            }
            else {
                if (cx == null) {
                    cx = new Context (appDomain);
                }
                else {
                    if (cx.m_Sealed)
                        OnSealedMutation ();
                }
                if (cx.enterCount != 0 || cx.factory != null) {
                    throw new Exception ();
                }

                if (!cx.creationEventWasSent) {
                    cx.creationEventWasSent = true;
                    ContextFactory.Global.FireOnContextCreated (cx);
                }
            }

            if (old == null) {
                Thread.SetData (LocalSlot, cx);
            }
            ++cx.enterCount;

            return cx;
        }

        /// <summary> Exit a block of code requiring a Context.
        /// 
        /// Calling <code>exit()</code> will remove the association between
        /// the current thread and a Context if the prior call to
        /// <code>enter()</code> on this thread newly associated a Context
        /// with this thread.
        /// Once the current thread no longer has an associated Context,
        /// it cannot be used to execute JavaScript until it is again associated
        /// with a Context.
        /// 
        /// </summary>
        public static void Exit ()
        {
            Context cx = CurrentContext;
            if (cx == null) {
                throw new Exception ("Calling Context.exit without previous Context.enter");
            }
            if (cx.factory != null) {
                // Context with associated factory will be released
                // automatically and does not need to change enterCount
                return;
            }
            if (cx.enterCount < 1)
                Context.CodeBug ();
            if (cx.m_Sealed)
                OnSealedMutation ();
            --cx.enterCount;
            if (cx.enterCount == 0) {
                Thread.SetData (LocalSlot, null);
                ContextFactory.Global.FireOnContextReleased (cx);
            }
        }


        /// <summary> Call {@link
        /// Callable#call(Context cx, Scriptable scope, Scriptable thisObj,
        /// Object[] args)}
        /// using the Context instance associated with the current thread.
        /// If no Context is associated with the thread, then
        /// {@link ContextFactory#makeContext()} will be called to construct
        /// new Context instance. The instance will be temporary associated
        /// with the thread during call to {@link ContextAction#run(Context)}.
        /// <p>
        /// It is allowed to use null for <tt>factory</tt> argument
        /// in which case the factory associated with the scope will be
        /// used to create new context instances.
        /// 
        /// </summary>
        public static object Call (ContextFactory factory, ICallable callable, IScriptable scope, IScriptable thisObj, object [] args)
        {
            if (factory == null) {
                factory = ContextFactory.Global;
            }

            Context cx = CurrentContext;
            if (cx != null) {
                object result;
                if (cx.factory != null) {
                    result = callable.Call (cx, scope, thisObj, args);
                }
                else {
                    // Context was associated with the thread via Context.enter,
                    // set factory to make Context.enter/exit to be no-op
                    // during call
                    cx.factory = factory;
                    try {
                        result = callable.Call (cx, scope, thisObj, args);
                    }
                    finally {
                        cx.factory = null;
                    }
                }
                return result;
            }

            cx = PrepareNewContext (AppDomain.CurrentDomain, factory);
            try {
                return callable.Call (cx, scope, thisObj, args);
            }
            finally {
                ReleaseContext (cx);
            }
        }

        internal void InitDefaultFeatures ()
        {
            m_Features = Features.None;

            SetFeature (Features.E4x, (Version == Context.Versions.Default || Version >= Context.Versions.JS1_6));
            SetFeature (Features.GetterAndSetter, (Version == Context.Versions.Default || Version >= Context.Versions.JS1_5));
            SetFeature (Features.NonEcmaGetYear, (Version == Context.Versions.JS1_0 || Version == Context.Versions.JS1_1 || Version == Context.Versions.JS1_2));
            SetFeature (Features.ToStringAsSource, Version == Context.Versions.JS1_2);
            SetFeature (Features.ParentProtoProperties, true);
        }


        private static Context PrepareNewContext (AppDomain appDomain, ContextFactory factory)
        {
            Context cx = new Context (appDomain);
            if (cx.factory != null || cx.enterCount != 0) {
                throw new Exception ("factory.makeContext() returned Context instance already associated with some thread");
            }
            cx.factory = factory;
            factory.FireOnContextCreated (cx);
            if (factory.Sealed && !cx.Sealed) {
                cx.Seal ((object)null);
            }
            Thread.SetData (LocalSlot, cx);
            return cx;
        }

        private static void ReleaseContext (Context cx)
        {
            Thread.SetData (LocalSlot, null);
            try {
                cx.factory.FireOnContextReleased (cx);
            }
            finally {
                cx.factory = null;
            }
        }



        /// <summary> Seal this Context object so any attempt to modify any of its properties
        /// including calling {@link #enter()} and {@link #exit()} methods will
        /// throw an exception.
        /// <p>
        /// If <tt>sealKey</tt> is not null, calling
        /// {@link #unseal(Object sealKey)} with the same key unseals
        /// the object. If <tt>sealKey</tt> is null, unsealing is no longer possible.
        /// 
        /// </summary>
        public void Seal (object sealKey)
        {
            if (m_Sealed)
                OnSealedMutation ();
            m_Sealed = true;
            this.m_SealKey = sealKey;
        }

        /// <summary> Unseal previously sealed Context object.
        /// The <tt>sealKey</tt> argument should not be null and should match
        /// <tt>sealKey</tt> suplied with the last call to
        /// {@link #seal(Object)} or an exception will be thrown.
        /// 
        /// </summary>
        public void Unseal (object sealKey)
        {
            if (sealKey == null)
                throw new ArgumentException ();
            if (this.m_SealKey != sealKey)
                throw new ArgumentException ();
            if (!m_Sealed)
                throw new Exception ();
            m_Sealed = false;
            this.m_SealKey = null;
        }

        internal static void OnSealedMutation ()
        {
            throw new Exception ();
        }

        /// <summary> 
        /// Get the current language version.
        /// </summary>        
        public Versions Version
        {
            get
            {
                return m_Version;
            }
            set
            {
                if (m_Sealed)
                    OnSealedMutation ();
                this.m_Version = value;

                InitDefaultFeatures ();
            }
        }

        public static bool IsValidLanguageVersion (int version)
        {
            return ToValidLanguageVersion (version) != Versions.Unknown;
        }

        public static Versions ToValidLanguageVersion (int version)
        {
            Versions ver = Versions.Unknown;
            if (version > 0 || version < (int)Versions.JS1_6)
                ver = (Versions)version;
            return ver;
        }

        public static void CheckLanguageVersion (int version)
        {
            if (IsValidLanguageVersion (version)) {
                return;
            }
            throw new ArgumentException ("Bad language version: " + version);
        }

        /// <summary> Get the current error reporter.
        /// 
        /// </summary>
        public ErrorReporter ErrorReporter
        {
            get
            {
                if (m_ErrorReporter == null) {
                    return DefaultErrorReporter.instance;
                }
                return m_ErrorReporter;
            }
            set
            {
                if (m_Sealed)
                    OnSealedMutation ();
                if (value == null)
                    throw new ArgumentException ();
                this.m_ErrorReporter = value;
            }
        }

        /// <summary>
        /// Get the current locale.  Returns the default locale if none has
        /// been set.
        /// </summary>
        public CultureInfo CurrentCulture
        {
            get
            {
                if (culture == null)
                    culture = Thread.CurrentThread.CurrentCulture;
                return culture;
            }
            set
            {
                if (m_Sealed)
                    OnSealedMutation ();
                culture = value;

                // HACK: Needed for example on DateTime.ToString()
                if (Thread.CurrentThread.CurrentCulture != culture)
                    Thread.CurrentThread.CurrentCulture = culture;
            }
        }

        /// <summary> Report a warning using the error reporter for the current thread.
        /// 
        /// </summary>
        /// <param name="message">the warning message to report
        /// </param>
        /// <param name="sourceName">a string describing the source, such as a filename
        /// </param>
        /// <param name="lineno">the starting line number
        /// </param>
        /// <param name="lineSource">the text of the line (may be null)
        /// </param>
        /// <param name="lineOffset">the offset into lineSource where problem was detected
        /// </param>

        public static void ReportWarning (string message, string sourceName, int lineno, string lineSource, int lineOffset)
        {
            Context cx = Context.CurrentContext;
            cx.ErrorReporter.Warning (message, sourceName, lineno, lineSource, lineOffset);
        }

        public static void ReportWarningById (string messageId, params string [] arguments)
        {
            int [] linep = new int [] { 0 };
            string filename = GetSourcePositionFromStack (linep);
            Context.ReportWarning (ScriptRuntime.GetMessage (messageId, arguments), filename, linep [0], null, 0);
        }
        
        /// <summary> Report a warning using the error reporter for the current thread.
        /// 
        /// </summary>
        /// <param name="message">the warning message to report
        /// </param>
        public static void ReportWarning (string message)
        {
            int [] linep = new int [] { 0 };
            string filename = GetSourcePositionFromStack (linep);
            Context.ReportWarning (message, filename, linep [0], null, 0);
        }

        /// <summary> Report an error using the error reporter for the current thread.
        /// 
        /// </summary>
        /// <param name="message">the error message to report
        /// </param>
        /// <param name="sourceName">a string describing the source, such as a filename
        /// </param>
        /// <param name="lineno">the starting line number
        /// </param>
        /// <param name="lineSource">the text of the line (may be null)
        /// </param>
        /// <param name="lineOffset">the offset into lineSource where problem was detected
        /// </param>        
        public static void ReportError (string message, string sourceName, int lineno, string lineSource, int lineOffset)
        {
            Context cx = CurrentContext;
            if (cx != null) {
                cx.ErrorReporter.Error (message, sourceName, lineno, lineSource, lineOffset);
            }
            else {
                throw new EcmaScriptRuntimeException (message, sourceName, lineno, lineSource, lineOffset);
            }
        }

        /// <summary> Report an error using the error reporter for the current thread.
        /// 
        /// </summary>
        /// <param name="message">the error message to report
        /// </param>

        public static void ReportError (string message)
        {
            int [] linep = new int [] { 0 };
            string filename = GetSourcePositionFromStack (linep);
            Context.ReportError (message, filename, linep [0], null, 0);
        }

        /// <summary> Report a runtime error using the error reporter for the current thread.
        /// 
        /// </summary>
        /// <param name="message">the error message to report
        /// </param>
        /// <param name="sourceName">a string describing the source, such as a filename
        /// </param>
        /// <param name="lineno">the starting line number
        /// </param>
        /// <param name="lineSource">the text of the line (may be null)
        /// </param>
        /// <param name="lineOffset">the offset into lineSource where problem was detected
        /// </param>
        /// <returns> a runtime exception that will be thrown to terminate the
        /// execution of the script
        /// </returns>        
        public static EcmaScriptRuntimeException ReportRuntimeError (string message, string sourceName, int lineno, string lineSource, int lineOffset)
        {
            Context cx = CurrentContext;
            if (cx != null) {
                return cx.ErrorReporter.RuntimeError (message, sourceName, lineno, lineSource, lineOffset);
            }
            else {
                throw new EcmaScriptRuntimeException (message, sourceName, lineno, lineSource, lineOffset);
            }
        }


        internal static EcmaScriptRuntimeException ReportRuntimeErrorById (string messageId, params object [] args)
        {
            return ReportRuntimeError (ScriptRuntime.GetMessage (messageId, args));
        }

        /// <summary> Report a runtime error using the error reporter for the current thread.
        /// 
        /// </summary>
        /// <param name="message">the error message to report
        /// </param>        
        public static EcmaScriptRuntimeException ReportRuntimeError (string message)
        {
            int [] linep = new int [] { 0 };
            string filename = GetSourcePositionFromStack (linep);
            return Context.ReportRuntimeError (message, filename, linep [0], null, 0);
        }

        /// <summary> Initialize the standard objects.
        /// 
        /// Creates instances of the standard objects and their constructors
        /// (Object, String, Number, Date, etc.), setting up 'scope' to act
        /// as a global object as in ECMA 15.1.<p>
        /// 
        /// This method must be called to initialize a scope before scripts
        /// can be evaluated in that scope.<p>
        /// 
        /// This method does not affect the Context it is called upon.
        /// 
        /// </summary>
        /// <returns> the initialized scope
        /// </returns>
        public ScriptableObject InitStandardObjects ()
        {
            return InitStandardObjects (null, false);
        }

        /// <summary> Initialize the standard objects.
        /// 
        /// Creates instances of the standard objects and their constructors
        /// (Object, String, Number, Date, etc.), setting up 'scope' to act
        /// as a global object as in ECMA 15.1.<p>
        /// 
        /// This method must be called to initialize a scope before scripts
        /// can be evaluated in that scope.<p>
        /// 
        /// This method does not affect the Context it is called upon.
        /// 
        /// </summary>
        /// <param name="scope">the scope to initialize, or null, in which case a new
        /// object will be created to serve as the scope
        /// </param>
        /// <returns> the initialized scope. The method returns the value of the scope
        /// argument if it is not null or newly allocated scope object which
        /// is an instance {@link ScriptableObject}.
        /// </returns>
        public IScriptable InitStandardObjects (ScriptableObject scope)
        {
            return InitStandardObjects (scope, false);
        }

        /// <summary> Initialize the standard objects.
        /// 
        /// Creates instances of the standard objects and their constructors
        /// (Object, String, Number, Date, etc.), setting up 'scope' to act
        /// as a global object as in ECMA 15.1.<p>
        /// 
        /// This method must be called to initialize a scope before scripts
        /// can be evaluated in that scope.<p>
        /// 
        /// This method does not affect the Context it is called upon.<p>
        /// 
        /// This form of the method also allows for creating "sealed" standard
        /// objects. An object that is sealed cannot have properties added, changed,
        /// or removed. This is useful to create a "superglobal" that can be shared
        /// among several top-level objects. Note that sealing is not allowed in
        /// the current ECMA/ISO language specification, but is likely for
        /// the next version.
        /// 
        /// </summary>
        /// <param name="scope">the scope to initialize, or null, in which case a new
        /// object will be created to serve as the scope
        /// </param>
        /// <param name="sealed">whether or not to create sealed standard objects that
        /// cannot be modified.
        /// </param>
        /// <returns> the initialized scope. The method returns the value of the scope
        /// argument if it is not null or newly allocated scope object.
        /// </returns>        
        public ScriptableObject InitStandardObjects (ScriptableObject scope, bool zealed)
        {
            return ScriptRuntime.InitStandardObjects (this, scope, zealed);
        }

        /// <summary> Evaluate a JavaScript source string.
        /// 
        /// The provided source name and line number are used for error messages
        /// and for producing debug information.
        /// 
        /// </summary>
        /// <param name="scope">the scope to execute in
        /// </param>
        /// <param name="source">the JavaScript source
        /// </param>
        /// <param name="sourceName">a string describing the source, such as a filename
        /// </param>
        /// <param name="lineno">the starting line number
        /// </param>
        /// <param name="securityDomain">an arbitrary object that specifies security
        /// information about the origin or owner of the script. For
        /// implementations that don't care about security, this value
        /// may be null.
        /// </param>
        /// <returns> the result of evaluating the string
        /// </returns>        
        public object EvaluateString (IScriptable scope, string source, string sourceName, int lineno, object securityDomain)
        {
            IScript script = CompileString (source, sourceName, lineno, securityDomain);
            if (script != null) {
                return script.Exec (this, scope);
            }
            else {
                return null;
            }
        }

        /// <summary> Evaluate a reader as JavaScript source.
        /// 
        /// All characters of the reader are consumed.
        /// 
        /// </summary>
        /// <param name="scope">the scope to execute in
        /// </param>
        /// <param name="in">the Reader to get JavaScript source from
        /// </param>
        /// <param name="sourceName">a string describing the source, such as a filename
        /// </param>
        /// <param name="lineno">the starting line number
        /// </param>
        /// <param name="securityDomain">an arbitrary object that specifies security
        /// information about the origin or owner of the script. For
        /// implementations that don't care about security, this value
        /// may be null.
        /// </param>
        /// <returns> the result of evaluating the source
        /// 
        /// </returns>
        /// <exception cref=""> IOException if an IOException was generated by the Reader
        /// </exception>
        public object EvaluateReader (IScriptable scope, StreamReader sr, string sourceName, int lineno, object securityDomain)
        {
            IScript script = CompileReader (sr, sourceName, lineno, securityDomain);

            if (script != null) {
                return script.Exec (this, scope);
            }
            return null;
        }

        /// <summary> Check whether a string is ready to be compiled.
        /// <p>
        /// stringIsCompilableUnit is intended to support interactive compilation of
        /// javascript.  If compiling the string would result in an error
        /// that might be fixed by appending more source, this method
        /// returns false.  In every other case, it returns true.
        /// <p>
        /// Interactive shells may accumulate source lines, using this
        /// method after each new line is appended to check whether the
        /// statement being entered is complete.
        /// 
        /// </summary>
        /// <param name="source">the source buffer to check
        /// </param>
        /// <returns> whether the source is ready for compilation
        /// </returns>
        public ScriptOrFnNode IsCompilableUnit (string source)
        {
            ScriptOrFnNode ret = null;
            bool errorseen = false;
            CompilerEnvirons compilerEnv = new CompilerEnvirons ();
            compilerEnv.initFromContext (this);
            // no source name or source text manager, because we're just
            // going to throw away the result.
            compilerEnv.setGeneratingSource (false);
            Parser p = new Parser (compilerEnv, DefaultErrorReporter.instance);
            try {
                ret = p.Parse (source, null, 1);
            }
            catch (EcmaScriptRuntimeException) {
                errorseen = true;
            }
            // Return false only if an error occurred as a result of reading past
            // the end of the file, i.e. if the source could be fixed by
            // appending more source.
            if (!(errorseen && p.Eof))
                return ret;
            return null;
        }


        /// <summary> Compiles the source in the given reader.
        /// <p>
        /// Returns a script that may later be executed.
        /// Will consume all the source in the reader.
        /// 
        /// </summary>
        /// <param name="in">the input reader
        /// </param>
        /// <param name="sourceName">a string describing the source, such as a filename
        /// </param>
        /// <param name="lineno">the starting line number for reporting errors
        /// </param>
        /// <param name="securityDomain">an arbitrary object that specifies security
        /// information about the origin or owner of the script. For
        /// implementations that don't care about security, this value
        /// may be null.
        /// </param>
        /// <returns> a script that may later be executed
        /// </returns>
        /// <exception cref=""> IOException if an IOException was generated by the Reader
        /// </exception>        
        public IScript CompileReader (StreamReader sr, string sourceName, int lineno, object securityDomain)
        {
            if (lineno < 0)
                throw new ArgumentException ("lineno may not be negative", "lineno");
            return (IScript)CompileImpl (null, sr, null, sourceName, lineno, securityDomain, false, null, null);
        }

        /// <summary> Compiles the source in the given string.
        /// <p>
        /// Returns a script that may later be executed.
        /// 
        /// </summary>
        /// <param name="source">the source string
        /// </param>
        /// <param name="sourceName">a string describing the source, such as a filename
        /// </param>
        /// <param name="lineno">the starting line number for reporting errors
        /// </param>
        /// <param name="securityDomain">an arbitrary object that specifies security
        /// information about the origin or owner of the script. For
        /// implementations that don't care about security, this value
        /// may be null.
        /// </param>
        /// <returns> a script that may later be executed
        /// </returns>        
        public IScript CompileString (string source, string sourceName, int lineno, object securityDomain)
        {
            if (lineno < 0) {
                // For compatibility IllegalArgumentException can not be thrown here
                lineno = 0;
            }
            return CompileString (source, null, null, sourceName, lineno, securityDomain);
        }

        internal IScript CompileString (string source, Interpreter compiler, ErrorReporter compilationErrorReporter, string sourceName, int lineno, object securityDomain)
        {
            return (IScript)CompileImpl (null, null, source, sourceName, lineno, securityDomain, false, compiler, compilationErrorReporter);
        }

        /// <summary> Compile a JavaScript function.
        /// <p>
        /// The function source must be a function definition as defined by
        /// ECMA (e.g., "function f(a) { return a; }").
        /// 
        /// </summary>
        /// <param name="scope">the scope to compile relative to
        /// </param>
        /// <param name="source">the function definition source
        /// </param>
        /// <param name="sourceName">a string describing the source, such as a filename
        /// </param>
        /// <param name="lineno">the starting line number
        /// </param>
        /// <param name="securityDomain">an arbitrary object that specifies security
        /// information about the origin or owner of the script. For
        /// implementations that don't care about security, this value
        /// may be null.
        /// </param>
        /// <returns> a Function that may later be called
        /// </returns>        
        public IFunction CompileFunction (IScriptable scope, string source, string sourceName, int lineno, object securityDomain)
        {
            return CompileFunction (scope, source, null, null, sourceName, lineno, securityDomain);
        }

        internal IFunction CompileFunction (IScriptable scope, string source, Interpreter compiler, ErrorReporter compilationErrorReporter, string sourceName, int lineno, object securityDomain)
        {
            return (IFunction)CompileImpl (scope, null, source, sourceName, lineno, securityDomain, true, compiler, compilationErrorReporter);
        }

        /// <summary> Decompile the script.
        /// <p>
        /// The canonical source of the script is returned.
        /// 
        /// </summary>
        /// <param name="script">the script to decompile
        /// </param>
        /// <param name="indent">the number of spaces to indent the result
        /// </param>
        /// <returns> a string representing the script source
        /// </returns>
        public string DecompileScript (IScript script, int indent)
        {
            BuiltinFunction scriptImpl = (BuiltinFunction)script;
            return scriptImpl.Decompile (indent, 0);
        }

        /// <summary> Decompile a JavaScript Function.
        /// <p>
        /// Decompiles a previously compiled JavaScript function object to
        /// canonical source.
        /// <p>
        /// Returns function body of '[native code]' if no decompilation
        /// information is available.
        /// 
        /// </summary>
        /// <param name="fun">the JavaScript function to decompile
        /// </param>
        /// <param name="indent">the number of spaces to indent the result
        /// </param>
        /// <returns> a string representing the function source
        /// </returns>
        public string DecompileFunction (IFunction fun, int indent)
        {
            if (fun is BaseFunction)
                return ((BaseFunction)fun).Decompile (indent, 0);
            else
                return "function " + fun.ClassName + "() {\n\t[native code]\n}\n";
        }

        /// <summary> Decompile the body of a JavaScript Function.
        /// <p>
        /// Decompiles the body a previously compiled JavaScript Function
        /// object to canonical source, omitting the function header and
        /// trailing brace.
        /// 
        /// Returns '[native code]' if no decompilation information is available.
        /// 
        /// </summary>
        /// <param name="fun">the JavaScript function to decompile
        /// </param>
        /// <param name="indent">the number of spaces to indent the result
        /// </param>
        /// <returns> a string representing the function body source.
        /// </returns>
        public string DecompileFunctionBody (IFunction fun, int indent)
        {
            if (fun is BaseFunction) {
                BaseFunction bf = (BaseFunction)fun;
                return bf.Decompile (indent, Decompiler.ONLY_BODY_FLAG);
            }
            // ALERT: not sure what the right response here is.
            return "[native code]\n";
        }

        /// <summary> Create a new JavaScript object.
        /// 
        /// Equivalent to evaluating "new Object()".
        /// </summary>
        /// <param name="scope">the scope to search for the constructor and to evaluate
        /// against
        /// </param>
        /// <returns> the new object
        /// </returns>
        public IScriptable NewObject (IScriptable scope)
        {
            return NewObject (scope, "Object", ScriptRuntime.EmptyArgs);
        }

        /// <summary> Create a new JavaScript object by executing the named constructor.
        /// 
        /// The call <code>newObject(scope, "Foo")</code> is equivalent to
        /// evaluating "new Foo()".
        /// 
        /// </summary>
        /// <param name="scope">the scope to search for the constructor and to evaluate against
        /// </param>
        /// <param name="constructorName">the name of the constructor to call
        /// </param>
        /// <returns> the new object
        /// </returns>
        public IScriptable NewObject (IScriptable scope, string constructorName)
        {
            return NewObject (scope, constructorName, ScriptRuntime.EmptyArgs);
        }

        /// <summary> Creates a new JavaScript object by executing the named constructor.
        /// 
        /// Searches <code>scope</code> for the named constructor, calls it with
        /// the given arguments, and returns the result.<p>
        /// 
        /// The code
        /// <pre>
        /// Object[] args = { "a", "b" };
        /// newObject(scope, "Foo", args)</pre>
        /// is equivalent to evaluating "new Foo('a', 'b')", assuming that the Foo
        /// constructor has been defined in <code>scope</code>.
        /// 
        /// </summary>
        /// <param name="scope">The scope to search for the constructor and to evaluate
        /// against
        /// </param>
        /// <param name="constructorName">the name of the constructor to call
        /// </param>
        /// <param name="args">the array of arguments for the constructor
        /// </param>
        /// <returns> the new object
        /// </returns>
        public IScriptable NewObject (IScriptable scope, string constructorName, object [] args)
        {
            scope = ScriptableObject.GetTopLevelScope (scope);
            IFunction ctor = ScriptRuntime.getExistingCtor (this, scope, constructorName);
            if (args == null) {
                args = ScriptRuntime.EmptyArgs;
            }
            return ctor.Construct (this, scope, args);
        }

        /// <summary> Create an array with a specified initial length.
        /// <p>
        /// </summary>
        /// <param name="scope">the scope to create the object in
        /// </param>
        /// <param name="length">the initial length (JavaScript arrays may have
        /// additional properties added dynamically).
        /// </param>
        /// <returns> the new array object
        /// </returns>
        public IScriptable NewArray (IScriptable scope, int length)
        {
            BuiltinArray result = new BuiltinArray (length);
            ScriptRuntime.setObjectProtoAndParent (result, scope);
            return result;
        }

        /// <summary> Create an array with a set of initial elements.
        /// 
        /// </summary>
        /// <param name="scope">the scope to create the object in.
        /// </param>
        /// <param name="elements">the initial elements. Each object in this array
        /// must be an acceptable JavaScript type and type
        /// of array should be exactly Object[], not
        /// SomeObjectSubclass[].
        /// </param>
        /// <returns> the new array object.
        /// </returns>
        public IScriptable NewArray (IScriptable scope, object [] elements)
        {
            Type elementType = elements.GetType ().GetElementType ();
            if (elementType != typeof (object))
                throw new ArgumentException ();
            BuiltinArray result = new BuiltinArray (elements);
            ScriptRuntime.setObjectProtoAndParent (result, scope);
            return result;
        }

        /// <summary> Get the elements of a JavaScript array.
        /// <p>
        /// If the object defines a length property convertible to double number,
        /// then the number is converted Uint32 value as defined in Ecma 9.6
        /// and Java array of that size is allocated.
        /// The array is initialized with the values obtained by
        /// calling get() on object for each value of i in [0,length-1]. If
        /// there is not a defined value for a property the Undefined value
        /// is used to initialize the corresponding element in the array. The
        /// Java array is then returned.
        /// If the object doesn't define a length property or it is not a number,
        /// empty array is returned.
        /// </summary>
        /// <param name="object">the JavaScript array or array-like object
        /// </param>
        /// <returns> a Java array of objects
        /// </returns>

        public object [] GetElements (IScriptable obj)
        {
            return ScriptRuntime.getArrayElements (obj);
        }


        /// <summary> Convenient method to convert java value to its closest representation
        /// in JavaScript.
        /// <p>
        /// If value is an instance of String, Number, Boolean, Function or
        /// Scriptable, it is returned as it and will be treated as the corresponding
        /// JavaScript type of string, number, boolean, function and object.
        /// <p>
        /// Note that for Number instances during any arithmetic operation in
        /// JavaScript the engine will always use the result of
        /// <tt>Number.doubleValue()</tt> resulting in a precision loss if
        /// the number can not fit into double.
        /// <p>
        /// If value is an instance of Character, it will be converted to string of
        /// length 1 and its JavaScript type will be string.
        /// <p>
        /// The rest of values will be wrapped as LiveConnect objects
        /// by calling {@link WrapFactory#wrap(Context cx, Scriptable scope,
        /// Object obj, Class staticType)} as in:
        /// <pre>
        /// Context cx = Context.getCurrentContext();
        /// return cx.getWrapFactory().wrap(cx, scope, value, null);
        /// </pre>
        /// 
        /// </summary>
        /// <param name="value">any Java object
        /// </param>
        /// <param name="scope">top scope object
        /// </param>
        /// <returns> value suitable to pass to any API that takes JavaScript values.
        /// </returns>
        public static object CliToJS (Context cx, object value, IScriptable scope)
        {
            if (value is string || CliHelper.IsNumber (value) || value is bool || value is IScriptable) {
                return value;
            }
            else if (value is char) {
                return Convert.ToString (((char)value));
            }
            else {
                Type type = (value as Type);
                if (type !=  null) {                                
                    return cx.Wrap (scope, value, (Type)value);
                } else {                   
                    return cx.Wrap (scope, value, null);                                
                }
            }
        }

        /// <summary> Convert a JavaScript value into the desired type.
        /// Uses the semantics defined with LiveConnect3 and throws an
        /// Illegal argument exception if the conversion cannot be performed.
        /// </summary>
        /// <param name="value">the JavaScript value to convert
        /// </param>
        /// <param name="desiredType">the Java type to convert to. Primitive Java
        /// types are represented using the TYPE fields in the corresponding
        /// wrapper class in java.lang.
        /// </param>
        /// <returns> the converted value
        /// </returns>
        /// <throws>  EvaluatorException if the conversion cannot be performed </throws>
        public static object JsToCli (object value, System.Type desiredType)
        {
            return CliObject.CoerceType (desiredType, value);
        }



        /// <summary> Rethrow the exception wrapping it as the script runtime exception.
        /// Unless the exception is instance of {@link EcmaError} or
        /// {@link EvaluatorException} it will be wrapped as
        /// {@link WrappedException}, a subclass of {@link EvaluatorException}.
        /// The resulting exception object always contains
        /// source name and line number of script that triggered exception.
        /// <p>
        /// This method always throws an exception, its return value is provided
        /// only for convenience to allow a usage like:
        /// <pre>
        /// throw Context.throwAsScriptRuntimeEx(ex);
        /// </pre>
        /// to indicate that code after the method is unreachable.
        /// </summary>
        /// <throws>  EvaluatorException </throws>
        /// <throws>  EcmaError </throws>		
        public static Exception ThrowAsScriptRuntimeEx (Exception e)
        {
            while ((e is TargetInvocationException)) {
                e = ((TargetInvocationException)e).InnerException;
            }
            if (e is EcmaScriptException) {
                throw e;
            }
            throw new EcmaScriptRuntimeException (e);
        }

        public static bool IsValidOptimizationLevel (int optimizationLevel)
        {
            return -1 <= optimizationLevel && optimizationLevel <= 9;
        }

        public static void CheckOptimizationLevel (int optimizationLevel)
        {
            if (IsValidOptimizationLevel (optimizationLevel)) {
                return;
            }
            throw new ArgumentException ("Optimization level outside [-1..9]: " + optimizationLevel);
        }

        /// <summary> Set the security controller for this context.
        /// <p> SecurityController may only be set if it is currently null
        /// and {@link SecurityController#hasGlobal()} is <tt>false</tt>.
        /// Otherwise a SecurityException is thrown.
        /// </summary>
        /// <param name="controller">a SecurityController object
        /// </param>
        /// <throws>  SecurityException if there is already a SecurityController </throws>
        /// <summary>         object for this Context or globally installed.
        /// </summary>        
        public SecurityController SecurityController
        {
            set
            {
                if (m_Sealed)
                    OnSealedMutation ();
                if (value == null)
                    throw new ArgumentException ();
                if (securityController != null) {
                    throw new System.Security.SecurityException ("Can not overwrite existing SecurityController object");
                }
                if (SecurityController.HasGlobal ()) {
                    throw new System.Security.SecurityException ("Can not overwrite existing global SecurityController object");
                }
                securityController = value;
            }
            get
            {
                SecurityController global = SecurityController.Global;
                if (global != null) {
                    return global;
                }
                return securityController;
            }
        }

        /// <summary> Get a value corresponding to a key.
        /// <p>
        /// Since the Context is associated with a thread it can be
        /// used to maintain values that can be later retrieved using
        /// the current thread.
        /// <p>
        /// Note that the values are maintained with the Context, so
        /// if the Context is disassociated from the thread the values
        /// cannot be retreived. Also, if private data is to be maintained
        /// in this manner the key should be a java.lang.Object
        /// whose reference is not divulged to untrusted code.
        /// </summary>
        /// <param name="key">the key used to lookup the value
        /// </param>
        /// <returns> a value previously stored using putThreadLocal.
        /// </returns>
        public object GetThreadLocal (object key)
        {
            if (hashtable == null)
                return null;
            return hashtable [key];
        }

        /// <summary> Put a value that can later be retrieved using a given key.
        /// <p>
        /// </summary>
        /// <param name="key">the key used to index the value
        /// </param>
        /// <param name="value">the value to save
        /// </param>
        public void PutThreadLocal (object key, object value)
        {
            if (m_Sealed)
                OnSealedMutation ();
            if (hashtable == null)
                hashtable = Hashtable.Synchronized (new Hashtable ());
            hashtable [key] = value;
        }

        /// <summary> Remove values from thread-local storage.</summary>
        /// <param name="key">the key for the entry to remove.
        /// </param>        
        public void RemoveThreadLocal (object key)
        {
            if (m_Sealed)
                OnSealedMutation ();
            if (hashtable == null)
                return;
            hashtable.Remove (key);
        }



        /// <summary> Return the current debugger.</summary>
        /// <returns> the debugger, or null if none is attached.
        /// </returns>
        public Debugger Debugger
        {
            get
            {
                return m_Debugger;
            }
        }

        /// <summary> Set the associated debugger.</summary>
        /// <param name="debugger">the debugger to be used on callbacks from
        /// the engine.
        /// </param>
        /// <param name="contextData">arbitrary object that debugger can use to store
        /// per Context data.
        /// </param>
        public void SetDebugger (Debugger debugger, object contextData)
        {
            if (m_Sealed)
                OnSealedMutation ();
            this.m_Debugger = debugger;
            debuggerData = contextData;
        }

        /// <summary> Return DebuggableScript instance if any associated with the script.
        /// If callable supports DebuggableScript implementation, the method
        /// returns it. Otherwise null is returned.
        /// </summary>
        public static DebuggableScript getDebuggableView (IScript script)
        {
            if (script is BuiltinFunction) {
                return ((BuiltinFunction)script).DebuggableView;
            }
            return null;
        }


        private Features m_Features = Features.None;

        /// <summary>
        /// Controls certain aspects of script semantics.
        /// Should be overwritten to alter default behavior.
        /// <remarks>
        /// The default implementation calls
        /// {@link ContextFactory#hasFeature(Context cx, int featureIndex)}
        /// that allows to customize Context behavior without introducing
        /// Context subclasses.  {@link ContextFactory} documentation gives
        /// an example of hasFeature implementation.
        /// </remarks>
        /// </summary>
        /// <param name="featureIndex">feature to check</param>
        /// <returns>
        /// true if the <code>feature</code> feature is turned on
        /// </returns>  
        public bool HasFeature (Features feature)
        {
            return (m_Features & feature) == feature;
        }

        public void SetFeature (Features feature, bool isEnabled)
        {
            if (isEnabled) {
                m_Features |= feature;
            }
            else {
                if (HasFeature (feature))
                    m_Features = m_Features ^ feature;
            }
        }


        /// <summary> Allow application to monitor counter of executed script instructions
        /// in Context subclasses.
        /// Run-time calls this when instruction counting is enabled and the counter
        /// reaches limit set by <code>setInstructionObserverThreshold()</code>.
        /// The method is useful to observe long running scripts and if necessary
        /// to terminate them.
        /// <p>
        /// The instruction counting support is available only for interpreted
        /// scripts generated when the optimization level is set to -1.
        /// <p>
        /// The default implementation calls
        /// {@link ContextFactory#observeInstructionCount(Context cx,
        /// int instructionCount)}
        /// that allows to customize Context behavior without introducing
        /// Context subclasses.
        /// 
        /// </summary>
        /// <param name="instructionCount">amount of script instruction executed since
        /// last call to <code>observeInstructionCount</code>
        /// </param>
        /// <throws>  Error to terminate the script </throws>        
        protected internal void ObserveInstructionCount (int instructionCount)
        {
            Factory.ObserveInstructionCount (this, instructionCount);
        }

        private object CompileImpl (IScriptable scope, StreamReader sourceReader, string sourceString, string sourceName, int lineno, object securityDomain, bool returnFunction, Interpreter compiler, ErrorReporter compilationErrorReporter)
        {
            if (securityDomain != null && securityController == null) {
                throw new ArgumentException ("securityDomain should be null if setSecurityController() was never called");
            }

            // One of sourceReader or sourceString has to be null
            if (!(sourceReader == null ^ sourceString == null))
                Context.CodeBug ();
            // scope should be given if and only if compiling function
            if (!(scope == null ^ returnFunction))
                Context.CodeBug ();

            CompilerEnvirons compilerEnv = new CompilerEnvirons ();
            compilerEnv.initFromContext (this);
            if (compilationErrorReporter == null) {
                compilationErrorReporter = compilerEnv.getErrorReporter ();
            }

            if (m_Debugger != null) {
                if (sourceReader != null) {
                    sourceString = sourceReader.ReadToEnd ();
                    sourceReader = null;
                }
            }

            Parser p = new Parser (compilerEnv, compilationErrorReporter);
            if (returnFunction) {
                p.calledByCompileFunction = true;
            }
            ScriptOrFnNode tree;
            if (sourceString != null) {
                tree = p.Parse (sourceString, sourceName, lineno);
            }
            else {
                tree = p.Parse (sourceReader, sourceName, lineno);
            }
            if (returnFunction) {
                if (!(tree.FunctionCount == 1 && tree.FirstChild != null && tree.FirstChild.Type == Token.FUNCTION)) {
                    // TODO: the check just look for the first child
                    // TODO: and allows for more nodes after it for compatibility
                    // TODO: with sources like function() {};;;
                    throw new ArgumentException ("compileFunction only accepts source with single JS function: " + sourceString);
                }
            }



            if (compiler == null) {
                compiler = new Interpreter ();
                //compiler = new Compiler();
            }

            string encodedSource = p.EncodedSource;

            object bytecode = compiler.Compile (compilerEnv, tree, encodedSource, returnFunction);

            if (m_Debugger != null) {
                if (sourceString == null)
                    Context.CodeBug ();
                if (bytecode is DebuggableScript) {
                    DebuggableScript dscript = (DebuggableScript)bytecode;
                    NotifyDebugger (this, dscript, sourceString);
                }
                else {
                    throw new Exception ("NOT SUPPORTED");
                }
            }

            object result;
            if (returnFunction) {
                result = compiler.CreateFunctionObject (this, scope, bytecode, securityDomain);
            }
            else {
                result = compiler.CreateScriptObject (bytecode, securityDomain);
            }

            return result;
        }

        private static void NotifyDebugger (Context cx, DebuggableScript dscript, string debugSource)
        {
            cx.m_Debugger.HandleCompilationDone (cx, dscript, debugSource);
            for (int i = 0; i != dscript.FunctionCount; ++i) {
                NotifyDebugger (cx, dscript.GetFunction (i), debugSource);
            }
        }

        internal static string GetSourcePositionFromStack (int [] linep)
        {
            Context cx = CurrentContext;
            if (cx == null)
                return null;
            if (cx.lastInterpreterFrame != null) {
                return Interpreter.GetSourcePositionFromStack (cx, linep);
            }

            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace ();
            System.Diagnostics.StackFrame sf = st.GetFrame (0);
            linep [0] = sf.GetFileLineNumber ();
            return Path.GetFileName (sf.GetFileName ());
        }


        /// <summary> Add a name to the list of names forcing the creation of real
        /// activation objects for functions.
        /// 
        /// </summary>
        /// <param name="name">the name of the object to add to the list
        /// </param>
        public void AddActivationName (string name)
        {
            if (m_Sealed)
                OnSealedMutation ();
            if (activationNames == null)
                activationNames = Hashtable.Synchronized (new Hashtable (5));
            activationNames [name] = name;
        }

        /// <summary> Check whether the name is in the list of names of objects
        /// forcing the creation of activation objects.
        /// 
        /// </summary>
        /// <param name="name">the name of the object to test
        /// 
        /// </param>
        /// <returns> true if an function activation object is needed.
        /// </returns>
        public bool IsActivationNeeded (string name)
        {
            return activationNames != null && activationNames.ContainsKey (name);
        }

        /// <summary> Remove a name from the list of names forcing the creation of real
        /// activation objects for functions.
        /// 
        /// </summary>
        /// <param name="name">the name of the object to remove from the list
        /// </param>
        public void RemoveActivationName (string name)
        {
            if (m_Sealed)
                OnSealedMutation ();
            if (activationNames != null)
                activationNames.Remove (name);
        }

        private static string implementationVersion;

        private ContextFactory factory;
        private bool m_Sealed;
        private object m_SealKey;

        internal IScriptable topCallScope;
        internal BuiltinCall currentActivationCall;
        internal XMLLib cachedXMLLib;

        // for Objects, Arrays to tag themselves as being printed out,
        // so they don't print themselves out recursively.
        // Use ObjToIntMap instead of java.util.HashSet for JDK 1.1 compatibility
        internal ObjToIntMap iterating;

        internal object interpreterSecurityDomain;

        internal Versions m_Version = Versions.Unknown;

        private SecurityController securityController;

        private ErrorReporter m_ErrorReporter;
        internal RegExpProxy regExpProxy;
        private System.Globalization.CultureInfo culture;
        private bool generatingDebug;
        private bool generatingDebugChanged;
        private bool generatingSource = true;
        internal bool compileFunctionsWithDynamicScopeFlag = false;
        internal bool useDynamicScope;
        private int m_OptimizationLevel;

        internal Debugger m_Debugger;
        private object debuggerData;
        private int enterCount;
        private Hashtable hashtable;

        private bool creationEventWasSent;

        /// <summary> This is the list of names of objects forcing the creation of
        /// function activation records.
        /// </summary>
        internal Hashtable activationNames;

        // For the interpreter to store the last frame for error reports etc.
        internal object lastInterpreterFrame;

        // For the interpreter to store information about previous invocations
        // interpreter invocations
        internal ObjArray previousInterpreterInvocations;

        // For instruction counting (interpreter only)
        internal int instructionCount;
        internal int instructionThreshold;

        // It can be used to return the second index-like result from function
        internal int scratchIndex;

        // It can be used to return the second uint32 result from function
        internal long scratchUint32;

        // It can be used to return the second Scriptable result from function
        internal IScriptable scratchScriptable;
        static Context ()
        {
            EmptyArgs = ScriptRuntime.EmptyArgs;
        }

        public void Dispose ()
        {
            Context.Exit ();
        }

        /// <summary>
        /// Throws RuntimeException to indicate failed assertion.
        /// The function never returns and its return type is RuntimeException
        /// only to be able to write <tt>throw EcmaScriptHelper.CodeBug()</tt> if plain
        /// <tt>EcmaScriptHelper.CodeBug()</tt> triggers unreachable code error.
        /// </summary>
        public static Exception CodeBug ()
        {
            Exception ex = new Exception ("FAILED ASSERTION");
            Console.Error.WriteLine (ex.ToString ());
            throw ex;
        }
        
    }
}
