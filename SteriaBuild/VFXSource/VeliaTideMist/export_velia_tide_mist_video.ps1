param([ValidatePattern('^[A-Za-z0-9_-]+$')][string]$PreviewName='reviewed-r5')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$preview=Join-Path $repo ('preview_exports/velia_tide_mist/'+$PreviewName)
$sequence=Join-Path $preview 'sequence60'
$frames=@(Get-ChildItem -LiteralPath $sequence -Filter 'frame_*.png' -File | Sort-Object Name)
if($frames.Count -ne 145){throw 'Expected the complete 145-frame native sequence'}
for($i=0;$i -lt 145;$i++){if($frames[$i].Name -ne ('frame_{0:D4}.png' -f $i)){throw 'Non-contiguous native sequence'}}
$ffmpeg=(Get-Command ffmpeg -ErrorAction Stop).Source
$ffprobe=(Get-Command ffprobe -ErrorAction Stop).Source
$video=Join-Path $preview 'velia_tide_mist_reviewed_r5_full_60fps.mp4'
$log=Join-Path $preview 'video-encode.log'
$encodeArgs=@('-hide_banner','-y','-framerate','60','-start_number','0','-i',(Join-Path $sequence 'frame_%04d.png'),
    '-frames:v','145','-c:v','h264_mf','-b:v','16M','-pix_fmt','nv12','-movflags','+faststart','-an',$video)
& $ffmpeg @encodeArgs *> $log
if($LASTEXITCODE -ne 0){Get-Content -LiteralPath $log -Tail 30;throw 'Video encoding failed'}
$probeText=& $ffprobe -v error -select_streams v:0 -count_frames -show_entries stream=codec_name,width,height,r_frame_rate,nb_read_frames,pix_fmt:format=duration,size -of json $video
if($LASTEXITCODE -ne 0){throw 'Video probing failed'}
$probe=$probeText | ConvertFrom-Json
$stream=$probe.streams[0]
if($stream.codec_name -ne 'h264' -or $stream.width -ne 1280 -or $stream.height -ne 720 -or $stream.r_frame_rate -ne '60/1' -or [int]$stream.nb_read_frames -ne 145){throw 'Encoded video does not preserve full native frame count/rate/dimensions'}
if([Math]::Abs([double]$probe.format.duration-145.0/60.0) -gt 0.02){throw 'Video duration mismatch'}
& $ffmpeg -v error -i $video -f null NUL *> (Join-Path $preview 'video-decode.log')
if($LASTEXITCODE -ne 0){throw 'Encoded video failed full decode'}
$record=[ordered]@{
    status='REVIEWED_R5_FULL_VIDEO_PASS'
    video=$video
    sha256=(Get-FileHash -LiteralPath $video -Algorithm SHA256).Hash
    source='Every native sequence60 PNG, frame_0000 through frame_0144, in order; no retiming or interpolation'
    encoder=$ffmpeg
    encoderArguments=$encodeArgs
    probe=$probe
    audio='None; this VFX scope adds no audio'
    display='Native proxy stage and UI with real repository sprites; not actual combat capture'
}
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $preview 'video-manifest.json') -Encoding utf8
Write-Host "VELIA_FULL_VIDEO_PASS $video"
