# Kids Paradise Admin — MAUI Android App

Native Android admin app for **Kids Paradise by Shoptick**. Uses the same REST API as the web admin panel (`/api/admin/*`). **No changes** were made to the existing API or React admin — this is a new project only.

## Features

| Module | Features |
|--------|----------|
| **Login** | Username/password, Remember me (30 days JWT) |
| **Categories** | List, search, create, edit, delete, image upload |
| **Toys** | List, filter, create, edit, delete, multiple images |
| **Orders** | List, filter, status update, full edit, create order |
| **Reviews** | List, search, edit (name, rating, comment, image) |
| **Site Images** | Upload / reset customization images |
| **Notifications** | Real-time order alerts via **SignalR** (your own API) |
| **Logout** | Flyout menu |

## API

Default base URL: `https://kidsparadise.shoptick.shop/api`

Change in `Config/AppSettings.cs` if needed.

## Notifications

When a customer places an order, the API pushes an alert through **SignalR** (`/hubs/admin-orders`). The MAUI app connects with your admin JWT and shows a **local Android notification**.

- Allow notification permission when prompted (Android 13+).
- Foreground service keeps the SignalR connection alive in the background.

## Build (Android)

```powershell
cd KidsParadiseByShoptick.AdminApp
dotnet build -f net9.0-android -c Release
```

Deploy APK from:

`bin\Release\net9.0-android\com.companyname.kidsparadisebyshoptick.adminapp-Signed.apk`

(or publish with `dotnet publish -f net9.0-android -c Release`)

## Run from Visual Studio

1. Open `KidsParadiseByShoptickSolution.sln`
2. Set **KidsParadiseByShoptick.AdminApp** as startup project
3. Select **Android Emulator** or physical device
4. Run (F5)

## Solution

Project: `KidsParadiseByShoptick.AdminApp` — added to `KidsParadiseByShoptickSolution.sln`
