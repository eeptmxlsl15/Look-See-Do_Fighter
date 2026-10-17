using System;
using Photon.Deterministic;
using UnityEngine;

namespace Quantum.LSDF {
  [Serializable]
  public sealed class LSDFJuggleSettings {
    [InspectorName("처음 뜨는 높이"), Tooltip("첫 피격 위치에서 올라가는 월드 높이입니다.")]
    public FP LaunchHeight = FP._0_25 + FP._0_05;
    [InspectorName("처음 상승 시간 (틱)"), Min(1)] public int LaunchRiseTicks = 12;
    [InspectorName("처음 머무는 시간 (틱)"), Min(0)] public int LaunchHoldTicks = 24;
    [InspectorName("재타격 고정 높이"), Tooltip("시동기가 맞은 위치 기준입니다. 공중 후속타마다 이 높이로 즉시 되돌리고 세로 속도를 0으로 초기화합니다.")]
    public FP ComboHeight = FP._0_25;
    // Preserve serialized values for compatibility; fixed-height rehits no longer add lift or rise gradually.
    [HideInInspector] public FP RehitLift = FP._0_05;
    [HideInInspector] public int RehitRiseTicks = 6;
    [InspectorName("재타격 머무는 시간 (틱)"), Min(0)] public int RehitHoldTicks = 12;
    [InspectorName("최대 공중 높이"), Tooltip("첫 상승 및 재타격 고정 높이의 상한입니다. 시동기가 맞은 위치 기준입니다.")]
    public FP MaximumHeight = FP._0_33 + FP._0_02;
    [InspectorName("공중 피격 밀림 속도"), Tooltip("낮출수록 공중 피격 때 덜 밀립니다. 마무리 날리기에는 적용하지 않습니다.")]
    public FP HitPushSpeed = FP._0_20;
    [InspectorName("공중 피격 밀림 시간 (틱)"), Min(0)] public int HitPushTicks = 6;
    [InspectorName("마무리 날리기 가로 속도")] public FP EnderSpeed = 4;
    [InspectorName("마무리 날리기 위쪽 속도")] public FP EnderUpSpeed = FP._1 + FP._0_50;
    [InspectorName("마무리 착지 후 회복 (틱)"), Min(1)] public int EnderRecoveryTicks = 24;
  }
}
