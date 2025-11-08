using System.Collections;
using UnityEngine;

namespace AudioSystem {
    public class SoundBuilder
    {
        readonly SoundManager soundManager;
        Vector3 position = Vector3.zero;
        bool randomPitch;

        public SoundBuilder(SoundManager soundManager)
        {
            this.soundManager = soundManager;
        }

        public SoundBuilder WithPosition(Vector3 position)
        {
            this.position = position;
            return this;
        }

        public SoundBuilder WithRandomPitch()
        {
            this.randomPitch = true;
            return this;
        }

        public SoundEmitter Play(SoundData soundData)
        {
            if (soundData == null)
            {
                Debug.LogError("SoundData is null");
                return null;
            }

            if (!soundManager.CanPlaySound(soundData)) return null;

            SoundEmitter soundEmitter = soundManager.Get();
            soundEmitter.Initialize(soundData);
            soundEmitter.transform.position = position;
            soundEmitter.transform.parent = soundManager.transform;

            if (randomPitch)
            {
                soundEmitter.WithRandomPitch();
            }

            if (soundData.frequentSound)
            {
                soundEmitter.Node = soundManager.FrequentSoundEmitters.AddLast(soundEmitter);
            }

            soundEmitter.Play();
            return soundEmitter; // Return the SoundEmitter instead of void
        }

        public void PlaySequence(params SoundData[] soundsToPlay)
        {
            if (soundsToPlay == null || soundsToPlay.Length == 0)
            {
                Debug.LogError("No sounds provided to PlaySequence");
                return;
            }

            soundManager.StartCoroutine(PlaySoundsInSequence(soundsToPlay));
        }
        
        private IEnumerator PlaySoundsInSequence(SoundData[] sounds) {
            foreach (SoundData soundData in sounds) {
                if (soundData == null) {
                    Debug.LogWarning("Null SoundData in sequence, skipping");
                    continue;
                }

                // Play the sound
                Play(soundData);
                
                // Wait for the sound to finish
                if (soundData.clip != null) {
                    yield return new WaitForSeconds(soundData.clip.length - soundData.clip.length * 0.5f);
                } else {
                    // Fallback delay if clip is null
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
    }
}