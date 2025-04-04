using Photon.Deterministic;
using UnityEngine;
using UnityEngine.Scripting;
using static UnityEngine.EventSystems.EventTrigger;

namespace Quantum.LSDF
{
    [Preserve]
    public unsafe class LSDF_PlayerSpawnSystem : SystemSignalsOnly, ISignalOnPlayerAdded//�÷��̾ �߰��ƴٴ� ��ȣ�� ������ OnPlayerAdded �Լ� ���� 
    {
        public void OnPlayerAdded(Frame f, PlayerRef player, bool firstTime)
        {
            
            RuntimePlayer data = f.GetPlayerData(player);

            var entityPrototypeAsset = f.FindAsset<EntityPrototype>(data.PlayerAvatar);

            var playerEnitiy = f.Create(entityPrototypeAsset);

            f.Add(playerEnitiy, new PlayerLink { PlayerRef = player });

            FPVector2 spawnPos = player == (PlayerRef)0 ? new FPVector2(-FP._0_50, 0) : new FPVector2(FP._0_50, 0);
            f.Set(playerEnitiy, new Transform2D { Position = spawnPos });

            

        }


    }
}
