# Seed sample categories and toys for system testing
$baseUrl = "http://localhost:5000/api"
$published = "E:\KidsParadiseByShoptickSolution\KidsParadiseByShoptick.Published"
$tempDir = Join-Path $env:TEMP "kp-seed-images"
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
New-Item -ItemType Directory -Force -Path $published | Out-Null

function Download-Image($url, $fileName) {
    $path = Join-Path $tempDir $fileName
    if (-not (Test-Path $path)) {
        Invoke-WebRequest -Uri $url -OutFile $path -UseBasicParsing
    }
    return $path
}

function Upload-Image($token, $localPath, $folder) {
    curl.exe -s -X POST "$baseUrl/admin/upload?folder=$folder" `
        -H "Authorization: Bearer $token" `
        -F "file=@$localPath;type=image/jpeg" | ConvertFrom-Json
}

function Api-Post($token, $path, $body) {
    Invoke-RestMethod -Uri "$baseUrl$path" -Method POST `
        -Headers @{ Authorization = "Bearer $token" } `
        -ContentType "application/json" `
        -Body ($body | ConvertTo-Json -Depth 5)
}

Write-Host "Logging in..."
$login = Invoke-RestMethod -Uri "$baseUrl/admin/auth/login" -Method POST `
    -ContentType "application/json" `
    -Body '{"username":"Hassan","password":"Qwerty123@"}'
$token = $login.token
Write-Host "Logged in as $($login.username)"

$categories = @(
    @{
        Name = "Soft Toys"
        ImageUrl = "https://images.unsplash.com/photo-1587654780291-39c9404d746b?w=800&q=80"
        File = "cat-soft.jpg"
    },
    @{
        Name = "Educational Toys"
        ImageUrl = "https://images.unsplash.com/photo-1596464716127-f2a82984de30?w=800&q=80"
        File = "cat-edu.jpg"
    },
    @{
        Name = "Remote Control"
        ImageUrl = "https://images.unsplash.com/photo-1558060370-d644479b6f0c?w=800&q=80"
        File = "cat-rc.jpg"
    },
    @{
        Name = "Board Games"
        ImageUrl = "https://images.unsplash.com/photo-1611195974226-ae4f128aee1f?w=800&q=80"
        File = "cat-board.jpg"
    }
)

$categoryIds = @{}
Write-Host "`nCreating categories..."
foreach ($cat in $categories) {
    $local = Download-Image $cat.ImageUrl $cat.File
    $upload = Upload-Image $token $local "categories"
    $created = Api-Post $token "/admin/categories" @{ name = $cat.Name; imagePath = $upload.path }
    $categoryIds[$cat.Name] = $created.id
    Write-Host "  + $($cat.Name) (id=$($created.id))"
}

$toys = @(
    @{ Name = "Teddy Bear - Brown"; Category = "Soft Toys"; Price = 2500; SalePrice = 1999; ImageUrl = "https://images.unsplash.com/photo-1530329885436-15917c577212?w=800&q=80"; File = "toy-teddy.jpg" },
    @{ Name = "Plush Bunny"; Category = "Soft Toys"; Price = 1800; SalePrice = $null; ImageUrl = "https://images.unsplash.com/photo-1566576721346-d4a3b4eaeb55?w=800&q=80"; File = "toy-bunny.jpg" },
    @{ Name = "Alphabet Learning Blocks"; Category = "Educational Toys"; Price = 3200; SalePrice = 2799; ImageUrl = "https://images.unsplash.com/photo-1588072432836-e100f743f2dd?w=800&q=80"; File = "toy-blocks.jpg" },
    @{ Name = "Kids Science Kit"; Category = "Educational Toys"; Price = 4500; SalePrice = $null; ImageUrl = "https://images.unsplash.com/photo-1503676260728-1c00da094a0b?w=800&q=80"; File = "toy-science.jpg" },
    @{ Name = "RC Racing Car"; Category = "Remote Control"; Price = 5500; SalePrice = 4999; ImageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=800&q=80"; File = "toy-rc-car.jpg" },
    @{ Name = "RC Helicopter"; Category = "Remote Control"; Price = 6200; SalePrice = $null; ImageUrl = "https://images.unsplash.com/photo-1473968512647-3e447244af8f?w=800&q=80"; File = "toy-rc-heli.jpg" },
    @{ Name = "Family Ludo Set"; Category = "Board Games"; Price = 1200; SalePrice = 999; ImageUrl = "https://images.unsplash.com/photo-1611374242577-fdf2865f5a87?w=800&q=80"; File = "toy-ludo.jpg" },
    @{ Name = "Chess Board Premium"; Category = "Board Games"; Price = 2800; SalePrice = $null; ImageUrl = "https://images.unsplash.com/photo-1529699211952-734e80c4d42b?w=800&q=80"; File = "toy-chess.jpg" },
    @{ Name = "Super Soft Panda"; Category = "Soft Toys"; Price = 2200; SalePrice = $null; ImageUrl = "https://images.unsplash.com/photo-1529773470564-83b8895f8aa5?w=800&q=80"; File = "toy-panda.jpg" },
    @{ Name = "Wooden Puzzle Set"; Category = "Educational Toys"; Price = 1500; SalePrice = 1299; ImageUrl = "https://images.unsplash.com/photo-1604881991728-f8ea435b37db?w=800&q=80"; File = "toy-puzzle.jpg" }
)

Write-Host "`nCreating toys..."
foreach ($toy in $toys) {
    $local = Download-Image $toy.ImageUrl $toy.File
    $upload = Upload-Image $token $local "toys"
    $body = @{
        categoryId = $categoryIds[$toy.Category]
        name = $toy.Name
        price = $toy.Price
        salePrice = $toy.SalePrice
        imagePaths = @($upload.path)
    }
    $created = Api-Post $token "/admin/toys" $body
    $priceText = if ($toy.SalePrice) { "Rs.$($toy.SalePrice) (was $($toy.Price))" } else { "Rs.$($toy.Price)" }
    Write-Host "  + $($toy.Name) - $priceText"
}

Write-Host "`nDone! Summary:"
sqlcmd -S "DESKTOP-FM6EHSP" -E -d "KidsParadiseByShoptick" -Q "SELECT c.Name AS Category, COUNT(t.Id) AS Toys FROM ToyCategories c LEFT JOIN Toys t ON t.CategoryId=c.Id GROUP BY c.Name ORDER BY c.Name" -W
