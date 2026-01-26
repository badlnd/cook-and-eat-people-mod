//#define DEBUGVAR
using GameNetcodeStuff;
using HarmonyLib;
using LC.CEPM.CEPMCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = LC.CEPM.CEPMLoggingUtils.Logger;

namespace LC.CEPM.GamePatches
{
    public class BurnPlayer : MonoBehaviour 
    {
        private static Logger PlayerLogger = new Logger();
        static bool torching;

        private static GameObject fire;
        private static GameObject justSpawnedBody;

        // Returns true or false depending on if the current copy of PlayerControllerB the code is executing on at that time
        // is the PlayerControllerB that is being controlled by the local client. (our computer.)
        private static bool InstanceIsLocal(PlayerControllerB instance)
        {
            if (GameNetworkManager.Instance.localPlayerController == null)
                return false;
            return (instance.playerClientId == GameNetworkManager.Instance.localPlayerController.playerClientId);
        }

        // This function is just used for setting up the logger component.
        // this WOULD be in the "Start" function, but GameNetworkManager.Instance.localPlayerController does not have any data assigned at that stage,
        // so it can't be used without errors. it DOES get assigned in this function, so we execute our patched code in this function after the rest of 
        // the code has executed.

        // Some info on Harmony:
        // [HarmonyPrefix] is a prefix and runs BEFORE the original code,
        // [HarmonyPostfix] will run after.

        // Making a patch function (bool) instead of (void) and returning false 
        // causes the orignal function to never run, allowing you to completely replace a function,
        // if you so wish.
        // If you need to alter one line of code in the middle of a function, that's kind of
        // your only option.

        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        static void start_postfix(PlayerControllerB __instance)
        {
            // PlayerControllerB __instance gives us access to the PlayerControllerB instance of this function as it's patched.
            // This means we have access to any of the specific class variables the original function had access to.


            //if playerlogger has already initialised, OR this playercontroller is not the local client's character, perform an early return.
            if (PlayerLogger.HasInit() || !InstanceIsLocal(__instance))
                return;

            PlayerLogger.Init("PLAYER");
            PlayerLogger.Log("Hooked PlayerControllerB Update method successfully.");

            // Create the fire asset from the loaded prefab we made before.
            fire = GameObject.Instantiate(CEPMAssets.particleAsset);
            fire.transform.parent = __instance.transform;
            fire.transform.localPosition = Vector3.zero;
            // While we want to CREATE the asset, we don't currently want to use it, so we will disable it for now.
            // (This will disable all functions of a gameobject, including rendering.)
            fire.SetActive(false);
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        static void upd_postfix(PlayerControllerB __instance)
        {
#if DEBUGVAR
            if (InstanceIsLocal(__instance))
            {
                if (Keyboard.current.tKey.wasPressedThisFrame && (!torching))
                {
                    if (__instance.AllowPlayerDeath())
                    {
                        __instance.StartCoroutine(Torch(__instance, 10));
                        PlayerLogger.Log("Torching");
                    }
                    else
                    {
                        PlayerLogger.Warning("Can't torch player! AllowPlayerDeath returns false.");
                    }
                }
            }
#endif
        }

        // An IEnumerator is a function which can run on a separate thread.
        // All functions will wait for each line of code to run before moving on to the next,
        // which means if funcionA is called during functionB, functionB must wait for functionA
        // to complete before it can move on.
        // Beginning an IEnumerator coroutine will bypass this limitation and
        // allow for things such as waiting for X seconds,
        // allowing for sequenced events such as taking Y amount of damage every X seconds for Z duration.

        // IEnumerators can run without forcing the function that called them to wait for them to finish.
        // This can be very useful.

        private static IEnumerator Torch(PlayerControllerB instance, int damagePerTick, float time = 6f, float tickTime = 0.25f)
        {
            if (!torching)
            {
                fire.SetActive(true);
                torching = true;
                float startTime = Time.realtimeSinceStartup;
                while (Time.realtimeSinceStartup - startTime < time)
                {
                    yield return new WaitForSeconds(tickTime);
                    instance.DamagePlayer(damagePerTick, true, true, CauseOfDeath.Burning, 6);
                }
                if (instance == GameNetworkManager.Instance.localPlayerController && !instance.isPlayerDead)
                {
                    instance.KillPlayer(Vector3.zero, true, CauseOfDeath.Burning, 6);
                }
                fire.SetActive(false);
                torching = false;
            }
        }

    }
}
