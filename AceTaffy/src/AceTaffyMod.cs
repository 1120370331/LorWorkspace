using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace AceTaffy
{
    // BaseMod discovers every concrete ModInitializer in the package assembly.
    public sealed class AceTaffyMod : BaseMod.BaseModInitializer
    {
        private const string HarmonyId = "lor.acetaffy.gameplay";
        private static bool _initialized;

        public static string RootPath { get; private set; }

        public static void Log(string message)
        {
            Debug.Log("[AceTaffy] " + message);
        }

        public override void OnInitializeMod()
        {
            base.OnInitializeMod();
            if (_initialized)
                return;

            string location = Assembly.GetExecutingAssembly().Location;
            RootPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(location), ".."));
            try
            {
                TaffyVfx.Prewarm();
                var harmony = new Harmony(HarmonyId);
                TaffyMechanics.Install(harmony);
                TaffyReceptionFormation.Install(harmony);
                TaffyVfx.Install(harmony);
                TaffyPreparationAlignment.Install(harmony);
                _initialized = true;
                Log("initialized from " + RootPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("[AceTaffy] initialization failed: " + ex);
                throw;
            }
        }
    }
}
