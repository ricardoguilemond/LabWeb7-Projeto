-- Migração: Remanejamento do menu — criar grupo "Faturamento" com subopções Manutenção e Relatório
-- Remove o grupo "Relatório" antigo (que continha Faturamento) e cria grupo "Faturamento" estruturado
-- Data: 2026

-- ======================================================================
-- PASSO 1: Limpar execuções anteriores desta migração (idempotente)
-- Remove grupo "Faturamento" e seus subitens caso já existam
-- ======================================================================
DO $$
DECLARE
    v_coluna_fat INTEGER;
BEGIN
    -- Encontrar a Coluna do grupo "Faturamento" existente
    SELECT "Coluna" INTO v_coluna_fat
    FROM "ControleDePerfilMenu"
    WHERE "Menu" = 'Faturamento' AND "Nivel" = '000'
    LIMIT 1;

    IF v_coluna_fat IS NOT NULL THEN
        DELETE FROM "ControleDePerfilMenu" WHERE "Coluna" = v_coluna_fat;
    END IF;
END
$$;

-- ======================================================================
-- PASSO 2: Remover grupo "Relatório" antigo (que continha Faturamento)
-- Apenas remove grupos cujo header é "Relatório" e possui subitem
-- com "Faturamento" no nome
-- ======================================================================
DO $$
DECLARE
    v_coluna_rel INTEGER;
BEGIN
    SELECT m1."Coluna" INTO v_coluna_rel
    FROM "ControleDePerfilMenu" m1
    INNER JOIN "ControleDePerfilMenu" m2
        ON m1."Coluna" = m2."Coluna"
    WHERE m1."Menu" = 'Relatório' AND m1."Nivel" = '000'
      AND m2."Menu" ILIKE '%faturamento%'
    LIMIT 1;

    IF v_coluna_rel IS NOT NULL THEN
        DELETE FROM "ControleDePerfilMenu" WHERE "Coluna" = v_coluna_rel;
    END IF;
END
$$;

-- ======================================================================
-- PASSO 3: Inserir novo grupo "Faturamento" com subitens
-- ======================================================================
DO $$
DECLARE
    v_coluna INTEGER;
BEGIN
    -- Próxima Coluna disponível = MAX + 1
    SELECT COALESCE(MAX("Coluna"), 0) + 1 INTO v_coluna FROM "ControleDePerfilMenu";

    -- Grupo principal
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna, 'Faturamento', NULL, NULL, NULL, '000', 1);

    -- Subitem: Manutenção
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna, 'Manutenção', NULL, 'ManutencaoFaturamento', 'Index', '001', 1);

    -- Subitem: Relatório
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna, 'Relatório', NULL, 'RelatorioFaturamento', 'Index', '002', 1);
END
$$;
