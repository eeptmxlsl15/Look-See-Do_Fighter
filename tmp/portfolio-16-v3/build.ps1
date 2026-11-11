$ErrorActionPreference='Stop'
$root='C:/Users/Admin/Look-See-Do_Fighter'
$build=Join-Path $root 'tmp/portfolio-16-v3'
$out=Join-Path $root 'output/portfolio'
$render=Join-Path $build 'powerpoint'
[void][IO.Directory]::CreateDirectory($out)
[void][IO.Directory]::CreateDirectory($render)
$Font='Malgun Gothic'
$C=@{Blue='0070D1'; Deep='004D8D'; Ink='121314'; Body='595959'; Muted='6B6B6B'; White='FFFFFF'; Black='000000'; Soft='F5F7FA'; Line='E5E5E5'; Dark='181818'; Pale='D0D0D0'; DLine='444444'}
$Metrics=[Collections.Generic.List[object]]::new()
$CaptureInfo=[Collections.Generic.List[object]]::new()
$SourceText=Get-Content -Raw -Encoding UTF8 (Join-Path $build 'source.md')
$SourceSlides=[regex]::Matches($SourceText,'(?ms)^### Slide (?<num>\d{2})\.(?<body>.*?)(?=^### Slide \d{2}\.|\z)')
$Sources=@(
 'docs/PORTFOLIO_SOURCE.md; docs/ai/evidence/2026-10-17-portfolio-summary.md',
 'docs/ai/architecture-audit.md; docs/ai/refactor-remaining-result.md',
 'docs/ai/loadout-isolation.md; docs/ai/best-of-five.md',
 'AGENTS.md; Assets/Resources/LSDF_SystemConfig.asset; Assets/QuantumUser/View/LSDF.View.asmdef',
 'docs/ai/refactor-p0-result.md; docs/ai/loadout-isolation.md; docs/ai/evidence/fighter-publish-validation.json',
 'docs/ai/refactor-phase2-result.md; docs/ai/refactor-phase3-result.md; docs/ai/refactor-phase4-result.md; docs/ai/refactor-remaining-result.md',
 'docs/ai/input-buffer.md; docs/ai/evidence/fighter-publish-validation.json (input tick 384 / dispatch tick 392)',
 'docs/ai/best-of-five.md; docs/ai/match-return-to-main.md; docs/ai/user-validation-match-return.md',
 'docs/ai/homing-air-damage-investigation-2026-10-22.md; docs/ai/air-combo-damage-scaling.md; docs/ai/evidence/air-combo-damage-scaling-validation.json',
 'docs/ai/natural-rehit-fall.md; docs/ai/air-hit-push-distance.md; docs/ai/air-sprite-pivot-fix.md; docs/ai/evidence/natural-rehit-fall-validation.json',
 'docs/ai/hit-effects-investigation.md; docs/ai/hit-effects-prefab-migration.md; docs/ai/effect-opacity-action-flash.md; docs/ai/health-bar-shake.md; docs/ai/counter-hit-zoom.md; docs/ai/camera-hit-shake.md',
 'docs/ai/infinite-stage-background.md; docs/ai/stage-background-flat-zoom.md; docs/ai/gameplay-ui-images.md; docs/ai/health-bar-image-fit.md; docs/ai/health-bar-sprite-flip.md',
 'AGENTS.md; AI_SETUP.md; Skill.md; Hook.md; docs/ai/context-resume.md',
 '.agents/skills/*/SKILL.md; .codex/hooks.json; tools/ai/quantum_guard.py; docs/ai/setup-verification.md',
 'docs/ai/evidence/fighter-publish-validation.json; docs/ai/evidence/health-shake-publish-validation.json; docs/ai/evidence/natural-rehit-fall-validation.json; docs/ai/evidence/air-hit-push-distance-validation.json; docs/ai/evidence/air-sprite-pivot-validation.json; docs/ai/evidence/2026-10-23-push-validation.json; docs/ai/push-infinite-stage-and-air-hit-2026-10-23.md',
 'docs/PORTFOLIO_SOURCE.md; docs/ai/current-state.md; docs/ai/development-workflow.md'
)
function Col([string]$h){[Convert]::ToInt32($h.Substring(0,2),16)+256*[Convert]::ToInt32($h.Substring(2,2),16)+65536*[Convert]::ToInt32($h.Substring(4,2),16)}
function Txt($s,[string]$text,$x,$y,$w,$h,$size=16,$color='121314',$bold=$false,$align=1,$name=''){
 $sh=$s.Shapes.AddTextbox(1,$x*72,$y*72,$w*72,$h*72)
 if($name){$sh.Name=$name}
 $tf=$sh.TextFrame;$tf.MarginLeft=0;$tf.MarginRight=0;$tf.MarginTop=0;$tf.MarginBottom=0;$tf.WordWrap=-1;$tf.AutoSize=0;$tf.VerticalAnchor=1
 $tr=$tf.TextRange;$tr.Text=$text;$tr.Font.Name=$Font;$tr.Font.NameFarEast=$Font;$tr.Font.NameComplexScript=$Font
 $tr.Font.Size=[single]$size;$tr.Font.Bold=$(if($bold){-1}else{0});$tr.Font.Color.RGB=Col $color
 $tr.ParagraphFormat.Alignment=$align;$tr.ParagraphFormat.SpaceBefore=0;$tr.ParagraphFormat.SpaceAfter=0
 $tr.ParagraphFormat.LineRuleWithin=-1;$tr.ParagraphFormat.SpaceWithin=[single]1.18
 $sh.Width=[single]($w*72);$sh.Height=[single]($h*72)
}
function Rect($s,$x,$y,$w,$h,$fill,$line=''){
 $r=$s.Shapes.AddShape(1,$x*72,$y*72,$w*72,$h*72);$r.Fill.ForeColor.RGB=Col $fill;$r.Line.Visible=0
 if($line){$r.Line.Visible=-1;$r.Line.ForeColor.RGB=Col $line;$r.Line.Weight=[single].7}
}
function Line($s,$x,$y,$w,$color='E5E5E5',$weight=.7){$a=$s.Shapes.AddLine($x*72,$y*72,($x+$w)*72,$y*72);$a.Line.ForeColor.RGB=Col $color;$a.Line.Weight=[single]$weight}
function VLine($s,$x,$y,$h,$color='E5E5E5'){$a=$s.Shapes.AddLine($x*72,$y*72,$x*72,($y+$h)*72);$a.Line.ForeColor.RGB=Col $color;$a.Line.Weight=[single].7}
function Arrow($s,$x1,$y1,$x2,$y2,$color='0070D1'){$a=$s.Shapes.AddLine($x1*72,$y1*72,$x2*72,$y2*72);$a.Line.ForeColor.RGB=Col $color;$a.Line.Weight=[single]1.3;$a.Line.EndArrowheadStyle=3}
function Head($s,$text,$x,$y,$w=5.5,$color='121314'){Txt $s $text $x $y $w .37 19 $color $true}
function Body($s,$text,$x,$y,$w,$h,$size=16,$color='595959'){Txt $s $text $x $y $w $h $size $color}
function Label($s,$text,$x,$y,$w=5.5,$color='0070D1'){Txt $s $text $x $y $w .25 11.5 $color $true}
function Lines($s,[string[]]$items,$x,$y,$w,$step=.39,$size=16,$color='595959'){
 for($i=0;$i -lt $items.Count;$i++){Txt $s ('• '+$items[$i]) $x ($y+$i*$step) $w ($step-.035) $size $color}
}
function Foot($s,$dark=$false){
 $fc=$(if($dark){$C.Pale}else{$C.Muted})
 Txt $s '김석진 · Look-See-Do Fighter' .75 7.12 10 .22 10.5 $fc
 Txt $s ('{0:00}' -f $s.SlideIndex) 12.05 7.12 .53 .22 10.5 $fc $false 3
}
function CaptureLayout($num,$x,$y,$w,$h,$label){
 $cl=$deck.SlideMaster.CustomLayouts.Add($deck.SlideMaster.CustomLayouts.Count+1)
 $cl.Name='Capture '+$num
 for($i=$cl.Shapes.Count;$i -ge 1;$i--){$cl.Shapes.Item($i).Delete()}
 $ph=$cl.Shapes.AddPlaceholder(18,$x*72,$y*72,$w*72,$h*72)
 $ph.Name='Capture_'+$num
 $ph.Fill.ForeColor.RGB=Col $C.Soft;$ph.Line.Visible=-1;$ph.Line.ForeColor.RGB=Col $C.Line
 $ph.TextFrame.TextRange.Text=$label
 $ph.TextFrame.TextRange.Font.Name=$Font;$ph.TextFrame.TextRange.Font.NameFarEast=$Font;$ph.TextFrame.TextRange.Font.Size=14
 $ph.LockAspectRatio=0;$ph.TextFrame.AutoSize=0;$ph.TextFrame.WordWrap=-1
 $ph.Left=[single]($x*72);$ph.Top=[single]($y*72);$ph.Width=[single]($w*72);$ph.Height=[single]($h*72)
 $CaptureInfo.Add([pscustomobject]@{slide=$num;x=$x;y=$y;width=$w;height=$h;label=$label;type='Native picture placeholder'})
 return $cl
}
function Std($chapter,$title,$subtitle,$dark=$false,$capture=$null){
 if($capture){$s=$deck.Slides.AddSlide($deck.Slides.Count+1,$capture)}else{$s=$deck.Slides.Add($deck.Slides.Count+1,12)}
 if($capture){
  $geo=$CaptureInfo[$CaptureInfo.Count-1]
  foreach($ph in $s.Shapes.Placeholders){
   if($ph.PlaceholderFormat.Type -eq 18){
    $ph.LockAspectRatio=0
    $ph.Left=[single]($geo.x*72);$ph.Top=[single]($geo.y*72);$ph.Width=[single]($geo.width*72);$ph.Height=[single]($geo.height*72)
   }
  }
 }
 $s.FollowMasterBackground=0;$s.Background.Fill.ForeColor.RGB=Col $(if($dark){$C.Black}else{$C.White})
 $ink=$(if($dark){$C.White}else{$C.Ink});$body=$(if($dark){$C.Pale}else{$C.Body})
 Txt $s $chapter .75 .40 11.83 .25 11.5 $C.Blue $true 1 'standard-chapter'
 Txt $s $title .75 .82 11.83 .55 30 $ink $true 1 'standard-title'
 Txt $s $subtitle .75 1.48 11.83 .36 16 $body $false 1 'standard-subtitle'
 Line $s .75 1.98 11.83 $(if($dark){$C.DLine}else{$C.Line})
 Foot $s $dark
 return $s
}
function CaptureLabel($s,$title,$subtitle,$x,$y,$w,$h){
 # Text sits outside the native picture placeholder so its insertion icon remains clickable.
 Label $s $title $x ($y-.34) $w
 Body $s $subtitle $x ($y+$h+.08) $w .44 11 $C.Muted
}
function Note($s,$extra=''){
 $idx=$s.SlideIndex-1
 $text=('Slide {0:00}. ' -f $s.SlideIndex)+$SourceSlides[$idx].Groups['body'].Value.Trim()+"`r`r출처`r"+$Sources[$idx]
 if($extra){$text+="`r`r"+$extra}
 $tr=$s.NotesPage.Shapes.Placeholders.Item(2).TextFrame.TextRange
 $tr.Text=$text;$tr.Font.Name=$Font;$tr.Font.NameFarEast=$Font;$tr.Font.Size=12
}
function Table($s,$x,$y,[double[]]$cols,[double[]]$heights,[object[]]$rows,$size=14.5,$dark=$false){
 $width=($cols|Measure-Object -Sum).Sum;$height=($heights|Measure-Object -Sum).Sum
 $sh=$s.Shapes.AddTable($rows.Count,$cols.Count,$x*72,$y*72,$width*72,$height*72);$tb=$sh.Table
 for($ci=1;$ci -le $cols.Count;$ci++){$tb.Columns.Item($ci).Width=$cols[$ci-1]*72}
 for($r=1;$r -le $rows.Count;$r++){
  $tb.Rows.Item($r).Height=$heights[$r-1]*72
  for($ci=1;$ci -le $cols.Count;$ci++){
   $cell=$tb.Cell($r,$ci);$cs=$cell.Shape
   $fill=$(if($r -eq 1){$C.Ink}elseif($dark){$C.Dark}elseif($r%2 -eq 0){$C.Soft}else{$C.White})
   $cs.Fill.ForeColor.RGB=Col $fill
   foreach($edge in 1..4){$cell.Borders.Item($edge).ForeColor.RGB=Col $(if($dark){$C.Black}else{$C.White});$cell.Borders.Item($edge).Weight=[single].7}
   $tf=$cs.TextFrame;$tf.MarginLeft=10;$tf.MarginRight=8;$tf.MarginTop=5;$tf.MarginBottom=4;$tf.VerticalAnchor=3
   $tr=$tf.TextRange;$tr.Text=[string]$rows[$r-1][$ci-1];$tr.Font.Name=$Font;$tr.Font.NameFarEast=$Font;$tr.Font.Size=[single]$size
      $tr.Font.Bold=$(if($r -eq 1){-1}else{0});$tr.Font.Color.RGB=Col $(if($r -eq 1 -or $dark){$C.White}else{$C.Ink})
   $tr.ParagraphFormat.SpaceBefore=0;$tr.ParagraphFormat.SpaceAfter=0;$tr.ParagraphFormat.SpaceWithin=[single]1.1
  }
 }
}
function Node($s,$title,$desc,$x,$y,$w,$h=.95,$accent=$false,$dark=$false,$size=17){
 $fill=$(if($accent){$C.Blue}elseif($dark){$C.Dark}else{$C.Soft});$ink=$(if($accent -or $dark){$C.White}else{$C.Ink})
 Rect $s $x $y $w $h $fill
 Txt $s $title ($x+.15) ($y+.15) ($w-.30) .35 $size $ink $true 2
 if($desc){Txt $s $desc ($x+.15) ($y+.57) ($w-.30) ($h-.64) 13.5 $(if($accent -or $dark){$C.Pale}else{$C.Body}) $false 2}
}
function Picture($s,$path,$x,$y,$w,$h){
 $sh=$s.Shapes.AddPicture($path,0,-1,$x*72,$y*72,-1,-1);$sh.LockAspectRatio=-1;$sh.Width=$w*72
 if($sh.Height -gt $h*72){$sh.Height=$h*72}
 $sh.Left=$x*72+($w*72-$sh.Width)/2;$sh.Top=$y*72+($h*72-$sh.Height)/2
}
$app=$null;$deck=$null;$hadApp=$false
try{
 try{$app=[Runtime.InteropServices.Marshal]::GetActiveObject('PowerPoint.Application');$hadApp=$true}catch{$app=New-Object -ComObject PowerPoint.Application}
 $oldAlerts=$app.DisplayAlerts;$app.DisplayAlerts=1
 $deck=$app.Presentations.Add(0);$deck.PageSetup.SlideWidth=[single]959.976;$deck.PageSetup.SlideHeight=540

 # 01 — editorial cover, all stack items retained.
 $s=$deck.Slides.Add(1,12);$s.FollowMasterBackground=0;$s.Background.Fill.ForeColor.RGB=Col $C.Black
 Txt $s 'AI NATIVE GAME DEVELOPMENT PORTFOLIO' .75 .48 11.83 .3 12 $C.Blue $true
 Txt $s 'Look-See-Do Fighter' .75 1.12 11.83 .78 44 $C.White $true
 Txt $s 'AI 협업을 통한 격투 게임 리팩터링 및 개발 자동화' .75 2.1 11.83 .5 24 $C.White $true
 Line $s .75 2.91 11.83 $C.DLine
 Body $s "개발을 중단했던 2D 온라인 격투 게임을 다시 분석하고, 기존 전투 콘텐츠를 보존하면서 결정론적 아키텍처와 게임플레이를 개선했습니다.`r`rCodex와 Unity MCP를 활용해 코드 조사부터 기능 구현, 테스트 및 검증 결과 관리까지 이어지는 AI Native 개발 워크플로를 구축했습니다." .75 3.35 6.15 2.43 18 $C.Pale
 Label $s 'TECH STACK' 7.65 3.35 4.93
 $stack=@(@('Game Engine','Unity 6 / C#'),@('Networking','Photon Quantum 3'),@('Architecture','Deterministic ECS / Predict & Rollback'),@('AI Development','Codex CLI / Astra / Unity MCP'),@('Automation','AGENTS.md · Skills · Hooks · Git/GitHub'),@('Testing','Unity Test Framework / NUnit / Python'))
 for($i=0;$i -lt $stack.Count;$i++){Txt $s $stack[$i][0] 7.65 (3.82+$i*.43) 1.28 .28 11 $C.Pale;Txt $s $stack[$i][1] 9.0 (3.80+$i*.43) 3.58 .37 13.5 $C.White}
 Txt $s '결정론적 네트워크 · 기존 시스템 리팩터링 · AI 개발 워크플로 · 자동 검증' .75 6.45 11.83 .32 14 $C.Pale
 Foot $s $true;Note $s 'SDK·기존 아트·기존 전투 콘텐츠 제작을 이번 협업의 신규 성과에 포함하지 않는다. GitHub 개인 링크는 사용자 요청에 따라 제외했다.'

 # 02 — problem context plus four deliberate goals.
 $s=Std '01  PROJECT / 개발 목표' '프로젝트 재개와 개발 목표' '기존 게임의 완성과 함께, 반복 가능한 AI Native 개발 방식을 구축했습니다.'
 Label $s '재개 시점의 과제' .75 2.23 4.7
 Head $s '전투 콘텐츠는 유지하고' .75 2.67 4.7
 Head $s '설계 경계는 다시 정리' .75 3.04 4.7
 Body $s "기본 전투·공격 기술·애니메이션·네트워크 구조가 구현된 프로젝트였습니다.`r`r공유 에셋과 로컬 데이터에 의존한 실행 상태, 공격별 중복 코드, 복잡한 전이와 반복적인 수동 검증이 유지보수 부담으로 이어졌습니다." .75 3.65 4.7 2.0
 VLine $s 5.85 2.23 3.45
 $goals=@(@('01','구조적 안정성','Quantum Simulation과 Unity View 책임 분리'),@('02','기존 콘텐츠 보존','37종 공격 타격 시점·애니메이션 참조 유지'),@('03','게임플레이 확장','선입력·3선승·공중 콤보·전투 연출'),@('04','AI 개발 체계','Skills·Hooks·MCP·테스트·버전 관리 표준화'))
 for($i=0;$i -lt 4;$i++){$yy=2.25+$i*.84;Txt $s $goals[$i][0] 6.3 $yy .6 .38 20 $C.Blue $true;Head $s $goals[$i][1] 7.05 $yy 5.5;Body $s $goals[$i][2] 7.05 ($yy+.42) 5.53 .32 15}
 Line $s .75 5.97 11.83
 Label $s '개발 접근 방식' .75 6.17 2
 Body $s '분석 → 결정론 구조 → 콘텐츠 리팩터링 → 기능 확장 → 플레이 오류 해결 → 검증·기록' 2.65 6.14 9.93 .65 15.5
 Note $s

 # 03 — game concepts and a native screenshot slot.
 $cl=CaptureLayout 3 7.55 3.23 5.03 2.83 '기술 선택 또는 실제 대전 화면'
 $s=Std '01  PROJECT / 게임 시스템' '기술 구성과 프레임 단위 심리전' '사용할 기술을 직접 구성하고, 발동·지속·회복과 거리·타이밍의 상호작용을 읽는 게임입니다.' $false $cl
 $ms=@(@('28','방향 7 × 버튼 4 슬롯'),@('3','슬롯별 기술 선택지'),@('140','기본 로드아웃 예산'))
 for($i=0;$i -lt 3;$i++){$xx=.75+$i*4.06;Txt $s $ms[$i][0] $xx 2.15 1.35 .60 34 $C.Blue $true;Body $s $ms[$i][1] ($xx+1.48) 2.37 2.4 .5 15.5}
 Head $s '기술 커스터마이징' .75 3.22 6.2
 Body $s '기술별 비용과 예산 제한 안에서 28슬롯의 로드아웃을 구성합니다.' .75 3.68 6.1 .58
 Head $s '프레임 기반 전투' .75 4.46 6.2
 Body $s '발동·지속·회복, 상·중·하단 판정과 가드·패링·카운터·경직을 이용해 거리와 공격 타이밍을 선택합니다.' .75 4.91 6.1 .85
 Head $s '온라인 매치' .75 5.84 6.2
 Body $s "Photon 입력 동기화 · 라운드 HP/전투 상태 초기화`r3승 선취 · KO/동시 KO · 최종 결과" .75 6.27 6.1 .58 14.5
 CaptureLabel $s 'GAMEPLAY CAPTURE' '기술 선택 화면 또는 실제 대전 화면을 삽입해 주세요.' 7.55 3.23 5.03 2.83
 Body $s "제한된 기술 구성과 프레임 단위 선택이`r전략적 깊이를 만드는 게임을 지향합니다." 7.55 6.37 5.03 .55 14
 Note $s '캡처 권장: 로드아웃 28슬롯 또는 두 캐릭터와 HUD가 보이는 GameView. 16:9 이미지. 기본 삽입 영역은 실제 PowerPoint 그림 자리 표시자다.'

 # 04 — architecture on a dark canvas with editable flow.
 $s=Std '02  ARCHITECTURE' 'Simulation과 Unity View의 책임 분리' '동일한 입력과 상태로 같은 전투 결과를 계산하도록 ECS·Frame 경계를 정리했습니다.' $true
 $nodes=@(@('Unity Local','메뉴·기술 선택`rPlayerPrefs·입력'),@('RuntimePlayer','로드아웃 복사`r연결 데이터 전달'),@('Quantum Frame','HP·상태·입력 버퍼`r콤보·라운드'),@('Quantum Systems','이동·공격 판정`r피격·상태 전환'),@('Unity View','Animator 표현·HUD`rVFX·카메라·사운드'))
 for($i=0;$i -lt 5;$i++){$xx=.75+$i*2.42;Node $s $nodes[$i][0] ($nodes[$i][1].Replace('`r',"`r")) $xx 2.38 2.15 1.42 ($i -eq 2) $true 14;if($i -lt 4){Arrow $s ($xx+2.19) 3.08 ($xx+2.36) 3.08}}
 Label $s '상태와 데이터' .75 4.24 5.65
 Lines $s @('전투 결과에 영향을 주는 지속 상태는 Frame','공격 설정은 불변 데이터로 취급','판정은 FP·정수 tick 기반 계산') .75 4.72 5.65 .53 16 $C.Pale
 Label $s '판정과 표현' 7.0 4.24 5.58
 Lines $s @('시각적 상태가 전투 결과를 결정하지 않음','Quantum Animator·시스템 실행 순서 보존','롤백 복원 상태와 화면 표현 상태를 구분') 7.0 4.72 5.58 .53 16 $C.Pale
 Line $s .75 6.39 11.83 $C.DLine
 Body $s '복원해야 하는 상태의 소유권을 명확히 하여 네트워크 동기화와 기능 확장에 대응했습니다.' .75 6.57 11.83 .32 15 $C.Pale
 Note $s '도식은 데이터 책임의 개념 흐름이다. Quantum Frame과 Systems는 실제로 반복적으로 상호작용한다. 입력은 CallbackPollInput 경로로 전달한다. 현재 noEngineReferences는 false이며 모든 Unity 참조가 컴파일러로 차단된다는 의미는 아니다.'

 # 05 — state ownership, two problems and two solutions.
 $s=Std '02  REFACTOR / 상태 소유권' '로컬 선택·공유 설정·Frame 실행 상태' '플레이어별 상태가 입력 계약과 Frame 복원 경계를 우회하던 구조를 바꿨습니다.'
 Label $s 'PROBLEM' .75 2.23 3.7
 Body $s "스폰이 각 클라이언트의 PlayerPrefs에서 기술을 직접 읽었습니다.`r`r잽 연계 예약 BufferedNextAttack은 공유 Window Event 에셋에 저장돼 있었습니다." .75 2.68 3.7 1.86
 VLine $s 4.83 2.23 3.78
 Head $s '로드아웃 전달 구조' 5.25 2.23 7.33
 Txt $s 'PlayerPrefs → 검증 → RuntimePlayer → AddPlayer → Frame' 5.25 2.82 7.33 .66 16 $C.Blue $true
 Lines $s @('기술 ID·슬롯 수·비용을 검증한 복사본 전달','Simulation은 f.GetPlayerData()로 접근','반복 AddPlayer의 중복 생성·상태 초기화 방지') 5.25 3.62 7.33 .43 16
 Head $s '실행 상태의 소유권' .75 4.86 3.7
 Body $s "공유 설정과 플레이어 상태,`r호출별 임시 계산을 분리했습니다." .75 5.38 3.7 .65 16
 Lines $s @('BufferedNextAttack → 플레이어 Frame 컴포넌트','호출별 임시 상태 → 지역변수','공격 수치·설정 → 불변 AssetObject','애니메이션 프레임 환산 → FP 계산') 5.25 5.02 7.33 .38 15.5
 Line $s .75 6.63 11.83
 Body $s '후속 검사: 서로 다른 로드아웃·반복 AddPlayer·입력 재생·Frame 복원 시나리오 확인' .75 6.78 11.83 .25 13.5
 Note $s

 # 06 — native contract table and migration sequence.
 $s=Std '02  REFACTOR / 공격 시스템' '37종 공격의 판정 계약 보존' '타격 데이터와 생성 절차를 분리하고, 공격별 고유 동작은 기존 이벤트에 유지했습니다.'
 Label $s 'PROBLEM → SOLUTION' .75 2.23 5.2
 Body $s '피해·경직·피격 높이·생성 코드가 중복됐지만, 단순 공통화는 타이밍·특수 동작·Animator·직렬화 참조를 바꿀 위험이 있었습니다.' .75 2.68 5.2 1.05
 Head $s '대표 유형부터 점진적으로 이관' .75 4.00 5.2
 $migrations=@(@('1','왼어퍼'),@('4','잽'),@('2','다단히트'),@('30','나머지'))
 for($i=0;$i -lt 4;$i++){$xx=.75+$i*1.34;Txt $s $migrations[$i][0] $xx 4.62 1.05 .56 30 $C.Blue $true;Body $s $migrations[$i][1] $xx 5.25 1.12 .32 14.5;if($i -lt 3){Txt $s '→' ($xx+1.02) 4.8 .28 .3 16 $C.Body}}
 Table $s 6.4 2.28 @(3.05,3.13) @(.42,.66,.66,.66,.66) @(@('구성 요소','담당 역할'),@('LSDFSingleHitDefinition','단타 피해·경직·판정 데이터'),@('LSDFTimedHitDefinition','다단 생성 프레임·타격 데이터'),@('LSDFHitboxFactory','Collider·Hitbox·파괴 tick'),@('기존 Window Event','이동·연계·특수 동작·종료')) 14
 Label $s '보존한 계약' 6.4 5.62 6.18
 Body $s "생성 프레임·피해·경직 / 타입명·GUID·Asset ID·Animator`r다단 타이밍·특수 처리 / 좌우 반전·생성/파괴 tick 경계" 6.4 6.01 6.18 .66 14.5
 Line $s .75 6.63 5.2
 Body $s '중복 생성 절차를 공통화하면서 기존 게임플레이 계약을 유지했습니다.' .75 6.04 5.2 .58 16
 Note $s '37종은 계약 이관 범위다. 모든 공격 조합을 온라인에서 전수 검증했다는 뜻은 아니다. StartupFrames-1 생성·생성 tick+1 파괴 및 실제 생성 시각과 startFrame 메타데이터 구분을 보존했다.'

 # 07 — explicit pending/routing distinction with historical ticks.
 $s=Std '03  GAMEPLAY / 입력 버퍼' '프레임 단위 선입력 시스템' '여러 tick을 거치는 Animator 전이에서 입력이 사라지지 않도록 대기와 전달 상태를 나눴습니다.'
 Node $s '입력 캡처' '새 눌림의 방향·버튼·tick' .75 2.25 2.68 1.12
 Node $s '대기' '행동 잠금 중 최신 명령 1개' 3.8 2.25 2.68 1.12
 Node $s '전달 중' 'Animator 소비까지 유지' 6.85 2.25 2.68 1.12 $true
 Node $s '정리' '만료·KO·라운드 전환' 9.9 2.25 2.68 1.12
 foreach($xx in @(3.47,6.52,9.57)){Arrow $s $xx 2.82 ($xx+.27) 2.82}
 Label $s '동작과 경계' .75 3.77 5.72
 Lines $s @('잠금 해제 후 첫 명령 처리 tick에 전달','Animator가 처리할 때까지 승인 명령 유지','홀드로 유효시간이 계속 갱신되지 않음','방향 변경·키 해제 후에도 저장 명령 유지','유효시간 초과·KO·라운드 전환 시 정리') .75 4.22 5.72 .44 16
 VLine $s 6.88 3.77 2.67
 Label $s '설정과 실제 관측' 7.3 3.77 5.28
 Txt $s '20 tick' 7.3 4.24 2.53 .58 30 $C.Blue $true
 Txt $s '10 tick' 10.13 4.24 2.45 .58 30 $C.Ink $true
 Body $s '현재 게임 설정' 7.3 4.92 2.53 .32 14.5
 Body $s '새 객체 초기값' 10.13 4.92 2.45 .32 14.5
 Head $s '입력 384 → 전달 392 tick' 7.3 5.58 5.28
 Body $s 'Photon 양쪽 세션에서 정상 전달 관측' 7.3 6.05 5.28 .35 15
 Line $s .75 6.59 11.83
 Body $s '후속 전체 검사 157 PASS · 8tick 차이는 해당 잠금 사례의 관측값이며 평균 입력 지연이 아닙니다.' .75 6.77 11.83 .26 13.5
 Note $s '157은 당시 전체 테스트 묶음이며 입력 버퍼만의 테스트 개수가 아니다. 현재 에셋 값 20tick과 기본 객체 10tick을 구분한다.'

 # 08 — match cycle, native picture placeholder for the result.
 $cl=CaptureLayout 8 8.24 3.84 4.34 2.44 '최종 3:2 결과 또는 Round / Fight 화면'
 $s=Std '03  GAMEPLAY / 라운드·매치' 'KO 이후 3선승 매치 흐름 연결' '라운드 종료·초기화·다음 시작·최종 종료의 기준 상태를 Quantum Frame으로 관리합니다.' $false $cl
 $labels=@('WaitingForPlayers','Intro','Fighting','RoundEnd','Intro / MatchEnd')
 for($i=0;$i -lt 5;$i++){$xx=.75+$i*2.42;Node $s $labels[$i] '' $xx 2.27 2.15 .68 ($i -eq 4) $false 14.5;if($i -lt 4){Arrow $s ($xx+2.19) 2.61 ($xx+2.36) 2.61}}
 Label $s '주요 구현' .75 3.35 6.8
 Lines $s @('KO 결과를 한 번만 확정해 중복 처리 방지','동시 KO는 무득점, 같은 라운드 재시작','HP·위치·공격·버퍼·콤보 상태 초기화','승수·기술 로드아웃은 라운드 사이 유지','결과 120tick · 인트로 150tick · 3승 선취','verified 매치 종료 → 연결 종료·메인 복귀') .75 3.83 6.95 .43 16
 CaptureLabel $s 'MATCH RESULT CAPTURE' '최종 결과 또는 Round/Fight 전환 화면을 넣어 주세요.' 8.24 3.84 4.34 2.44
 Line $s .75 6.59 11.83
 Body $s 'Photon: KO 5회·초기화/인트로 4회·최종 3:2·verified Frame 진행 확인. 메인 복귀는 후속 구현·사용자 확인.' .75 6.77 11.83 .28 13
 Note $s '기존 최초 KO 결과 고정에서 확장한 흐름이다. 매치 단계의 도식이며 정확한 시스템 실행 순서도는 아니다. 메인 복귀 자동 fixture 미검증 이력과 사용자 테스트 확인은 구분한다. 캡처는 실제 대전의 결과 화면을 권장한다.'

 # 09 — root cause and native table, failure remains visible.
 $s=Std '04  DEBUGGING / 공중 피해' '애니메이션 진입 수와 실제 타격 수' '다른 의미의 카운터를 피해 공식에 사용하던 문제를 실제 콤보 접촉 횟수 기준으로 수정했습니다.'
 Label $s 'ROOT CAUSE' .75 2.23 5.15
 Head $s 'LSDF_Player.hitCount' .75 2.68 5.15
 Body $s '실제 적중 수가 아닌 Air 애니메이션 진입 횟수였습니다. 피격 전이가 바뀌면서 두 값이 달라졌고, 특정 카운트에서 피해 0 분기가 반복됐습니다.' .75 3.17 5.15 1.12
 Arrow $s 1.02 4.49 1.02 4.87
 Head $s 'LSDF_AirCombo.HitCount' .75 5.03 5.15 $C.Blue
 Body $s 'Juggle.OnHit → 실제 HitCount 갱신 → ApplyHit 계산. 정수 비율과 곱셈 중간 오버플로를 고려했습니다.' .75 5.57 5.15 .91
 Table $s 6.36 2.3 @(2.65,1.38,2.19) @(.5,.61,.61,.61,.61) @(@('타격','배율','기본 피해 20'),@('지상 시동기','100%','20'),@('공중 후속 1타','70%','14'),@('공중 후속 2타','50%','10'),@('공중 후속 3~5타','30%','6')) 15
 Txt $s '109 PASS / 1 FAIL' 6.36 5.63 6.22 .43 23 $C.Blue $true
 Body $s '110개 중 체력 단위 32개 PASS. 통합 예상값 보정 후 재실행하지 않은 범위는 미검증으로 기록했습니다.' 6.36 6.12 6.22 .61 14.5
 Body $s '데이터가 실제로 무엇을 의미하는지 확인하는 것이 원인 분석의 핵심이었습니다.' .75 6.78 11.83 .25 13.5
 Note $s '지상 카운터 120%는 유지. 후속 비율은 정수 버림이며 최소 피해 1을 임의 강제하지 않는다. 1 FAIL은 통합 fixture 내부의 구 entry count 기대 불일치이며 32개 체력 단위 테스트와 구분한다.'

 # 10 — progression and original asset comparison, no invented gameplay screenshot.
 $s=Std '04  DEBUGGING / 공중 궤적' '공중 상태와 자연스러운 타격 반응' '고정 높이의 부자연스러운 움직임과, 화면상 재상승 현상을 서로 다른 원인으로 분리했습니다.'
 $phases=@('RehitRising','Returning','SlowFalling','Natural Fall')
 for($i=0;$i -lt 4;$i++){$xx=.75+$i*3.05;Node $s $phases[$i] '' $xx 2.25 2.68 .67 ($i -eq 2) $false 16;if($i -lt 3){Arrow $s ($xx+2.72) 2.58 ($xx+2.99) 2.58}}
 Head $s 'Frame 기반 궤적' .75 3.32 6.0
 Lines $s @('단계·기준 높이·시작/종료 tick 저장','재타격 작은 상승·기준 높이 복귀','Hermite 보간·tick 기반 자연 낙하 가속','5후속타·지정 마무리의 별도 날리기','공격자 반대 방향 밀림, 거리·시간 Inspector') .75 3.83 6.0 .44 15.5
 VLine $s 7.0 3.31 2.74
 Head $s 'Sprite pivot 원인 분리' 7.42 3.32 5.16
 Body $s '물리 높이와 표시 기준점을 비교해 원인을 확인했습니다. 양쪽 공중 애니메이션 후반부의 메타데이터 60개를 통일했습니다.' 7.42 3.83 5.16 1.03
 Picture $s (Join-Path $root 'Assets/Sprites/Character/Player/Move/Idle/Look-See-Do_Bot_export_Idle_01.png') 7.92 4.94 1.4 1.05
 Picture $s (Join-Path $root 'Assets/Sprites/Character/Player/Attack/Lp/LeftUpper/Look-See-Do_Bot_export_LeftUpper_20.png') 10.12 4.94 1.4 1.05
 Body $s '원본 동작 에셋 · 실행 화면/pivot 전후 비교 아님' 7.42 6.06 5.16 .23 10.5
 Line $s .75 6.32 11.83
 Txt $s '106 PASS' .75 6.50 2.35 .36 21 $C.Blue $true
 Txt $s '99 PASS' 3.87 6.50 2.35 .36 21 $C.Blue $true
 Txt $s '66 PASS' 6.99 6.50 2.35 .36 21 $C.Blue $true
 Body $s '자연 낙하' .75 6.87 2.35 .20 10
 Body $s '공중 밀림' 3.87 6.87 2.35 .20 10
 Body $s 'Sprite pivot' 6.99 6.87 2.35 .20 10
 Body $s "제어된 입력 재생·Frame 복원`r체크섬 비교 수행" 10.1 6.49 2.48 .60 11.5
 Note $s '각 수치는 다른 작업의 실행 결과이며 합산하지 않는다. 106개 자연 낙하 검사에서 실제 Photon 두 세션·입력 재생·Frame 복원 근거가 있다. 강제 네트워크 지터 검사는 미실행. 원본 이미지 경로: Assets/Sprites/Character/Player/Move/Idle/Look-See-Do_Bot_export_Idle_01.png 및 Assets/Sprites/Character/Player/Attack/Lp/LeftUpper/Look-See-Do_Bot_export_LeftUpper_20.png. 두 이미지는 원본 캐릭터 동작 소개이며 pivot 수정 전후를 재현한 이미지가 아니다.'

 # 11 — presentation ownership, capture as replaceable picture placeholder.
 $cl=CaptureLayout 11 7.86 2.65 4.72 2.655 '피격 이펙트·카운터 줌 화면'
 $s=Std '05  PRESENTATION / 전투 연출' '판정과 분리된 전투 피드백' '게임 결과를 바꾸지 않으면서 예측 표현의 생성·확인·취소 수명을 관리했습니다.' $false $cl
 Head $s '이펙트와 예측 이벤트' .75 2.28 6.55
 Lines $s @('경기 updater 소유 ViewComponent로 연결','타격 유형별 프리팹·Animator 주기 기반 수명','예측 이벤트 취소 시 VFX·오디오 정리','MaterialPropertyBlock 기반 색상 표현') .75 2.80 6.55 .41 16
 Head $s '체력과 카메라 연출' .75 4.73 6.55
 Lines $s @('HP 변화에 따른 체력바 흔들림','기술별 카운터 줌 설정','Follow → Zoom → Shake 변환 합성','중복 이벤트·잔여 오프셋 방지') .75 5.24 6.55 .4 16
 CaptureLabel $s 'COMBAT FEEDBACK CAPTURE' '피격 순간의 이펙트와 HUD·카메라 구도가 보이는 화면' 7.86 2.65 4.72 2.655
 Label $s '화면 상태의 기준' 7.86 5.86 4.72
 Body $s "HP: predicted / 승패·라운드: verified`r예측 시각효과와 확정 결과를 구분" 7.86 6.25 4.72 .59 13.5
 Note $s 'MPB는 후속 호밍·패링 흰색 표현에서 사용한 경로다. 일부 VFX·카메라 fixture 수정 후 자동 미검증 이력과 사용자 직접 확인은 원천 자료에 구분돼 있다. 이 슬라이드는 모든 실제 표현의 자동 검증 완료를 주장하지 않는다. 실제 피격 또는 카운터 순간을 캡처해 그림 자리 표시자에 넣을 수 있다.'

 # 12 — reusable tools with screenshot slot, all four evidence results retained.
 $cl=CaptureLayout 12 8.24 4.0 4.34 2.441 '게임플레이 HUD 또는 Inspector 이미지 설정'
 $s=Std '05  PRESENTATION / 스테이지·UI' '아트를 조정할 수 있는 Unity View 도구' '카메라 투영·배경 재사용·HUD 원본 비율을 고려하고 Inspector에서 조절할 수 있게 했습니다.' $false $cl
 Head $s '무한 스테이지 배경' .75 2.25 6.94
 Body $s "배경 깊이를 보정해 캐릭터와 줌 배율을 맞췄습니다.`r카메라별 SpriteRenderer 3개를 고정 재사용하고,`r화면 밖 타일을 반대편으로 이동합니다." .75 2.75 6.94 .93 15.5
 Body $s "큰 줌아웃은 Tiled 범위 확장, 무제한 객체 생성 방지.`r시각 배경과 Quantum 충돌 지형의 책임을 분리했습니다." .75 3.77 6.94 .59 15
 Head $s '이미지 기반 HUD' .75 4.48 6.94
 Body $s "체력 배경·채움, Round1~5·Fight·Win/Lose와 승리 표시.`rInspector에서 Sprite·크기·미리보기·Undo를 지원하고,`r원본 종횡비와 이미지의 상대 크기를 유지했습니다." .75 4.97 6.94 .93 15.5
 Head $s '체력바 Sprite 반전' .75 6.02 6.94
 Body $s "감소 방향·Scale·fillOrigin은 유지했습니다.`r전체 Sprite atlas UV의 X만 반사했습니다." .75 6.45 6.94 .53 14.5
 Label $s '작업별 실행 결과 · 합산하지 않음' 8.24 2.25 4.34
 Body $s "배경·Photon 30 PASS  /  HUD 39 PASS`r이미지 비율 22 PASS  /  UV 28 PASS" 8.24 2.69 4.34 .74 14.5
 CaptureLabel $s 'HUD / INSPECTOR CAPTURE' '실제 HUD 또는 Sprite·크기 설정 Inspector를 넣어 주세요.' 8.24 4.0 4.34 2.441
 Note $s '배경 30개는 배경 29+Photon 공중 1이다. UI 작업은 로컬 미커밋 상태. 이미지 배치 영역에는 실제 HUD가 보이는 GameView 또는 Inspector를 권장한다. Inspector 전체가 길다면 관련 Sprite·비율·반전 설정만 캡처한다.'

 # 13 — AI workflow, native environment table and repository knowledge.
 $s=Std '06  AI NATIVE / 개발 환경' '제약과 근거를 공유하는 AI 개발 환경' 'AI가 프로젝트 제약을 이해하고 검증 가능한 결과를 만들도록 작업 지식을 파일로 유지했습니다.'
 Table $s .75 2.25 @(2.2,4.35) @(.42,.47,.47,.47,.47,.47,.47,.47) @(@('구성 요소','프로젝트에서의 활용'),@('Codex CLI','코드 분석·구현·리팩터링 협업'),@('Unity MCP','Editor·Console 조사·테스트 실행'),@('AGENTS.md','프로젝트 규칙·기술 제약 관리'),@('Skills','작업별 절차·검증 기준 표준화'),@('SessionStart Hook','시작·재개 시 규칙·도구 진입점 안내'),@('Python Guard','Simulation 위험 패턴 정적 검사'),@('Git / GitHub','변경 관리·검토·게시 절차')) 14.5
 Head $s '프로젝트 지식의 외부화' 7.82 2.25 4.76
 $knowledge=@(@('AGENTS.md','개발 제약·공통 규칙'),@('docs/ai/current-state.md','현재 상태·미완료 작업'),@('docs/ai/development-workflow.md','구현·검증 절차'),@('docs/ai/evidence/','실행 결과·검증 근거'))
 for($i=0;$i -lt 4;$i++){$yy=2.91+$i*.83;Txt $s $knowledge[$i][0] 7.82 $yy 4.76 .35 14.5 $C.Blue $true;Body $s $knowledge[$i][1] 7.82 ($yy+.38) 4.76 .30 16}
 Line $s .75 6.5 11.83
 Body $s '새 세션에서도 최신 문서를 실제 Git·소스와 대조해 작업을 이어갑니다. 같은 설명을 반복하는 부담을 줄였습니다.' .75 6.7 11.83 .35 14.5
 Note $s '이 구조는 파일 기반 지식과 인계다. 문서 존재만으로 모든 실행 환경에서 자동 강제가 된다고 주장하지 않는다. Astra는 이전 포트폴리오 재구성의 모델 사용 이력이며 개별 코드 작성 비율을 의미하지 않는다.'

 # 14 — five Skills, session hook and guard are distinct.
 $s=Std '06  AI NATIVE / Skills·Hooks' '프로젝트 전용 절차와 자동 진입점' 'Skill은 작업 절차, Hook은 이벤트 진입점, 테스트는 실제 동작 검증을 담당합니다.'
 Table $s .75 2.25 @(3.05,3.0) @(.44,.61,.61,.61,.61,.61) @(@('Skill','주요 책임'),@('lsdf-game-development','기능·버그·UI·밸런스'),@('lsdf-quantum-refactor','상태 소유권·구조 리팩터링'),@('lsdf-quantum-verify','CodeGen·컴파일·재생 검증'),@('lsdf-portfolio-evidence','기술 성과·검증 근거 정리'),@('publish-unity-pr','Git 변경 검토·게시 준비')) 14
 Label $s '현재 등록: SessionStart' 7.29 2.25 5.29
 Body $s "시작·재개·압축 시 프로젝트 루트를 찾고`r공통 지침·최신 상태·검사기 진입점을 안내합니다.`r실제 코드와 최신 문서를 대조해 재개합니다." 7.29 2.7 5.29 1.14 15.5
 Head $s 'quantum_guard.py' 7.29 4.16 5.29
 Lines $s @('PlayerPrefs 등 로컬 데이터 직접 접근','FP → float·비결정론 연산 패턴','판정 영역의 부적절한 Unity API','시간·난수·스레딩·공유 가변 상태') 7.29 4.68 5.29 .42 15
 Line $s .75 6.35 11.83
 Body $s '5개 Skill로 개발 작업과 검증 기준을 표준화했습니다. 정적 패턴 검사 통과는 의미적 결정론 증명이 아닙니다.' .75 6.58 11.83 .45 15
 Note $s '현재 .codex/hooks.json에는 SessionStart만 등록돼 있으며 Stop Hook과 Git commit hook·서버 CI를 설치한 근거는 없다. Hook은 정적 검사기를 매 종료 시 자동 실행하는 구조가 아니다. 사용자 규칙에 따라 작업당 최종 검증 한 차례다.'

 # 15 — evidence table and publication process, limited claim.
 $s=Std '06  AI NATIVE / 검증·게시' '실행 결과와 후보 변경을 연결하는 절차' '요구사항 → 조사 → 구현 → CodeGen/컴파일 → 정적 검사 → Unity 테스트 → 기록 → 승인 범위의 Git 게시'
 Table $s .75 2.25 @(3.85,1.63) @(.43,.49,.49,.49,.49,.49,.49) @(@('대표 검증 범위','실행 결과'),@('선입력·매치','157 PASS'),@('로드아웃·체력 연출','235 PASS'),@('자연 재타격 낙하','106 PASS'),@('공중 피격 밀림','99 PASS'),@('Sprite pivot','66 PASS'),@('배경·Photon 후속','30 PASS')) 14.5
 Body $s '서로 다른 시점의 실행 결과입니다. 중복 시나리오가 있으므로 합산하지 않습니다.' .75 6.02 5.48 .61 14
 Label $s 'Unity MCP' 6.76 2.25 5.82
 Body $s 'refresh·컴파일·Console 오류/경고 확인, Scene·Asset·Animator 참조 분석, CodeGen·Unity 테스트 결과 수집, Photon 연결·Frame 복원을 수행했습니다.' 6.76 2.68 5.82 1.05 15.5
 Head $s 'GitHub 게시 후보 검토' 6.76 4.0 5.82
 Lines $s @('기존 저장소의 브랜치·변경 확인','관련 파일만 staging, 비밀·대형 파일·이력 검사','테스트 후보와 커밋 대상 tree 일치 확인','publish-unity-pr · 승인 범위의 Commit/Push') 6.76 4.5 5.82 .4 14.5
 Body $s "최근 게시에서 후보·커밋 tree의 일치와`r원격 SHA 일치를 확인했습니다." 6.76 6.15 5.82 .58 14
 Body $s '실패 후 수정해도 재실행하지 않았다면 미검증으로 기록합니다. 미실행을 PASS로 바꾸지 않습니다.' .75 6.81 11.83 .24 13
 Note $s '수치는 원천 자료의 역사적 실행 결과다. 최근 게시에서는 사용자 직접 테스트 확인을 별도 근거로 사용했고 자동 공백 실패·보정 후 미검증 이력은 유지했다. 모든 자동 게이트가 항상 PASS 후 게시된다는 의미가 아니다. 후보 tree와 commit tree가 일치하며 remote SHA가 해당 commit과 일치하는 구분이다. 실제 Draft PR·Actions CI 설치 완료는 주장하지 않는다.'

 # 16 — dark conclusion with skills and explicit next work.
 $s=Std '07  OUTCOME / 성과·다음 과제' '게임 개발 지식과 AI 운영을 함께 확장' '문제를 정의하고 기술적 제약을 전달하며, AI의 결과를 검증할 수 있는 판단력을 쌓았습니다.' $true
 $capabilities=@(@('결정론적 시스템 설계','로컬·공유 설정·Frame 분리, 기존 공격 보존'),@('게임플레이 문제 해결','입력·매치·공중·전이·표현의 원인 분석'),@('AI 개발 환경 구축','AGENTS·Skills·Hook·MCP·Guard와 인계'),@('검증·변경 이력 관리','실행 근거·한계 기록, Git 후보·상태 추적'))
 for($i=0;$i -lt 4;$i++){$yy=2.25+$i*.92;Txt $s ('0'+($i+1)) .75 $yy .55 .35 20 $C.Blue $true;Head $s $capabilities[$i][0] 1.48 $yy 5.3 $C.White;Body $s $capabilities[$i][1] 1.48 ($yy+.44) 5.3 .40 14.5 $C.Pale}
 VLine $s 7.13 2.25 3.65 $C.DLine
 Label $s '남은 개선 과제' 7.63 2.25 4.95
 Lines $s @('일부 공격 이벤트·미검증 테스트 정리','강제 지연·지터 환경의 rollback 검사','독립 빌드·다중 기기·배포 검증','GitHub Actions CI·자동 PR 확장','동일 조건 성능 측정·효과 정량화') 7.63 2.84 4.95 .53 15.5 $C.Pale
 Line $s .75 6.18 11.83 $C.DLine
 Txt $s 'AI가 안정적으로 개발할 수 있는 환경을 설계하는 게임 개발자' .75 6.43 11.83 .43 23 $C.White $true
 Note $s '미완료 범위에는 KazumiHoming clip/graph의 빈 공격 이벤트, 일부 fixture 보정 후 미검증, 강제 지터·독립 빌드·교차 플랫폼·성능 미측정이 있다. 사용자가 제시한 향후 CI/PR 방향은 계획이며 완료 성과가 아니다.'

 foreach($design in $deck.Designs){try{$scheme=$design.SlideMaster.Theme.ThemeFontScheme;foreach($fi in 1..6){$scheme.MajorFont.Item($fi).Name=$Font;$scheme.MinorFont.Item($fi).Name=$Font}}catch{}}
 $pptx=Join-Path $out '김석진_LSDF_AI_Native_포트폴리오_16장_v3.pptx'
 $pdf=Join-Path $out '김석진_LSDF_AI_Native_포트폴리오_16장_v3.pdf'
 $deck.SaveAs($pptx,24,-1);$deck.SaveAs($pdf,32)
 foreach($sl in $deck.Slides){
  $sl.Export((Join-Path $render ('slide-{0:00}.png' -f $sl.SlideIndex)),'PNG',1600,900)
  foreach($sh in $sl.Shapes){
   if($sh.HasTextFrame -eq -1 -and $sh.TextFrame.HasText -eq -1){$tr=$sh.TextFrame.TextRange;$Metrics.Add([pscustomobject]@{slide=$sl.SlideIndex;name=$sh.Name;text=$tr.Text;x=[double]$sh.Left;y=[double]$sh.Top;width=[double]$sh.Width;height=[double]$sh.Height;boundWidth=[double]$tr.BoundWidth;boundHeight=[double]$tr.BoundHeight;boundLeft=[double]$tr.BoundLeft;boundTop=[double]$tr.BoundTop;font=$tr.Font.Name;size=[double]$tr.Font.Size;placeholder=($sh.Type -eq 14)})}
   if($sh.HasTable -eq -1){for($r=1;$r -le $sh.Table.Rows.Count;$r++){for($c=1;$c -le $sh.Table.Columns.Count;$c++){$cs=$sh.Table.Cell($r,$c).Shape;$tr=$cs.TextFrame.TextRange;$Metrics.Add([pscustomobject]@{slide=$sl.SlideIndex;name=('table-'+$r+'-'+$c);text=$tr.Text;x=[double]$cs.Left;y=[double]$cs.Top;width=[double]$cs.Width-18;height=[double]$cs.Height-9;boundWidth=[double]$tr.BoundWidth;boundHeight=[double]$tr.BoundHeight;boundLeft=[double]$tr.BoundLeft;boundTop=[double]$tr.BoundTop;font=$tr.Font.Name;size=[double]$tr.Font.Size;placeholder=$false})}}}
  }
 }
 $Metrics|ConvertTo-Json -Depth 5|Set-Content -Encoding UTF8 (Join-Path $build 'text-metrics.json')
 $CaptureInfo|ConvertTo-Json -Depth 5|Set-Content -Encoding UTF8 (Join-Path $build 'captures.json')
 [pscustomobject]@{slides=$deck.Slides.Count;pptx=$pptx;pdf=$pdf;sourceParagraphs=$SourceSlides.Count;nativeTables=@(6,9,13,14,15);captureSlides=@(3,8,11,12);method='Microsoft PowerPoint COM';font=$Font;dimensionsInches=@(13.333,7.5)}|ConvertTo-Json -Depth 5|Set-Content -Encoding UTF8 (Join-Path $build 'manifest.json')
 Write-Output ('Created '+$deck.Slides.Count+' slides, PPTX/PDF and PowerPoint renders.')
}catch{Write-Output ('Stopped at slide '+$deck.Slides.Count);Write-Output $_.ScriptStackTrace;throw}
finally{
 if($deck){$deck.Close()}
 if($app){$app.DisplayAlerts=$oldAlerts;if(-not $hadApp -and $app.Presentations.Count -eq 0){$app.Quit()}}
 if($deck){[void][Runtime.InteropServices.Marshal]::ReleaseComObject($deck)}
 if($app){[void][Runtime.InteropServices.Marshal]::ReleaseComObject($app)}
 [GC]::Collect()
}
