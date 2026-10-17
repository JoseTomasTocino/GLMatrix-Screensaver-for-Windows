$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputDirectory = Join-Path $projectRoot 'dist'
$outputFile = Join-Path $outputDirectory 'GLMatrix.scr'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw "The .NET Framework compiler was not found: $compiler"
}

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

$compilerArguments = @(
    '/nologo'
    '/target:winexe'
    '/optimize+'
    '/platform:x64'
    "/out:$outputFile"
    "/win32manifest:$(Join-Path $projectRoot 'app.manifest')"
    "/win32icon:$(Join-Path $projectRoot 'Assets\project-icon.ico')"
    "/resource:$(Join-Path $projectRoot 'Assets\matrix3.png'),MatrixSCR.Assets.matrix3.png"
    '/reference:System.dll'
    '/reference:System.Drawing.dll'
    '/reference:System.Windows.Forms.dll'
    (Join-Path $projectRoot 'Program.cs')
    (Join-Path $projectRoot 'SaverSettings.cs')
    (Join-Path $projectRoot 'SaverForm.cs')
    (Join-Path $projectRoot 'MatrixRenderer.cs')
    (Join-Path $projectRoot 'ConfigForm.cs')
    (Join-Path $projectRoot 'Properties\AssemblyInfo.cs')
)

& $compiler $compilerArguments

if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }
Write-Host "Created: $outputFile"
