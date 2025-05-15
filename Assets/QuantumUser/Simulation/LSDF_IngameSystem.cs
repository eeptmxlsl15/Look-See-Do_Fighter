using Photon.Deterministic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_IngameSystem : SystemMainThread
    {
        private const int START_HP = 170;
        private const int CANT_MOVE_DURATION_FRAMES = 210;
        private const int ROUND_TIME_SECONDS = 60;
        private const int MAX_ROUND_WINS = 3;

        
        private int lastTimeTickFrame;

        public override void Update(Frame f)
        {
            // 두 플레이어의 Entity 찾기
            EntityRef p1Ref = default;
            EntityRef p2Ref = default;

            if (p1Ref == default || p2Ref == default)
            {
                foreach (var pair in f.GetComponentIterator<LSDF_Player>())
                {
                    var entity = pair.Entity;
                    var link = f.Get<PlayerLink>(entity);

                    if (link.PlayerRef == 0)
                    {
                        p1Ref = entity;

                    }
                    else
                    {
                        p2Ref = entity;

                    }


                }
            }

            //둘 다 있을 경우
            if (!p1Ref.IsValid || !p2Ref.IsValid)
                return;

            var hp1 = f.Get<LSDF_Player>(p1Ref).playerHp;
            var hp2 = f.Get<LSDF_Player>(p2Ref).playerHp;

            var player1 = f.Get<LSDF_Player>(p1Ref);
            var player2 = f.Get<LSDF_Player>(p2Ref);

            //타이머



            //if (!player1.isCantMove && !player2.isCantMove)
            //{
            //    // 현재 프레임 기준에서 1초(60프레임) 지났는지 확인
            //    if (f.Number - lastTimeTickFrame >= 60)
            //    {
            //        lastTimeTickFrame = f.Number;

            //        if (f.Global->Time > 0)
            //        {
            //            f.Global->Time--;
            //            var roundTime = f.GetSingleton<LSDF_Timer>();
            //            roundTime.currentTime = f.Global->Time;
            //            f.SetSingleton(roundTime);
            //        }
            //    }
            //}
            




            // 한쪽이라도 죽었을 때
            if (hp1 <= 0 || hp2 <= 0 )
            {



                if (hp1 <= 0)
                {

                    
                    player1.loseRound++;

                    f.Set(p1Ref, player1);
                    Debug.Log("1P 패배");
                }
                if (hp2 <= 0)
                {
                    
                    player2.loseRound++;
                    f.Set(p2Ref, player2);
                    Debug.Log("2P 패배");
                }

                // 양쪽 모두 리셋
                ResetPlayer(f, p1Ref, (PlayerRef)0);
                ResetPlayer(f, p2Ref, (PlayerRef)1);




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
            if (player->playerHp == 0)
            {
                player->playerHp++;

            }
            
            //못움직이고 3초후에 움직임
            player->tickToEnableMove = f.Number + CANT_MOVE_DURATION_FRAMES;
            body->Velocity.X = 0;

            



        }
    }
}