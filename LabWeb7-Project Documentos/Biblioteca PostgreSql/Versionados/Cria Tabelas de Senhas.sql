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
);


CREATE TABLE IF NOT EXISTS UsuariosWeb
(
  Id                     SERIAL      NOT NULL,
  IdSenha                int         NOT NULL,
  CPFUsuario             varchar(11) NOT NULL,
  DataNascimentoUsuario  TIMESTAMP   NOT NULL,
  DataCadastro           TIMESTAMP   NOT NULL,
  CONSTRAINT iUsuariosWeb1 PRIMARY KEY (Id),
  CONSTRAINT iUsuariosWeb_Senhas FOREIGN KEY (IdSenha) REFERENCES Senhas(Id)
);
