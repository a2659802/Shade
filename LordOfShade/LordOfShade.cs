using System;
using System.Diagnostics;
using System.Reflection;
using Modding;
using JetBrains.Annotations;
using ModCommon;
using MonoMod.RuntimeDetour;
using UnityEngine.SceneManagement;
using UnityEngine;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using UObject = UnityEngine.Object;
using System.Collections.Generic;
using System.IO;

namespace LordOfShade
{
    [UsedImplicitly]
    public class LordOfShade : Mod<SaveSettings>, ITogglableMod
    {
        public static Dictionary<string, GameObject> preloadedGO = new Dictionary<string, GameObject>();

        public static LordOfShade Instance;

        //public static readonly List<Sprite> SPRITES = new List<Sprite>();

        public override string GetVersion()
        {
            return "0.0.0.0";
        }

        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>
            {
                ("",""),
            };
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Storing GOs");
            preloadedGO.Add("shade", null);
            Instance = this;
            Log("Initalizing.");

            Unload();
            ModHooks.Instance.AfterSavegameLoadHook += AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook += AddComponent;
            ModHooks.Instance.LanguageGetHook += LangGet;
            ModHooks.Instance.SetPlayerVariableHook += SetVariableHook;
            ModHooks.Instance.GetPlayerVariableHook += GetVariableHook;
        }

        private string LangGet(string key, string sheettitle)
        {
            switch (key)
            {
                case "SHADE_NAME": return "Shade";
                case "SHADE_DESC": return "Lingering will of a mind long gone.";
                default: return Language.Language.GetInternal(key, sheettitle);
            }
        }

        private void AfterSaveGameLoad(SaveGameData data) => AddComponent();

        private void AddComponent()
        {
            GameManager.instance.gameObject.AddComponent<GOSetup>();
            GameManager.instance.gameObject.AddComponent<ShadeFinder>();
        }

        private object SetVariableHook(Type t, string key, object obj)
        {
            if (key == "ShadeLordCompl")
                Settings.Completion = (BossStatue.Completion)obj;
            return obj;
        }

        private object GetVariableHook(Type t, string key, object orig)
        {
            return key == "ShadeLordCompl"
                ? Settings.Completion
                : orig;
        }

        public void Unload()
        {
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            ModHooks.Instance.AfterSavegameLoadHook -= AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook -= AddComponent;
            ModHooks.Instance.LanguageGetHook -= LangGet;
            ModHooks.Instance.SetPlayerVariableHook -= SetVariableHook;
            ModHooks.Instance.GetPlayerVariableHook -= GetVariableHook;

            // ReSharper disable once Unity.NoNullPropogation
            var x = GameManager.instance?.gameObject.GetComponent<ShadeFinder>();
            if (x == null) return;
            UObject.Destroy(x);
        }
    }
}