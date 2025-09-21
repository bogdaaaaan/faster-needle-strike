using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

[BepInPlugin("com.bodyando.hks_fasterneedlestrike", "Faster Needle Strike", "1.1.0")]
public class FasterNeedleStrike : BaseUnityPlugin
{
    private static ManualLogSource logger;
    // Track previous equipped state
    private static bool? wasQuickToolEquipped = null;

    // Config entry for enabling/disabling the mod
    private static ConfigEntry<bool> ModEnabledConfig;
    private static ConfigEntry<float> ChargeTimeConfig;
    private static ConfigEntry<float> ChargeTimeQuickToolConfig;

    private void Awake()
    {
        logger = Logger;
        logger.LogInfo("Faster Needle Strike Mod loaded and initialized");

        // Bind the config entry (shows up in the F1 config menu)
        ModEnabledConfig = Config.Bind(
            "General",
            "ModEnabled",
            true,
            "Enable or disable the Faster Needle Strike"
        );

        ChargeTimeConfig = Config.Bind(
            "General",
            "ChargeTime",
            0.8f,
            "Charge time without Quick Tool"
        );

        ChargeTimeQuickToolConfig = Config.Bind(
            "General",
            "ChargeTimeQuickTool",
            0.4f,
            "Charge time with Quick Tool equipped"
        );

        // Subscribe to the SettingChanged events
        ModEnabledConfig.SettingChanged += (sender, args) =>
        {
            logger.LogInfo($"Faster Needle Strike Mod is now {(ModEnabledConfig.Value ? "ENABLED" : "DISABLED")}");
        };

        ChargeTimeConfig.SettingChanged += (sender, args) =>
        {
            logger.LogInfo($"Charge Time changed to: {ChargeTimeConfig.Value}");
        };

        ChargeTimeQuickToolConfig.SettingChanged += (sender, args) =>
        {
            logger.LogInfo($"Charge Time with tool changed to: {ChargeTimeQuickToolConfig.Value}");
        };

        Harmony.CreateAndPatchAll(typeof(FasterNeedleStrike), "com.bodyando.hks_fasterneedlestrike");
        logger.LogDebug("Harmony patches applied.");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HeroController), "get_CurrentNailChargeTime")]
    private static void IncreaseNailChargeTime(ref float __result, HeroController __instance)
    {
        if (__instance == null)
        {
            logger?.LogWarning("HeroController instance is null in IncreaseNailChargeTime.");
            return;
        }

        bool isEquipped = __instance.NailChargeTimeQuickTool.IsEquipped;
        float original = __result;

        // Only log if the equipped state has changed
        if (wasQuickToolEquipped == null || wasQuickToolEquipped != isEquipped)
        {
            // Always log the original value, regardless of mod state
            logger?.LogInfo($"[Mod {(ModEnabledConfig.Value ? "ENABLED" : "DISABLED")}] Current charge time (value set by game): {original} (Quick Tool equipped: {isEquipped})");

            // Only log the reduction if the mod is enabled
            if (ModEnabledConfig.Value)
            {
                if (isEquipped)
                {
                    logger?.LogInfo($"Quick Tool equipped: Charge time reduced from {original} to {ChargeTimeQuickToolConfig.Value}");
                }
                else
                {
                    logger?.LogInfo($"Quick Tool not equipped: Charge time reduced from {original} to {ChargeTimeConfig.Value}");
                }
            }

            wasQuickToolEquipped = isEquipped;
        }

        if (!ModEnabledConfig.Value)
            return;

        __result = isEquipped ? ChargeTimeQuickToolConfig.Value : ChargeTimeConfig.Value;
    }
}