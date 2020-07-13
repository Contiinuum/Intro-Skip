using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using Harmony;

namespace AudicaModding
{
    public class AudicaMod : MelonMod
    {
    
        //for intro skipping
        static public bool hardcoreFullComboMode = false;
        static public bool introSkip = false;
        static public bool introSkipped = false;
        static public bool isPlaying = false;
        static public bool skipQueued = false;
        static public bool canSkip = false;
        static public GameObject popup;
        static public VRHardwareSetup.VRHardwareMode hardware;
        static public string button = "";


        //for menu handling
        public static MenuState.State menuState;
        public static OptionsMenu optionMenu;
        public static MenuState.State oldMenuState;
        public static bool menuSpawned = false;
        public static OptionsMenuButton toggleButton = null;
        public static bool miscPageFound = false;

        

        public static class BuildInfo
        {
            public const string Name = "IntroSkip";  // Name of the Mod.  (MUST BE SET)
            public const string Author = "Continuum"; // Author of the Mod.  (Set as null if none)
            public const string Company = null; // Company that made the Mod.  (Set as null if none)
            public const string Version = "0.1.0"; // Version of the Mod.  (MUST BE SET)
            public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
        }

        private void CreateConfig()
        {
            ModPrefs.RegisterPrefBool("IntroSkip", "enabled", false);
        }

        private void LoadConfig()
        {
            introSkip = ModPrefs.GetBool("IntroSkip", "enabled");
            //MelonModLogger.Log("Loaded config!: " + "IntroSkip is " + (introSkip ? "enabled" : "disabled"));
        }

        private void SaveConfig()
        {
            ModPrefs.SetBool("IntroSkip", "enabled", introSkip);
            //MelonModLogger.Log("Config saved");
        }

        public override void OnApplicationStart()
        {
            HarmonyInstance instance = HarmonyInstance.Create("AudicaMod");
            Hooks.ApplyHooks(instance);
        }

        public override void OnLevelWasLoaded(int level)
        {
            if (!ModPrefs.HasKey("IntroSkip", "enabled"))
            {
                CreateConfig();
            }
            else
            {
                LoadConfig();
            }
        }

        public static int GetFirstTick()
        {
            SongList.SongData songData = SongDataHolder.I.songData;
            KataConfig.Difficulty diff = KataConfig.I.GetDifficulty();
            return SongCues.GetCues(songData, diff)[0].tick;
        }

        public static float GetCurrentTick()
        {
            SongList.SongData songData = SongDataHolder.I.songData;
            return AudioDriver.I.GetTick(AudioDriver.TickContext.Audio);
        }

        //Queues an intro skip for when AudioDriver is not instantiated yet
        public static void QueueSkip()
        {
            skipQueued = true;
            //MelonModLogger.Log("Skip queued");
        }

        public static void SkipIntro()
        {
            if (GetCurrentTick() <= GetFirstTick() - 3840)
            {             
                AudioDriver.I.JumpToTick(AudicaMod.GetFirstTick() - 1920);
                AudicaMod.introSkipped = true;
                AudicaMod.skipQueued = false;
                //MelonModLogger.Log("Intro skipped!");
            }
        }

        public static void TriggerRestart()
        {
            //MelonModLogger.Log("Restart triggered");
            introSkipped = false;
        }
        
        public override void OnUpdate()
        {
            if (MenuState.sState == 0) return;

            if (introSkip)
            {
                if (!isPlaying && MenuState.sState == MenuState.State.Launched && AudioDriver.I is AudioDriver)
                {
                    isPlaying = true;
                }

                else if (isPlaying && (MenuState.sState != MenuState.State.Launched || AudioDriver.I is null))
                {
                    isPlaying = false;
                    introSkipped = false;
                }

                if (isPlaying)
                {
                    if (GetCurrentTick() < GetFirstTick() - 3840 && !skipQueued && !introSkipped)
                    {
                        canSkip = true;
                        if (popup is null || !popup.activeSelf)
                        {
                            if (button == "")
                            {
                                switch (VRHardwareSetup.I.hardware)
                                {
                                    case VRHardwareSetup.VRHardwareMode.Cosmos:
                                        button = "X";
                                        break;
                                    case VRHardwareSetup.VRHardwareMode.Knuckles:
                                    case VRHardwareSetup.VRHardwareMode.OculusNative:
                                    case VRHardwareSetup.VRHardwareMode.OculusOpenVR:
                                        button = "A";
                                        break;
                                    case VRHardwareSetup.VRHardwareMode.ViveWand:
                                    case VRHardwareSetup.VRHardwareMode.WinMROpenVR:
                                        button = "R Menu Button";
                                        break;
                                    case VRHardwareSetup.VRHardwareMode.Unknown:
                                        button = "?";
                                        break;
                                }
                            }
                            popup = KataConfig.I.CreateDebugText("Skip Intro by pressing <color=#85e359>" + button + "</color>", new Vector3(0f, -1f, 5f), 3f, null, false, 0.001f).gameObject;
                        }
                    }
                    else
                    {
                        if (canSkip) canSkip = false;
                        if (popup is GameObject)
                        {
                            GameObject.Destroy(popup);
                            popup = null;
                        }

                    }
                }
            }

            if (MenuState.sState == MenuState.State.Launched) return;
            if (MenuState.sState != MenuState.State.SettingsPage) miscPageFound = false;
            if (MenuState.sState == MenuState.State.SettingsPage && !miscPageFound)
            {
                if (OptionsMenu.I is OptionsMenu) optionMenu = OptionsMenu.I;
                else return;

                if (optionMenu.mPage == OptionsMenu.Page.Misc)
                {
                    miscPageFound = true;
                }
                

                if (miscPageFound && !menuSpawned)
                {

                    if (optionMenu.mPage == OptionsMenu.Page.Misc)
                    {


                        string toggleText = "OFF";

                        if (introSkip)
                        {
                            toggleText = "ON";
                        }

                        optionMenu.AddHeader(0, "Skip Intro");

                        toggleButton = optionMenu.AddButton
                            (0,
                            toggleText,
                            new Action(() =>
                            {
                                if (introSkip)
                                {
                                    introSkip = false;
                                    toggleButton.label.text = "OFF";
                                    SaveConfig();
                                }
                                else
                                {
                                    introSkip = true;
                                    toggleButton.label.text = "ON";
                                    SaveConfig();
                                }
                            }),
                            null,
                            "Skip Intro by pressing:\n" +
                            "A on Oculus and Index\n" +
                            "R Menu Button on Vive\n" +
                            "");

                        menuSpawned = true;
                    }
                    
                }
                else if (!miscPageFound)
                {
                    menuSpawned = false;
                }

            }

            
        }
    }
}



