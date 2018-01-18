//------------------------------------------------------------------------------
// <license file="IdFunctionCall.cs">
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

    /// <summary> 
    /// Master for id-based functions that knows their properties and how to
    /// execute them.
    /// </summary>
    public interface IIdFunctionCall
    {

        /// <summary> 
        /// 'thisObj' will be null if invoked as constructor, in which case
        /// instance of Scriptable should be returned
        /// </summary>
        object ExecIdCall (IdFunctionObject f, Context cx, IScriptable scope, IScriptable thisObj, object [] args);

    }

}