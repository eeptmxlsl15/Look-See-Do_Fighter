using Photon.Deterministic;
using UnityEngine;

namespace Quantum.LSDF
{
    public class LSDF_GameConfig : AssetObject
        //Assetobjcet로 상속하면 이하의 변수들을 퀀텀 시뮬레이션에서 사용할 수 있고 
        //스크립터블 오브젝트를 상속하고 있어 유니티에서 편집 가능함
    {
        [Header("Asteroids configuration")]
        //유니티에서 프리펩을 스폰하듯이
        //퀀텀에서 엔티티프로로타입을 스폰하기위해서 만든건데
        //AssetRef<클래스명>을 넣어줘야 에셋들을 퀀텀 시뮬레이션에서 참조 할 수 있다
        //TODO 이펙트를 여기서 만들듯
        //[Tooltip("Prototype reference to spawn asteroids")]
        //public AssetRef<EntityPrototype> AsteroidPrototype;
        
        //[Tooltip("Speed applied to the asteroid when spawned")]
        public FP AsteroidInitialSpeed = 8;
        //[Tooltip("Minimum torque applied to the asteroid when spawned")]
        //public FP AsteroidInitialTorqueMin = 7;
        //[Tooltip("Maximum torque applied to the asteroid when spawned")]
        //public FP AsteroidInitialTorqueMax = 20;
        //[Tooltip("Distance to the center of the map. This value is the radius in a random circular location where the asteroid is spawned")]
        //public FP AsteroidSpawnDistanceToCenter = 20;
        //[Tooltip("Amount of asteroids spawned in level 1. In each level, the number os asteroids spawned is increased by one")]
        //public int InitialAsteroidsCount = 5;
        //[Header("Map configuration")]
        //[Tooltip("Total size of the map. This is used to calculate when an entity is outside de gameplay area and then wrap it to the other side")]
        //public FPVector2 GameMapSize = new FPVector2(25, 25);

        //public FPVector2 MapExtends => _mapExtends;

        //private FPVector2 _mapExtends;
        //public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
        //{
        //    base.Loaded(resourceManager, allocator);

        //    _mapExtends = GameMapSize / 2;
        //}

    }
}