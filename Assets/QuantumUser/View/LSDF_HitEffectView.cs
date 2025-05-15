using UnityEngine;

namespace Quantum.LSDF
{


    public class LSDF_HitEffectView : QuantumEntityViewComponent
    {
        public GameObject HitEffectPrefab;
        public GameObject ImmortalityIndicator;

        


    
        private ParticleSystem _particles;

        public override void OnInitialize()
        {
            _particles = GetComponent<ParticleSystem>();

            //QuantumEvent.Subscribe<OnPlayerHit>(this, OnDamaged);
            //QuantumEvent.Subscribe(listener: this, handler: (OnPlayerHit e) => Debug.Log($"MyEvent {e}"));
        }

        private void OnDamaged(OnPlayerHit e)
        {
            //if (e.EntityRef == EntityRef)
            //{
            //    _particles.Play();
            //}
        }
    }
}


