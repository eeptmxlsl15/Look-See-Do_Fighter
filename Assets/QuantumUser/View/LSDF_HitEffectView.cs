using UnityEngine;

namespace Quantum.LSDF
{


    public class LSDF_HitEffectView : QuantumEntityViewComponent
    {
        

        


    
        public ParticleSystem HitParicle;
        public ParticleSystem GuardParicle;
        public ParticleSystem ParringParicle;
        public ParticleSystem CounterParicle;

        public override void OnInitialize()
        {
            //_particles = GetComponent<ParticleSystem>();

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
        private void RegisterCallbacks()
        {
            Debug.Log("사운드 : 등록");
            QuantumEvent.Subscribe<EventOnHitEffect>(this, OnHitEffect);
            QuantumEvent.Subscribe<EventOnGuardEffect>(this, OnGuardEffect);
            QuantumEvent.Subscribe<EventOnCounterEffect>(this, OnCounterEffect);
            QuantumEvent.Subscribe<EventOnParringEffect>(this, OnParringEffect);
            //QuantumEvent.Subscribe<EventOnAttackEffect>(this, OnAttackSound);
            //QuantumEvent.Subscribe<EventOnAsteroidDestroyed>(this, OnAsteroidDestroyed);
            //QuantumEvent.Subscribe<EventOnStartNewLevel>(this, OnStartNewLevel);
        }

        private void OnHitEffect(EventOnHitEffect OnHitEffect)
        {
            Debug.Log("이펙트 : 히트");
            HitParicle.Play();
        }

        private void OnGuardEffect(EventOnGuardEffect OnGuardEffect)
        {
            Debug.Log("이펙트 : 가드");
            GuardParicle.Play();
        }

        private void OnCounterEffect(EventOnCounterEffect OnCounterEffect)
        {
            Debug.Log("이펙트 : 카운터");
            CounterParicle.Play();
        }


        private void OnParringEffect(EventOnParringEffect OnParringEffect)
        {
            Debug.Log("이펙트 : 패링");
            ParringParicle.Play();
        }
    }
}


