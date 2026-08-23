-- Conecte-se ao banco LABWEB7Empresas
\c LABWEB7Empresas

-- 1. Permitir uso e criação no schema public
GRANT USAGE, CREATE ON SCHEMA public TO sistema;

-- 2. Permitir criação de tabelas temporárias
GRANT TEMP ON DATABASE "LABWEB7Empresas" TO sistema;

-- 3. Conceder permissões em todas as tabelas existentes
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO sistema;

-- 4. Conceder permissões em todas as sequências (caso use campos auto-incremento)
GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO sistema;

-- 5. Garantir que futuras tabelas também recebam permissões automaticamente
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO sistema;

-- 6. Garantir que futuras sequências também recebam permissões
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO sistema;