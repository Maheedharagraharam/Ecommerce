# Launch all 4 microservices in separate terminal windows
Write-Host "Building solution once before launch..." -ForegroundColor Cyan
dotnet build --configuration Release

Write-Host "`nStarting E-Commerce Microservices Backend..." -ForegroundColor Cyan

Start-Process powershell -ArgumentList "-NoExit", "-Command", "Write-Host 'Starting Catalog Service (Port 5001)...' -ForegroundColor Green; cd '$PSScriptRoot/src/Services/Catalog/Catalog.API'; dotnet run --no-build -c Release --launch-profile http"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Write-Host 'Starting Ordering Service (Port 5002)...' -ForegroundColor Green; cd '$PSScriptRoot/src/Services/Ordering/Ordering.API'; dotnet run --no-build -c Release --launch-profile http"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Write-Host 'Starting Notification Worker (Port 5003)...' -ForegroundColor Green; cd '$PSScriptRoot/src/Services/Notification/Notification.Worker'; dotnet run --no-build -c Release --launch-profile http"
Start-Sleep -Seconds 2
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Write-Host 'Starting API Gateway (Port 5000)...' -ForegroundColor Yellow; cd '$PSScriptRoot/src/Gateway/ApiGateway'; dotnet run --no-build -c Release --launch-profile http"

Write-Host "`nAll 4 microservices successfully launched!" -ForegroundColor Green
Write-Host "API Gateway (Single Entry Point): http://localhost:5000" -ForegroundColor Yellow
Write-Host "Catalog Swagger UI:             http://localhost:5001/swagger" -ForegroundColor Cyan
Write-Host "Ordering Swagger UI:            http://localhost:5002/swagger" -ForegroundColor Cyan
Write-Host "Notification Swagger UI:        http://localhost:5003/swagger" -ForegroundColor Cyan
