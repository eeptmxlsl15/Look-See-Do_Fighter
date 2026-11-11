param([int]$PreviewCount=0)
. ([scriptblock]::Create((Get-Content -Raw -Encoding UTF8 'C:/Users/Admin/Look-See-Do_Fighter/tmp/portfolio-astra/helpers.ps1')))
$C.Muted='595959';$C.Line='D9DDE2';$C.Pale='D9DDE2'
function Label($s,$txt,$x,$y,$w=3.5){T $s $txt $x $y $w .3 14 $C.Blue $true}
function Body($s,$txt,$x,$y,$w,$h=1){T $s $txt $x $y $w $h 18 $C.Body}
function Heading($s,$txt,$x,$y,$w,$size=22){T $s $txt $x $y $w .5 $size $C.Ink $true}
function VLine($s,$x,$y,$h,$color='D9DDE2'){$l=$s.Shapes.AddLine($x*72,$y*72,$x*72,($y+$h)*72);$l.Line.ForeColor.RGB=Col $color;$l.Line.Weight=[single]1}
function Dot($s,$x,$y,$size=.10,$color='0070D1'){$d=$s.Shapes.AddShape(9,$x*72,$y*72,$size*72,$size*72);$d.Fill.ForeColor.RGB=Col $color;$d.Line.Visible=0}
function Evidence($s,$txt){T $s $txt .75 6.48 11.83 .36 14 $C.Body}
function Box($s,$title,$detail,$x,$y,$w,$h=1.2,$blue=$false){
  Rect $s $x $y $w $h $(if($blue){$C.Blue}else{$C.Soft})
  $ink=$(if($blue){$C.White}else{$C.Ink})
  T $s $title ($x+.2) ($y+.17) ($w-.4) .37 19 $ink $true
  T $s $detail ($x+.2) ($y+.65) ($w-.4) ($h-.75) 16 $ink
}
$app=$null;$deck=$null;$hadApp=$false
try{
  try{$app=[Runtime.InteropServices.Marshal]::GetActiveObject('PowerPoint.Application');$hadApp=$true}catch{$app=New-Object -ComObject PowerPoint.Application}
  $app.Visible=-1;$originalAlerts=$app.DisplayAlerts;$app.DisplayAlerts=1
  $deck=$app.Presentations.Add(0);$deck.PageSetup.SlideWidth=960;$deck.PageSetup.SlideHeight=540

  # 01: Quiet typographic cover. No composited scene or decorative art.
  $s=$deck.Slides.Add(1,12);$s.FollowMasterBackground=0;$s.Background.Fill.ForeColor.RGB=Col $C.Black
  T $s '김석진  /  AI Native · 클라이언트 개발' .75 .55 11.8 .35 16 $C.White
  T $s "1년 멈춘 격투게임,`rAI와 다시 만든 6일" .75 1.7 11.8 1.65 44 $C.White $true
  Rule $s .75 4.05 11.83 $C.Blue 2
  T $s 'Look-See-Do Fighter' .75 4.55 11.8 .6 30 $C.White $true
  T $s "전투 상태의 구조부터 입력 감각, 공중 콤보, 경기 종료까지.`r기존 프로젝트를 다시 플레이 가능한 흐름으로 다듬은 기록입니다." .75 5.48 11.6 .95 18 $C.Pale
  Footer $s $true
  Notes $s '기간과 중단 맥락은 사용자 설명. 6일은 작업 흐름을 재구성한 기간이며 정확한 날짜별 배치를 뜻하지 않음. SDK와 원본 아트는 기존 프로젝트 기반이다. 모든 게임 기능이나 상품 출시 완료를 주장하지 않는다. 기준 b571a8f15e617788a0c86fe6507e992146f1800c. 사용자가 GitHub를 제외하고 이메일을 제공하지 않아 넣지 않았다.'

  # 02: Product context, large genuine screenshot if delivered.
  $s=Standard '01  PROJECT' 'Look-See-Do Fighter' '방향과 공격 버튼에 기술을 배치하는 Photon Quantum 3 기반 2D 격투게임'
  $capture=Join-Path $build 'assets/gameplay.png'
  if(Test-Path $capture){Pic $s $capture .75 2.23 8.0 4.12;T $s '프로젝트 게임 화면' .75 6.48 8 .3 14 $C.Body}
  else{
    T $s '대기 동작' .75 2.28 3.7 .4 20 $C.Ink $true
    T $s '어퍼 동작' 4.95 2.28 3.7 .4 20 $C.Ink $true
    Pic $s (Join-Path $root 'Assets/Sprites/Character/Player/Move/Idle/Look-See-Do_Bot_export_Idle_01.png') 1.35 2.95 2.45 2.55
    Pic $s (Join-Path $root 'Assets/Sprites/Character/Player/Attack/Lp/LeftUpper/Look-See-Do_Bot_export_LeftUpper_20.png') 5.55 2.95 2.45 2.55
    Rule $s .75 5.81 7.9 $C.Line
    T $s '7 방향 × 4 버튼, 28개 슬롯을 플레이어별로 구성' .75 6.03 7.9 .4 18 $C.Ink
    T $s '프로젝트 원본 동작 에셋 / 실행 화면 아님' .75 6.57 7.9 .3 14 $C.Body
  }
  VLine $s 9.12 2.24 4.42
  Label $s '이번 재개발의 초점' 9.48 2.27 3.1
  Heading $s '전투 구조' 9.48 2.82 3.1
  Body $s '플레이어 데이터와`rFrame 상태의 소유권'.Replace('`r',"`r") 9.48 3.38 3.1 .85
  Heading $s '플레이 흐름' 9.48 4.45 3.1
  Body $s "선입력과 공중 콤보`r라운드 종료와 메뉴 복귀" 9.48 5.02 3.1 1.1
  Notes $s '환경: Unity 6000.3.26f1, Photon Quantum 3.0.14.2241, C#, Quantum DSL(.qtn). 근거 docs/ai/evidence/2026-10-17-portfolio-summary.md. 화면을 삽입했다면 부모 작업에서 수집한 실제 게임/Editor 화면이며 별도 이미지 출처 기록을 따른다. SDK, 애드온, 원본 아트 제작은 작성자 기여로 산정하지 않는다.'

  # 03: Six phases in two aligned bands, not a table of dates.
  $s=Standard '01  RESTART' '6일 동안 무엇을 바꿨는가' '기존 전투를 보존하면서 구조를 정리하고, 실제 플레이의 불편을 고쳤습니다.'
  $items=@(
    @('01','기준을 세우다','기존 코드와 참조 조사','경계와 보존 기준 정리'),
    @('02','상태를 옮기다','로컬 스킬과 공유 버퍼 분리','플레이어 데이터·Frame 연결'),
    @('03','공격을 정리하다','37종 타격 생성 공통화','기존 판정 시점 유지'),
    @('04','입력을 이어주다','선입력과 라운드 진행 추가','공중 재피격·낙하 보완'),
    @('05','체감을 다듬다','카메라·체력바·VFX 연결','실제 피해 타수 수정'),
    @('06','끝까지 연결하다','3승 종료와 메뉴 복귀','직접 플레이 확인·기록 정리')
  )
  for($idx=0;$idx -lt 6;$idx++){
    $xx=.75+($idx%3)*4.06;$yy=2.24+[math]::Floor($idx/3)*2.12;$v=$items[$idx]
    T $s $v[0] $xx $yy .65 .42 23 $C.Blue $true
    T $s $v[1] $xx ($yy+.55) 3.70 .42 21 $C.Ink $true
    T $s ($v[2]+"`r"+$v[3]) $xx ($yy+1.14) 3.70 .78 16 $C.Body
  }
  T $s '설명을 위한 작업 순서이며, 실제 날짜별 작업량과는 다릅니다.' .75 6.62 11.83 .3 14 $C.Body
  Notes $s '정확한 날짜별 배치는 중요하지 않으며 러프하게 재구성해도 된다는 사용자 지시를 반영한 설명 순서. 실제 작업은 병행·재방문했다. docs/ai/current-state.md 및 evidence/2026-10-17-portfolio-summary.md, air-combo-damage-scaling.md, match-return-to-main.md.'

  # 04: Clear left-to-right architecture at one height.
  $s=Standard '02  CASE STUDY / 상태의 소유권' '로컬 선택값을 경기 데이터로 전달' '각 클라이언트의 저장값을 Simulation이 직접 읽던 경로를 정리했습니다.'
  Label $s '문제' .75 2.25
  Body $s 'PlayerPrefs에서 읽은 기술 선택이 Quantum의 플레이어 데이터 전달 경로를 우회했습니다.' .75 2.7 11.8 .6
  $xs=@(.75,3.78,6.81,9.84)
  $names=@('메뉴에서 선택','입력 계약 검사','RuntimePlayer','Frame 스킬맵')
  $details=@('로컬 저장값 읽기','28슬롯 ID·비용 검증','AddPlayer로 전달','플레이어별 기술 맵')
  for($idx=0;$idx -lt 4;$idx++){
    Box $s $names[$idx] $details[$idx] $xs[$idx] 3.65 2.74 1.36 ($idx -eq 3)
    if($idx -lt 3){Arrow $s ($xs[$idx]+2.77) 4.33 ($xs[$idx+1]-.06) 4.33}
  }
  T $s '7 × 4 = 28' .75 5.58 3.85 .72 34 $C.Blue $true
  Body $s "슬롯의 순서·길이·ID 의미를 유지했습니다.`rSimulation은 f.GetPlayerData()로 전달받은 값만 읽습니다." 5.0 5.48 7.58 1.12
  Notes $s '근거 docs/ai/loadout-isolation.md, evidence/2026-10-17-portfolio-summary.md. 구현 Assets/QuantumUser/Simulation/LSDFLoadout.cs, LSDF_PlayerSpawnSystem.cs 및 Assets/Photon/QuantumMenu/Runtime/QuantumMenuConnectionBehaviour.LSDF.cs. 수신 시에도 검증하며 잘못된 payload에는 기본값 적용. 기존 사용자 disconnect 원인을 확정했다고 주장하지 않는다.'

  # 05: Semantic ownership diagram with explicit Frame boundary.
  $s=Standard '02  CASE STUDY / 상태의 소유권' '공유 에셋과 플레이어 상태의 분리' '롤백할 때 함께 돌아가야 하는 값은 Frame이 소유하도록 옮겼습니다.'
  Label $s '이전 구조' .75 2.28 4.9
  Heading $s '공유 공격 에셋' .75 2.89 4.95 25
  Body $s "공격 설정과 연타 예약 상태가 공존`r여러 플레이어가 같은 에셋 참조" .75 3.62 4.95 1.1
  T $s 'BufferedNextAttack' .75 5.05 4.95 .5 22 $C.Ink $true
  Body $s 'Frame 밖에 남는 플레이어별 실행 상태' .75 5.65 4.95 .8
  VLine $s 6.25 2.28 4.25
  Label $s '변경한 구조' 6.75 2.28 5.83
  Box $s '불변 AssetObject' '피해·타이밍·타격 형상' 6.75 2.92 5.83 1.27
  Box $s 'Frame 컴포넌트' '입력 버퍼·연타 예약·공중 콤보 진행' 6.75 4.47 5.83 1.27 $true
  T $s '호출 중 임시 계산은 지역변수에 유지' 6.75 6.06 5.83 .4 17 $C.Body
  Notes $s '근거 docs/ai/refactor-p0-result.md, refactor-phase4-result.md, evidence/2026-10-17-portfolio-summary.md. 잽 BufferedNextAttack을 QTN 플레이어 상태로 이관. currentFrame 지역변수화, 충돌 가드 static 실행 상태 제거. QTN은 설치 CodeGen으로 생성. 구조 자체가 강제 네트워크 rollback 검증을 뜻하지 않음.'

  # 06: Attack refactor plus true timing contract.
  $s=Standard '02  CASE STUDY / 판정 보존' '37종 공격을 하나의 생성 경로로' '중복을 줄이면서 타격 시점과 기존 에셋 참조를 그대로 보존했습니다.'
  T $s '37' .75 2.27 2.3 1.05 58 $C.Blue $true
  T $s '공격의 타격 정의' .75 3.4 2.85 .45 20 $C.Ink $true
  Body $s '단타·다단히트 데이터를`r불변 정의로 분리'.Replace('`r',"`r") .75 4.08 2.9 1.1
  VLine $s 4.02 2.3 3.7
  Heading $s '공통 HitboxFactory' 4.55 2.34 7.9 24
  Body $s '공격 이벤트는 이동·연계를 유지하고, 히트박스 구성만 공통 생성기로 모았습니다.' 4.55 3.04 7.9 .9
  Rule $s 4.75 4.86 7.35 $C.Ink 1.4
  Dot $s 5.33 4.8 .14;Dot $s 10.61 4.8 .14
  T $s 'StartupFrames - 1' 4.55 4.12 4.3 .45 22 $C.Blue $true
  T $s '생성 tick + 1' 9.25 4.12 3.33 .45 22 $C.Blue $true
  T $s '히트박스 생성' 4.55 5.25 4.3 .45 18 $C.Ink
  T $s '히트박스 파괴' 9.25 5.25 3.33 .45 18 $C.Ink
  Evidence $s 'GUID · Quantum asset ID · Animator 연결 · 시스템 실행 순서를 보존했습니다.'
  Notes $s '근거 docs/ai/refactor-phase4-result.md, evidence/remaining-attack-contracts.json, evidence/2026-10-17-portfolio-summary.md. 1+4+2+30=37종. 타임라인은 상대 계약 개념도이며 두 점 사이 실제 전체 공격 지속시간을 비례 표현하지 않는다. 모든 37개 온라인 조합을 전수 검사했다는 주장은 하지 않는다.'

  # 07: Input timing timeline built from measured historic ticks.
  $s=Standard '03  GAMEPLAY / 입력 감각' '행동이 끝나면 선입력을 바로 전달' '잠금 중 누른 방향과 버튼을 보관하고, 명령을 받을 수 있는 첫 tick에 사용합니다.'
  T $s '20 tick' .75 2.3 3.4 .85 40 $C.Blue $true
  Body $s "현재 에셋의 입력 유지 범위`r최신 명령 1개를 Frame에 저장" .75 3.36 3.65 1.15
  VLine $s 4.65 2.3 3.95
  Label $s '과거 두 세션에서 관측한 예' 5.12 2.3 7.46
  Rect $s 5.12 3.45 4.7 .72 $C.Soft
  T $s '행동 잠금' 5.38 3.64 4.1 .32 16 $C.Body
  Rule $s 5.12 4.43 7.46 $C.Ink 1.4
  Dot $s 5.55 4.36 .15;Dot $s 10.28 4.36 .15
  T $s '384' 5.12 4.86 2.45 .6 28 $C.Blue $true
  T $s '392' 9.85 4.86 2.45 .6 28 $C.Blue $true
  T $s '입력 저장' 5.12 5.55 2.8 .42 18 $C.Ink
  T $s '첫 가능 tick에 전달' 9.12 5.55 3.46 .42 18 $C.Ink
  Evidence $s '유지 범위는 대기 시간이 아닙니다. 키를 놓아도 입력 순간의 방향·버튼을 유지합니다.'
  Notes $s '근거 docs/ai/input-buffer.md 및 evidence/fighter-publish-validation.json의 press=384, dispatch=392. 과거 해당 검사 시점의 관측이며 현재 버전 전체 성능·지연 측정으로 사용하지 않는다. 현재 에셋20과 새 설정 초기값10을 구분. 대기명령과 Animator 전달 중 명령을 구분하며 KO/라운드 전환시 정리.'

  # 08: Failure investigation. Strong numerical diagnosis, not a table.
  $s=Standard '03  CASE STUDY / 공중 콤보' '맞았는데 HP가 줄지 않는 이유' '실제 공중 후속타 수와 피해 계산이 참조하는 카운터가 달랐습니다.'
  Label $s '원인 조사에서 재현한 피해 함수' .75 2.28 7.5
  T $s '170' .75 2.85 2.6 .9 44 $C.Ink $true
  Arrow $s 3.35 3.3 5.15 3.3
  T $s '170' 5.6 2.85 2.6 .9 44 $C.Blue $true
  T $s '호출 전 HP' .75 3.95 2.6 .4 18 $C.Body
  T $s '4회 적용 후 HP' 5.6 3.95 3.1 .4 18 $C.Body
  T $s '기본 피해 20' 3.1 2.73 2.7 .35 16 $C.Body $false 2
  T $s '구 카운트 1' 3.1 3.68 2.7 .35 16 $C.Body $false 2
  VLine $s 9.0 2.3 3.85
  Heading $s '두 개의 타수' 9.45 2.35 3.13 24
  Body $s "피해가 읽던 값`rLSDF_Player.hitCount" 9.45 3.1 3.13 1.1
  Body $s "실제 후속타 상태`rLSDF_AirCombo.HitCount" 9.45 4.6 3.13 1.25
  Rule $s .75 5.22 7.8 $C.Line
  Body $s "Air 재진입을 생략하면 구 카운트가 1에 머물 수 있습니다.`r이때 기존 0 피해 분기를 적용하는 경로를 확인했습니다." .75 5.53 7.8 .95
  Notes $s '근거 docs/ai/homing-air-damage-investigation-2026-10-22.md 및 evidence/homing-air-damage-investigation-2026-10-22.json. 읽기 전용 프로브에서 실제 피해 함수에 hitCount1/HP170/base20을 네 번 적용해 HP170 유지 확인. 실제 매치의 3~4타 프레임을 재현한 것으로 주장하지 않는다. 기존 hitCount1/3 0피해 분기와 실제 AirCombo 카운트 분리 확인.'

  # 09: Editable bars as exact specified damage values.
  $s=Standard '03  CASE STUDY / 공중 콤보' '피해를 실제 후속타 수에 연결' '일반·카운터 경로 모두 Frame의 공중 콤보 타수를 기준으로 계산합니다.'
  Label $s '기본 피해 20일 때 적용할 후속타 피해' .75 2.25 8.0
  $values=@(14,10,6,6,6);$perc=@('70%','50%','30%','30%','30%')
  $chartX=.95;$barW=.82;$gap=.54;$base=5.7;$scale=.16
  Rule $s .75 $base 7.35 $C.Line
  for($idx=0;$idx -lt 5;$idx++){
    $xx=$chartX+$idx*($barW+$gap);$hh=$values[$idx]*$scale
    Rect $s $xx ($base-$hh) $barW $hh $C.Blue
    T $s ([string]$values[$idx]) ($xx-.15) ($base-$hh-.62) ($barW+.3) .5 28 $C.Blue $true 2
    T $s ($perc[$idx]) ($xx-.15) 6.0 ($barW+.3) .34 16 $C.Body $false 2
    T $s (($idx+1).ToString()+'타') ($xx-.15) 6.43 ($barW+.3) .3 14 $C.Body $false 2
  }
  VLine $s 8.55 2.3 4.3
  Heading $s '계산 기준' 9.0 2.31 3.58 24
  Body $s "시동기를 제외한 후속 1~5타`r정수 백분율 계산 후 버림" 9.0 3.02 3.58 1.2
  Heading $s '보존한 규칙' 9.0 4.57 3.58 22
  Body $s "지상 카운터 120%`r후속 5타 제한과 기존 궤적" 9.0 5.25 3.58 1.13
  Notes $s '근거 docs/ai/air-combo-damage-scaling.md. 도표는 구현된 계산 규칙의 예시이며 온라인 전체 콤보 측정 그래프가 아니다. 기본20시14/10/6/6/6, 시동기20 별도. long 중간곱 정수 버림. 실제 LSDF_AirCombo.ComboActive/HitCount 사용. 최신 테스트109PASS1FAIL, Frame fixture 보정 후 미검증이며 13장에 표시. 사용자 직접 테스트 완료 보고는 user-validation-air-combo-2026-10-22.md.'

  # 10: A regression caused by lifecycle refactoring, as a concrete lesson.
  $s=Standard '04  CASE STUDY / 클라이언트 연출' '리팩토링 뒤 사라진 타격 이펙트' '이벤트 호출 코드는 남아 있었지만, 수신 객체와 새 활성화 경로가 연결되지 않았습니다.'
  Label $s '발견한 불일치' .75 2.26
  Heading $s 'Start에서 OnActivate로 구독 이동' .75 2.79 11.83 25
  Body $s '메뉴의 EffectManager가 경기 EntityViewUpdater에 연결되지 않았습니다.' .75 3.47 11.83 .8
  Box $s '피격 이벤트' 'Simulation에서 발생' .75 4.62 3.42 1.31
  Arrow $s 4.24 5.27 4.81 5.27
  Box $s '경기 updater' '활성화와 씬 바인딩' 4.9 4.62 3.43 1.31 $true
  Arrow $s 8.4 5.27 8.97 5.27
  Box $s '이펙트 View' '구독 후 VFX 생성' 9.07 4.62 3.51 1.31
  Evidence $s '경기 updater와 효과 수신 객체를 명시적으로 연결하는 방향으로 보완했습니다.'
  Notes $s '근거 docs/ai/hit-effects-investigation.md, hit-effects-prefab-migration.md, effect-opacity-action-flash.md 및 current-state의 후속 사용자 확인. 회귀는 3f45e7d1 Start→OnActivate 전환과 씬 배치 불일치로 소스확인. 편집 모드 IsActive=false만으로 실행재현 주장을 하지 않는다. 후속 씬/updater 연결 구현. 자동 표현 fixture 보정 후 미검증과 사용자의 플레이 확인은 구분. 도식은 보완한 연결 의도/구조다.'

  # 11: Complete play loop with finite match state, not an inventory.
  $s=Standard '04  GAME FLOW' '한 판을 끝내고 메뉴로 돌아오기' '첫 KO에서 멈추던 흐름을 라운드 전환과 최종 승패까지 연결했습니다.'
  $rounds=@(@('1R','승'),@('2R','패'),@('3R','승'),@('4R','패'),@('5R','승'))
  Label $s '5판 3선승 진행 예' .75 2.3
  for($idx=0;$idx -lt 5;$idx++){
    $xx=.75+$idx*1.48
    T $s $rounds[$idx][0] $xx 3.05 1.14 .45 21 $C.Body $true 2
    Rect $s $xx 3.72 1.14 .72 $(if($rounds[$idx][1] -eq '승'){$C.Blue}else{$C.Soft})
    T $s $rounds[$idx][1] $xx 3.89 1.14 .38 20 $(if($rounds[$idx][1] -eq '승'){$C.White}else{$C.Ink}) $true 2
  }
  T $s '3 : 2' .75 5.1 6.9 .72 38 $C.Blue $true
  T $s '예시의 최종 결과' .75 6.0 6.9 .45 18 $C.Body
  VLine $s 8.3 2.3 4.3
  Heading $s '매치 종료 후' 8.8 2.34 3.78 24
  Body $s "결과 표시`r기본 120 tick 유지" 8.8 3.08 3.78 1.0
  Arrow $s 9.0 4.28 9.0 4.66
  Body $s "세션과 경기 씬 정리`r메인 메뉴 표시" 8.8 4.92 3.78 1.12
  Notes $s '근거 docs/ai/best-of-five.md, match-return-to-main.md, user-validation-match-return.md. 왼쪽 승패 순서는 3:2 종료를 설명하기 위한 예시이며 실제 라운드 승자 로그의 재현이 아니다. 최종 결과 표시 기본120tick(60Hz 2초), verified MatchEnd 확인 후 메뉴 복귀. 기존19라운드검사PASS, 신규복귀fixture5개 미실행 이력 및 사용자 직접테스트 완료 보고를 구분. 이번 문서에서 게임 재실행하지 않음.'

  # 12: Concrete human/AI responsibility, two parallel columns and workflow.
  $s=Standard '05  AI COLLABORATION' 'AI와 함께 작업한 방식' '플레이의 기준은 직접 정하고, AI가 소스 조사와 구현을 넓게 수행하도록 했습니다.'
  Label $s '김석진' .75 2.28 5.5
  Heading $s '원하는 동작과 완료 판단' .75 2.84 5.5 24
  Body $s "선입력 20 tick, 공중 피해 배율 지정`r플레이 중 불편과 회귀 증상 전달`r직접 테스트 후 작업물 확인" .75 3.59 5.5 1.65
  VLine $s 6.45 2.28 3.18
  Label $s 'AI' 6.95 2.28 5.63
  Heading $s '조사·구현·근거 정리' 6.95 2.84 5.63 24
  Body $s "코드와 씬을 함께 읽어 원인 추적`rFrame·View 경계에 맞춰 패치 작성`r테스트와 실패 기록을 다음 작업에 연결" 6.95 3.59 5.63 1.65
  Rule $s .75 5.69 11.83 $C.Line
  T $s '작업 규칙으로 유지한 것' .75 6.04 4.1 .45 20 $C.Ink $true
  T $s '기존 변경 보존    판정 계약 유지    실패 기록 인계' 5.3 6.07 7.28 .6 17 $C.Body
  Notes $s '근거 AGENTS.md, evidence/2026-10-17-portfolio-summary.md, input-buffer.md, air-combo-damage-scaling.md, user-validation-air-combo-2026-10-22.md. 요구와 플레이 확인은 사용자, 조사·구현·테스트 작성은 AI협업 산출물. 개별아이디어 최초제안자/세부코드리뷰/AI작성률 미확인으로 수치산정하지 않음. SDK와 외부아트 제작은 기여범위에서 제외.'

  # 13: Evidence and remaining work on a single honest page.
  $s=Standard '05  EVIDENCE' '확인한 결과와 남아 있는 검증' '과거 실행 근거와 현재 상태를 구분해 기록했습니다.'
  Label $s '과거 실제 Photon 2연결 검사' .75 2.25 6.0
  T $s '67' .75 2.86 2.85 .88 44 $C.Blue $true
  T $s '136' 4.12 2.86 2.85 .88 44 $C.Blue $true
  T $s "로드아웃 검사`r체크섬 일치 횟수" .75 3.92 2.9 .92 18 $C.Ink
  T $s "낙하 검사`r체크섬 일치 횟수" 4.12 3.92 2.9 .92 18 $C.Ink
  Body $s '같은 Editor의 독립 Quantum 세션에서 비교했습니다. 입력 재생과 Frame 복원도 확인했습니다.' .75 5.22 6.3 1.05
  VLine $s 7.65 2.28 4.24
  Label $s '최신 공중 피해 자동 검사' 8.15 2.25 4.43
  Heading $s '109 PASS / 1 FAIL' 8.15 2.93 4.43 24
  Body $s "통합 fixture의 구 카운트 예상 불일치`r테스트 보정 후 미검증" 8.15 3.68 4.43 1.1
  T $s '직접 플레이 테스트 결과는 별도로 기록' 8.15 4.98 4.43 .65 16 $C.Ink
  T $s "남은 범위`r독립 빌드·교차 플랫폼·강제 네트워크 rollback`r성능 측정" 8.15 5.65 4.43 1.1 14 $C.Body
  Notes $s '근거 docs/ai/evidence/health-shake-publish-validation.json: 로드아웃67회, 당시235/235PASS. natural-rehit-fall-validation.json: 낙하136회/replay2/Frame복원, 당시106/106PASS. 서로다른 시점 수치를 합산하지 않는다. 두연결은 같은Editor 독립세션이며 독립빌드아님. air-combo-damage-scaling-validation.json: 110중109PASS1FAIL, Frame통합fixture 기대1관측2. fixture보정후미검증. user-validation-air-combo-2026-10-22.md의 사용자테스트완료후 b571a8f1로컬커밋. 최신5타통합완료/replay/강제rollbackNOT RUN. KazumiHoming clip/graph 공격이벤트빈상태는 후속확인항목. 기존다른수정후미검증이력은 current-state에 보존. 과거PASS를최신전체PASS로쓰지않음.'

  # 14: Closing with concrete competence rather than an inflated claim.
  $s=$deck.Slides.Add(14,12);$s.FollowMasterBackground=0;$s.Background.Fill.ForeColor.RGB=Col $C.Black
  T $s '김석진' .75 .65 11.83 .6 30 $C.White $true
  T $s "게임의 규칙을 이해하고,`rAI의 구현을 끝까지 연결합니다" .75 1.88 11.83 1.58 38 $C.White $true
  Rule $s .75 4.02 11.83 $C.Blue 2
  T $s '상태의 소유권' .75 4.65 3.7 .5 22 $C.White $true
  T $s '플레이 중 문제 해결' 4.82 4.65 3.7 .5 22 $C.White $true
  T $s '확인 가능한 협업' 8.89 4.65 3.7 .5 22 $C.White $true
  T $s "Quantum 입력과 Frame`rSimulation·View 경계" .75 5.48 3.7 .96 17 $C.Pale
  T $s "실제 타수와 피해의 연결`r씬·이벤트 수명 추적" 4.82 5.48 3.7 .96 17 $C.Pale
  T $s "사람의 판단과 AI 구현 구분`r실행 결과와 한계 기록" 8.89 5.48 3.7 .96 17 $C.Pale
  Footer $s $true
  Notes $s '요약은 본문에 설명한 실제 구현범위에 근거. 전체게임상용완성/성능향상률/전면검증완료를 주장하지 않는다. 주요근거 docs/ai/evidence/2026-10-17-portfolio-summary.md, loadout-isolation.md, input-buffer.md, hit-effects-investigation.md, air-combo-damage-scaling.md. 제작과 문서검증 기록은 제출슬라이드에 포함하지 않는다.'

  foreach($design in $deck.Designs){try{$scheme=$design.SlideMaster.Theme.ThemeFontScheme;foreach($fi in 1..6){$scheme.MajorFont.Item($fi).Name=$Font;$scheme.MinorFont.Item($fi).Name=$Font}}catch{}}
  $pptx=Join-Path $output '김석진_LSDF_포트폴리오_Astra_v2.pptx'
  $pdf=Join-Path $output '김석진_LSDF_포트폴리오_Astra_v2.pdf'
  $deck.SaveAs($pptx,24,-1);$deck.SaveAs($pdf,32)
  foreach($sl in $deck.Slides){$sl.Export((Join-Path $render ('slide-{0:00}.png' -f $sl.SlideIndex)),'PNG',1600,900)}
  $metrics=@();foreach($sl in $deck.Slides){foreach($sh in $sl.Shapes){if($sh.HasTextFrame -eq -1 -and $sh.TextFrame.HasText -eq -1){$tr=$sh.TextFrame.TextRange;$metrics += [pscustomobject]@{slide=$sl.SlideIndex;name=$sh.Name;text=$tr.Text;left=[double]$sh.Left;top=[double]$sh.Top;width=[double]$sh.Width;height=[double]$sh.Height;boundWidth=[double]$tr.BoundWidth;boundHeight=[double]$tr.BoundHeight;boundTop=[double]$tr.BoundTop;boundLeft=[double]$tr.BoundLeft;font=$tr.Font.Name;size=[double]$tr.Font.Size}}}}
  $metrics|ConvertTo-Json -Depth 5|Set-Content -Encoding UTF8 (Join-Path $build 'text-metrics.json')
  [pscustomobject]@{slides=$deck.Slides.Count;standardHeaders=$HeaderSlides.ToArray();pptx=$pptx;pdf=$pdf;font=$Font;widthInches=13.333333;heightInches=7.5;method='PowerPoint COM; native text and diagrams; embedded fonts'}|ConvertTo-Json -Depth 4|Set-Content -Encoding UTF8 (Join-Path $build 'build-manifest.json')
  Write-Output 'Created 14 slides with PPTX, PDF and PNG exports.'
}catch{Write-Output "Stopped at slide $($deck.Slides.Count)";Write-Output $_.ScriptStackTrace;throw}
finally{if($deck){$deck.Close()};if($app -and $hadApp){$app.DisplayAlerts=$originalAlerts};if($app -and -not $hadApp -and $app.Presentations.Count -eq 0){$app.Quit()};if($deck){[void][Runtime.InteropServices.Marshal]::ReleaseComObject($deck)};if($app){[void][Runtime.InteropServices.Marshal]::ReleaseComObject($app)};[GC]::Collect()}

