@echo off
echo === Step 1: Generate TiledPrint.ico ===
dotnet run --project tools\GenerateIcon\GenerateIcon.csproj -- "%~dp0TiledPrint.ico"
if errorlevel 1 (
    echo ERROR: Icon generation failed.
    pause
    exit /b 1
)

echo.
echo === Step 2: Clean stale build cache ===
if exist obj rd /s /q obj
if exist bin\Release rd /s /q bin\Release

echo.
echo === Step 3: Build TiledPrint.exe (development) ===
dotnet build -c Release
if errorlevel 1 (
    echo ERROR: Build failed.
    pause
    exit /b 1
)

echo.
echo === Step 4: Publish single-file release (for sharing) ===
dotnet publish -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o publish
if errorlevel 1 (
    echo ERROR: Publish failed.
    pause
    exit /b 1
)

echo.
echo === Step 5: Package for distribution ===
if exist TiledPrint-release.zip del TiledPrint-release.zip
powershell -NoProfile -Command ^
    "Compress-Archive -Path 'publish\TiledPrint.exe','README.md' -DestinationPath 'TiledPrint-release.zip'"
if errorlevel 1 (
    echo WARNING: Could not create zip ^(PowerShell not available?^).
    echo          The exe is still at: publish\TiledPrint.exe
) else (
    echo ZIP created: TiledPrint-release.zip
)

echo.
echo ============================================================
echo  Done!
echo ============================================================
echo  Development exe : bin\Release\net9.0-windows\TiledPrint.exe
echo  Sharable exe    : publish\TiledPrint.exe
echo  Release zip     : TiledPrint-release.zip
echo.
echo  The release zip contains just TiledPrint.exe + README.md.
echo  Share this zip on the forum or attach it to a GitHub Release.
echo ============================================================
pause
