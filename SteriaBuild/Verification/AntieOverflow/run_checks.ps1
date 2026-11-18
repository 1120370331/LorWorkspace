param([switch]$Preview, [string]$Unity = 'C:/Program Files/Unity/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
Push-Location $repo
try {
    dotnet build SteriaBuild/Steria.csproj -c Release --no-restore -v quiet -clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { throw 'Steria build failed' }
    dotnet build SteriaBuild/Verification/AntieOverflow/Checks.csproj -v quiet -clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { throw 'Antie overflow fixture build failed' }

    $run = Join-Path $repo ('output/antie-overflow/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
    New-Item -ItemType Directory -Force -Path $run | Out-Null
    if (!$Preview) {
        $log = Join-Path $run 'checks.log'
        $mono = Join-Path (Split-Path -Parent $Unity) 'Data/MonoBleedingEdge/bin/mono.exe'
        & $mono 'SteriaBuild/Verification/AntieOverflow/bin/Debug/net472/Checks.exe' $repo 2>&1 | Tee-Object -FilePath $log
        if ($LASTEXITCODE -ne 0) { throw "Antie overflow fixture failed, log $log" }
        Write-Output "Antie overflow fixture log $log"
        return
    }
    $project = Join-Path $run 'UnityProject'
    foreach ($directory in @('Assets/Editor', 'Packages', 'ProjectSettings')) {
        New-Item -ItemType Directory -Force -Path (Join-Path $project $directory) | Out-Null
    }
    Copy-Item 'SteriaBuild/Verification/AntieOverflow/bin/Debug/net472' (Join-Path $run 'fixture') -Recurse
    Set-Content -LiteralPath (Join-Path $project 'Packages/manifest.json') -Encoding utf8 -Value @'
{"dependencies":{"com.unity.textmeshpro":"2.0.1","com.unity.ugui":"1.0.0","com.unity.modules.audio":"1.0.0","com.unity.modules.animation":"1.0.0","com.unity.modules.physics":"1.0.0","com.unity.modules.particlesystem":"1.0.0","com.unity.modules.imageconversion":"1.0.0","com.unity.modules.assetbundle":"1.0.0","com.unity.modules.ui":"1.0.0"}}
'@
    Set-Content -LiteralPath (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -Encoding utf8 -Value 'm_EditorVersion: 2019.3.15f1'
    Set-Content -LiteralPath (Join-Path $project 'Assets/Editor/AntieOverflowFixture.cs') -Encoding utf8 -Value @'
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class AntieOverflowFixture
{
    public static void Run()
    {
        try
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../fixture"));
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                string path = Path.Combine(directory, new AssemblyName(args.Name).Name + ".dll");
                return File.Exists(path) ? Assembly.LoadFrom(path) : null;
            };
            string repo = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../../.."));
            Assembly.LoadFrom(Path.Combine(directory, "Checks.exe")).GetType("AntieOverflowScenario")
                .GetMethod("Run").Invoke(null, new object[] { repo });
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
'@
    $log = Join-Path $run 'checks.log'
    $process = Start-Process $Unity -WindowStyle Hidden -PassThru -ArgumentList '-batchmode','-nographics','-projectPath',('"' + $project + '"'),'-executeMethod','AntieOverflowFixture.Run','-logFile',('"' + $log + '"')
    Write-Output "Antie overflow fixture PID $($process.Id) log $log"
} finally { Pop-Location }
