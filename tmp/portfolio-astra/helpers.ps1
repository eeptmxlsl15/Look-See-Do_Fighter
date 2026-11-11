$ErrorActionPreference = 'Stop'
$root = 'C:\Users\Admin\Look-See-Do_Fighter'
$build = Join-Path $root 'tmp\portfolio-astra'
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
  T $s '김석진' .75 7.12 8 .22 11 $footerColor
  T $s ('{0:00}' -f $s.SlideIndex) 12.05 7.12 .53 .22 11 $footerColor $false 3
}
function Standard($chapter,$title,$subtitle,$dark=$false) {
  $s=$deck.Slides.Add($deck.Slides.Count+1,12)
  $s.FollowMasterBackground=0;$s.Background.Fill.ForeColor.RGB=Col $(if($dark){$C.Black}else{$C.White})
  $ink=$(if($dark){$C.White}else{$C.Ink});$body=$(if($dark){$C.Pale}else{$C.Body});$line=$(if($dark){$C.DLine}else{$C.Line})
  T $s $chapter .75 .40 11.83 .25 12 $C.Blue $true 1 'standard-chapter'
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
