using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalSettings;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("com.bogdaaaaan.hks_fastercharge", "HKS Faster Charge Mod", "1.0.0")]
public class HKS_FasterCharge : BaseUnityPlugin
{
    private static ManualLogSource logger;
    // Track previous equipped state
    private static bool? wasQuickToolEquipped = null;

    private static readonly float NEW_CHARGE_TIME = 0.8f;
    private static readonly float NEW_CHARGE_TIME_QUICK_TOOL = 0.4f;

    // Config entry for enabling/disabling the mod
    private static ConfigEntry<bool> ModEnabledConfig;

    private void Awake()
    {
        logger = Logger;
        logger.LogInfo("Plugin loaded and initialized");

        // Bind the config entry (shows up in the F1 config menu)
        ModEnabledConfig = Config.Bind(
            "General",
            "ModEnabled",
            true,
            "Enable or disable the HKS Faster Charge Mod"
        );

        // Subscribe to the SettingChanged event
        ModEnabledConfig.SettingChanged += (sender, args) =>
        {
            logger.LogInfo($"HKS Faster Charge Mod is now {(ModEnabledConfig.Value ? "ENABLED" : "DISABLED")}");
        };

        Harmony.CreateAndPatchAll(typeof(HKS_FasterCharge), null);
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
                    logger?.LogInfo($"Quick Tool equipped: Charge time reduced from {original} to {NEW_CHARGE_TIME_QUICK_TOOL}");
                }
                else
                {
                    logger?.LogInfo($"Quick Tool not equipped: Charge time reduced from {original} to {NEW_CHARGE_TIME}");
                }
            }

            wasQuickToolEquipped = isEquipped;
        }

        if (!ModEnabledConfig.Value)
            return;

        __result = isEquipped ? NEW_CHARGE_TIME_QUICK_TOOL : NEW_CHARGE_TIME;
    }
}