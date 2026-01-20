using BepInEx;
using HarmonyLib;
using LC.CEPM.CEPMCore.ItemBehaviours;
using LC.CEPM.CEPMCore.UnityAssetManager;
using LC.CEPM.GamePatches;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Unity;
using UnityEngine;
using Logger = LC.CEPM.CEPMLoggingUtils.Logger;

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
            NetcodePatcher();

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

        // This is where we use LethalLib's functions for registering the flamethrower item. 
        void RegisterFlamethrowerItem()
        {
            GameObject ftPrefab = CEPMAssets.flamethrowerAsset.spawnPrefab;

            // Adding the custom behaviour script. This cannot be added directly in unity, because unity does
            // not allow including executable code in an asset bundle for security reasons.
            Flamethrower flamethrowerScript = ftPrefab.AddComponent<Flamethrower>();
            flamethrowerScript.grabbable = true;
            flamethrowerScript.itemProperties = CEPMAssets.flamethrowerAsset;

            NetworkPrefabs.RegisterNetworkPrefab(ftPrefab);
            LethalLib.Modules.Utilities.FixMixerGroups(ftPrefab);
            //                     Item we're registering      | Price (Subject to change)
            Items.RegisterShopItem(CEPMAssets.flamethrowerAsset, 500);
        }

        /// <summary>
        /// Netcode patcher stuff, in order to have unity's ServerRPC and ClientRPC methods to compile, as they would with the unity compiler. This is only run once and you do not need to interact with this.
        /// </summary>
        private void NetcodePatcher()
        {
            // Extra context: Lethal Company uses Unity NetCode for GameObjects, which is a system that sends and recieves 
            // RPC calls from GameObject code, using ServerRPC and ClientRPC functions. These are properly compiled
            // just fine when compiling using unity's custom compiler, but when making a mod, and therefore using a standard
            // .NET compiler, these extra steps don't happen, so the exact same RPC calls won't function how they should.
            // This function is part of a helpful post compiler event that patches RPC calls to function exactly how
            // they would, had they been compiled via the unity compiler. 

            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attribs = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attribs.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

    }
}
