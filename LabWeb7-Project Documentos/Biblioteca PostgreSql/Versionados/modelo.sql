/*
   Script para Criação das Tabelas para PostgreSQL da base LAB_WEB7
   Data: 28/08/2022
   Convertido de MySQL para PostgreSQL
*/


DROP TABLE IF EXISTS Pacientes;
CREATE TABLE Pacientes
(
  Id                  SERIAL         NOT NULL,
  IdPacienteExterno   varchar(20),
  NomePaciente        varchar(100)   NOT NULL,
  NomeSocial          varchar(100),
  NomePai             varchar(100),
  NomeMae             varchar(100),
  Nascimento          DATE           NOT NULL,
  CPF                 varchar(11),
  Identidade          varchar(9),
  Emissor             varchar(15),
  CarteiraSUS         varchar(15),
  EstadoCivil         int            DEFAULT 0,
  Sexo                varchar(1),
  Cor                 varchar(7),
  EtniaIndigena       varchar(60),
  TipoSanguineo       varchar(3),
  DUM                 DATE,
  TempoGestacao       int            DEFAULT 0,
  Profissao           varchar(100),
  Naturalidade        varchar(30),
  Nacionalidade       varchar(30),
  DataEntradaBrasil   DATE,
  Logradouro          varchar(8),
  Endereco            varchar(60),
  Complemento         varchar(25),
  Bairro              varchar(45),
  Cidade              varchar(45),
  Estado              varchar(2),
  CEP                 varchar(8),
  Telefone            VARCHAR(60),
  Email               VARCHAR(60),
  Observacao          TEXT,
  DataEntrada         DATE           NOT NULL,
  HoraEntrada         VARCHAR(5)     NOT NULL,
  DataBaixa           DATE,
  HoraBaixa           VARCHAR(5),
  StatusBaixa         int            DEFAULT 0,
  DataRegistro        DATE           NOT NULL,
  HoraRegistro        VARCHAR(5)     NOT NULL,
  CONSTRAINT iPacientes1 PRIMARY KEY (Id)
);
CREATE INDEX iPacientes2  ON Pacientes (NomePaciente,IdPacienteExterno);
CREATE INDEX iPacientes3  ON Pacientes (NomeSocial,IdPacienteExterno);
CREATE INDEX iPacientes4  ON Pacientes (IdPacienteExterno);


DROP TABLE IF EXISTS Controle;
CREATE TABLE Controle
(
  IdPacientes              int DEFAULT 0,
  IdPlanoExames            int DEFAULT 0,
  IdMedicos                int DEFAULT 0,
  IdExamesRealizados       int DEFAULT 0,
  IdItensExamesRealizados  int DEFAULT 0,
  IdRequisicaoOriginal     int DEFAULT 0,
  IdExamesImpressos        int DEFAULT 0,
  IdExamesPendentes        int DEFAULT 0
);


DROP TABLE IF EXISTS ERTemporario;
CREATE TABLE ERTemporario
(
  Id                  SERIAL         NOT NULL,
  IdExame             int            DEFAULT 0 NOT NULL,
  IdPaciente          int            DEFAULT 0 NOT NULL,
  IdInstituicao       int            DEFAULT 0 NOT NULL,
  IdTabelaExames      int            DEFAULT 0 NOT NULL,
  IdMedico            int            DEFAULT 0 NOT NULL,
  Sequencial          int            DEFAULT 0 NOT NULL,
  IdClasseExames      int            DEFAULT 0 NOT NULL,
  HistoricoClinico    TEXT,
  DataIni             DATE,
  DataFim             DATE,
  Liberacao           int            DEFAULT 0,
  DataExame           DATE,
  DataEntrega         DATE,
  Baixado             int            DEFAULT 0,
  CONSTRAINT iERTemporario1 PRIMARY KEY (Id)
);
CREATE INDEX iERTemporario2 ON ERTemporario (IdExame);
CREATE INDEX iERTemporario3 ON ERTemporario (IdPaciente,IdExame);


DROP TABLE IF EXISTS Empresa;
CREATE TABLE Empresa
(
  Id                  SERIAL         NOT NULL,
  Matriz              int            DEFAULT 1 NOT NULL, 
  Sigla               varchar(20)    NOT NULL,
  NomeFantasia        varchar(100),
  RazaoSocial         varchar(100)   NOT NULL,
  CNPJ                varchar(14)    NOT NULL,
  Logradouro          varchar(8)     NOT NULL,
  Endereco            VARCHAR(60)    NOT NULL,
  Complemento         VARCHAR(25),
  Bairro              varchar(45)    NOT NULL,
  Cidade              varchar(45)    NOT NULL,
  UF                  varchar(2)     NOT NULL,
  CEP                 varchar(8)     NOT NULL,
  Telefones           varchar(60)    NOT NULL,
  Email               varchar(60)    NOT NULL,
  Site                VARCHAR(150),
  HostLogoMarca       VARCHAR(150),
  UnidadeLogoMarca    varchar(2),
  CaminhoLogoMarca    VARCHAR(150),
  NomeLogoMarca       varchar(25),
  TituloEmpresa       VARCHAR(100),
  SubTituloEmpresa    VARCHAR(100),
  Rodape              VARCHAR(140),
  DataExpira          DATE,
  EmailPopLogin       varchar(60),
  EmailPopSenha       varchar(20),
  EmailPopPorta       int            DEFAULT 0,
  EmailPopTLS         int            DEFAULT 0,
  EmailPopSSL         int            DEFAULT 0,
  EmailSmtpLogin      varchar(60)    NOT NULL,
  EmailSmtpSenha      varchar(20)    NOT NULL,
  EmailSmtpPorta      int            DEFAULT 0 NOT NULL,
  EmailSmtpTLS        int            DEFAULT 0,
  EmailSmtpSSL        int            DEFAULT 0,
  IPServidorInterno   VARCHAR(15),
  IPServidorExterno   VARCHAR(15),
  CONSTRAINT iEmpresa1 PRIMARY KEY (Id)
);
CREATE INDEX iEmpresa2 ON Empresa (CNPJ,Sigla);
CREATE INDEX iEmpresa3 ON Empresa (Sigla);
CREATE INDEX iEmpresa4 ON Empresa (NomeFantasia,Sigla);
CREATE INDEX iEmpresa5 ON Empresa (RazaoSocial,Sigla);


DROP TABLE IF EXISTS EstadoCivil;
CREATE TABLE EstadoCivil
(
  Id                  SERIAL         NOT NULL,
  Descricao           varchar(10)    NOT NULL,
  CONSTRAINT iEstadoCivil1 PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX iEstadoCivil2 ON EstadoCivil (Descricao);


DROP TABLE IF EXISTS ExamesExportados;
CREATE TABLE ExamesExportados
(
  Id                  SERIAL         NOT NULL,
  IdExame             int            DEFAULT 0 NOT NULL,
  IdPaciente          int            DEFAULT 0 NOT NULL,
  IdInstituicao       int            DEFAULT 0 NOT NULL,
  IdTabelaExames      int            DEFAULT 0 NOT NULL,
  IdMedico            int            DEFAULT 0 NOT NULL,
  Sequencial          int            DEFAULT 0 NOT NULL,
  LaboratorioApoio    VARCHAR(15),
  ControleApoio       VARCHAR(15)    NOT NULL,
  DataColeta          DATE,
  DataExportado       DATE,
  DataImportado       DATE,
  CONSTRAINT iExamesExportados1 PRIMARY KEY (Id,IdExame),
  FOREIGN KEY (IdPaciente) REFERENCES Pacientes(Id),
  FOREIGN KEY (IdExame) REFERENCES ExamesRealizados(Id),
  FOREIGN KEY (IdInstituicao) REFERENCES Instituicao(Id),
  FOREIGN KEY (IdTabelaExames) REFERENCES TabelaExames(Id),
  FOREIGN KEY (IdMedico) REFERENCES Medicos(Id)
);
CREATE INDEX iExamesExportados2 ON ExamesExportados (IdExame);


DROP TABLE IF EXISTS ExamesImpressos;
CREATE TABLE ExamesImpressos
(
  Id                  SERIAL         NOT NULL,
  IdPaciente          int            DEFAULT 0 NOT NULL,
  IdInstituicao       int            DEFAULT 0 NOT NULL,
  IdTabelaExames      int            DEFAULT 0 NOT NULL,
  Sequencial          int            DEFAULT 0,
  DataExame           DATE,
  DataImpresso        DATE,
  TotalImpresso       int            DEFAULT 0,
   CONSTRAINT iExamesImpressos1 PRIMARY KEY (Id),
   FOREIGN KEY (IdPaciente) REFERENCES Pacientes(Id),
   FOREIGN KEY (IdInstituicao) REFERENCES Instituicao(Id),
   FOREIGN KEY (IdTabelaExames) REFERENCES TabelaExames(Id)
);
CREATE INDEX iExamesImpressos2 ON ExamesImpressos (IdPaciente,Id);


DROP TABLE IF EXISTS ExamesPendentes;
CREATE TABLE ExamesPendentes
(
  Id                  SERIAL         NOT NULL,
  IdPaciente          int            DEFAULT 0 NOT NULL,
  IdInstituicao       int            DEFAULT 0 NOT NULL,
  IdTabelaExames      int            DEFAULT 0 NOT NULL,
  IdMedico            int            DEFAULT 0 NOT NULL,
  IdClasseExames      int            DEFAULT 0 NOT NULL,
  Sequencial          int            DEFAULT 0,
  LaboratorioApoio    VARCHAR(15),
  ControleApoio       VARCHAR(15),
  ContaExame          varchar(11),
  NomeFolha           varchar(50),
  NomeGrupo           varchar(50),
  NomeItem            varchar(50),
  DataIni             DATE,
  CONSTRAINT iExamesPendentes1 PRIMARY KEY (Id),
  FOREIGN KEY (IdPaciente) REFERENCES Pacientes(Id),
  FOREIGN KEY (IdInstituicao) REFERENCES Instituicao(Id),
  FOREIGN KEY (IdTabelaExames) REFERENCES TabelaExames(Id),
  FOREIGN KEY (IdMedico) REFERENCES Medicos(Id),
  FOREIGN KEY (IdClasseExames) REFERENCES ClasseExames(Id)
);
CREATE INDEX iExamesPendentes2 ON ExamesPendentes (IdPaciente,Id);


DROP TABLE IF EXISTS ExamesRealizados;
CREATE TABLE ExamesRealizados
(
  Id                  SERIAL         NOT NULL,
  IdPaciente          int            DEFAULT 0 NOT NULL,
  IdTabelaExames      int            DEFAULT 0 NOT NULL,
  IdInstituicao       int            DEFAULT 0 NOT NULL,
  IdPosto             int            DEFAULT 0,
  IdMedico            int            DEFAULT 0 NOT NULL,
  IdClasseExames      int            DEFAULT 0 NOT NULL,
  Sequencial          int            DEFAULT 0,
  LaboratorioApoio    VARCHAR(15),
  ControleApoio       VARCHAR(15)    NOT NULL,
  HistoricoClinico    TEXT,
  ExameColado         VARCHAR(250),
  ExameColadoImagens  VARCHAR(250),
  TravaColado         int            DEFAULT 0,
  DataIni             DATE           NOT NULL,
  DataFim             DATE,
  Liberacao           int            DEFAULT 0,
  DataExame           DATE,
  DataColeta          varchar(10),
  DataEntrega         DATE,
  Baixado             int            DEFAULT 0,
  EnviarEmail         int            DEFAULT 0,
  Situacao            int            DEFAULT 0,
  TotalImpresso       int            DEFAULT 0,
  Faturado            BOOLEAN        DEFAULT FALSE,
  CONSTRAINT iExamesRealizados1 PRIMARY KEY (Id,ControleApoio),
  FOREIGN KEY (IdPaciente) REFERENCES Pacientes(Id),
  FOREIGN KEY (IdTabelaExames) REFERENCES TabelaExames(Id),
  FOREIGN KEY (IdInstituicao) REFERENCES Instituicao(Id),
  FOREIGN KEY (IdPosto) REFERENCES Postos(Id),
  FOREIGN KEY (IdMedico) REFERENCES Medicos(Id),
  FOREIGN KEY (IdClasseExames) REFERENCES ClasseExames(Id)
);
CREATE INDEX iExamesRealizados2  ON ExamesRealizados (IdPaciente,Id);
CREATE INDEX iExamesRealizados3  ON ExamesRealizados (IdPaciente,IdClasseExames,Id);
CREATE INDEX iExamesRealizados4  ON ExamesRealizados (LaboratorioApoio,Id);
CREATE INDEX iExamesRealizados5  ON ExamesRealizados (LaboratorioApoio,ControleApoio);
CREATE INDEX iExamesRealizados6  ON ExamesRealizados (IdPaciente,IdInstituicao,Sequencial);
CREATE INDEX iExamesRealizados7  ON ExamesRealizados (IdInstituicao,Sequencial,IdPaciente);
CREATE INDEX iExamesRealizados8  ON ExamesRealizados (DataColeta,IdInstituicao,Sequencial);
CREATE INDEX iExamesRealizados9  ON ExamesRealizados (DataExame,IdInstituicao,Sequencial);


DROP TABLE IF EXISTS ExamesRealizadosAM;
CREATE TABLE ExamesRealizadosAM
(
  Id                  SERIAL         NOT NULL,
  IdExame             int            DEFAULT 0 NOT NULL,  
  IdPaciente          int            DEFAULT 0 NOT NULL,
  IdTabelaExames      int            DEFAULT 0 NOT NULL,
  IdInstituicao       int            DEFAULT 0 NOT NULL,
  IdPosto             int            DEFAULT 0,
  IdMedico            int            DEFAULT 0 NOT NULL,
  Sequencial          int            DEFAULT 0 NOT NULL,
  LaboratorioApoio    VARCHAR(15),
  ControleApoio       VARCHAR(15)    NOT NULL,
  IdClasseExames      int            DEFAULT 0 NOT NULL,
  HistoricoClinico    TEXT,
  ExameColado         VARCHAR(250),
  ExameColadoImagens  VARCHAR(250),
  TravaColado         int            DEFAULT 0,
  DataIni             DATE           NOT NULL,
  DataFim             DATE,
  Liberacao           int            DEFAULT 0,
  DataExame           DATE,
  DataColeta          varchar(10),
  DataEntrega         DATE,
  Baixado             int            DEFAULT 0,
  EnviarEmail         varchar(1),
  Situacao            int            DEFAULT 0,
  TotalImpresso       int            DEFAULT 0,
  Faturado            BOOLEAN        DEFAULT FALSE,
  CONSTRAINT iExamesRealizadosAM1 PRIMARY KEY (Id),
  FOREIGN KEY (IdPaciente) REFERENCES Pacientes(Id),
  FOREIGN KEY (IdTabelaExames) REFERENCES TabelaExames(Id),
  FOREIGN KEY (IdInstituicao) REFERENCES Instituicao(Id),
  FOREIGN KEY (IdPosto) REFERENCES Postos(Id),
  FOREIGN KEY (IdMedico) REFERENCES Medicos(Id),
  FOREIGN KEY (IdClasseExames) REFERENCES ClasseExames(Id)
);
CREATE INDEX iExamesRealizadosAM2 ON ExamesRealizadosAM (IdExame);
CREATE INDEX iExamesRealizadosAM3 ON ExamesRealizadosAM (DataColeta,IdInstituicao,Sequencial);
CREATE INDEX iExamesRealizadosAM4 ON ExamesRealizadosAM (DataExame,IdInstituicao,Sequencial);


DROP TABLE IF EXISTS FichasInternas;
CREATE TABLE FichasInternas
(
  Id                  SERIAL         NOT NULL,
  NomeFicha           varchar(50),
  ContaExame          varchar(11),
  Descricao           varchar(50),
  Resultado           varchar(30),
  MapaHorizontal      varchar(6),
  IdExame             int            DEFAULT 0 NOT NULL,
  IdPaciente          int            DEFAULT 0 NOT NULL,
  IdMedico            int            DEFAULT 0 NOT NULL,
  IdInstituicao       int            DEFAULT 0 NOT NULL,
  DataExame           DATE           NOT NULL,
  ControleApoio       VARCHAR(15),
  Sequencial          int            DEFAULT 0,
  HistoricoClinico    TEXT,
  DataIni             DATE           NOT NULL,
  DataFim             DATE,
  Pagina              int            DEFAULT 0,
  Coluna1             varchar(6),
  Coluna2             varchar(6),
  Coluna3             varchar(6),
  Coluna4             varchar(6),
  Coluna5             varchar(6),
  Coluna6             varchar(6),
  Coluna7             varchar(6),
  Coluna8             varchar(6),
  Coluna9             varchar(6),
  Coluna10            varchar(6),
  Coluna11            varchar(6),
  Coluna12            varchar(6),
  Coluna13            varchar(6),
  Coluna14            varchar(6),
  Coluna15            varchar(6),
  Coluna16            varchar(6),
  Coluna17            varchar(6),
  Coluna18            varchar(6),
  CONSTRAINT iFichasInternas1 PRIMARY KEY (Id),
  FOREIGN KEY (IdExame) REFERENCES ExamesRealizados(Id),
  FOREIGN KEY (IdPaciente) REFERENCES Pacientes(Id),
  FOREIGN KEY (IdMedico) REFERENCES Medicos(Id),
  FOREIGN KEY (IdInstituicao) REFERENCES Instituicao(Id)
);
CREATE INDEX iFichasInternas2  ON FichasInternas (NomeFicha,ContaExame);
CREATE INDEX iFichasInternas3  ON FichasInternas (Descricao,ContaExame);
CREATE INDEX iFichasInternas4  ON FichasInternas (IdExame,ContaExame);
CREATE INDEX iFichasInternas5  ON FichasInternas (ControleApoio,ContaExame);
CREATE INDEX iFichasInternas6  ON FichasInternas (IdInstituicao,Sequencial,ContaExame);
CREATE INDEX iFichasInternas7  ON FichasInternas (NomeFicha,IdExame,ContaExame);
CREATE INDEX iFichasInternas8  ON FichasInternas (NomeFicha,ControleApoio,ContaExame);
CREATE INDEX iFichasInternas9  ON FichasInternas (NomeFicha,IdInstituicao,Sequencial,ContaExame);
CREATE INDEX iFichasInternas10 ON FichasInternas (Pagina,IdExame,ContaExame);


DROP TABLE IF EXISTS FichasLotes;
CREATE TABLE FichasLotes
(
  Id                  SERIAL         NOT NULL,
  NomeFicha           varchar(50),
  ContaExame          varchar(11),
  Descricao           varchar(50),
  Resultado           varchar(30),
  MapaHorizontal      varchar(6),
  IdExame             int            DEFAULT 0 NOT NULL,
  IdPaciente          int            DEFAULT 0 NOT NULL,
  IdMedico            int            DEFAULT 0 NOT NULL,
  IdInstituicao       int            DEFAULT 0 NOT NULL,
  IdTabelaExames      int            DEFAULT 0 NOT NULL,
  DataExame           DATE,
  ControleApoio       VARCHAR(15),
  Sequencial          int            DEFAULT 0,
  HistoricoClinico    TEXT,
  DataIni             DATE,
  DataFim             DATE,
  Lote                int            DEFAULT 0,
  LiberadoExclusao    varchar(1),
  CONSTRAINT iFichasLotes1 PRIMARY KEY (Id),
  FOREIGN KEY (IdExame) REFERENCES ExamesRealizados(Id),
  FOREIGN KEY (IdPaciente) REFERENCES Pacientes(Id),
  FOREIGN KEY (IdMedico) REFERENCES Medicos(Id),
  FOREIGN KEY (IdInstituicao) REFERENCES Instituicao(Id),
  FOREIGN KEY (IdTabelaExames) REFERENCES TabelaExames(Id)
);
CREATE INDEX iFichasLotes2  ON FichasLotes (NomeFicha,ContaExame);
CREATE INDEX iFichasLotes3  ON FichasLotes (Descricao,ContaExame);
CREATE INDEX iFichasLotes4  ON FichasLotes (IdExame,ContaExame);
CREATE INDEX iFichasLotes5  ON FichasLotes (IdInstituicao,Sequencial,ContaExame);
CREATE INDEX iFichasLotes6  ON FichasLotes (NomeFicha,IdExame,ContaExame);
CREATE INDEX iFichasLotes7  ON FichasLotes (NomeFicha,ControleApoio,ContaExame);
CREATE INDEX iFichasLotes8  ON FichasLotes (NomeFicha,IdInstituicao,Sequencial,ContaExame);
CREATE INDEX iFichasLotes9  ON FichasLotes (DataExame,Lote,ControleApoio,ContaExame);
CREATE INDEX iFichasLotes10 ON FichasLotes (DataExame,Lote,IdInstituicao,Sequencial,ContaExame);


DROP TABLE IF EXISTS FichasPlanilhas;
CREATE TABLE FichasPlanilhas
(
  Id                  SERIAL         NOT NULL,
  NomeFicha           varchar(50),
  ContaExame          varchar(11),
  Descricao           varchar(50),
  Resultado           varchar(30),
  MapaHorizontal      varchar(6),
  IdExame             int            DEFAULT 0 NOT NULL,
  IdPaciente          int            DEFAULT 0 NOT NULL,
  IdMedico            int            DEFAULT 0 NOT NULL,
  IdInstituicao       int            DEFAULT 0 NOT NULL,
  IdTabelaExames      int            DEFAULT 0 NOT NULL,
  DataExame           DATE,
  ControleApoio       VARCHAR(15),
  Sequencial          int            DEFAULT 0,
  HistoricoClinico    TEXT,
  DataIni             DATE           NOT NULL,
  DataFim             DATE,
  Lote                int            DEFAULT 0,
  LiberadoExclusao    varchar(1),
  CONSTRAINT iFichasPlanilhas1 PRIMARY KEY (Id),
  FOREIGN KEY (IdExame) REFERENCES ExamesRealizados(Id),
  FOREIGN KEY (IdPaciente) REFERENCES Pacientes(Id),
  FOREIGN KEY (IdMedico) REFERENCES Medicos(Id),
  FOREIGN KEY (IdInstituicao) REFERENCES Instituicao(Id),
  FOREIGN KEY (IdTabelaExames) REFERENCES TabelaExames(Id)
);
CREATE INDEX iFichasPlanilhas2  ON FichasPlanilhas (NomeFicha,ContaExame);
CREATE INDEX iFichasPlanilhas3  ON FichasPlanilhas (Descricao,ContaExame);
CREATE INDEX iFichasPlanilhas4  ON FichasPlanilhas (IdExame,ContaExame);
CREATE INDEX iFichasPlanilhas5  ON FichasPlanilhas (ControleApoio,ContaExame);
CREATE INDEX iFichasPlanilhas6  ON FichasPlanilhas (IdInstituicao,Sequencial,ContaExame);
CREATE INDEX iFichasPlanilhas7  ON FichasPlanilhas (NomeFicha,IdExame,ContaExame);
CREATE INDEX iFichasPlanilhas8  ON FichasPlanilhas (NomeFicha,ControleApoio,ContaExame);
CREATE INDEX iFichasPlanilhas9  ON FichasPlanilhas (NomeFicha,IdInstituicao,Sequencial,ContaExame);
CREATE INDEX iFichasPlanilhas10 ON FichasPlanilhas (Lote,NomeFicha,IdExame,ContaExame);
CREATE INDEX iFichasPlanilhas11 ON FichasPlanilhas (DataExame,Lote,IdExame,ContaExame);
CREATE INDEX iFichasPlanilhas12 ON FichasPlanilhas (DataExame,Lote,IdPaciente,ContaExame);
CREATE INDEX iFichasPlanilhas13 ON FichasPlanilhas (DataExame,Lote,ControleApoio,ContaExame);
CREATE INDEX iFichasPlanilhas14 ON FichasPlanilhas (DataExame,Lote,IdInstituicao,Sequencial,ContaExame);


DROP TABLE IF EXISTS Instituicao;
CREATE TABLE Instituicao
(
  Id                  SERIAL         NOT NULL,
  Sigla               VARCHAR(15)    NOT NULL,
  Nome                varchar(100)   NOT NULL,
  CNPJ                varchar(14)    NOT NULL,
  Sequencial          int            DEFAULT 0,
  Email               VARCHAR(60)    NOT NULL,
  TituloTimbre        varchar(60),
  SubTituloTimbre     varchar(80),
  Timbre              BYTEA,
  Logomarca           BYTEA,            
  Carimbo             int            DEFAULT 0,
  TimbreSN            int            DEFAULT 0,
  Logradouro          varchar(8),
  Endereco            varchar(60),
  Complemento         varchar(25),
  Bairro              varchar(45),
  Cidade              varchar(45),
  UF                  varchar(2),
  CEP                 varchar(8),
  Contato             VARCHAR(60)    NOT NULL,
  Telefone            VARCHAR(60)    NOT NULL,
  Celular             VARCHAR(60),
  UsuarioCaminhoFTP   VARCHAR(250),
  UsuarioEmailFTP     VARCHAR(150),
  UsuarioPortaFTP     int            DEFAULT 0,
  UsuarioSenhaFTP     VARCHAR(60),
  ValorExameCitologia NUMERIC(18,4),
  Propaganda          int            DEFAULT 0,
  AvisoRodape1        varchar(140),
  AvisoRodape2        varchar(140),
  CONSTRAINT iInstituicao1 PRIMARY KEY (Id)
);
CREATE INDEX iInstituicao2 ON Instituicao (CNPJ,Sigla);
CREATE INDEX iInstituicao3 ON Instituicao (Sigla,Sequencial);
CREATE INDEX iInstituicao4 ON Instituicao (Nome,Sigla,Sequencial);


DROP TABLE IF EXISTS ItensExamesRealizados;
CREATE TABLE ItensExamesRealizados
(
  Id                  SERIAL         NOT NULL,
  IdPaciente          int            DEFAULT 0 NOT NULL,
  IdClasseExames      int            DEFAULT 0 NOT NULL,
  NomeClasseExames    varchar(50)    NOT NULL,
  IdExame             int            DEFAULT 0 NOT NULL,
  IdTabelaExames      int            DEFAULT 0 NOT NULL,
  OrdemItem           int            DEFAULT 0 NOT NULL,
  RefExame            varchar(50)    NOT NULL,
  RefItem             varchar(50)    NOT NULL,
  ContaExame          varchar(11)    NOT NULL,
  CitoTituloFolha     int            DEFAULT 0,
  CitoTituloExame     int            DEFAULT 0,
  CitoRefItem         int            DEFAULT 0,
  IdInstituicao       int            DEFAULT 0 NOT NULL,
  Sequencial          int            DEFAULT 0 NOT NULL,
  LaboratorioApoio    VARCHAR(15),
  ControleApoio       VARCHAR(15),
  LaboratorioExterno  VARCHAR(15),
  MaterialSaida       varchar(16),
  MaterialRetorno     varchar(16),
  Descricao           varchar(50),
  CitoDescricao       TEXT,
  Resultado           varchar(30),
  UnidadeMedida       varchar(20),
  Referencia          varchar(60),
  ValorItem           NUMERIC(18,4),
  Laudo               BYTEA,
  Etiquetas           int            DEFAULT 0,
  DataEntregaParcial  DATE,
  Liberado            int            DEFAULT 0,
  Baixado             int            DEFAULT 0,
  CONSTRAINT iItensExamesRealizados1 PRIMARY KEY (Id),
  FOREIGN KEY (IdExame) REFERENCES ExamesRealizados(Id),
  FOREIGN KEY (IdPaciente) REFERENCES Pacientes(Id),
  FOREIGN KEY (IdClasseExames) REFERENCES ClasseExames(Id),
  FOREIGN KEY (IdTabelaExames) REFERENCES TabelaExames(Id),
  FOREIGN KEY (IdInstituicao) REFERENCES Instituicao(Id)
);
CREATE INDEX iItensExamesRealizados2  ON ItensExamesRealizados (IdExame,OrdemItem);
CREATE INDEX iItensExamesRealizados3  ON ItensExamesRealizados (IdClasseExames,IdExame,OrdemItem);
CREATE INDEX iItensExamesRealizados4  ON ItensExamesRealizados (IdPaciente,IdClasseExames,IdExame,OrdemItem);
CREATE INDEX iItensExamesRealizados5  ON ItensExamesRealizados (DataEntregaParcial,IdClasseExames,IdExame,OrdemItem);
CREATE INDEX iItensExamesRealizados6  ON ItensExamesRealizados (ControleApoio,IdExame,OrdemItem);
CREATE INDEX iItensExamesRealizados7  ON ItensExamesRealizados (MaterialSaida,IdExame,OrdemItem);
CREATE INDEX iItensExamesRealizados8  ON ItensExamesRealizados (MaterialRetorno,IdExame,OrdemItem);
CREATE INDEX iItensExamesRealizados9  ON ItensExamesRealizados (LaboratorioApoio,ControleApoio);
CREATE INDEX iItensExamesRealizados10 ON ItensExamesRealizados (LaboratorioExterno,MaterialSaida);
CREATE INDEX iItensExamesRealizados11 ON ItensExamesRealizados (LaboratorioExterno,MaterialRetorno);
CREATE INDEX iItensExamesRealizados12 ON ItensExamesRealizados (IdPaciente,ContaExame);
CREATE INDEX iItensExamesRealizados13 ON ItensExamesRealizados (IdPaciente,IdInstituicao,Sequencial);
CREATE INDEX iItensExamesRealizados14 ON ItensExamesRealizados (IdInstituicao,Sequencial,IdPaciente);
CREATE INDEX iItensExamesRealizados15 ON ItensExamesRealizados (DataEntregaParcial,IdPaciente);


DROP TABLE IF EXISTS ItensExamesRealizadosAM;
CREATE TABLE ItensExamesRealizadosAM
(
  Id                  SERIAL         NOT NULL,
  IdPaciente          int            DEFAULT 0 NOT NULL,
  IdClasseExames      int            DEFAULT 0 NOT NULL,
  NomeClasseExames    varchar(50),
  IdExame             int            DEFAULT 0 NOT NULL,
  OrdemItem           int            DEFAULT 0 NOT NULL,
  RefExame            varchar(50)    NOT NULL,
  RefItem             varchar(50)    NOT NULL,
  ContaExame          varchar(11)    NOT NULL,
  CitoTituloFolha     int            DEFAULT 0,
  CitoTituloExame     int            DEFAULT 0,
  CitoRefItem         int            DEFAULT 0,
  IdInstituicao       int            DEFAULT 0 NOT NULL,
  Sequencial          int            DEFAULT 0 NOT NULL,
  IdTabelaExames      int            DEFAULT 0 NOT NULL,
  LaboratorioApoio    VARCHAR(15),
  ControleApoio       VARCHAR(15),
  LaboratorioExterno  VARCHAR(15),
  MaterialSaida       varchar(16),
  MaterialRetorno     varchar(16),
  Descricao           varchar(50),
  CitoDescricao       TEXT,
  Resultado           varchar(30),
  UnidadeMedida       varchar(20),
  Referencia          varchar(60),
  ValorItem           NUMERIC(18,4),
  Laudo               BYTEA,
  Etiquetas           int            DEFAULT 0,
  DataEntregaParcial  DATE,
  Liberado            int            DEFAULT 0,
  Baixado             int            DEFAULT 0,
  CONSTRAINT iItensExamesRealizadosAM1 PRIMARY KEY (Id,IdExame)
);
CREATE INDEX iItensExamesRealizadosAM2 ON ItensExamesRealizadosAM (IdExame,OrdemItem);


DROP TABLE IF EXISTS Logradouro;
CREATE TABLE Logradouro
(
  Id                  SERIAL         NOT NULL,
  Descricao           varchar(8)     NOT NULL,
  CONSTRAINT iLogradouro1 PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX iLogradouro2 ON Logradouro (Descricao);


DROP TABLE IF EXISTS Medicos;
CREATE TABLE Medicos
(
  Id                  SERIAL         NOT NULL,
  NomeMedico          varchar(100)   NOT NULL,
  Especialidade       varchar(100),
  CRM                 VARCHAR(15)    NOT NULL,
  Telefone            VARCHAR(60),
  Email               VARCHAR(60),
  CONSTRAINT iMedicos1 PRIMARY KEY (Id)
);
CREATE INDEX iMedicos2 ON Medicos (NomeMedico,Id);


DROP TABLE IF EXISTS MemoAuxiliar;
CREATE TABLE MemoAuxiliar
(
  Id                  SERIAL         NOT NULL,
  NomeFolha           varchar(50),
  Linha1              varchar(250),
  Linha2              varchar(250),
  Linha3              varchar(250),
  Linha4              varchar(250),
  Linha5              varchar(250),
  Linha6              varchar(250),
  CampoMemo           BYTEA,
  CONSTRAINT iMemoAuxiliar1 PRIMARY KEY (Id)
);


DROP TABLE IF EXISTS MenuSistema;
CREATE TABLE MenuSistema
(
  Id                  SERIAL         NOT NULL,
  IdUsuario           int            DEFAULT 0 NOT NULL,
  Opcao               int            DEFAULT 0 NOT NULL,
  Tipo                varchar(9)     NOT NULL,
  Nivel               int            DEFAULT 0 NOT NULL,
  SubNivel            int            DEFAULT 0 NOT NULL,
  Descricao           varchar(60)    NOT NULL,
  Modulo              varchar(150),
  Execucao            VARCHAR(60),
  Tag                 int            DEFAULT 0,
  Status              varchar(1)     NOT NULL,
  CONSTRAINT iMenuSistema1 PRIMARY KEY (Id)
);
CREATE INDEX iMenuSistema2 ON MenuSistema (IdUsuario);
CREATE INDEX iMenuSistema3 ON MenuSistema (Opcao);
CREATE INDEX iMenuSistema4 ON MenuSistema (Tipo,Nivel,Descricao);
CREATE INDEX iMenuSistema5 ON MenuSistema (Nivel,Tipo);
CREATE INDEX iMenuSistema6 ON MenuSistema (Descricao,Opcao);


DROP TABLE IF EXISTS MenuSistemaInterfaces;
CREATE TABLE MenuSistemaInterfaces
(
  Id                  SERIAL         NOT NULL,
  IdUsuario           int            DEFAULT 0 NOT NULL,
  Opcao               int            DEFAULT 0 NOT NULL,
  Tipo                varchar(9)     NOT NULL,
  Nivel               int            DEFAULT 0 NOT NULL,
  SubNivel            int            DEFAULT 0 NOT NULL,
  Descricao           VARCHAR(60)    NOT NULL,
  Modulo              varchar(150),
  Execucao            VARCHAR(60),
  Tag                 int            DEFAULT 0,
  Status              varchar(1)     NOT NULL,
  CONSTRAINT iMenuSistemaInterfaces1 PRIMARY KEY (Id)
);
CREATE INDEX iMenuSistemaInterfaces2 ON MenuSistemaInterfaces (IdUsuario);
CREATE INDEX iMenuSistemaInterfaces3 ON MenuSistemaInterfaces (Opcao);
CREATE INDEX iMenuSistemaInterfaces4 ON MenuSistemaInterfaces (Tipo,Nivel,Descricao);
CREATE INDEX iMenuSistemaInterfaces5 ON MenuSistemaInterfaces (Nivel,Tipo);
CREATE INDEX iMenuSistemaInterfaces6 ON MenuSistemaInterfaces (Descricao,Opcao);


DROP TABLE IF EXISTS PlanoExames;
CREATE TABLE PlanoExames
(
  Id                  SERIAL         NOT NULL,
  ClasseExamesId      int            DEFAULT 0 NOT NULL,
  CitoInstituicao     int            DEFAULT 0,
  CitoTituloFolha     varchar(60),
  CitoTituloExame     int            DEFAULT 0,
  CitoParteDescricao  varchar(100),
  CitoDescricao       TEXT,
  RefExame            varchar(50)    NOT NULL,
  RefItem             varchar(50)    NOT NULL,
  IdTabelaExames      int            DEFAULT 0 NOT NULL,
  ContaExame          varchar(11)    NOT NULL,
  Descricao           varchar(50)    NOT NULL,
  ValorCusto          NUMERIC(18,4),
  ValorItem           NUMERIC(18,4),
  TABELACH            varchar(10),
  QCH                 int            DEFAULT 0,
  ICH                 NUMERIC(18,2),
  UnidadeMedida       varchar(20),
  Referencia          varchar(60),
  Etiqueta            int            DEFAULT 0,
  Etiquetas           int            DEFAULT 0,
  GraficoNoItem       INT            NULL,
  Laudo               BYTEA,
  AlinhaLaudo         int            DEFAULT 0,
  Seleciona           int            DEFAULT 0,
  NaoMostrar          int            DEFAULT 0,
  MapaHorizontal      varchar(6),
  ResultadoMinimo     NUMERIC(18,4),
  ResultadoMaximo     NUMERIC(18,4),
  LaboratorioExterno  VARCHAR(15),
  CONSTRAINT iPlanoExames1 PRIMARY KEY (Id)
);
CREATE INDEX iPlanoExames2  ON PlanoExames (IdTabelaExames,ContaExame,Descricao);
CREATE INDEX iPlanoExames3  ON PlanoExames (ContaExame,IdTabelaExames,Descricao);
CREATE INDEX iPlanoExames4  ON PlanoExames (Descricao,IdTabelaExames,ContaExame);
CREATE INDEX iPlanoExames5  ON PlanoExames (ContaExame,IdExame,RefExame,IdTabelaExames,Id);
CREATE INDEX iPlanoExames6  ON PlanoExames (Descricao,RefItem,RefExame,ContaExame);
CREATE INDEX iPlanoExames7  ON PlanoExames (IdExame,ContaExame);
CREATE INDEX iPlanoExames8  ON PlanoExames (CitoParteDescricao,ContaExame);
CREATE INDEX iPlanoExames9  ON PlanoExames (CitoTituloFolha,CitoParteDescricao);
CREATE INDEX iPlanoExames10 ON PlanoExames (CitoTituloExame,ContaExame);
CREATE INDEX iPlanoExames11 ON PlanoExames (ContaExame,CitoInstituicao);
CREATE INDEX iPlanoExames12 ON PlanoExames (CitoInstituicao,ContaExame);


DROP TABLE IF EXISTS Rastreamentos;
CREATE TABLE Rastreamentos
(
  Id                  SERIAL         NOT NULL,
  IdUsuario           int            DEFAULT 0 NOT NULL,
  DataOcorrencia      DATE           NOT NULL,
  HoraOcorrencia      varchar(8)     NOT NULL,
  SistemaUtilizado    VARCHAR(30),
  VersaoSistema       varchar(26),
  OpcaoMenu           varchar(26),
  OperacaoRealizada   varchar(250),
  OperacaoComplementar varchar(250),
  Falha               varchar(250),
  CONSTRAINT iRastreamentos1 PRIMARY KEY (Id)
);
CREATE INDEX iRastreamentos2 ON Rastreamentos (IdUsuario);


DROP TABLE IF EXISTS RequisicaoOriginal;
CREATE TABLE RequisicaoOriginal
(
  Id                  SERIAL         NOT NULL,
  IdPaciente          int            DEFAULT 0 NOT NULL,
  IdClasseExames      int            DEFAULT 0 NOT NULL,
  NomeClasseExames    varchar(50)    NOT NULL,
  IdExame             int            DEFAULT 0 NOT NULL,
  OrdemItem           int            DEFAULT 0 NOT NULL,
  RefExame            varchar(50),
  RefItem             varchar(50),
  ContaExame          varchar(11)    NOT NULL,
  IdInstituicao       int            DEFAULT 0 NOT NULL,
  IdTabelaExames      int            DEFAULT 0 NOT NULL,
  LaboratorioApoio    VARCHAR(15),
  ControleApoio       VARCHAR(15),
  LaboratorioExterno  VARCHAR(15),
  MaterialSaida       varchar(16),
  MaterialRetorno     varchar(16),
  Descricao           varchar(50),
  Resultado           varchar(30),
  UnidadeMedida       varchar(20),
  Referencia          varchar(60),
  ValorItem           NUMERIC(18,4),
  Laudo               BYTEA,
  Etiquetas           int            DEFAULT 0,
  DataIni             DATE           NOT NULL,
  DataEntregaParcial  DATE,
  Liberado            int            DEFAULT 0,
  Baixado             int            DEFAULT 0,
  CONSTRAINT iRequisicaoOriginal1 PRIMARY KEY (Id),
  FOREIGN KEY (IdPaciente) REFERENCES Pacientes(Id),
  FOREIGN KEY (IdTabelaExames) REFERENCES TabelaExames(Id),
  FOREIGN KEY (IdInstituicao) REFERENCES Instituicao(Id),
  FOREIGN KEY (IdClasseExames) REFERENCES ClasseExames(Id)
);
CREATE INDEX iRequisicaoOriginal2  ON RequisicaoOriginal (IdExame,OrdemItem);
CREATE INDEX iRequisicaoOriginal3  ON RequisicaoOriginal (IdClasseExames,IdExame,ContaExame);
CREATE INDEX iRequisicaoOriginal4  ON RequisicaoOriginal (ControleApoio,IdExame,ContaExame);
CREATE INDEX iRequisicaoOriginal5  ON RequisicaoOriginal (MaterialSaida,IdExame,ContaExame);
CREATE INDEX iRequisicaoOriginal6  ON RequisicaoOriginal (MaterialRetorno,IdExame,ContaExame);
CREATE INDEX iRequisicaoOriginal7  ON RequisicaoOriginal (LaboratorioApoio,ControleApoio);
CREATE INDEX iRequisicaoOriginal8  ON RequisicaoOriginal (LaboratorioExterno,MaterialSaida);
CREATE INDEX iRequisicaoOriginal9  ON RequisicaoOriginal (LaboratorioExterno,MaterialRetorno);
CREATE INDEX iRequisicaoOriginal10 ON RequisicaoOriginal (IdPaciente,ContaExame);
CREATE INDEX iRequisicaoOriginal11 ON RequisicaoOriginal (DataIni,IdExame,ContaExame);


DROP TABLE IF EXISTS SN;
CREATE TABLE SN
(
  Id                  SERIAL         NOT NULL,
  Sigla               varchar(1),
  Descricao           varchar(3),
  CONSTRAINT iSN1 PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX iSN2 ON SN (Sigla);
CREATE UNIQUE INDEX iSN3 ON SN (Descricao);


DROP TABLE IF EXISTS Senhas;
CREATE TABLE Senhas
(
  Id                  SERIAL         NOT NULL,
  NomeUsuario         VARCHAR(15)    NOT NULL,
  NomeCompleto        VARCHAR(100),
  SenhaUsuario        varchar(6)     NOT NULL,
  DataCadastro        DATE           NOT NULL,
  DataExpira          DATE,
  Assinatura          BYTEA,
  UsarAssinatura      int            DEFAULT 0,
  Bloqueado           int            DEFAULT 0,
  Administrador       int            DEFAULT 0,
  CONSTRAINT iSenhas1 PRIMARY KEY (Id,NomeUsuario)
);
CREATE UNIQUE INDEX iSenhas2 ON Senhas (NomeUsuario,Id);


DROP TABLE IF EXISTS SenhasInterfaces;
CREATE TABLE SenhasInterfaces
(
  Id                  SERIAL         NOT NULL,
  NomeUsuario         VARCHAR(15)    NOT NULL,
  NomeCompleto        VARCHAR(100),
  SenhaUsuario        varchar(6)     NOT NULL,
  DataCadastro        DATE,
  DataExpira          DATE,
  Bloqueado           int            DEFAULT 0,
  Administrador       int            DEFAULT 0,
  CONSTRAINT iSenhasInterfaces1 PRIMARY KEY (Id,NomeUsuario)
);
CREATE UNIQUE INDEX iSenhasInterfaces2 ON SenhasInterfaces (NomeUsuario,Id);


DROP TABLE IF EXISTS SituacaoExames;
CREATE TABLE SituacaoExames
(
  Id                  SERIAL         NOT NULL,
  Descricao           varchar(40),
  CONSTRAINT iSituacaoExames1 PRIMARY KEY (Id)
);
CREATE INDEX iSituacaoExames2 ON SituacaoExames (Descricao,Id);


DROP TABLE IF EXISTS TabelaExames;
CREATE TABLE TabelaExames
(
  Id                  SERIAL         NOT NULL,
  SiglaTabela         VARCHAR(15)    NOT NULL,
  NomeTabela          varchar(50)    NOT NULL,
  Orcamento           int            DEFAULT 0,
  Bloqueado           int            DEFAULT 0,
  CONSTRAINT iTabelaExames1 PRIMARY KEY (Id)
);
CREATE INDEX iTabelaExames2 ON TabelaExames (SiglaTabela);
CREATE INDEX iTabelaExames3 ON TabelaExames (NomeTabela,SiglaTabela);


DROP TABLE IF EXISTS TextosProntos;
CREATE TABLE TextosProntos
(
  Id                  SERIAL         NOT NULL,
  Texto               varchar(100),
  CONSTRAINT iTextosProntos1 PRIMARY KEY (Id)
);
CREATE INDEX iTextosProntos2 ON TextosProntos (Texto);


DROP TABLE IF EXISTS UF;
CREATE TABLE UF
(
  Id                  SERIAL         NOT NULL,
  Sigla               varchar(2),
  Descricao           varchar(20),
  CONSTRAINT iUF1 PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX iUF2 ON UF (Sigla);
CREATE UNIQUE INDEX iUF3 ON UF (Descricao);


DROP TABLE IF EXISTS Cor;
CREATE TABLE Cor
(
  Id                  SERIAL         NOT NULL,
  Cor                 varchar(8)     NOT NULL,
  CONSTRAINT iCor1 PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX iCor2 ON Cor (Cor);


DROP TABLE IF EXISTS Postos;
CREATE TABLE Postos
(
  Id                  SERIAL         NOT NULL,
  NomePosto           varchar(45)    NOT NULL,
  Responsavel         varchar(45)    NOT NULL,
  Logradouro          varchar(8),
  Endereco            varchar(60),
  Complemento         VARCHAR(25),
  Bairro              varchar(45),
  Cidade              varchar(45),
  UF                  varchar(2),
  CEP                 varchar(8),
  Telefone            VARCHAR(60),
  CONSTRAINT iPostos1 PRIMARY KEY (Id)
);
CREATE INDEX iPostos2 ON Postos (NomePosto);
CREATE INDEX iPostos3 ON Postos (UF,Cidade,Bairro,Endereco);


DROP TABLE IF EXISTS Sexo;
CREATE TABLE Sexo
(
  Id                  SERIAL         NOT NULL,
  Sigla               varchar(1),
  Descricao           varchar(15),
  CONSTRAINT iSexo1 PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX iSexo2 ON Sexo (Descricao);


DROP TABLE IF EXISTS TipoSanguineo;
CREATE TABLE TipoSanguineo
(
  Id                  SERIAL         NOT NULL,
  Tipo                varchar(2)     NOT NULL,
  RH                  varchar(1)     NOT NULL,
  DoaPara             varchar(40),
  RecebeDe            varchar(40),
  CONSTRAINT iTipoSanguineo1 PRIMARY KEY (Id)
);
CREATE INDEX iTipoSanguineo2 ON TipoSanguineo (Tipo,RH);


DROP TABLE IF EXISTS TituloExames;
CREATE TABLE TituloExames
(
  Id                  SERIAL         NOT NULL,
  TituloExame         varchar(60)    NOT NULL,
  CONSTRAINT iTituloExames1 PRIMARY KEY (Id)
);
CREATE INDEX iTituloExames2 ON TituloExames (TituloExame,Id);
