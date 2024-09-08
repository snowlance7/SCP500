using BepInEx.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using static SCP500.Plugin;

namespace SCP500
{
    internal class SCP500Behavior : PhysicsProp
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;

        public List<GameObject> PillsInBottle;
        public AudioClip PillSwallowSFX;

        public override void Start()
        {
            base.Start();

            int pillAmount = UnityEngine.Random.Range(config500MinAmount.Value, config500MaxAmount.Value);

            int pillsToTakeOut = PillsInBottle.Count - pillAmount;

            for (int i = 0; i < pillsToTakeOut; i++)
            {
                RemovePillFromBottle();
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);

            if (buttonDown && !itemUsedUp)
            {
                RemovePillFromBottle();
                SCP500Controller.TakePill();
                playerHeldBy.statusEffectAudio.PlayOneShot(PillSwallowSFX, 1f);

                if (PillsInBottle.Count == 0)
                {
                    itemUsedUp = true;
                }
            }
        }

        private void RemovePillFromBottle()
        {
            GameObject pill = PillsInBottle.Last();
            PillsInBottle.Remove(pill);
            Destroy(pill);
        }
    }
}
