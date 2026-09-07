param([ValidateSet('First','Reviewed')][string]$Mode='First',[string]$Unity='C:/Program Files/Unity/Editor/Unity.exe',[ValidatePattern('^[A-Za-z0-9_-]*$')][string]$PreviewName='',[ValidateSet('Enemy','Player','Both')][string]$Facing='Enemy')
$ErrorActionPreference='Stop'
if($Mode -eq 'First' -and $Facing -eq 'Both'){throw 'Use distinct First revisions for Enemy and Player; Both is available for one Reviewed bundle readback'}
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$project=Join-Path $PSScriptRoot 'UnityProject'
$running=@(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {$_.CommandLine -like '*VeliaTideMist*'})
if($running.Count){throw 'A Unity instance is already using the VeliaTideMist project'}
$output=Join-Path $repo 'output/velia-tide-mist/implementation'
$preview=Join-Path $repo 'preview_exports/velia_tide_mist'
New-Item -ItemType Directory -Force $output,$preview,(Join-Path $project 'Assets/Scripts') | Out-Null
$sources=@('VeliaTideMistVisualController.cs','VeliaTideMistScreenFilter.cs')
$hashes=@{}
foreach($source in $sources){
    $canonical=Join-Path $repo ('SteriaBuild/'+$source)
    $mirror=Join-Path $project ('Assets/Scripts/'+$source)
    Copy-Item -LiteralPath $canonical -Destination $mirror -Force
    $hashes[$source]=(Get-FileHash $canonical -Algorithm SHA256).Hash
    if((Get-FileHash $mirror -Algorithm SHA256).Hash -ne $hashes[$source]){throw 'Pure Unity source mirror mismatch'}
}
& (Join-Path $PSScriptRoot 'verify_velia_tide_mist_source.ps1')
$method=if($Mode -eq 'First'){'VeliaTideMistBundleBuilder.PreviewFirst'}else{'VeliaTideMistBundleBuilder.BuildBundle'}
$run=[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')
$log=Join-Path $output ('unity-'+$Mode.ToLowerInvariant()+'-'+$run+'.log')
$arguments=@('-batchmode','-quit','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod',$method,'-logFile',('"'+$log+'"'))
if($PreviewName){$arguments+=@('-veliaPreviewName',$PreviewName)}
$arguments+=@('-veliaFacing',$Facing)
$process=Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru -WindowStyle Hidden
@{pid=$process.Id;log=$log;mode=$Mode;facing=$Facing;startedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content (Join-Path $output 'last-run.json') -Encoding utf8
Write-Host "Unity PID=$($process.Id) log=$log"
$timer=[Diagnostics.Stopwatch]::StartNew()
while(!$process.WaitForExit(1000)){
    if($timer.Elapsed.TotalMinutes -gt 15){Stop-Process -Id $process.Id -Force;throw 'Only this invocation was stopped after 15 minutes'}
}
if($process.ExitCode -ne 0){Get-Content $log -Tail 60;throw "Unity exit $($process.ExitCode)"}
$marker=if($Mode -eq 'First'){'VELIA_FIRST_NATIVE_PREVIEW_PASS'}else{'VELIA_BUNDLE_NATIVE_EXPORT_PASS'}
if(!(Select-String -LiteralPath $log -Pattern $marker -SimpleMatch -Quiet)){Get-Content $log -Tail 60;throw 'Native success marker absent'}
foreach($source in $sources){if((Get-FileHash (Join-Path $repo ('SteriaBuild/'+$source)) -Algorithm SHA256).Hash -ne $hashes[$source]){throw 'Canonical source changed during export'}}
Write-Host "$marker (no deployment or Steria.dll writes)"
