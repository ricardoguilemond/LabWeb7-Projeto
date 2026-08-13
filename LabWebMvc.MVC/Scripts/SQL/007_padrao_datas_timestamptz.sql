-- ==============================================================================
-- SCRIPT DE ACERTO DE TABELAS POSTGRESQL - PADRÃO DE DATAS
-- ==============================================================================
-- Objetivo: Ajustar a estrutura das tabelas para o padrão de datas do sistema.
--
-- Regras aplicadas:
--   1. Colunas de data/hora de eventos, registro, cadastro etc. -> TIMESTAMPTZ
--      Valores existentes são interpretados como America/Sao_Paulo e convertidos
--      para UTC, pois o sistema grava em UTC e exibe em horário local.
--   2. Colunas de data pura (sem horário) -> DATE
--      Aplica-se a: Nascimento, DUM, DataEntradaBrasil, DataNascimentoUsuario.
--   3. Sincroniza a sequence da tabela ContasRecebimento, se ela existir.
--
-- Segurança:
--   - Verifica existência de tabelas e colunas antes de alterar.
--   - Ignora silenciosamente colunas/tabelas que não existam ou já estejam no
--     padrão.
--   - Pode ser executado múltiplas vezes sem erro.
-- ==============================================================================

-- ==============================================================================
-- PASSO 1: Converter colunas que devem armazenar APENAS DATA (sem horário)
-- ==============================================================================

DO $$
DECLARE
    rec record;
BEGIN
    FOR rec IN
        SELECT * FROM (VALUES
            ('public', 'Pacientes', 'Nascimento'),
            ('public', 'Pacientes', 'DUM'),
            ('public', 'Pacientes', 'DataEntradaBrasil'),
            ('public', 'UsuariosWeb', 'DataNascimentoUsuario')
        ) AS t(schema_name, table_name, column_name)
    LOOP
        IF EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = rec.schema_name
              AND table_name = rec.table_name
              AND column_name = rec.column_name
              AND data_type = 'timestamp without time zone'
        ) THEN
            EXECUTE format(
                'ALTER TABLE %I.%I ALTER COLUMN %I TYPE DATE USING %I::DATE',
                rec.schema_name, rec.table_name, rec.column_name, rec.column_name
            );
        END IF;
    END LOOP;
END $$;

-- ==============================================================================
-- PASSO 2: Converter demais colunas TIMESTAMP para TIMESTAMPTZ (UTC)
-- ==============================================================================
-- Os valores existentes são tratados como horário local America/Sao_Paulo
-- e convertidos para UTC, seguindo a regra de persistência UTC do sistema.

DO $$
DECLARE
    rec record;
BEGIN
    FOR rec IN
        SELECT table_schema, table_name, column_name
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND data_type = 'timestamp without time zone'
    LOOP
        EXECUTE format(
            'ALTER TABLE %I.%I ALTER COLUMN %I TYPE TIMESTAMPTZ USING %I AT TIME ZONE ''America/Sao_Paulo''',
            rec.table_schema, rec.table_name, rec.column_name, rec.column_name
        );
    END LOOP;
END $$;

-- ==============================================================================
-- PASSO 3: Sincronizar sequence de ContasRecebimento (se existir)
-- ==============================================================================
-- Evita erro de chave primária duplicada após seed com Id explícito.

DO $$
DECLARE
    seq_name text;
    max_id int;
    tabela_nome text;
BEGIN
    -- Resolve o nome real da tabela (com ou sem aspas)
    SELECT quote_ident(c.relname)
    INTO tabela_nome
    FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'public'
      AND lower(c.relname) = 'contasrecebimento'
      AND c.relkind = 'r';

    IF tabela_nome IS NULL THEN
        RETURN;
    END IF;

    seq_name := pg_get_serial_sequence(tabela_nome, 'Id');

    IF seq_name IS NOT NULL THEN
        EXECUTE format('SELECT COALESCE(MAX("Id"), 0) + 1 FROM %s', tabela_nome) INTO max_id;
        PERFORM setval(seq_name, max_id, false);
    END IF;
END $$;
