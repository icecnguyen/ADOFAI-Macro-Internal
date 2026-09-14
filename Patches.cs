using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ADOFAI_Macro_Internal
{
    public static class Patches
    {
        public static float AutoOffsetMs = 0f;
        static int lastHitFloor = -1;
        static double lastTargetTime = 0;
        static int cachedTargetSeqID = -1;
        static double cachedSpoofOffset = 0.0;
        static Dictionary<int, byte> floorToKeyMap = new Dictionary<int, byte>();
        static Dictionary<int, double> floorToSameKeyDiff = new Dictionary<int, double>();
        static int lastProcessedFloorCount = -1;

        private static double GetGaussianRandom(double mean, double stdDev)
        {
            double u1 = 1.0 - (double)UnityEngine.Random.value;
            if (u1 <= 0.000001)
            {
                u1 = 0.000001;
            }

            double u2 = 1.0 - (double)UnityEngine.Random.value;
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }

        public static void PreprocessLevel()
        {
            floorToKeyMap.Clear();
            floorToSameKeyDiff.Clear();

            if (ADOBase.lm == null || ADOBase.lm.listFloors == null)
            {
                return;
            }

            lastProcessedFloorCount = ADOBase.lm.listFloors.Count;
            HashSet<int> holdReleaseFloors = new HashSet<int>();

            for (int i = 0; i < ADOBase.lm.listFloors.Count; i++)
            {
                scrFloor f = ADOBase.lm.listFloors[i];

                if (f != null && f.holdLength > -1)
                {
                    scrFloor tail = f.nextfloor;

                    while (tail != null && (tail.midSpin || tail.auto))
                    {
                        tail = tail.nextfloor;
                    }

                    if (tail != null && tail.holdLength == -1)
                    {
                        holdReleaseFloors.Add(tail.seqID);
                    }
                }
            }

            var validTiles = new List<scrFloor>();

            for (int i = 0; i < ADOBase.lm.listFloors.Count; i++)
            {
                scrFloor f = ADOBase.lm.listFloors[i];

                if (f != null && !f.midSpin && !f.auto && !holdReleaseFloors.Contains(f.seqID))
                {
                    validTiles.Add(f);
                }
            }

            if (validTiles.Count == 0)
            {
                return;
            }

            var inputList = AdoMacro3Engine.GenerateInputList(validTiles);

            for (int i = 0; i < validTiles.Count && i < inputList.Count; i++)
            {
                floorToKeyMap[validTiles[i].seqID] = inputList[i].vkCode;
            }

            for (int i = 0; i < validTiles.Count && i < inputList.Count; i++)
            {
                byte vk = inputList[i].vkCode;
                double nextSameTime = -1;

                for (int j = i + 1; j < validTiles.Count && j < inputList.Count && j < i + 128; j++)
                {
                    if (inputList[j].vkCode == vk)
                    {
                        nextSameTime = validTiles[j].entryTime - validTiles[i].entryTime;
                        break;
                    }
                }

                floorToSameKeyDiff[validTiles[i].seqID] = nextSameTime > 0 ? nextSameTime : 2.0;
            }

            SaveJsonCacheAsync(validTiles, inputList);
        }

        private static void SaveJsonCacheAsync(List<scrFloor> validTiles, List<AdoMacro3Engine.InputNote> notes)
        {
            Task.Run(() =>
            {
                try
                {
                    string levelName = "level_" + (ADOBase.lm != null && ADOBase.lm.listFloors != null ? ADOBase.lm.listFloors.Count.ToString() : "map");
                    try
                    {
                        var gcsType = AccessTools.TypeByName("GCS");

                        if (gcsType != null)
                        {
                            var field = AccessTools.Field(gcsType, "customLevelPaths");

                            if (field != null)
                            {
                                var paths = field.GetValue(null) as string[];

                                if (paths != null && paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
                                {
                                    levelName = Path.GetFileNameWithoutExtension(paths[0]);
                                }
                            }
                        }
                    }
                    catch
                    {}

                    foreach (char c in Path.GetInvalidFileNameChars())
                    {
                        levelName = levelName.Replace(c, '_');
                    }

                    string cacheDir = Path.Combine(Main.Mod.Path, "Cache");

                    if (!Directory.Exists(cacheDir))
                    {
                        Directory.CreateDirectory(cacheDir);
                    }

                    string filePath = Path.Combine(cacheDir, levelName + ".json");
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("{");
                    sb.AppendLine($"  \"level\": \"{levelName}\",");
                    sb.AppendLine($"  \"rollStyle\": {Main.Settings.RollStyle},");
                    sb.AppendLine($"  \"oneHandBPM\": {Main.Settings.OneHandBPM},");
                    sb.AppendLine($"  \"minPressBPM\": {Main.Settings.MinPressBPM},");
                    sb.AppendLine($"  \"engine\": \"ADOFAI_Macro_3 High-Performance Engine\",");
                    sb.AppendLine($"  \"mappedFloors\": {floorToKeyMap.Count},");
                    sb.AppendLine($"  \"generatedAt\": \"{DateTime.UtcNow:O}\",");
                    sb.AppendLine("  \"notes\": [");

                    for (int i = 0; i < validTiles.Count && i < notes.Count; i++)
                    {
                        var f = validTiles[i];
                        var n = notes[i];
                        sb.AppendLine("    {");
                        sb.AppendLine($"      \"index\": {i},");
                        sb.AppendLine($"      \"seqId\": {f.seqID},");
                        sb.AppendLine($"      \"pushTime\": {n.push_time:F2},");
                        sb.AppendLine($"      \"hand\": {n.hand},");
                        sb.AppendLine($"      \"finger\": {n.finger},");
                        sb.AppendLine($"      \"vkCode\": {n.vkCode}");
                        sb.Append("    }");
                        if (i < validTiles.Count - 1 && i < notes.Count - 1) sb.Append(",");
                        sb.AppendLine();
                    }

                    sb.AppendLine("  ]");
                    sb.AppendLine("}");
                    File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                }
                catch
                {}
            });
        }

        [HarmonyPatch(typeof(scrController), "Awake")]

        public static class scrController_Awake_Patch
        {
            public static void Postfix()
            {
                lastHitFloor = -1;
                lastTargetTime = 0;
                cachedTargetSeqID = -1;
                cachedSpoofOffset = 0.0;
            }
        }

        [HarmonyPatch(typeof(scrController), "Restart")]

        public static class scrController_Restart_Patch
        {
            public static void Postfix()
            {
                lastHitFloor = -1;
                lastTargetTime = 0;
                cachedTargetSeqID = -1;
                cachedSpoofOffset = 0.0;

                if (Main.Settings != null && Main.Settings.RollStyle == 4)
                {
                    PreprocessLevel();
                }
            }
        }

        [HarmonyPatch(typeof(scrController), "PlayerControl_Update")]

        public static class scrController_PlayerControl_Update_Patch
        {
            public static void Prefix(scrController __instance)
            {
                try
                {
                    if (Main.Settings == null || !Main.Settings.EnableAutoplay)
                    {
                        return;
                    }

                    if (__instance.paused || __instance.audioPaused)
                    {
                        return;
                    }

                    if (ADOBase.lm == null || ADOBase.lm.listFloors == null || ADOBase.conductor == null)
                    {
                        return;
                    }

                    int currentFloor = (__instance.currFloor != null) ? __instance.currFloor.seqID : __instance.currentSeqID;
                    bool isNewLevel = (floorToKeyMap.Count == 0 || lastProcessedFloorCount != ADOBase.lm.listFloors.Count);
                    bool isRestart = (currentFloor == 0 && lastHitFloor >= 0) || (lastHitFloor != -1 && currentFloor < lastHitFloor - 1);

                    if (isNewLevel)
                    {
                        lastHitFloor = -1;
                        lastTargetTime = 0;
                        cachedTargetSeqID = -1;
                        cachedSpoofOffset = 0.0;
                        PreprocessLevel();
                    }
                    else if (isRestart)
                    {
                        lastHitFloor = -1;
                        lastTargetTime = 0;
                        cachedTargetSeqID = -1;
                        cachedSpoofOffset = 0.0;

                        if (Main.Settings != null && Main.Settings.RollStyle == 4)
                        {
                            PreprocessLevel();
                        }
                    }

                    int floorToCheck = (lastHitFloor == -1 || lastHitFloor < currentFloor - 2 || lastHitFloor > currentFloor + 15) ? currentFloor : lastHitFloor;
                    int iterations = 0;

                    while (floorToCheck >= 0 && floorToCheck < ADOBase.lm.listFloors.Count && iterations < 16)
                    {
                        iterations++;
                        scrFloor currFloorObj = ADOBase.lm.listFloors[floorToCheck];

                        if (currFloorObj == null || currFloorObj.nextfloor == null)
                        {
                            break;
                        }

                        scrFloor nextFloor = currFloorObj.nextfloor;

                        while (nextFloor != null && (nextFloor.midSpin || nextFloor.auto))
                        {
                            nextFloor = nextFloor.nextfloor;
                        }

                        if (nextFloor == null || nextFloor.midSpin || nextFloor.auto)
                        {
                            break;
                        }

                        double targetTime = nextFloor.entryTime;
                        double currentTime = ADOBase.conductor.songposition_minusi;
                        double totalOffsetMs = Main.Settings.TimingOffsetMs;
                        double offsetSeconds = totalOffsetMs / 1000.0;
                        double frameLead = Math.Max(0.001, (double)Time.unscaledDeltaTime * 0.5) + 0.0015;

                        if (nextFloor.seqID != cachedTargetSeqID)
                        {
                            cachedTargetSeqID = nextFloor.seqID;

                            if (Main.Settings.EnableHumanSpoof && Main.Settings.SpoofJitterMs > 0.01f)
                            {
                                double frameCompMs = Math.Min(8.0, Math.Max(2.0, (double)(Time.unscaledDeltaTime * 500.0) + 1.2));
                                double jitterMs = GetGaussianRandom(-frameCompMs, (double)Main.Settings.SpoofJitterMs);
                                double maxAllowedMs = Math.Min((double)Main.Settings.SpoofJitterMs * 2.5, 20.0);
                                jitterMs = Math.Max(-maxAllowedMs - frameCompMs, Math.Min(maxAllowedMs - frameCompMs, jitterMs));
                                cachedSpoofOffset = jitterMs / 1000.0;
                            }
                            else
                            {
                                cachedSpoofOffset = 0.0;
                            }
                        }

                        double spoof = Main.Settings.EnableHumanSpoof ? cachedSpoofOffset : 0.0;

                        if (currentTime + frameLead >= targetTime + offsetSeconds + spoof)
                        {
                            double timeDiffToNext = 0.5;
                            scrFloor lookAhead = nextFloor.nextfloor;

                            while (lookAhead != null && (lookAhead.midSpin || lookAhead.auto))
                            {
                                lookAhead = lookAhead.nextfloor;
                            }

                            if (lookAhead != null)
                            {
                                timeDiffToNext = lookAhead.entryTime - nextFloor.entryTime;
                            }

                            double holdTimeOverride = -1;

                            if (nextFloor.holdLength > -1 && lookAhead != null)
                            {
                                double timeUntilNextTarget = (lookAhead.entryTime + offsetSeconds) - currentTime;
                                holdTimeOverride = Math.Max(0.005, timeUntilNextTarget + 0.0015);
                            }

                            double prevTimeDiff = targetTime - lastTargetTime;
                            lastTargetTime = targetTime;

                            if (nextFloor.holdLength > -1 && lookAhead != null && lookAhead.holdLength == -1)
                            {
                                lastHitFloor = lookAhead.seqID;
                                floorToCheck = lastHitFloor;
                                lastTargetTime = lookAhead.entryTime;
                            }
                            else
                            {
                                lastHitFloor = nextFloor.seqID;
                                floorToCheck = lastHitFloor;
                            }

                            byte vkCode = 0xDD;

                            if (floorToKeyMap.TryGetValue(nextFloor.seqID, out byte mappedKey))
                            {
                                vkCode = mappedKey;
                            }
                            else
                            {
                                vkCode = InputSimulator.UnityKeyCodeToVK(Main.Settings.Keys[0] != KeyCode.None ? Main.Settings.Keys[0] : KeyCode.E);
                            }

                            double timeDiffToSameKey = timeDiffToNext;

                            if (floorToSameKeyDiff.TryGetValue(nextFloor.seqID, out double diff))
                            {
                                timeDiffToSameKey = diff;
                            }

                            InputSimulator.SimulateKeyPressByVK(vkCode, timeDiffToSameKey, holdTimeOverride, prevTimeDiff);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Main.Mod.Logger.Log($"[ERROR] Lỗi trong Prefix Update: {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }
}