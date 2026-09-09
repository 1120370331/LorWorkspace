"""Shared offline layered-character renderer, grounding and projection export."""
import os
import argparse
import base64
import hashlib
import json
import math
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageOps

def rotate_point(point, center, degrees):
    a = math.radians(degrees)
    x, y = (point[0] - center[0], point[1] - center[1])
    return [center[0] + x * math.cos(a) - y * math.sin(a), center[1] + x * math.sin(a) + y * math.cos(a)]

class ModelProject:

    def __init__(self, root):
        self.root = Path(root).resolve()

    def load_model(self):
        model = json.loads((self.root / 'model.json').read_text(encoding='utf-8'))
        prepared = model.get('prepared_from')
        if prepared and hashlib.sha256((self.root/prepared['path']).read_bytes()).hexdigest()!=prepared['sha256']:
            raise ValueError('Source layout changed; run prepare_model.py before building.')
        if model['schema_version'] != 1:
            raise ValueError('Unsupported model schema')
        ids = [p['id'] for p in model['parts']]
        if len(ids) != len(set(ids)):
            raise ValueError('Duplicate part IDs')
        for p in model['parts']:
            path = (self.root / p['path']).resolve()
            if not path.is_relative_to(self.root) or not path.is_file():
                raise ValueError(f'Invalid source: {p['id']}')
            with Image.open(path) as im:
                if im.mode != 'RGBA' or im.getchannel('A').getextrema()[0] != 0:
                    raise ValueError(f'Source must have actual alpha: {path}')
        for name, target in model['aliases'].items():
            if target not in model['poses'] or name in model['poses']:
                raise ValueError(f'Invalid alias: {name}')
        return model

    def resolved_parts(self, model, action, preset=None):
        pose = model['poses'][model['aliases'].get(action, action)]
        head = model['head_presets'][preset or model.get('default_head', 'velia')]
        result = []
        for original in sorted(model['parts'], key=lambda p: p['z']):
            if original['id'] in head.get('hide', []):
                continue
            part = {**original, **model['head']['expressions'][pose['expression']].get(original['id'], {}), **head.get('overrides', {}).get(original['id'], {}), **pose['overrides'].get(original['id'], {})}
            at = part['at']
            angle = part.get('angle', 0)
            if part['domain'] == 'head':
                if 'neck_at' in pose:
                    at = rotate_point(at, [0, 0], pose.get('head_angle', 0))
                    at = [at[0] + pose['neck_at'][0], at[1] + pose['neck_at'][1]]
                else:
                    at = rotate_point(at, model['head']['anchor'], pose.get('head_angle', 0))
                angle += pose.get('head_angle', 0)
            part['at'] = rotate_point(at, model['canvas']['root'], pose.get('body_angle', 0))
            part['angle'] = angle + pose.get('body_angle', 0)
            result.append(part)
        return result

    def render_part(self, model, part, samples=3):
        source = Image.open(self.root / part['path']).convert('RGBA')
        if part.get('tint'):
            tint = Image.new('RGBA', source.size, model['head']['color'][part['tint']])
            alpha = source.getchannel('A')
            source = ImageChops.multiply(source, tint)
            source.putalpha(alpha)
        source = source.resize((max(1, round(part['size'][0] * samples)), max(1, round(part['size'][1] * samples))), Image.Resampling.LANCZOS)
        scale_x, scale_y = (part['size'][0] / source.width, part['size'][1] / source.height)
        a = math.radians(part['angle'])
        co, si = (math.cos(a), math.sin(a))
        x, y = part['at']
        px, py = (part['pivot'][0] * source.width, part['pivot'][1] * source.height)
        transform = (co / scale_x, si / scale_x, px - (co * x + si * y) / scale_x, -si / scale_y, co / scale_y, py + (si * x - co * y) / scale_y)
        transform = (transform[0] / samples, transform[1] / samples, transform[2], transform[3] / samples, transform[4] / samples, transform[5])
        return source.transform((model['canvas']['width'] * samples, model['canvas']['height'] * samples), Image.Transform.AFFINE, transform, Image.Resampling.BICUBIC)

    def render(self, model, action, preset=None, domain=None, plane=None, part_ids=None):
        samples = 3
        size = (model['canvas']['width'], model['canvas']['height'])
        canvas = Image.new('RGBA', (size[0] * samples, size[1] * samples))
        for part in self.resolved_parts(model, action, preset):
            if (domain is None or part['domain'] == domain) and (plane is None or part.get('plane', 'main') == plane) and (part_ids is None or part['id'] in part_ids):
                canvas.alpha_composite(self.render_part(model, part, samples))
        return canvas.resize(size, Image.Resampling.LANCZOS)

    def ground_contact(self, model, action):
        """Measure the support/sole edge, excluding heads, weapons and effects."""
        canonical = model['aliases'].get(action, action)
        config = model['grounding']
        body = self.render(model, action, part_ids=set(model['wearing']['body']))
        top, bottom = config['sole_band_y']
        mask = body.getchannel('A').crop((0, top, body.width, bottom))
        bounds = mask.point(lambda a: 255 if a > config['alpha_threshold'] else 0).getbbox()
        if not bounds:
            raise ValueError(f'No sole/support pixels in the contact band: {action}')
        special = config['exceptions'].get(canonical)
        if special and (not special.get('reason')):
            raise ValueError(f'Ground-height exception needs its design reason: {action}')
        offset = special['offset_world_y'] if special else 0
        target = config['target_sole_root_y'] + offset
        contact_y = top + bounds[3]
        origin_y = contact_y + target * model['canvas']['pixels_per_unit']
        return {'contact_canvas_y': contact_y, 'target_root_y': target, 'origin_canvas_y': origin_y, 'designed_offset': offset}

    def projection_xml(self, model, target):
        root = ET.Element('ModInfo')
        cloth = ET.SubElement(root, 'ClothInfo')
        ET.SubElement(cloth, 'Name').text = model.get('projection_skin', model['id'] + '_Projection')
        cw, ch = (model['canvas']['width'], model['canvas']['height'])
        rx = model['canvas']['root'][0]
        px = 1024 * rx / cw - 512
        grounding = {}
        for action in list(model['poses']) + list(model['aliases']):
            grounding[action] = self.ground_contact(model, action)
            ry = grounding[action]['origin_canvas_y']
            py = 1024 * (ch - ry) / ch - 512
            node = ET.SubElement(cloth, action)
            ET.SubElement(node, 'Direction').text = 'Front'
            ET.SubElement(node, 'Pivot', pivot_x=f'{px:g}', pivot_y=f'{py:g}')
            pose = model['poses'][model['aliases'].get(action, action)]
            if 'neck_at' in pose:
                offset = rotate_point(model['head']['runtime_origin_from_neck'], [0, 0], pose.get('head_angle', 0))
                anchor = [pose['neck_at'][0] + offset[0], pose['neck_at'][1] + offset[1]]
            else:
                anchor = rotate_point(model['head']['anchor'], [rx, ry], pose['body_angle'])
            factor = 100 / model['canvas']['pixels_per_unit']
            ET.SubElement(node, 'Head', head_x=f'{(anchor[0] - rx) * factor:.4f}', head_y=f'{(ry - anchor[1]) * factor:.4f}', rotation=f'{-pose.get('body_angle', 0) - pose.get('head_angle', 0):g}', head_enable='True')
        ET.indent(root)
        ET.ElementTree(root).write(target, encoding='utf-8', xml_declaration=True)
        (target.parent / 'grounding.json').write_text(json.dumps(grounding, indent=2) + '\n', encoding='utf-8')

    def contact_sheet(self, model, out):
        sheet = Image.new('RGB', (1200, 1000), '#e6e5e0')
        draw = ImageDraw.Draw(sheet)
        font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 19)
        for i, action in enumerate(model['poses']):
            tile = self.render(model, action).crop((48,90,464,420)).resize((380,301),Image.Resampling.LANCZOS)
            x, y = (i % 3 * 400 + 10, i // 3 * 326 + 9)
            sheet.paste(tile, (x, y), tile)
            draw.text((x, y + 300), action, font=font, fill='#353e43')
        sheet.save(out / 'poses.png')
        comparison = Image.new('RGB', (1200, 450), '#e6e5e0')
        draw = ImageDraw.Draw(comparison)
        previous = self.root.parents[1] / ('SteriaModFolder/Resource/CharacterSkin/' + model.get('character', 'Velia') + '/ClothCustom/Default.png')
        items = [('Current skin', Image.open(previous).convert('RGBA')), (model.get('display_name', 'Layered model'), self.render(model, 'Default')), ('Same wearing / native head', self.render(model, 'Default', 'librarian')), ('Projection texture', self.render(model, 'Default', domain='wearing'))]
        for i, (label, sprite) in enumerate(items):
            tile = sprite.crop((120, 140, 405, 415)).resize((285, 275))
            comparison.paste(tile, (i * 300, 85), tile)
            draw.text((i * 300 + 12, 30), label, font=font, fill='#353e43')
        comparison.save(out / 'comparison.png')
        detail = self.render(model, 'Default').crop((208, 155, 306, 263)).resize((392, 432))
        detail_bg = Image.new('RGBA', detail.size, '#a2aaa6')
        detail_bg.alpha_composite(detail)
        detail_bg.save(out / 'head_detail.png')
        anchors = Image.new('RGB', (1200, 1000), '#e6e5e0')
        for i, (action, pose) in enumerate(model['poses'].items()):
            tile = self.render(model, action)
            d = ImageDraw.Draw(tile)
            d.line([(100, 390), (410, 390)], fill='#8a9790', width=1)
            for side in ['l', 'r']:
                pts = [pose['joints'][k + '_' + side] for k in ['shoulder', 'elbow', 'wrist']]
                d.line([tuple(p) for p in pts], fill='#4b9386', width=1)
            for (px, py), color in [(model['canvas']['root'], '#ba6455'), (pose['neck_at'], '#1d8fbc')]:
                d.ellipse((px - 3, py - 3, px + 3, py + 3), outline=color, width=2)
            tile = tile.crop((48,90,464,420)).resize((380,301),Image.Resampling.LANCZOS)
            x, y = (i % 3 * 400 + 10, i // 3 * 326 + 9)
            anchors.paste(tile, (x, y), tile)
            ImageDraw.Draw(anchors).text((x, y + 300), action, font=font, fill='#353e43')
        anchors.save(out / 'anchors.png')

    def write_preview(self, model, out):
        data = dict(model)
        data['resolved'] = {preset: {action: self.resolved_parts(model, action, preset) for action in model['poses']} for preset in model['head_presets']}
        paths = {p['path'] for variants in data['resolved'].values() for parts in variants.values() for p in parts}
        data['images'] = {p: 'data:image/png;base64,' + base64.b64encode((self.root / p).read_bytes()).decode() for p in sorted(paths)}
        template_path = self.root / 'preview_template.html'
        if not template_path.exists(): template_path = Path(__file__).parent / 'preview_template.html'
        template = template_path.read_text(encoding='utf-8').replace('__TITLE__', model.get('display_name', model['id']))
        (out / 'preview.html').write_text(template.replace('__MODEL_DATA__', json.dumps(data, ensure_ascii=False)), encoding='utf-8')

    def build(self, out):
        model = self.load_model()
        out.mkdir(parents=True, exist_ok=True)
        poses = out / 'assembled'
        projection = out / model.get('projection_skin', model['id'] + '_Projection')
        cloth = projection / 'ClothCustom'
        poses.mkdir(exist_ok=True)
        cloth.mkdir(parents=True, exist_ok=True)
        for action in list(model['poses']) + list(model['aliases']):
            self.render(model, action).save(poses / (action + '.png'))
            self.render(model, action, domain='wearing', plane='main').save(cloth / (action + '.png'))
            self.render(model, action, domain='wearing', plane='front').save(cloth / (action + '_front.png'))
        self.projection_xml(model, projection / 'ModInfo.Xml')
        self.contact_sheet(model, out)
        self.write_preview(model, out)
        paths = sorted(self.root.glob('*.json')) + sorted(self.root.glob('*.py'))
        paths += sorted(p for p in (self.root / 'sources').rglob('*') if p.is_file() and p.suffix in ['.png','.json'])
        if (self.root / 'preview_template.html').exists(): paths.append(self.root / 'preview_template.html')
        paths += sorted(Path(__file__).parent.glob('*.py')) + [Path(__file__).parent / 'preview_template.html']
        hashes = {Path(os.path.relpath(p, self.root)).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
        identity = hashlib.sha256(json.dumps(hashes, sort_keys=True).encode()).hexdigest()
        (out / 'build_manifest.json').write_text(json.dumps({'candidate': identity, 'sources': hashes, 'status': 'authoring_prototype_not_deployed', 'actions': list(model['poses']), 'aliases': model['aliases']}, indent=2) + '\n', encoding='utf-8')
        print(f'Built {len(model['poses'])} poses + {len(model['aliases'])} explicit aliases: {out}')
        print(f'Candidate: {identity}')
