-- ==============================================================================
-- SCRIPT: Dropar todas as tabelas exceto as de configuração/controle
-- ==============================================================================
-- Objetivo: Limpar a base de dados para recriação a partir de Tabelas_Vazias.sql,
--           preservando tabelas de configuração, empresa e acesso.
--
-- Tabelas PRESERVADAS (NÃO serão dropadas):
--   - Configuracoes
--   - Empresa
--   - ReCaptchaMonitoramento
--   - Senhas
--   - UsuariosWeb
--
-- Atenção:
--   - Este script apaga dados. Use com cuidado.
--   - Recomendado fazer backup antes de executar.
--   - Executa múltiplas rodadas para garantir a remoção mesmo com dependências.
-- ==============================================================================

DO $$
DECLARE
    tabela record;
    total_restante int;
    max_rodadas int := 10;
    rodada int := 0;
BEGIN
    LOOP
        rodada := rodada + 1;
        EXIT WHEN rodada > max_rodadas;

        FOR tabela IN
            SELECT tablename
            FROM pg_tables
            WHERE schemaname = 'public'
              AND lower(tablename) NOT IN (
                  'configuracoes',
                  'empresa',
                  'recaptchamonitoramento',
                  'senhas',
                  'usuariosweb'
              )
            ORDER BY tablename
        LOOP
            EXECUTE format('DROP TABLE IF EXISTS %I.%I CASCADE', 'public', tabela.tablename);
        END LOOP;

        SELECT COUNT(*)
        INTO total_restante
        FROM pg_tables
        WHERE schemaname = 'public'
          AND lower(tablename) NOT IN (
              'configuracoes',
              'empresa',
              'recaptchamonitoramento',
              'senhas',
              'usuariosweb'
          );

        EXIT WHEN total_restante = 0;
    END LOOP;

    IF total_restante > 0 THEN
        RAISE WARNING 'Ainda restam % tabela(s) não protegida(s) após % rodadas.', total_restante, max_rodadas;
    ELSE
        RAISE NOTICE 'Todas as tabelas não protegidas foram removidas com sucesso.';
    END IF;
END $$;
