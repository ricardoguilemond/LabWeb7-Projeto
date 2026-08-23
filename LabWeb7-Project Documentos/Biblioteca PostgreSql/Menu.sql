-- ==============================================================================
-- SCRIPT: Menu
-- ==============================================================================
-- Objetivo: Popular a tabela ControleDePerfilMenu com todas as opções de menu
--           do sistema de forma idempotente.
--
-- Dependências:
--   - A tabela "ControleDePerfilMenu" deve existir (criada em "Controle de Acesso.sql").
--
-- Ordem de execução recomendada na implantação:
--   1. Tabelas_Vazias.sql
--   2. Controle de Acesso.sql
--   3. Menu.sql
-- ==============================================================================

DO $$
BEGIN
    -- ======================================================================
    -- Limpa o menu atual para garantir que fique idêntico a este script
    -- ======================================================================
    DELETE FROM "ControleDePerfilMenu";

    -- ======================================================================
    -- GRUPO 1: Cadastros
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (1, 'Cadastros', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (1, 'Pacientes', NULL, 'Pacientes', 'Index', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (1, 'Médicos', NULL, 'Medicos', 'Index', '002', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (1, 'Instituições', NULL, 'Instituicoes', 'Index', '003', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (1, 'Postos', NULL, 'Postos', 'Index', '004', 1);

    -- ======================================================================
    -- GRUPO 2: Exames
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (2, 'Exames', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (2, 'Requisição', NULL, 'Requisitar', 'Index', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (2, 'Consultar Exames', NULL, 'ConsultarExames', 'Index', '002', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (2, 'Resultados', NULL, 'ResultadoExames', 'Index', '003', 1);

    -- ======================================================================
    -- GRUPO 3: Plano de Exames
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (3, 'Plano de Exames', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (3, 'Folha de Exames', NULL, 'ClasseExames', 'Index', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (3, 'Plano de Exames', NULL, 'PlanoExames', 'Index', '002', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (3, 'Tabela de Preços', NULL, 'PlanoExamesItens', 'Index', '003', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (3, 'Referências de Laudos', NULL, 'ExameReferencia', 'Index', '004', 1);

    -- ======================================================================
    -- GRUPO 4: Carga de Dados
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (4, 'Carga de Dados', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (4, 'Implantação', NULL, 'CargaDados', 'Index', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (4, 'Importar Referências', NULL, 'Manutencao', 'ImportarReferencias', '002', 1);

    -- ======================================================================
    -- GRUPO 5: Controle de Acesso
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (5, 'Controle de Acesso', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (5, 'Usuários', NULL, 'Senhas', 'Index', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (5, 'Perfil/Permissões', NULL, NULL, NULL, '002', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (5, 'Auditoria', NULL, NULL, NULL, '003', 1);

    -- ======================================================================
    -- GRUPO 6: ReCaptcha
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (6, 'ReCaptcha', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (6, 'Gráfico ReCaptcha', NULL, 'Graficos', 'GraficoReCaptcha', '001', 1);

    -- ======================================================================
    -- GRUPO 7: Manutenção
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (7, 'Manutenção', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (7, 'Configurações', NULL, 'Configuracoes', 'Index', '001', 1);
    --Feito pelo Qoder em 23/08/2026 — item "Compactar Requisições" removido: a tabela Requisitar foi
    --extinta e a tela excluída do sistema (Manutencao/CompactarRequisicoes não existe mais).

    -- ======================================================================
    -- GRUPO 8: Sobre
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (8, 'Sobre', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (8, 'Privacidade', NULL, 'Home', 'Privacy', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (8, 'Nosso Sistema', NULL, 'Home', 'NossoSistema', '002', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (8, 'Versão Ambiente', NULL, 'Release', 'Release', '003', 1);

    -- ======================================================================
    -- GRUPO 9: Login / Logout
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (9, 'Login/Logout', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (9, 'Login', NULL, 'Home', 'Home/Login', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (9, 'Logout', NULL, 'Home', 'Logout', '002', 1);

    -- ======================================================================
    -- GRUPO 10: Faturamento
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (10, 'Faturamento', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (10, 'Manutenção', NULL, 'ManutencaoFaturamento', 'Index', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (10, 'Relatório', NULL, 'RelatorioFaturamento', 'Index', '002', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (10, 'Contas de Recebimento', NULL, 'ContasRecebimento', 'Index', '003', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (10, 'Formas de Recebimento', NULL, 'FormasRecebimento', 'Index', '004', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (10, 'Catálogo de Recebimentos', NULL, 'CatalogoRecebimentos', 'Index', '005', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (10, 'Consulta de Recebimentos', NULL, 'CatalogoRecebimentos', 'Consulta', '006', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (10, 'Relatório de Recebimentos', NULL, 'CatalogoRecebimentos', 'Relatorio', '007', 1);
END $$;
