using System;
using System.Runtime.InteropServices;
using System.Threading;
using FloatDock.Native;

namespace FloatDock.Services
{
    /// <summary>
    /// Implements Tool 1 (Back Button):
    /// Dispatches navigation commands to the currently active application without stealing focus.
    /// Primary: VK_BROWSER_BACK. Fallback: Alt+Left. Long-press/Secondary: Esc.
    /// </summary>
    public static class NavigationService
    {
        public static void SendNavigateBack()
        {
            try
            {
                // Primary approach: VK_BROWSER_BACK keydown + keyup
                var inputs = new NativeMethods.INPUT[2];

                inputs[0] = new NativeMethods.INPUT
                {
                    type = NativeMethods.INPUT_KEYBOARD,
                    u = new NativeMethods.InputUnion
                    {
                        ki = new NativeMethods.KEYBDINPUT
                        {
                            wVk = NativeMethods.VK_BROWSER_BACK,
                            wScan = 0,
                            dwFlags = NativeMethods.KEYEVENTF_EXTENDEDKEY,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };

                inputs[1] = new NativeMethods.INPUT
                {
                    type = NativeMethods.INPUT_KEYBOARD,
                    u = new NativeMethods.InputUnion
                    {
                        ki = new NativeMethods.KEYBDINPUT
                        {
                            wVk = NativeMethods.VK_BROWSER_BACK,
                            wScan = 0,
                            dwFlags = NativeMethods.KEYEVENTF_EXTENDEDKEY | NativeMethods.KEYEVENTF_KEYUP,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };

                uint result = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
                if (result == 0)
                {
                    LoggerService.LogWarning("VK_BROWSER_BACK SendInput returned 0. Triggering Alt+Left fallback.");
                    SendAltLeftFallback();
                }
                else
                {
                    LoggerService.LogInfo("Dispatched VK_BROWSER_BACK to foreground window.");
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Exception in SendNavigateBack. Triggering Alt+Left fallback.", ex);
                SendAltLeftFallback();
            }
        }

        public static void SendAltLeftFallback()
        {
            try
            {
                // Fallback approach: Alt Down -> Left Down -> Left Up -> Alt Up
                var inputs = new NativeMethods.INPUT[4];

                // 1. Alt Down
                inputs[0] = new NativeMethods.INPUT
                {
                    type = NativeMethods.INPUT_KEYBOARD,
                    u = new NativeMethods.InputUnion
                    {
                        ki = new NativeMethods.KEYBDINPUT
                        {
                            wVk = NativeMethods.VK_MENU,
                            dwFlags = 0
                        }
                    }
                };

                // 2. Left Arrow Down
                inputs[1] = new NativeMethods.INPUT
                {
                    type = NativeMethods.INPUT_KEYBOARD,
                    u = new NativeMethods.InputUnion
                    {
                        ki = new NativeMethods.KEYBDINPUT
                        {
                            wVk = NativeMethods.VK_LEFT,
                            dwFlags = NativeMethods.KEYEVENTF_EXTENDEDKEY
                        }
                    }
                };

                // 3. Left Arrow Up
                inputs[2] = new NativeMethods.INPUT
                {
                    type = NativeMethods.INPUT_KEYBOARD,
                    u = new NativeMethods.InputUnion
                    {
                        ki = new NativeMethods.KEYBDINPUT
                        {
                            wVk = NativeMethods.VK_LEFT,
                            dwFlags = NativeMethods.KEYEVENTF_EXTENDEDKEY | NativeMethods.KEYEVENTF_KEYUP
                        }
                    }
                };

                // 4. Alt Up
                inputs[3] = new NativeMethods.INPUT
                {
                    type = NativeMethods.INPUT_KEYBOARD,
                    u = new NativeMethods.InputUnion
                    {
                        ki = new NativeMethods.KEYBDINPUT
                        {
                            wVk = NativeMethods.VK_MENU,
                            dwFlags = NativeMethods.KEYEVENTF_KEYUP
                        }
                    }
                };

                NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
                LoggerService.LogInfo("Dispatched Alt+Left fallback navigation.");
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to send Alt+Left fallback navigation.", ex);
            }
        }

        public static void SendEscape()
        {
            try
            {
                var inputs = new NativeMethods.INPUT[2];

                inputs[0] = new NativeMethods.INPUT
                {
                    type = NativeMethods.INPUT_KEYBOARD,
                    u = new NativeMethods.InputUnion
                    {
                        ki = new NativeMethods.KEYBDINPUT
                        {
                            wVk = NativeMethods.VK_ESCAPE,
                            dwFlags = 0
                        }
                    }
                };

                inputs[1] = new NativeMethods.INPUT
                {
                    type = NativeMethods.INPUT_KEYBOARD,
                    u = new NativeMethods.InputUnion
                    {
                        ki = new NativeMethods.KEYBDINPUT
                        {
                            wVk = NativeMethods.VK_ESCAPE,
                            dwFlags = NativeMethods.KEYEVENTF_KEYUP
                        }
                    }
                };

                NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));
                LoggerService.LogInfo("Dispatched Escape key to foreground window.");
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to send Escape key.", ex);
            }
        }
    }
}
