param([ValidatePattern('^[A-Za-z0-9_-]+$')][string]$PreviewName='first-r9-enemy')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$preview=Join-Path $repo ('preview_exports/velia_tide_mist/'+$PreviewName)
$facingPath=Join-Path $preview 'facing.json'
$facingRecord=if(Test-Path -LiteralPath $facingPath){Get-Content -Raw -LiteralPath $facingPath | ConvertFrom-Json}else{$null}
$sequence=Join-Path $preview 'sequence60'
$frames=@(Get-ChildItem -LiteralPath $sequence -Filter 'frame_*.png' -File | Sort-Object Name)
$timingPath=Join-Path $preview 'timing.csv'
$timing=@(Import-Csv -LiteralPath $timingPath)
$frameCount=$timing.Count
if($frameCount -lt 2 -or $frames.Count -ne $frameCount){throw 'Native sequence must contain every timing.csv frame'}
$step=[double]$timing[1].time-[double]$timing[0].time
if($step -le 0){throw 'Invalid native time step'}
$frameRate=[int][Math]::Round(1.0/$step)
if($frameRate -le 0 -or $timing[-1].complete -ne 'True'){throw 'Native timing must end in a completed effect at a positive frame rate'}
for($i=0;$i -lt $frameCount;$i++){
    if($frames[$i].Name -ne ('frame_{0:D4}.png' -f $i) -or [int]$timing[$i].frame -ne $i){throw 'Non-contiguous native sequence/timing'}
    if([Math]::Abs([double]$timing[$i].time-$i/[double]$frameRate) -gt 0.000002){throw 'Native timing is not uniformly sampled; do not retime it during encoding'}
}
$ffmpeg=(Get-Command ffmpeg -ErrorAction Stop).Source
$ffprobe=(Get-Command ffprobe -ErrorAction Stop).Source
$video=Join-Path $preview ('velia_tide_mist_'+$PreviewName+'_full_60fps.mp4')
$log=Join-Path $preview 'video-encode.log'
$encodeArgs=@('-hide_banner','-y','-framerate',([string]$frameRate),'-start_number','0','-i',(Join-Path $sequence 'frame_%04d.png'),
    '-frames:v',([string]$frameCount),'-c:v','h264_mf','-b:v','16M','-pix_fmt','nv12','-movflags','+faststart','-an',$video)
& $ffmpeg @encodeArgs *> $log
if($LASTEXITCODE -ne 0){Get-Content -LiteralPath $log -Tail 30;throw 'Video encoding failed'}
$probeText=& $ffprobe -v error -select_streams v:0 -count_frames -show_entries stream=codec_name,width,height,r_frame_rate,nb_read_frames,pix_fmt:format=duration,size -of json $video
if($LASTEXITCODE -ne 0){throw 'Video probing failed'}
$probe=$probeText | ConvertFrom-Json
$stream=$probe.streams[0]
if($stream.codec_name -ne 'h264' -or $stream.width -ne 1280 -or $stream.height -ne 720 -or $stream.r_frame_rate -ne ($frameRate.ToString()+'/1') -or [int]$stream.nb_read_frames -ne $frameCount){throw 'Encoded video does not preserve full native frame count/rate/dimensions'}
if([Math]::Abs([double]$probe.format.duration-$frameCount/[double]$frameRate) -gt 0.02){throw 'Video duration mismatch'}
& $ffmpeg -v error -i $video -f null NUL *> (Join-Path $preview 'video-decode.log')
if($LASTEXITCODE -ne 0){throw 'Encoded video failed full decode'}
$record=[ordered]@{
    status='NATIVE_SEQUENCE_VIDEO_ENCODE_DECODE_COMPLETED'
    previewRevision=$PreviewName
    facing=$facingRecord
    visualAcceptance='Not determined by encoder; independent main-thread review required'
    video=$video
    sha256=(Get-FileHash -LiteralPath $video -Algorithm SHA256).Hash
    source=('Every native sequence60 PNG, frame_0000 through frame_{0:D4}, in order; no retiming or interpolation' -f ($frameCount-1))
    sourceFrameCount=$frameCount
    sourceFrameRate=$frameRate
    timingSha256=(Get-FileHash -LiteralPath $timingPath -Algorithm SHA256).Hash
    encoder=$ffmpeg
    encoderArguments=$encodeArgs
    probe=$probe
    audio='None; this VFX scope adds no audio'
    display='Native proxy stage and UI with real repository sprites; not actual combat capture'
}
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $preview 'video-manifest.json') -Encoding utf8
Write-Host "VELIA_VIDEO_ENCODE_DECODE_COMPLETED $video"
