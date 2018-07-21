//------------------------------------------------------------------------------
// <license file="Ref.cs">
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

    /// <summary> Generic notion of reference object that know how to query/modify the
    /// target objects based on some property/index.
    /// </summary>

    public interface IRef
    {
        bool Has (Context cx);        

        object Get (Context cx);

        object Set (Context cx, object value);

        bool Delete (Context cx);
        
	}
}