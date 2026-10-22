# 공중 콤보 후속타 피해 70/50/30% 적용

기준: `dev/Prototype`, HEAD `97b6acb020a672193d44053edc955d109e8759b7`.
사용자 요청: 공중 콤보 1타 70%, 2타 50%, 3·4·5타 30%.
기존 콤보 계약에 따라 지상에서 띄우는 시동기는 제외하고 공중 후속타부터 1타로 센다.
[원인 조사](homing-air-damage-investigation-2026-10-22.md)의 피해 누락 경로를 수정한다.

## 동작

| 접촉 | 기본 피해 20 | 기본 피해 10 |
|---|---:|---:|
| 지상 일반 시동기 | 20 | 10 |
| 공중 후속 1타 (70%) | 14 | 7 |
| 공중 후속 2타 (50%) | 10 | 5 |
| 공중 후속 3·4·5타 (30%) | 각각 6 | 각각 3 |

정수 백분율 곱셈 후 소수점을 버린다. 최소 피해 1을 새로 강제하지 않으므로 기본 피해가 매우 작으면 버림 결과가 0일 수 있다.
지상 카운터의 기존 120%는 유지하고, 진행 중인 공중 콤보는 카운터 처리 경로에서도 해당 타수의 70/50/30%를 적용한다.
5번째 타격으로 날리기 상태에 들어가더라도 그 타격의 30% 피해를 적용한다. 이후 피격 차단은 기존 Combat/Juggle 경로가 유지한다.
조기 마무리도 실제 후속타 순서의 배율을 쓴다. 착지·취소 뒤 다음 시동기는 100%, 다음 공중 1타는 다시 70%다.

## 구현

- [LSDFPlayerHealth](../../Assets/QuantumUser/Simulation/LSDFPlayerHealth.cs): `ApplyHit`가 현재 Frame의
  `LSDF_AirCombo.ComboActive/HitCount`를 읽는다. `LSDF_Player.hitCount`는 더 이상 피해 보정에 쓰지 않는다.
  곱셈 중간값에 `long`을 사용해 큰 기본 피해의 정수 overflow와 FP 소수 근사 경계 오차를 피한다.
- [Combat](../../Assets/QuantumUser/Simulation/LSDF_PlayerSystem.Combat.cs): 일반/카운터 모두 Juggle 접촉 등록 뒤 `ApplyHit`에 연결한다.
  구 배율·카운트를 출력하던 정상 피격 로그도 실제 HP 로그로 바꿨다. 비ASCII 기존 바이트는 그대로 보존하는 ASCII 치환만 수행했다.
- [프로토콜](../../Assets/QuantumUser/Simulation/LSDFLoadout.cs): 결과가 다른 클라이언트가 같은 방에 섞이지 않도록
  `lsdf-loadout-v14-air-combo-damage`로 변경한다.
- [체력 테스트](../../Assets/Tests/Editor/LSDFPlayerHealthTests.cs): 전 타수·정확한 백분율 경계·정수 버림·큰 정수·지상 카운터/체력 처리를 확인한다.
- [Frame/그래프 테스트](../../Assets/Tests/Editor/LSDFHitReactionTests.cs): 기존 중단 경계 38개에 피해 시나리오 8개를 추가했다.
  Player/Enemy 그래프 × 양쪽 피격자 × 일반/카운터 시동기를 조합하고, 실제 Juggle 등록→Air 전환→피해 적용 연산으로
  후속타 1~5의 HP, 상대 엔티티 격리, 구 hitCount 1/3과 무관한 피해, Frame 복원 후 동일 체크섬,
  콤보 취소 뒤 새 시동기/첫 후속 마무리를 관측한다. 사용자 생성 Frame용 heap 1개를 예약한다.

새 가변 상태나 QTN/생성 코드 변경은 없다. 타수는 기존 Frame 컴포넌트를 쓰고 자산에 경기 상태를 저장하지 않는다.
SDK/애드온, 시스템 등록 순서, 판정 프레임, 히트박스 수명, 5회 제한과 이동 궤적은 변경하지 않는다.
KazumiHoming 참조 누락과 기존 삭제된 사용자 에셋은 이번 피해 요청 범위에 포함하지 않았다.

## 최종 한 차례 검증

정적 정책, Unity 컴파일/콘솔, 체력·공중·마무리·라운드·실제 Frame/그래프 시나리오, 변경 공백/문서 링크를 한 차례 실행했다.
결과는 [검증 근거](evidence/air-combo-damage-scaling-validation.json)에 기록했다. 실패 후 자동 재실행하지 않았다.

| 검사 | 결과 |
|---|---|
| Simulation 정적 정책 | PASS — 94개 파일, 신규 적중 0 |
| Unity 컴파일 | PASS — 오류 0, 기존 AvailableScenes obsolete 경고 5개. 아래 fixture 보정 전 결과 |
| 체력·공중·마무리·라운드 | PASS — 체력 32, 공중 36, 마무리 22, 라운드 19 = 109개 |
| 실제 Frame/그래프 통합 | FAIL — NUnit 1개 안의 내부 46개 사례가 구 entry count 1 예상/2 관측으로 실패 |
| 문서 링크/JSON/공백 | PASS — 상대 링크 46개, 누락·끝 공백 0, JSON 파싱 및 git diff --check 종료 0. fixture 보정 전 결과 |
| 전체 replay/강제 온라인 rollback/교차 플랫폼 | NOT RUN |

job `be083f39f32b4b939f46877fd69b37e6`: **110개 중 109 PASS / 1 FAIL / SKIP 0**.
신규 피해 8개 사례는 첫 후속타 HP 확인 뒤 구 카운트 불변 Assert에서 중단됐다.
따라서 이 통합 fixture의 후속 2~5타·최종 Frame 복원 비교·새 콤보 완료를 검증했다고 주장하지 않는다.

실패 원인에 관련된 현재 SDK/그래프 소스를 읽었다. Air 이벤트 시작은 RawValue 1092이고,
`AnimatorEvent.Evaluate`가 `LastTime <= Time`을 사용해 정확히 경계에 놓인 첫 두 평가에서 OnEnter가 다시 호출될 수 있다.
이는 `EnterAir`가 애니메이션 시간을 되감았다는 증거와 구분한다. 이번 피해 코드는 구 entry count를 사용하지 않는다.
실패 뒤 테스트만 보정했다. 기존 중단 검사는 재피격 직후 시간 보존/다음 평가 후 시간 진행을 직접 확인하고,
신규 피해 검사는 구 hitCount 1/3을 주입해 피해 독립성을 확인하도록 했다. **fixture 수정 후 미검증**이다.
제품 코드/배율/프로토콜은 검사 후 변경하지 않았고 정적·컴파일·테스트·공백 검사를 재실행하지 않았다.

Frame fixture는 실제 피해 함수를 실행하지만 물리 TriggerInfo2D dispatch/전체 입력 스트림 매치 재현을 대신하지 않는다.
전체 입력 replay·강제 온라인 rollback·교차 플랫폼 결정론은 실행하지 않으면 NOT RUN으로 남긴다.
이전 작업의 자동 FAIL/NOT RUN/수정 후 미검증 이력을 이번 결과로 바꾸지 않는다.

기존 IDE 수정/미추적 csproj, Rk_Enemy KazumiHoming 삭제 75개, 앞선 조사 문서/근거와 current-state 변경을 보존한다.
staging/커밋/게시하지 않는다. 새 구현 기록과 검증 JSON은 미추적이다.
