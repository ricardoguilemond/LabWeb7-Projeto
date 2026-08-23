-- Script definitivo para corrigir o menu Carga de Dados
-- RECRIA a coluna 4 do zero, eliminando qualquer duplicata ou lixo.
-- ATENCAO: executar APENAS uma vez. A coluna 4 e seus subitens serao apagados e recriados.

DO $$
DECLARE
    v_coluna integer := 4;
BEGIN
    -- Apaga TUDO da coluna 4 (presumivelmente o grupo Carga de Dados e seus subitens)
    DELETE FROM "ControleDePerfilMenu"
    WHERE "Coluna" = v_coluna;

    -- Recria o grupo Carga de Dados
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna, 'Carga de Dados', NULL, NULL, NULL, '000', 1);

    -- Recria o subitem Implantação apontando para o novo controller
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna, 'Implantação', 'Areas', 'CargaDados', 'Index', '001', 1);

    -- Recria o subitem Importar Referencias
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo")
    VALUES (v_coluna, 'Importar Referências', NULL, 'Manutencao', 'ImportarReferencias', '002', 1);
END $$;

-- Verificacao
SELECT * FROM "ControleDePerfilMenu"
WHERE "Coluna" = 4
ORDER BY "Nivel";

-- Verificacao de todos os menus
SELECT * FROM "ControleDePerfilMenu"
ORDER BY "Coluna", "Nivel";
