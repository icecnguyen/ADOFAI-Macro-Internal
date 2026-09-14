using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;

namespace ADOFAI_Macro_Internal
{
    public static class Main
    {
        public static UnityModManager.ModEntry Mod;
        public static Harmony HarmonyInstance;
        public static Settings Settings;
        public static bool IsAuthorized = true;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Mod = modEntry;
            Mod.OnToggle = OnToggle;
            Mod.OnGUI = OnGUI;
            Mod.OnSaveGUI = OnSaveGUI;
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

            if (Settings.Keys == null || Settings.Keys.Length != 32)
            {
                var oldKeys = Settings.Keys;
                Settings.Keys = new KeyCode[32];

                if (oldKeys != null)
                {
                    if (oldKeys.Length == 24)
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            Settings.Keys[i] = oldKeys[i];
                        }

                        for (int i = 0; i < 12; i++)
                        {
                            Settings.Keys[16 + i] = oldKeys[12 + i];
                        }
                    }
                    else
                    {
                        for (int i = 0; i < oldKeys.Length && i < 32; i++)
                        {
                            Settings.Keys[i] = oldKeys[i];
                        }
                    }
                }
            }

            HarmonyInstance = new Harmony(modEntry.Info.Id);
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            if (value)
            {
                try
                {
                    HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                    Mod.Logger.Log("Macro Enabled!");
                }
                catch (Exception ex)
                {
                    Mod.Logger.Log("[HARMONY] " + ex.Message);
                }
            }
            else
            {
                try
                {
                    HarmonyInstance.UnpatchAll(modEntry.Info.Id);
                    Mod.Logger.Log("Macro Disabled!");
                }
                catch { }
            }

            return true;
        }

        private static int listeningIndex = -1;

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Toggle(Settings.Language == "VI", "Tiếng Việt", GUILayout.Width(100)))
            {
                Settings.Language = "VI";
            }

            if (GUILayout.Toggle(Settings.Language == "EN", "English", GUILayout.Width(100)))
            {
                Settings.Language = "EN";
            }

            GUILayout.EndHorizontal();
            bool isVN = Settings.Language == "VI";
            GUILayout.Space(10);
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b><size=16>ADOFAI Macro</size></b>");
            Settings.EnableAutoplay = GUILayout.Toggle(Settings.EnableAutoplay, isVN ? "Bật/Tắt Macro" : "Enable/Disable Macro");
            GUILayout.Space(10);
            GUILayout.Label(isVN ? "<b>Kiểu Roll:</b>" : "<b>Roll Style:</b>");
            GUILayout.BeginHorizontal();

            if (GUILayout.Toggle(Settings.RollStyle == 0, "Inward", GUILayout.Width(150)))
            {
                if (Settings.RollStyle != 0)
                {
                    Settings.RollStyle = 0;
                    Patches.PreprocessLevel();
                }
            }

            if (GUILayout.Toggle(Settings.RollStyle == 1, "Outward", GUILayout.Width(150)))
            {
                if (Settings.RollStyle != 1)
                {
                    Settings.RollStyle = 1;
                    Patches.PreprocessLevel();
                }
            }

            if (GUILayout.Toggle(Settings.RollStyle == 2, "Staircase", GUILayout.Width(150)))
            {
                if (Settings.RollStyle != 2)
                {
                    Settings.RollStyle = 2;
                    Patches.PreprocessLevel();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            if (GUILayout.Toggle(Settings.RollStyle == 3, "Reverse Staircase", GUILayout.Width(150)))
            {
                if (Settings.RollStyle != 3)
                {
                    Settings.RollStyle = 3;
                    Patches.PreprocessLevel();
                }
            }

            if (GUILayout.Toggle(Settings.RollStyle == 4, isVN ? "Ngẫu nhiên" : "Random", GUILayout.Width(220)))
            {
                if (Settings.RollStyle != 4)
                {
                    Settings.RollStyle = 4;
                    Patches.PreprocessLevel();
                }
            }

            GUILayout.EndHorizontal();

            if (Settings.RollStyle == 4)
            {
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.Label(isVN ? $"Thời gian đổi kiểu: <b>{Settings.RandomRollIntervalSec:F0}s</b>" : $"Style Interval: <b>{Settings.RandomRollIntervalSec:F0}s</b>", GUILayout.Width(170));
                float newInterval = GUILayout.HorizontalSlider(Settings.RandomRollIntervalSec, 3f, 30f, GUILayout.Width(180));

                if (Math.Abs(newInterval - Settings.RandomRollIntervalSec) > 0.4f)
                {
                    Settings.RandomRollIntervalSec = (float)Math.Round(newInterval);
                    Patches.PreprocessLevel();
                }

                if (GUILayout.Button(isVN ? "Làm mới" : "Refresh", GUILayout.Width(120)))
                {
                    Patches.PreprocessLevel();
                }
                
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Label(isVN ? "<b>Tay chính:</b>" : "<b>Main Hand:</b>");
            GUILayout.BeginHorizontal();

            if (GUILayout.Toggle(Settings.MainHand == 0, isVN ? "Tay trái" : "Left Hand", GUILayout.Width(180)))
            {
                if (Settings.MainHand != 0)
                {
                    Settings.MainHand = 0;
                    Patches.PreprocessLevel();
                }
            }

            if (GUILayout.Toggle(Settings.MainHand == 1, isVN ? "Tay phải" : "Right Hand", GUILayout.Width(180)))
            {
                if (Settings.MainHand != 1)
                {
                    Settings.MainHand = 1;
                    Patches.PreprocessLevel();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.Label(isVN ? "<b>Giới hạn phím:</b>" : "<b>Key Limit:</b>");
            GUILayout.BeginHorizontal();

            if (GUILayout.Toggle(Settings.FingersPerHand == 0, isVN ? "Không giới hạn" : "None", GUILayout.Width(150)))
            {
                if (Settings.FingersPerHand != 0)
                {
                    Settings.FingersPerHand = 0;
                    Patches.PreprocessLevel();
                }
            }

            if (GUILayout.Toggle(Settings.FingersPerHand == 1, isVN ? "2 phím" : "2 Keys", GUILayout.Width(100)))
            {
                if (Settings.FingersPerHand != 1)
                {
                    Settings.FingersPerHand = 1;
                    Patches.PreprocessLevel();
                }
            }

            if (GUILayout.Toggle(Settings.FingersPerHand == 2, isVN ? "4 phím" : "4 Keys", GUILayout.Width(100)))
            {
                if (Settings.FingersPerHand != 2)
                {
                    Settings.FingersPerHand = 2;
                    Patches.PreprocessLevel();
                }
            }

            if (GUILayout.Toggle(Settings.FingersPerHand == 3, isVN ? "6 phím" : "6 Keys", GUILayout.Width(100)))
            {
                if (Settings.FingersPerHand != 3)
                {
                    Settings.FingersPerHand = 3;
                    Patches.PreprocessLevel();
                }
            }

            if (GUILayout.Toggle(Settings.FingersPerHand == 4, isVN ? "8 phím" : "8 Keys", GUILayout.Width(130)))
            {
                if (Settings.FingersPerHand != 4)
                {
                    Settings.FingersPerHand = 4;
                    Patches.PreprocessLevel();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            if (GUILayout.Toggle(Settings.FingersPerHand == 5, isVN ? "10 phím" : "10 Keys", GUILayout.Width(130)))
            {
                if (Settings.FingersPerHand != 5)
                {
                    Settings.FingersPerHand = 5;
                    Patches.PreprocessLevel();
                }
            }

            if (GUILayout.Toggle(Settings.FingersPerHand == 6, isVN ? "12 phím" : "12 Keys", GUILayout.Width(100)))
            {
                if (Settings.FingersPerHand != 6)
                {
                    Settings.FingersPerHand = 6;
                    Patches.PreprocessLevel();
                }
            }

            if (GUILayout.Toggle(Settings.FingersPerHand == 7, isVN ? "14 phím" : "14 Keys", GUILayout.Width(100)))
            {
                if (Settings.FingersPerHand != 7)
                {
                    Settings.FingersPerHand = 7;
                    Patches.PreprocessLevel();
                }
            }

            if (GUILayout.Toggle(Settings.FingersPerHand == 8, isVN ? "16 phím" : "16 Keys", GUILayout.Width(100)))
            {
                if (Settings.FingersPerHand != 8)
                {
                    Settings.FingersPerHand = 8;
                    Patches.PreprocessLevel();
                }
            }

            if (GUILayout.Toggle(Settings.FingersPerHand == 16, isVN ? "32 phím" : "32 Keys", GUILayout.Width(120)))
            {
                if (Settings.FingersPerHand != 16)
                {
                    Settings.FingersPerHand = 16;
                    Patches.PreprocessLevel();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            string currentModeLabel = Settings.FingersPerHand == 0 ? (isVN ? "Chế độ hiện tại: <b>Không giới hạn</b>" : "Current mode: <b>None</b>") : (isVN ? $"Chế độ hiện tại: <b>{Settings.FingersPerHand} phím </b>" : $"Current mode: <b>{Settings.FingersPerHand} keys </b>");
            GUILayout.Label(currentModeLabel, GUILayout.Width(300));
            GUILayout.Label(isVN ? "Tùy chỉnh (0 = Không giới hạn):" : "Custom (0 = None):", GUILayout.Width(150));
            string fingersStr = GUILayout.TextField(Settings.FingersPerHand.ToString(), GUILayout.Width(60));

            if (int.TryParse(fingersStr, out int newFingers) && newFingers >= 0 && newFingers <= 16 && newFingers != Settings.FingersPerHand)
            {
                Settings.FingersPerHand = newFingers;
                Patches.PreprocessLevel();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label(isVN ? "Ngưỡng BPM Roll:" : "Roll Threshold:", GUILayout.Width(250));
            string oneHandStr = GUILayout.TextField(Settings.OneHandBPM.ToString("F0"), GUILayout.Width(100));

            if (double.TryParse(oneHandStr, out double newOneHand) && newOneHand > 0 && Math.Abs(newOneHand - Settings.OneHandBPM) > 0.1)
            {
                Settings.OneHandBPM = newOneHand;
                Patches.PreprocessLevel();
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(isVN ? "Tốc độ nhả phím:" : "Release Speed:", GUILayout.Width(250));
            string minPressStr = GUILayout.TextField(Settings.MinPressBPM.ToString("F0"), GUILayout.Width(100));

            if (double.TryParse(minPressStr, out double newMinPress) && newMinPress > 0)
            {
                Settings.MinPressBPM = newMinPress;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label(isVN ? "Độ trễ (ms):" : "Timing Offset (ms):", GUILayout.Width(250));
            string offsetStr = GUILayout.TextField(Settings.TimingOffsetMs.ToString(), GUILayout.Width(100));

            if (int.TryParse(offsetStr, out int newOffset))
            {
                Settings.TimingOffsetMs = newOffset;
            }
            
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            Settings.EnableHumanSpoof = GUILayout.Toggle(Settings.EnableHumanSpoof, isVN ? "Human Spoof" : "Human Spoof");

            if (Settings.EnableHumanSpoof)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(isVN ? "Sai số ngẫu nhiên (ms):" : "Random Jitter (ms):", GUILayout.Width(250));
                string jitterStr = GUILayout.TextField(Settings.SpoofJitterMs.ToString("F1"), GUILayout.Width(100));

                if (float.TryParse(jitterStr, out float newJitter))
                {
                    Settings.SpoofJitterMs = Mathf.Max(0f, Mathf.Min(30f, newJitter));
                }
                
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Label(isVN ? "Cài đặt phím" : "Key Binding");
            Event e = Event.current;

            if (listeningIndex != -1 && e.isKey && e.type == EventType.KeyDown)
            {
                Settings.Keys[listeningIndex] = e.keyCode;
                listeningIndex = -1;
                Patches.PreprocessLevel();
                e.Use();
            }

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical("box", GUILayout.Width(320));
            GUILayout.Label(isVN ? "<b>Tay Trái</b>" : "<b>Left Hand</b>");

            for (int i = 0; i < 16; i++)
            {
                string keyName = Settings.Keys[i] == KeyCode.None ? (isVN ? "Trống" : "None") : Settings.Keys[i].ToString();

                if (listeningIndex == i)
                {
                    keyName = isVN ? "Nhấn phím..." : "Press key...";
                }

                GUILayout.BeginHorizontal();

                if (GUILayout.Button($"Phím {i + 1}: {keyName}", GUILayout.Width(240)))
                {
                    listeningIndex = (listeningIndex == i) ? -1 : i;
                }

                if (GUILayout.Button("X", GUILayout.Width(35)))
                {
                    Settings.Keys[i] = KeyCode.None;
                    if (listeningIndex == i) listeningIndex = -1;
                    Patches.PreprocessLevel();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.Space(20);
            GUILayout.BeginVertical("box", GUILayout.Width(320));
            GUILayout.Label(isVN ? "<b>Tay Phải</b>" : "<b>Right Hand</b>");

            for (int i = 16; i < 32; i++)
            {
                string keyName = Settings.Keys[i] == KeyCode.None ? (isVN ? "Trống" : "None") : Settings.Keys[i].ToString();

                if (listeningIndex == i)
                {
                    keyName = isVN ? "Nhấn phím..." : "Press key...";
                }

                GUILayout.BeginHorizontal();

                if (GUILayout.Button($"Phím {i + 1}: {keyName}", GUILayout.Width(240)))
                {
                    listeningIndex = (listeningIndex == i) ? -1 : i;
                }

                if (GUILayout.Button("X", GUILayout.Width(35)))
                {
                    Settings.Keys[i] = KeyCode.None;
                    if (listeningIndex == i) listeningIndex = -1;
                    Patches.PreprocessLevel();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (listeningIndex != -1)
            {
                GUILayout.Label(isVN ? "<i>Đang chờ bấm phím... Nhấn phím bất kỳ trên bàn phím.</i>" : "<i>Listening... Press any key.</i>");
            }

            GUILayout.EndVertical();
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }
    }
}