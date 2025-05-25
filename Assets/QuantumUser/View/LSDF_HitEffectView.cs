using Photon.Deterministic;
using UnityEngine;

namespace Quantum.LSDF
{


    public class LSDF_HitEffectView : QuantumEntityViewComponent
    {
        

        


    
        public GameObject HitParticle;
        public GameObject GuardParticle;
        public GameObject ParringParticle;
        public GameObject CounterParticle;

        private void Start()
        {
            RegisterCallbacks();
        }
        //public override void OnInitialize()
        //{
        //    //_particles = GetComponent<ParticleSystem>();

        //    //QuantumEvent.Subscribe<OnPlayerHit>(this, OnDamaged);
        //    //QuantumEvent.Subscribe(listener: this, handler: (OnPlayerHit e) => Debug.Log($"MyEvent {e}"));
        //}


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
        void SpawnEffect(GameObject prefab, FPVector2 position)
        {
            if (prefab == null) return;

            var worldPos = new Vector3(position.X.AsFloat, position.Y.AsFloat, 0f);
            var effect = Instantiate(prefab, worldPos, Quaternion.identity);

            var particle = effect.GetComponent<ParticleSystem>();
            if (particle != null)
                particle.Play();

            Destroy(effect, 0.1f);
        }

        private void OnHitEffect(EventOnHitEffect OnHitEffect)
        {
            Debug.Log("����Ʈ : ��Ʈ");
            
            Debug.Log("����Ʈ : ��ġ "+ HitParticle.transform.position);
            SpawnEffect(HitParticle, OnHitEffect.position);
            //HitParticle.Play();
        }

        private void OnGuardEffect(EventOnGuardEffect OnGuardEffect)
        {

            SpawnEffect(GuardParticle, OnGuardEffect.position);
            Debug.Log("����Ʈ : ����");
            //GuardParticle.Play();
        }

        private void OnCounterEffect(EventOnCounterEffect OnCounterEffect)
        {
            SpawnEffect(CounterParticle, OnCounterEffect.position);
            Debug.Log("����Ʈ : ī����");
            //CounterParticle.Play();
        }


        private void OnParringEffect(EventOnParringEffect OnParringEffect)
        {
            SpawnEffect(ParringParticle, OnParringEffect.position);
            Debug.Log("����Ʈ : �и�");
            //ParringParticle.Play();
        }
    }
}


