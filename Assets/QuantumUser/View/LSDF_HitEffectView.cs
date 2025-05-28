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
            Debug.Log("사운드 : 등록");
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
            ////플레이어에 따라 생성되는 이펙트의 z값 바꾸기
            //var game = QuantumRunner.Default.Game;
            //var frame = game.Frames?.Predicted;

            //if (!frame.TryGet<PlayerLink>(EntityRef, out var playerLink))
            //    return;
            //Debug.Log("이펙트 ");
            //var localPlayer = QuantumRunner.Default.Game.GetLocalPlayers().FirstOrDefault();
            //bool isMine = playerLink.PlayerRef == localPlayer;

            //float zPosition = 1f;

            //if (playerLink.PlayerRef == (PlayerRef)0)
            //    zPosition = 1 * zPosition;
            //else if (playerLink.PlayerRef == (PlayerRef)1)
            //    zPosition = -1 * zPosition;

            if (prefab == null) return;

            // FP → Unity Vector 변환
            var worldPos = new Vector3(position.X.AsFloat, position.Y.AsFloat, 0f);

            // 각도
            Quaternion rotation = Quaternion.Euler(angleDeg, 0f, 0f);

            // 이펙트 생성
            var effect = Instantiate(prefab, worldPos, rotation);

            // 스케일 설정
            if (scale != null)
            {
                effect.transform.localScale = scale.Value;
            }

            // 파티클 재생
            var particle = effect.GetComponent<ParticleSystem>();
            if (particle != null)
                particle.Play();

            // 자동 제거
            Destroy(effect, 0.1f); // 충분히 길게 설정 권장
        }

        private void OnHitEffect(EventOnHitEffect OnHitEffect)
        {
            Debug.Log("이펙트 : 히트");
            
            Debug.Log("이펙트 : 위치 "+ HitParticle.transform.position);
            SpawnEffect(HitParticle, OnHitEffect.position,90, new Vector3 (0.2f,0.2f,0.2f));
            //HitParticle.Play();
        }

        private void OnGuardEffect(EventOnGuardEffect OnGuardEffect)
        {

            SpawnEffect(GuardParticle, OnGuardEffect.position, 90, new Vector3(0.3f, 0.3f, 0.3f));
            Debug.Log("이펙트 : 가드");
            //GuardParticle.Play();
        }

        private void OnCounterEffect(EventOnCounterEffect OnCounterEffect)
        {
            SpawnEffect(CounterParticle, OnCounterEffect.position, 90, new Vector3(0.3f, 0.3f, 0.3f));
            Debug.Log("이펙트 : 카운터");
            //CounterParticle.Play();
        }


        private void OnParringEffect(EventOnParringEffect OnParringEffect)
        {
            SpawnEffect(ParringParticle, OnParringEffect.position, 90, new Vector3(0.5f, 0.5f, 0.5f));
            Debug.Log("이펙트 : 패링");
            //ParringParticle.Play();
        }
    }
}


