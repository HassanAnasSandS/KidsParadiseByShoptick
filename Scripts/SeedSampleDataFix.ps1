# Fix missing images and toys
$baseUrl = "http://localhost:5000/api"
$tempDir = Join-Path $env:TEMP "kp-seed-images"
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

function Download-Image($seed, $fileName) {
    $path = Join-Path $tempDir $fileName
    $url = "https://picsum.photos/seed/$seed/800/600.jpg"
    try {
        Invoke-WebRequest -Uri $url -OutFile $path -UseBasicParsing
        if ((Get-Item $path).Length -lt 1000) { throw "File too small" }
    } catch {
        # fallback placeholder
        $url2 = "https://placehold.co/800x600/4F46E5/FFFFFF/png?text=$seed"
        Invoke-WebRequest -Uri $url2 -OutFile $path -UseBasicParsing
    }
    return $path
}

function Upload-Image($token, $localPath, $folder) {
    curl.exe -s -X POST "$baseUrl/admin/upload?folder=$folder" `
        -H "Authorization: Bearer $token" `
        -F "file=@$localPath" | ConvertFrom-Json
}

function Api-Post($token, $path, $body) {
    Invoke-RestMethod -Uri "$baseUrl$path" -Method POST `
        -Headers @{ Authorization = "Bearer $token" } `
        -ContentType "application/json" `
        -Body ($body | ConvertTo-Json -Depth 5)
}

function Api-Put($token, $path, $body) {
    Invoke-RestMethod -Uri "$baseUrl$path" -Method PUT `
        -Headers @{ Authorization = "Bearer $token" } `
        -ContentType "application/json" `
        -Body ($body | ConvertTo-Json -Depth 5)
}

$login = Invoke-RestMethod -Uri "$baseUrl/admin/auth/login" -Method POST `
    -ContentType "application/json" `
    -Body '{"username":"Hassan","password":"Qwerty123@"}'
$token = $login.token

# Fix category images
$catFixes = @(
    @{ Id = 3; Name = "Remote Control"; Seed = "remotecontrol" },
    @{ Id = 4; Name = "Board Games"; Seed = "boardgames" }
)
foreach ($cat in $catFixes) {
    $local = Download-Image $cat.Seed "cat-$($cat.Id).jpg"
    $upload = Upload-Image $token $local "categories"
    Api-Put $token "/admin/categories/$($cat.Id)" @{ name = $cat.Name; imagePath = $upload.path } | Out-Null
    Write-Host "Updated category: $($cat.Name)"
}

# Add missing toys
$missingToys = @(
    @{ Name = "Teddy Bear - Brown"; CategoryId = 1; Price = 2500; SalePrice = 1999; Seed = "teddybear" },
    @{ Name = "Alphabet Learning Blocks"; CategoryId = 2; Price = 3200; SalePrice = 2799; Seed = "blocks" },
    @{ Name = "Family Ludo Set"; CategoryId = 4; Price = 1200; SalePrice = 999; Seed = "ludo" },
    @{ Name = "Super Soft Panda"; CategoryId = 1; Price = 2200; SalePrice = $null; Seed = "panda" },
    @{ Name = "Wooden Puzzle Set"; CategoryId = 2; Price = 1500; SalePrice = 1299; Seed = "puzzle" }
)

foreach ($toy in $missingToys) {
    $local = Download-Image $toy.Seed "toy-$($toy.Seed).jpg"
    $upload = Upload-Image $token $local "toys"
    $body = @{
        categoryId = $toy.CategoryId
        name = $toy.Name
        price = $toy.Price
        salePrice = $toy.SalePrice
        imagePaths = @($upload.path)
    }
    Api-Post $token "/admin/toys" $body | Out-Null
    Write-Host "Added toy: $($toy.Name)"
}

Write-Host "`nFinal summary:"
sqlcmd -S "DESKTOP-FM6EHSP" -E -d "KidsParadiseByShoptick" -Q "SELECT c.Name AS Category, COUNT(t.Id) AS Toys FROM ToyCategories c LEFT JOIN Toys t ON t.CategoryId=c.Id GROUP BY c.Name ORDER BY c.Name" -W
