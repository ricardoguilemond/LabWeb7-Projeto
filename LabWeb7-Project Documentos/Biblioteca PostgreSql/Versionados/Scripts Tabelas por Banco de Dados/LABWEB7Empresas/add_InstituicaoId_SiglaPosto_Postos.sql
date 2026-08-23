-- =====================================================================
-- Script de migração: vincular Postos a Instituicao + adicionar SiglaPosto
-- Aplicar em CADA banco de empresa-cliente do LABWEB7Empresas em homologação.
-- Estratégia (aprovada):
--   D1) Postos órfãos (sem nenhum exame) → EXCLUIR.
--   D2) Postos compartilhados entre Instituicoes → DUPLICAR (uma cópia por
--       Instituicao adicional) e religar exames das 3 tabelas.
--   D3) Inferência ampliada: ExamesRealizados → ExamesRealizadosAM → Requisitar.
--   D4) Adicionar coluna SiglaPosto VARCHAR(20) NOT NULL.
--   D6) SiglaPosto = UPPER(NomePosto) sem acentos, restrito a [A-Z0-9 ._-].
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- 1) Colunas novas (nullable temporariamente)
-- ---------------------------------------------------------------------
ALTER TABLE "Postos" ADD COLUMN IF NOT EXISTS "InstituicaoId" INT;
ALTER TABLE "Postos" ADD COLUMN IF NOT EXISTS "SiglaPosto"   VARCHAR(20);

-- ---------------------------------------------------------------------
-- 2) Inferência ampliada (D3): primeiro exame que referencia o Posto.
--    Prioridade: ExamesRealizados (1) > ExamesRealizadosAM (2) > Requisitar (3)
-- ---------------------------------------------------------------------
WITH inferencia AS (
    SELECT DISTINCT ON (sub."PostoId") sub."PostoId", sub."InstituicaoId"
    FROM (
        SELECT "PostoId","InstituicaoId","Id", 1 AS prio FROM "ExamesRealizados"   WHERE "PostoId" IS NOT NULL
        UNION ALL
        SELECT "PostoId","InstituicaoId","Id", 2 AS prio FROM "ExamesRealizadosAM" WHERE "PostoId" IS NOT NULL
        UNION ALL
        SELECT "PostoId","InstituicaoId","Id", 3 AS prio FROM "Requisitar"         WHERE "PostoId" IS NOT NULL
    ) sub
    ORDER BY sub."PostoId", sub.prio ASC, sub."Id" ASC
)
UPDATE "Postos" p
SET "InstituicaoId" = i."InstituicaoId"
FROM inferencia i
WHERE p."Id" = i."PostoId";

-- ---------------------------------------------------------------------
-- 3) Excluir Postos órfãos (D1) — sem exame em nenhuma das 3 tabelas
-- ---------------------------------------------------------------------
DELETE FROM "Postos" WHERE "InstituicaoId" IS NULL;

-- ---------------------------------------------------------------------
-- 4) Duplicar Postos compartilhados entre Instituicoes (D2)
--    Para cada combinação (PostoId, InstituicaoId-extra) que difere do
--    InstituicaoId atual do Posto, cria nova cópia (gap-filling) e religa
--    os exames daquela Instituicao.
-- ---------------------------------------------------------------------
DO $$
DECLARE
    r RECORD;
    novo_id INT;
BEGIN
    FOR r IN
        SELECT DISTINCT t."PostoId", t."InstituicaoId" AS inst_extra,
               p."NomePosto", p."Responsavel", p."Telefone", p."Endereco",
               p."Logradouro", p."Numero", p."Bairro", p."Complemento",
               p."Cidade", p."UF", p."CEP"
        FROM (
            SELECT "PostoId","InstituicaoId" FROM "ExamesRealizados"   WHERE "PostoId" IS NOT NULL
            UNION
            SELECT "PostoId","InstituicaoId" FROM "ExamesRealizadosAM" WHERE "PostoId" IS NOT NULL
            UNION
            SELECT "PostoId","InstituicaoId" FROM "Requisitar"         WHERE "PostoId" IS NOT NULL
        ) t
        JOIN "Postos" p ON p."Id" = t."PostoId"
        WHERE t."InstituicaoId" <> p."InstituicaoId"
    LOOP
        -- Reaproveita o primeiro Id vago (gap-filling, padrão LabWeb7)
        SELECT COALESCE(
            (SELECT seq."n"
             FROM generate_series(1,(SELECT COALESCE(MAX("Id"),0)+1 FROM "Postos")) AS seq("n")
             WHERE NOT EXISTS (SELECT 1 FROM "Postos" px WHERE px."Id" = seq."n")
             ORDER BY seq."n"
             LIMIT 1),
            (SELECT COALESCE(MAX("Id"),0)+1 FROM "Postos")
        ) INTO novo_id;

        INSERT INTO "Postos" ("Id","NomePosto","Responsavel","Telefone","Endereco",
                              "Logradouro","Numero","Bairro","Complemento",
                              "Cidade","UF","CEP","InstituicaoId")
        VALUES (novo_id, r."NomePosto", r."Responsavel", r."Telefone", r."Endereco",
                r."Logradouro", r."Numero", r."Bairro", r."Complemento",
                r."Cidade", r."UF", r."CEP", r.inst_extra);

        UPDATE "ExamesRealizados"   SET "PostoId" = novo_id WHERE "PostoId" = r."PostoId" AND "InstituicaoId" = r.inst_extra;
        UPDATE "ExamesRealizadosAM" SET "PostoId" = novo_id WHERE "PostoId" = r."PostoId" AND "InstituicaoId" = r.inst_extra;
        UPDATE "Requisitar"         SET "PostoId" = novo_id WHERE "PostoId" = r."PostoId" AND "InstituicaoId" = r.inst_extra;
    END LOOP;
END $$;

-- ---------------------------------------------------------------------
-- 5) Preencher SiglaPosto (D6 + D7):
--    UPPER(NomePosto[1..20]) sem acentos, somente [A-Z0-9 ._-]
-- ---------------------------------------------------------------------
UPDATE "Postos"
SET "SiglaPosto" = UPPER(
    REGEXP_REPLACE(
        TRANSLATE(SUBSTRING("NomePosto" FROM 1 FOR 20),
                  'ÁÀÂÃÄÅÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇÑáàâãäåéèêëíìîïóòôõöúùûüçñ',
                  'AAAAAAEEEEIIIIOOOOOUUUUCNAAAAAAEEEEIIIIOOOOOUUUUCN'),
        '[^A-Za-z0-9 ._\-]', '', 'g'
    )
)
WHERE "SiglaPosto" IS NULL;

-- Garantia: Postos cujo NomePosto resulta em sigla vazia → preencher com "P" + Id
UPDATE "Postos"
SET "SiglaPosto" = ('P' || "Id"::text)
WHERE "SiglaPosto" IS NULL OR LENGTH(TRIM("SiglaPosto")) = 0;

-- ---------------------------------------------------------------------
-- 6) NOT NULL nas duas colunas
-- ---------------------------------------------------------------------
ALTER TABLE "Postos" ALTER COLUMN "InstituicaoId" SET NOT NULL;
ALTER TABLE "Postos" ALTER COLUMN "SiglaPosto"    SET NOT NULL;

-- ---------------------------------------------------------------------
-- 7) FK + índice de performance
-- ---------------------------------------------------------------------
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'iPostos_Instituicao') THEN
        ALTER TABLE "Postos"
            ADD CONSTRAINT "iPostos_Instituicao"
            FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id")
            ON DELETE RESTRICT;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS "iPostos_InstituicaoId" ON "Postos" ("InstituicaoId");

COMMIT;
