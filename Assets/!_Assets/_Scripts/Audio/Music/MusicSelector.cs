// using System.Collections;
// using System.Collections.Generic;
// using AudioSystem;
// using UnityEngine;
// using UnityEngine.SceneManagement;

// /// <summary>
// /// Handles music selection logic based on scenes and events
// /// </summary>
// public class MusicSelector : PersistentSingleton<MusicSelector>
//     {
//         [SerializeField] private MusicConfiguration musicConfig;
//         [SerializeField] private bool autoPlaySceneMusic = true;
//         [SerializeField] private bool eventMusicOverridesSceneMusic = true;
    
//         private string currentScene;
//         private Dictionary<string, int> activeEvents = new Dictionary<string, int>();
//         private string highestPriorityEvent;
        
//         protected override void Awake()
//         {
//             base.Awake();
            
//             if (musicConfig == null)
//             {
//                 Debug.LogError("No MusicConfiguration assigned!");
//                 enabled = false;
//                 return;
//             }
            
//             // Track current scene
//             currentScene = SceneManager.GetActiveScene().name;
            
//             // Subscribe to scene load events
//             if (autoPlaySceneMusic)
//             {
//                 SceneManager.sceneLoaded += OnSceneLoaded;
//             }
//         }
        
//         private void Start()
//         {
//             // Initial music for the first scene
//             if (autoPlaySceneMusic)
//             {
//                 PlayMusicForCurrentScene();
//             }
//         }
        
//         private void OnDestroy()
//         {
//             SceneManager.sceneLoaded -= OnSceneLoaded;
//         }
        
//         private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
//         {
//             currentScene = scene.name;

//             // Only play scene music if no event music is active or events don't override
//             if (!eventMusicOverridesSceneMusic || string.IsNullOrEmpty(highestPriorityEvent))
//             {
//                 PlayMusicForScene(scene.name);
//             }
//         }
        
//         /// <summary>
//         /// Play music for the current Unity scene
//         /// </summary>
//         public void PlayMusicForCurrentScene()
//         {
//             PlayMusicForScene(SceneManager.GetActiveScene().name);
//         }
        
//         /// <summary>
//         /// Play music for a specific scene name
//         /// </summary>
//         public void PlayMusicForScene(string sceneName)
//         {
//             AudioClip clip = GetMusicForScene(sceneName);
//             if (clip != null)
//             {
//                 MusicManager.Instance.Play(clip);
//             }
//         }
        
//         /// <summary>
//         /// Get the music clip associated with a scene
//         /// </summary>
//         public AudioClip GetMusicForScene(string sceneName)
//         {
//             foreach (var pair in musicConfig.sceneMusicMappings)
//             {
//                 if (pair.sceneName == sceneName)
//                 {
//                     return pair.musicClip;
//                 }
//             }
//             return musicConfig.defaultMusic;
//         }
        
//         /// <summary>
//         /// Trigger event-based music with a priority
//         /// </summary>
//         public void TriggerEventMusic(string eventName, int priority = -1)
//         {
//             AudioClip eventClip = GetMusicForEvent(eventName);
//             if (eventClip == null) return;
            
//             // Use default priority if none specified
//             if (priority < 0)
//             {
//                 priority = GetDefaultPriorityForEvent(eventName);
//             }
            
//             // Store the event and its priority
//             activeEvents[eventName] = priority;
            
//             // Determine highest priority event
//             UpdateHighestPriorityEvent();
            
//             // Play this music if it's the highest priority
//             if (eventName == highestPriorityEvent)
//             {
//                 MusicManager.Instance.Play(eventClip);
//             }
//         }
        
//         /// <summary>
//         /// End event-based music and return to appropriate music
//         /// </summary>
//         public void EndEventMusic(string eventName)
//         {
//             if (!activeEvents.ContainsKey(eventName)) return;
                
//             // Check if this was the active event music
//             bool wasHighestPriority = (eventName == highestPriorityEvent);
            
//             // Remove the event
//             activeEvents.Remove(eventName);
            
//             // If this wasn't the playing event, nothing more to do
//             if (!wasHighestPriority) return;
            
//             // Update priority and play appropriate music
//             UpdateHighestPriorityEvent();
            
//             if (!string.IsNullOrEmpty(highestPriorityEvent))
//             {
//                 // Play the next highest priority event music
//                 AudioClip nextClip = GetMusicForEvent(highestPriorityEvent);
//                 if (nextClip != null)
//                 {
//                     MusicManager.Instance.Play(nextClip);
//                 }
//             }
//             else
//             {
//                 // No more active events, return to scene music
//                 PlayMusicForScene(currentScene);
//             }
//         }
        
//         /// <summary>
//         /// Get the music clip associated with an event
//         /// </summary>
//         public AudioClip GetMusicForEvent(string eventName)
//         {
//             foreach (var pair in musicConfig.eventMusicMappings)
//             {
//                 if (pair.eventName == eventName)
//                 {
//                     return pair.musicClip;
//                 }
//             }
//             return null;
//         }
        
//         /// <summary>
//         /// Get the default priority for an event from the config
//         /// </summary>
//         private int GetDefaultPriorityForEvent(string eventName)
//         {
//             foreach (var pair in musicConfig.eventMusicMappings)
//             {
//                 if (pair.eventName == eventName)
//                 {
//                     return pair.defaultPriority;
//                 }
//             }
//             return 1; // Default priority
//         }
        
//         /// <summary>
//         /// Update which event has the highest priority
//         /// </summary>
//         private void UpdateHighestPriorityEvent()
//         {
//             int highestPriority = 0;
//             highestPriorityEvent = null;
            
//             foreach (var evt in activeEvents)
//             {
//                 if (evt.Value > highestPriority)
//                 {
//                     highestPriority = evt.Value;
//                     highestPriorityEvent = evt.Key;
//                 }
//             }
//         }
//     }
