$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$runtime = Join-Path $repo 'SteriaBuild'
function Require([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
$driver = Get-Content -Raw (Join-Path $runtime 'VeliaTideMistVisualController.cs')
$filter = Get-Content -Raw (Join-Path $runtime 'VeliaTideMistScreenFilter.cs')
$effect = Get-Content -Raw (Join-Path $runtime 'FarAreaEffect_Steria_VeliaTideMist.cs')
$action = Get-Content -Raw (Join-Path $runtime 'BehaviourAction_Steria_VeliaTideMist.cs')
$audio = Get-Content -Raw (Join-Path $runtime 'VeliaTideMistAudioController.cs')
Require ($audio -notmatch '\b(BattleUnitModel|StageController|Faction|MonoBehaviour|UnityWebRequest)\b|Time\.deltaTime') 'Audio companion must use only explicit host delta and Unity audio APIs'
Require (($audio+$effect) -notmatch 'AudioListener\.(volume|pause)\s*=|PlayOneShot\(|soundVolume_all|soundVolume_effect') 'No global audio mutation, extra layered hits or squared options'
Require ([regex]::Matches($effect,'GetVolumeEffect\(').Count -eq 1) 'Velia host reads the existing master-times-effects option in one bridge'
Require ($driver -notmatch '\b(BattleUnitModel|BattleCamManager|FarAreaEffect|Faction|StageController|Direction|Time\.deltaTime)\b') 'Controller must be pure Unity with an explicit clock'
Require ($filter -notmatch '\b(BattleUnitModel|BattleCamManager|FarAreaEffect|Faction|StageController|Direction)\b|\.Advance\(') 'Filter must render only, without game dependencies or clock advancement'
Require ($filter -match 'new Material\(materialTemplate\)' -and $filter -match 'Graphics.Blit') 'Filter must clone its template and use native Blit'
Require (($filter + $effect) -notmatch 'RemoveCameraFilterAll|RenderSettings\.|\.fieldOfView\s*=|\.targetTexture\s*=|\.rect\s*=') 'Do not mutate shared camera/global state'
Require ($effect -notmatch '\.GiveDamage\(|\.RecoverHP\(|\.RecoverBreakLife\(|\.OnEndFarAreaBehaviourAtk\(') 'Visual host must not perform settlement'
Require ($effect -match 'cardBehaviorQueue.Count' -and $effect -match '_visual.PulseFinished') 'Read the public queue at pulse tail'
Require ($effect -notmatch 'if\s*\(_isDoneEffect\s*\|\|') 'Dice completion must not stop inter-dice Update'
Require ($effect -match 'EffectCam' -and $effect -match 'ReferenceEquals\(Active, this\)') 'Use the effect camera and identity-guarded session cleanup'
Require ($action -match 'GetOrCreate') 'Factory must reuse the current card session'
[xml]$cards = Get-Content -Raw (Join-Path $runtime 'SteriaModFolder/Data/CardInfo.xml')
$card = $cards.SelectSingleNode('//Card[@ID="9004004"]')
Require ($null -ne $card -and $card.BehaviourList.Behaviour.Count -eq 2) 'Expected two dice on 9004004'
foreach ($die in $card.BehaviourList.Behaviour) {
    Require ($die.ActionScript -eq 'Steria_VeliaTideMist') '9004004 action route missing'
    Require ($die.EffectRes -eq 'Steria_WaterSlash') 'Original EffectRes must remain'
}
$original = Join-Path $repo 'SteriaBuild/VFXSource/SlazeyaStormMass/source_assets/round2/mist_density_lighting_4x4.png'
$reference = Get-Content -Raw (Join-Path $PSScriptRoot 'source_assets/references.json') | ConvertFrom-Json
Require ((Get-FileHash $original -Algorithm SHA256).Hash -eq $reference.mist.sha256) 'Read-only mist source drift'
# Validate the decoded production input, not just the shader's property spelling.
Require ($null -ne $reference.cloudPlate) 'Cloud plate contract missing from references.json'
$cloud = Join-Path $repo $reference.cloudPlate.path
Require (Test-Path -LiteralPath $cloud -PathType Leaf) "Cloud art not yet available: $cloud"
Require ($reference.cloudPlate.sha256 -match '^[A-Fa-f0-9]{64}$') 'Cloud plate source SHA must be recorded before native export'
Require ((Get-FileHash -LiteralPath $cloud -Algorithm SHA256).Hash -eq $reference.cloudPlate.sha256) 'Cloud plate source drift'
$png = [IO.File]::ReadAllBytes($cloud)
Require ($png.Length -gt 33 -and [BitConverter]::ToString($png[0..7]) -eq '89-50-4E-47-0D-0A-1A-0A') 'Cloud plate must be PNG'
Require ($png[24] -eq 8 -and $png[25] -eq 6) 'Cloud plate must be true 8-bit RGBA, not RGB/indexed transparency'
Add-Type -AssemblyName System.Drawing
if (-not ('VeliaCloudInputCheck' -as [type])) {
    $drawingReferences = @([Drawing.Bitmap].Assembly.Location, [Drawing.Rectangle].Assembly.Location) + @(Get-ChildItem -LiteralPath $PSHOME -Filter 'System.Private.Windows*.dll' | Select-Object -ExpandProperty FullName)
    $drawingReferences = $drawingReferences | Select-Object -Unique
    Add-Type -ReferencedAssemblies $drawingReferences -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class VeliaCloudInputCheck {
    public static double[] Read(string path) {
        using (var bitmap = new Bitmap(path)) {
            var data = bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            try {
                byte[] row = new byte[bitmap.Width*4]; int min=255,max=0,opaque=0,soft=0,clear=0;
                int corridor=0,corridorClear=0,lower=0,lowerClear=0;
                for(int y=0;y<bitmap.Height;y++) {
                    Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),row,0,row.Length);
                    for(int x=0;x<bitmap.Width;x++) {
                        int a=row[x*4+3]; min=Math.Min(min,a); max=Math.Max(max,a);
                        if(a>=230)opaque++; if(a>0&&a<230)soft++; if(a<=5)clear++;
                        if(x>=bitmap.Width*.54&&x<bitmap.Width*.56) {corridor++;if(a<=5)corridorClear++;}
                        if(y>=bitmap.Height*.65&&x>=bitmap.Width*.15&&x<bitmap.Width*.85) {lower++;if(a<=5)lowerClear++;}
                    }
                }
                double n=bitmap.Width*(double)bitmap.Height;
                return new[]{(double)bitmap.Width,bitmap.Height,min,max,opaque/n,soft/n,clear/n,corridorClear/(double)corridor,lowerClear/(double)lower};
            } finally {bitmap.UnlockBits(data);}
        }
    }
}
'@
}
$stats = [VeliaCloudInputCheck]::Read($cloud)
Require ($stats[0] -eq $reference.cloudPlate.width -and $stats[1] -eq $reference.cloudPlate.height) 'Cloud dimensions differ from recorded source'
Require ($stats[0] -gt $stats[1] -and $stats[0] -le 2048 -and $stats[1] -le 2048) 'Cloud art must preserve its landscape aspect within the 2048 import limit'
Require ($stats[2] -eq 0 -and $stats[3] -ge 250 -and $stats[4] -ge 0.05 -and $stats[5] -gt 0.001) 'Cloud requires clear pixels, thick cores, and genuinely soft alpha edges'
Require ($stats[7] -ge 0.90 -and $stats[8] -ge 0.90) 'Cloud central passage/lower battlefield must remain transparent'
Require ($reference.cloudPlate.sRGB -eq $true -and $reference.cloudPlate.alpha -eq 'straight') 'Cloud is sRGB color with straight alpha, unlike the linear atlas'
Write-Host ('VELIA_CLOUD_INPUT_PASS {0}x{1} alpha={2}..{3} opaque={4:P1} corridorClear={5:P1} lowerClear={6:P1} SHA256={7}' -f $stats[0],$stats[1],$stats[2],$stats[3],$stats[4],$stats[7],$stats[8],$reference.cloudPlate.sha256)
Write-Host 'VELIA_SOURCE_PASS'
