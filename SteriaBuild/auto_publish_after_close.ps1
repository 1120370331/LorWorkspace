$srcRoot = "C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild"
$dstRoot = "C:/Program Files (x86)/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder"
$log = "C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/auto_publish_after_close.log"

"[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] waiting game close" | Out-File -FilePath $log -Encoding UTF8 -Append
while (Get-Process -Name LibraryOfRuina -ErrorAction SilentlyContinue) {
    Start-Sleep -Milliseconds 800
}

$items = @(
    @{ src = "$srcRoot/bin/Release/net472/Steria.dll"; dst = "$dstRoot/Assemblies/Steria.dll" },
    @{ src = "$srcRoot/SteriaModFolder/Data/StageInfo.xml"; dst = "$dstRoot/Data/StageInfo.xml" },
    @{ src = "$srcRoot/SteriaModFolder/Data/PassiveList.xml"; dst = "$dstRoot/Data/PassiveList.xml" },
    @{ src = "$srcRoot/SteriaModFolder/Data/EquipPage_Enemy.xml"; dst = "$dstRoot/Data/EquipPage_Enemy.xml" },
    @{ src = "$srcRoot/SteriaModFolder/Data/EquipPage_Librarian.xml"; dst = "$dstRoot/Data/EquipPage_Librarian.xml" },
    @{ src = "$srcRoot/SteriaModFolder/Localize/cn/PassiveDesc/_PassiveDesc.txt"; dst = "$dstRoot/Localize/cn/PassiveDesc/_PassiveDesc.txt" },
    @{ src = "$srcRoot/SteriaModFolder/Localize/cn/EffectTexts/EffectTexts.xml"; dst = "$dstRoot/Localize/cn/EffectTexts/EffectTexts.xml" },
    @{ src = "$srcRoot/SteriaModFolder/Resource/ArtWork/CardBuf_DreamDirectiveMark.png"; dst = "$dstRoot/Resource/ArtWork/CardBuf_DreamDirectiveMark.png" }
)

foreach ($it in $items) {
    $dir = Split-Path $it.dst -Parent
    if (!(Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    Copy-Item -Path $it.src -Destination $it.dst -Force
    "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] copied: $($it.dst)" | Out-File -FilePath $log -Encoding UTF8 -Append
}

"[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] publish done" | Out-File -FilePath $log -Encoding UTF8 -Append
