@echo off
set cspath=C:\Windows\Microsoft.NET\Framework\v4.0.30319
for /f "tokens=2*" %%A in ('reg query HKCU\Software\Microsoft\Fiddler2\InstallerSettings /v InstallPath') do (set fiddlerpath=%%B)
%cspath%\csc /d:TRACE /target:library /out:"ToPython.dll" ToPython.cs /reference:"%fiddlerpath%Fiddler.exe"
move "%cd%\ToPython.dll" "%USERPROFILE%\Documents\Fiddler2\Scripts"
pause