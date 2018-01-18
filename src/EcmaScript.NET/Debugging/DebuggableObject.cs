//------------------------------------------------------------------------------
// <license file="DebuggableObject.cs">
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
namespace EcmaScript.NET.Debugging
{

    /// <summary> This interface exposes debugging information from objects.</summary>
    public interface DebuggableObject
    {
        /// <summary> Returns an array of ids for the properties of the object.
        /// 
        /// <p>All properties, even those with attribute {DontEnum}, are listed.
        /// This allows the debugger to display all properties of the object.<p>
        /// 
        /// </summary>
        /// <returns> an array of java.lang.Objects with an entry for every
        /// listed property. Properties accessed via an integer index will
        /// have a corresponding
        /// Integer entry in the returned array. Properties accessed by
        /// a String will have a String entry in the returned array.
        /// </returns>
        object [] AllIds
        {
            get;

        }
    }
}