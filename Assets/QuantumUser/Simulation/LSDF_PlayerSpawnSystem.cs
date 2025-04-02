using UnityEngine;
using UnityEngine.Scripting;

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


        }

        
    }
}
