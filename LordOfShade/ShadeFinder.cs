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
using System.Reflection;
using System.IO;

namespace LordOfShade
{
    internal class ShadeFinder : MonoBehaviour
    {
        Texture _oldTex;
        string currentScene;
        GameObject shade;
        PlayMakerFSM dreamNail;
        Sprite _statusDisp;
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
            //bs.StatueState = LordOfShade.Instance.Settings.ShadeLordCompletion; 
            var details = new BossStatue.BossUIDetails(); 
            details.nameKey = details.nameSheet = "SHADE_NAME";
            details.descriptionKey = details.descriptionSheet = "SHADE_DESC";
            bs.bossDetails = details;
            /*foreach (var i in bs.statueDisplay.GetComponentsInChildren<SpriteRenderer>(true))
            {
                i.sprite = new Sprite();
            }*/
            if(_statusDisp == null)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                foreach (string resource in assembly.GetManifestResourceNames())
                {
                    if (!resource.EndsWith(".png"))
                    {
                        continue;
                    }

                    using (Stream stream = assembly.GetManifestResourceStream(resource))
                    {
                        if (stream == null) continue;

                        byte[] buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
                        stream.Dispose();

                        // Create texture from bytes
                        var texture = new Texture2D(1, 1);
                        texture.LoadImage(buffer, true);
                        // Create sprite from texture
                        _statusDisp = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                    }
                }
            }
            bs.statueDisplay.GetComponentInChildren<SpriteRenderer>().sprite = _statusDisp;

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
            if (BossSceneController.Instance != null)
            {
                if (BossSceneController.Instance.BossLevel > 0) 
                {
                    
                    var tendrils = Instantiate(LordOfShade.preloadedGO["tendrils"]);
                    tendrils.transform.SetParent(null);
                    tendrils.transform.localScale = new Vector3(1.5f, 4f, 1);
                    tendrils.transform.position = new Vector3(38, 16f);
                    tendrils.transform.eulerAngles = new Vector3(0, 0, 180);
                    tendrils.SetActive(true);
                    var fsm = tendrils.LocateMyFSM("Control");
                    fsm.InsertMethod("Still Close?", 0, () => fsm.SetState("Idle"));
                    fsm.SetState("Emerge");

                    //yield break;
                    //yield return new WaitForSeconds(1f);

                    tendrils = Instantiate(LordOfShade.preloadedGO["tendrils"]);
                    tendrils.transform.SetParent(null);
                    tendrils.transform.localScale = new Vector3(1.5f, 4f, 1);
                    tendrils.transform.position = new Vector3(50, 16f);
                    tendrils.transform.eulerAngles = new Vector3(0, 0, 180);
                    tendrils.SetActive(true);
                    var fsm2 = tendrils.LocateMyFSM("Control");
                    fsm2.InsertMethod("Still Close?", 0, () => fsm2.SetState("Idle"));
                    fsm2.SetState("Emerge");
                }
            }
        }
        public void SpawnShade()
        {
            shade = Instantiate(LordOfShade.preloadedGO["shade"]);
            shade.SetActive(true);
            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            shade.transform.SetPosition2D(xH + 8f, yH + 1f);
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