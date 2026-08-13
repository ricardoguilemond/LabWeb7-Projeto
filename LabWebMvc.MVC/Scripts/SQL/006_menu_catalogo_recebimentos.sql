-- Migração: Adicionar itens do Catálogo de Recebimentos ao menu existente
-- Adiciona subitens no grupo "Faturamento" já criado pela migração 002.
-- Data: 2026

DO $$
DECLARE
    v_coluna INTEGER;
BEGIN
    -- Localiza a coluna do grupo "Faturamento"
    SELECT "Coluna" INTO v_coluna
    FROM "ControleDePerfilMenu"
    WHERE "Menu" = 'Faturamento' AND "Nivel" = '000'
    LIMIT 1;

    IF v_coluna IS NULL THEN
        RAISE NOTICE 'Grupo Faturamento nao encontrado. Nenhum item foi inserido.';
        RETURN;
    END IF;

    -- Remove itens anteriores desta migração para garantir idempotência
    DELETE FROM "ControleDePerfilMenu"
    WHERE "Coluna" = v_coluna
      AND "Nivel" IN ('003', '004', '005', '006', '007')
      AND "Menu" IN ('Contas de Recebimento', 'Formas de Recebimento', 'Catálogo de Recebimentos', 'Consulta de Recebimentos', 'Relatório de Recebimentos');

    -- Subitem: Contas de Recebimento
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna, 'Contas de Recebimento', NULL, 'ContasRecebimento', 'Index', '003', 1);

    -- Subitem: Formas de Recebimento
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna, 'Formas de Recebimento', NULL, 'FormasRecebimento', 'Index', '004', 1);

    -- Subitem: Catálogo de Recebimentos
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna, 'Catálogo de Recebimentos', NULL, 'CatalogoRecebimentos', 'Index', '005', 1);

    -- Subitem: Consulta de Recebimentos
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna, 'Consulta de Recebimentos', NULL, 'CatalogoRecebimentos', 'Consulta', '006', 1);

    -- Subitem: Relatório de Recebimentos
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna, 'Relatório de Recebimentos', NULL, 'CatalogoRecebimentos', 'Relatorio', '007', 1);
END
$$;
