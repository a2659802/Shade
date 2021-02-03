using System;
using Modding;
using UnityEngine;

namespace LordOfShade
{
    //Copied from 56's pale prince :)
    //https://github.com/5FiftySix6/HollowKnight.Pale-Prince/blob/master/Pale%20Prince/SaveSettings.cs

    [Serializable]
    public class SaveSettings : ModSettings
    {
        public BossStatue.Completion ShadeLordCompletion = new BossStatue.Completion
        {
            isUnlocked = true
        };
    }
}