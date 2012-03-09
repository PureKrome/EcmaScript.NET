//------------------------------------------------------------------------------
// <license file="SubString.cs">
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

namespace EcmaScript.NET.Types.RegExp
{

    class SubString
    {

        public SubString ()
        {
        }

        public SubString (string str)
        {
            index = 0;
            charArray = str.ToCharArray ();
            length = str.Length;
        }

        public SubString (char [] source, int start, int len)
        {
            index = 0;
            length = len;
            charArray = new char [len];
            Array.Copy (source, start, charArray, 0, len);
        }

        public override string ToString ()
        {
            return charArray == null ?
                string.Empty : new string (charArray, index, length);
        }

        internal static readonly SubString EmptySubString = new SubString ();

        internal char [] charArray;
        internal int index;
        internal int length;
    }
}