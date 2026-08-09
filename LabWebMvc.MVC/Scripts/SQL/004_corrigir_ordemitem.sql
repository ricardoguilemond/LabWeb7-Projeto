-- Correção de OrdemItem: atribui sequência 1,2,3... por exame
-- O campo OrdemItem não existia no Firebird — todos os registros importados
-- receberam DEFAULT 0, tornando a ordenação indeterminada.
-- Este script reatribui OrdemItem sequencial ordenado por ContaExame
-- (ordem natural dos itens no plano de exames).
-- Data: 2026

-- ======================================================================
-- 1. ItensExamesRealizados (exames ativos)
-- ======================================================================
UPDATE "ItensExamesRealizados"
SET "OrdemItem" = sub.nova_ordem
FROM (
    SELECT "Id",
           ROW_NUMBER() OVER (
               PARTITION BY "ExameRealizadoId"
               ORDER BY "ContaExame"
           ) AS nova_ordem
    FROM "ItensExamesRealizados"
) sub
WHERE "ItensExamesRealizados"."Id" = sub."Id";

-- ======================================================================
-- 2. ItensExamesRealizadosAM (exames arquivados)
-- ======================================================================
UPDATE "ItensExamesRealizadosAM"
SET "OrdemItem" = sub.nova_ordem
FROM (
    SELECT "Id",
           ROW_NUMBER() OVER (
               PARTITION BY "ExameRealizadoAMId"
               ORDER BY "ContaExame"
           ) AS nova_ordem
    FROM "ItensExamesRealizadosAM"
) sub
WHERE "ItensExamesRealizadosAM"."Id" = sub."Id";
