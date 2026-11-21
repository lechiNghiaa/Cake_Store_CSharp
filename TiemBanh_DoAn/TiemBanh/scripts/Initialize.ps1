# Triển khai phần mềm — sinh cấu hình bí mật trên Windows, không ghi đè file hiện có.
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")
if (Test-Path .env) { Write-Host ".env đã tồn tại; giữ nguyên cấu hình."; exit 0 }
function New-RandomSecret { param([int]$Length=32)
 $bytes = New-Object byte[] $Length
 $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
 $rng.GetBytes($bytes); $rng.Dispose()
 return [Convert]::ToBase64String($bytes)
}
$sql = "Sql9!" + (New-RandomSecret)
$jwt = New-RandomSecret 48
$admin = "Banh9!" + (New-RandomSecret 18)
@("SQL_PASSWORD=$sql", "JWT_KEY=$jwt", "ADMIN_PASSWORD=$admin", "SEED_DEMO=true") | Set-Content -Encoding ASCII .env
Write-Host "Đã tạo .env. Mật khẩu đăng nhập mẫu nằm ở ADMIN_PASSWORD trong file này. Không chia sẻ .env."
