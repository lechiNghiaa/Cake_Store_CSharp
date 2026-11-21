-- Sao lưu cơ sở dữ liệu — CHECKSUM và INIT cho file snapshot trước khi chép ra host.
BACKUP DATABASE [TiemBanh] TO DISK = '/var/opt/mssql/tiembanh.bak' WITH INIT, CHECKSUM;
