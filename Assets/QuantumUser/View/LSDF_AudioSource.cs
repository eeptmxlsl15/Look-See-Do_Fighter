
namespace Quantum.Asteroids
{
    using UnityEngine;
    using UnityEngine.Audio;

    [System.Serializable]
    public struct LSDF_AudioConfiguration
    {
        public AudioResource Resource;

        [Range(0, 1.0f)]
        public float Volume;

        public bool Is2D;
        public bool Loop;
        public float Delay;

        public string Name
        {
            get { return Resource == null ? "No Resource selected" : Resource.name; }
        }

        public bool IsValid()
        {
            return Resource != null;
        }

        public void AssignToAudioSource(AudioSource audioSource)
        {
            audioSource.volume = Volume;
            audioSource.resource = Resource;
            audioSource.spatialBlend = Is2D ? 0.0f : 1.0f;
            audioSource.loop = Loop;
        }
    }
}