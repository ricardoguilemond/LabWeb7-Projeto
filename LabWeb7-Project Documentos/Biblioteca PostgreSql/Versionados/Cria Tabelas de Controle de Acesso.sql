/*
    Criação das tabelas PostgreSQL para a base LAB_WEB7
    Data: 27/03/2023
	Finalidade: Criar as tabelas de Controle de Acesso de todo o sistema
    Por Ricardo Guilemond
    Convertido de MSSQL para PostgreSQL
*/
-- Conectar ao banco: \c LABWEB7
---Tem que "dropar" nesta ordem, por causa dos relacionamentos
--DROP TABLE IF EXISTS "ControleDePerfil";
--DROP TABLE IF EXISTS "ControleDeAcesso";
--DROP TABLE IF EXISTS "ControleDePerfilMenu";
--DROP TABLE IF EXISTS "ControleDePerfilModelo";
--DROP TABLE IF EXISTS "ControleDePerfilTipo";

CREATE TABLE IF NOT EXISTS "ControleDeAcesso" (
  "Id"                  SERIAL         NOT NULL,
  "IdSenha"             int            NOT NULL,
  CONSTRAINT "iControleDeAcesso1" PRIMARY KEY ("Id"),
  CONSTRAINT "iControleDeAcesso2" UNIQUE("IdSenha")     --RESTRIÇÃO ÚNICA, chave única relacionada com Senhas (1 x 1)
);

-------------------------------------------------------------------------------------------------------------------------------------------
/* Controla Perfil para qualquer opção de Menu de até 5 níveis de qualquer tamanho numérico 001.002.003.004.005 (exemplo: 099.121.087.111.001  ou   007.114.001.021.987)  */
CREATE TABLE IF NOT EXISTS "ControleDePerfil" (
  "Id"                  SERIAL         NOT NULL,
  "IdControleDeAcesso"  int            NOT NULL,
  "MenuNivelMenu"       varchar(3)     NOT NULL,
  "MenuNivel1"          varchar(3)     NOT NULL,
  "MenuNivel2"          varchar(3)     NOT NULL,
  "MenuNivel3"          varchar(3)     NOT NULL,
  "MenuNivel4"          varchar(3)     NOT NULL,
  "Ativo"               int            NOT NULL DEFAULT 0,
  CONSTRAINT "iControleDePerfil1" PRIMARY KEY ("Id"),
  CONSTRAINT "iControleDePerfil2" FOREIGN KEY ("IdControleDeAcesso") REFERENCES "ControleDeAcesso"("Id")
);
CREATE INDEX IF NOT EXISTS "iControleDePerfil3" ON "ControleDePerfil"("MenuNivelMenu","MenuNivel1","MenuNivel2","MenuNivel3","MenuNivel4");

-------------------------------------------------------------------------------------------------------------------------------------------
/* Lista das opções do MENU - Opções principais do Menu do PRIMEIRO NÍVEL APENAS */
CREATE TABLE IF NOT EXISTS "ControleDePerfilMenu" (
  "Id"                  SERIAL         NOT NULL,
  "Coluna"              int            NOT NULL,     ---posicionamento da coluna 1,2,..9999 na ordem que vai ficar disposta em tela!
  "Menu"                varchar(100)   NOT NULL,     ---texto do menu
  "Area"                varchar(100)       NULL,
  "Controller"          varchar(100)       NULL,
  "Action"              varchar(100)       NULL,
  "Nivel"               varchar(3)     NOT NULL,     ---ordem das opções estando uma abaixo da outra pela ordem dessa numeração 000,001..999.
  "Ativo"               int            NOT NULL DEFAULT 0,
  CONSTRAINT "iControleDePerfilMenu1" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "iControleDePerfilMenu2" ON "ControleDePerfilMenu"("Coluna","Nivel");
--
--Monta as opções principais do menu (Nível 000 — cabeçalhos de grupo)
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (1, 'Cadastros', null, null, null, '000', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (2, 'Exames', null, null, null, '000', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (3, 'Plano de Exames', null, null, null, '000', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (4, 'Carga de Dados', null, null, null, '000', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (5, 'Controle de Acesso', null, null, null, '000', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (6, 'ReCaptcha', null, null, null, '000', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (7, 'Manutenção', null, null, null, '000', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (8, 'Sobre', null, null, null, '000', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (9, 'Login/Logout', null, null, null, '000', 1);
--
--Monta os itens do menu: Cadastros (Coluna 1)
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (1, 'Pacientes', null, 'Pacientes', 'Index', '001', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (1, 'Médicos', null, 'Medicos', 'Index', '002', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (1, 'Instituições', null, 'Instituicoes', 'Index', '003', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (1, 'Postos', null, 'Postos', 'Index', '004', 1);
--
--Monta os itens do menu: Exames (Coluna 2)
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (2, 'Requisição', null, 'Requisitar', 'Index', '001', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (2, 'Consultar Exames', null, 'ConsultarExames', 'Index', '002', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (2, 'Resultados', null, 'ResultadoExames', 'Index', '004', 1);
--
--Monta os itens do menu: Plano de Exames (Coluna 3)
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (3, 'Folha de Exames', null, 'ClasseExames', 'Index', '001', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (3, 'Plano de Exames', null, 'PlanoExames', 'Index', '002', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (3, 'Tabela de Preços', null, 'PlanoExamesItens', 'Index', '003', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (3, 'Referências de Laudos', null, 'ExameReferencia', 'Index', '004', 1);
--
--Monta os itens do menu: Carga de Dados (Coluna 4)
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (4, 'Implantação', null, 'CargaDados', 'Index', '001', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (4, 'Importar Referências', null, 'Manutencao', 'ImportarReferencias', '002', 1);
--
--Monta os itens do menu: Controle de Acesso (Coluna 5)
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (5, 'Usuários', null, 'Senhas', 'Index', '001', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (5, 'Perfil/Permissões', null, null, null, '002', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (5, 'Auditoria', null, null, null, '003', 1);
--
--Monta os itens do menu: ReCaptcha (Coluna 6)
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (6, 'Gráfico ReCaptcha', null, 'Graficos', 'GraficoReCaptcha', '001', 1);
--
--Monta os itens do menu: Manutenção (Coluna 7)
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (7, 'Configurações', null, 'Configuracoes', 'Index', '001', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (7, 'Compactar Requisições', null, 'Manutencao', 'CompactarRequisicoes', '002', 1);
--
--Monta os itens do menu: Sobre (Coluna 8)
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (8, 'Privacidade', null, 'Home', 'Privacy', '001', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (8, 'Nosso Sistema', null, 'Home', 'NossoSistema', '002', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (8, 'Versão Ambiente', null, 'Release', 'Release', '003', 1);
--
--Monta os itens do menu: Login/Logout (Coluna 9)
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (9, 'Login', null, 'Home', 'Home/Login', '001', 1);
INSERT INTO "ControleDePerfilMenu" ("Coluna", "Menu", "Area", "Controller", "Action", "Nivel", "Ativo") VALUES (9, 'Logout', null, 'Home', 'Logout', '002', 1);


-------------------------------------------------------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "ControleDePerfilModelo" (
  "Id"                  SERIAL         NOT NULL,
  "MenuNivel1"          varchar(3)     NOT NULL,
  "MenuNivel2"          varchar(3)     NOT NULL,
  "MenuNivel3"          varchar(3)     NOT NULL,
  "MenuNivel4"          varchar(3)     NOT NULL,
  "MenuNivel5"          varchar(3)     NOT NULL,
  CONSTRAINT "iControleDePerfilModelo1" PRIMARY KEY ("Id")
);
CREATE INDEX IF NOT EXISTS "iControleDePerfilModelo2" ON "ControleDePerfilModelo"("MenuNivel1","MenuNivel2","MenuNivel3","MenuNivel4","MenuNivel5");

-------------------------------------------------------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "ControleDePerfilTipo" (
  "Id"                  SERIAL         NOT NULL,
  "Tipo"                varchar(10)    NOT NULL,
  CONSTRAINT "iControleDePerfilTipo1" PRIMARY KEY ("Id")
);
INSERT INTO "ControleDePerfilTipo" ("Tipo") VALUES ('ADM');
INSERT INTO "ControleDePerfilTipo" ("Tipo") VALUES ('AVANCADO');
INSERT INTO "ControleDePerfilTipo" ("Tipo") VALUES ('BASICO');



/*
CREATE TABLE IF NOT EXISTS Senhas
(
  Id                   SERIAL       NOT NULL,
  LoginUsuario         varchar(60)  NOT NULL,             ----Email do usuario (via código) ou qualquer nick válido (via SQL script)
  NomeUsuario          varchar(15)  NOT NULL,
  NomeCompleto         varchar(100),
  SenhaUsuario         varchar(256) NOT NULL,
  DataCadastro         TIMESTAMP    NOT NULL,
  DataExpira           TIMESTAMP,
  Assinatura           BYTEA,
  UsarAssinatura       int          NOT NULL DEFAULT 0,
  Bloqueado            int          NOT NULL DEFAULT 0,
  Administrador        int          NOT NULL DEFAULT 0,
  Email                varchar(100) NOT NULL,
  EmailConfirmado      int          NOT NULL DEFAULT 0,
  CONSTRAINT iSenhas1 PRIMARY KEY (Id),
  CONSTRAINT iSenhas2 UNIQUE(LoginUsuario)   ----RESTRIÇÃO EXCLUSIVA DE COLUNA: a coluna LoginUsuario JAMAIS conterá outro valor igual!
) 

CREATE TABLE IF NOT EXISTS UsuariosWeb
(
  Id                     SERIAL      NOT NULL,
  IdSenha                int         NOT NULL,
  CPFUsuario             varchar(11) NOT NULL,
  DataNascimentoUsuario  TIMESTAMP   NOT NULL,
  DataCadastro           TIMESTAMP   NOT NULL,
  CONSTRAINT iUsuariosWeb1 PRIMARY KEY (Id),
  CONSTRAINT iUsuariosWeb_Senhas FOREIGN KEY (IdSenha) REFERENCES Senhas(Id)
) 
*/
