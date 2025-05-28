using Photon.Deterministic;
using System.Linq;
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
        void SpawnEffect(GameObject prefab, FPVector2 position, float angleDeg = 0f, Vector3? scale = null)
        {
            ////�÷��̾ ���� �����Ǵ� ����Ʈ�� z�� �ٲٱ�
            //var game = QuantumRunner.Default.Game;
            //var frame = game.Frames?.Predicted;

            //if (!frame.TryGet<PlayerLink>(EntityRef, out var playerLink))
            //    return;
            //Debug.Log("����Ʈ ");
            //var localPlayer = QuantumRunner.Default.Game.GetLocalPlayers().FirstOrDefault();
            //bool isMine = playerLink.PlayerRef == localPlayer;

            //float zPosition = 1f;

            //if (playerLink.PlayerRef == (PlayerRef)0)
            //    zPosition = 1 * zPosition;
            //else if (playerLink.PlayerRef == (PlayerRef)1)
            //    zPosition = -1 * zPosition;

            if (prefab == null) return;

            // FP �� Unity Vector ��ȯ
            var worldPos = new Vector3(position.X.AsFloat, position.Y.AsFloat, 0f);

            // ����
            Quaternion rotation = Quaternion.Euler(angleDeg, 0f, 0f);

            // ����Ʈ ����
            var effect = Instantiate(prefab, worldPos, rotation);

            // ������ ����
            if (scale != null)
            {
                effect.transform.localScale = scale.Value;
            }

            // ��ƼŬ ���
            var particle = effect.GetComponent<ParticleSystem>();
            if (particle != null)
                particle.Play();

            // �ڵ� ����
            Destroy(effect, 0.1f); // ����� ��� ���� ����
        }

        private void OnHitEffect(EventOnHitEffect OnHitEffect)
        {
            Debug.Log("����Ʈ : ��Ʈ");
            
            Debug.Log("����Ʈ : ��ġ "+ HitParticle.transform.position);
            SpawnEffect(HitParticle, OnHitEffect.position,90, new Vector3 (0.2f,0.2f,0.2f));
            //HitParticle.Play();
        }

        private void OnGuardEffect(EventOnGuardEffect OnGuardEffect)
        {

            SpawnEffect(GuardParticle, OnGuardEffect.position, 90, new Vector3(0.3f, 0.3f, 0.3f));
            Debug.Log("����Ʈ : ����");
            //GuardParticle.Play();
        }

        private void OnCounterEffect(EventOnCounterEffect OnCounterEffect)
        {
            SpawnEffect(CounterParticle, OnCounterEffect.position, 90, new Vector3(0.3f, 0.3f, 0.3f));
            Debug.Log("����Ʈ : ī����");
            //CounterParticle.Play();
        }


        private void OnParringEffect(EventOnParringEffect OnParringEffect)
        {
            SpawnEffect(ParringParticle, OnParringEffect.position, 90, new Vector3(0.5f, 0.5f, 0.5f));
            Debug.Log("����Ʈ : �и�");
            //ParringParticle.Play();
        }
    }
}


