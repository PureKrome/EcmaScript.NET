//------------------------------------------------------------------------------
// <license file="UniqueTag.cs">
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
    /// Class instances represent serializable tags to mark special Object values.    
    /// </summary>
    public class UniqueTag
    {

        /// <summary>
        /// Tag to mark non-existing values.
        /// </summary>		
        public static readonly UniqueTag NotFound = new UniqueTag ("NotFound");

        /// <summary>
        /// Tag to distinguish between uninitialized and null values.
        /// </summary>		
        public static readonly UniqueTag NullValue = new UniqueTag ("NullValue");

        /// <summary>
        /// Tag to indicate that a object represents "double" with the real value
        /// stored somewhere else.
        /// </summary>		
        public static readonly UniqueTag DoubleMark = new UniqueTag ("DoubleMark");

        /// <summary>
        /// Tag to indicate that a object represents "long" with the real value
        /// stored somewhere else.
        /// </summary>		
        public static readonly UniqueTag LongMark = new UniqueTag ("LongMark");
        
        private string tagName;        

        private UniqueTag (string tagName)
        {
            this.tagName = tagName;
        }

        /// <summary>
        /// Returns a string represenation of this UniqueTag
        /// </summary>
        /// <returns></returns>
        public override string ToString ()
        {
           return "UniqueTag." + this.tagName;
        }
        
    }
}