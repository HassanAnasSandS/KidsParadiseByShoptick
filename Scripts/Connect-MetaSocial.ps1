param(
    [Parameter(Mandatory = $true)]
    [string]$UserAccessToken,

    [string]$WhatsAppCatalogId = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$appDataDir = Join-Path $repoRoot "KidsParadiseByShoptick.Published\.app-data"
$metaFile = Join-Path $appDataDir "meta-oauth.json"
$appsettingsPath = Join-Path $repoRoot "KidsParadiseByShoptick.APIs\appsettings.json"
$legacyMahirahCatalogId = "234921813846539"
$kidsParadiseBusinessId = "2551630811980010"
$secretsFiles = @(
    (Join-Path $repoRoot "KidsParadiseByShoptick.APIs\appsettings.Secrets.json"),
    (Join-Path $repoRoot "KidsParadiseByShoptick.Published\Live\appsettings.Secrets.json")
)

Write-Host ""
Write-Host "=== Meta Setup - Required Graph API permissions ===" -ForegroundColor Cyan
Write-Host "In Graph API Explorer (developers.facebook.com/tools/explorer):"
Write-Host "  App: KidsParadiseByShoptick"
Write-Host "  Add permissions:"
Write-Host "    - pages_show_list"
Write-Host "    - pages_read_engagement"
Write-Host "    - pages_manage_posts"
Write-Host "    - instagram_basic"
Write-Host "    - instagram_content_publish"
Write-Host "    - catalog_management          (WhatsApp catalog)"
Write-Host "    - whatsapp_business_management  (WhatsApp catalog)"
Write-Host "  Generate Access Token -> approve Kids Paradise Page only (portfolio: Kids Paradise By Shoptick)"
Write-Host ""

function Get-ConfiguredMetaIds {
    $catalogId = $null
    $wabaId = $null
    foreach ($path in @($secretsFiles + $appsettingsPath)) {
        if (-not (Test-Path $path)) { continue }
        $json = Get-Content $path -Raw | ConvertFrom-Json
        $m = $json.MetaSocial
        if (-not $m) { continue }
        if ($m.WhatsAppCatalogId) { $catalogId = $m.WhatsAppCatalogId }
        if ($m.WhatsAppBusinessAccountId) { $wabaId = $m.WhatsAppBusinessAccountId }
    }
    [pscustomobject]@{
        WhatsAppCatalogId         = $catalogId
        WhatsAppBusinessAccountId = $wabaId
    }
}

Write-Host "Checking Meta business portfolios for this token..."
try {
    $meUri = "https://graph.facebook.com/v25.0/me?fields=id,name&access_token=$UserAccessToken"
    $me = Invoke-RestMethod -Uri $meUri
    $bizUri = "https://graph.facebook.com/v25.0/$($me.id)/business_users?fields=business{id,name},role&access_token=$UserAccessToken"
    $biz = Invoke-RestMethod -Uri $bizUri
    $names = @($biz.data | ForEach-Object { $_.business.name })
    Write-Host "  Logged in as: $($me.name) ($($me.id))"
    foreach ($entry in $biz.data) {
        $flag = if ($entry.business.id -eq $kidsParadiseBusinessId) { "OK" } else { "non-Kids-Paradise" }
        Write-Host "  - $($entry.business.name) [$flag]"
    }
    $mahirah = @($biz.data | Where-Object { $_.business.name -match 'Mahirah|hairaccessories' })
    if ($mahirah.Count -gt 0) {
        Write-Host ""
        Write-Host "Warning: Token still has access to Mahirah/other portfolios." -ForegroundColor Yellow
        Write-Host "  Leave them in Meta: business.facebook.com -> Settings -> Users -> Remove yourself"
        Write-Host "  Use only portfolio: Kids Paradise By Shoptick ($kidsParadiseBusinessId)"
    }
} catch {
    Write-Host "  Could not list business portfolios (add business_management to verify)." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Fetching Facebook Pages from Meta..."
$uri = "https://graph.facebook.com/v25.0/me/accounts?fields=id,name,access_token,instagram_business_account{id,username},whatsapp_business_account{id}&access_token=$UserAccessToken"
$response = Invoke-RestMethod -Uri $uri
$pages = @($response.data)

if ($pages.Count -eq 0) {
    throw @"
No Facebook Pages were returned for this token.

In Graph API Explorer:
1. Meta App: KidsParadiseByShoptick
2. Add permissions listed above (especially catalog_management + whatsapp_business_management)
3. Generate Access Token again
4. In the Facebook popup, select your Kids Paradise Page and approve all pages
5. Run: me/accounts and confirm your page appears
"@
}

$preferred = $pages | Where-Object {
    $_.name -match 'paradise|closet|toy|kids'
} | Select-Object -First 1

if (-not $preferred) {
    $preferred = $pages[0]
}

$pageId = $preferred.id
$pageToken = $preferred.access_token
$pageName = $preferred.name
$igId = $preferred.instagram_business_account.id
$wabaId = $preferred.whatsapp_business_account.id

Write-Host "Selected page: $pageName ($pageId)"
if ($igId) {
    Write-Host "Instagram Business ID: $igId"
} else {
    Write-Host "Warning: No Instagram Business account is linked to this page." -ForegroundColor Yellow
}

$catalogId = $WhatsAppCatalogId
$configured = Get-ConfiguredMetaIds
if (-not $catalogId) { $catalogId = $configured.WhatsAppCatalogId }
if (-not $wabaId) { $wabaId = $configured.WhatsAppBusinessAccountId }

if ($wabaId) {
    Write-Host "WhatsApp Business Account ID: $wabaId"
    if (-not $catalogId) {
        try {
            $wabaUri = "https://graph.facebook.com/v25.0/$wabaId`?fields=product_catalog{id}&access_token=$pageToken"
            $waba = Invoke-RestMethod -Uri $wabaUri
            $catalogId = $waba.product_catalog.id
        } catch {
            Write-Host "Could not auto-discover catalog from WABA." -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "Step 1 NOT met: WhatsApp Business API (WABA) is NOT linked to this Facebook Page." -ForegroundColor Red
    Write-Host "  Page phone (+923217175896) can show on Facebook while WABA is still missing."
    Write-Host "  Fix: developers.facebook.com -> KidsParadiseByShoptick -> WhatsApp -> Getting Started -> add phone"
    Write-Host "  Also add permission: whatsapp_business_management in Graph API Explorer"
    if (-not $catalogId) {
        try {
            $pageCatUri = "https://graph.facebook.com/v25.0/$pageId`?fields=product_catalogs{id,name,product_count}&access_token=$pageToken"
            $pageCat = Invoke-RestMethod -Uri $pageCatUri
            $firstCatalog = @($pageCat.product_catalogs.data | Where-Object { $_.id -ne $legacyMahirahCatalogId })[0]
            if ($firstCatalog.id) {
                $catalogId = $firstCatalog.id
                Write-Host "  Found Meta Commerce catalog on Page: $($firstCatalog.name) ($catalogId) — toys can be added via API." -ForegroundColor Yellow
                Write-Host "  WhatsApp app catalog will stay empty until WABA is linked and catalog is synced (Step 3)." -ForegroundColor Yellow
            }
        } catch {
            Write-Host "  Could not read Page product catalog." -ForegroundColor Yellow
        }
    }
}

if ($catalogId -eq $legacyMahirahCatalogId) {
    Write-Host "Rejecting legacy Mahirah catalog $legacyMahirahCatalogId — using Kids Paradise catalog from appsettings." -ForegroundColor Yellow
    $catalogId = $configured.WhatsAppCatalogId
}

if ($catalogId) {
    Write-Host "WhatsApp Catalog ID: $catalogId"
} elseif ($wabaId) {
    Write-Host "Step 2 NOT met: No catalog linked to WhatsApp." -ForegroundColor Red
    Write-Host "  Fix: Commerce Manager -> create catalog -> WhatsApp Manager -> enable Catalog"
    Write-Host "  Then re-run with: -WhatsAppCatalogId YOUR_CATALOG_ID"
}

Write-Host ""
Write-Host "Checking token permissions..."
try {
    $secretsPath = $secretsFiles | Where-Object { Test-Path $_ } | Select-Object -First 1
    $appId = "1409004537733994"
    if ($secretsPath) {
        $secrets = Get-Content $secretsPath -Raw | ConvertFrom-Json
        if ($secrets.MetaSocial.AppId) { $appId = $secrets.MetaSocial.AppId }
    }
    $appSecret = $env:META_APP_SECRET
    if (-not $appSecret -and $secretsPath) {
        $appSecret = $secrets.MetaSocial.AppSecret
    }
    if ($appSecret) {
        $debugUri = "https://graph.facebook.com/v25.0/debug_token?input_token=$pageToken&access_token=$appId|$appSecret"
        $debug = Invoke-RestMethod -Uri $debugUri
        $scopes = @($debug.data.scopes)
        $required = @("catalog_management", "whatsapp_business_management", "pages_manage_posts", "instagram_content_publish")
        $missing = $required | Where-Object { $scopes -notcontains $_ }
        if ($missing.Count -eq 0) {
            Write-Host "Step 3 OK: All required permissions granted." -ForegroundColor Green
        } else {
            $missingList = $missing -join ", "
            Write-Host "Step 3 NOT met: Missing permissions: $missingList" -ForegroundColor Red
        }
    } else {
        Write-Host "Step 3: Set META_APP_SECRET env var to verify permissions, or check Graph API Explorer." -ForegroundColor Yellow
    }
} catch {
    Write-Host "Step 3: Could not verify permissions automatically." -ForegroundColor Yellow
}

$store = [ordered]@{
    longLivedUserToken           = $UserAccessToken.Trim()
    pageAccessToken              = $pageToken
    facebookPageId               = $pageId
    instagramBusinessAccountId   = $igId
    whatsAppBusinessAccountId    = $wabaId
    whatsAppCatalogId            = $catalogId
    updatedAt                    = (Get-Date).ToUniversalTime().ToString("o")
}

New-Item -ItemType Directory -Force -Path $appDataDir | Out-Null
$store | ConvertTo-Json -Depth 4 | Set-Content -Path $metaFile -Encoding UTF8
Write-Host ""
Write-Host "Saved token store: $metaFile"

foreach ($secretsPath in $secretsFiles) {
    if (-not (Test-Path $secretsPath)) { continue }
    $json = Get-Content $secretsPath -Raw | ConvertFrom-Json
    if (-not $json.MetaSocial) { $json | Add-Member -NotePropertyName MetaSocial -NotePropertyValue ([pscustomobject]@{}) }
    $json.MetaSocial.Enabled = $true
    $json.MetaSocial.FacebookPageId = $pageId
    $json.MetaSocial.InstagramBusinessAccountId = $igId
    if ($json.MetaSocial.PSObject.Properties.Name -notcontains "WhatsAppBusinessAccountId") {
        $json.MetaSocial | Add-Member -NotePropertyName WhatsAppBusinessAccountId -NotePropertyValue $wabaId
    } else {
        $json.MetaSocial.WhatsAppBusinessAccountId = $wabaId
    }
    if ($json.MetaSocial.PSObject.Properties.Name -notcontains "WhatsAppCatalogId") {
        $json.MetaSocial | Add-Member -NotePropertyName WhatsAppCatalogId -NotePropertyValue $catalogId
    } else {
        $json.MetaSocial.WhatsAppCatalogId = $catalogId
    }
    if ($json.MetaSocial.PSObject.Properties.Name -contains "WhatsAppLinkedCatalogId") {
        $json.MetaSocial.PSObject.Properties.Remove("WhatsAppLinkedCatalogId")
    }
    if ($json.MetaSocial.PSObject.Properties.Name -contains "PageAccessToken") {
        $json.MetaSocial.PSObject.Properties.Remove("PageAccessToken")
    }
    $json | ConvertTo-Json -Depth 6 | Set-Content -Path $secretsPath -Encoding UTF8
    Write-Host "Updated secrets: $secretsPath"
}

Write-Host ""
Write-Host "Meta setup saved. Restart the API, then GET /api/admin/meta/requirements to verify all 4 steps." -ForegroundColor Green
