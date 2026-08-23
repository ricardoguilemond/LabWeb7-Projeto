-- =====================================================================
-- Correção do campo MedicoId em Requisitar
-- Criado pelo Kiro em 19/07/2026
--
-- CONTEXTO:
-- Requisitar é a junção de ExamesRealizados (header) + ItensExamesRealizados
-- (itens). A tabela de origem no Firebird (RequisicaoOriginal) NÃO possui o
-- campo MedicoResp — o médico está apenas em ExamesRealizados.
-- Logo, Requisitar.MedicoId deve ser obtido de ExamesRealizados.MedicoId via
-- ExameRealizadoId (vínculo lógico Requisitar.ExameRealizadoId = ExamesRealizados.Id).
--
-- REGRA DE DISPENSA:
-- Registros de Requisitar que NÃO alcancem MedicoId em ExamesRealizados
-- (ExameRealizadoId NULL, ou ExamesRealizados correspondente ausente,
--  ou ExamesRealizados.MedicoId NULL) são DISPENSADOS (deletados).
--
-- USO:
-- 1) Rodar a SEÇÃO 1 (PRÉ-PROCESSAMENTO) antes de reimportar a tabela Requisitar.
-- 2) Reimportar apenas a tabela Requisitar (Carga de Dados / Kiro).
-- 3) Rodar a SEÇÃO 2 (PÓS-PROCESSAMENTO) após a reimportação.
-- =====================================================================


-- #####################################################################
-- SEÇÃO 1: PRÉ-PROCESSAMENTO (executar ANTES da reimportação)
-- #####################################################################

-- 1.1 Garantir que MedicoId seja NULLABLE durante a importação.
--     Como RequisicaoOriginal não tem MedicoResp, a importação insere NULL em
--     MedicoId. Se a coluna for NOT NULL, TODOS os INSERTs falham (23502).
--     Tornando-a nullable, os registros são inseridos com NULL e preenchidos
--     no pós-processamento.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Requisitar'
          AND column_name = 'MedicoId'
          AND is_nullable = 'NO'
    ) THEN
        ALTER TABLE "Requisitar" ALTER COLUMN "MedicoId" DROP NOT NULL;
        RAISE NOTICE 'Requisitar.MedicoId: NOT NULL removido (nullable) para importação.';
    ELSE
        RAISE NOTICE 'Requisitar.MedicoId: já é nullable.';
    END IF;
END $$;

-- 1.2 Remover DEFAULT 0 (se existir) para que NULLs não sejam mascarados.
--     Com DEFAULT 0, INSERTs sem a coluna gerariam 0 (não NULL). Removendo o
--     default, os INSERTs geram NULL — distinto de um MedicoId válido.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Requisitar'
          AND column_name = 'MedicoId'
          AND column_default IS NOT NULL
    ) THEN
        ALTER TABLE "Requisitar" ALTER COLUMN "MedicoId" DROP DEFAULT;
        RAISE NOTICE 'Requisitar.MedicoId: DEFAULT removido.';
    ELSE
        RAISE NOTICE 'Requisitar.MedicoId: sem DEFAULT (ok).';
    END IF;
END $$;

-- 1.3 Remover a FK física temporariamente (se existir) para evitar violação
--     durante a importação. Os NULLs não violam FK, mas 0 (se sobrar algum
--     DEFAULT) violaria. Remover a FK isola a importação.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE table_schema = 'public'
          AND constraint_name = 'iRequisitar_Medicos'
    ) THEN
        ALTER TABLE "Requisitar" DROP CONSTRAINT "iRequisitar_Medicos";
        RAISE NOTICE 'FK iRequisitar_Medicos removida temporariamente.';
    ELSE
        RAISE NOTICE 'FK iRequisitar_Medicos já não existe.';
    END IF;
END $$;

-- >>> REIMPORTAR A TABELA REQUISITAR AGORA (Carga de Dados) <<<


-- #####################################################################
-- SEÇÃO 2: PÓS-PROCESSAMENTO (executar APÓS a reimportação)
-- #####################################################################

-- 2.1 Diagnóstico pré-correção.
SELECT 'ANTES' AS fase,
       COUNT(*) AS total,
       COUNT(*) FILTER (WHERE "MedicoId" IS NULL) AS medicoid_null,
       COUNT(*) FILTER (WHERE "MedicoId" = 0) AS medicoid_zero,
       COUNT(*) FILTER (WHERE "ExameRealizadoId" IS NULL) AS exame_realizado_id_null
FROM "Requisitar";

-- 2.2 Backfill: obter MedicoId de ExamesRealizados via ExameRealizadoId.
--     Cadeia: Requisitar.ExameRealizadoId = ExamesRealizados.Id → ExamesRealizados.MedicoId
--     Requisitar é a junção de ExamesRealizados (header) + ItensExamesRealizados (itens),
--     então MedicoId pertence ao header e é espelhado aqui.
UPDATE "Requisitar" r
SET "MedicoId" = er."MedicoId"
FROM "ExamesRealizados" er
WHERE r."ExameRealizadoId" = er."Id"
  AND (r."MedicoId" IS NULL OR r."MedicoId" = 0)
  AND er."MedicoId" IS NOT NULL;

-- 2.3 Dispensar registros órfãos: aqueles que NÃO alcançam MedicoId em
--     ExamesRealizados (ExameRealizadoId NULL, ou ExamesRealizados ausente,
--     ou ExamesRealizados.MedicoId NULL). Estes registros não têm propósito
--     em Requisitar sem o médico do header.
DELETE FROM "Requisitar"
WHERE "MedicoId" IS NULL
   OR "MedicoId" = 0;

-- 2.4 Garantir NOT NULL (alinhamento com schema — não há mais NULLs/0s).
ALTER TABLE "Requisitar" ALTER COLUMN "MedicoId" SET NOT NULL;

-- 2.5 Recriar a FK física iRequisitar_Medicos (MedicoId → Medicos.Id),
--     alinhando com Tabelas_Vazias.sql.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE table_schema = 'public'
          AND constraint_name = 'iRequisitar_Medicos'
    ) THEN
        ALTER TABLE "Requisitar"
            ADD CONSTRAINT "iRequisitar_Medicos"
            FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id");
        RAISE NOTICE 'FK iRequisitar_Medicos recriada.';
    ELSE
        RAISE NOTICE 'FK iRequisitar_Medicos já existe.';
    END IF;
END $$;

-- 2.6 Diagnóstico pós-correção.
SELECT 'DEPOIS' AS fase,
       COUNT(*) AS total,
       COUNT(*) FILTER (WHERE "MedicoId" IS NULL) AS medicoid_null,
       COUNT(*) FILTER (WHERE "MedicoId" = 0) AS medicoid_zero,
       COUNT(*) FILTER (WHERE "ExameRealizadoId" IS NULL) AS exame_realizado_id_null
FROM "Requisitar";

-- 2.7 Validação de integridade: confirmar que todo MedicoId existe em Medicos.
--     Esperado: 0 (zero órfãos de FK).
SELECT 'ORFAOS_FK' AS verificacao,
       COUNT(*) AS medicoid_inexistente_em_medicos
FROM "Requisitar" r
WHERE NOT EXISTS (SELECT 1 FROM "Medicos" m WHERE m."Id" = r."MedicoId");
