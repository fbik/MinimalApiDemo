#!/bin/bash
echo "Resetting admin password to 'Admin123!'..."

# Создаем SQL скрипт для сброса пароля
SQL_SCRIPT=$(cat << 'SQL'
-- Подключаемся к базе и сбрасываем пароль
UPDATE "AppUsers" 
SET "PasswordHash" = 'JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=' 
WHERE "Username" = 'admin';
SQL
)

# Выполняем SQL в контейнере PostgreSQL
docker exec -i minimalapi-postgres psql -U postgres -d minimalapi <<< "$SQL_SCRIPT"

echo "Password reset completed!"
echo "Now you can login with: admin / Admin123!"
