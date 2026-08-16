-- Migração: Mover itens de Recebimentos do grupo "Faturamento" para grupo próprio "Recebimentos"
-- Os subitens Contas de Recebimento, Formas de Recebimento, Catálogo de Recebimentos,
-- Consulta de Recebimentos e Relatório de Recebimentos saem do grupo Faturamento (migração 002/006)
-- e passam a formar o grupo próprio "Recebimentos".
-- Idempotente: pode ser reexecutada sem duplicar registros.
-- Data: 16/08/2026

DO $$
DECLARE
    v_coluna_fat INTEGER;
    v_coluna_rec INTEGER;
BEGIN
    -- Localiza a coluna do grupo "Faturamento"
    SELECT "Coluna" INTO v_coluna_fat
    FROM "ControleDePerfilMenu"
    WHERE "Menu" = 'Faturamento' AND "Nivel" = '000' AND "Ativo" = 1
    LIMIT 1;

    IF v_coluna_fat IS NULL THEN
        RAISE NOTICE 'Grupo Faturamento nao encontrado. Migração abortada.';
        RETURN;
    END IF;

    -- Remove o grupo "Recebimentos" e seus subitens criados em execuções anteriores (idempotência)
    SELECT "Coluna" INTO v_coluna_rec
    FROM "ControleDePerfilMenu"
    WHERE "Menu" = 'Recebimentos' AND "Nivel" = '000'
    LIMIT 1;

    IF v_coluna_rec IS NOT NULL THEN
        DELETE FROM "ControleDePerfilMenu" WHERE "Coluna" = v_coluna_rec;
    END IF;

    -- Remove os itens de recebimento que estavam no grupo Faturamento (migração 006)
    DELETE FROM "ControleDePerfilMenu"
    WHERE "Coluna" = v_coluna_fat
      AND "Controller" IN ('ContasRecebimento', 'FormasRecebimento', 'CatalogoRecebimentos');

    -- Próxima Coluna disponível = MAX + 1
    SELECT COALESCE(MAX("Coluna"), 0) + 1 INTO v_coluna_rec FROM "ControleDePerfilMenu";

    -- Grupo principal "Recebimentos"
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna_rec, 'Recebimentos', NULL, NULL, NULL, '000', 1);

    -- Subitem: Contas de Recebimento
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna_rec, 'Contas de Recebimento', NULL, 'ContasRecebimento', 'Index', '001', 1);

    -- Subitem: Formas de Recebimento
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna_rec, 'Formas de Recebimento', NULL, 'FormasRecebimento', 'Index', '002', 1);

    -- Subitem: Catálogo de Recebimentos
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna_rec, 'Catálogo de Recebimentos', NULL, 'CatalogoRecebimentos', 'Index', '003', 1);

    -- Subitem: Consulta de Recebimentos
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna_rec, 'Consulta de Recebimentos', NULL, 'CatalogoRecebimentos', 'Consulta', '004', 1);

    -- Subitem: Relatório de Recebimentos
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna_rec, 'Relatório de Recebimentos', NULL, 'CatalogoRecebimentos', 'Relatorio', '005', 1);
END
$$;
