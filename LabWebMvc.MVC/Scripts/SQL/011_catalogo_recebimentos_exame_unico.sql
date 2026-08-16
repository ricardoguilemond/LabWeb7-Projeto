-- Migração: Garantir que um exame conste em um único catálogo de recebimentos
-- Objetivo: impedir registro duplicado de recebimento (duplo clique no Confirmar).
--           Um exame só pode estar vinculado a UM catálogo; o segundo INSERT
--           concorrente falha no banco (índice único), mesmo sob corrida de requisições.
-- Data: 16/08/2026 (Qoder)

-- ==============================================================================
-- PASSO 1: Remover catálogos duplicados do mesmo exame (mantém o menor Id).
-- O DELETE em "CatalogoRecebimentos" cascateia vínculos e formas (ON DELETE CASCADE).
-- ==============================================================================

DELETE FROM "CatalogoRecebimentos" c
WHERE EXISTS (
    SELECT 1
    FROM "CatalogoRecebimentosExames" e1
    WHERE e1."CatalogoRecebimentoId" = c."Id"
      AND EXISTS (
          SELECT 1
          FROM "CatalogoRecebimentosExames" e2
          WHERE e2."ExameRealizadoId" = e1."ExameRealizadoId"
            AND e2."CatalogoRecebimentoId" < c."Id"
      )
);

-- ==============================================================================
-- PASSO 2: Índice único — trava definitiva no banco contra recebimento em dobro.
-- ==============================================================================

CREATE UNIQUE INDEX IF NOT EXISTS "idx_u_CatalogoRecebimentosExames_Exame"
    ON "CatalogoRecebimentosExames" ("ExameRealizadoId");
