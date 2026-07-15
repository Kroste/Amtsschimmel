# Liest die Version aus Directory.Build.props, prueft den Git-Zustand,
# erstellt einen annotierten Tag vX.Y.Z und pusht ihn (loest die Release-Action aus).
# Pure ASCII wegen Windows PowerShell 5.1 ANSI-Decoding; Aufruf bevorzugt via pwsh.
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

$props = Get-Content 'Directory.Build.props' -Raw
if ($props -notmatch '<Version>([^<]+)</Version>') {
    Write-Error 'Version nicht in Directory.Build.props gefunden.'
    exit 1
}
$version = $Matches[1]
$tag = "v$version"

if (git status --porcelain) {
    Write-Error 'Es gibt uncommittete Aenderungen. Erst committen.'
    exit 1
}
if (git log --branches --not --remotes --oneline) {
    Write-Error 'Es gibt ungepushte Commits. Erst pushen.'
    exit 1
}
git rev-parse $tag 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) {
    $answer = Read-Host "Tag $tag existiert bereits. Loeschen und neu setzen? [j/N]"
    if ($answer -ne 'j') {
        Write-Host 'Abgebrochen.'
        exit 0
    }
    git tag -d $tag
    git push origin ":refs/tags/$tag"
}

git tag -a $tag -m "Release $tag"
git push origin $tag
Write-Host "Tag $tag gepusht - die Release-Action baut jetzt die Pakete."
