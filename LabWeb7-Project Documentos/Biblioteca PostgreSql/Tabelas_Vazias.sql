/*
    "Cria"ção das tabelas "PostgreSQL" para a base "LAB_WEB7"
    "Data": 28/08/2022
    "Atualizado" em: 16/09/2022
    "Convertido" de "MSSQL" para "PostgreSQL"

	"A" "L"Ó"GICA" "PARA" "IMPLANTAR" "CLIENTES" "NOVOS":

    1) "Cria" um registro do cliente na tabela "LABWEB7Empresas"."EmpresaCliente"  ("SOMENTE" "ESTA" "DEVER"Á "ESTAR" "PREENCHIDA")
       "Dados": "CNPJ" do cliente, "Email" principal do cliente, "String" de "Conex"ão, "Limite" de "Usu"ários.
      
    2) "Cria" uma tabela do cliente em "LABWEB7Empresas"."EmpresaLogin<CNPJ>" ("EmpresaLogin" + nome do cliente) 
    3) "Cria" o "Banco" de dados do cliente chamada de "LABWEB7Cliente" ("Cliente" = "LABWEB7" + nome curto do cliente)

*/
-- Conectar ao banco: \c LABWEB7

----Tem que "dropar" nesta ordem, por causa dos relacionamentos
--Feito pelo Qoder em 16/08/2026 — lista completa das tabelas do script, filhos antes dos pais
--DROP TABLE IF EXISTS "CatalogoRecebimentosExames";
--DROP TABLE IF EXISTS "CatalogoRecebimentosFormas";
--DROP TABLE IF EXISTS "FichasInternas";
--DROP TABLE IF EXISTS "FichasLotes";
--DROP TABLE IF EXISTS "FichasPlanilhas";
--DROP TABLE IF EXISTS "IntegracaoDadosExecucaoArquivo";
--DROP TABLE IF EXISTS "ItensExamesRealizados";
--DROP TABLE IF EXISTS "ItensExamesRealizadosAM";
--DROP TABLE IF EXISTS "LogArquivos";
--DROP TABLE IF EXISTS "IntegracaoDadosExecucao";
--DROP TABLE IF EXISTS "IntegracaoDadosLayout";
--DROP TABLE IF EXISTS "IntegracaoDadosConfiguracao";
--DROP TABLE IF EXISTS "IntegracaoDadosArmazenamento";
--DROP TABLE IF EXISTS "IntegracaoDadosPeriodicidade";
--DROP TABLE IF EXISTS "CatalogoRecebimentos";
--DROP TABLE IF EXISTS "ExamesExportados";
--DROP TABLE IF EXISTS "ExamesImpressos";
--DROP TABLE IF EXISTS "ExamesPendentes";
--DROP TABLE IF EXISTS "ExamesRealizados";
--DROP TABLE IF EXISTS "ExamesRealizadosAM";
--DROP TABLE IF EXISTS "ExameReferencia";
--DROP TABLE IF EXISTS "ERTemporario";
--DROP TABLE IF EXISTS "Postos";
--DROP TABLE IF EXISTS "UsuariosWeb";
--DROP TABLE IF EXISTS "ClasseExames";
--DROP TABLE IF EXISTS "ContasRecebimento";
--DROP TABLE IF EXISTS "FormasRecebimento";
--DROP TABLE IF EXISTS "Instituicao";
--DROP TABLE IF EXISTS "Medicos";
--DROP TABLE IF EXISTS "Pacientes";
--DROP TABLE IF EXISTS "Senhas";
--DROP TABLE IF EXISTS "TabelaExames";
--DROP TABLE IF EXISTS "Assinaturas";
--DROP TABLE IF EXISTS "Configuracoes";
--DROP TABLE IF EXISTS "ControleConcorrencia";
--DROP TABLE IF EXISTS "Cor";
--DROP TABLE IF EXISTS "Empresa";
--DROP TABLE IF EXISTS "EstadoCivil";
--DROP TABLE IF EXISTS "Logradouro";
--DROP TABLE IF EXISTS "MemoAuxiliar";
--DROP TABLE IF EXISTS "PlanoExames";
--DROP TABLE IF EXISTS "Rastreamentos";
--DROP TABLE IF EXISTS "ReCaptchaMonitoramento";
--DROP TABLE IF EXISTS "Sexo";
--DROP TABLE IF EXISTS "SituacaoExames";
--DROP TABLE IF EXISTS "TextosProntos";
--DROP TABLE IF EXISTS "TipoSanguineo";
--DROP TABLE IF EXISTS "TituloExames";
--DROP TABLE IF EXISTS "UF";
--..Qoder


CREATE TABLE IF NOT EXISTS "ClasseExames"
(
  "Id"                  SERIAL         NOT NULL,
  "RefExame"            varchar(50),
  "Etiquetas"           int            NOT NULL DEFAULT 0,
  "TipoMapa"            varchar(1),
  "Assinatura1"         int            NOT NULL DEFAULT 0,
  "Assinatura2"         int            NOT NULL DEFAULT 0,
  "Assinatura3"         int            NOT NULL DEFAULT 0,
  "Assinatura4"         int            NOT NULL DEFAULT 0,
  "ImgAss1"             BYTEA          NOT NULL,    ----a primeira assinatura é obrigatória!
  "ImgAss2"             BYTEA,
  "ImgAss3"             BYTEA,
  "ImgAss4"             BYTEA,
  "NomeAss1"            varchar(100)   NOT NULL,    ----o nome do arquivo da primeira assinatura é obrigatório!
  "NomeAss2"            varchar(100),
  "NomeAss3"            varchar(100),
  "NomeAss4"            varchar(100),
  "Marcado"             int            NOT NULL DEFAULT 0,
  "Planilha"            int            NOT NULL DEFAULT 0,
  "MHI"                 int            NOT NULL DEFAULT 0,   ----índice que ordena a Folha no Mapa Horizontal
  "LaboratorioExterno"  varchar(20),                         ----Laboratório Externo ou Instituição exclusiva da Folha de Exames
  CONSTRAINT "iClasseExames1" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "Assinaturas" (
  "Id"                  SERIAL         NOT NULL,
  "Assinatura1"         BYTEA,
  "Usar1"               int            NOT NULL DEFAULT 0,
  "CRBio1"              varchar(12)    NOT NULL DEFAULT '123456789',
  "Assinatura2"         BYTEA,
  "Usar2"               int            NOT NULL DEFAULT 0,
  "CRBio2"              varchar(12),
  "Assinatura3"         BYTEA,
  "Usar3"               int            NOT NULL DEFAULT 0,
  "CRBio3"              varchar(12),
  "Assinatura4"         BYTEA,
  "Usar4"               int            NOT NULL DEFAULT 0,
  "CRBio4"              varchar(12),
  CONSTRAINT "iAssinaturas1" PRIMARY KEY ("Id")
);


CREATE TABLE IF NOT EXISTS "Pacientes"
(
  "Id"                  SERIAL         NOT NULL,
  "IdPacienteExterno"   varchar(20),
  "NomePaciente"        varchar(100)   NOT NULL,
  "NomeSocial"          varchar(100),
  "NomePai"             varchar(100),
  "NomeMae"             varchar(100),
  "Nascimento"          DATE             NOT NULL,
  "CPF"                 varchar(11),
  "TipoDocumento"       int            NOT NULL,
  "Identidade"          varchar(20),
  "Emissor"             int            NOT NULL DEFAULT 0,
  "CarteiraSUS"         varchar(15),
  "EstadoCivil"         int            NOT NULL DEFAULT 0,
  "Sexo"                varchar(1),
  "Cor"                 varchar(7),
  "EtniaIndigena"       varchar(60),
  "TipoSanguineo"       varchar(3),
  "DUM"                 DATE,
  "TempoGestacao"       int            NOT NULL DEFAULT 0,
  "Profissao"           varchar(100),
  "Naturalidade"        varchar(30),
  "Nacionalidade"       varchar(30),
  "DataEntradaBrasil"   DATE,
  "Logradouro"          varchar(8),
  "Endereco"            varchar(100),
  "Numero"              varchar(15),
  "Complemento"         varchar(25),
  "Bairro"              varchar(45),
  "Cidade"              varchar(45),
  "UF"                  varchar(2),
  "CEP"                 varchar(8),
  "Telefone"            varchar(15),
  "Email"               varchar(100),
  "Observacao"          varchar(2000),
  "DataEntrada"         DATE             NOT NULL, -- Feito pelo Qoder em 22/08/2026 — datas de negócio em DATE (migração TIMESTAMPTZ→DATE)
  "DataBaixa"           DATE,
  "StatusBaixa"         int            NOT NULL DEFAULT 0,
  "DataRegistro"        TIMESTAMPTZ      NOT NULL,
  CONSTRAINT "iPacientes1" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "ERTemporario"
(
  "Id"                  SERIAL         NOT NULL,
  "ExameId"             int            NOT NULL DEFAULT 0,
  "PacienteId"          int            NOT NULL DEFAULT 0,
  "InstituicaoId"       int            NOT NULL DEFAULT 0,
  "TabelaExamesId"      int            NOT NULL DEFAULT 0,
  "MedicoId"            int            NOT NULL DEFAULT 0,
  "Sequencial"          int            NOT NULL DEFAULT 0,
  "ClasseExamesId"      int            NOT NULL DEFAULT 0,
  "HistoricoClinico"    varchar(2000),
  "DataIni"             DATE, -- Feito pelo Qoder em 22/08/2026 — datas de negócio em DATE
  "DataFim"             DATE,
  "Liberacao"           int            NOT NULL DEFAULT 0,
  "DataExame"           DATE,
  "DataEntrega"         DATE,
  "Baixado"             int            NOT NULL DEFAULT 0,
  CONSTRAINT "iERTemporario1" PRIMARY KEY ("Id")
);


CREATE TABLE IF NOT EXISTS "EstadoCivil"
(
  "Id"                  SERIAL         NOT NULL,
  "Descricao"           varchar(10)    NOT NULL,
  CONSTRAINT "iEstadoCivil1" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "TabelaExames"
(
  "Id"                  SERIAL         NOT NULL,
  "SiglaTabela"         varchar(20)    NOT NULL,
  "NomeTabela"          varchar(50)    NOT NULL,
  "Orcamento"           int            NOT NULL DEFAULT 0,
  "Bloqueado"           int            NOT NULL DEFAULT 0,
  CONSTRAINT "iTabelaExames1" PRIMARY KEY ("Id"),
  CONSTRAINT "iTabelaExames2" UNIQUE("SiglaTabela")   ----RESTRIÇÃO EXCLUSIVA DE COLUNA: a coluna SiglaTabela JAMAIS conterá outro valor igual!
);

CREATE TABLE IF NOT EXISTS "ExameReferencia"
(
  "Id"                  SERIAL         NOT NULL,
  "ContaExame"          varchar(11)    NOT NULL,
  "TabelaExamesId"      int            NOT NULL,
  "ConteudoBinario"     BYTEA          NOT NULL,
  "FormatoOrigem"       varchar(10)    NOT NULL DEFAULT 'RTF',
  "AlinhaLaudo"         int            NOT NULL DEFAULT 0,
  "DataCriacao"         TIMESTAMPTZ    NOT NULL,
  "DataAlteracao"       TIMESTAMPTZ    NOT NULL,
  "UsuarioAlteracao"    varchar(100)   NOT NULL,
  "Versao"              int            NOT NULL DEFAULT 1,
  CONSTRAINT "iExameReferencia1" PRIMARY KEY ("Id"),
  CONSTRAINT "iExameReferencia_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id") ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE TABLE IF NOT EXISTS "Instituicao"
(
  "Id"                  SERIAL         NOT NULL,
  "Sigla"               varchar(20)    NOT NULL,
  "Nome"                varchar(100)   NOT NULL,
  "CNPJ"                varchar(14)    NOT NULL,
  "Sequencial"          int            NOT NULL DEFAULT 0,
  "Email"               varchar(100)   NOT NULL,
  "TituloTimbre"        varchar(60),
  "SubTituloTimbre"     varchar(80),
  "Timbre"              BYTEA,
  "Logomarca"           BYTEA,
  "NomeTimbre"          varchar(250),
  "NomeLogomarca"       varchar(250),
  "CarimboSN"           int            NOT NULL DEFAULT 0,
  "TimbreSN"            int            NOT NULL DEFAULT 0,
  "Logradouro"          varchar(8),
  "Endereco"            varchar(100),
  "Numero"              varchar(15),
  "Complemento"         varchar(25),
  "Bairro"              varchar(45),
  "Cidade"              varchar(45),
  "UF"                  varchar(2),
  "CEP"                 varchar(8),
  "Contato"             varchar(60)    NOT NULL,
  "Telefone"            varchar(15)    NOT NULL,
  "Celular"             varchar(15),
  "UsuarioCaminhoFTP"   varchar(250),
  "UsuarioEmailFTP"     varchar(150),
  "UsuarioPortaFTP"     int            NULL,
  "UsuarioSenhaFTP"     varchar(60),
  "ValorExameCitologia" DECIMAL(18,4),
  "Propaganda"          int            NULL,
  "AvisoRodape1"        varchar(140),
  "AvisoRodape2"        varchar(140),
  CONSTRAINT "iInstituicao1" PRIMARY KEY ("Id"),
  CONSTRAINT "iInstituicao2" UNIQUE("Sigla")   ----RESTRIÇÃO EXCLUSIVA DE COLUNA: a coluna Sigla JAMAIS conterá outro valor igual!
);

CREATE TABLE IF NOT EXISTS "Postos"
(
  "Id"                  SERIAL         NOT NULL,
  "InstituicaoId"       int            NOT NULL,
  "SiglaPosto"          varchar(20)    NOT NULL,
  "NomePosto"           varchar(60)    NOT NULL,
  "Responsavel"         varchar(60)    NOT NULL,
  "Logradouro"          varchar(8),
  "Endereco"            varchar(100),
  "Numero"              varchar(15),
  "Complemento"         varchar(25),
  "Bairro"              varchar(45),
  "Cidade"              varchar(45),
  "UF"                  varchar(2),
  "CEP"                 varchar(8),
  "Telefone"            varchar(60),
  CONSTRAINT "iPostos1" PRIMARY KEY ("Id"),
  CONSTRAINT "iPostos_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id") ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS "iPostos_InstituicaoId" ON "Postos" ("InstituicaoId");

CREATE TABLE IF NOT EXISTS "Medicos"
(
  "Id"                  SERIAL         NOT NULL,
  "NomeMedico"          varchar(100)   NOT NULL,
  "Especialidade"       varchar(100),
  "CRM"                 varchar(15)    NOT NULL,
  "Telefone"            varchar(15),
  "Email"               varchar(100),
  CONSTRAINT "iMedicos1" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "ExamesRealizados"
(
  "Id"                  SERIAL         NOT NULL,
  "PacienteId"          int            NOT NULL DEFAULT 0,
  "TabelaExamesId"      int            NOT NULL DEFAULT 0,
  "InstituicaoId"       int            NOT NULL DEFAULT 0,
  "PostoId"             int,
  "MedicoId"            int            NOT NULL DEFAULT 0,
  "Sequencial"          int            NOT NULL DEFAULT 0,
  "LaboratorioApoio"    varchar(20),
  "ControleApoio"       varchar(20)    NOT NULL,
  "HistoricoClinico"    varchar(2000),
  "ExameColado"         varchar(250),
  "ExameColadoImagens"  varchar(250),
  "TravaColado"         int            NOT NULL DEFAULT 0,
  "DataIni"             DATE             NOT NULL, -- Feito pelo Qoder em 22/08/2026 — datas de negócio em DATE
  "DataFim"             DATE,
  "Liberacao"           int            NOT NULL DEFAULT 0,
  "DataExame"           DATE,
  "DataColeta"          varchar(10),
  "DataEntrega"         DATE,
  "Baixado"             int            NOT NULL DEFAULT 0,
  "EnviarEmail"         int            NOT NULL DEFAULT 0,
  "Situacao"            int            NOT NULL DEFAULT 0,
  "TotalImpresso"       int            NOT NULL DEFAULT 0,
  "Faturado"            BOOLEAN        DEFAULT FALSE,
  "EmCatalogoRecebimentos" BOOLEAN     DEFAULT FALSE,
  CONSTRAINT "iExamesRealizados1" PRIMARY KEY ("Id"),
  CONSTRAINT "iExamesRealizados_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
  CONSTRAINT "iExamesRealizados_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id"),
  CONSTRAINT "iExamesRealizados_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
  CONSTRAINT "iExamesRealizados_Medicos" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id"),
  CONSTRAINT "iExamesRealizados_Postos" FOREIGN KEY ("PostoId") REFERENCES "Postos"("Id")
);

CREATE TABLE IF NOT EXISTS "ExamesRealizadosAM"
(
  "Id"                  SERIAL         NOT NULL,
  "OrigemId"            int            NOT NULL,
  "PacienteId"          int            NOT NULL DEFAULT 0,
  "TabelaExamesId"      int            NOT NULL DEFAULT 0,
  "InstituicaoId"       int            NOT NULL DEFAULT 0,
  "PostoId"             int,
  "MedicoId"            int            NOT NULL DEFAULT 0,
  "Sequencial"          int            NOT NULL DEFAULT 0,
  "LaboratorioApoio"    varchar(20),
  "ControleApoio"       varchar(20)    NOT NULL,
  "HistoricoClinico"    varchar(2000),
  "ExameColado"         varchar(250),
  "ExameColadoImagens"  varchar(250),
  "TravaColado"         int            NOT NULL DEFAULT 0,
  "DataIni"             DATE             NOT NULL, -- Feito pelo Qoder em 22/08/2026 — datas de negócio em DATE
  "DataFim"             DATE,
  "Liberacao"           int            NOT NULL DEFAULT 0,
  "DataExame"           DATE,
  "DataColeta"          varchar(10),
  "DataEntrega"         DATE,
  "Baixado"             int            NOT NULL DEFAULT 0,
  "EnviarEmail"         int            NOT NULL DEFAULT 0,
  "Situacao"            int            NOT NULL DEFAULT 0,
  "TotalImpresso"       int            NOT NULL DEFAULT 0,
  "Faturado"            BOOLEAN        DEFAULT FALSE,
  "EmCatalogoRecebimentos" BOOLEAN     DEFAULT FALSE,
  CONSTRAINT "iExamesRealizadosAM1" PRIMARY KEY ("Id"),
  CONSTRAINT "iExamesRealizadosAM_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
  CONSTRAINT "iExamesRealizadosAM_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id"),
  CONSTRAINT "iExamesRealizadosAM_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
  CONSTRAINT "iExamesRealizadosAM_Medicos" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id"),
  CONSTRAINT "iExamesRealizadosAM_Postos" FOREIGN KEY ("PostoId") REFERENCES "Postos"("Id")
);

CREATE TABLE IF NOT EXISTS "ExamesExportados"
(
  "Id"                  SERIAL         NOT NULL,
  "ExameId"             int            NOT NULL DEFAULT 0,
  "PacienteId"          int            NOT NULL DEFAULT 0,
  "InstituicaoId"       int            NOT NULL DEFAULT 0,
  "TabelaExamesId"      int            NOT NULL DEFAULT 0,
  "MedicoId"            int            NOT NULL DEFAULT 0,
  "Sequencial"          int            NOT NULL DEFAULT 0,
  "LaboratorioApoio"    varchar(20),
  "ControleApoio"       varchar(20)    NOT NULL,
  "DataColeta"          DATE, -- Feito pelo Qoder em 22/08/2026 — data de negócio em DATE (DataExportado/DataImportado seguem TIMESTAMPTZ)
  "DataExportado"       TIMESTAMPTZ,
  "DataImportado"       TIMESTAMPTZ,
  CONSTRAINT "iExamesExportados1" PRIMARY KEY ("Id"),
  CONSTRAINT "iExamesExportados_Pacientes" FOREIGN KEY("PacienteId") REFERENCES "Pacientes"("Id"),
  CONSTRAINT "iExamesExportados_ExamesRealizados" FOREIGN KEY ("ExameId") REFERENCES "ExamesRealizados"("Id"),
  CONSTRAINT "iExamesExportados_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
  CONSTRAINT "iExamesExportados_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id"),
  CONSTRAINT "iExamesExportados_Medicos" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id")
);

CREATE TABLE IF NOT EXISTS "ExamesImpressos"
(
  "Id"                  SERIAL         NOT NULL,
  "PacienteId"          int            NOT NULL DEFAULT 0,
  "InstituicaoId"       int            NOT NULL DEFAULT 0,
  "TabelaExamesId"      int            NOT NULL DEFAULT 0,
  "Sequencial"          int            NOT NULL DEFAULT 0,
  "DataExame"           DATE, -- Feito pelo Qoder em 22/08/2026 — data de negócio em DATE (DataImpresso segue TIMESTAMPTZ)
  "DataImpresso"        TIMESTAMPTZ,
  "TotalImpresso"       int            NOT NULL DEFAULT 0,
  CONSTRAINT "iExamesImpressos1" PRIMARY KEY ("Id"),
  CONSTRAINT "iExamesImpressos_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
  CONSTRAINT "iExamesImpressos_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
  CONSTRAINT "iExamesImpressos_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id")
);

CREATE TABLE IF NOT EXISTS "ExamesPendentes"
(
  "Id"                  SERIAL         NOT NULL,
  "PacienteId"          int            NOT NULL DEFAULT 0,
  "InstituicaoId"       int            NOT NULL DEFAULT 0,
  "TabelaExamesId"      int            NOT NULL DEFAULT 0,
  "MedicoId"            int            NOT NULL DEFAULT 0,
  "ClasseExamesId"      int            NOT NULL DEFAULT 0,
  "Sequencial"          int            NOT NULL DEFAULT 0,
  "LaboratorioApoio"    varchar(20),
  "ControleApoio"       varchar(15),
  "ContaExame"          varchar(11),
  "NomeFolha"           varchar(50),
  "NomeGrupo"           varchar(50),
  "NomeItem"            varchar(50),
  "DataIni"             DATE, -- Feito pelo Qoder em 22/08/2026 — data de negócio em DATE
  CONSTRAINT "iExamesPendentes1" PRIMARY KEY ("Id"),
  CONSTRAINT "iExamesPendentes_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
  CONSTRAINT "iExamesPendentes_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
  CONSTRAINT "iExamesPendentes_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id"),
  CONSTRAINT "iExamesPendentes_Medicos" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id"),
  CONSTRAINT "iExamesPendentes_ClasseExames" FOREIGN KEY ("ClasseExamesId") REFERENCES "ClasseExames"("Id")
);

CREATE TABLE IF NOT EXISTS "FichasInternas"
(
  "Id"                  SERIAL         NOT NULL,
  "NomeFicha"           varchar(50),
  "ContaExame"          varchar(11),
  "Descricao"           varchar(50),
  "Resultado"           varchar(30),
  "MapaHorizontal"      varchar(6),
  "ExamesRealizadosId"  int            NOT NULL DEFAULT 0,
  "PacienteId"          int            NOT NULL DEFAULT 0,
  "MedicoId"            int            NOT NULL DEFAULT 0,
  "InstituicaoId"       int            NOT NULL DEFAULT 0,
  "DataExame"           DATE             NOT NULL, -- Feito pelo Qoder em 22/08/2026 — datas de negócio em DATE
  "ControleApoio"       varchar(20),
  "Sequencial"          int            NOT NULL DEFAULT 0,
  "HistoricoClinico"    varchar(2000),
  "DataIni"             DATE             NOT NULL,
  "DataFim"             DATE,
  "Pagina"              int            NOT NULL DEFAULT 0,
  "Coluna1"             varchar(6),
  "Coluna2"             varchar(6),
  "Coluna3"             varchar(6),
  "Coluna4"             varchar(6),
  "Coluna5"             varchar(6),
  "Coluna6"             varchar(6),
  "Coluna7"             varchar(6),
  "Coluna8"             varchar(6),
  "Coluna9"             varchar(6),
  "Coluna10"            varchar(6),
  "Coluna11"            varchar(6),
  "Coluna12"            varchar(6),
  "Coluna13"            varchar(6),
  "Coluna14"            varchar(6),
  "Coluna15"            varchar(6),
  "Coluna16"            varchar(6),
  "Coluna17"            varchar(6),
  "Coluna18"            varchar(6),
  CONSTRAINT "iFichasInternas1" PRIMARY KEY ("Id"),
  CONSTRAINT "iFichasInternas_ExamesRealizados" FOREIGN KEY ("ExamesRealizadosId") REFERENCES "ExamesRealizados"("Id"),
  CONSTRAINT "iFichasInternas_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
  CONSTRAINT "iFichasInternas_Medicos" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id"),
  CONSTRAINT "iFichasInternas_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id")
);

CREATE TABLE IF NOT EXISTS "FichasLotes"
(
  "Id"                  SERIAL         NOT NULL,
  "NomeFicha"           varchar(50),
  "ContaExame"          varchar(11),
  "Descricao"           varchar(50),
  "Resultado"           varchar(30),
  "MapaHorizontal"      varchar(6),
  "ExamesRealizadosId"  int            NOT NULL DEFAULT 0,
  "PacienteId"          int            NOT NULL DEFAULT 0,
  "MedicoId"            int            NOT NULL DEFAULT 0,
  "InstituicaoId"       int            NOT NULL DEFAULT 0,
  "TabelaExamesId"      int            NOT NULL DEFAULT 0,
  "DataExame"           DATE, -- Feito pelo Qoder em 22/08/2026 — datas de negócio em DATE
  "ControleApoio"       varchar(20),
  "Sequencial"          int            NOT NULL DEFAULT 0,
  "HistoricoClinico"    varchar(2000),
  "DataIni"             DATE,
  "DataFim"             DATE,
  "Lote"                int            NOT NULL DEFAULT 0,
  "LiberadoExclusao"    varchar(1),
  CONSTRAINT "iFichasLotes1" PRIMARY KEY ("Id"),
  CONSTRAINT "iFichasLotes_ExamesRealizados" FOREIGN KEY ("ExamesRealizadosId") REFERENCES "ExamesRealizados"("Id"),
  CONSTRAINT "iFichasLotes_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
  CONSTRAINT "iFichasLotes_Medicos" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id"),
  CONSTRAINT "iFichasLotes_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
  CONSTRAINT "iFichasLotes_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id")
);

CREATE TABLE IF NOT EXISTS "FichasPlanilhas"
(
  "Id"                  SERIAL         NOT NULL,
  "NomeFicha"           varchar(50),
  "ContaExame"          varchar(11),
  "Descricao"           varchar(50),
  "Resultado"           varchar(30),
  "MapaHorizontal"      varchar(6),
  "ExamesRealizadosId"  int            NOT NULL DEFAULT 0,
  "PacienteId"          int            NOT NULL DEFAULT 0,
  "MedicoId"            int            NOT NULL DEFAULT 0,
  "InstituicaoId"       int            NOT NULL DEFAULT 0,
  "TabelaExamesId"      int            NOT NULL DEFAULT 0,
  "DataExame"           DATE, -- Feito pelo Qoder em 22/08/2026 — datas de negócio em DATE
  "ControleApoio"       varchar(20),
  "Sequencial"          int            NOT NULL DEFAULT 0,
  "HistoricoClinico"    varchar(2000),
  "DataIni"             DATE             NOT NULL,
  "DataFim"             DATE,
  "Lote"                int            NOT NULL DEFAULT 0,
  "LiberadoExclusao"    varchar(1),
  CONSTRAINT "iFichasPlanilhas1" PRIMARY KEY ("Id"),
  CONSTRAINT "iFichasPlanilhas_ExamesRealizados" FOREIGN KEY ("ExamesRealizadosId") REFERENCES "ExamesRealizados"("Id"),
  CONSTRAINT "iFichasPlanilhas_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
  CONSTRAINT "iFichasPlanilhas_Medicos" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id"),
  CONSTRAINT "iFichasPlanilhas_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
  CONSTRAINT "iFichasPlanilhas_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id")
);

CREATE TABLE IF NOT EXISTS "ItensExamesRealizados"
(
  "Id"                  SERIAL         NOT NULL,
  "PacienteId"          int            NOT NULL DEFAULT 0,
  "ClasseExamesId"      int            NOT NULL DEFAULT 0,
  "ClasseExamesNome"    varchar(50)    NOT NULL,
  "ExameRealizadoId"    int            NOT NULL DEFAULT 0,
  "TabelaExamesId"      int            NOT NULL DEFAULT 0,
  "OrdemItem"           int            NOT NULL DEFAULT 0,
  "RefExame"            varchar(50)    NOT NULL,
  "RefItem"             varchar(50)    NOT NULL,
  "ContaExame"          varchar(11)    NOT NULL,
  "CitoTituloFolha"     int            NOT NULL DEFAULT 0,
  "CitoTituloExame"     int            NOT NULL DEFAULT 0,
  "CitoRefItem"         int            NOT NULL DEFAULT 0,
  "InstituicaoId"       int            NOT NULL DEFAULT 0,
  "Sequencial"          int            NOT NULL DEFAULT 0,
  "LaboratorioApoio"    varchar(20),
  "ControleApoio"       varchar(20),
  "LaboratorioExterno"  varchar(20),
  "MaterialSaida"       varchar(16),
  "MaterialRetorno"     varchar(16),
  "Descricao"           varchar(50),
  "CitoDescricao"       varchar(2000),
  "Resultado"           varchar(30),
  "UnidadeMedida"       varchar(20),
  "Referencia"          varchar(60),
  "ValorItem"           DECIMAL(18,4),
  "Laudo"               BYTEA,
  "Etiquetas"           int            NOT NULL DEFAULT 0,
  "DataEntregaParcial"  DATE, -- Feito pelo Qoder em 22/08/2026 — data de negócio em DATE
  "Liberado"            int            NOT NULL DEFAULT 0,
  "Baixado"             int            NOT NULL DEFAULT 0,
  CONSTRAINT "iItensExamesRealizados1" PRIMARY KEY ("Id"),
  CONSTRAINT "iItensExamesRealizados_ExamesRealizados" FOREIGN KEY ("ExameRealizadoId") REFERENCES "ExamesRealizados"("Id") ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT "iItensExamesRealizados_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
  CONSTRAINT "iItensExamesRealizados_ClasseExames" FOREIGN KEY ("ClasseExamesId") REFERENCES "ClasseExames"("Id"),
  CONSTRAINT "iItensExamesRealizados_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id"),
  CONSTRAINT "iItensExamesRealizados_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id")
);

CREATE TABLE IF NOT EXISTS "ItensExamesRealizadosAM"
(
  "Id"                  SERIAL         NOT NULL,
  "OrigemAMId"          int            NOT NULL,
  "PacienteId"          int            NOT NULL DEFAULT 0,
  "ClasseExamesId"      int            NOT NULL DEFAULT 0,
  "ClasseExamesNome"    varchar(50)    NOT NULL,
  "ExameRealizadoAMId"  int            NOT NULL DEFAULT 0,
  "TabelaExamesId"      int            NOT NULL DEFAULT 0,
  "OrdemItem"           int            NOT NULL DEFAULT 0,
  "RefExame"            varchar(50)    NOT NULL,
  "RefItem"             varchar(50)    NOT NULL,
  "ContaExame"          varchar(11)    NOT NULL,
  "CitoTituloFolha"     int            NOT NULL DEFAULT 0,
  "CitoTituloExame"     int            NOT NULL DEFAULT 0,
  "CitoRefItem"         int            NOT NULL DEFAULT 0,
  "InstituicaoId"       int            NOT NULL DEFAULT 0,
  "Sequencial"          int            NOT NULL DEFAULT 0,
  "LaboratorioApoio"    varchar(20),
  "ControleApoio"       varchar(20),
  "LaboratorioExterno"  varchar(20),
  "MaterialSaida"       varchar(16),
  "MaterialRetorno"     varchar(16),
  "Descricao"           varchar(50),
  "CitoDescricao"       varchar(2000),
  "Resultado"           varchar(30),
  "UnidadeMedida"       varchar(20),
  "Referencia"          varchar(60),
  "ValorItem"           DECIMAL(18,4),
  "Laudo"               BYTEA,
  "Etiquetas"           int            NOT NULL DEFAULT 0,
  "DataEntregaParcial"  DATE, -- Feito pelo Qoder em 22/08/2026 — data de negócio em DATE
  "Liberado"            int            NOT NULL DEFAULT 0,
  "Baixado"             int            NOT NULL DEFAULT 0,
  CONSTRAINT "iItensExamesRealizadosAM1" PRIMARY KEY ("Id"),
  CONSTRAINT "iItensExamesRealizadosAM1_ExamesRealizados" FOREIGN KEY ("ExameRealizadoAMId") REFERENCES "ExamesRealizadosAM"("Id") ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT "iItensExamesRealizadosAM1_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
  CONSTRAINT "iItensExamesRealizadosAM1_ClasseExames" FOREIGN KEY ("ClasseExamesId") REFERENCES "ClasseExames"("Id"),
  CONSTRAINT "iItensExamesRealizadosAM1_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id"),
  CONSTRAINT "iItensExamesRealizadosAM1_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id")
);

CREATE TABLE IF NOT EXISTS "Logradouro"
(
  "Id"                  SERIAL        NOT NULL,
  "Descricao"           varchar(8)    NOT NULL,
  CONSTRAINT "iLogradouro1" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "MemoAuxiliar"
(
  "Id"                  SERIAL        NOT NULL,
  "NomeFolha"           varchar(50),
  "Linha1"              varchar(250),
  "Linha2"              varchar(250),
  "Linha3"              varchar(250),
  "Linha4"              varchar(250),
  "Linha5"              varchar(250),
  "Linha6"              varchar(250),
  "CampoMemo"           BYTEA,
  CONSTRAINT "iMemoAuxiliar1" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "PlanoExames"
(
  "Id"                  SERIAL        NOT NULL,
  "ClasseExamesId"      int           NOT NULL DEFAULT 0,
  "CitoInstituicao"     int           NOT NULL DEFAULT 0,
  "CitoTituloFolha"     varchar(60),
  "CitoTituloExame"     int           NOT NULL DEFAULT 0,
  "CitoParteDescricao"  varchar(100),
  "CitoDescricao"       BYTEA,
  "RefExame"            varchar(50)   NOT NULL,
  "RefItem"             varchar(50)   NOT NULL,
  "TabelaExamesId"      int           NOT NULL DEFAULT 0,
  "ContaExame"          varchar(11)   NOT NULL,
  "Descricao"           varchar(50)   NOT NULL,
  "ValorCusto"          DECIMAL(18,4),
  "ValorItem"           DECIMAL(18,4),
  "TABELACH"            varchar(10),
  "QCH"                 int           NOT NULL DEFAULT 0,
  "ICH"                 DECIMAL(18,2),
  "UnidadeMedida"       varchar(20),
  "Referencia"          varchar(60),
  "Etiqueta"            int           NOT NULL DEFAULT 0,
  "Etiquetas"           int           NOT NULL DEFAULT 0,
  "Laudo"               BYTEA,
  "AlinhaLaudo"         int           NOT NULL DEFAULT 0,
  "Seleciona"           int           NOT NULL DEFAULT 0,
  "NaoMostrar"          int           NOT NULL DEFAULT 0,
  "MapaHorizontal"      varchar(6),
  "ResultadoMinimo"     DECIMAL(18,4),
  "ResultadoMaximo"     DECIMAL(18,4),
  "LaboratorioExterno"  varchar(20),
  "PrazoResultadoDias"  int,
  CONSTRAINT "iPlanoExames1" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "Rastreamentos"
(
  "Id"                   SERIAL        NOT NULL,
  "UsuarioId"            int           NOT NULL DEFAULT 0,
  "DataOcorrencia"       TIMESTAMPTZ     NOT NULL,
  "SistemaUtilizado"     varchar(30),
  "VersaoSistema"        varchar(26),
  "OpcaoMenu"            varchar(26),
  "OperacaoRealizada"    varchar(500),
  "OperacaoComplementar" varchar(500),
  "Falha"                varchar(1000),
  Exception            varchar(4000),
  "IPLocal"              varchar(15),
  "IPExterno"            varchar(15),
  "NomeComputador"       varchar(100),
  CONSTRAINT "iRastreamentos1" PRIMARY KEY ("Id")
);

--Feito pelo Qoder em 23/08/2026 — tabela "Requisitar" removida definitivamente: a entidade foi
--eliminada do modelo e a tela "Compactar Requisições" foi excluída do sistema.

CREATE TABLE IF NOT EXISTS "SituacaoExames"
(
  "Id"                   SERIAL       NOT NULL,
  "Descricao"            varchar(40),
  CONSTRAINT "iSituacaoExames1" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "TextosProntos"
(
  "Id"                   SERIAL       NOT NULL,
  "Texto"                varchar(100),
  CONSTRAINT "iTextosProntos1" PRIMARY KEY ("Id"),
  CONSTRAINT "iTextosProntos2" UNIQUE("Texto")   ----RESTRIÇÃO EXCLUSIVA DE COLUNA: a coluna Texto JAMAIS conterá outro valor igual!
);

CREATE TABLE IF NOT EXISTS "UF"
(
  "Id"                   SERIAL       NOT NULL,
  "Sigla"                varchar(2),
  "Descricao"            varchar(20),
  CONSTRAINT "iUF1" PRIMARY KEY ("Id"),
  CONSTRAINT "iUF2" UNIQUE("Sigla")   ----RESTRIÇÃO EXCLUSIVA DE COLUNA
);

CREATE TABLE IF NOT EXISTS "Cor"
(
  "Id"                   SERIAL       NOT NULL,
  "Cor"                  varchar(8)   NOT NULL,
  CONSTRAINT "iCor1" PRIMARY KEY ("Id"),
  CONSTRAINT "iCor2" UNIQUE("Cor")   ----RESTRIÇÃO EXCLUSIVA DE COLUNA
);

CREATE TABLE IF NOT EXISTS "Sexo"
(
  "Id"                   SERIAL       NOT NULL,
  "Sigla"                varchar(1),
  "Descricao"            varchar(15),
  CONSTRAINT "iSexo1" PRIMARY KEY ("Id"),
  CONSTRAINT "iSexo2" UNIQUE("Sigla")   ----RESTRIÇÃO EXCLUSIVA DE COLUNA
);

CREATE TABLE IF NOT EXISTS "TipoSanguineo"
(
  "Id"                   SERIAL       NOT NULL,
  "Tipo"                 varchar(2)   NOT NULL,
  "RH"                   varchar(1)   NOT NULL,
  "DoaPara"              varchar(40),
  "RecebeDe"             varchar(40),
  CONSTRAINT "iTipoSanguineo1" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "TituloExames"
(
  "Id"                   SERIAL       NOT NULL,
  "TituloExame"          varchar(60)  NOT NULL,
  CONSTRAINT "iTituloExames1" PRIMARY KEY ("Id")
);

/* 
    "Tabelas" das "Integra"ções de "Dados" ("Exporta"ção/"Importa"ção) 
	"Pela" ordem do CASCADE
*/
CREATE TABLE IF NOT EXISTS "IntegracaoDadosArmazenamento"
(
  "Id"                             SERIAL        NOT NULL,
  "Senha"                          varchar(100)  NULL,
  "TipoArmazenamento"              int           NOT NULL,
  "Host"                           varchar(500)  NULL,
  "Usuario"                        int           NULL,
  "UsuarioLogin"                   varchar(100)  NULL,
  CONSTRAINT "iIntegracaoDadosArmazenamento1" PRIMARY KEY ("Id")
);


CREATE TABLE IF NOT EXISTS "IntegracaoDadosConfiguracao"
(
  "Id"                             SERIAL        NOT NULL,
  "IntegracaoDadosArmazenamentoId" int           NOT NULL,
  "PastaSaida"                     varchar(500)  NULL,
  "PastaEntrada"                   varchar(500)  NULL,
  "NomeArquivo"                    varchar(200)  NOT NULL,
  "HoraExecucao"                   varchar(5)    NOT NULL,             --Hora inicial que um serviço começa a funcionar
  "HoraEncerramento"               varchar(5)    NULL,                 --Hora máxima que um serviço pode atingir funcionamento
  "DiaExecucao"                    int           NULL,                 --Dia único de execução dentro do mês
  "Periodicidade"                  int           NOT NULL DEFAULT 1,   --1=diario 2=semanal 3=mensal 4=retroativo
  "IntegraUmaUnicaVezNoDia"        SMALLINT      NOT NULL DEFAULT 1,   --True (só permite criar o arquivo uma vez ao dia)
  "PausaDoEventoEmMinutos"         int           NOT NULL DEFAULT 1,   --O serviço irá intercalar pausa de 1 minuto a cada evento
  "PastaEntradaProcessado"         varchar(500)  NULL,
  "PastaEntradaProcessadoErro"     varchar(500)  NULL,
  "PastaEntradaProcessadoParcial"  varchar(500)  NULL,
  "UsuarioPadrao"                  int           NULL,
  CONSTRAINT "iIntegracaoDadosConfiguracao1" PRIMARY KEY ("Id"),
  CONSTRAINT "iIntegracaoDadosConfiguracao2" FOREIGN KEY ("IntegracaoDadosArmazenamentoId") REFERENCES "IntegracaoDadosArmazenamento"("Id") ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE TABLE IF NOT EXISTS "IntegracaoDadosLayout"
(
  "Id"                            SERIAL       NOT NULL,
  "IntegracaoDadosConfiguracaoId" int          NOT NULL,
  "Descricao"                     varchar(60)  NOT NULL,
  "TipoServico"                   int          NOT NULL,
  "Exportacao"                    SMALLINT     NOT NULL,
  "Habilitado"                    SMALLINT     NOT NULL,
  "DataInicial"                   TIMESTAMPTZ    NULL,
  "DataFinal"                     TIMESTAMPTZ    NULL,
  CONSTRAINT "iIntegracaoDadosLayout1" PRIMARY KEY ("Id"),
  CONSTRAINT "iIntegracaoDadosLayout2" FOREIGN KEY ("IntegracaoDadosConfiguracaoId") REFERENCES "IntegracaoDadosConfiguracao"("Id") ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE TABLE IF NOT EXISTS "IntegracaoDadosExecucao"
(
  "Id"                            SERIAL        NOT NULL,
  "IntegracaoDadosLayoutId"       int           NOT NULL,
  "Inicio"                        TIMESTAMPTZ     NOT NULL,
  "Termino"                       TIMESTAMPTZ     NULL,
  "Sucesso"                       SMALLINT      NOT NULL,
  "Resumo"                        varchar(4000) NULL,
  "NomeServico"                   varchar(500)  NULL,
  "NomeArquivo"                   varchar(200)  NULL,
  "Header"                        varchar(1000) NULL,
  "Summary"                       varchar(1000) NULL,
  CONSTRAINT "iIntegracaoDadosExecucao1" PRIMARY KEY ("Id"),
  CONSTRAINT "iIntegracaoDadosExecucao2" FOREIGN KEY ("IntegracaoDadosLayoutId") REFERENCES "IntegracaoDadosLayout"("Id") ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE TABLE IF NOT EXISTS "IntegracaoDadosExecucaoArquivo"
(
  "Id"                            SERIAL        NOT NULL,
  "IntegracaoDadosExecucaoId"     int           NOT NULL,
  "NomeArquivo"                   varchar(200)  NULL,
  "Status"                        int           NOT NULL,
  "Resumo"                        varchar(4000) NULL,
  "NomeArquivoProcessado"         varchar(200)  NULL,
  "NomeArquivoGerado"             varchar(200)  NULL,
  CONSTRAINT "iIntegracaoDadosExecucaoArquivo1" PRIMARY KEY ("Id"),
  CONSTRAINT "iIntegracaoDadosExecucaoArquivo2" FOREIGN KEY ("IntegracaoDadosExecucaoId") REFERENCES "IntegracaoDadosExecucao"("Id") ON DELETE CASCADE ON UPDATE CASCADE
);

/* "Apenas" informação mesmo, pois está fixo no código */
CREATE TABLE IF NOT EXISTS "IntegracaoDadosPeriodicidade"
(
  "Id"                            SERIAL        NOT NULL,
  "TipoPeriodoExtracao"           varchar(12)   NOT NULL,
  CONSTRAINT "iIntegracaoDadosPeriodicidade1" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "LogArquivos"
(
  "Id"                            SERIAL        NOT NULL,
  "IntegracaoDadosLayoutId"       int           NULL,
  "StrRef"                        varchar(250)  NOT NULL,
  "NomeArquivo"                   varchar(200)  NULL,
  "Data"                          TIMESTAMPTZ     NOT NULL,
  "DataPeriodoInicial"            TIMESTAMPTZ     NULL,
  "DataPeriodoFinal"              TIMESTAMPTZ     NULL,
  "TipoServico"                   int           NOT NULL,
  CONSTRAINT "iLogArquivos1" PRIMARY KEY ("Id"),
  CONSTRAINT "iLogArquivos2" FOREIGN KEY ("IntegracaoDadosLayoutId") REFERENCES "IntegracaoDadosLayout"("Id") ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE TABLE IF NOT EXISTS "ControleConcorrencia"
(
  "Processo" varchar(200) NOT NULL,
  "DataHora" TIMESTAMPTZ    NOT NULL,
  CONSTRAINT "iControleConcorrencia1" PRIMARY KEY ("Processo")
);

CREATE TABLE IF NOT EXISTS "Senhas"
(
  "Id"                   SERIAL       NOT NULL,
  "LoginUsuario"         varchar(60)  NOT NULL,             ----Email do usuario (via código) ou qualquer nick válido (via SQL script)
  "NomeUsuario"          varchar(15)  NOT NULL,
  "NomeCompleto"         varchar(100),
  "SenhaUsuario"         varchar(256) NOT NULL,
  "DataCadastro"         TIMESTAMPTZ    NOT NULL,
  "DataExpira"           TIMESTAMPTZ,
  "Assinatura"           BYTEA,
  "UsarAssinatura"       int          NOT NULL DEFAULT 0,
  "NomeAssinatura"       varchar(250),
  "Bloqueado"            int          NOT NULL DEFAULT 0,
  "Administrador"        int          NOT NULL DEFAULT 0,
  "Email"                varchar(100) NOT NULL,
  "EmailConfirmado"      int          NOT NULL DEFAULT 0,
  "CNPJEmpresa"          varchar(14)  NOT NULL,
  CONSTRAINT "iSenhas1" PRIMARY KEY ("Id"),
  CONSTRAINT "iSenhas2" UNIQUE("LoginUsuario")   ----RESTRIÇÃO EXCLUSIVA DE COLUNA: a coluna LoginUsuario JAMAIS conterá outro valor igual!
);

/* "Ter"á somente um único registro, mas precisa ter "ID" para o "Entity", e não terá o "IDENTITY", pois forçamos a primary key ser eternamente = 1 */
CREATE TABLE IF NOT EXISTS "Configuracoes"
(
  "Id"                   int            NOT NULL,
  "ImpressoraCupom1"     varchar(500),
  "ImpressoraCupom2"     varchar(500),
  "ImpressoraCupom3"     varchar(500),
  "UsarImpressoraCupom1" int            NOT NULL DEFAULT 0,
  "UsarImpressoraCupom2" int            NOT NULL DEFAULT 0,
  "UsarImpressoraCupom3" int            NOT NULL DEFAULT 0,
  "FonteNome"            VARCHAR(100)       NULL DEFAULT 'Consolas',
  "FonteTamanho"         int                NULL DEFAULT 8,
  "LarguraPapel"         int                NULL DEFAULT 283,      -- em centésimos de polegada
  "AlturaPapel"          int                NULL DEFAULT 32767,    -- em centésimos de polegada
  "MargemEsquerda"       int                NULL DEFAULT 5,
  "MargemDireita"        int                NULL DEFAULT 5,
  "MargemSuperior"       int                NULL DEFAULT 5,
  "MargemInferior"       int                NULL DEFAULT 5,
  CONSTRAINT "iConfiguracoes1" PRIMARY KEY ("Id"),
  CONSTRAINT "CHK_IdUnico" CHECK ("Id" = 1)
);


CREATE TABLE IF NOT EXISTS "UsuariosWeb"
(
  "Id"                     SERIAL      NOT NULL,
  "SenhaId"                int         NOT NULL,
  "CPFUsuario"             varchar(11) NOT NULL,
  "DataNascimentoUsuario"  DATE          NOT NULL,
  "CNPJEmpresa"            varchar(14) NOT NULL,
  "DataCadastro"           TIMESTAMPTZ   NOT NULL,
  CONSTRAINT "iUsuariosWeb1"            PRIMARY KEY ("Id"),
  CONSTRAINT "iUsuariosWeb_Senhas"      FOREIGN KEY ("SenhaId") REFERENCES "Senhas"("Id") ON DELETE CASCADE ON UPDATE CASCADE
);

----Insere Senhas e UsuariosWeb para testes
---- Inserção de dados iniciais de teste
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM "Senhas" WHERE "Id" > 0) THEN
    INSERT INTO "Senhas"
           ("LoginUsuario", "NomeUsuario", "NomeCompleto", "SenhaUsuario", "DataCadastro", "DataExpira", "Assinatura", "UsarAssinatura", "Bloqueado", "Administrador", "Email", "EmailConfirmado", "CNPJEmpresa") 
    VALUES 
           ('rguilemond@gmail.com', 'Ricardo', 'Ricardo Guilemond', 'BAHImD+dYlY+zWRFNMimXw==', '2023-03-25 00:00:00+00', NULL, NULL, 0, 0, 1, 'rguilemond@gmail.com', 1, '00000000000100');
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "UsuariosWeb" WHERE "Id" > 0) THEN
    INSERT INTO "UsuariosWeb"
           ("SenhaId", "CPFUsuario", "DataNascimentoUsuario", "CNPJEmpresa", "DataCadastro") 
    VALUES 
           (1, '77777777777', '1966-05-05', '00000000000100', '2023-03-25 00:00:00+00');
  END IF;
END $$;


----Controle das empresas/clientes do Sistema
----"StringConexao": determina para qual base de dados o cliente irá se conectar!
CREATE TABLE IF NOT EXISTS "Empresa"
(
  "Id"                  SERIAL         NOT NULL,
  "Matriz"              int            NOT NULL DEFAULT 1, 
  "Filial"              int            NOT NULL DEFAULT 0,    ----Se matriz=1 e filial=0 então trata-se de uma matriz!
  "Sigla"               varchar(20)    NOT NULL,
  "NomeFantasia"        varchar(100)   NOT NULL,
  "RazaoSocial"         varchar(100)   NOT NULL,
  "CNPJ"                char(14)       NOT NULL,
  "Logradouro"          varchar(8),
  "Endereco"            varchar(100),
  "Numero"              varchar(15),
  "Complemento"         varchar(25),
  "Bairro"              varchar(45),
  "Cidade"              varchar(45),
  "UF"                  varchar(2),
  "CEP"                 char(8),
  "Telefones"           varchar(15)    NOT NULL,
  "Email"               varchar(500)   NOT NULL,
  "SiteURL"             varchar(2000),
  "HostLogoMarca"       varchar(2000),
  "UnidadeLogoMarca"    varchar(2),
  "CaminhoLogoMarca"    varchar(2000),
  "NomeLogoMarca"       varchar(500),
  "TituloEmpresa"       varchar(100),
  "SubTituloEmpresa"    varchar(100),
  "Rodape"              varchar(140),
  "StringConexao"       varchar(2000),
  "SmtpServer"          varchar(2000), 
  "SmtpPortSSL"         int            NOT NULL DEFAULT 0,
  "SmtpRequerSSL"       SMALLINT       NOT NULL DEFAULT 0,
  "SmtpPortTLS"         int            NOT NULL DEFAULT 0,
  "SmtpRequerTLS"       SMALLINT       NOT NULL DEFAULT 0, 
  "SmtpUsername"        varchar(500),
  "SmtpPassword"        varchar(500),
  "SmtpName"            varchar(500),
  "SmtpSenhaApp"        varchar(500),
  "PopServer"           varchar(500),
  "PopPortSSL"          int            NOT NULL DEFAULT 0,
  "PopRequerSSL"        SMALLINT       NOT NULL DEFAULT 0,
  "PopUsername"         varchar(500),
  "PopPassword"         varchar(500),
  "PopName"             varchar(500),
  "DataExpira"          TIMESTAMPTZ,
  "DataCadastro"        TIMESTAMPTZ      NOT NULL DEFAULT NOW(),
  CONSTRAINT "iEmpresa1" PRIMARY KEY ("Id"),
  CONSTRAINT "iEmpresa2" UNIQUE ("Sigla", "Matriz", "Filial")
);
------
----Sendo criada a tabela de Empresa pela primeira vez, então o primeiro registro é de testes, que pode
----ser excluído se desejar.
------
DO $$
BEGIN
  IF NOT EXISTS (SELECT * FROM "Empresa" WHERE "Matriz" = 1) THEN
     INSERT INTO "Empresa"
        ("Matriz", "Sigla", "NomeFantasia", "RazaoSocial", "CNPJ", "Logradouro", "Endereco", "Numero", "Complemento", "Bairro", "Cidade", "UF", "CEP",
         "Telefones", "Email", "TituloEmpresa", "SubTituloEmpresa", "Rodape", 
         "StringConexao",
         "SmtpServer", "SmtpPortSSL", "SmtpRequerSSL", "SmtpPortTLS", "SmtpRequerTLS", 
         "SmtpUsername", "SmtpPassword", 
         "SmtpName", "SmtpSenhaApp",
         "PopServer", "PopPortSSL", 
         "PopRequerSSL", "PopUsername", "PopPassword", "PopName",  
         "DataCadastro") 
     VALUES 
        (1, 'TESTE', 'TESTE EMPRESA', 'TESTE EMPRESA FICTICIA', '00000000000100', 'Rua', 'Endereço Teste', '1', '1-andar', 'Barra da Tijuca', 'Rio de Janeiro', 'RJ', '20000000',
         '21992624215', 'ricardoguilemond@gmail.com', 'TESTE LABORATORIOS', 'ANÁLISES CLÍNICAS E PATOLÓGICAS', 'Laboratório Credenciado',
         'Host=localhost;Database=LABWEB7;Username=sistema;Password=Acer@105;',
         'smtp.gmail.com', 465, 1, 587, 1, 
         'ricardoguilemond@gmail.com', 'Acer@105',
         'Ricardo Guilemond', 'fbducjybmmyflfqc',   ----Senha gerada no Google que habilita o App a usar o Email/Smtp livremente para o Email informado SITE.
         'imap.gmail.com', 993,                     ----porta padrão seria 995, mas Google está na 993 e requer SSL
         1, 'ricardoguilemond@gmail.com', 'Acer@105', 'Ricardo Guilemond',
         NOW());
  END IF;
END $$;

CREATE TABLE IF NOT EXISTS "ReCaptchaMonitoramento" 
(
    "Id"          SERIAL        NOT NULL,
    "NomeProjeto" VARCHAR(100),
    "QuantidadeSolicitacoes" INT NOT NULL DEFAULT 0,
    "MesReferencia" INT NOT NULL,
    "AnoReferencia" INT NOT NULL,
    CONSTRAINT "iReCaptchaMonitoramento1" PRIMARY KEY ("Id")
);

-- ==============================================================================
-- Tabelas do Catálogo de Recebimentos
-- ==============================================================================

CREATE TABLE IF NOT EXISTS "ContasRecebimento"
(
  "Id"                  SERIAL         NOT NULL,
  "Nome"                varchar(100)   NOT NULL,
  "Tipo"                int            NOT NULL DEFAULT 4,
  "Identificacao"       varchar(100),
  "PadraoPortaria"      BOOLEAN        DEFAULT FALSE,
  "Ativo"               BOOLEAN        DEFAULT TRUE,
  "DataRegistro"        TIMESTAMPTZ      NOT NULL DEFAULT NOW(),
  CONSTRAINT "iContasRecebimento1" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "iContasRecebimento_Ativo" ON "ContasRecebimento" ("Ativo");

CREATE INDEX IF NOT EXISTS "iContasRecebimento_PadraoPortaria" ON "ContasRecebimento" ("PadraoPortaria") WHERE "PadraoPortaria" = TRUE;

CREATE TABLE IF NOT EXISTS "FormasRecebimento"
(
  "Id"                  SERIAL         NOT NULL,
  "Nome"                varchar(100)   NOT NULL,
  "PermiteParticular"   BOOLEAN        DEFAULT TRUE,
  "PermiteInstituicao"  BOOLEAN        DEFAULT TRUE,
  "Ativo"               BOOLEAN        DEFAULT TRUE,
  "DataRegistro"        TIMESTAMPTZ      NOT NULL DEFAULT NOW(),
  CONSTRAINT "iFormasRecebimento1" PRIMARY KEY ("Id"),
  CONSTRAINT "iFormasRecebimento2" UNIQUE("Nome")
);

CREATE INDEX IF NOT EXISTS "iFormasRecebimento_Ativo" ON "FormasRecebimento" ("Ativo");

CREATE TABLE IF NOT EXISTS "CatalogoRecebimentos"
(
  "Id"                  SERIAL         NOT NULL,
  "Origem"              int            NOT NULL DEFAULT 1,
  "InstituicaoId"       int            NOT NULL,
  --Feito pelo Qoder em 16/08/2026 — NULL no recebimento consolidado por instituição/período (migração 013)
  "PacienteId"          int            NULL,
  "PeriodoFaturamento"  varchar(10),
  "ValorTotal"          DECIMAL(18,2)  NOT NULL DEFAULT 0,
  "ValorDesconto"       DECIMAL(18,2)  NOT NULL DEFAULT 0,
  "ValorTotalDevido"    DECIMAL(18,2)  NULL,
  "CobrancaInstituicao" BOOLEAN        NOT NULL DEFAULT FALSE,
  "DataRecebimento"     DATE           NOT NULL,
  "Status"              int            NOT NULL DEFAULT 0,
  "Observacao"          TEXT,
  "UsuarioRegistro"     varchar(100),
  "DataRegistro"        TIMESTAMPTZ      NOT NULL DEFAULT NOW(),
  CONSTRAINT "iCatalogoRecebimentos1" PRIMARY KEY ("Id"),
  CONSTRAINT "iCatalogoRecebimentos_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
  CONSTRAINT "iCatalogoRecebimentos_Paciente" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id")
);

CREATE INDEX IF NOT EXISTS "iCatalogoRecebimentos_InstituicaoId" ON "CatalogoRecebimentos" ("InstituicaoId");

CREATE INDEX IF NOT EXISTS "iCatalogoRecebimentos_PacienteId" ON "CatalogoRecebimentos" ("PacienteId");

CREATE INDEX IF NOT EXISTS "iCatalogoRecebimentos_PeriodoFaturamento" ON "CatalogoRecebimentos" ("PeriodoFaturamento");

CREATE INDEX IF NOT EXISTS "iCatalogoRecebimentos_DataRecebimento" ON "CatalogoRecebimentos" ("DataRecebimento");

CREATE TABLE IF NOT EXISTS "CatalogoRecebimentosExames"
(
  "Id"                    SERIAL        NOT NULL,
  "CatalogoRecebimentoId" int           NOT NULL,
  "ExameRealizadoId"      int           NOT NULL,
  "Valor"                 DECIMAL(18,2) NOT NULL DEFAULT 0,
  CONSTRAINT "iCatalogoRecebimentosExames1" PRIMARY KEY ("Id"),
  CONSTRAINT "iCatalogoRecebimentosExames_Catalogo" FOREIGN KEY ("CatalogoRecebimentoId") REFERENCES "CatalogoRecebimentos"("Id") ON DELETE CASCADE,
  CONSTRAINT "iCatalogoRecebimentosExames_Exame" FOREIGN KEY ("ExameRealizadoId") REFERENCES "ExamesRealizados"("Id")
);

CREATE INDEX IF NOT EXISTS "iCatalogoRecebimentosExames_Catalogo" ON "CatalogoRecebimentosExames" ("CatalogoRecebimentoId");

CREATE INDEX IF NOT EXISTS "iCatalogoRecebimentosExames_Exame" ON "CatalogoRecebimentosExames" ("ExameRealizadoId");

--Feito pelo Qoder em 16/08/2026 — migração 011: um exame só pode constar em um único catálogo
CREATE UNIQUE INDEX IF NOT EXISTS "idx_u_CatalogoRecebimentosExames_Exame" ON "CatalogoRecebimentosExames" ("ExameRealizadoId");
--..Qoder

CREATE TABLE IF NOT EXISTS "CatalogoRecebimentosFormas"
(
  "Id"                    SERIAL        NOT NULL,
  "CatalogoRecebimentoId" int           NOT NULL,
  "FormaRecebimentoId"    int           NOT NULL,
  "ContaRecebimentoId"    int           NOT NULL,
  "Valor"                 DECIMAL(18,2) NOT NULL DEFAULT 0,
  "DataRecebimento"       DATE          NOT NULL,
  "Observacao"            TEXT,
  CONSTRAINT "iCatalogoRecebimentosFormas1" PRIMARY KEY ("Id"),
  CONSTRAINT "iCatalogoRecebimentosFormas_Catalogo" FOREIGN KEY ("CatalogoRecebimentoId") REFERENCES "CatalogoRecebimentos"("Id") ON DELETE CASCADE,
  CONSTRAINT "iCatalogoRecebimentosFormas_Forma" FOREIGN KEY ("FormaRecebimentoId") REFERENCES "FormasRecebimento"("Id"),
  CONSTRAINT "iCatalogoRecebimentosFormas_Conta" FOREIGN KEY ("ContaRecebimentoId") REFERENCES "ContasRecebimento"("Id")
);

CREATE INDEX IF NOT EXISTS "iCatalogoRecebimentosFormas_Catalogo" ON "CatalogoRecebimentosFormas" ("CatalogoRecebimentoId");

CREATE INDEX IF NOT EXISTS "iCatalogoRecebimentosFormas_Forma" ON "CatalogoRecebimentosFormas" ("FormaRecebimentoId");

CREATE INDEX IF NOT EXISTS "iCatalogoRecebimentosFormas_Conta" ON "CatalogoRecebimentosFormas" ("ContaRecebimentoId");

-- Seed de Conta de Recebimento padrão (Caixa da Portaria)
INSERT INTO "ContasRecebimento" ("Id", "Nome", "Tipo", "Identificacao", "PadraoPortaria", "Ativo")
VALUES (1, 'Caixa', 1, 'Recebimentos de Portaria', TRUE, TRUE)
ON CONFLICT ("Id") DO NOTHING;

--Feito pelo Qoder em 16/08/2026 — migração 005: sincroniza a sequence após o seed com Id explícito
SELECT setval(pg_get_serial_sequence('"ContasRecebimento"', 'Id'), COALESCE((SELECT MAX("Id") FROM "ContasRecebimento"), 1), true);
--..Qoder

-- Seed de Formas de Recebimento padrão
INSERT INTO "FormasRecebimento" ("Nome", "PermiteParticular", "PermiteInstituicao", "Ativo")
VALUES
    ('Dinheiro', TRUE, TRUE, TRUE),
    ('Cheque / Promissória', TRUE, TRUE, TRUE),
    ('Cartão de Crédito', TRUE, TRUE, TRUE),
    ('Cartão de Débito', TRUE, TRUE, TRUE),
    ('Boleto', FALSE, TRUE, TRUE),
    ('Transferência Bancária / PIX', TRUE, TRUE, TRUE),
    ('Convênio / Instituição (pagamento direto)', FALSE, TRUE, TRUE)
ON CONFLICT ("Nome") DO NOTHING;

--INSERT INTO ReCaptchaMonitoramento (NomeProjeto, QuantidadeSolicitacoes, MesReferencia, AnoReferencia) VALUES ('labwebmvc', 171, 7, 2025);
-- select * from ReCaptchaMonitoramento;

-- select * from Empresa;

-- Para consultar em outro banco, conectar via: \c LABWEB7Barros
/*
update "Senhas"
set "LoginUsuario" = 'ricardoguilemond@outlook.com',
    "Email" = 'ricardoguilemond@outlook.com',
    "NomeCompleto" = 'Ricardo Silva',
    "CNPJEmpresa" = '02557289000170'
where "Id" = 1;

update "UsuariosWeb"
set "CNPJEmpresa" = '02557289000170', "SenhaId" = 1;
*/

-- select * from Senhas;
-- select * from UsuariosWeb;

-- select * 
-- from Senhas senhas
-- join UsuariosWeb usuweb on senhas.Id = usuweb.SenhaId;
