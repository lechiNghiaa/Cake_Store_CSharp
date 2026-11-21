$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
Write-Host '== Kiem thu xUnit =='
dotnet test tests/TiemBanh.Tests/TiemBanh.Tests.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw 'xUnit that bai' }
Write-Host '== Kiem thu giao dien =='
node --test tests/web/ui-contract.test.cjs
if ($LASTEXITCODE -ne 0) { throw 'Web tests that bai' }
Write-Host '== Kiem tra Docker Compose =='
docker compose config --quiet
if ($LASTEXITCODE -ne 0) { throw 'Compose khong hop le' }
Write-Host '== Build image =='
docker compose build
if ($LASTEXITCODE -ne 0) { throw 'Docker build that bai' }
Write-Host 'Tat ca buoc da chay thanh cong (khong bao gom integration voi SQL Server).'
