using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = LC.CEPM.CEPMLoggingUtils.Logger;

namespace LC.CEPM.CEPMCore.UnityAssetManager
{
    /// <summary>
    /// Tool for loading files easily without really interacting with assetbundles.
    /// </summary>
    public static class AssetBundleLoader
    {
        private static Logger ABLogger = new Logger("ABLOADER");
        
        /// <summary>
        /// The list of all loaded assetbundles, pointed to via bundle name.
        /// </summary>
        public static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        // To note, a dictionary is like a table of contents, where we can access a value via a key.
        // In this case, the key is the name of the file before runtime, and can be accessed via loadedBundles["bundle1"], given the bundle was called "bundle1" in files.

        /// <summary>
        /// Load an asset. The AssetBundleLoader class will check through every loaded bundle.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            // Explaining what T means here:
            // T is a generic type which can be any sort of data type.
            // "where T : UnityEngine.GameObject" means:
            // "This function's T value can be any sort of Unity data type."
            // This allows both LoadAsset<GameObject> and LoadAsset<Item> to
            // handle a GameObject or an Item type respectively.

            // Unity data types are types you would see on unity assets.
            // GameObject is the most common example, however there is also stuff
            // such as Sprite, Texture2D, AudioClip, AudioSource, Collider, etc.

            // To load an asset of type Texture2D, you would have a command such as:
            // Texture2D myNewTextureVar = AssetBundleLoader.LoadAsset<Texture2D>(path-to-texture);

            // Hope this makes sense!

            // Iterate through each loaded bundle.
            foreach (var pair in loadedBundles)
            {
                AssetBundle bundle = pair.Value;
                T asset = bundle.LoadAsset<T>(path);
                if (asset != null)
                {
                    // asset is not null, return this asset and break loop.
                    ABLogger.Log("Found " +typeof(T)+ " '" + asset.name + "' in bundle " + bundle.name + " and successfully loaded it.");
                    return asset;
                }
                // asset was null, continue loop
            }
            // no asset of type T was found.
            ABLogger.Error(typeof(T) + " asset was not found in any bundle. Usage of this unloaded variable will cause errors.");
            return null;
        }

        /// <summary>
        /// Load all the bundles in a path. Usually this only needs to be run once.
        /// </summary>
        /// <param name="bundlePath"></param>
        public static void LoadBundles(string bundlePath = "assets/bundles")
        {

            // The assembly location refers to the location the mod's dll is placed in.
            // the full hierarchy starting from the LC folder would be Lethal Company/BepInEx/plugins/fayemoddinggroup.CEPM/assets/bundles.
            string path = Path.Combine(CEPMInfo.AssemblyLocation, bundlePath);
            
            // These two lines gather the names of all the files located in the "bundles" folder.
            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] allDirs = di.GetFiles();

            // The foreach loop assumes every file in the bundles folder is an assetbundle and attempts to load it.
            foreach (FileInfo file in allDirs)
            {
                // load a new assetbundle from the path to the bundles folder ADD the file name, creating a complete directory to that bundle.
                AssetBundle newAssetbundle = AssetBundle.LoadFromFile(Path.Combine(path, file.Name));

                // if newAssetBundle is null (hasn't been loaded as an assetbundle)
                if (!newAssetbundle)
                {
                    // File either corrupted or was not an assetbundle, it is ignored
                    ABLogger.Error("Tried loading file " + file.Name + " as AssetBundle but failed unexpectedly! Skipping...");
                }
                else
                {
                    // File was an assetbundle, add it to the list of bundles.
                    ABLogger.Log("Loaded file " + file.Name + " as AssetBundle successfully!");
                    newAssetbundle.name = file.Name;
                    loadedBundles.Add(newAssetbundle.name, newAssetbundle);
                }
            }
        }

    }
}
