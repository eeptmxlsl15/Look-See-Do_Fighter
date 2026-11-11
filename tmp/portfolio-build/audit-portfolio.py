"""Read-only package/PDF/layout audit and PDF rasterization. No game checks."""
from pathlib import Path
import json
import re
import zipfile
import xml.etree.ElementTree as ET
import fitz
from PIL import Image, ImageChops, ImageStat

ROOT = Path(__file__).resolve().parents[2]
BUILD = ROOT / 'tmp' / 'portfolio-build'
OUT = ROOT / 'output' / 'portfolio'
PPTX = OUT / '김석진_LSDF_AI-Native_클라이언트_포트폴리오.pptx'
PDF = PPTX.with_suffix('.pdf')
NS = {'p': 'http://schemas.openxmlformats.org/presentationml/2006/main',
      'a': 'http://schemas.openxmlformats.org/drawingml/2006/main'}
report = {'scope': 'portfolio artifacts only; historical game checks were not rerun',
          'font_policy': {'basis': 'user_request', 'families': ['Malgun Gothic']}}
with zipfile.ZipFile(PPTX) as z:
    report['zip_integrity'] = z.testzip() is None
    p = ET.fromstring(z.read('ppt/presentation.xml'))
    size = p.find('p:sldSz', NS)
    report['pptx_size_emu'] = dict(size.attrib)
    report['embedded_fonts'] = len(p.findall('.//p:embeddedFont', NS))
    slides = sorted((n for n in z.namelist() if re.fullmatch(r'ppt/slides/slide\d+\.xml', n)),
                    key=lambda x: int(re.search(r'(\d+)\.xml', x).group(1)))
    report['slide_count'] = len(slides)
    fonts, headers, tables, images, total_shapes, unexpected = set(), {}, 0, 0, 0, []
    slide_texts = []
    for idx, name in enumerate(slides, 1):
        e = ET.fromstring(z.read(name))
        total_shapes += len(e.findall('.//p:sp', NS))
        images += len(e.findall('.//p:pic', NS))
        tables += len(e.findall('.//a:tbl', NS))
        slide_texts.append(''.join(t.text or '' for t in e.findall('.//a:t', NS)))
        for tag in ('latin', 'ea', 'cs'):
            for node in e.findall('.//a:' + tag, NS):
                fonts.add(node.get('typeface'))
        for sh in e.findall('.//p:sp', NS):
            prop = sh.find('p:nvSpPr/p:cNvPr', NS)
            if prop is None or not prop.get('name', '').startswith('standard-'):
                continue
            transform = sh.find('p:spPr/a:xfrm', NS)
            off = transform.find('a:off', NS).attrib
            ext = transform.find('a:ext', NS).attrib
            headers.setdefault(prop.get('name'), []).append({'slide': idx, **off, **ext})
    report['text_fonts'] = sorted(fonts)
    report['single_font_pass'] = fonts <= {'Malgun Gothic'}
    report['native_tables'] = tables
    report['native_text_shapes'] = total_shapes
    report['original_art_pictures'] = images
    report['header_geometry'] = headers
    report['fixed_headers_pass'] = all(len({(x['x'], x['y'], x['cx'], x['cy']) for x in values}) == 1
                                       for values in headers.values())
    report['github_text_absent'] = not any('github' in s.lower() for s in slide_texts)
    report['placeholder_text_absent'] = not any(re.search(r'(?i)lorem|TODO|TBD|placeholder', s) for s in slide_texts)

metrics = json.loads((BUILD / 'text-metrics.json').read_text(encoding='utf-8-sig'))
report['text_fit_suspects'] = [m for m in metrics if m['boundHeight'] > m['height'] + 1.8
                              or m['boundWidth'] > m['width'] + 1.8]
report['off_slide_text'] = [m for m in metrics if m['left'] < -0.5 or m['top'] < -0.5
                          or m['left'] + m['width'] > 960.2 or m['top'] + m['height'] > 540.5]
report['pdf_pages'] = []
pdf_dir = BUILD / 'pdf'
pdf_dir.mkdir(exist_ok=True)
with fitz.open(PDF) as doc:
    report['pdf_page_count'] = len(doc)
    for i, page in enumerate(doc):
        fonts = page.get_fonts(full=True)
        page_info = {'page': i + 1, 'size_pt': list(page.rect),
                     'font_names': sorted(set(f[3] for f in fonts)),
                     'font_extensions': sorted(set(f[1] for f in fonts)),
                     'korean_characters': len(re.findall('[가-힣]', page.get_text())),
                     'replacement_glyphs': page.get_text().count('\ufffd')}
        pix = page.get_pixmap(matrix=fitz.Matrix(1600 / page.rect.width, 1600 / page.rect.width), alpha=False)
        path = pdf_dir / f'page-{i+1:02}.png'
        pix.save(path)
        ppt_render = BUILD / 'powerpoint' / f'slide-{i+1:02}.png'
        with Image.open(path) as a, Image.open(ppt_render) as b:
            b = b.convert('RGB').resize(a.size)
            page_info['ppt_pdf_mean_abs_pixel_difference_0_255'] = round(
                sum(ImageStat.Stat(ImageChops.difference(a.convert('RGB'), b)).mean) / 3, 3)
        report['pdf_pages'].append(page_info)
report['pdf_fonts_embedded_pass'] = all(p['font_extensions'] == ['ttf'] for p in report['pdf_pages'])
report['pdf_font_names_pass'] = all(all('MalgunGothic' in n.replace(' ', '') for n in p['font_names'])
                                    for p in report['pdf_pages'])
report['pdf_korean_pass'] = all(p['korean_characters'] > 8 and p['replacement_glyphs'] == 0 for p in report['pdf_pages'])
(BUILD / 'artifact-audit.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k: v for k, v in report.items() if k not in ('header_geometry', 'pdf_pages', 'text_fit_suspects')}, ensure_ascii=True, indent=2))
print('Text fit suspect count:', len(report['text_fit_suspects']))
print('PDF font names:', sorted(set(n for p in report['pdf_pages'] for n in p['font_names'])))
print('PDF page dimensions:', report['pdf_pages'][0]['size_pt'])
