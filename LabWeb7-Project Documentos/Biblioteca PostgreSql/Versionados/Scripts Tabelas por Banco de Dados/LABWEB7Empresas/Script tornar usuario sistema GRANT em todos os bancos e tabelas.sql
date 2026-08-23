DO $$
DECLARE
    db RECORD;
BEGIN
    FOR db IN
        SELECT datname FROM pg_database WHERE datname LIKE 'LABWEB7%'
    LOOP
        EXECUTE format('
            -- Conectar ao banco
            GRANT TEMP ON DATABASE %I TO sistema;

            -- Permissões no schema public
            GRANT USAGE, CREATE ON SCHEMA public TO "sistema";
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO "sistema";
            GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO "sistema";
            ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO "sistema";
            ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO "sistema";
        ', db.datname);
    END LOOP;
END $$;
