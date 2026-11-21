#!/usr/bin/env bash
# Triển khai phần mềm — tạo bí mật cục bộ trên Linux/macOS, không in mật khẩu.
set -euo pipefail
cd "$(dirname "$0")/.."
if [ -f .env ]; then echo '.env đã tồn tại; giữ nguyên cấu hình.'; exit 0; fi
umask 077
{
 printf 'SQL_PASSWORD=Sql9!%s\n' "$(openssl rand -base64 32 | tr -d '\n')"
 printf 'JWT_KEY=%s\n' "$(openssl rand -base64 48 | tr -d '\n')"
 printf 'ADMIN_PASSWORD=Banh9!%s\n' "$(openssl rand -base64 18 | tr -d '\n')"
 printf 'SEED_DEMO=true\n'
} > .env
echo 'Đã tạo .env; mật khẩu mẫu nằm ở ADMIN_PASSWORD. Không chia sẻ .env.'
