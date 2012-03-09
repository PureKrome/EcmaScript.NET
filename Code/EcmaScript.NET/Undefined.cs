//------------------------------------------------------------------------------
// <license file="Undefined.cs">
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
    /// This class implements the Undefined value in JavaScript.
    /// </summary>	
    public sealed class Undefined
    {

        public static readonly object Value = new Undefined ();

        private Undefined ()
        {
            ;
        }

    }

}