-- ==============================================================================
-- SCRIPT: Controle de Acesso
-- ==============================================================================
-- Objetivo: Criar as tabelas de controle de perfis, acesso e menu do sistema.
--
-- Dependências:
--   - A tabela "Senhas" deve existir previamente (criada em Tabelas_Vazias.sql),
--     pois "ControleDeAcesso" possui FK para ela.
--
-- Ordem de execução recomendada na implantação:
--   1. Tabelas_Vazias.sql
--   2. Controle de Acesso.sql
--   3. 002_menu_faturamento.sql
--   4. 006_menu_catalogo_recebimentos.sql
-- ==============================================================================

-- ==============================================================================
-- DROP das tabelas (ordem inversa das dependências)
-- ==============================================================================

DROP TABLE IF EXISTS "ControleDePerfil";
DROP TABLE IF EXISTS "ControleDeAcesso";
DROP TABLE IF EXISTS "ControleDePerfilMenu";
DROP TABLE IF EXISTS "ControleDePerfilModelo";
DROP TABLE IF EXISTS "ControleDePerfilTipo";

-- ==============================================================================
-- CREATE das tabelas
-- ==============================================================================

CREATE TABLE IF NOT EXISTS "ControleDePerfilTipo"
(
  "Id"     SERIAL       NOT NULL,
  "Tipo"   varchar(10)  NOT NULL,
  CONSTRAINT "iControleDePerfilTipo1" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "ControleDePerfilModelo"
(
  "Id"          SERIAL       NOT NULL,
  "MenuNivel1"  varchar(3)   NOT NULL,
  "MenuNivel2"  varchar(3)   NOT NULL,
  "MenuNivel3"  varchar(3)   NOT NULL,
  "MenuNivel4"  varchar(3)   NOT NULL,
  "MenuNivel5"  varchar(3)   NOT NULL,
  CONSTRAINT "iControleDePerfilModelo1" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "iControleDePerfilModelo2"
  ON "ControleDePerfilModelo" ("MenuNivel1", "MenuNivel2", "MenuNivel3", "MenuNivel4", "MenuNivel5");

CREATE TABLE IF NOT EXISTS "ControleDePerfilMenu"
(
  "Id"          SERIAL       NOT NULL,
  "Coluna"      int          NOT NULL,
  "Menu"        varchar(100) NOT NULL,
  "Area"        varchar(100),
  "Controller"  varchar(100),
  "Action"      varchar(100),
  "Nivel"       varchar(3)   NOT NULL,
  "Ativo"       int          NOT NULL,
  CONSTRAINT "iControleDePerfilMenu1" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "iControleDePerfilMenu2"
  ON "ControleDePerfilMenu" ("Coluna", "Nivel");

CREATE TABLE IF NOT EXISTS "ControleDeAcesso"
(
  "Id"        SERIAL  NOT NULL,
  "SenhaId"   int     NOT NULL,
  CONSTRAINT "iControleDeAcesso1" PRIMARY KEY ("Id"),
  CONSTRAINT "iControleDeAcesso2" UNIQUE ("SenhaId"),
  CONSTRAINT "iControleDeAcesso_Senhas" FOREIGN KEY ("SenhaId") REFERENCES "Senhas"("Id") ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE TABLE IF NOT EXISTS "ControleDePerfil"
(
  "Id"                  SERIAL  NOT NULL,
  "ControleDeAcessoId"  int     NOT NULL,
  "MenuNivelMenu"       varchar(3)  NOT NULL,
  "MenuNivel1"          varchar(3)  NOT NULL,
  "MenuNivel2"          varchar(3)  NOT NULL,
  "MenuNivel3"          varchar(3)  NOT NULL,
  "MenuNivel4"          varchar(3)  NOT NULL,
  "Ativo"               int         NOT NULL,
  CONSTRAINT "iControleDePerfil1" PRIMARY KEY ("Id"),
  CONSTRAINT "iControleDePerfil2" FOREIGN KEY ("ControleDeAcessoId") REFERENCES "ControleDeAcesso"("Id"),
  CONSTRAINT "iControleDePerfil3" UNIQUE ("MenuNivelMenu", "MenuNivel1", "MenuNivel2", "MenuNivel3", "MenuNivel4")
);
