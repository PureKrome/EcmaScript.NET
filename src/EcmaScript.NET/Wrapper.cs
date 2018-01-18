//------------------------------------------------------------------------------
// <license file="Wrapper.cs">
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
    /// Objects that can wrap other values for reflection in the JS environment
    /// will implement Wrapper.
    /// 
    /// Wrapper defines a single method that can be called to unwrap the object.
    /// </summary>	
    public interface Wrapper
    {

        /// <summary> 
        /// Unwrap the object by returning the wrapped value.
        /// </summary>
        /// <returns>a wrapped value</returns>
        object Unwrap ();
    }

}