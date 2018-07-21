//------------------------------------------------------------------------------
// <license file="RegExpProxy.cs">
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


    public enum RegExpActions
    {        
        None = 0,
        Match = 1,
        Replace = 2,
        Search = 3
    }
    
    /// <summary>
    /// A proxy for the regexp package, so that the regexp package can be
    /// loaded optionally.
    /// </summary>    
    public interface RegExpProxy
    {

        bool IsRegExp (IScriptable obj);

        object Compile (Context cx, string source, string flags);

        IScriptable Wrap (Context cx, IScriptable scope, object compiled);

        object Perform (Context cx, IScriptable scope, IScriptable thisObj, object [] args, RegExpActions actionType);

        int FindSplit (Context cx, IScriptable scope, string target, string separator, IScriptable re, int [] ip, int [] matchlen, bool [] matched, string [] [] parensp);
        
    }
}