using UnityModManagerNet;
using UnityEngine;

namespace ADOFAI_Macro_Internal
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        public bool EnableAutoplay = true;
        public int TimingOffsetMs = 0;
        public bool EnableHumanSpoof = true;
        public float SpoofJitterMs = 0.0f;
        public string Language = "VI";
        public int RollStyle = 0;
        public float RandomRollIntervalSec = 0.0f;
        public int MainHand = 1;
        public int FingersPerHand = 0;
        public double OneHandBPM = 1000.0;
        public double MinPressBPM = 1000.0;
        public KeyCode[] Keys = new KeyCode[32];

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange()
        {
        }
    }
}