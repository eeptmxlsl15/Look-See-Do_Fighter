using System;
using Photon.Deterministic;
using UnityEngine;

namespace Quantum.LSDF {
  [Serializable]
  public sealed class LSDFJuggleSettings {
    [InspectorName("처음 뜨는 높이"), Tooltip("첫 피격 위치에서 올라가는 월드 높이입니다.")]
    public FP LaunchHeight = FP._0_25 + FP._0_05;
    [InspectorName("처음 상승 시간 (틱)"), Min(1)] public int LaunchRiseTicks = 12;
    [InspectorName("처음 머무는 시간 (틱)"), Tooltip("시동기 후 추가타가 없으면 이 시간이 끝난 뒤 원래 중력으로 자연 낙하합니다."), Min(0)]
    public int LaunchHoldTicks = 24;
    [InspectorName("콤보 기준 높이 (Combo Height)"), Tooltip("시동기 피격 위치 기준입니다. 재타격 시 이 높이보다 조금 높게 올랐다가 복귀한 뒤 천천히 내려갑니다.")]
    public FP ComboHeight = FP._0_25;
    [InspectorName("재타격 추가 상승 높이")] public FP RehitLift = FP._0_05;
    [InspectorName("재타격 상승 시간 (틱)"), Min(1)] public int RehitRiseTicks = 6;
    [InspectorName("기준 높이 복귀 시간 (틱)"), Min(1)] public int RehitReturnTicks = 6;
    [InspectorName("기준 높이 아래 낙하 시작 속도"), Tooltip("기준 높이에 도착할 때의 하강 속도입니다. 이후 낙하 가속도가 더해집니다. 월드 단위/초이며 최소 0.01로 보정합니다.")]
    public FP SlowFallSpeed = FP._0_20;
    [InspectorName("기준 높이 아래 낙하 가속도"), Tooltip("기준 높이 아래에서 서서히 빨라지는 정도입니다. 낮출수록 더 오래 떠 있습니다. 월드 단위/초²이며 0이면 일정 속도로 내려갑니다.")]
    public FP SlowFallAcceleration = FP._0_10;
    // Preserve old serialized hold tuning. Rehits now move continuously through the return and slow fall.
    [HideInInspector] public int RehitHoldTicks = 12;
    [InspectorName("최대 공중 높이"), Tooltip("첫 상승과 기준 높이 + 추가 상승 높이의 상한입니다. 시동기 피격 위치 기준입니다.")]
    public FP MaximumHeight = FP._0_33 + FP._0_02;
    [Header("공중 피격 밀림")]
    [InspectorName("공중 피격 1회 밀림 거리"), Tooltip("시동기와 일반 공중 피격마다 공격자 반대 방향으로 밀릴 월드 거리입니다. 밀림 시간에 나눠 이동하며 0이면 끕니다. 마무리 날리기에는 적용하지 않습니다.")]
    public FP HitPushDistance = FP._0_05;
    // Retain the legacy serialized speed without interpreting its value as a distance.
    [HideInInspector]
    public FP HitPushSpeed = FP._0_20;
    [InspectorName("공중 피격 밀림 시간 (틱)"), Tooltip("밀림 거리를 이동하는 시간입니다. 60Hz에서 6틱은 약 0.1초입니다. 0이면 끕니다. 후속 피격 시 새 방향과 거리로 다시 시작합니다."), Min(0)]
    public int HitPushTicks = 6;
    [InspectorName("마무리 날리기 가로 속도")] public FP EnderSpeed = 4;
    [InspectorName("마무리 날리기 위쪽 속도")] public FP EnderUpSpeed = FP._1 + FP._0_50;
    [InspectorName("날아갈 때 콜라이더 위치"), Tooltip("5타 또는 마무리 기술로 멀리 날아가는 동안만 적용되는 콜라이더 중심입니다. X는 오른쪽(+), Y는 위쪽(+)인 월드축 기준이며 착지·회복 시 원래 위치로 복구합니다. 캐릭터 위치와 콜라이더 크기는 바꾸지 않습니다.")]
    public FPVector2 EnderColliderCenter = new FPVector2(FP._0, FP._0_01);
    [InspectorName("일어날 때 콜라이더 위치"), Tooltip("누워 있다가 일어나는 애니메이션 동안만 적용되는 콜라이더 중심입니다. X는 오른쪽(+), Y는 위쪽(+)인 월드축 기준입니다. 기상이 끝나거나 중단되면 현재 자세의 중심으로 복구합니다. 콜라이더 크기는 유지합니다.")]
    public FPVector2 GetUpColliderCenter = new FPVector2(FP._0, FP._0_01);
    [InspectorName("마무리 착지 후 회복 (틱)"), Min(1)] public int EnderRecoveryTicks = 24;
  }
}
