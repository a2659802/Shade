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
using System.Collections;
using ModCommon.Util;
using HutongGames.PlayMaker.Actions;

namespace LordOfShade
{
    [UsedImplicitly]
    public class LordOfShade :Mod, ITogglableMod
    {
        public static Dictionary<string, GameObject> preloadedGO = new Dictionary<string, GameObject>();

        public static LordOfShade Instance;

        //public static readonly List<Sprite> SPRITES = new List<Sprite>();

        public override string GetVersion()
        {
            return "0.2.0.0";
        }
        public override List<(string, string)> GetPreloadNames()
        {
            return new List<(string, string)>()
            {
                ("Fungus3_44", "shadow_gate"),
                ("Abyss_10","Abyss Tendrils")
            };
        }
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Storing GOs");
            preloadedGO.Add("shade", null);
            var gate = preloadedObjects["Fungus3_44"]["shadow_gate"];
            foreach (Transform t in gate.transform)
            {
                if (t.name.StartsWith("shad"))
                    UObject.Destroy(t.gameObject);
            }
            gate.SetActive(false);
            preloadedGO.Add("gate", gate);

            var tendrils = preloadedObjects["Abyss_10"]["Abyss Tendrils"];
            UObject.Destroy(tendrils.LocateMyFSM("Black Charm"));
            UObject.Destroy(tendrils.transform.Find("Alert Range").gameObject);
            //tendrils.LocateMyFSM("Control").Fsm.GetState("Idle").Transitions = new HutongGames.PlayMaker.FsmTransition[0];
            tendrils.LocateMyFSM("Control").Fsm.SaveActions();
            tendrils.transform.Find("Extra Box").gameObject.AddComponent<NonBouncer>();
            
            preloadedGO.Add("tendrils", tendrils);

            Instance = this;
            Log("Initalizing.");

            //Unload();
            ModHooks.Instance.AfterSavegameLoadHook += AfterSaveGameLoad;
            ModHooks.Instance.NewGameHook += AddComponent;
            ModHooks.Instance.LanguageGetHook += LangGet;
            ModHooks.Instance.SetPlayerVariableHook += SetVariableHook;
            ModHooks.Instance.GetPlayerVariableHook += GetVariableHook;

            // ModHooks.Instance.HeroUpdateHook += TestShade;
            new GameObject().AddComponent<NonBouncer>().StartCoroutine(LoadGO());
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
            //GameManager.instance.gameObject.AddComponent<GOSetup>();
            GameManager.instance.gameObject.AddComponent<ShadeFinder>();
        }
        IEnumerator LoadGO()
        {
            
            GameObject go = new GameObject();
            yield return null;
            foreach (GameObject i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.name == "Hollow Shade")
                {
                    LordOfShade.preloadedGO["shade"] = UObject.Instantiate(i);
                    go = LordOfShade.preloadedGO["shade"];
                    UObject.DontDestroyOnLoad(go);
                    go.SetActive(false);
                    Log("Setup " + go.name);
                }

                if (i.name.Contains("Shadow Ball"))
                {
                    LordOfShade.preloadedGO["ball"] = UObject.Instantiate(i);
                    UObject.DontDestroyOnLoad(LordOfShade.preloadedGO["ball"]);
                    UObject.Destroy(LordOfShade.preloadedGO["ball"].LocateMyFSM("damages_hero"));
                    var dh = preloadedGO["ball"].AddComponent<DamageHero>();
                    dh.hazardType = 1;
                    dh.damageDealt = 1;
                    LordOfShade.preloadedGO["ball"].SetActive(false);
                    Log("FOUND BALL");
                }
            }

            var fsm = go.LocateMyFSM("Shade Control");
            LordOfShade.preloadedGO["slash"] = UObject.Instantiate(fsm.GetAction<ActivateGameObject>("Slash", 0).gameObject.GameObject.Value);
            UObject.DontDestroyOnLoad(LordOfShade.preloadedGO["slash"]);
            LordOfShade.preloadedGO["slash"].SetActive(false);
            Log("Setup " + LordOfShade.preloadedGO["slash"].name);

        }
        private object SetVariableHook(Type t, string key, object obj)
        {
            if (key == "ShadeLordCompl")
            {
                //Log($"Key == Shade,{JsonUtility.ToJson(obj)}");
                Settings.ShadeLordCompletion = (BossStatue.Completion)obj;
            }
                
            return obj;
        }

        private object GetVariableHook(Type t, string key, object orig)
        {
            return key == "ShadeLordCompl"
                ? Settings.ShadeLordCompletion
                : orig;
        }
        public SaveSettings Settings = new SaveSettings();
        public override ModSettings SaveSettings { get => Settings; set => Settings = (SaveSettings)value; }
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