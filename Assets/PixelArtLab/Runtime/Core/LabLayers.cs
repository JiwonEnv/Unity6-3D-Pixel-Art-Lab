namespace PixelArtLab
{
    // Layer slots the features rely on. Another project that uses different slots changes them
    // here only; the names are set in Project Settings > Tags and Layers (Docs/Conventions.md).
    public static class LabLayers
    {
        public const int Water=4;     // Unity's built-in Water layer: kept out of reflection captures
        public const int Character=8; // player body: kept out of ground probes
        public const int Effects=13;  // weather particles, splashes, footprints: never reflected or probed
        // Ground probes (puddles, footprints, lamp placement) hit only real surfaces.
        public const int GroundProbeMask=~((1<<Character)|(1<<Water)|(1<<Effects));
    }
}
