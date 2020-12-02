using System;
using System.Runtime.InteropServices;

namespace AdcControl
{
    public static class NativeMethods
    {
        //Private

        [Flags()]
        private enum EXECUTION_STATE : uint //Determine Monitor State
        {
            ES_AWAYMODE_REQUIRED = 0x40,
            ES_CONTINUOUS = 0x80000000u,
            ES_DISPLAY_REQUIRED = 0x2,
            ES_SYSTEM_REQUIRED = 0x1
            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }

        //Enables an application to inform the system that it is in use, thereby preventing the system from entering sleep or turning off the display while the application is running.
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        //Public

        public static bool PreventSleep()
        {
            return SetThreadExecutionState(
                EXECUTION_STATE.ES_CONTINUOUS | 
                EXECUTION_STATE.ES_DISPLAY_REQUIRED | 
                EXECUTION_STATE.ES_AWAYMODE_REQUIRED | 
                EXECUTION_STATE.ES_SYSTEM_REQUIRED
                ) != 0;
        }

        public static bool AllowSleep()
        {
            return SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS) != 0;
        }
    }
}
