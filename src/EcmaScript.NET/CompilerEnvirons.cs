//------------------------------------------------------------------------------
// <license file="CompilerEnvirons.cs">
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
using System.Collections;

namespace EcmaScript.NET
{

    public class CompilerEnvirons
    {
        virtual public bool UseDynamicScope
        {
            get
            {
                return useDynamicScope;
            }

        }
        public CompilerEnvirons ()
        {
            errorReporter = DefaultErrorReporter.instance;
            languageVersion = Context.Versions.Default;
            generateDebugInfo = true;
            useDynamicScope = false;
            reservedKeywordAsIdentifier = false;
            allowMemberExprAsFunctionName = false;
            xmlAvailable = true;
            optimizationLevel = 0;
            generatingSource = true;
        }

        public virtual void initFromContext (Context cx)
        {
            setErrorReporter (cx.ErrorReporter);
            this.languageVersion = cx.Version;
            useDynamicScope = cx.compileFunctionsWithDynamicScopeFlag;
            generateDebugInfo = (!cx.GeneratingDebugChanged || cx.GeneratingDebug);
            reservedKeywordAsIdentifier = cx.HasFeature (Context.Features.ReservedKeywordAsIdentifier);
            allowMemberExprAsFunctionName = cx.HasFeature (Context.Features.MemberExprAsFunctionName);
            xmlAvailable = cx.HasFeature (Context.Features.E4x);
            getterAndSetterSupport = cx.HasFeature (Context.Features.GetterAndSetter);

            optimizationLevel = cx.OptimizationLevel;

            generatingSource = cx.GeneratingSource;
            activationNames = cx.activationNames;
        }

        public ErrorReporter getErrorReporter ()
        {
            return errorReporter;
        }

        public virtual void setErrorReporter (ErrorReporter errorReporter)
        {
            if (errorReporter == null)
                throw new ArgumentException ();
            this.errorReporter = errorReporter;
        }

        public Context.Versions LanguageVersion
        {
            get
            {
                return languageVersion;
            }
            set
            {
                languageVersion = value;
            }
        }

        public bool isGenerateDebugInfo ()
        {
            return generateDebugInfo;
        }

        public virtual void setGenerateDebugInfo (bool flag)
        {
            this.generateDebugInfo = flag;
        }

        public bool isReservedKeywordAsIdentifier ()
        {
            return reservedKeywordAsIdentifier;
        }

        public virtual void setReservedKeywordAsIdentifier (bool flag)
        {
            reservedKeywordAsIdentifier = flag;
        }

        public bool isAllowMemberExprAsFunctionName ()
        {
            return allowMemberExprAsFunctionName;
        }

        public virtual void setAllowMemberExprAsFunctionName (bool flag)
        {
            allowMemberExprAsFunctionName = flag;
        }

        public bool isXmlAvailable ()
        {
            return xmlAvailable;
        }

        public virtual void setXmlAvailable (bool flag)
        {
            xmlAvailable = flag;
        }

        public int getOptimizationLevel ()
        {
            return optimizationLevel;
        }

        public virtual void setOptimizationLevel (int level)
        {
            Context.CheckOptimizationLevel (level);
            this.optimizationLevel = level;
        }

        public bool isGeneratingSource ()
        {
            return generatingSource;
        }

        /// <summary> Specify whether or not source information should be generated.
        /// <p>
        /// Without source information, evaluating the "toString" method
        /// on JavaScript functions produces only "[native code]" for
        /// the body of the function.
        /// Note that code generated without source is not fully ECMA
        /// conformant.
        /// </summary>
        public virtual void setGeneratingSource (bool generatingSource)
        {
            this.generatingSource = generatingSource;
        }

        private ErrorReporter errorReporter;

        private Context.Versions languageVersion;
        private bool generateDebugInfo;
        private bool useDynamicScope;
        private bool reservedKeywordAsIdentifier;
        private bool allowMemberExprAsFunctionName;
        private bool xmlAvailable;
        private int optimizationLevel;
        private bool generatingSource;
        internal Hashtable activationNames;
        internal bool getterAndSetterSupport;
    }
}