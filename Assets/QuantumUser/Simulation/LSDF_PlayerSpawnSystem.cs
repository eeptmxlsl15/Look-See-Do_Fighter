using Photon.Deterministic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_PlayerSpawnSystem : SystemSignalsOnly, ISignalOnPlayerAdded
    {
        public void OnPlayerAdded(Frame f, PlayerRef player, bool firstTime)
        {

            if (!LSDFSpawnPolicy.CanSpawn(f, player)) return;
            RuntimePlayer data = f.GetPlayerData(player);
            if (data == null || !data.PlayerAvatar.Id.IsValid) return;
            var entityPrototypeAsset = f.FindAsset<EntityPrototype>(data.PlayerAvatar);
            if (entityPrototypeAsset == null) return;
            var playerEntity = f.Create(entityPrototypeAsset);
            if (!f.Has<LSDF_Player>(playerEntity) || !f.Has<AnimatorComponent>(playerEntity) || !f.Has<PhysicsBody2D>(playerEntity)) {
                f.Destroy(playerEntity);
                return;
            }

            f.Add(playerEntity, new PlayerLink { PlayerRef = player });

            // 위치 설정 (왼쪽, 오른쪽)
            FPVector2 spawnPos = player == (PlayerRef)0 ? new FPVector2(-LSDFBalance.SpawnOffset(f), 0) : new FPVector2(LSDFBalance.SpawnOffset(f), 0);

            
            f.Set(playerEntity, new Transform2D
            {
                Position = spawnPos,
            });

            //CommandSkillMap 초기화
            if (!f.Unsafe.TryGetPointer<LSDF_Player>(playerEntity, out var LSDF_player)) return;
            //초기화용
            //for (int i = 0; i < LSDFLoadout.SlotCount; i++)
            //{
            //    LSDF_player->CommandSkillMap[i] = 0;
            //}

            var validatedSkills = LSDFLoadout.CopyBudgetValidated(data.CommandSkills, LSDFBalance.Config(f));
            for (int i = 0; i < LSDFLoadout.SlotCount; i++)
            {
                LSDF_player->CommandSkillMap[i] = validatedSkills[i]; // 기본값은 0
                Debug.Log("커맨드 : " + LSDF_player->CommandSkillMap[i]);
            }
            //체력 초기화
            LSDF_player->playerHp = LSDFBalance.MaximumHealth(f);

            if (firstTime && player == (PlayerRef)0) f.Global->Time = LSDFBalance.RoundSeconds(f);
            

        }
    }
}