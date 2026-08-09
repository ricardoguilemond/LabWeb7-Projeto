-- Remover grupo "Relatório(s)" órfão do menu lateral
-- Executar após 002_menu_faturamento.sql
-- Seguro: só remove se não houver subitens exclusivos restantes

DO $$
DECLARE
    v_coluna_rel INTEGER;
    v_qtd_exclusivos INTEGER;
BEGIN
    -- Localizar grupo cujo header é "Relatório" ou "Relatórios"
    SELECT "Coluna" INTO v_coluna_rel
    FROM "ControleDePerfilMenu"
    WHERE "Menu" IN ('Relatório', 'Relatórios')
      AND "Nivel" = '000'
    LIMIT 1;

    IF v_coluna_rel IS NULL THEN
        RAISE NOTICE 'Nenhum grupo Relatório(s) encontrado. Nada a fazer.';
        RETURN;
    END IF;

    -- Verificar se há subitens exclusivos (que NÃO existem no grupo Faturamento)
    SELECT COUNT(*) INTO v_qtd_exclusivos
    FROM "ControleDePerfilMenu" m
    WHERE m."Coluna" = v_coluna_rel
      AND m."Nivel" != '000'
      AND NOT EXISTS (
          SELECT 1
          FROM "ControleDePerfilMenu" f
          INNER JOIN "ControleDePerfilMenu" fg
              ON fg."Menu" = 'Faturamento' AND fg."Nivel" = '000'
          WHERE f."Coluna" = fg."Coluna"
            AND f."Controller" = m."Controller"
            AND f."Action" = m."Action"
      );

    IF v_qtd_exclusivos > 0 THEN
        RAISE NOTICE 'Grupo Relatórios possui % subitem(ns) exclusivo(s). Remoção NÃO executada por segurança.', v_qtd_exclusivos;
        RETURN;
    END IF;

    -- Remover todos os registros do grupo (header + subitens)
    DELETE FROM "ControleDePerfilMenu"
    WHERE "Coluna" = v_coluna_rel;

    RAISE NOTICE 'Grupo Relatório(s) (Coluna %) removido com sucesso.', v_coluna_rel;
END
$$;
