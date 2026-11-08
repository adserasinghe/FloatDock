using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using FloatDock.Native;

namespace FloatDock.Services
{
    public class HotkeyService : IDisposable
    {
        private readonly IntPtr _hWnd;
        private HwndSource? _source;
        private int _currentId = 9000;
        private readonly Dictionary<int, Action> _hotkeyActions = new();

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        public event Action<string>? HotkeyConflictDetected;

        public HotkeyService(IntPtr hWnd)
        {
            _hWnd = hWnd;
            _source = HwndSource.FromHwnd(_hWnd);
            _source?.AddHook(HwndHook);
        }

        public int Register(string hotkeyString, Action callback)
        {
            if (string.IsNullOrWhiteSpace(hotkeyString)) return -1;

            if (!ParseHotkey(hotkeyString, out uint modifiers, out uint vk))
            {
                LoggerService.LogWarning($"Could not parse hotkey string: '{hotkeyString}'");
                return -1;
            }

            int id = ++_currentId;
            bool success = NativeMethods.RegisterHotKey(_hWnd, id, modifiers | MOD_NOREPEAT, vk);

            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                LoggerService.LogWarning($"Failed to register hotkey '{hotkeyString}'. Win32 Error: {error} (Possible conflict with another app)");
                HotkeyConflictDetected?.Invoke(hotkeyString);
                return -1;
            }

            _hotkeyActions[id] = callback;
            LoggerService.LogInfo($"Registered global hotkey '{hotkeyString}' with ID {id}.");
            return id;
        }

        public void Unregister(int id)
        {
            if (_hotkeyActions.ContainsKey(id))
            {
                NativeMethods.UnregisterHotKey(_hWnd, id);
                _hotkeyActions.Remove(id);
            }
        }

        public void UnregisterAll()
        {
            foreach (var id in new List<int>(_hotkeyActions.Keys))
            {
                NativeMethods.UnregisterHotKey(_hWnd, id);
            }
            _hotkeyActions.Clear();
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_hotkeyActions.TryGetValue(id, out var action))
                {
                    action.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private static bool ParseHotkey(string str, out uint modifiers, out uint vk)
        {
            modifiers = 0;
            vk = 0;

            string[] tokens = str.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return false;

            for (int i = 0; i < tokens.Length - 1; i++)
            {
                string token = tokens[i].ToUpperInvariant();
                if (token is "CTRL" or "CONTROL") modifiers |= MOD_CONTROL;
                else if (token is "ALT") modifiers |= MOD_ALT;
                else if (token is "SHIFT") modifiers |= MOD_SHIFT;
                else if (token is "WIN" or "WINDOWS") modifiers |= MOD_WIN;
            }

            string keyToken = tokens[^1].ToUpperInvariant();

            if (Enum.TryParse<Key>(keyToken, true, out var key))
            {
                vk = (uint)KeyInterop.VirtualKeyFromKey(key);
                return true;
            }

            if (keyToken.Length == 1 && char.IsLetterOrDigit(keyToken[0]))
            {
                vk = keyToken[0];
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            UnregisterAll();
            if (_source != null)
            {
                _source.RemoveHook(HwndHook);
                _source = null;
            }
        }
    }
}
