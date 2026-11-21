# Sao lưu SQL Server — dùng BACKUP DATABASE; mật khẩu chỉ đọc trong container.
$ErrorActionPreference="Stop"
Set-Location (Join-Path $PSScriptRoot "..")
$stamp=Get-Date -Format "yyyyMMdd-HHmmss"
$dir=Join-Path (Get-Location) "backups/$stamp"
New-Item -ItemType Directory -Force $dir | Out-Null
# Dừng nhận ghi để SQL và ảnh có cùng điểm sao lưu; DB vẫn chạy.
docker compose stop web api
try {
 docker compose cp scripts/backup.sql db:/tmp/tiembanh-backup.sql
 if ($LASTEXITCODE -ne 0) { throw "Không chép được script backup" }
 docker compose exec -T db bash -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -i /tmp/tiembanh-backup.sql'
 if ($LASTEXITCODE -ne 0) { throw "SQL backup thất bại" }
 docker compose cp db:/var/opt/mssql/tiembanh.bak "$dir/database.bak"
 docker compose cp api:/app/uploads "$dir/uploads"
 Get-Date -Format o | Set-Content "$dir/manifest.txt"
} finally { docker compose start api web }
Write-Host "Sao lưu hoàn tất tại $dir; hãy chuyển sang nơi lưu trữ ngoài máy chủ."
