namespace Quantum.Asteroids
{
    using System.Collections.Generic;
    using Quantum;
    using UnityEngine;
    using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

    /// <summary>
    /// This Behavior handles events not related to player actions that require audio feedback, such as shooting and explosions
    /// Uses the default Unity Audio API for simplicity
    /// </summary>
    public class SfxManager : MonoBehaviour
    {
        [Header("References")]
        public AudioSource AudioSourcePrefab;

        [Header("Configurations")]
        public int MaxAudioSources = 16;
        public Transform AudioSourceDefaultParent;

        [Header("Audios")]
        public LSDF_AudioConfiguration OnHitAudio;
        public LSDF_AudioConfiguration OnGuardAudio;
        public LSDF_AudioConfiguration OnCounterAudio;
        //public LSDF_AudioConfiguration ShipShootAudio;
        //public LSDF_AudioConfiguration StartLevelAudio;

        private readonly Stack<AudioSource> _freeAudioSources = new Stack<AudioSource>();
        private List<AudioSource> _audioSourcesInUse = new List<AudioSource>();


        private void Start()
        {
            for (int i = 0; i < MaxAudioSources; i++)
            {

                var audioSource = Instantiate(AudioSourcePrefab, AudioSourceDefaultParent);
                audioSource.transform.position = Vector3.zero;
                _freeAudioSources.Push(audioSource);
            }
            RegisterCallbacks();
        }

        private void Update()
        {
            for (var i = _audioSourcesInUse.Count - 1; i >= 0; i--)
            {
                var source = _audioSourcesInUse[i];
                if (!source.isPlaying)
                {
                    _freeAudioSources.Push(source);
                    _audioSourcesInUse.RemoveAt(i);
                    source.transform.SetParent(AudioSourceDefaultParent);
                }
            }
        }

        private void RegisterCallbacks()
        {
            Debug.Log("사운드 : 등록");
            QuantumEvent.Subscribe<EventOnHit>(this, OnHit);
            QuantumEvent.Subscribe<EventOnGuard>(this, OnGuard);
            QuantumEvent.Subscribe<EventOnCounter>(this, OnCounter);
            //QuantumEvent.Subscribe<EventOnAsteroidDestroyed>(this, OnAsteroidDestroyed);
            //QuantumEvent.Subscribe<EventOnStartNewLevel>(this, OnStartNewLevel);
        }

        AudioSource GetAvailableAudioSource()
        {
            if (_freeAudioSources.Count > 0)
            {
                var source = _freeAudioSources.Pop();
                _audioSourcesInUse.Add(source);
                return source;
            }
            else
            {
                var source = _audioSourcesInUse[0];
                _audioSourcesInUse.RemoveAt(0);
                _audioSourcesInUse.Add(source);
                return source;
            }
        }

        void PlayAudioClip(LSDF_AudioConfiguration audioConfig)
        {
            var source = GetAvailableAudioSource();
            audioConfig.AssignToAudioSource(source);

            source.transform.position = Vector3.zero;
            source.Play();
        }




        private void OnHit(EventOnHit eventData)
        {
            Debug.Log("사운드 : 힛");
            PlayAudioClip(OnHitAudio);
        }

        private void OnGuard(EventOnGuard eventData)
        {
            PlayAudioClip(OnGuardAudio);
        }

        private void OnCounter(EventOnCounter eventData)
        {
            PlayAudioClip(OnCounterAudio);
        }

        //private unsafe void OnAsteroidDestroyed(EventOnAsteroidDestroyed eventData)
        //{
        //    PlayAudioClip(AsteroidDestroyAudio);
        //}

        //private unsafe void OnStartNewLevel(EventOnStartNewLevel eventData)
        //{
        //    PlayAudioClip(StartLevelAudio);
        //}
    }
}