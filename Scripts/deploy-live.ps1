# Kids Paradise — one-click deploy to IIS Live folder
$ErrorActionPreference = "Stop"
$root = "E:\KidsParadiseByShoptickSolution"

Write-Host "==> Building React..."
Set-Location "$root\kidsparadisebyshoptick.react"
npm run build

Write-Host "==> Publishing API to Live..."
Set-Location "$root\KidsParadiseByShoptick.APIs"
dotnet publish -c Release -o "$root\KidsParadiseByShoptick.Published\Live"

Write-Host "==> Ensuring upload folders..."
@("categories", "toys", "reviews", "site") | ForEach-Object {
    New-Item -ItemType Directory -Force -Path "$root\KidsParadiseByShoptick.Published\uploads\$_" | Out-Null
}

Write-Host "==> Triggering IIS app recycle (web.config touch)..."
$wc = "$root\KidsParadiseByShoptick.Published\Live\web.config"
Copy-Item "$root\KidsParadiseByShoptick.APIs\web.config" $wc -Force
Add-Content -Path $wc -Value "<!-- deploy:$(Get-Date -Format o) -->"

Write-Host "Done. Live: $root\KidsParadiseByShoptick.Published\Live"
Write-Host "Site: https://kidsparadise.shoptick.shop"
