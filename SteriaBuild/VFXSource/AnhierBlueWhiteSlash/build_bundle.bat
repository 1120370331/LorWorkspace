@echo off
set UNITY_EXE=C:\Program Files\Unity\Hub\Editor\2019.4.40f1\Editor\Unity.exe
"%UNITY_EXE%" -batchmode -quit -nographics -projectPath "%~dp0UnityProject" -executeMethod AnhierBlueWhiteSlashBundleBuilder.BuildBundle -logFile "%~dp0unity-build.log"
