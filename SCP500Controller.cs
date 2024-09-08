using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static SCP500.Plugin;

namespace SCP500
{
    public class SCP500Controller : MonoBehaviour
    {
        internal static SCP500Controller? Instance;
        private float timer = 0f;

        public static bool LocalPlayerAffectedBySCP500 = false;

        private Coroutine? scp500Coroutine;

        public void Update()
        {
            if (configRemoveDrunkness.Value) { localPlayer.drunkness = 0f; }
            if (configRemoveMovementHindered.Value) { localPlayer.isMovementHindered = 0; }
            if (configRemoveInsanity.Value) { localPlayer.insanityLevel = 0f; }
            if (configRemoveFear.Value) { localPlayer.playersManager.fearLevel = 0f; }

            localPlayer.bleedingHeavily = !configRemoveBleeding.Value;
            localPlayer.isPlayerAlone = !configRemovePlayerAlone.Value;
        }

        public static void TakePill()
        {
            localPlayer.health = 100;
            HUDManager.Instance.UpdateHealthUI(localPlayer.health, false);
            localPlayer.MakeCriticallyInjured(false);
            localPlayer.hasBeenCriticallyInjured = false;

            if (Instance == null)
            {
                Instance = new GameObject("SCP500Controller").AddComponent<SCP500Controller>();
            }

            Instance.timer += config500EffectTime.Value;

            if (Instance.scp500Coroutine == null)
            {
                Instance.scp500Coroutine = Instance.StartCoroutine(Instance.ApplySCP500EffectCoroutine());
            }
        }

        private IEnumerator ApplySCP500EffectCoroutine()
        {
            LocalPlayerAffectedBySCP500 = true;

            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            LocalPlayerAffectedBySCP500 = false;
            scp500Coroutine = null;
            Instance = null;
            Destroy(gameObject);
        }
    }

    [HarmonyPatch]
    internal class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(HauntedMaskItem), nameof(HauntedMaskItem.BeginAttachment))]
        public static bool BeginAttachmentPrefix()
        {
            if (SCP500Controller.LocalPlayerAffectedBySCP500) { return !configRemoveMaskEffect.Value; }
            return true;
        }
    }
}
