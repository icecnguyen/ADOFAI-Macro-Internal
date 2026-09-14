using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace ADOFAI_Macro_Internal
{
    public static class InputSimulator
    {
        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, uint uMapType);
        [DllImport("user32.dll")]
        public static extern short VkKeyScan(char ch);
        public const int KEYEVENTF_KEYDOWN = 0x0000;
        public const int KEYEVENTF_KEYUP = 0x0002;
        public const int KEYEVENTF_EXTENDEDKEY = 0x0001;

        public static bool IsExtendedKey(byte vkCode)
        {
            switch (vkCode)
            {
                case 0x5D:
                case 0x21:
                case 0x22:
                case 0x23:
                case 0x24:
                case 0x25:
                case 0x26:
                case 0x27:
                case 0x28:
                case 0x2D:
                case 0x2E:
                case 0x5B:
                case 0x5C:
                case 0x6F:
                case 0x90:
                case 0xA3:
                case 0xA5:
                    return true;
                default:
                    return false;
            }
        }

        private static readonly byte[] VkToScanMap = new byte[256];
        private static readonly int[] KeyGeneration = new int[256];

        private struct KeyReleaseItem
        {
            public byte vkCode;
            public byte scanCode;
            public uint dwFlags;
            public long releaseTimestampTicks;
            public int generation;
        }

        private static readonly ConcurrentQueue<KeyReleaseItem> ReleaseQueue = new ConcurrentQueue<KeyReleaseItem>();
        private static readonly AutoResetEvent ReleaseEvent = new AutoResetEvent(false);
        private static Thread _releaserThread;
        private static volatile bool _isRunning = true;

        static InputSimulator()
        {
            for (uint i = 0; i < 256; i++)
            {
                VkToScanMap[i] = (byte)MapVirtualKey(i, 0);
            }

            StartKeyReleaserThread();
        }

        private static void StartKeyReleaserThread()
        {
            if (_releaserThread != null && _releaserThread.IsAlive)
            {
                return;
            }

            _isRunning = true;
            _releaserThread = new Thread(KeyReleaserLoop) { IsBackground = true, Priority = System.Threading.ThreadPriority.AboveNormal, Name = "ADOFAI_FastKeyReleaser" };
            _releaserThread.Start();
        }

        private static void KeyReleaserLoop()
        {
            var pendingList = new List<KeyReleaseItem>(64);

            while (_isRunning)
            {
                while (ReleaseQueue.TryDequeue(out KeyReleaseItem item))
                {
                    pendingList.Add(item);
                }

                if (pendingList.Count == 0)
                {
                    ReleaseEvent.WaitOne(10);
                    continue;
                }

                long nowTicks = Stopwatch.GetTimestamp();
                long nextReleaseTicks = long.MaxValue;

                for (int i = pendingList.Count - 1; i >= 0; i--)
                {
                    var item = pendingList[i];

                    if (nowTicks >= item.releaseTimestampTicks)
                    {
                        if (item.generation == KeyGeneration[item.vkCode])
                        {
                            keybd_event(item.vkCode, item.scanCode, item.dwFlags | KEYEVENTF_KEYUP, 0);
                        }

                        pendingList.RemoveAt(i);
                    }
                    else
                    {
                        if (item.releaseTimestampTicks < nextReleaseTicks)
                        {
                            nextReleaseTicks = item.releaseTimestampTicks;
                        }
                    }
                }

                if (pendingList.Count > 0)
                {
                    long remainingTicks = nextReleaseTicks - Stopwatch.GetTimestamp();
                    double remainingMs = (double)remainingTicks * 1000.0 / Stopwatch.Frequency;

                    if (remainingMs > 1.5)
                    {
                        Thread.Sleep(1);
                    }
                    else if (remainingMs > 0.1)
                    {
                        Thread.SpinWait(20);
                    }
                }
            }
        }

        public static void SimulateKeyPressByVK(byte vkCode, double timeDiffToNextSameKey, double holdTimeOverride, double prevTimeDiff)
        {
            if (vkCode == 0)
            {
                return;
            }

            byte scanCode = VkToScanMap[vkCode];
            int holdDuration;

            if (holdTimeOverride > 0)
            {
                holdDuration = (int)Math.Round(holdTimeOverride * 1000.0);
            }
            else
            {
                double bpm = (Main.Settings != null && Main.Settings.MinPressBPM > 0.0001) ? Main.Settings.MinPressBPM : 500.0;
                holdDuration = (int)Math.Round(60000.0 / bpm);
                int sameKeyDiffMs = (int)(timeDiffToNextSameKey * 1000.0);

                if (sameKeyDiffMs > 6)
                {
                    if (holdDuration >= sameKeyDiffMs)
                    {
                        holdDuration = Math.Max(4, sameKeyDiffMs - 4);
                    }
                }
                else if (sameKeyDiffMs > 0)
                {
                    holdDuration = Math.Max(2, sameKeyDiffMs - 1);
                }

                if (Main.Settings != null && Main.Settings.EnableHumanSpoof && Main.Settings.SpoofJitterMs > 0.01f)
                {
                    int microJitter = UnityEngine.Random.Range(-1, 2);
                    holdDuration = Math.Max(2, holdDuration + microJitter);
                }
            }

            uint extFlags = IsExtendedKey(vkCode) ? (uint)KEYEVENTF_EXTENDEDKEY : 0;
            int gen = Interlocked.Increment(ref KeyGeneration[vkCode]);
            keybd_event(vkCode, scanCode, extFlags | KEYEVENTF_KEYDOWN, 0);

            if (holdDuration <= 0)
            {
                keybd_event(vkCode, scanCode, extFlags | KEYEVENTF_KEYUP, 0);
            }
            else
            {
                long releaseTimestamp = Stopwatch.GetTimestamp() + (long)((double)holdDuration * Stopwatch.Frequency / 1000.0);
                ReleaseQueue.Enqueue(new KeyReleaseItem { vkCode = vkCode, scanCode = scanCode, dwFlags = extFlags, releaseTimestampTicks = releaseTimestamp, generation = gen });
                ReleaseEvent.Set();
            }
        }

        public static byte UnityKeyCodeToVK(KeyCode keyCode)
        {
            int kc = (int)keyCode;

            if (kc >= 97 && kc <= 122)
            {
                return (byte)(kc - 32);
            }

            if (kc >= 48 && kc <= 57)
            {
                return (byte)kc;
            }
            if (kc >= 256 && kc <= 265)
            {
                return (byte)(kc - 256 + 0x60);
            }

            if (kc >= 282 && kc <= 293)
            {
                return (byte)(kc - 282 + 0x70);
            }

            if (kc >= 294 && kc <= 296)
            {
                return (byte)(kc - 294 + 0x7C);
            }

            switch (keyCode)
            {
                case KeyCode.KeypadPeriod: return 0x6E;
                case KeyCode.KeypadDivide: return 0x6F;
                case KeyCode.KeypadMultiply: return 0x6A;
                case KeyCode.KeypadMinus: return 0x6D;
                case KeyCode.KeypadPlus: return 0x6B;
                case KeyCode.KeypadEnter: return 0x0D;
                case KeyCode.KeypadEquals: return 0xBB;
                case KeyCode.CapsLock: return 0x14;
                case KeyCode.Numlock: return 0x90;
                case KeyCode.ScrollLock: return 0x91;
                case KeyCode.Space: return 0x20;
                case KeyCode.Return: return 0x0D;
                case KeyCode.Escape: return 0x1B;
                case KeyCode.LeftArrow: return 0x25;
                case KeyCode.UpArrow: return 0x26;
                case KeyCode.RightArrow: return 0x27;
                case KeyCode.DownArrow: return 0x28;
                case KeyCode.Tab: return 0x09;
                case KeyCode.LeftShift:
                case KeyCode.RightShift: return 0x10;
                case KeyCode.LeftControl:
                case KeyCode.RightControl: return 0x11;
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt: return 0x12;
                case KeyCode.Backspace: return 0x08;
                case KeyCode.Comma: return 0xBC;
                case KeyCode.Period: return 0xBE;
                case KeyCode.Slash: return 0xBF;
                case KeyCode.Semicolon: return 0xBA;
                case KeyCode.Quote: return 0xDE;
                case KeyCode.LeftBracket: return 0xDB;
                case KeyCode.RightBracket: return 0xDD;
                case KeyCode.Backslash: return 0xDC;
                case KeyCode.Minus: return 0xBD;
                case KeyCode.Equals: return 0xBB;
                case KeyCode.BackQuote: return 0xC0;
                case KeyCode.Menu: return 0x5D;
                case KeyCode.LeftWindows:
                case KeyCode.LeftCommand: return 0x5B;
                case KeyCode.RightWindows:
                case KeyCode.RightCommand: return 0x5C;
                case KeyCode.Insert: return 0x2D;
                case KeyCode.Delete: return 0x2E;
                case KeyCode.Home: return 0x24;
                case KeyCode.End: return 0x23;
                case KeyCode.PageUp: return 0x21;
                case KeyCode.PageDown: return 0x22;
                case KeyCode.Pause: return 0x13;
                case KeyCode.Print: return 0x2C;
                case KeyCode.Clear: return 0x0C;
                default:
                    try
                    {
                        if (kc > 0 && kc < 128)
                        {
                            short v = VkKeyScan((char)kc);
                            if (v != -1) return (byte)(v & 0xFF);
                        }
                    }
                    catch
                    {}

                    return 0;
            }
        }
    }
}