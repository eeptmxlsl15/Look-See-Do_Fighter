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

            // ȸ�� ���� (�� ��° �÷��̾�� 180�� ȸ�� �� �ݴ� ���� �ٶ�)
            FP rotation = player == (PlayerRef)0 ? FP._0 : FP.Pi;

            f.Set(playerEntity, new Transform2D
            {
                Position = spawnPos,
                Rotation = rotation
            });
        }
    }
}