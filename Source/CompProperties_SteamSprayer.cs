namespace SteamCorp
{
    public class CompProperties_SteamSprayer : CompProperties_Steam
    {
        public int MinTicksBetweenSprays;

        public int MaxTicksBetweenSprays;

        public int MinSprayDuration;

        public int MaxSprayDuration;

        public float SmokeAmount;

        public float PressureCutoff;

        public CompProperties_SteamSprayer()
        {
            compClass = typeof(CompSteamSprayer);
        }
    }
}