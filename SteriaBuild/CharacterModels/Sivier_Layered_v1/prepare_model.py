from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT.parent/'shared'))
from asset_preparation import prepare
if __name__=='__main__':prepare(ROOT)
