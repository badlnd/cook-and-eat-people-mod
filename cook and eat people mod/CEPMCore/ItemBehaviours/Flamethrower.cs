using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using Base = LC.CEPM.CEPMCore.CEPMBase;

namespace LC.CEPM.CEPMCore.ItemBehaviours
{
    internal class Flamethrower : PhysicsProp
    {
        private ParticleSystem preFireParticles;
        private ParticleSystem fireParticles;
        private ParticleSystem postFireParticles;

        private AudioSource gasAudio;
        private AudioSource flameAudio;
        private AudioSource oneShotAudio;

        private Coroutine firingCoroutine;
        private bool inUse;

        private RaycastHit[] enemyColliders;

        private int failChanceOutOf10 = 1;
        private float flamethrowerRange = 7.0f;

        public int DamagePerTick = 15;
        public float TimeBetweenDamageTicks = 0.3f;

        bool damageSwitch = true;

        bool firePlaying = false;
        public override void Update()
        {
            base.Update();
            DischargeBattery();
            if (OutOfCharge())
            {
                gasAudio.pitch = 1.5f;
            }
            else
            {
                gasAudio.pitch = 1;
            }
            //Base.PropLogger.Log(inUse);
        }

        private IEnumerator DamageEntitiesOverTime()
        {
            while (true)
            {
                yield return new WaitForSeconds(TimeBetweenDamageTicks);
                DamageEntitiesInPathServerRPC(firePlaying);
                yield return new WaitForEndOfFrame(); 
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void DamageEntitiesInPathServerRPC(bool isFiring)
        {
            DamageEntitiesInPathClientRPC(isFiring);
        }

        [ClientRpc]
        private void DamageEntitiesInPathClientRPC(bool isFiring)
        {

            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            
            if (!isHeld || !isFiring || playerHeldBy == null || localPlayer == null)
                return;
            
            Vector3 pos = transform.position;
            float dist = Vector3.Distance(pos, localPlayer.transform.position);

            if (dist < 5f)
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
            else if (dist < 25f)
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);

            Vector3 forward = playerHeldBy.gameplayCamera.transform.forward;
            Vector3 closestPoint = localPlayer.playerCollider.ClosestPoint(pos);

            enemyColliders = new RaycastHit[10];

            Ray ray = new Ray(pos - forward * 10f, forward);
            int hits = Physics.SphereCastNonAlloc(ray, flamethrowerRange, enemyColliders, 15f, 524288, QueryTriggerInteraction.Collide);
            List<EnemyAI> list = new List<EnemyAI>();
            for (int i = 0; i < hits; i++)
            {
                if (!enemyColliders[i].transform.GetComponent<EnemyAICollisionDetect>())
                {
                    continue;
                }
                EnemyAI enemyAI = enemyColliders[i].transform.GetComponent<EnemyAICollisionDetect>().mainScript;
                IHittable iHittable;
                if (!Physics.Linecast(pos, enemyColliders[i].point, out var hitInfo, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) && enemyColliders[i].transform.TryGetComponent<IHittable>(out iHittable))
                {
                    // Damage switch is here to ensure a damage tick only affects an enemy every OTHER tick, because enemies' health is different to ours.
                    // A sand spider's health is only 3 (3 shovel hits) and the hit damage is an integer, so the value cannot be lower than one.
                    // This means a sand spider would die in three flamethrower ticks. To balance things out, we can do it every other tick.
                    damageSwitch = !damageSwitch;
                    if (damageSwitch)
                    {
                        float enemyToGunDist = Vector3.Distance(pos, enemyColliders[i].point);
                        EnemyAICollisionDetect collision = enemyColliders[i].collider.GetComponent<EnemyAICollisionDetect>();
                        if ((!(collision != null) || (!(collision.mainScript == null) && !list.Contains(collision.mainScript))) && iHittable.Hit(1, forward, playerHeldBy, true) && collision != null)
                        {
                            list.Add(collision.mainScript);
                        }
                    }
                }
            }

            if (Physics.Linecast(pos, closestPoint, out var playerHit, StartOfRound.Instance.collidersAndRoomMaskAndPlayers, QueryTriggerInteraction.Ignore) && Vector3.Angle(forward, closestPoint - pos) < 20f && dist <= flamethrowerRange)
            {
                if (playerHit.transform.TryGetComponent<PlayerControllerB>(out PlayerControllerB player))
                {
                    if (player == localPlayer && player != playerHeldBy)
                    {
                        Base.PropLogger.Log("Hit player! Distance: '" + dist + "', Range: '" + flamethrowerRange + "', Angle: '" + Vector3.Angle(forward, closestPoint - pos) + "', Player: '" + player.name);
                        localPlayer.DamagePlayer(DamagePerTick, true, true, CauseOfDeath.Burning, 6);
                    }
                }
            }
        }

        private void DischargeBattery()
        {
            if (firePlaying && isHeld && insertedBattery.charge > 0)
            {
                insertedBattery.charge -= 0.1f * Time.deltaTime;
            }
        }
        public override void Start()
        {
            base.Start();
            gasAudio = gameObject.transform.Find("AudioSources/Gas").GetComponent<AudioSource>();
            flameAudio = gameObject.transform.Find("AudioSources/Fire").GetComponent<AudioSource>();
            oneShotAudio = gameObject.transform.Find("AudioSources/Oneshot").GetComponent<AudioSource>();

            preFireParticles = gameObject.transform.Find("Flamethrower/Particles/PreFireParticle").GetComponent<ParticleSystem>();
            postFireParticles = gameObject.transform.Find("Flamethrower/Particles/PostFireParticle").GetComponent<ParticleSystem>();
            fireParticles = gameObject.transform.Find("Flamethrower/Particles/FireParticle").GetComponent<ParticleSystem>();

            StartCoroutine(DamageEntitiesOverTime());

            insertedBattery.charge = 1;
        }

        // Multiplayer games such as Lethal Company use a Client-Server model, rather than p2p. 
        // This means there is one central server and up to 4 client (without mods).
        // [ServerRpc] functions will execute on the server, and the server can communicate with
        // all the clients. The clients cannot directly communicate with each other,
        // so the communication happens through the server.

        // What's happening here, is the client that executed the ItemActivate function says to
        // the server "Hey, I want you to tell all the other clients to execute this instruction"
        // rather than directly saying to the clients "hey do this".
        // The server is a window into all the other clients. p2p networking is the other method,
        // which DOES allow direct communcation between clients, but no server.
        // That is not what we have.

        // Another note: Rpc functions only work on NetworkObjects, which must be registered. All items
        // derive from networkobjects, so they only have to be registered as a network prefab.
        // Fortunately, LethalLib (by the same person that made the netcode patcher, actually) does
        // all of this for us, as can be seen in CEPMBase.cs.


        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            // Stage 1
            // Hey server, tell all clients to start the firing function
            Base.PropLogger.Log("(Client " + GameNetworkManager.Instance.localPlayerController.playerClientId + ") Trying to send ServerRpc instruction...");
            StartFireServerRPC(used, buttonDown);
        }

        [ServerRpc(RequireOwnership = false)]
        private void StartFireServerRPC(bool used, bool buttonDown)
        {
            // Stage 2
            // Okay, telling all clients to start this function
            Base.PropLogger.Log("(Server) Recieved instruction to distribute to all clients");
            System.Random r = new System.Random();
            int num = r.Next(1, 10);
            StartFireClientRPC(used, buttonDown, num);
        }

        [ClientRpc]
        private void StartFireClientRPC(bool used, bool buttonDown, int num)
        {
            // Stage 3
            // This client has recieved the instruction from the server to begin this function.
            Base.PropLogger.Log("(Client " + GameNetworkManager.Instance.localPlayerController.playerClientId + ") Recieved instruction");
            if (playerHeldBy == null)
                return;
            inUse = buttonDown;
            if (buttonDown)
            {

                if (firingCoroutine != null)
                {
                    StopCoroutine(firingCoroutine);
                }
                if (insertedBattery.charge == 0)
                    inUse = false;
                firingCoroutine = StartCoroutine(startFiringAnim(num));
            }
        }

        private bool OutOfCharge()
        {
            return (insertedBattery.charge <= 0);
        }

        private IEnumerator VolumeIncrease(AudioSource audioSource, float time = 0.3f, float start = 0, float end = 1)
        {
            float elapsedTime = 0;
            audioSource.volume = start;
            while (elapsedTime < time)
            {
                elapsedTime += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(start, end, elapsedTime / time);
                yield return null;
            }
            audioSource.volume = end;
        }
        private IEnumerator startFiringAnim(int random)
        {
            bool outOfChargeAtStart = OutOfCharge();
            float time = random / 10;
            
            StartCoroutine(VolumeIncrease(gasAudio));
            gasAudio.Play();

            playerHeldBy.activatingItem = true;
            preFireParticles.Play();
            yield return new WaitForSeconds((float)random / 10);

            if (inUse && !OutOfCharge())
            {
                if (random > failChanceOutOf10 && !OutOfCharge())
                {
                    oneShotAudio.Play();
                    StartCoroutine(VolumeIncrease(flameAudio));
                    gasAudio.volume = 0.2f;
                    flameAudio.Play();

                    fireParticles.Play();
                    firePlaying = true;
                }
            }
            yield return new WaitUntil(() => !inUse || (OutOfCharge() && inUse && !outOfChargeAtStart) || playerHeldBy == null);
            flameAudio.Stop();
            fireParticles.Stop();
            preFireParticles.Stop();
            if (firePlaying)
                postFireParticles.Play();

            firePlaying = false;

            if (playerHeldBy.isPlayerDead)
                inUse = false;

            if (inUse)
            {
                gasAudio.volume = 1;
            }

            yield return new WaitUntil(() => !inUse);
            gasAudio.Stop();
            playerHeldBy.activatingItem = false;
        }
    }
}

