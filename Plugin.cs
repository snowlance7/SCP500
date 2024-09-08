using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SCP500
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin PluginInstance;
        public static ManualLogSource LoggerInstance;
        private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        public static PlayerControllerB localPlayer { get { return StartOfRound.Instance.localPlayerController; } }

        public static AssetBundle? ModAssets;

        // Configs
        public static ConfigEntry<int> config500MinValue;
        public static ConfigEntry<int> config500MaxValue;
        public static ConfigEntry<string> config500LevelRarities;
        public static ConfigEntry<string> config500CustomLevelRarities;
        public static ConfigEntry<int> config500MaxAmount;
        public static ConfigEntry<int> config500MinAmount;
        public static ConfigEntry<float> config500EffectTime;

        public static ConfigEntry<bool> configRemoveDrunkness;
        public static ConfigEntry<bool> configRemoveBleeding;
        public static ConfigEntry<bool> configRemoveMovementHindered;
        public static ConfigEntry<bool> configRemovePlayerAlone;
        public static ConfigEntry<bool> configRemoveInsanity;
        public static ConfigEntry<bool> configRemoveFear;
        public static ConfigEntry<bool> configRemoveMaskEffect;

        private void Awake()
        {
            if (PluginInstance == null)
            {
                PluginInstance = this;
            }

            LoggerInstance = PluginInstance.Logger;

            harmony.PatchAll();

            //InitializeNetworkBehaviours();

            // Configs
            config500MinValue = Config.Bind("SCP-500", "Min Value", 10, "Minimum scrap value for SCP-500.");
            config500MaxValue = Config.Bind("SCP-500", "Max Value", 100, "Maximum scrap value for SCP-500.");
            config500LevelRarities = Config.Bind("SCP-500 Rarities", "Level Rarities", "ExperimentationLevel:10, AssuranceLevel:10, VowLevel:10, OffenseLevel:15, AdamanceLevel:20, MarchLevel:15, RendLevel:20, DineLevel:20, TitanLevel:20, ArtificeLevel:30, EmbrionLevel:10, All:10, Modded:10", "Rarities for each level. See default for formatting.");
            config500CustomLevelRarities = Config.Bind("SCP-500 Rarities", "Custom Level Rarities", "", "Rarities for modded levels. Same formatting as level rarities.");
            config500MaxAmount = Config.Bind("SCP-500", "Max amount", 15, "Maximum pills for SCP-500. Max is ");
            config500MinAmount = Config.Bind("SCP-500", "Min amount", 2, "Minimum pills for SCP-500");
            config500EffectTime = Config.Bind("SCP-500", "Effect time", 30f, "How long the effect lasts for SCP-500");

            configRemoveDrunkness = Config.Bind("SCP-500 Effects", "Remove Drunkness", true, "Remove Drunkness");
            configRemoveBleeding = Config.Bind("SCP-500 Effects", "Remove Bleeding", true, "Remove Bleeding");
            configRemoveMovementHindered = Config.Bind("SCP-500 Effects", "Remove Movement Hinderance", true, "Remove Movement Hinderance");
            configRemovePlayerAlone = Config.Bind("SCP-500 Effects", "Remove Player Alone", true, "Remove Player Alone");
            configRemoveInsanity = Config.Bind("SCP-500 Effects", "Remove Insanity", true, "Remove Insanity");
            configRemoveFear = Config.Bind("SCP-500 Effects", "Remove Fear", true, "Remove Fear");
            configRemoveMaskEffect = Config.Bind("SCP-500 Effects", "Remove Mask Effect", true, "Remove Mask Effect");

            // Loading Assets
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "scp500_assets"));
            if (ModAssets == null)
            {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }
            LoggerInstance.LogDebug($"Got AssetBundle at: {Path.Combine(sAssemblyLocation, "scp500_assets")}");

            // Getting SCP-500
            Item SCP500 = ModAssets.LoadAsset<Item>("Assets/ModAssets/SCP500/SCP500Item.asset");
            if (SCP500 == null) { LoggerInstance.LogError("Error: Couldnt get SCP500 from assets"); return; }
            LoggerInstance.LogDebug($"Got SCP500 prefab");

            SCP500.minValue = config500MinValue.Value;
            SCP500.maxValue = config500MaxValue.Value;

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(SCP500.spawnPrefab);
            Utilities.FixMixerGroups(SCP500.spawnPrefab);
            Items.RegisterScrap(SCP500, GetLevelRarities(config500LevelRarities.Value), GetCustomLevelRarities(config500CustomLevelRarities.Value));

            // Finished
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        public Dictionary<Levels.LevelTypes, int> GetLevelRarities(string levelsString)
        {
            try
            {
                Dictionary<Levels.LevelTypes, int> levelRaritiesDict = new Dictionary<Levels.LevelTypes, int>();

                if (levelsString != null && levelsString != "")
                {
                    string[] levels = levelsString.Split(',');

                    foreach (string level in levels)
                    {
                        string[] levelSplit = level.Split(':');
                        if (levelSplit.Length != 2) { continue; }
                        string levelType = levelSplit[0].Trim();
                        string levelRarity = levelSplit[1].Trim();

                        if (Enum.TryParse<Levels.LevelTypes>(levelType, out Levels.LevelTypes levelTypeEnum) && int.TryParse(levelRarity, out int levelRarityInt))
                        {
                            levelRaritiesDict.Add(levelTypeEnum, levelRarityInt);
                        }
                        else
                        {
                            LoggerInstance.LogError($"Error: Invalid level rarity: {levelType}:{levelRarity}");
                        }
                    }
                }
                return levelRaritiesDict;
            }
            catch (Exception e)
            {
                Logger.LogError($"Error: {e}");
                return null;
            }
        }

        public Dictionary<string, int> GetCustomLevelRarities(string levelsString)
        {
            try
            {
                Dictionary<string, int> customLevelRaritiesDict = new Dictionary<string, int>();

                if (levelsString != null)
                {
                    string[] levels = levelsString.Split(',');

                    foreach (string level in levels)
                    {
                        string[] levelSplit = level.Split(':');
                        if (levelSplit.Length != 2) { continue; }
                        string levelType = levelSplit[0].Trim();
                        string levelRarity = levelSplit[1].Trim();

                        if (int.TryParse(levelRarity, out int levelRarityInt))
                        {
                            customLevelRaritiesDict.Add(levelType, levelRarityInt);
                        }
                        else
                        {
                            LoggerInstance.LogError($"Error: Invalid level rarity: {levelType}:{levelRarity}");
                        }
                    }
                }
                return customLevelRaritiesDict;
            }
            catch (Exception e)
            {
                Logger.LogError($"Error: {e}");
                return null;
            }
        }

        /*private static void InitializeNetworkBehaviours()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
            LoggerInstance.LogDebug("Finished initializing network behaviours");
        }*/
    }
}
