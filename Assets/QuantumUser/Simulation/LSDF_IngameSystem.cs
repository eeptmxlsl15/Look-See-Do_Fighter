using Photon.Deterministic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_IngameSystem : SystemMainThread
    {
        private const int START_HP = 170;
        private const int CANT_MOVE_DURATION_FRAMES = 180; // 예: 3초 (60FPS 기준)
        private const int ROUND_TIME_SECONDS = 60;
        private const int MAX_ROUND_WINS = 3;

        

        public override void Update(Frame f)
        {
            // 두 플레이어의 Entity 찾기
            EntityRef p1 = default;
            EntityRef p2 = default;

            if (p1 == default || p2 == default)
            {
                foreach (var pair in f.GetComponentIterator<LSDF_Player>())
                {
                    var entity = pair.Entity;
                    var link = f.Get<PlayerLink>(entity);

                    if (link.PlayerRef == 0)
                    {
                        p1 = entity;

                    }
                    else
                    {
                        p2 = entity;

                    }


                }
            }


            if (!p1.IsValid || !p2.IsValid)
                return;

            var hp1 = f.Get<LSDF_Player>(p1).playerHp;
            var hp2 = f.Get<LSDF_Player>(p2).playerHp;





            // 한쪽이라도 죽었을 때
            if (hp1 <= 0 || hp2 <= 0)
            {
                


                    if (hp1 <= 0)
                    {

                        var player = f.Get<LSDF_Player>(p1);
                        player.loseRound++;
                        f.Set(p1, player);
                        Debug.Log("1P 패배");
                    }
                    if (hp2 <= 0)
                    {
                        var player = f.Get<LSDF_Player>(p2);
                        player.loseRound++;
                        f.Set(p2, player);
                        Debug.Log("2P 패배");
                    }

                    // 양쪽 모두 리셋
                    ResetPlayer(f, p1, (PlayerRef)0);
                    ResetPlayer(f, p2, (PlayerRef)1);
                



            }
            
            

        }

        private void ResetPlayer(Frame f, EntityRef entity, PlayerRef playerRef)
        {
            
            if (!f.Unsafe.TryGetPointer<Transform2D>(entity, out var transform)) return;
            if (!f.Unsafe.TryGetPointer<LSDF_Player>(entity, out var player)) return;
            if (!f.Unsafe.TryGetPointer<PhysicsBody2D>(entity, out var body)) return;

            // 자리 리셋 나중에 벽이 생기면 하자
            //transform->Position = playerRef == (PlayerRef)0 ? new FPVector2(-FP._0_50, 0) : new FPVector2(FP._0_50, 0);

            //0으로 되면 계속 지게 되서 한번 1로 올려야함
            player->playerHp ++;
            
            //못움직이고 3초후에 움직임
            player->tickToEnableMove = f.Number + CANT_MOVE_DURATION_FRAMES;
            body->Velocity.X = 0;

            



        }
    }
}