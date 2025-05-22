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

            RuntimePlayer data = f.GetPlayerData(player);
            var entityPrototypeAsset = f.FindAsset<EntityPrototype>(data.PlayerAvatar);
            var playerEntity = f.Create(entityPrototypeAsset);

            f.Add(playerEntity, new PlayerLink { PlayerRef = player });

            // ��ġ ���� (����, ������)
            FPVector2 spawnPos = player == (PlayerRef)0 ? new FPVector2(-FP._0_50, 0) : new FPVector2(FP._0_50, 0);

            
            f.Set(playerEntity, new Transform2D
            {
                Position = spawnPos,
            });

            //CommandSkillMap �ʱ�ȭ
            f.Unsafe.TryGetPointer<LSDF_Player>(playerEntity, out var LSDF_player);
                //�ʱ�ȭ��
            //for (int i = 0; i < 28; i++)
            //{
            //    LSDF_player->CommandSkillMap[i] = 0;
            //    PlayerPrefs.SetInt($"Skill_{i}", 0);
            //}

            
            for (int i = 0; i < 28; i++)
            {
                LSDF_player->CommandSkillMap[i] = PlayerPrefs.GetInt($"Skill_{i}", 0); // �⺻���� 0
                Debug.Log("Ŀ�ǵ� : " + LSDF_player->CommandSkillMap[i]);
            }
            //ü�� �ʱ�ȭ
            LSDF_player->playerHp = 170;

            f.Global->Time = 60;
            

        }
    }
}