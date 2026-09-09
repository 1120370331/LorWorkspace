"""Velia project entry point; render math is shared across character models."""
import argparse
import sys
from pathlib import Path
ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT.parent / 'shared'))
from layered_model import ModelProject, rotate_point
project = ModelProject(ROOT)
load_model = project.load_model
resolved_parts = project.resolved_parts
render_part = project.render_part
render = project.render
ground_contact = project.ground_contact
projection_xml = project.projection_xml
contact_sheet = project.contact_sheet
write_preview = project.write_preview
build = project.build
if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out',type=Path,default=ROOT/'build')
    build(parser.parse_args().out.resolve())
