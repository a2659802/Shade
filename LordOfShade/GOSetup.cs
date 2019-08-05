using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using ModCommon.Util;
using ModCommon;
using Modding;
using UnityEngine;
using UnityEngine.UI;
using Logger = Modding.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using System.IO;
using System;
using On;

namespace LordOfShade
{
    internal class GOSetup : MonoBehaviour
    {
        void Start()
        {
            StartCoroutine(LoadGO());
        }
        IEnumerator LoadGO()
        {
            GameObject go = new GameObject();
            yield return null;
            foreach (GameObject i in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (i.name == "Hollow Shade")
                {
                    LordOfShade.preloadedGO["shade"] = Instantiate(i);
                    go = LordOfShade.preloadedGO["shade"];
                    DontDestroyOnLoad(go);
                    go.SetActive(false);
                    Logger.Log("Setup " + go.name);
                }
            }

            var fsm = go.LocateMyFSM("Shade Control");
            LordOfShade.preloadedGO["slash"] =  Instantiate(fsm.GetAction<ActivateGameObject>("Slash", 0).gameObject.GameObject.Value);
            DontDestroyOnLoad(LordOfShade.preloadedGO["slash"]);
            LordOfShade.preloadedGO["slash"].SetActive(false);
            Logger.Log("Setup " + LordOfShade.preloadedGO["slash"].name);

        }
        private void OnDestroy()
        {

        }
    }
}
