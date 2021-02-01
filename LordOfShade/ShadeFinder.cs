using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker.Actions;
using Logger = Modding.Logger;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;
using ModCommon.Util;
using ModCommon;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace LordOfShade
{
    internal class ShadeFinder : MonoBehaviour
    {
        Texture _oldTex;
        string currentScene;
        GameObject shade;
        PlayMakerFSM dreamNail;

        //xH >= 244.3f && xH <= 252.7f && yH >= 6f && yH <= 7f
        private void Start()
        {
            USceneManager.activeSceneChanged += SceneChanged;
        }

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            if (arg0.name == "GG_Hollow_Knight" && arg1.name == "GG_Workshop")
            {
                //GameCameras.instance.cameraFadeFSM.Fsm.SetState("FadeIn");
                //Destroy(shade.GetComponent<Shade>());
            }

            if (arg1.name == "GG_Workshop") SetStatue();

            if (arg1.name != "GG_Hollow_Knight") return;
            if (arg0.name != "GG_Workshop") return;
            Log("Got to add");
            StartCoroutine(AddComponent());
        }

        private void SetStatue()
        {
            //Used 56's pale prince code here
            GameObject statue = Instantiate(GameObject.Find("GG_Statue_ElderHu"));
            statue.transform.SetPosition2D(25.4f, statue.transform.GetPositionY());
            var scene = ScriptableObject.CreateInstance<BossScene>();
            scene.sceneName = "GG_Hollow_Knight";
            var bs = statue.GetComponent<BossStatue>();
            bs.bossScene = scene;
            bs.statueStatePD = "ShadeLordCompl";
            var gg = new BossStatue.Completion
            {
                completedTier1 = true,
                seenTier3Unlock = true,
                completedTier2 = true,
                completedTier3 = true,
                isUnlocked = true,
                hasBeenSeen = true,
                usingAltVersion = false
            };
            bs.StatueState = gg;
            var details = new BossStatue.BossUIDetails();
            details.nameKey = details.nameSheet = "SHADE_NAME";
            details.descriptionKey = details.descriptionSheet = "SHADE_DESC";
            bs.bossDetails = details;
            foreach (var i in bs.statueDisplay.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.sprite = new Sprite();
            }

        }

        private IEnumerator AddComponent()
        {
            yield return null;
            var hkfsm = GameObject.Find("HK Prime").LocateMyFSM("Control");
            hkfsm.RemoveAction("Intro Roar End", 0);
            hkfsm.InsertMethod("Intro Idle", 0, () => Destroy(GameObject.Find("HK Prime")));
            hkfsm.SetState("Intro Roar End");
            Destroy(GameObject.Find("Godseeker Crowd"));
            yield return null;
            yield return new WaitForSeconds(0.5f);
            SpawnShade();
        }
        public void SpawnShade()
        {
            shade = Instantiate(LordOfShade.preloadedGO["shade"]);
            shade.SetActive(true);
            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            shade.transform.SetPosition2D(xH + 8f, yH + 5f);
            shade.AddComponent<Shade>();
        }
        private void OnDestroy()
        {
            USceneManager.activeSceneChanged -= SceneChanged;
        }
        public static void Log(object o)
        {
            Logger.Log("[Shade Finder] " + o);
        }
    }
}