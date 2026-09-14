using System;
using System.Collections.Generic;
using UnityEngine;

namespace ADOFAI_Macro_Internal
{
    public static class AdoMacro3Engine
    {
        public static double OneHandBPM => (Main.Settings != null ? Main.Settings.OneHandBPM : 500.0);
        public static double MinPressBPM => (Main.Settings != null ? Main.Settings.MinPressBPM : 200.0);
        public static int FingerNumber => (Main.Settings != null && Main.Settings.FingersPerHand > 0) ? Math.Min(16, Math.Max(1, Main.Settings.FingersPerHand)) : 16;
        public const int LeftKeysCount = 16;
        public const int RightKeysCount = 16;
        public const int MaxHandNumber = 2;
        public const bool IncreaseHandSpeed = true;

        public struct InputNote
        {
            public int hand;
            public int finger;
            public double push_time;
            public double release_time;
            public byte vkCode;
        }

        public static double BPM_to_Time(double bpm)
        {
            if (bpm <= 0.0001)
            {
                bpm = 120.0;
            }
            return 60000.0 / bpm;
        }

        public static double HandSpeed(double bpm)
        {
            return BPM_to_Time(bpm);
        }

        public static double SetHandBPM(double bpm, double abs_bpm, double prev_bpm)
        {
            double targetLimit = OneHandBPM;
            if (bpm <= 0.0001)
            {
                return targetLimit;
            }

            while (bpm > targetLimit)
            {
                bpm /= 2.0;
            }

            while (bpm < targetLimit)
            {
                bpm *= 2.0;
            }

            if (bpm - targetLimit > targetLimit - bpm / 2.0)
            {
                bpm /= 2.0;
            }

            if (abs_bpm <= 0.0001)
            {
                return targetLimit;
            }

            while (abs_bpm > targetLimit)
            {
                abs_bpm /= 2.0;
            }

            while (abs_bpm < targetLimit)
            {
                abs_bpm *= 2.0;
            }

            if (abs_bpm - targetLimit > targetLimit - abs_bpm / 2.0)
            {
                abs_bpm /= 2.0;
            }

            if (prev_bpm == 0)
            {
                prev_bpm = abs_bpm;
            }

            prev_bpm -= 20.0;

            if (Math.Abs(abs_bpm - prev_bpm) < Math.Abs(bpm - prev_bpm))
            {
                bpm = prev_bpm;
            }

            bpm += 20.0;
            return bpm;
        }

        public static List<InputNote> GenerateInputList(List<scrFloor> validTiles)
        {
            var inputList = new List<InputNote>();

            if (validTiles == null || validTiles.Count == 0)
            {
                return inputList;
            }

            for (int idx = 0; idx < validTiles.Count; idx++)
            {
                inputList.Add(new InputNote{hand = 0, finger = 0, push_time = validTiles[idx].entryTime * 1000.0, release_time = -1, vkCode = 0});
            }

            double hand_duration = 0;
            int finger = 0;
            double hand_speed_coefficient = 1.0;
            double hand_bpm = 0;
            bool reset_hand_speed_coefficient = false;
            double prev_duration = 0;

            for (int i = 0; i < inputList.Count - 1; i++)
            {
                double duration = inputList[i + 1].push_time - inputList[i].push_time;
                if (duration <= 0.001)
                {
                    duration = 1.0;
                }
                
                double currentBpm = 60000.0 / duration;
                hand_bpm = SetHandBPM(currentBpm, currentBpm, hand_bpm);
                double hand_speed = HandSpeed(hand_bpm) * hand_speed_coefficient;

                if (prev_duration == 0)
                {
                    prev_duration = duration;
                }

                hand_duration += duration;

                if (hand_duration < hand_speed)
                {
                    var note = inputList[i + 1];
                    note.finger = ++finger;
                    inputList[i + 1] = note;

                    if (finger >= FingerNumber)
                    {
                        for (int j = i + 1; j >= 0 && inputList[j].finger > 0; j--)
                        {
                            var n = inputList[j];
                            n.finger = 0;
                            inputList[j] = n;
                        }

                        i -= finger;

                        if (i < -1)
                        {
                            i = -1;
                        }

                        hand_speed_coefficient /= 2.0;
                        finger = 0;
                        hand_duration = 0;
                        prev_duration = 0;
                        reset_hand_speed_coefficient = false;
                        continue;
                    }
                }
                else if (reset_hand_speed_coefficient)
                {
                    hand_speed_coefficient = 1.0;
                    reset_hand_speed_coefficient = false;
                    hand_duration = 0;
                    prev_duration = 0;
                    finger = 0;
                    continue;
                }
                else
                {
                    reset_hand_speed_coefficient = true;
                    hand_duration = 0;
                    prev_duration = 0;
                    finger = 0;
                    continue;
                }

                prev_duration = duration;
            }

            int hand = MaxHandNumber - 1;

            for (int i = 0; i < inputList.Count; i++)
            {
                var note = inputList[i];
                note.finger %= FingerNumber;

                if (note.finger == 0)
                {
                    hand++;
                    hand %= MaxHandNumber;
                }

                note.hand = hand;
                inputList[i] = note;
            }

            int rollStyle = (Main.Settings != null) ? Main.Settings.RollStyle : 0;
            var rng = (rollStyle == 4) ? new System.Random() : null;
            int currentEffectiveStyle = (rollStyle == 4) ? rng.Next(0, 4) : rollStyle;
            double styleStartTime = (inputList.Count > 0) ? inputList[0].push_time : 0;
            double switchIntervalMs = (Main.Settings != null && Main.Settings.RandomRollIntervalSec > 1.0f) ? (Main.Settings.RandomRollIntervalSec * 1000.0) : 8000.0;
            int begin = 0;
            
            for (int i = 0; i < inputList.Count; i++)
            {
                if (inputList[i].finger == 0 && i > begin)
                {
                    int burstLen = i - begin;
                    ApplyStyleToBurst(inputList, begin, burstLen, currentEffectiveStyle);
                    begin = i;

                    if (rollStyle == 4)
                    {
                        double currentTimeMs = inputList[begin].push_time;
                        double gapFromPrev = (begin > 0) ? (currentTimeMs - inputList[begin - 1].push_time) : 0;
                        bool intervalPassed = (currentTimeMs - styleStartTime >= switchIntervalMs);
                        bool isNaturalPause = (gapFromPrev >= 250.0);

                        if (intervalPassed || (currentTimeMs - styleStartTime >= switchIntervalMs * 0.75 && isNaturalPause))
                        {
                            int nextStyle;

                            do
                            {
                                nextStyle = rng.Next(0, 4);
                            }
                            while (nextStyle == currentEffectiveStyle);

                            currentEffectiveStyle = nextStyle;
                            styleStartTime = currentTimeMs;
                        }
                    }
                }
            }

            if (begin < inputList.Count)
            {
                ApplyStyleToBurst(inputList, begin, inputList.Count - begin, currentEffectiveStyle);
            }

            var leftKeys = new List<byte>();
            var rightKeys = new List<byte>();

            for (int k = 0; k < LeftKeysCount; k++)
            {
                if (Main.Settings != null && Main.Settings.Keys[k] != KeyCode.None)
                {
                    leftKeys.Add(InputSimulator.UnityKeyCodeToVK(Main.Settings.Keys[k]));
                }
            }

            for (int k = LeftKeysCount; k < LeftKeysCount + RightKeysCount; k++)
            {
                if (Main.Settings != null && Main.Settings.Keys[k] != KeyCode.None)
                {
                    rightKeys.Add(InputSimulator.UnityKeyCodeToVK(Main.Settings.Keys[k]));
                }
            }

            if (leftKeys.Count == 0)
            {
                leftKeys.Add(InputSimulator.UnityKeyCodeToVK(KeyCode.E));
            }

            if (rightKeys.Count == 0)
            {
                rightKeys.Add(InputSimulator.UnityKeyCodeToVK(KeyCode.RightBracket));
            }

            bool isMainLeft = (Main.Settings == null || Main.Settings.MainHand == 0);

            for (int k = 0; k < inputList.Count; k++)
            {
                var note = inputList[k];
                bool noteIsLeft = isMainLeft ? (note.hand == 0) : (note.hand == 1);
                var keys = noteIsLeft ? leftKeys : rightKeys;
                note.vkCode = keys[note.finger % keys.Count];
                inputList[k] = note;
            }

            return inputList;
        }

        private static void ApplyStyleToBurst(List<InputNote> inputList, int begin, int burstLen, int rollStyle)
        {
            if (burstLen <= 1)
            {
                return;
            }

            int hand = inputList[begin].hand;
            bool isMainLeft = (Main.Settings == null || Main.Settings.MainHand == 0);
            bool noteIsLeft = isMainLeft ? (hand == 0) : (hand == 1);
            bool isDescending = false;

            switch (rollStyle)
            {
                case 0:
                    isDescending = true;
                    break;
                case 1:
                    isDescending = false;
                    break;
                case 2:
                    isDescending = noteIsLeft;
                    break;
                case 3:
                    isDescending = !noteIsLeft;
                    break;
                default:
                    isDescending = false;
                    break;
            }

            if (isDescending)
            {
                for (int k = 0; k < burstLen; k++)
                {
                    var note = inputList[begin + k];
                    note.finger = (burstLen - 1) - k;
                    inputList[begin + k] = note;
                }
            }
        }
    }
}