-- ==============================================================================
-- SCRIPT: Menu
-- ==============================================================================
-- Objetivo: Popular a tabela ControleDePerfilMenu com todas as opções de menu
--           do sistema de forma idempotente.
--
-- Ordem canônica dos grupos do menu (regra de 23/08/2026):
--   Coluna 1  = Cadastros
--   Coluna 2  = Exames
--   Coluna 3  = Plano de Exames
--   Coluna 4  = Mapas de Trabalho
--   Coluna 5  = Controle de Acesso
--   Coluna 6  = Recebimentos
--   Coluna 7  = Faturamento
--   Coluna 8  = ReCaptcha
--   Coluna 9  = Manutenção
--   Coluna 10 = Carga de Dados
--   Coluna 11 = Sobre
--   Coluna 12 = Login/Logout
--
-- Regras:
--   R1. "Coluna" é o ordinal do grupo (única por grupo) e define a ordem de
--       exibição na barra de menus (o MenuDinamicoViewComponent ordena por
--       Coluna e depois por Nivel).
--   R2. Nivel '000' = pai do grupo (Area/Controller/Action NULL); filhos usam
--       '001', '002', ... na ordem de exibição dentro do grupo.
--   R3. Idempotência por reconstrução total: o script dá DELETE na tabela e
--       reinsere tudo. Executar SEMPRE o arquivo inteiro, nunca blocos parciais.
--   R4. Mudanças de ordem, inclusão ou exclusão de grupos/itens são feitas
--       somente neste script e aplicadas reexecutando-o por inteiro no banco.
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
    -- GRUPO 4: Mapas de Trabalho
    -- Feito pelo Qoder em 23/08/2026 — portabilidade das 6 frentes de mapas
    -- de trabalho do sistema Delphi para um único controller (MapasTrabalho).
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (4, 'Mapas de Trabalho', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (4, 'Mapa Eletrônico', NULL, 'MapasTrabalho', 'MapaEletronico', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (4, 'Mapa Planilhado (Excel)', NULL, 'MapasTrabalho', 'MapaPlanilhado', '002', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (4, 'Mapa Agrupado', NULL, 'MapasTrabalho', 'MapaAgrupado', '003', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (4, 'Mapa Horizontal', NULL, 'MapasTrabalho', 'MapaHorizontal', '004', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (4, 'Mapa Meia-Folha', NULL, 'MapasTrabalho', 'MapaMeiaFolha', '005', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (4, 'Etiquetas', NULL, 'MapasTrabalho', 'Etiquetas', '006', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (4, 'Ficha 40 Colunas', NULL, 'MapasTrabalho', 'FichaQuarentaColunas', '007', 1);

    -- ======================================================================
    -- GRUPO 5: Controle de Acesso
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (5, 'Controle de Acesso', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (5, 'Usuários', NULL, 'Senhas', 'Index', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (5, 'Perfil/Permissões', NULL, NULL, NULL, '002', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (5, 'Auditoria', NULL, NULL, NULL, '003', 1);

    -- ======================================================================
    -- GRUPO 6: Recebimentos
    -- Feito pelo Qoder em 23/08/2026 — grupo próprio (antes vivia dentro do
    -- grupo Faturamento); mantém os 5 itens do catálogo de recebimentos.
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (6, 'Recebimentos', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (6, 'Contas de Recebimento', NULL, 'ContasRecebimento', 'Index', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (6, 'Formas de Recebimento', NULL, 'FormasRecebimento', 'Index', '002', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (6, 'Catálogo de Recebimentos', NULL, 'CatalogoRecebimentos', 'Index', '003', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (6, 'Consulta de Recebimentos', NULL, 'CatalogoRecebimentos', 'Consulta', '004', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (6, 'Relatório de Recebimentos', NULL, 'CatalogoRecebimentos', 'Relatorio', '005', 1);

    -- ======================================================================
    -- GRUPO 7: Faturamento
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (7, 'Faturamento', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (7, 'Manutenção', NULL, 'ManutencaoFaturamento', 'Index', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (7, 'Relatório', NULL, 'RelatorioFaturamento', 'Index', '002', 1);

    -- ======================================================================
    -- GRUPO 8: ReCaptcha
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (8, 'ReCaptcha', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (8, 'Gráfico ReCaptcha', NULL, 'Graficos', 'GraficoReCaptcha', '001', 1);

    -- ======================================================================
    -- GRUPO 9: Manutenção
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (9, 'Manutenção', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (9, 'Configurações', NULL, 'Configuracoes', 'Index', '001', 1);
    --Feito pelo Qoder em 23/08/2026 — item "Compactar Requisições" removido: a tabela Requisitar foi
    --extinta e a tela excluída do sistema (Manutencao/CompactarRequisicoes não existe mais).

    -- ======================================================================
    -- GRUPO 10: Carga de Dados
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (10, 'Carga de Dados', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (10, 'Implantação', NULL, 'CargaDados', 'Index', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (10, 'Importar Referências', NULL, 'Manutencao', 'ImportarReferencias', '002', 1);

    -- ======================================================================
    -- GRUPO 11: Sobre
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (11, 'Sobre', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (11, 'Privacidade', NULL, 'Home', 'Privacy', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (11, 'Nosso Sistema', NULL, 'Home', 'NossoSistema', '002', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (11, 'Versão Ambiente', NULL, 'Release', 'Release', '003', 1);

    -- ======================================================================
    -- GRUPO 12: Login / Logout
    -- ======================================================================
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (12, 'Login/Logout', NULL, NULL, NULL, '000', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (12, 'Login', NULL, 'Home', 'Login', '001', 1);
    INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (12, 'Logout', NULL, 'Home', 'Logout', '002', 1);
END $$;
