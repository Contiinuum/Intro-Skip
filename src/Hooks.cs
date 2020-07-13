using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Harmony;
using MelonLoader;
using UnityEngine;

namespace AudicaModding
{
    internal static class Hooks
    {
        public static void ApplyHooks(HarmonyInstance instance)
        {
            instance.PatchAll(Assembly.GetExecutingAssembly());
        }
        
        [HarmonyPatch(typeof(AudioDriver), "StartPlaying")]
        private static class PatchStartPlaying
        {
            private static void Prefix(AudioDriver __instance)
            {
                if (AudicaMod.skipQueued)
                {
                    AudicaMod.SkipIntro();
                }
            }

        }

        [HarmonyPatch(typeof(LaunchPanel), "Play")]
        private static class PatchLaunchPlay
        {
            private static void Prefix(LaunchPanel __instance)
            {
                AudicaMod.cachedFirstTick = 0;
            }
        }
        
        [HarmonyPatch(typeof(OVRInput), "Update")]
        private static class PatchOVRUpdate
        {
            private static bool Prefix(OVRInput __instance)
            {
                if (!AudicaMod.introSkip || AudicaMod.introSkipped || AudicaMod.skipQueued || !AudicaMod.canSkip) return true;

                if (OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.All))
                {
                    if (AudioDriver.I is null) AudicaMod.QueueSkip();
                    else AudicaMod.SkipIntro();
                }

                return true;
            }
        }
        
        [HarmonyPatch(typeof(InGameUI), "Restart")]
        private static class PatchRestart
        {
            private static void Prefix(InGameUI __instance)
            {
                AudicaMod.TriggerRestart();
            }
        }
        

    }
}
