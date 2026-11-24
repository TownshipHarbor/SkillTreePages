param([string]$SkillTreePages = '..\Skill-Tree-Pages')
$ErrorActionPreference = 'Stop'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$pack = Join-Path $here 'res\panel.pack'
$out  = Join-Path $here 'build'
$bundle = Join-Path $SkillTreePages 'Assets\shrine-navigation'
$place  = Join-Path $SkillTreePages 'Assets\shrine-placement.json'

if (-not (Test-Path $bundle)) { throw "clone https://github.com/calebv2/Skill-Tree-Pages and pass its folder with -SkillTreePages" }
New-Item -ItemType Directory -Force (Join-Path $here 'res') | Out-Null
& py -3 -I (Join-Path $here 'tools\bake.py') $bundle $place $pack
if ($LASTEXITCODE -ne 0) { throw 'bake failed' }

& dotnet build (Join-Path $here 'quest\SkillTreePagesQuest.csproj') -c Release -o $out -nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'build failed' }
$dll = Join-Path $out 'SkillTreePagesQuest.dll'
Write-Host ("{0} {1}" -f $dll, (Get-FileHash $dll -Algorithm SHA256).Hash.Substring(0, 8).ToLower())
