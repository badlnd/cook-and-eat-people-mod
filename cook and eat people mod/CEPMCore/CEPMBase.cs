using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity;
using HarmonyLib;
using BepInEx;
using LethalLib.Modules;

using Logger = LC.CEPM.CEPMLoggingUtils.Logger;
using LC.CEPM.GamePatches;
using LC.CEPM.CEPMCore.UnityAssetManager;
using LC.CEPM.CEPMCore.ItemBehaviours;

// I've tried to comment as much as I can, and give as much info as I can think
// but I don't know what you may know / don't know.

namespace LC.CEPM.CEPMCore
{
    [BepInPlugin(CEPMInfo.longGuid, CEPMInfo.name, CEPMInfo.version)]
    public class CEPMBase : BaseUnityPlugin
    {
        // TODO : Create and set up networking system using evasia's netcode patcher.
        // the flamethrower's whole system must be done via this networking system,
        // wherein a [ServerRPC] function is called when used, and all the existing
        // code is called in a [ClientRPC].
        
        // we will rarely if at all need to use this instance, but it might be useful.
        public static CEPMBase instance { get; private set; }
        
        public readonly Harmony harmony = new Harmony(CEPMInfo.name);

        public static Logger BaseLogger = new Logger("CEPM");
        public static Logger PropLogger = new Logger("PROPS");

        // Awake is the entrypoint for unity scripts, that is why it is used here. 
        void Awake()
        {
            // AssetBundleLoader is the class we will use for any asset loading. 
            // Asset bundles are packed files with any sort of unity asset stored within them.
            // We load all of them here, and then - when we want to load an asset - we don't need
            // to know what assetbundle it's in. The AssetBundleLoader class will automatically
            // handle everything to do with AssetBundles.

            AssetBundleLoader.LoadBundles();

            // Example of assets being loaded, given we know their directory in the unity editor.

            CEPMAssets.particleAsset = AssetBundleLoader.LoadAsset<GameObject>("Assets/CEPM/PlayerFire.prefab");
            CEPMAssets.flamethrowerAsset = AssetBundleLoader.LoadAsset<Item>("Assets/CEPM/FlamethrowerItem.asset");

            // This function just registers the flamethrower item as a shop item using helpful LethalLib functions.

            RegisterFlamethrowerItem();

            // Harmony is the utility used for patching class methods, either as an entrypoint for our own code
            // or for adding onto an existing function.

            harmony.PatchAll(typeof(BurnPlayer));

        }

        void RegisterFlamethrowerItem()
        {
            GameObject ftPrefab = CEPMAssets.flamethrowerAsset.spawnPrefab;

            Flamethrower flamethrowerScript = ftPrefab.AddComponent<Flamethrower>();
            flamethrowerScript.grabbable = true;
            flamethrowerScript.itemProperties = CEPMAssets.flamethrowerAsset;

            NetworkPrefabs.RegisterNetworkPrefab(ftPrefab);
            LethalLib.Modules.Utilities.FixMixerGroups(ftPrefab);
            Items.RegisterShopItem(CEPMAssets.flamethrowerAsset, 25);
        }

    }
}
