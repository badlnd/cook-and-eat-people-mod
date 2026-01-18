using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
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

        private int failChanceOutOf10 = 1;

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
            if (firePlaying)
            {

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

            insertedBattery.charge = 1;
        }
        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
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
                firingCoroutine = StartCoroutine(startFiring());
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
        private IEnumerator startFiring()
        {
            bool outOfChargeAtStart = OutOfCharge();
            System.Random r = new System.Random();
            int num = r.Next(1, 10);
            float time = num / 10;
            
            StartCoroutine(VolumeIncrease(gasAudio));
            gasAudio.Play();

            playerHeldBy.activatingItem = true;
            preFireParticles.Play();
            yield return new WaitForSeconds((float)num / 10);

            if (inUse && !OutOfCharge())
            {
                if (num > failChanceOutOf10 && !OutOfCharge())
                {
                    oneShotAudio.Play();
                    StartCoroutine(VolumeIncrease(flameAudio));
                    gasAudio.volume = 0.2f;
                    flameAudio.Play();

                    fireParticles.Play();
                    firePlaying = true;
                }
            }
            yield return new WaitUntil(() => !inUse || (OutOfCharge() && inUse && !outOfChargeAtStart));
            flameAudio.Stop();
            fireParticles.Stop();
            preFireParticles.Stop();
            firePlaying = false;
            if (firePlaying)
                postFireParticles.Play();

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

