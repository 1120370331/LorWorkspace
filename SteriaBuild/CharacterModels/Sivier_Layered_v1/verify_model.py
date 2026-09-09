from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT.parent/'shared'))
from layered_model import ModelProject
from verification import verify
if __name__=='__main__':verify(ModelProject(ROOT))
