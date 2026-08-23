-- =============================================================================
-- MIGRACAO: timestamp without time zone -> timestamptz (UTC)
-- PROJETO: LabWeb7
-- DATA:    2026-04-21
-- AUTOR:   Estrategia de Timestamp Robusta
--
-- REGRAS:
--   1. Todas as colunas de timestamp migram para timestamptz (UTC no banco)
--   2. Dados existentes sao interpretados como horario de Brasilia e convertidos
--      para UTC via: AT TIME ZONE 'America/Sao_Paulo'
--   3. Colunas de criacao/auditoria recebem DEFAULT CURRENT_TIMESTAMP
--   4. Backup obrigatorio antes da execucao
-- =============================================================================

-- ------------------------------------------------------------------------------
-- 0. BACKUP (executar manualmente antes)
-- ------------------------------------------------------------------------------
-- pg_dump -U sistema -h localhost -d labweb7 -f backup_pre_migracao_timestamptz.sql

-- ------------------------------------------------------------------------------
-- 1. CONTROLE DE CONCORRENCIA
-- ------------------------------------------------------------------------------
ALTER TABLE "ControleConcorrencia"
    ALTER COLUMN "DataHora" TYPE timestamptz
    USING "DataHora" AT TIME ZONE 'America/Sao_Paulo';

-- ------------------------------------------------------------------------------
-- 2. EMPRESA (INSTITUICAO)
-- ------------------------------------------------------------------------------
ALTER TABLE "Empresa"
    ALTER COLUMN "DataCadastro" TYPE timestamptz
    USING "DataCadastro" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "Empresa"
    ALTER COLUMN "DataCadastro" SET DEFAULT CURRENT_TIMESTAMP;

ALTER TABLE "Empresa"
    ALTER COLUMN "DataExpira" TYPE timestamptz
    USING "DataExpira" AT TIME ZONE 'America/Sao_Paulo';

-- ------------------------------------------------------------------------------
-- 3. ER TEMPORARIO
-- ------------------------------------------------------------------------------
ALTER TABLE "ERTemporario"
    ALTER COLUMN "DataEntrega" TYPE timestamptz
    USING "DataEntrega" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ERTemporario"
    ALTER COLUMN "DataExame" TYPE timestamptz
    USING "DataExame" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ERTemporario"
    ALTER COLUMN "DataFim" TYPE timestamptz
    USING "DataFim" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ERTemporario"
    ALTER COLUMN "DataIni" TYPE timestamptz
    USING "DataIni" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ERTemporario"
    ALTER COLUMN "DataIni" SET DEFAULT CURRENT_TIMESTAMP;

-- ------------------------------------------------------------------------------
-- 4. EXAMES EXPORTADOS
-- ------------------------------------------------------------------------------
ALTER TABLE "ExamesExportados"
    ALTER COLUMN "DataColeta" TYPE timestamptz
    USING "DataColeta" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ExamesExportados"
    ALTER COLUMN "DataExportado" TYPE timestamptz
    USING "DataExportado" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ExamesExportados"
    ALTER COLUMN "DataExportado" SET DEFAULT CURRENT_TIMESTAMP;

ALTER TABLE "ExamesExportados"
    ALTER COLUMN "DataImportado" TYPE timestamptz
    USING "DataImportado" AT TIME ZONE 'America/Sao_Paulo';

-- ------------------------------------------------------------------------------
-- 5. EXAMES IMPRESSOS
-- ------------------------------------------------------------------------------
ALTER TABLE "ExamesImpressos"
    ALTER COLUMN "DataExame" TYPE timestamptz
    USING "DataExame" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ExamesImpressos"
    ALTER COLUMN "DataImpresso" TYPE timestamptz
    USING "DataImpresso" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ExamesImpressos"
    ALTER COLUMN "DataImpresso" SET DEFAULT CURRENT_TIMESTAMP;

-- ------------------------------------------------------------------------------
-- 6. EXAMES PENDENTES
-- ------------------------------------------------------------------------------
ALTER TABLE "ExamesPendentes"
    ALTER COLUMN "DataIni" TYPE timestamptz
    USING "DataIni" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ExamesPendentes"
    ALTER COLUMN "DataIni" SET DEFAULT CURRENT_TIMESTAMP;

-- ------------------------------------------------------------------------------
-- 7. EXAMES REALIZADOS
-- ------------------------------------------------------------------------------
ALTER TABLE "ExamesRealizados"
    ALTER COLUMN "DataEntrega" TYPE timestamptz
    USING "DataEntrega" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ExamesRealizados"
    ALTER COLUMN "DataExame" TYPE timestamptz
    USING "DataExame" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ExamesRealizados"
    ALTER COLUMN "DataFim" TYPE timestamptz
    USING "DataFim" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ExamesRealizados"
    ALTER COLUMN "DataIni" TYPE timestamptz
    USING "DataIni" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ExamesRealizados"
    ALTER COLUMN "DataIni" SET DEFAULT CURRENT_TIMESTAMP;

-- ------------------------------------------------------------------------------
-- 8. EXAMES REALIZADOS AM
-- ------------------------------------------------------------------------------
ALTER TABLE "ExamesRealizadosAM"
    ALTER COLUMN "DataEntrega" TYPE timestamptz
    USING "DataEntrega" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ExamesRealizadosAM"
    ALTER COLUMN "DataExame" TYPE timestamptz
    USING "DataExame" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ExamesRealizadosAM"
    ALTER COLUMN "DataFim" TYPE timestamptz
    USING "DataFim" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ExamesRealizadosAM"
    ALTER COLUMN "DataIni" TYPE timestamptz
    USING "DataIni" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "ExamesRealizadosAM"
    ALTER COLUMN "DataIni" SET DEFAULT CURRENT_TIMESTAMP;

-- ------------------------------------------------------------------------------
-- 9. FICHAS INTERNAS
-- ------------------------------------------------------------------------------
ALTER TABLE "FichasInternas"
    ALTER COLUMN "DataExame" TYPE timestamptz
    USING "DataExame" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "FichasInternas"
    ALTER COLUMN "DataFim" TYPE timestamptz
    USING "DataFim" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "FichasInternas"
    ALTER COLUMN "DataIni" TYPE timestamptz
    USING "DataIni" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "FichasInternas"
    ALTER COLUMN "DataIni" SET DEFAULT CURRENT_TIMESTAMP;

-- ------------------------------------------------------------------------------
-- 10. FICHAS LOTES
-- ------------------------------------------------------------------------------
ALTER TABLE "FichasLotes"
    ALTER COLUMN "DataExame" TYPE timestamptz
    USING "DataExame" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "FichasLotes"
    ALTER COLUMN "DataFim" TYPE timestamptz
    USING "DataFim" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "FichasLotes"
    ALTER COLUMN "DataIni" TYPE timestamptz
    USING "DataIni" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "FichasLotes"
    ALTER COLUMN "DataIni" SET DEFAULT CURRENT_TIMESTAMP;

-- ------------------------------------------------------------------------------
-- 11. FICHAS PLANILHAS
-- ------------------------------------------------------------------------------
ALTER TABLE "FichasPlanilhas"
    ALTER COLUMN "DataExame" TYPE timestamptz
    USING "DataExame" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "FichasPlanilhas"
    ALTER COLUMN "DataFim" TYPE timestamptz
    USING "DataFim" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "FichasPlanilhas"
    ALTER COLUMN "DataIni" TYPE timestamptz
    USING "DataIni" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "FichasPlanilhas"
    ALTER COLUMN "DataIni" SET DEFAULT CURRENT_TIMESTAMP;

-- ------------------------------------------------------------------------------
-- 12. INTEGRACAO DADOS EXECUCAO
-- ------------------------------------------------------------------------------
ALTER TABLE "IntegracaoDadosExecucao"
    ALTER COLUMN "Inicio" TYPE timestamptz
    USING "Inicio" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "IntegracaoDadosExecucao"
    ALTER COLUMN "Termino" TYPE timestamptz
    USING "Termino" AT TIME ZONE 'America/Sao_Paulo';

-- ------------------------------------------------------------------------------
-- 13. INTEGRACAO DADOS LAYOUT
-- ------------------------------------------------------------------------------
ALTER TABLE "IntegracaoDadosLayout"
    ALTER COLUMN "DataFinal" TYPE timestamptz
    USING "DataFinal" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "IntegracaoDadosLayout"
    ALTER COLUMN "DataInicial" TYPE timestamptz
    USING "DataInicial" AT TIME ZONE 'America/Sao_Paulo';

-- ------------------------------------------------------------------------------
-- 14. ITENS EXAMES REALIZADOS
-- ------------------------------------------------------------------------------
ALTER TABLE "ItensExamesRealizados"
    ALTER COLUMN "DataEntregaParcial" TYPE timestamptz
    USING "DataEntregaParcial" AT TIME ZONE 'America/Sao_Paulo';

-- ------------------------------------------------------------------------------
-- 15. ITENS EXAMES REALIZADOS AM
-- ------------------------------------------------------------------------------
ALTER TABLE "ItensExamesRealizadosAM"
    ALTER COLUMN "DataEntregaParcial" TYPE timestamptz
    USING "DataEntregaParcial" AT TIME ZONE 'America/Sao_Paulo';

-- ------------------------------------------------------------------------------
-- 16. LOG ARQUIVOS
-- ------------------------------------------------------------------------------
ALTER TABLE "LogArquivos"
    ALTER COLUMN "Data" TYPE timestamptz
    USING "Data" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "LogArquivos"
    ALTER COLUMN "Data" SET DEFAULT CURRENT_TIMESTAMP;

ALTER TABLE "LogArquivos"
    ALTER COLUMN "DataPeriodoFinal" TYPE timestamptz
    USING "DataPeriodoFinal" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "LogArquivos"
    ALTER COLUMN "DataPeriodoInicial" TYPE timestamptz
    USING "DataPeriodoInicial" AT TIME ZONE 'America/Sao_Paulo';

-- ------------------------------------------------------------------------------
-- 17. PACIENTES
-- ------------------------------------------------------------------------------
ALTER TABLE "Pacientes"
    ALTER COLUMN "DataBaixa" TYPE timestamptz
    USING "DataBaixa" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "Pacientes"
    ALTER COLUMN "DataEntrada" TYPE timestamptz
    USING "DataEntrada" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "Pacientes"
    ALTER COLUMN "DataEntrada" SET DEFAULT CURRENT_TIMESTAMP;

ALTER TABLE "Pacientes"
    ALTER COLUMN "DataEntradaBrasil" TYPE timestamptz
    USING "DataEntradaBrasil" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "Pacientes"
    ALTER COLUMN "DataRegistro" TYPE timestamptz
    USING "DataRegistro" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "Pacientes"
    ALTER COLUMN "DataRegistro" SET DEFAULT CURRENT_TIMESTAMP;

ALTER TABLE "Pacientes"
    ALTER COLUMN "DUM" TYPE timestamptz
    USING "DUM" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "Pacientes"
    ALTER COLUMN "Nascimento" TYPE timestamptz
    USING "Nascimento" AT TIME ZONE 'America/Sao_Paulo';

-- ------------------------------------------------------------------------------
-- 18. RASTREAMENTOS
-- ------------------------------------------------------------------------------
ALTER TABLE "Rastreamentos"
    ALTER COLUMN "DataOcorrencia" TYPE timestamptz
    USING "DataOcorrencia" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "Rastreamentos"
    ALTER COLUMN "DataOcorrencia" SET DEFAULT CURRENT_TIMESTAMP;

-- ------------------------------------------------------------------------------
-- 19. REQUISITAR
-- ------------------------------------------------------------------------------
ALTER TABLE "Requisitar"
    ALTER COLUMN "DataEntregaParcial" TYPE timestamptz
    USING "DataEntregaParcial" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "Requisitar"
    ALTER COLUMN "DataIni" TYPE timestamptz
    USING "DataIni" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "Requisitar"
    ALTER COLUMN "DataIni" SET DEFAULT CURRENT_TIMESTAMP;

-- ------------------------------------------------------------------------------
-- 20. SENHAS
-- ------------------------------------------------------------------------------
ALTER TABLE "Senhas"
    ALTER COLUMN "DataCadastro" TYPE timestamptz
    USING "DataCadastro" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "Senhas"
    ALTER COLUMN "DataCadastro" SET DEFAULT CURRENT_TIMESTAMP;

ALTER TABLE "Senhas"
    ALTER COLUMN "DataExpira" TYPE timestamptz
    USING "DataExpira" AT TIME ZONE 'America/Sao_Paulo';

-- ------------------------------------------------------------------------------
-- 21. USUARIOS WEB
-- ------------------------------------------------------------------------------
ALTER TABLE "UsuariosWeb"
    ALTER COLUMN "DataCadastro" TYPE timestamptz
    USING "DataCadastro" AT TIME ZONE 'America/Sao_Paulo';

ALTER TABLE "UsuariosWeb"
    ALTER COLUMN "DataCadastro" SET DEFAULT CURRENT_TIMESTAMP;

ALTER TABLE "UsuariosWeb"
    ALTER COLUMN "DataNascimentoUsuario" TYPE timestamptz
    USING "DataNascimentoUsuario" AT TIME ZONE 'America/Sao_Paulo';

-- ------------------------------------------------------------------------------
-- 21. VERIFICACAO FINAL
-- ------------------------------------------------------------------------------
-- Verifique se todas as colunas estao com timestamptz:
-- SELECT table_name, column_name, data_type
-- FROM information_schema.columns
-- WHERE table_schema = 'public'
--   AND data_type = 'timestamp with time zone'
-- ORDER BY table_name, column_name;

-- Verifique o timezone do servidor PostgreSQL:
-- SHOW timezone;
