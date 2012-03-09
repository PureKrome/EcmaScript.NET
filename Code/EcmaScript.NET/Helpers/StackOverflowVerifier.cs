using System;
using System.Collections.Generic;
using System.Text;

namespace EcmaScript.NET.Helpers
{

    public class StackOverflowVerifier : IDisposable
    {

        [ThreadStatic]
        private static long m_Counter = long.MinValue;

        private int m_MaxStackSize = 0;
        
        public StackOverflowVerifier (int maxStackSize)
        {
            m_MaxStackSize = maxStackSize;
            
            ChangeStackDepth (+1);
        }

        public void Dispose ()
        {
            ChangeStackDepth (-1);
        }

        void ChangeStackDepth (int offset)
        {
            if (m_Counter == long.MinValue)
                m_Counter = 0;
            m_Counter += offset;
            if (m_Counter > m_MaxStackSize)
                throw new StackOverflowVerifierException ();
        }
        
    }
    
}
