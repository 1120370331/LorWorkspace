"""Stage the verified AceTaffy release ZIP for the BaseMod Workshop uploader."""

import hashlib
import json
import sys
from pathlib import Path, PurePosixPath
from zipfile import ZipFile

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "Verification/release-manifest.json"
PREVIEW_SOURCE = ROOT / "Promo/taffy-default-display-square.png"
sys.path.insert(0, str(ROOT / "Verification"))
from verify_package import verify


def main() -> None:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    version = manifest["version"]
    archive_path = ROOT / "dist" / f"AceTaffy-{version}.zip"
    actual_hash = hashlib.sha256(archive_path.read_bytes()).hexdigest().upper()
    if actual_hash != manifest["zip_sha256"].upper():
        raise SystemExit("Release ZIP does not match the verified manifest")

    stage = Path(__file__).resolve().parent / f"v{version}" / "AceTaffyMod"
    if stage.exists():
        raise SystemExit(f"Stage already exists; inspect it before replacing: {stage}")
    stage.mkdir(parents=True)
    with ZipFile(archive_path) as archive:
        names = archive.namelist()
        for name in names:
            path = PurePosixPath(name)
            if path.is_absolute() or ".." in path.parts or not path.parts or path.parts[0] != "AceTaffyMod":
                raise SystemExit(f"Unexpected ZIP member: {name}")
        archive.extractall(stage.parent)

    with Image.open(PREVIEW_SOURCE) as source:
        cover = source.convert("RGB").resize((512, 512), Image.Resampling.LANCZOS)
        cover.save(stage / "preview.jpg", "JPEG", quality=88, optimize=True)
    if (stage / "preview.jpg").stat().st_size >= 1_000_000:
        raise SystemExit("Workshop preview is too large")

    description = f"""Title:永雏塔菲：蒸汽来客 v{version}
Tag:Invitation
```
版本：v{version}

《废墟图书馆》单场接待模组。永雏塔菲作为蒸汽发明家来到图书馆，带来蒸汽压力机制、九张战斗书页、可获取的核心书页、独立剧情立绘与蒸汽战斗效果。

邀请方式：提交一本邀请价值为6的书籍。
依赖：BaseMod for Workshop（创意工坊物品 2603522001）。

本版更新：4费「1883式蒸汽爆轰」首骰加入专属压锋与破裂压弧，改进不同掷骰倍率下的效果挂接。
```
"""
    (stage / "desc.txt").write_bytes(description.encode("utf-8"))

    errors = verify(stage, False)
    if errors:
        raise SystemExit("Staged package verification failed:\n" + "\n".join(errors))
    print(f"Workshop stage ready: {stage}")
    print(f"Release ZIP SHA256: {actual_hash}")
    print(f"Preview: {(stage / 'preview.jpg').stat().st_size} bytes")


if __name__ == "__main__":
    main()
