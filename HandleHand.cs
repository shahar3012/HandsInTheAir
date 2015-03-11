using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HandsInTheAir
{
    public static class HandleHand
    {
        private static bool m_EnableSelect = true;
        public static bool EnableSelect { get { return m_EnableSelect; } }

        public static bool ToggleSelectEnable()
        {
            m_EnableSelect = !m_EnableSelect;
            return EnableSelect;
        }


        private static bool m_EnableMove = true;
        public static bool MoveEnabled { get { return m_EnableMove; } }

        public static bool EnableMove()
        {
            m_EnableMove = true;
            return MoveEnabled;
        }


        public static bool DisableMove()
        {
            m_EnableMove = false;
            return MoveEnabled;
        }



    }
}
