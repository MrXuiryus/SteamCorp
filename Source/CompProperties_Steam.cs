using Verse;

namespace SteamCorp
{
    public class CompProperties_Steam : CompProperties
    {
        public bool transmitsSteam;

        public float baseSteamConsumption;

        public SoundDef soundPowerOn;

        public SoundDef soundPowerOff;

        public SoundDef soundAmbientPowered;

        public bool allowPipeUnderneath;

        public CompProperties_Steam()
        {
            compClass = typeof(CompSteam);
        } 
    }
}
