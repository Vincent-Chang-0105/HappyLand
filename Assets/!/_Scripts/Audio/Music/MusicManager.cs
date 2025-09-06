using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioSystem {
    public class MusicManager : PersistentSingleton<MusicManager> {
        const float crossFadeTime = 1.0f;
        const float pauseFadeTime = 0.5f; // Fade time for pause/resume
        
        float fading;
        float pauseFading;
        bool isPaused = false;
        bool isResuming = false;
        bool isStopping = false;
        
        // Store volumes for pause/resume
        float pausedVolumeA = 0f;
        float pausedVolumeB = 0f;
        
        // Use two persistent AudioSources instead of creating/destroying
        AudioSource audioSourceA;
        AudioSource audioSourceB;
        AudioSource current;
        AudioSource previous;
        
        readonly Queue<AudioClip> playlist = new();

        [SerializeField] List<AudioClip> initialPlaylist;
        [SerializeField] AudioMixerGroup musicMixerGroup;

        // Public properties
        public bool IsPaused => isPaused;
        public bool IsPlaying => current && current.isPlaying && !isPaused;
        public AudioClip CurrentClip => current ? current.clip : null;

        protected override void Awake() {
            base.Awake();
            
            // Create two persistent AudioSources
            audioSourceA = gameObject.AddComponent<AudioSource>();
            audioSourceB = gameObject.AddComponent<AudioSource>();
            
            // Configure both AudioSources
            SetupAudioSource(audioSourceA);
            SetupAudioSource(audioSourceB);
            
            current = audioSourceA;
        }

        void SetupAudioSource(AudioSource source) {
            source.outputAudioMixerGroup = musicMixerGroup;
            source.loop = true;
            source.volume = 0;
            source.bypassListenerEffects = true;
            source.playOnAwake = false;
        }

        void Start() { 
            foreach (var clip in initialPlaylist) {
                AddToPlaylist(clip);
            }
        }

        public void AddToPlaylist(AudioClip clip) {
            playlist.Enqueue(clip);
            if (current == null && previous == null) {
                PlayNextTrack();
            }
        }

        public void Clear() => playlist.Clear();

        public void PlayNextTrack() {
            if (playlist.TryDequeue(out AudioClip nextTrack)) {
                Play(nextTrack);
            }
        }

        public void Play(AudioClip clip) {
            if (current && current.clip == clip && !isPaused) return;

            // Reset pause state when playing new clip
            ResetPauseState();

            // Swap the AudioSources
            previous = current;
            current = (current == audioSourceA) ? audioSourceB : audioSourceA;

            // Setup the new current AudioSource
            current.clip = clip;
            current.volume = 0;
            current.Play();

            fading = 0.001f;
        }

        /// <summary>
        /// Pause the current music with fade out
        /// </summary>
        public void Pause() {
            if (isPaused || !IsPlaying) return;

            isPaused = true;
            isResuming = false;
            
            // Store current volumes before pausing
            pausedVolumeA = audioSourceA.volume;
            pausedVolumeB = audioSourceB.volume;
            
            pauseFading = 0.001f;
            
            Debug.Log("Music paused");
        }

        /// <summary>
        /// Resume the paused music with fade in
        /// </summary>
        public void Resume() {
            if (!isPaused) return;

            isPaused = false;
            isResuming = true;
            pauseFading = 0.001f;
            
            Debug.Log("Music resumed");
        }

        /// <summary>
        /// Toggle between pause and resume
        /// </summary>
        public void TogglePause() {
            if (isPaused) {
                Resume();
            } else {
                Pause();
            }
        }

        /// <summary>
        /// Stop the current music with fade out
        /// </summary>
        public void Stop() {
            if (!IsPlaying && !isPaused) return;

            isStopping = true;
            isPaused = false;
            isResuming = false;
            pauseFading = 0.001f;
            
            Debug.Log("Music stopped");
        }

        /// <summary>
        /// Stop the music immediately without fade
        /// </summary>
        public void StopImmediate() {
            ResetPauseState();
            
            if (current) {
                current.Stop();
                current.volume = 0;
                current.clip = null;
            }
            
            if (previous) {
                previous.Stop();
                previous.volume = 0;
                previous.clip = null;
            }
            
            current = audioSourceA;
            previous = null;
            fading = 0f;
            
            Debug.Log("Music stopped immediately");
        }

        private void ResetPauseState() {
            isPaused = false;
            isResuming = false;
            isStopping = false;
            pauseFading = 0f;
            pausedVolumeA = 0f;
            pausedVolumeB = 0f;
        }

        void Update() {
            HandleCrossFade();
            HandlePauseResume();

            // Check if current track finished and play next
            // if (current && !current.isPlaying && !isPaused && playlist.Count > 0) {
            //     PlayNextTrack();
            // }
        }

        void HandleCrossFade() {
            if (fading <= 0f || isPaused || isStopping) return;
            
            fading += Time.deltaTime;

            float fraction = Mathf.Clamp01(fading / crossFadeTime);

            // Logarithmic fade
            float logFraction = fraction.ToLogarithmicFraction();

            if (previous) previous.volume = 1.0f - logFraction;
            if (current) current.volume = logFraction;

            if (fraction >= 1) {
                fading = 0.0f;
                if (previous) {
                    previous.Stop(); // Stop the previous track instead of destroying
                    previous.clip = null; // Clear the clip reference
                }
                previous = null;
            }
        }

        void HandlePauseResume() {
            if (pauseFading <= 0f) return;

            pauseFading += Time.deltaTime;
            float fraction = Mathf.Clamp01(pauseFading / pauseFadeTime);

            if (isPaused) {
                // Fading out to pause
                float fadeOut = 1.0f - fraction;
                audioSourceA.volume = pausedVolumeA * fadeOut;
                audioSourceB.volume = pausedVolumeB * fadeOut;

                if (fraction >= 1f) {
                    // Pause complete - actually pause the audio sources
                    if (current) current.Pause();
                    if (previous) previous.Pause();
                    pauseFading = 0f;
                }
            }
            else if (isResuming) {
                // Fading in from pause
                if (fraction == 0.001f / Time.deltaTime) {
                    // First frame of resume - unpause the audio sources
                    if (current) current.UnPause();
                    if (previous) previous.UnPause();
                }

                audioSourceA.volume = pausedVolumeA * fraction;
                audioSourceB.volume = pausedVolumeB * fraction;

                if (fraction >= 1f) {
                    // Resume complete
                    isResuming = false;
                    pauseFading = 0f;
                }
            }
            else if (isStopping) {
                // Fading out to stop
                float fadeOut = 1.0f - fraction;
                audioSourceA.volume = pausedVolumeA * fadeOut;
                audioSourceB.volume = pausedVolumeB * fadeOut;

                if (fraction >= 1f) {
                    // Stop complete
                    StopImmediate();
                }
            }
        }
    }
}