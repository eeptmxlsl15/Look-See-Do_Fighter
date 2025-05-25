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
            Debug.Log("���� : ���");
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
            Debug.Log("����Ʈ : ��Ʈ");
            HitParicle.Play();
        }

        private void OnGuardEffect(EventOnGuardEffect OnGuardEffect)
        {
            Debug.Log("����Ʈ : ����");
            GuardParicle.Play();
        }

        private void OnCounterEffect(EventOnCounterEffect OnCounterEffect)
        {
            Debug.Log("����Ʈ : ī����");
            CounterParicle.Play();
        }


        private void OnParringEffect(EventOnParringEffect OnParringEffect)
        {
            Debug.Log("����Ʈ : �и�");
            ParringParicle.Play();
        }
    }
}


