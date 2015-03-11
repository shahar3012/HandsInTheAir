using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HandsInTheAir
{
    public static class HandleHand
    {
        public static bool EnableSelect = false;

        public static bool ToggleSelectEnable()
        {
            EnableSelect = !EnableSelect;
            return EnableSelect;
        }



    }
}
