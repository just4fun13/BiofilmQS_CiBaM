using UnityEngine;

namespace Assets.Scripts.MVVM_CA.Analytics
{
    public static class BuddrusCurves
    {
        public static string[] Names => new[]
        {
            "CC2",
            "CC1",
            "CC3"
        };

        public static Vector2[][] Ahl => new[]
        {
            CC2_AHL,
            CC1_AHL,
            CC3_AHL
        };

        public static Vector2[][] Biomass => new[]
        {
            CC2_Biomass,
            CC1_Biomass,
            CC3_Biomass
        };

        private static readonly Vector2[] CC2_AHL =
        {
            new Vector2(0f,  0.00f),
            new Vector2(13f, 0.13f),
            new Vector2(15f, 0.30f),
            new Vector2(18f, 0.50f),
            new Vector2(20f, 0.80f),
            new Vector2(22f, 0.50f),
            new Vector2(24f, 0.40f),
            new Vector2(25f, 0.28f),
            new Vector2(27f, 0.25f),
            new Vector2(30f, 0.17f),
            new Vector2(36f, 0.16f),
            new Vector2(38f, 0.22f),
            new Vector2(40f, 0.08f),
            new Vector2(42f, 0.14f)
        };

        private static readonly Vector2[] CC2_Biomass =
        {
            new Vector2( 0f, 0.00f),
            new Vector2(13f, 0.26f),
            new Vector2(15f, 0.36f),
            new Vector2(18f, 0.49f),
            new Vector2(20f, 0.48f),
            new Vector2(22f, 0.49f),
            new Vector2(24f, 0.48f),
            new Vector2(25f, 0.48f),
            new Vector2(27f, 0.49f),
            new Vector2(30f, 0.49f),
            new Vector2(36f, 0.49f),
            new Vector2(38f, 0.50f),
            new Vector2(40f, 0.49f),
            new Vector2(42f, 0.48f)
        };

        private static readonly Vector2[] CC1_AHL =
        {
            new Vector2(0.3f, 0.135f),
            new Vector2(5.2f, 0.162f),
            new Vector2(7f, 0.160f),
            new Vector2(8f, 0.160f),
            new Vector2(9f, 0.160f),
            new Vector2(10.1f, 0.181f),
            new Vector2(11.1f, 0.400f),
            new Vector2(12f, 0.291f),
            new Vector2(14.3f, 0.368f),
            new Vector2(15f, 0.473f),
            new Vector2(16.1f, 0.701f),
            new Vector2(17.1f, 1.083f),
            new Vector2(18f, 0.503f),
            new Vector2(19f, 0.664f),
            new Vector2(20.1f, 0.558f),
            new Vector2(21.1f, 0.427f),
            new Vector2(22.1f, 0.524f),
            new Vector2(23f, 0.414f),
            new Vector2(23.9f, 0.621f),
            new Vector2(30.1f, 0.402f),
            new Vector2(31.1f, 0.425f),
            new Vector2(32.1f, 0.371f),
            new Vector2(33.1f, 0.312f),
            new Vector2(34.1f, 0.409f),
            new Vector2(36f, 0.181f),
            new Vector2(37.1f, 0.366f),
            new Vector2(38.1f, 0.265f),
            new Vector2(40f, 0.630f)
        };

        private static readonly Vector2[] CC1_Biomass =
        {
            new Vector2(0f, 0.00f),
            new Vector2(5f, 0.00f),
            new Vector2(8f, 0.05f),
            new Vector2(10f, 0.20f),
            new Vector2(11f, 0.38f),
            new Vector2(13f, 0.47f),
            new Vector2(15.1f, 1.11f),
            new Vector2(16f, 1.50f),
            new Vector2(18f, 0.81f),
            new Vector2(19f, 1.07f),
            new Vector2(20.1f, 1.66f),
            new Vector2(21.1f, 3.01f),
            new Vector2(22.1f, 2.76f),
            new Vector2(23f, 2.57f),
            new Vector2(30f, 1.98f),
            new Vector2(31.1f, 1.21f),
            new Vector2(32.1f, 1.79f),
            new Vector2(33.1f, 1.84f),
            new Vector2(34.1f, 2.49f),
            new Vector2(35f, 1.59f),
            new Vector2(36.1f, 1.83f),
            new Vector2(37.1f, 1.53f),
            new Vector2(38.1f, 1.88f),
            new Vector2(40f, 1.80f)
        };

        private static readonly Vector2[] CC3_AHL =
        {
            new Vector2(0f, 0.000f),
            new Vector2(15.8f, 0.235f),
            new Vector2(18f, 0.495f),
            new Vector2(18.9f, 0.771f),
            new Vector2(20f, 0.344f),
            new Vector2(20.9f, 0.110f),
            new Vector2(21.9f, 0.155f),
            new Vector2(22.9f, 0.465f),
            new Vector2(23.9f, 0.974f),
            new Vector2(25f, 0.395f),
            new Vector2(28f, 1.411f),
            new Vector2(29f, 0.973f),
            new Vector2(29.9f, 1.168f),
            new Vector2(30.9f, 1.188f),
            new Vector2(32f, 1.220f),
            new Vector2(32.9f, 1.901f),
            new Vector2(34f, 3.235f),
            new Vector2(40.1f, 2.228f),
            new Vector2(41.9f, 3.444f),
            new Vector2(43f, 2.049f),
            new Vector2(44f, 1.283f),
            new Vector2(44.9f, 2.617f),
            new Vector2(45.9f, 4.396f),
            new Vector2(47f, 2.825f),
            new Vector2(47.9f, 2.056f),
            new Vector2(50f, 2.800f)
        };

        private static readonly Vector2[] CC3_Biomass =
        {
            new Vector2(0f, 0.000f),
            new Vector2(20f, 0.000f),
            new Vector2(22f, 0.020f),
            new Vector2(24f, 0.030f),
            new Vector2(26f, 0.019f),
            new Vector2(28f, 0.040f),
            new Vector2(29.9f, 0.080f),
            new Vector2(30.9f, 0.114f),
            new Vector2(32f, 0.200f),
            new Vector2(33f, 0.320f),
            new Vector2(33.9f, 0.454f),
            new Vector2(36f, 0.750f),
            new Vector2(38f, 0.950f),
            new Vector2(39.9f, 1.106f),
            new Vector2(41f, 1.300f),
            new Vector2(43f, 1.537f),
            new Vector2(44f, 1.518f),
            new Vector2(45f, 1.550f),
            new Vector2(45.9f, 1.447f),
            new Vector2(47f, 1.476f),
            new Vector2(47.9f, 1.328f),
            new Vector2(49f, 1.250f),
            new Vector2(50f, 1.200f)
        };
    }
}