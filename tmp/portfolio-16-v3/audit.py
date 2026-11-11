from pathlib import Path
import hashlib,json,re,zipfile,xml.etree.ElementTree as ET
import fitz
from PIL import Image,ImageChops,ImageStat

ROOT=Path(__file__).resolve().parents[2]
BUILD=ROOT/'tmp/portfolio-16-v3'
OUT=ROOT/'output/portfolio'
PPTX=OUT/'김석진_LSDF_AI_Native_포트폴리오_16장_v3.pptx'
PDF=PPTX.with_suffix('.pdf')
NS={'p':'http://schemas.openxmlformats.org/presentationml/2006/main','a':'http://schemas.openxmlformats.org/drawingml/2006/main'}
report={'font_policy':{'basis':'user_request','families':['Malgun Gothic']},'scope':'Presentation artifacts only; no game tests'}
source=(BUILD/'source.md').read_text(encoding='utf-8')
originals=re.findall(r'(?ms)^### Slide (\d{2})\.(.*?)(?=^### Slide \d{2}\.|\Z)',source)
norm=lambda s: re.sub(r'\s+','',s)
with zipfile.ZipFile(PPTX) as z:
 report['zip_integrity']=z.testzip() is None
 pres=ET.fromstring(z.read('ppt/presentation.xml'))
 report['size_emu']=pres.find('p:sldSz',NS).attrib
 report['embedded_fonts']=len(pres.findall('.//p:embeddedFont',NS))
 names=sorted([n for n in z.namelist() if re.fullmatch(r'ppt/slides/slide\d+\.xml',n)],key=lambda n:int(re.search(r'(\d+)\.xml',n).group(1)))
 report['slide_count']=len(names)
 fonts=set();headers={};tables=[];captures=[];notes_coverage=[];texts=[];shapes=0;images=0;geometry=[];capture_geometry=[]
 for i,n in enumerate(names,1):
  e=ET.fromstring(z.read(n))
  texts.append(''.join(x.text or '' for x in e.findall('.//a:t',NS)))
  shapes+=len(e.findall('.//p:sp',NS));images+=len(e.findall('.//p:pic',NS))
  if e.findall('.//a:tbl',NS):tables.append(i)
  if e.findall('.//p:ph[@type="pic"]',NS):captures.append(i)
  for sh in e.find('p:cSld/p:spTree',NS):
   xf=sh.find('p:spPr/a:xfrm',NS)
   if xf is None:xf=sh.find('p:xfrm',NS)
   if xf is None:continue
   off=xf.find('a:off',NS);ext=xf.find('a:ext',NS)
   if off is None or ext is None:continue
   g={'slide':i,**{k:int(v) for k,v in off.attrib.items()},**{k:int(v) for k,v in ext.attrib.items()}}
   geometry.append(g)
   if sh.find('.//p:ph[@type="pic"]',NS) is not None:capture_geometry.append(g)
  for tag in ('latin','ea','cs'):
   fonts.update(x.get('typeface') for x in e.findall('.//a:'+tag,NS))
  for sh in e.findall('.//p:sp',NS):
   prop=sh.find('p:nvSpPr/p:cNvPr',NS)
   if prop is None or not prop.get('name','').startswith('standard-'):continue
   xf=sh.find('p:spPr/a:xfrm',NS)
   headers.setdefault(prop.get('name'),[]).append({'slide':i,**xf.find('a:off',NS).attrib,**xf.find('a:ext',NS).attrib})
  note=ET.fromstring(z.read(f'ppt/notesSlides/notesSlide{i}.xml'))
  note_text=''.join(x.text or '' for x in note.findall('.//a:t',NS))
  notes_coverage.append({'slide':i,'full_user_source_retained':norm(originals[i-1][1].strip()) in norm(note_text)})
 report.update(text_fonts=sorted(fonts),single_font=fonts<={'Malgun Gothic'},native_table_slides=tables,picture_placeholder_slides=captures,native_text_shapes=shapes,original_art_images=images,source_notes=notes_coverage)
 report['fixed_headers']=all(len({(x['x'],x['y'],x['cx'],x['cy']) for x in v})==1 and len(v)==15 for v in headers.values())
 report['header_geometry']=headers
 report['off_slide_shapes']=[g for g in geometry if min(g['x'],g['y']) < -1000 or g['x']+g['cx']>12192000 or g['y']+g['cy']>6859000]
 report['capture_geometry']=capture_geometry
 capture_targets=json.loads((BUILD/'captures.json').read_text(encoding='utf-8-sig'))
 report['capture_geometry_matches']=len(capture_geometry)==4 and all(all(abs(g[k]-round(c[t]*914400))<100 for k,t in [('x','x'),('y','y'),('cx','width'),('cy','height')]) for g,c in zip(capture_geometry,capture_targets))
 expected={'standard-chapter':(.75,.40,11.83,.25),'standard-title':(.75,.82,11.83,.55),'standard-subtitle':(.75,1.48,11.83,.36)}
 report['specified_headers']=all(all(abs(int(g[k])-round(v*914400))<100 for k,v in zip(['x','y','cx','cy'],expected[name])) for name,geos in headers.items() for g in geos)
 report['no_personal_github_url']=not any('github.com/' in t.lower() for t in texts)
 report['failure_and_limits_visible']=all(x in texts[8] for x in ['109 PASS / 1 FAIL','미검증']) and '합산하지' in texts[14]
 report['required_table_slides']=all(i in tables for i in [6,9,13,14,15])
 report['required_capture_slides']=captures==[3,8,11,12]
 report['full_user_content_in_notes']=len(originals)==16 and all(n['full_user_source_retained'] for n in notes_coverage)
 report['slide_texts']=texts
metrics=json.loads((BUILD/'text-metrics.json').read_text(encoding='utf-8-sig'))
report['text_fit_suspects']=[m for m in metrics if not m.get('placeholder') and (m['boundWidth']>m['width']+1.8 or m['boundHeight']>m['height']+1.8)]
report['off_slide_text']=[m for m in metrics if m['x']<-.5 or m['y']<-.5 or m['x']+m['width']>960.2 or m['y']+m['height']>540.5]
pdfdir=BUILD/'pdf';pdfdir.mkdir(exist_ok=True)
pages=[]
with fitz.open(PDF) as doc:
 report['pdf_page_count']=len(doc)
 for i,page in enumerate(doc,1):
  fs=page.get_fonts(full=True)
  text=page.get_text()
  d={'page':i,'size':list(page.rect),'fonts':sorted(set(f[3] for f in fs)),'extensions':sorted(set(f[1] for f in fs)),'replacement_glyphs':text.count('\ufffd'),'korean_characters':len(re.findall('[가-힣]',text))}
  pix=page.get_pixmap(matrix=fitz.Matrix(1600/page.rect.width,1600/page.rect.width),alpha=False)
  dest=pdfdir/f'page-{i:02}.png';pix.save(dest)
  with Image.open(dest) as a,Image.open(BUILD/'powerpoint'/f'slide-{i:02}.png') as b:
   b=b.convert('RGB').resize(a.size)
   d['ppt_pdf_mean_pixel_difference']=round(sum(ImageStat.Stat(ImageChops.difference(a.convert('RGB'),b)).mean)/3,3)
  pages.append(d)
report['pdf_pages']=pages
report['pdf_font_names']=all(all('MalgunGothic' in n.replace(' ','') for n in p['fonts']) for p in pages)
report['pdf_embedded_fonts']=all(p['extensions']==['ttf'] for p in pages)
report['pdf_korean']=all(p['korean_characters']>8 and p['replacement_glyphs']==0 for p in pages)
report['sha256']={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in [PPTX,PDF]}
report['checks']={'sixteen_slides':report['slide_count']==16 and report['pdf_page_count']==16,'widescreen':abs(int(report['size_emu']['cx'])/914400-13.333)<.001 and int(report['size_emu']['cy'])==6858000 and all(abs(p['size'][2]/p['size'][3]-16/9)<.001 for p in pages),'source_complete':report['full_user_content_in_notes'],'headers':report['fixed_headers'] and report['specified_headers'],'font':report['single_font'] and report['pdf_font_names'] and report['pdf_embedded_fonts'],'korean':report['pdf_korean'],'editable_tables':report['required_table_slides'],'capture_placeholders':report['required_capture_slides'] and report['capture_geometry_matches'],'package':report['zip_integrity'],'visible_limits':report['failure_and_limits_visible'],'text_fit':not report['text_fit_suspects'],'onslide':not report['off_slide_text'] and not report['off_slide_shapes']}
(BUILD/'artifact-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k in ['checks','size_emu','embedded_fonts','native_table_slides','picture_placeholder_slides','native_text_shapes','original_art_images','text_fonts','slide_count','pdf_page_count']},ensure_ascii=False,indent=2))
print('FIT_SUSPECTS '+json.dumps([{'slide':m['slide'],'name':m['name'],'text':m['text'],'height':m['height'],'boundHeight':m['boundHeight'],'width':m['width'],'boundWidth':m['boundWidth']} for m in report['text_fit_suspects']],ensure_ascii=False))
