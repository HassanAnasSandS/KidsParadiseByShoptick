# Add extra images to existing toys (no seeding — direct DB + upload)
$baseUrl = "http://localhost:5000/api"
$tempDir = Join-Path $env:TEMP "kp-extra-toy-images"
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

function Download-Image($seed, $fileName) {
    $path = Join-Path $tempDir $fileName
    $url = "https://picsum.photos/seed/$seed/800/600.jpg"
    Invoke-WebRequest -Uri $url -OutFile $path -UseBasicParsing
    return $path
}

function Upload-Image($token, $localPath) {
    $result = curl.exe -s -X POST "$baseUrl/admin/upload?folder=toys" `
        -H "Authorization: Bearer $token" `
        -F "file=@$localPath"
    return ($result | ConvertFrom-Json)
}

function Add-ToyImage($toyId, $imagePath, $sortOrder) {
    $escaped = $imagePath.Replace("'", "''")
    sqlcmd -S "DESKTOP-FM6EHSP" -E -d "KidsParadiseByShoptick" -Q `
        "INSERT INTO ToyImages (ToyId, ImagePath, SortOrder, CreatedAt) VALUES ($toyId, N'$escaped', $sortOrder, GETUTCDATE());" -W | Out-Null
}

Write-Host "Logging in..."
$login = Invoke-RestMethod -Uri "$baseUrl/admin/auth/login" -Method POST `
    -ContentType "application/json" `
    -Body '{"username":"Hassan","password":"Qwerty123@"}'
$token = $login.token

$toys = @(
    @{ Id = 2;  Name = "Plush Bunny";              Seeds = @("bunny2", "bunny3") },
    @{ Id = 4;  Name = "Kids Science Kit";         Seeds = @("science2", "science3") },
    @{ Id = 5;  Name = "RC Racing Car";            Seeds = @("rccar2", "rccar3") },
    @{ Id = 6;  Name = "RC Helicopter";            Seeds = @("rcheli2", "rcheli3") },
    @{ Id = 8;  Name = "Chess Board Premium";      Seeds = @("chess2", "chess3") },
    @{ Id = 11; Name = "Teddy Bear - Brown";       Seeds = @("teddy2", "teddy3") },
    @{ Id = 12; Name = "Alphabet Learning Blocks"; Seeds = @("blocks2", "blocks3") },
    @{ Id = 13; Name = "Family Ludo Set";          Seeds = @("ludo2", "ludo3") },
    @{ Id = 14; Name = "Super Soft Panda";         Seeds = @("panda2", "panda3") },
    @{ Id = 15; Name = "Wooden Puzzle Set";        Seeds = @("puzzle2", "puzzle3") }
)

foreach ($toy in $toys) {
    $sort = 1
    foreach ($seed in $toy.Seeds) {
        $local = Download-Image $seed "toy-$($toy.Id)-$seed.jpg"
        $upload = Upload-Image $token $local
        Add-ToyImage $toy.Id $upload.path $sort
        $sort++
        Write-Host "  + $($toy.Name) image $sort ($($upload.path))"
    }
}

Write-Host "`nDone! Image counts:"
sqlcmd -S "DESKTOP-FM6EHSP" -E -d "KidsParadiseByShoptick" -Q "SELECT t.Id, t.Name, COUNT(ti.Id) AS Images FROM Toys t LEFT JOIN ToyImages ti ON ti.ToyId=t.Id GROUP BY t.Id, t.Name ORDER BY t.Id" -W
