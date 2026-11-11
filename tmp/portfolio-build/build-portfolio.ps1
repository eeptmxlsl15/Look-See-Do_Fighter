$ErrorActionPreference = 'Stop'
$root = 'C:\Users\Admin\Look-See-Do_Fighter'
$build = Join-Path $root 'tmp\portfolio-build'
$output = Join-Path $root 'output\portfolio'
$render = Join-Path $build 'powerpoint'
[void][IO.Directory]::CreateDirectory($output)
[void][IO.Directory]::CreateDirectory($render)
$script:C = @{ Blue='0070D1'; Ink='121314'; Body='595959'; Muted='6B6B6B'; White='FFFFFF'; Black='000000'; Soft='F5F7FA'; Line='E5E5E5'; Dark='181818'; Pale='D0D0D0'; DLine='444444' }
$script:Font = 'Malgun Gothic'
$script:TextBoxes = [Collections.Generic.List[object]]::new()
$script:HeaderSlides = [Collections.Generic.List[int]]::new()
$script:W = 13.333
$script:H = 7.5
function Col([string]$hex) { return [Convert]::ToInt32($hex.Substring(0,2),16) + 256*[Convert]::ToInt32($hex.Substring(2,2),16) + 65536*[Convert]::ToInt32($hex.Substring(4,2),16) }
function Rect($s,$x,$y,$w,$h,$fill,$line='') {
  $sh=$s.Shapes.AddShape(1,$x*72,$y*72,$w*72,$h*72)
  $sh.Fill.ForeColor.RGB=Col $fill
  $sh.Line.Visible=0
  if($line){$sh.Line.Visible=-1;$sh.Line.ForeColor.RGB=Col $line;$sh.Line.Weight=0.65}
}
function Rule($s,$x,$y,$w,$color,$weight=0.6) {
  $l=$s.Shapes.AddLine($x*72,$y*72,($x+$w)*72,$y*72)
  $l.Line.ForeColor.RGB=Col $color;$l.Line.Weight=[single]$weight
}
function Arrow($s,$x1,$y1,$x2,$y2,$color='0070D1') {
  $l=$s.Shapes.AddLine($x1*72,$y1*72,$x2*72,$y2*72)
  $l.Line.ForeColor.RGB=Col $color;$l.Line.Weight=1.3;$l.Line.EndArrowheadStyle=3
}
function Segment($s,$x1,$y1,$x2,$y2) {
  $l=$s.Shapes.AddLine($x1*72,$y1*72,$x2*72,$y2*72)
  $l.Line.ForeColor.RGB=Col $C.Blue;$l.Line.Weight=1.3
}
function T($s,[string]$txt,$x,$y,$w,$h,$size=17,$color='121314',$bold=$false,$align=1,[string]$name='') {
  $sh=$s.Shapes.AddTextbox(1,$x*72,$y*72,$w*72,$h*72)
  if($name){$sh.Name=$name}
  $tf=$sh.TextFrame
  $tf.MarginLeft=0;$tf.MarginRight=0;$tf.MarginTop=0;$tf.MarginBottom=0
  $tf.WordWrap=-1;$tf.AutoSize=0
  $tf.VerticalAnchor=1
  $tr=$tf.TextRange;$tr.Text=$txt
  $tr.Font.Name=$script:Font;$tr.Font.NameFarEast=$script:Font
  $tr.Font.Size=$size;$tr.Font.Bold=$(if($bold){-1}else{0});$tr.Font.Color.RGB=Col $color
  $tr.ParagraphFormat.Alignment=$align
  $tr.ParagraphFormat.SpaceBefore=0;$tr.ParagraphFormat.SpaceAfter=0
  $tr.ParagraphFormat.LineRuleWithin=-1;$tr.ParagraphFormat.SpaceWithin=1.16
  $sh.Width=[single]($w*72);$sh.Height=[single]($h*72)
  $script:TextBoxes.Add([pscustomobject]@{slide=$s.SlideIndex;id=$sh.Id;text=$txt;width=$w;height=$h;font=$size})
}
function Notes($s,[string]$n) {
  $tr=$s.NotesPage.Shapes.Placeholders.Item(2).TextFrame.TextRange
  $tr.Text=$n;$tr.Font.Name=$script:Font;$tr.Font.NameFarEast=$script:Font;$tr.Font.Size=12
}
function Footer($s,$dark=$false) {
  $footerColor=$(if($dark){$C.Pale}else{$C.Muted})
  T $s '김석진 / Look-See-Do Fighter' .75 7.08 8 .20 10 $footerColor
  T $s ('{0:00}' -f $s.SlideIndex) 12.05 7.08 .53 .20 10 $footerColor $false 3
}
function Standard($chapter,$title,$subtitle,$dark=$false) {
  $s=$deck.Slides.Add($deck.Slides.Count+1,12)
  $s.FollowMasterBackground=0;$s.Background.Fill.ForeColor.RGB=Col $(if($dark){$C.Black}else{$C.White})
  $ink=$(if($dark){$C.White}else{$C.Ink});$body=$(if($dark){$C.Pale}else{$C.Body});$line=$(if($dark){$C.DLine}else{$C.Line})
  T $s $chapter .75 .40 11.83 .25 11 $C.Blue $true 1 'standard-chapter'
  T $s $title .75 .82 11.83 .55 30 $ink $true 1 'standard-title'
  T $s $subtitle .75 1.48 11.83 .36 16 $body $false 1 'standard-subtitle'
  Rule $s .75 1.98 11.83 $line
  Footer $s $dark
  $script:HeaderSlides.Add($s.SlideIndex)
  return $s
}
function NativeTable($s,$x,$y,$width,[double[]]$cols,[double[]]$heights,[object[]]$rows,$font=16) {
  $sh=$s.Shapes.AddTable($rows.Count,$cols.Count,$x*72,$y*72,$width*72,($heights|Measure-Object -Sum).Sum*72)
  $tb=$sh.Table
  for($colIdx=1;$colIdx -le $cols.Count;$colIdx++){$tb.Columns.Item($colIdx).Width=$cols[$colIdx-1]*72}
  for($r=1;$r -le $rows.Count;$r++) {
    $tb.Rows.Item($r).Height=$heights[$r-1]*72
    for($colIdx=1;$colIdx -le $cols.Count;$colIdx++) {
      $cell=$tb.Cell($r,$colIdx);$cs=$cell.Shape
      $cs.Fill.ForeColor.RGB=Col $(if($r -eq 1){$C.Ink}elseif($r%2 -eq 0){$C.Soft}else{$C.White})
      foreach($edge in 1..4){$cell.Borders.Item($edge).ForeColor.RGB=Col $C.White;$cell.Borders.Item($edge).Weight=1}
      $tf=$cs.TextFrame;$tf.MarginLeft=12;$tf.MarginRight=10;$tf.MarginTop=7;$tf.MarginBottom=5;$tf.VerticalAnchor=3
      $tr=$tf.TextRange;$tr.Text=[string]$rows[$r-1][$colIdx-1]
      $tr.Font.Name=$script:Font;$tr.Font.NameFarEast=$script:Font;$tr.Font.Size=$font
      $tr.Font.Bold=$(if($r -eq 1){-1}else{0});$tr.Font.Color.RGB=Col $(if($r -eq 1){$C.White}else{$C.Ink})
      $tr.ParagraphFormat.SpaceAfter=0;$tr.ParagraphFormat.SpaceBefore=0;$tr.ParagraphFormat.SpaceWithin=1.1
    }
  }
}
function Node($s,$title,$body,$x,$y,$w,$h,$dark=$false,$accent=$false) {
  $fill=$(if($accent){$C.Blue}elseif($dark){$C.Dark}else{$C.Soft})
  $ink=$(if($accent -or $dark){$C.White}else{$C.Ink});$b=$(if($accent){$C.White}elseif($dark){$C.Pale}else{$C.Body})
  Rect $s $x $y $w $h $fill
  T $s $title ($x+.22) ($y+.18) ($w-.44) .40 20 $ink $true
  T $s $body ($x+.22) ($y+.75) ($w-.44) ($h-.9) 16 $b
}
function Pic($s,$path,$x,$y,$w,$h,$crop=$false,$leftAlign=$false) {
  $sh=$s.Shapes.AddPicture($path,0,-1,$x*72,$y*72,-1,-1)
  $ow=$sh.Width;$oh=$sh.Height;$targetW=$w*72;$targetH=$h*72
  if($crop) {
    $scale=[Math]::Max($targetW/$ow,$targetH/$oh)
    $cr=$sh.PictureFormat.Crop
    $cr.ShapeWidth=$targetW;$cr.ShapeHeight=$targetH
    $cr.PictureWidth=$ow*$scale;$cr.PictureHeight=$oh*$scale
    $cr.PictureOffsetX=$(if($leftAlign){($ow*$scale-$targetW)/2}else{0})
    $cr.PictureOffsetY=0
    $sh.Left=$x*72;$sh.Top=$y*72
  } else {
    $sh.LockAspectRatio=-1;$sh.Width=$targetW
    if($sh.Height -gt $targetH){$sh.Height=$targetH}
    $sh.Left=($x*72+($targetW-$sh.Width)/2);$sh.Top=($y*72+($targetH-$sh.Height)/2)
  }
}
$stage=Join-Path $root 'Assets\Sprites\Background\목재 로봇 폐기장 아레나.png'
$pIdle=Join-Path $root 'Assets\Sprites\Character\Player\Move\Idle\Look-See-Do_Bot_export_Idle_01.png'
$pUpper=Join-Path $root 'Assets\Sprites\Character\Player\Attack\Lp\LeftUpper\Look-See-Do_Bot_export_LeftUpper_20.png'
$eIdle=Join-Path $root 'Assets\Sprites\Character\Enemy\Move_Enemy\MoveBack_Enemy\Look-See-Do_Bot_export_MoveBack_Idle_01.png'
$eKazumi=Join-Path $root 'Assets\Sprites\Character\Enemy\Attack_Enemy\Rp_Enemy\KazumiHoming_Enemy\Look-See-Do_Bot_export_KazumiHoming_Enemy_01.png'
$app=$null;$deck=$null;$hadApp=$false
try {
  try {$app=[Runtime.InteropServices.Marshal]::GetActiveObject('PowerPoint.Application');$hadApp=$true} catch {$app=New-Object -ComObject PowerPoint.Application}
  $app.Visible=-1
  $originalAlerts=$app.DisplayAlerts
  $app.DisplayAlerts=1
  $deck=$app.Presentations.Add(0)
  $deck.PageSetup.SlideWidth=$W*72;$deck.PageSetup.SlideHeight=$H*72
  # 01. Cover. Original game artwork is cropped natively to exclude its flag emblem.
  $s=$deck.Slides.Add(1,12);$s.FollowMasterBackground=0;$s.Background.Fill.ForeColor.RGB=Col $C.Black
  T $s 'AI NATIVE / CLIENT DEVELOPER PORTFOLIO' .75 .47 11.83 .3 12 $C.Blue $true
  T $s "멈췄던 격투게임을\nAI와 다시 만드는 6일".Replace('\n',"`r") .75 1.10 11.7 1.5 40 $C.White $true
  T $s 'Look-See-Do Fighter' .78 2.98 10 .55 26 $C.White $true
  T $s '김석진' .78 3.63 4 .4 20 $C.White
  Pic $s $stage 6.78 3.42 5.8 3.47 $true $true
  Pic $s $pUpper 9.20 4.98 1.25 1.65
  Pic $s $eKazumi 10.78 5.06 1.30 1.6
  T $s "1년 전 중단한 개인 프로젝트\n6일간의 AI 협업 재개발 기록".Replace('\n',"`r") .78 5.26 5.7 1.0 18 $C.Pale
  T $s '오른쪽 이미지는 프로젝트 원본 아트를 조합한 장면입니다.' 6.78 6.99 5.8 .22 10 $C.Pale
  Notes $s "지원자: 김석진. GitHub 및 미제공 연락처를 기재하지 않는다. 1년 전 중단 및 6일간의 재개발은 사용자 제공 프로젝트 맥락이다. 일별 순서는 설명을 위한 재구성이다. 원본 아트 배경과 스프라이트를 PowerPoint에 배치했으며 실행 화면 캡처가 아니다. SDK/애드온/기존 아트 제작을 이번 AI 협업 성과로 산정하지 않는다. 기준 커밋 b571a8f15e617788a0c86fe6507e992146f1800c."
  # 02. Scope and reader orientation.
  $s=Standard '01  PROJECT' '프로젝트 개요와 재개발 범위' '기존 전투 콘텐츠를 바탕으로 온라인 대전의 구조와 플레이 흐름을 보완했습니다.'
  T $s 'Look-See-Do Fighter' .75 2.22 6.6 .5 23 $C.Ink $true
  T $s "Photon Quantum 3 기반 2D 격투게임\n방향과 공격 버튼 조합에 스킬을 배치하는 대전 구조".Replace('\n',"`r") .75 2.91 6.5 .92 17 $C.Body
  Rule $s .75 4.02 6.2 $C.Line
  T $s '재개발 범위' .75 4.27 6.2 .36 20 $C.Ink $true
  T $s "입력·로드아웃 전달, 전투 상태 소유권, 공격 공통화\n공중 콤보, 라운드 진행, 피격 연출, 종료 후 메뉴 복귀".Replace('\n',"`r") .75 4.89 6.2 .97 17 $C.Body
  T $s 'Unity 6000.3.26f1 / Quantum 3.0.14.2241 / C# / QTN / NUnit' .75 6.40 11.83 .3 13 $C.Muted
  Rect $s 7.42 2.18 5.16 3.98 $C.Soft
  Pic $s $pIdle 7.96 2.60 1.77 2.45
  Pic $s $eIdle 10.17 2.60 1.78 2.45
  T $s '스킬 로드아웃' 7.72 5.35 2.2 .3 15 $C.Body
  T $s '28 슬롯' 10.11 5.20 2.16 .5 27 $C.Blue $true 3
  Notes $s '근거: docs/ai/evidence/2026-10-17-portfolio-summary.md, docs/ai/loadout-isolation.md. 환경은 기록 시점 기준. 원본 스프라이트이며 게임 실행 화면이 아니다. 기존 게임·SDK·애드온·아트와 이번 개발 기여를 구분한다.'
  # 03. Before/after table.
  $s=Standard '01  PROJECT' '재개 시점의 문제와 변경' '구조적 위험과 미완성 기능을 나누어 수정 범위를 정했습니다.'
  $rows=@(
    @('영역','기존 상태','재개발 결과'),
    @('로드아웃','Simulation에서 로컬 저장값 읽기','RuntimePlayer 전달과 수신 검증'),
    @('가변 상태','공유 에셋의 플레이어별 예약 값','Frame 컴포넌트로 상태 이관'),
    @('공격 구현','공격마다 반복된 타격 생성 코드','37개 공격을 불변 정의와 생성기로 분리'),
    @('경기 진행','첫 KO 이후 진행 흐름 부족','5판 3선승과 매치 종료 흐름 구현'),
    @('표현 계층','전역 참조와 이벤트 수명 혼재','경기별 참조와 표현 이벤트 소유권 정리')
  )
  NativeTable $s .75 2.20 11.83 @(1.55,4.90,5.38) @(.53,.74,.74,.74,.74,.74) $rows 16
  T $s '기존 GUID·Animator 계약·시스템 순서를 보존하며, 구조 변경과 밸런스 변경을 분리했습니다.' .75 6.60 11.83 .25 13 $C.Muted
  Notes $s '근거: docs/ai/architecture-audit.md(초기 기록), refactor-p0-result.md, refactor-remaining-result.md, refactor-phase4-result.md. 초기 감사의 위험 항목을 실제 재현된 네트워크 장애로 표현하지 않는다.'
  # 04. Reconstructed 6-day narrative.
  $s=Standard '02  AI COLLABORATION' '6일간의 작업 흐름' '기록된 작업을 여섯 단계로 재구성했습니다. 실제 날짜별 작업량을 뜻하지 않습니다.'
  $phases=@(
    @('01','기존 코드 감사','로컬 저장 의존과 공유 상태 확인','Quantum 3 경계·보존 계약 정리'),
    @('02','전투 구조 정리','Frame 상태·37개 공격 정리','FP·정수 연산과 타격 시점 보존'),
    @('03','입력과 대전 흐름','선입력과 로드아웃 수신 검증','두 Photon 세션의 체크섬 비교'),
    @('04','공중 콤보 개선','재피격 낙하와 5후속타 종료','타수별 피해 70/50/30% 적용'),
    @('05','클라이언트 연출','체력바·타격 효과·카메라 조정','View 수명과 씬 참조 연결 보완'),
    @('06','통합과 플레이 확인','메뉴 복귀·에셋 재베이크','직접 플레이로 체감·수치 확인')
  )
  for($i=0;$i -lt 6;$i++) {
    $x=.75+($i%3)*4.04;$y=2.24+[Math]::Floor($i/3)*2.32;$v=$phases[$i]
    Rule $s $x $y 3.70 $C.Line
    T $s $v[0] $x ($y+.15) .64 .55 28 $C.Blue $true
    T $s $v[1] ($x+.80) ($y+.17) 2.90 .40 20 $C.Ink $true
    T $s ($v[2]+"`r"+$v[3]) $x ($y+.92) 3.70 1.02 16 $C.Body
  }
  Notes $s '사용자가 정확한 날짜별 작업 배치는 중요하지 않으므로 러프하게 재구성하도록 허용했다. 실제 기록은 2026-10-16~22에 걸쳐 있으며 작업은 병행·재방문했다. 각 단계는 docs/ai/evidence/2026-10-17-portfolio-summary.md 및 current-state.md에 연결된 작업 기록으로 추적한다.'
  # 05. Human and AI accountability.
  $s=Standard '02  AI COLLABORATION' '사람의 결정과 AI의 수행' '요구사항과 완료 판단은 사람이 맡고, AI가 조사·구현·검증 자료 작성을 수행했습니다.'
  NativeTable $s .75 2.20 11.83 @(2.05,4.89,4.89) @(.56,.93,.93,.93) @(
    @('작업','김석진의 요구·결정','AI 협업 산출물'),
    @('구조 개선','Quantum 3 경계와 기존 참조 보존','소스 감사, 설계안, 리팩토링 패치'),
    @('게임 감각','선입력 20tick, 피해 70/50/30% 지정','Frame 상태 연결과 테스트 사례 작성'),
    @('완료 판단','직접 플레이 확인과 로컬 커밋 요청','자동 검사 결과·미검증 항목 기록')
  ) 16
  Rule $s .75 5.82 11.83 $C.Line
  T $s '검토 기준을 작업 규칙으로 남김' .75 6.06 5.6 .38 20 $C.Ink $true
  T $s "기존 변경 보존 / 상태·표현 경계 유지\n실패와 미실행을 구분해 다음 작업에 인계".Replace('\n',"`r") 6.80 6.02 5.78 .76 16 $C.Body
  Notes $s '근거: AGENTS.md, docs/ai/evidence/2026-10-17-portfolio-summary.md, input-buffer.md, air-combo-damage-scaling.md, user-validation-air-combo-2026-10-22.md. 개별 설계의 최초 제안자나 줄 단위 AI 작성 비율은 기록이 없어 산정하지 않는다. 사용자 세부 코드 리뷰를 확인된 사실처럼 주장하지 않는다.'
  # 06. Architecture.
  $s=Standard '03  DETERMINISTIC GAMEPLAY' '시뮬레이션과 표현의 경계' '판정은 Frame과 Quantum 입력에서 결정하고, Unity View는 결과를 읽어 표현합니다.'
  Node $s 'Unity 메뉴' "스킬 선택·로컬 저장\n28슬롯 ID·비용 검증".Replace('\n',"`r") .75 2.28 3.48 1.58
  Node $s 'RuntimePlayer' "AddPlayer로 전달\n수신 시 다시 검증".Replace('\n',"`r") 4.92 2.28 3.48 1.58
  Node $s 'Frame' "플레이어별 스킬맵\n입력 버퍼·전투 상태".Replace('\n',"`r") 9.09 2.28 3.49 1.58 $false $true
  Arrow $s 4.30 3.06 4.80 3.06;Arrow $s 8.47 3.06 8.97 3.06
  Node $s 'Quantum Simulation' "FP·정수 틱으로 이동·판정·피해 계산\n불변 공격 정의와 Frame 상태 사용".Replace('\n',"`r") .75 4.62 5.42 1.67
  Node $s 'Unity View' "Frame·이벤트를 읽어 UI·SFX·VFX 재생\n표현 이벤트의 취소·종료 수명 관리".Replace('\n',"`r") 7.16 4.62 5.42 1.67
  Segment $s 10.83 3.86 10.83 4.24
  Segment $s 10.83 4.24 3.46 4.24
  Arrow $s 3.46 4.24 3.46 4.52
  Arrow $s 6.29 5.41 7.03 5.41
  T $s '입력: CallbackPollInput' .75 6.53 5.4 .29 14 $C.Body
  T $s 'View의 Transform·Animator가 판정 권한을 갖지 않음' 7.16 6.53 5.42 .29 14 $C.Body
  Notes $s '근거: Assets/QuantumUser/Simulation/LSDF_PlayerSpawnSystem.cs, LSDFInputBuffer.cs, LSDFPlayerHealth.cs, Assets/QuantumUser/View/LSDFPresentationLedger.cs. RuntimePlayer/AddPlayer는 Quantum 3 경로다. Frame 상자의 세부 상태는 단일 컴포넌트라는 의미가 아니다.'
  # 07. Loadout contract.
  $s=Standard '03  DETERMINISTIC GAMEPLAY' '로드아웃을 플레이어 데이터로 전달' '서로 다른 로컬 저장값이 시뮬레이션을 우회하지 않도록 입력 계약을 고정했습니다.'
  T $s '방향 7개 × 공격 버튼 4개' .75 2.24 5.2 .40 21 $C.Ink $true
  $grid=@();$grid+=,@('방향 그룹','LP','RP','LK','RK')
  for($r=1;$r -le 7;$r++){$grid+=,@("그룹 $r",[string](($r-1)*4+1),[string](($r-1)*4+2),[string](($r-1)*4+3),[string](($r-1)*4+4))}
  NativeTable $s .75 2.98 5.48 @(1.64,.96,.96,.96,.96) @(.45,.44,.44,.44,.44,.44,.44,.44) $grid 14
  T $s '슬롯 번호는 28개 계약을 설명하기 위한 1부터의 표기입니다.' .75 6.59 5.52 .27 10 $C.Muted
  T $s '선택과 실행의 데이터 경로' 7.00 2.24 5.58 .40 21 $C.Ink $true
  T $s "메뉴에서 ID·비용 검증\nRuntimePlayer로 선택값 전달\n수신한 플레이어의 Frame 스킬맵에 복사".Replace('\n',"`r") 7.00 3.00 5.58 1.38 17 $C.Body
  Rule $s 7.00 4.70 5.58 $C.Line
  T $s '서로 다른 로드아웃으로 2세션 검사' 7.00 4.96 5.58 .41 20 $C.Blue $true
  T $s "5개 명령에서 10개 공격 경로 관측\n동일 verified tick의 체크섬 67회 일치".Replace('\n',"`r") 7.00 5.63 5.58 .85 17 $C.Body
  Notes $s '근거: docs/ai/loadout-isolation.md, evidence/health-shake-publish-validation.json. 온라인 검사는 같은 Unity Editor의 독립 Quantum 세션 두 개와 실제 Photon 연결 두 개를 사용했다. 과거 검사 시점 결과이며 사용자 제보 disconnect의 정확한 원인을 재현·해결했다는 뜻이 아니다. 계약상 배열 인덱스는 소스 그대로 유지되며 표는 1-based 개념도다.'
  # 08. Ownership comparison.
  $s=Standard '03  DETERMINISTIC GAMEPLAY' '롤백 대상 상태를 Frame에 배치' '프레임을 넘어 남는 플레이어별 값과 읽기 전용 설정을 분리했습니다.'
  T $s '공유 에셋에 있던 실행 상태' .75 2.23 5.45 .42 21 $C.Ink $true
  Rect $s .75 3.00 5.45 2.47 $C.Soft
  T $s '공격 에셋' 1.02 3.28 4.91 .38 20 $C.Ink $true
  T $s "BufferedNextAttack 같은 예약 값\n플레이어별 호출이 같은 에셋을 참조\nFrame 복원만으로 값 복원이 어려운 경계".Replace('\n',"`r") 1.02 4.00 4.91 1.15 16 $C.Body
  T $s '변경한 소유권' 7.12 2.23 5.46 .42 21 $C.Ink $true
  Node $s '불변 AssetObject' '공격 수치·타이밍·형상 정의' 7.12 3.00 5.46 1.28
  Node $s 'Frame 컴포넌트' '버퍼·예약·타이머·공중 콤보 진행' 7.12 4.54 5.46 1.28 $false $true
  Rule $s .75 6.12 11.83 $C.Line
  T $s '임시 계산은 지역변수 / 지속 상태는 QTN / 생성 코드는 설치된 CodeGen 사용' .75 6.42 11.83 .34 17 $C.Ink
  Notes $s '근거: docs/ai/refactor-p0-result.md, refactor-phase4-result.md, evidence/2026-10-17-portfolio-summary.md. 공유 버퍼와 static 가드 상태를 Frame으로 이관했다. 이 구조 설명 자체를 네트워크 rollback 완료 증거로 취급하지 않는다. 입력 기록 재생과 Frame 복원 검사는 별도 검증 슬라이드에서 범위를 구분한다.'
  # 09. Attack factory.
  $s=Standard '03  DETERMINISTIC GAMEPLAY' '37개 공격의 타격 생성 공통화' '공격마다 반복하던 생성 절차를 분리하며 기존 타격 시점을 보존했습니다.'
  Node $s '공격 정의' "피해·타격 형상·오프셋\n단타·시간차 타격 정의".Replace('\n',"`r") .75 2.24 3.49 1.88
  Node $s '공통 생성기' "LSDFHitboxFactory\nHitbox 컴포넌트 생성".Replace('\n',"`r") 4.92 2.24 3.49 1.88 $false $true
  Node $s '공격 이벤트' "이동·연계 의도 유지\n기존 프레임에서 호출".Replace('\n',"`r") 9.09 2.24 3.49 1.88
  Arrow $s 4.31 3.18 4.80 3.18;Arrow $s 8.97 3.18 8.48 3.18
  T $s '보존한 판정 계약' .75 4.65 11.83 .42 21 $C.Ink $true
  Rule $s .75 5.28 11.83 $C.Line
  T $s '생성 시점' .75 5.54 3.45 .3 15 $C.Body
  T $s 'StartupFrames - 1' .75 6.04 3.45 .44 20 $C.Ink $true
  T $s '수명' 4.92 5.54 3.45 .3 15 $C.Body
  T $s '생성 tick + 1에 파괴' 4.92 6.04 3.45 .44 20 $C.Ink $true
  T $s '직렬화와 실행 순서' 9.09 5.54 3.49 .3 15 $C.Body
  T $s 'GUID·asset ID·순서 유지' 9.09 6.04 3.49 .44 19 $C.Ink $true
  Notes $s '근거: docs/ai/refactor-remaining-result.md, evidence/2026-10-17-portfolio-summary.md, Assets/QuantumUser/Simulation/LSDFSingleHitDefinition.cs, LSDFHitboxFactory.cs. 이관은 왼어퍼 1, 잽 4, 다단 2, 나머지 30 공격. 공격 이벤트에서 공통 정의·생성기를 호출하며 데이터 정의와 실행 상태를 분리한다. SDK와 Animator 애드온은 프로젝트 구현 성과에 포함하지 않는다.'
  # 10. Input buffer timeline.
  $s=Standard '04  PLAYABLE COMBAT' '행동 잠금 중 입력을 다음 행동에 연결' '키를 놓은 뒤에도 유효한 선입력을 보관하고, 실행 가능해지면 한 번 소비합니다.'
  T $s '실제 온라인 검사에서 관측한 틱' .75 2.24 11.83 .4 21 $C.Ink $true
  Rule $s 1.15 3.87 10.98 $C.Line 2
  $xs=@(1.15,5.54,9.69);$titles=@('384 tick','입력 보관','392 tick');$bodies=@("공격 버튼 누름\n행동 잠금 상태", "방향·버튼을 Frame에 저장\n키 해제 후에도 유지", "잠금 해제 뒤 전달\nLeftUpper 실행")
  for($i=0;$i -lt 3;$i++) {
    Rect $s $xs[$i] 3.78 .18 .18 $C.Blue
    T $s $titles[$i] ($xs[$i]-.18) 3.02 2.9 .48 25 $C.Blue $true
    T $s $bodies[$i].Replace('\n',"`r") ($xs[$i]-.18) 4.23 3.05 1.05 17 $C.Body
  }
  Rule $s .75 5.73 11.83 $C.Line
  T $s '현재 에셋의 선입력 창: 20tick' .75 6.01 6.35 .40 21 $C.Ink $true
  T $s '누르고 있기만 해서는 만료 시간이 갱신되지 않음' 7.55 6.07 5.03 .52 15 $C.Body
  Notes $s '근거: docs/ai/input-buffer.md, evidence/fighter-publish-validation.json, Assets/QuantumUser/Simulation/LSDFInputBuffer.cs. job 43a31becf2a1483c8def35772cc9be78의 157개 PASS 당시 press=384/dispatch=392를 관측했다. 초기 설정 기본값 10tick, 실제 사용자 에셋 20tick, 라우팅 안전 한계 16프레임은 서로 다른 값이다. 시간축은 관측 순서를 표현한 개념도이며 축척 그래프가 아니다.'
  # 11. Match flow.
  $s=Standard '04  PLAYABLE COMBAT' '한 라운드에서 한 경기를 완주하는 흐름' '5판 3선승과 라운드 초기화, 매치 종료 후 메뉴 복귀를 연결했습니다.'
  Node $s '라운드 전투' "체력·입력·공격 상태 진행\nKO 시 승패와 포즈 확정".Replace('\n',"`r") .75 2.30 3.49 1.78
  Node $s '승수 판정' "3승 미만: 다음 라운드\n3승 달성: 매치 종료".Replace('\n',"`r") 4.92 2.30 3.49 1.78 $false $true
  Node $s '결과 표시' "기존 결과 표시 시간 대기\n기본 120tick".Replace('\n',"`r") 9.09 2.30 3.49 1.78
  Arrow $s 4.31 3.19 4.8 3.19;Arrow $s 8.48 3.19 8.97 3.19
  Node $s '다음 라운드 초기화' "전투 상태·위치·입력 예약 정리\n인트로 후 다시 시작".Replace('\n',"`r") .75 4.82 5.43 1.53
  Node $s '세션 종료 후 메인 메뉴' "Runner·Photon 종료와 경기 씬 정리\n중복 종료 방지·재진입 값 초기화".Replace('\n',"`r") 7.16 4.82 5.42 1.53
  Arrow $s 6.67 4.13 3.45 4.70;Arrow $s 10.83 4.13 10.83 4.70
  T $s '과거 온라인 검사: 5라운드·3:2 종료·종료 후 240 verified tick 안정 구간 관측' .75 6.53 11.83 .30 15 $C.Body
  Notes $s '근거: docs/ai/best-of-five.md 및 docs/ai/evidence/fighter-publish-validation.json, docs/ai/match-return-to-main.md, docs/ai/evidence/match-return-validation.json. 라운드 종료 3:2와 240tick은 과거 157 PASS 시점 관측. 메뉴 복귀 신규 fixture 5개는 당시 Unity 미임포트로 NOT RUN이었고 실제 Photon 복귀/씬 unload 자동 검사는 NOT RUN이다. 사용자 이후 직접 테스트 완료 보고와 자동 결과를 구분한다. 결과 대기는 verified MatchEnd 및 TickToEnableMove를 사용한다.'
  # 12. Juggle design.
  $s=Standard '04  PLAYABLE COMBAT' '공중 콤보의 궤적과 종료 조건' '재피격 순간의 위치를 보존하면서 낙하와 후속타 제한을 Frame 상태로 관리합니다.'
  Node $s '시동기' "지상에서 띄우기\n후속타 카운트 시작 전".Replace('\n',"`r") .75 2.27 3.49 1.72
  Node $s '공중 재피격' "짧은 상승·복귀·가속 낙하\n위치의 순간 이동 방지".Replace('\n',"`r") 4.92 2.27 3.49 1.72 $false $true
  Node $s '종료와 회복' "후속 5타 또는 조기 마무리\n날리기·착지 후 회복".Replace('\n',"`r") 9.09 2.27 3.49 1.72
  Arrow $s 4.31 3.13 4.80 3.13;Arrow $s 8.48 3.13 8.97 3.13
  T $s '판정·위치·진행 상태' .75 4.58 5.45 .42 21 $C.Ink $true
  T $s "FP 연산과 Frame의 콤보 상태 사용\n시동기를 제외한 실제 후속타 수를 기준으로 종료\n연출용 Transform이 판정 위치를 결정하지 않음".Replace('\n',"`r") .75 5.23 5.45 1.36 17 $C.Body
  T $s '과거 재현 검사에서 확인한 범위' 7.12 4.58 5.46 .42 21 $C.Ink $true
  T $s "자연 낙하·재피격 낙하 체크섬 136회 일치\n입력 기록 재생 2회와 과거 Frame 복원 비교\n같은 Editor의 두 Photon 세션 조건".Replace('\n',"`r") 7.12 5.23 5.46 1.36 17 $C.Body
  Notes $s '근거: docs/ai/natural-rehit-fall.md, air-combo-arc.md, evidence/natural-rehit-fall-validation.json. 당시 targeted 106 PASS. photon_air_fall 136 comparisons/replay_runs 2, Frame restore natural launcher fall 및 accelerating rehit fall. 해당 과거 조건의 결과이며 현재 전체 빌드나 강제 네트워크 rollback 검증을 뜻하지 않는다. 후속타 5회 제한, 조기 마무리, 회복은 별도 ender fixture에도 기록됐다.'
  # 13. Actual damage change.
  $s=Standard '04  PLAYABLE COMBAT' '공중 피해를 실제 후속타 수에 연결' '애니메이션 진입 횟수와 피해 계산을 분리해 70/50/30%를 적용했습니다.'
  T $s '공중 후속타별 기본 피해 배율' .75 2.22 6.05 .40 21 $C.Ink $true
  $vals=@(70,50,30,30,30);$labels=@('1타','2타','3타','4타','5타')
  for($i=0;$i -lt 5;$i++) {
    $y=3.01+$i*.64
    T $s $labels[$i] .75 $y .60 .32 16 $C.Body
    Rect $s 1.53 ($y+.02) 4.30 .25 $C.Soft
    Rect $s 1.53 ($y+.02) (4.30*$vals[$i]/100) .25 $C.Blue
    T $s ($vals[$i].ToString()+'%') 6.00 ($y-.03) .82 .38 18 $C.Blue $true 3
  }
  T $s '기본 피해 20일 때: 14 / 10 / 6 / 6 / 6' .75 6.39 6.10 .33 17 $C.Ink $true
  T $s '피해 계산의 상태 기준' 7.43 2.22 5.15 .40 21 $C.Ink $true
  T $s "접촉 등록 후 Frame의\nLSDF_AirCombo.HitCount 읽기\n정수 백분율 곱셈 후 소수점 버림".Replace('\n',"`r") 7.43 3.03 5.15 1.4 17 $C.Body
  Rule $s 7.43 4.75 5.15 $C.Line
  T $s '유지한 규칙' 7.43 5.03 5.15 .36 20 $C.Ink $true
  T $s "지상 일반 시동기 100%\n지상 카운터 120%\n공중 5번째 마무리 타격도 30% 적용".Replace('\n',"`r") 7.43 5.65 5.15 1.05 16 $C.Body
  Notes $s '근거: docs/ai/air-combo-damage-scaling.md, homing-air-damage-investigation-2026-10-22.md, Assets/QuantumUser/Simulation/LSDFPlayerHealth.cs, LSDF_PlayerSystem.Combat.cs. 0은 지상 시동기, 양수는 실제 공중 후속타. ApplyHit는 Juggle.OnHit의 접촉 등록 후 Frame 상태를 읽는다. (long)baseDamage * percent / 100. 매우 작은 기본 피해는 정수 버림 결과 0일 수 있으며 최소 1 피해를 강제하지 않는다. 최신 전체 통합 fixture 보정 후 자동 재실행은 하지 않았다. 사용자 직접 테스트 완료 보고와 별도.'
  # 14. Client presentation.
  $s=Standard '05  CLIENT PRESENTATION' '타격을 읽을 수 있게 만드는 표현' '피격 피드백은 View에서 조정하고, 전투 판정과 표현 수명을 분리했습니다.' $true
  Pic $s $stage .75 2.22 5.42 4.16 $true $true
  Pic $s (Join-Path $root 'Assets\Sprites\Character\Player\Attack\Lp\LeftUpper\Look-See-Do_Bot_export_LeftUpper_30.png') 3.08 4.66 1.36 1.72
  T $s '원본 배경·스프라이트로 구성한 장면 예시' .75 6.55 5.42 .25 11 $C.Pale
  T $s '체력바 피격 피드백' 7.16 2.25 5.42 .40 21 $C.White $true
  T $s '체력 변화와 흔들림 표현' 7.16 2.88 5.42 .40 17 $C.Pale
  Rule $s 7.16 3.53 5.42 $C.DLine
  T $s '타격 효과와 캐릭터 표시' 7.16 3.81 5.42 .40 21 $C.White $true
  T $s 'VFX 연결, 투명도, 공격·피격 색상 표시' 7.16 4.44 5.42 .56 17 $C.Pale
  Rule $s 7.16 5.07 5.42 $C.DLine
  T $s '카메라와 전투 화면' 7.16 5.35 5.42 .40 21 $C.White $true
  T $s '카운터 줌·타격 흔들림·배경 구도 조정' 7.16 5.98 5.42 .56 17 $C.Pale
  Notes $s '근거: docs/ai/camera-hit-shake.md, hit-effects-investigation.md, hit-effects-prefab-migration.md, effect-opacity-action-flash.md, stage-background-flat-zoom.md, current-state.md. 이 슬라이드의 이미지는 원본 아트 배치로, 열거한 실시간 효과를 캡처한 사진이 아니다. 사용자 플레이 확인이 있으며 관련 최신 View 자동 fixture에는 FAIL/수정 후 미검증 이력이 남아 있다. 성능 수치나 전체 연출 자동 검증 완료를 주장하지 않는다.'
  # 15. Regression case study.
  $s=Standard '05  CLIENT PRESENTATION' '타격 효과 회귀의 원인 추적' '구독 시점 변경 뒤 사라진 효과를 소스와 씬의 생명주기 경계에서 조사했습니다.'
  NativeTable $s .75 2.22 11.83 @(2.1,9.73) @(.52,.78,.78,.78,.78) @(
    @('조사 단계','확인한 내용'),
    @('현상','전투 코드에 효과 이벤트가 있어도 화면에 효과가 나타나지 않음'),
    @('소스 추적','구독을 Start에서 OnActivate로 옮긴 변경 확인'),
    @('씬 추적','효과 View가 경기 updater 및 바인딩 경로에 연결되지 않은 상태 확인'),
    @('보완','경기 씬의 효과 View와 updater 참조를 명시적으로 연결')
  ) 16
  Rule $s .75 6.11 11.83 $C.Line
  T $s '코드의 이벤트 발행, View 활성화, 씬 참조를 함께 확인해야 했던 사례' .75 6.39 11.83 .38 20 $C.Blue $true
  Notes $s '근거: docs/ai/hit-effects-investigation.md, hit-effects-prefab-migration.md 및 current-state.md. 3f45e7d1의 Start→OnActivate 이관과 EffectManager가 메뉴에만 존재한 경계를 추적했다. EditMode의 IsActive=false/updater=null 관측은 실행 중 실제 타격 재현과 구분했다. 이후 prefab 및 gameplay updater 연결을 구현했다. 관련 최신 자동 fixture는 Assert 별칭 보정 후 미검증이며 사용자 테스트 완료 보고와 구분한다. AI가 수행한 리팩토링 변경도 회귀 원인이 될 수 있어 소스 외 직렬화 참조를 확인한 사례다.'
  # 16. Version-bounded evidence.
  $s=Standard '06  VALIDATION' '검증 결과와 적용 범위' '통과한 버전과 시나리오를 기록하고, 실패·미실행 결과도 함께 남겼습니다.'
  NativeTable $s .75 2.22 11.83 @(2.10,2.03,4.18,3.52) @(.56,1.04,1.04,1.04) @(
    @('검사 시점','자동 결과','확인한 범위','해석 범위'),
    @('로드아웃·체력바','235 / 235 PASS','서로 다른 로드아웃과 실제 Photon 2세션','해당 과거 후보의 결과'),
    @('재피격 낙하','106 / 106 PASS','낙하·후속타·재생·과거 Frame 복원','같은 Editor 세션 조건'),
    @('최신 피해 변경','109 PASS / 1 FAIL','110개 중 Frame·그래프 통합 fixture 실패','fixture 보정 후 미검증')
  ) 15
  T $s '직접 플레이 확인' .75 6.20 4.20 .40 21 $C.Blue $true
  T $s "사용자의 직접 테스트 완료 보고를 기록했습니다.\n자동 검사 결과와 구분합니다.".Replace('\n',"`r") 5.13 6.21 7.45 .64 16 $C.Body
  Notes $s '원본: docs/ai/evidence/health-shake-publish-validation.json (job 537a3260b7b340fb82c01bab7c1e94e7), natural-rehit-fall-validation.json (job 0d39ff47dbdc444d970e853f374ba71b), air-combo-damage-scaling-validation.json (job be083f39f32b4b939f46877fd69b37e6), docs/ai/user-validation-air-combo-2026-10-22.md. 과거 235 PASS를 이후 모든 변경의 PASS로 재사용하지 않는다. 최신 1개 fixture는 구 entry count 1 예상/2 관측에서 중단됐고 보정 후 자동 재실행하지 않았다. 직접 플레이 확인의 상세 로그·반복 횟수는 제공되지 않았다.'
  # 17. Verification ladder.
  $s=Standard '06  VALIDATION' '결정론 검증의 근거와 한계' '상태 소유권 설계와 실제 실행 증거를 구분해 설명할 수 있도록 정리했습니다.'
  $levels=@(
    @('정적 정책','금지 API·공유 상태 패턴의 소스 검사','결정론·실제 게임 동작을 단독으로 보증하지 않음'),
    @('Photon 2세션','과거 낙하 검사에서 체크섬 136회 일치','같은 Editor의 독립 Quantum 세션 조건'),
    @('재생·Frame 복원','기록 입력 재생과 과거 Frame에서 재시뮬레이션','강제 네트워크 지연에 의한 rollback과 구분'),
    @('후속 검증','독립 실행 빌드·교차 플랫폼·강제 네트워크 rollback','현재 제출 자료에서는 NOT RUN')
  )
  for($i=0;$i -lt 4;$i++) {
    $y=2.25+$i*1.12
    Rule $s .75 $y 11.83 $C.Line
    T $s $levels[$i][0] .75 ($y+.23) 2.38 .42 20 $C.Blue $true
    T $s $levels[$i][1] 3.50 ($y+.18) 9.08 .38 18 $C.Ink
    T $s $levels[$i][2] 3.50 ($y+.65) 9.08 .30 14 $C.Body
  }
  Notes $s '근거: docs/ai/validation.md, evidence/health-shake-publish-validation.json, evidence/natural-rehit-fall-validation.json 및 최신 air-combo-damage-scaling-validation.json. 이 문서 제작을 위한 게임 검사 재실행은 하지 않는다. Frame restore/replay는 네트워크 지터 유도 rollback 증거와 구분한다. 성능·교차 플랫폼·독립 빌드 결과는 측정하지 않았다.'
  # 18. Role-fit mapping.
  $s=Standard '07  ROLE FIT' '지원 직무와 연결되는 개발 경험' 'AI 작업을 검토 가능한 단위로 만들고, 클라이언트의 상태·입력·표현을 끝까지 연결했습니다.'
  T $s 'AI Native 개발' .75 2.24 5.45 .48 24 $C.Blue $true
  T $s '작업 제약을 실행 규칙으로 정리' .75 3.10 5.45 .38 20 $C.Ink $true
  T $s 'Quantum 경계, 기존 변경 보존, 검증·인계 기준' .75 3.66 5.45 .58 16 $C.Body
  Rule $s .75 4.49 5.45 $C.Line
  T $s 'AI 산출물을 근거와 연결' .75 4.79 5.45 .38 20 $C.Ink $true
  T $s "소스·씬 참조를 함께 추적한 회귀 조사\n자동 FAIL과 플레이 확인을 분리한 작업 기록".Replace('\n',"`r") .75 5.36 5.45 1.10 17 $C.Body
  T $s '클라이언트 개발' 7.12 2.24 5.46 .48 24 $C.Blue $true
  T $s '온라인 전투 상태와 입력 처리' 7.12 3.10 5.46 .38 20 $C.Ink $true
  T $s '28슬롯 전달, Frame 버퍼, 실제 후속타 피해' 7.12 3.66 5.46 .58 16 $C.Body
  Rule $s 7.12 4.49 5.46 $C.Line
  T $s '경기와 씬의 생명주기 연결' 7.12 4.79 5.46 .38 20 $C.Ink $true
  T $s "라운드 초기화와 매치 종료 후 메뉴 복귀\nView 이벤트의 취소·확정·종료 처리".Replace('\n',"`r") 7.12 5.36 5.46 1.10 17 $C.Body
  Notes $s '직무 연결은 기록된 개발 사례에 대한 지원자 포트폴리오 해석이다. 사람·AI의 기여 구분은 5번 슬라이드의 기록 범위를 따른다. 근거: AGENTS.md, docs/ai/evidence/2026-10-17-portfolio-summary.md, hit-effects-investigation.md, match-return-to-main.md, air-combo-damage-scaling.md.'
  # 19. Evidence and remaining work appendix.
  $s=Standard 'APPENDIX  EVIDENCE' '근거 문서와 남은 확인 항목' '상세 작업 기록을 보존하고, 현재 소스의 미완료 항목을 다음 작업으로 분리했습니다.'
  T $s '대표 근거' .75 2.24 5.45 .42 21 $C.Ink $true
  $refs=@(
    @('구조·기여','2026-10-17-portfolio-summary.md'),
    @('로드아웃','loadout-isolation.md'),
    @('입력','input-buffer.md'),
    @('연출 회귀','hit-effects-investigation.md'),
    @('최신 피해','air-combo-damage-scaling.md')
  )
  for($i=0;$i -lt 5;$i++) {
    $y=3.03+$i*.65
    T $s $refs[$i][0] .75 $y 1.35 .31 15 $C.Body
    T $s $refs[$i][1] 2.26 $y 4.13 .35 12 $C.Ink
  }
  T $s '현재 기준 커밋: b571a8f1' .75 6.54 5.45 .29 13 $C.Muted
  T $s '남은 확인 항목' 7.12 2.24 5.46 .42 21 $C.Ink $true
  T $s 'KazumiHoming의 베이크 이벤트' 7.12 3.01 5.46 .4 19 $C.Blue $true
  T $s "현재 clip·그래프에 공격 이벤트가 없습니다.\n기술별 연결을 다시 확인해야 합니다.".Replace('\n',"`r") 7.12 3.59 5.46 .88 16 $C.Body
  Rule $s 7.12 4.76 5.46 $C.Line
  T $s '자동 검증과 측정의 후속 범위' 7.12 5.03 5.46 .40 19 $C.Ink $true
  T $s "Frame·그래프 통합 fixture 재검사\n독립 빌드·교차 플랫폼\n강제 네트워크 rollback·장치별 성능".Replace('\n',"`r") 7.12 5.64 5.46 1.05 16 $C.Body
  Notes $s '전체 상대 경로: docs/ai/evidence/2026-10-17-portfolio-summary.md, docs/ai/loadout-isolation.md, docs/ai/input-buffer.md, docs/ai/hit-effects-investigation.md, docs/ai/air-combo-damage-scaling.md. 최신 기준 b571a8f15e617788a0c86fe6507e992146f1800c. 사용자 재베이크 이후 clip/graph attack events empty는 docs/ai/user-validation-air-combo-2026-10-22.md에 기록됐다. 사용자의 직접 테스트 완료 보고를 전체 자동 검사 통과나 공격 이벤트 복구 증거로 바꾸지 않는다. 원본 JSON의 검증 조건은 해당 시점에 한정된다. 외부 SDK·애드온 및 원본 아트의 기여를 분리한다. 공개 GitHub 링크는 사용자 요청으로 생략했다.'
  # 20. Closing.
  $s=$deck.Slides.Add(20,12);$s.FollowMasterBackground=0;$s.Background.Fill.ForeColor.RGB=Col $C.Black
  T $s 'KIM SEOKJIN' .75 .53 11.83 .28 12 $C.Blue $true
  T $s '김석진' .75 1.44 11.83 .82 44 $C.White $true
  T $s 'AI Native · 클라이언트 개발자' .75 2.66 11.83 .51 25 $C.White
  Rule $s .75 3.62 11.83 $C.DLine
  T $s "중단했던 격투게임을 다시 열고,\n전투 상태와 플레이 흐름을 완성해 간 6일.".Replace('\n',"`r") .75 4.19 11.83 1.12 28 $C.White $true
  T $s '요구사항을 구현과 검증 근거까지 연결한 AI 협업 개발 기록입니다.' .75 6.15 11.83 .43 18 $C.Pale
  Footer $s $true
  Notes $s '연락처는 제공되지 않아 기재하지 않았다. GitHub 링크는 사용자 요청으로 생략했다. 기간은 사용자 설명을 바탕으로 여섯 단계의 작업 흐름으로 재구성했다. 이번 협업에서 달성한 범위와 현재 남은 검증은 앞 슬라이드에서 구분한다.'

  # Make all used theme fonts consistent, including default future text.
  foreach($master in $deck.Designs){
    try {
      $scheme=$master.SlideMaster.Theme.ThemeFontScheme
      foreach($i in 1..6){$scheme.MajorFont.Item($i).Name=$Font;$scheme.MinorFont.Item($i).Name=$Font}
    } catch { }
  }
  try {
    $props=$deck.BuiltInDocumentProperties
    $props.Item('Title').Value='김석진 / Look-See-Do Fighter 개발 포트폴리오'
    $props.Item('Author').Value='김석진'
    $props.Item('Subject').Value='AI Native / 클라이언트 개발'
  } catch { Write-Output 'Optional COM document metadata unavailable; slide content is unaffected.' }
  $pptx=Join-Path $output '김석진_LSDF_AI-Native_클라이언트_포트폴리오.pptx'
  $pdf=Join-Path $output '김석진_LSDF_AI-Native_클라이언트_포트폴리오.pdf'
  $deck.SaveAs($pptx,24,-1)
  $deck.SaveAs($pdf,32)
  foreach($sl in $deck.Slides){$sl.Export((Join-Path $render ('slide-{0:00}.png' -f $sl.SlideIndex)),'PNG',1600,900)}
  $metrics=@()
  foreach($sl in $deck.Slides){
    foreach($sh in $sl.Shapes){
      if($sh.HasTextFrame -eq -1 -and $sh.TextFrame.HasText -eq -1){
        $tr=$sh.TextFrame.TextRange
        $metrics += [pscustomobject]@{slide=$sl.SlideIndex;name=$sh.Name;text=$tr.Text;left=[double]$sh.Left;top=[double]$sh.Top;width=[double]$sh.Width;height=[double]$sh.Height;boundWidth=[double]$tr.BoundWidth;boundHeight=[double]$tr.BoundHeight;boundTop=[double]$tr.BoundTop;boundLeft=[double]$tr.BoundLeft;font=$tr.Font.Name;size=[double]$tr.Font.Size}
      }
    }
  }
  $metrics|ConvertTo-Json -Depth 5|Set-Content -Encoding UTF8 (Join-Path $build 'text-metrics.json')
  [pscustomobject]@{slides=$deck.Slides.Count;standardHeaders=$HeaderSlides.ToArray();pptx=$pptx;pdf=$pdf;font=$Font;widthInches=$W;heightInches=$H;method='Microsoft PowerPoint COM; native text/tables/diagrams; embedded fonts requested'}|ConvertTo-Json -Depth 4|Set-Content -Encoding UTF8 (Join-Path $build 'build-manifest.json')
  Write-Output "Created $($deck.Slides.Count) slides, PPTX, PDF, and slide PNGs."
} catch {
  Write-Output "Authoring stopped at slide $($deck.Slides.Count)"
  Write-Output $_.ScriptStackTrace
  throw
} finally {
  if($deck){$deck.Close()}
  if($app -and $hadApp){$app.DisplayAlerts=$originalAlerts}
  if($app -and -not $hadApp -and $app.Presentations.Count -eq 0){$app.Quit()}
  if($deck){[void][Runtime.InteropServices.Marshal]::ReleaseComObject($deck)}
  if($app){[void][Runtime.InteropServices.Marshal]::ReleaseComObject($app)}
  [GC]::Collect()
}
