-- ===============================================
-- Banco: LABWEB7
-- Criação das tabelas de Controle de Acesso
-- Por: Ricardo Guilemond
-- ===============================================

-- ===============================
-- DROP das tabelas (em ordem de dependência)
-- ===============================
--DROP TABLE IF EXISTS UsuariosWeb;
--DROP TABLE IF EXISTS Senhas;
--DROP TABLE IF EXISTS ControleDePerfilMenu;
--DROP TABLE IF EXISTS ControleDePerfilModelo;
--DROP TABLE IF EXISTS ControleDePerfilTipo;
--DROP TABLE IF EXISTS ControleDePerfil;
--DROP TABLE IF EXISTS ControleDeAcesso;

-- ===============================
-- ControleDeAcesso
-- ===============================
CREATE TABLE IF NOT EXISTS "ControleDeAcesso" (
    "Id" SERIAL PRIMARY KEY,
    "SenhaId" INT NOT NULL UNIQUE -- restrição única relacionada com Senhas (1x1)
);

-- ===============================
-- ControleDePerfil
-- ===============================
CREATE TABLE IF NOT EXISTS "ControleDePerfil" (
    "Id" SERIAL PRIMARY KEY,
    "ControleDeAcessoId" INT NOT NULL REFERENCES "ControleDeAcesso"("Id"),
    "MenuNivelMenu" VARCHAR(3) NOT NULL,
    "MenuNivel1" VARCHAR(3) NOT NULL,
    "MenuNivel2" VARCHAR(3) NOT NULL,
    "MenuNivel3" VARCHAR(3) NOT NULL,
    "MenuNivel4" VARCHAR(3) NOT NULL,
    "Ativo" INT NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS "iControleDePerfil3"
ON "ControleDePerfil"("MenuNivelMenu", "MenuNivel1", "MenuNivel2", "MenuNivel3", "MenuNivel4");

-- ===============================
-- ControleDePerfilMenu
-- ===============================
CREATE TABLE IF NOT EXISTS "ControleDePerfilMenu" (
    "Id" SERIAL PRIMARY KEY,
    "Coluna" INT NOT NULL,
    "Menu" VARCHAR(100) NOT NULL,
    "Area" VARCHAR(100),
    "Controller" VARCHAR(100),
    "Action" VARCHAR(100),
    "Nivel" VARCHAR(3) NOT NULL,
    "Ativo" INT NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS "iControleDePerfilMenu2"
ON "ControleDePerfilMenu"("Coluna", "Nivel");

-- ===============================
-- Inserção dos menus principais
-- ===============================
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES
(1, 'Cadastros', NULL, NULL, NULL, '000', 1),
(2, 'Exames', NULL, NULL, NULL, '000', 1),
(3, 'Plano de Exames', NULL, NULL, NULL, '000', 1),
(4, 'Carga de Dados', NULL, NULL, NULL, '000', 1),
(5, 'Controle de Acesso', NULL, NULL, NULL, '000', 1),
(6, 'ReCaptcha', NULL, NULL, NULL, '000', 1),
(7, 'Configurações', NULL, NULL, NULL, '000', 1),
(8, 'Sobre', NULL, NULL, NULL, '000', 1),
(9, 'Login/Logout', NULL, NULL, NULL, '000', 1);

-- Secundários (Cadastros)
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES
(1, 'Pacientes', NULL, 'Pacientes', 'Index', '001', 1),
(1, 'Médicos', NULL, 'Medicos', 'Index', '002', 1),
(1, 'Instituições', NULL, 'Instituicoes', 'Index', '003', 1),
(1, 'Postos', NULL, 'Postos', 'Index', '004', 1);

-- Exames
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES
(2, 'Requisição', NULL, 'Requisitar', 'Index', '001', 1),
(2, 'Consultar Exames', NULL, 'ConsultarExames', 'Index', '002', 1),
(2, 'Cancelar Exames', NULL, '', '', '003', 1),
(2, 'Resultados', NULL, '', '', '004', 1);

-- Folhas e Planos de Exames
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES
(3, 'Folha de Exames', NULL, 'ClasseExames', 'Index', '001', 1),
(3, 'Plano de Exames', NULL, 'PlanoExames', 'Index', '002', 1),
(3, 'Tabela de Preços', NULL, 'PlanoExamesItens', 'Index', '003', 1);

-- Implantação
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES
(4, 'Implantação', NULL, 'Implantacao', 'Index', '001', 1);

-- Controle de Acesso
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES
(5, 'Usuários', NULL, 'Senhas', 'Index', '001', 1),
(5, 'Perfil/Permissões', NULL, '', '', '002', 1),
(5, 'Auditoria', NULL, '', '', '003', 1);

-- Gráficos
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES
(6, 'Gráfico ReCaptcha', NULL, 'Graficos', 'GraficoReCaptcha', '001', 1);

-- Configurações
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES
(7, 'Configurações', NULL, 'Configuracoes', 'Index', '001', 1);

-- Sobre
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES
(8, 'Privacidade', NULL, 'Home', 'Privacy', '001', 1),
(8, 'Nosso Sistema', NULL, 'Home', 'NossoSistema', '002', 1),
(8, 'Versão Ambiente', NULL, 'Release', 'Release', '003', 1);

-- Login/Logout
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES
(9, 'Login', NULL, 'Home', 'Home/Login', '001', 1),
(9, 'Logout', NULL, 'Home', 'Logout', '002', 1);

-- ===============================
-- ControleDePerfilModelo
-- ===============================
CREATE TABLE IF NOT EXISTS "ControleDePerfilModelo" (
    "Id" SERIAL PRIMARY KEY,
    "MenuNivel1" VARCHAR(3) NOT NULL,
    "MenuNivel2" VARCHAR(3) NOT NULL,
    "MenuNivel3" VARCHAR(3) NOT NULL,
    "MenuNivel4" VARCHAR(3) NOT NULL,
    "MenuNivel5" VARCHAR(3) NOT NULL
);

CREATE INDEX IF NOT EXISTS "iControleDePerfilModelo2"
ON "ControleDePerfilModelo"("MenuNivel1", "MenuNivel2", "MenuNivel3", "MenuNivel4", "MenuNivel5");

-- ===============================
-- ControleDePerfilTipo
-- ===============================
CREATE TABLE IF NOT EXISTS "ControleDePerfilTipo" (
    "Id" SERIAL PRIMARY KEY,
    "Tipo" VARCHAR(10) NOT NULL
);

INSERT INTO "ControleDePerfilTipo" ("Tipo") VALUES 
('ADM'), ('AVANCADO'), ('BASICO');

/*
-- ===============================
-- Senhas
-- ===============================
CREATE TABLE IF NOT EXISTS "Senhas" (
    "Id" SERIAL PRIMARY KEY,
    "LoginUsuario" VARCHAR(60) NOT NULL UNIQUE,
    "NomeUsuario" VARCHAR(15) NOT NULL,
    "NomeCompleto" VARCHAR(100),
    "SenhaUsuario" VARCHAR(256) NOT NULL,
    "DataCadastro" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "DataExpira" TIMESTAMP,
    "Assinatura" BYTEA,
    "UsarAssinatura" INT NOT NULL DEFAULT 0,
    "Bloqueado" INT NOT NULL DEFAULT 0,
    "Administrador" INT NOT NULL DEFAULT 0,
    "Email" VARCHAR(100) NOT NULL,
    "EmailConfirmado" INT NOT NULL DEFAULT 0
);

-- ===============================
-- UsuariosWeb
-- ===============================
CREATE TABLE IF NOT EXISTS "UsuariosWeb" (
    "Id" SERIAL PRIMARY KEY,
    "SenhaId" INT NOT NULL REFERENCES "Senhas"("Id"),
    "CPFUsuario" VARCHAR(11) NOT NULL,
    "DataNascimentoUsuario" TIMESTAMP NOT NULL,
    "DataCadastro" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
*/