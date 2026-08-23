-- ============================================================================
-- LabWeb7-Projeto — Migração de datas de negócio de TIMESTAMPTZ para DATE
-- Feito pelo Qoder em 22/08/2026
--
-- OBJETIVO: datas de negócio passam a gravar somente dia/mês/ano (DATE).
-- Auditoria/rastreio (DataRegistro, DataCadastro, DataExpira, DataOcorrencia,
-- LogArquivos.Data, ControleConcorrencia.DataHora, DataExportado,
-- DataImportado, DataImpresso, IntegracaoDadosExecucao.Inicio/Termino)
-- PERMANECEM TIMESTAMPTZ.
--
-- POR QUE O CAST É SEGURO (não vira o dia):
--   As datas de negócio são digitadas/salvas como MEIA-NOITE na representação
--   local. Nas duas eras de gravação do sistema:
--     * Legado (local gravado direto): '01/01 00:00' ficou armazenado como
--       '01/01 00:00 UTC' -> o cast abaixo devolve 01/01.
--     * Atual (local -> UTC, America/Sao_Paulo = UTC-3): '01/01 00:00' local
--       virou '01/01 03:00 UTC' -> o cast abaixo devolve 01/01 (mesmo dia).
--   Por isso o cast é feito EXPLICITAMENTE em UTC. NUNCA usar
--   AT TIME ZONE 'America/Sao_Paulo' aqui: ele retrocederia o legado para
--   21:00 do dia anterior (viraria o dia).
--
-- EXECUÇÃO:
--   1) FAÇA BACKUP COMPLETO DO BANCO ANTES DE EXECUTAR.
--   2) Execute em janela de manutenção: cada ALTER reescreve a tabela e
--      mantém lock de escrita durante a operação.
--   3) Suba o código (anotações [Column(TypeName="date")] + Fluent API nos
--      db.cs) JUNTAMENTE com este script.
--   4) Todo o bloco é transacional: qualquer falha desfaz tudo (ROLLBACK).
-- ============================================================================

BEGIN;

-- ----------------------------------------------------------------------------
-- Pacientes (DataRegistro permanece TIMESTAMPTZ)
-- ----------------------------------------------------------------------------
ALTER TABLE "Pacientes" ALTER COLUMN "Nascimento"        TYPE date USING ("Nascimento"        AT TIME ZONE 'UTC')::date;
ALTER TABLE "Pacientes" ALTER COLUMN "DUM"               TYPE date USING ("DUM"               AT TIME ZONE 'UTC')::date;
ALTER TABLE "Pacientes" ALTER COLUMN "DataEntradaBrasil" TYPE date USING ("DataEntradaBrasil" AT TIME ZONE 'UTC')::date;
ALTER TABLE "Pacientes" ALTER COLUMN "DataEntrada"       TYPE date USING ("DataEntrada"       AT TIME ZONE 'UTC')::date;
ALTER TABLE "Pacientes" ALTER COLUMN "DataBaixa"         TYPE date USING ("DataBaixa"         AT TIME ZONE 'UTC')::date;

-- ----------------------------------------------------------------------------
-- UsuariosWeb (DataCadastro permanece TIMESTAMPTZ)
-- ----------------------------------------------------------------------------
ALTER TABLE "UsuariosWeb" ALTER COLUMN "DataNascimentoUsuario" TYPE date USING ("DataNascimentoUsuario" AT TIME ZONE 'UTC')::date;

-- ----------------------------------------------------------------------------
-- Exames realizados (normais e AM) — ERTemporario é o espelho temporário
-- ----------------------------------------------------------------------------
ALTER TABLE "ExamesRealizados"   ALTER COLUMN "DataIni"     TYPE date USING ("DataIni"     AT TIME ZONE 'UTC')::date;
ALTER TABLE "ExamesRealizados"   ALTER COLUMN "DataFim"     TYPE date USING ("DataFim"     AT TIME ZONE 'UTC')::date;
ALTER TABLE "ExamesRealizados"   ALTER COLUMN "DataExame"   TYPE date USING ("DataExame"   AT TIME ZONE 'UTC')::date;
ALTER TABLE "ExamesRealizados"   ALTER COLUMN "DataEntrega" TYPE date USING ("DataEntrega" AT TIME ZONE 'UTC')::date;

ALTER TABLE "ExamesRealizadosAM" ALTER COLUMN "DataIni"     TYPE date USING ("DataIni"     AT TIME ZONE 'UTC')::date;
ALTER TABLE "ExamesRealizadosAM" ALTER COLUMN "DataFim"     TYPE date USING ("DataFim"     AT TIME ZONE 'UTC')::date;
ALTER TABLE "ExamesRealizadosAM" ALTER COLUMN "DataExame"   TYPE date USING ("DataExame"   AT TIME ZONE 'UTC')::date;
ALTER TABLE "ExamesRealizadosAM" ALTER COLUMN "DataEntrega" TYPE date USING ("DataEntrega" AT TIME ZONE 'UTC')::date;

ALTER TABLE "ERTemporario"       ALTER COLUMN "DataIni"     TYPE date USING ("DataIni"     AT TIME ZONE 'UTC')::date;
ALTER TABLE "ERTemporario"       ALTER COLUMN "DataFim"     TYPE date USING ("DataFim"     AT TIME ZONE 'UTC')::date;
ALTER TABLE "ERTemporario"       ALTER COLUMN "DataExame"   TYPE date USING ("DataExame"   AT TIME ZONE 'UTC')::date;
ALTER TABLE "ERTemporario"       ALTER COLUMN "DataEntrega" TYPE date USING ("DataEntrega" AT TIME ZONE 'UTC')::date;

-- ----------------------------------------------------------------------------
-- Exames pendentes
-- ----------------------------------------------------------------------------
ALTER TABLE "ExamesPendentes"    ALTER COLUMN "DataIni"     TYPE date USING ("DataIni"     AT TIME ZONE 'UTC')::date;

-- ----------------------------------------------------------------------------
-- Itens (entrega parcial)
-- ----------------------------------------------------------------------------
ALTER TABLE "ItensExamesRealizados"   ALTER COLUMN "DataEntregaParcial" TYPE date USING ("DataEntregaParcial" AT TIME ZONE 'UTC')::date;
ALTER TABLE "ItensExamesRealizadosAM" ALTER COLUMN "DataEntregaParcial" TYPE date USING ("DataEntregaParcial" AT TIME ZONE 'UTC')::date;

-- ----------------------------------------------------------------------------
-- Fichas de trabalho
-- ----------------------------------------------------------------------------
ALTER TABLE "FichasInternas"   ALTER COLUMN "DataExame" TYPE date USING ("DataExame" AT TIME ZONE 'UTC')::date;
ALTER TABLE "FichasInternas"   ALTER COLUMN "DataIni"   TYPE date USING ("DataIni"   AT TIME ZONE 'UTC')::date;
ALTER TABLE "FichasInternas"   ALTER COLUMN "DataFim"   TYPE date USING ("DataFim"   AT TIME ZONE 'UTC')::date;

ALTER TABLE "FichasLotes"      ALTER COLUMN "DataExame" TYPE date USING ("DataExame" AT TIME ZONE 'UTC')::date;
ALTER TABLE "FichasLotes"      ALTER COLUMN "DataIni"   TYPE date USING ("DataIni"   AT TIME ZONE 'UTC')::date;
ALTER TABLE "FichasLotes"      ALTER COLUMN "DataFim"   TYPE date USING ("DataFim"   AT TIME ZONE 'UTC')::date;

ALTER TABLE "FichasPlanilhas"  ALTER COLUMN "DataExame" TYPE date USING ("DataExame" AT TIME ZONE 'UTC')::date;
ALTER TABLE "FichasPlanilhas"  ALTER COLUMN "DataIni"   TYPE date USING ("DataIni"   AT TIME ZONE 'UTC')::date;
ALTER TABLE "FichasPlanilhas"  ALTER COLUMN "DataFim"   TYPE date USING ("DataFim"   AT TIME ZONE 'UTC')::date;

-- ----------------------------------------------------------------------------
-- Exportação/impressão e catálogo de recebimentos (formas)
-- ----------------------------------------------------------------------------
ALTER TABLE "ExamesExportados"          ALTER COLUMN "DataColeta"      TYPE date USING ("DataColeta"      AT TIME ZONE 'UTC')::date;
ALTER TABLE "ExamesImpressos"           ALTER COLUMN "DataExame"       TYPE date USING ("DataExame"       AT TIME ZONE 'UTC')::date;
ALTER TABLE "CatalogoRecebimentosFormas" ALTER COLUMN "DataRecebimento" TYPE date USING ("DataRecebimento" AT TIME ZONE 'UTC')::date;

-- ----------------------------------------------------------------------------
-- Verificação rápida após a migração (deve retornar apenas 'date'):
--   SELECT table_name, column_name, data_type
--   FROM information_schema.columns
--   WHERE (table_name, column_name) IN (
--       ('Pacientes','Nascimento'), ('Pacientes','DUM'), ('Pacientes','DataEntradaBrasil'),
--       ('Pacientes','DataEntrada'), ('Pacientes','DataBaixa'),
--       ('UsuariosWeb','DataNascimentoUsuario'),
--       ('ExamesRealizados','DataIni'), ('ExamesRealizados','DataFim'),
--       ('ExamesRealizados','DataExame'), ('ExamesRealizados','DataEntrega'),
--       ('ExamesRealizadosAM','DataIni'), ('ExamesRealizadosAM','DataFim'),
--       ('ExamesRealizadosAM','DataExame'), ('ExamesRealizadosAM','DataEntrega'),
--       ('ERTemporario','DataIni'), ('ERTemporario','DataFim'),
--       ('ERTemporario','DataExame'), ('ERTemporario','DataEntrega'),
--       ('ExamesPendentes','DataIni'),
--       ('ItensExamesRealizados','DataEntregaParcial'),
--       ('ItensExamesRealizadosAM','DataEntregaParcial'),
--       ('FichasInternas','DataExame'), ('FichasInternas','DataIni'), ('FichasInternas','DataFim'),
--       ('FichasLotes','DataExame'), ('FichasLotes','DataIni'), ('FichasLotes','DataFim'),
--       ('FichasPlanilhas','DataExame'), ('FichasPlanilhas','DataIni'), ('FichasPlanilhas','DataFim'),
--       ('ExamesExportados','DataColeta'), ('ExamesImpressos','DataExame'),
--       ('CatalogoRecebimentosFormas','DataRecebimento'))
--   ORDER BY table_name, column_name;
-- ----------------------------------------------------------------------------

COMMIT;

-- ============================================================================
-- ROLLBACK (emergência): converter de volta para TIMESTAMPTZ.
-- ATENÇÃO: o horário original é PERDIDO na ida para date; a volta reconstrói
-- apenas a meia-noite da data, em UTC. Só use se for estritamente necessário.
--   Ex.: ALTER TABLE "Pacientes" ALTER COLUMN "DUM"
--        TYPE timestamptz USING ("DUM"::date AT TIME ZONE 'UTC');
-- ============================================================================
