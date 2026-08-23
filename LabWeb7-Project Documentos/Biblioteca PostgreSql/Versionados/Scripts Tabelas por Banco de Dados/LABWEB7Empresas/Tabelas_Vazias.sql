-- Atualizado pelo Kiro em 03/05/2026: TIMESTAMP u{2192} TIMESTAMPTZ para compatibilidade UTC
/*
    Criação das tabelas PostgreSQL para a base LAB_WEB7
    Data: 28/08/2022
    Atualizado em: 16/09/2022

	A LÓGICA PARA IMPLANTAR CLIENTES NOVOS:

    1) Cria um registro do cliente na tabela "LABWEB7Empresas"."EmpresaCliente"  (SOMENTE ESTA DEVERÁ ESTAR PREENCHIDA)
       Dados: CNPJ do cliente, Email principal do cliente, String de Conexão, Limite de Usuários.
      
    2) Cria uma tabela do cliente em "LABWEB7Empresas"."EmpresaLogin<CNPJ>" (EmpresaLogin + nome do cliente) 
    3) Cria o Banco de dados do cliente chamada de "LABWEB7<Cliente>" (Cliente = LABWEB7 + nome curto do cliente)

*/

----Tem que "dropar" nesta ordem, por causa dos relacionamentos
--DROP TABLE IF EXISTS "UF";
--DROP TABLE IF EXISTS "TituloExames";
--DROP TABLE IF EXISTS "TipoSanguineo";
--DROP TABLE IF EXISTS "Sexo";
--DROP TABLE IF EXISTS "Cor";
--DROP TABLE IF EXISTS "TextosProntos";
--DROP TABLE IF EXISTS "SituacaoExames";
--DROP TABLE IF EXISTS "Rastreamentos";
--DROP TABLE IF EXISTS "PlanoExames";
--DROP TABLE IF EXISTS "MenuSistemaInterfaces";
--DROP TABLE IF EXISTS "MenuSistema";
--DROP TABLE IF EXISTS "MemoAuxiliar";
--DROP TABLE IF EXISTS "Logradouro";
--DROP TABLE IF EXISTS "FichasPlanilhas";
--DROP TABLE IF EXISTS "FichasLotes";
--DROP TABLE IF EXISTS "FichasInternas";
--DROP TABLE IF EXISTS "ExamesExportados";
--DROP TABLE IF EXISTS "ItensExamesRealizados";
--DROP TABLE IF EXISTS "ExamesRealizados";
--DROP TABLE IF EXISTS "ItensExamesRealizadosAM";
--DROP TABLE IF EXISTS "ExamesRealizadosAM";
--DROP TABLE IF EXISTS "ExamesPendentes";
--DROP TABLE IF EXISTS "ExamesImpressos";
--DROP TABLE IF EXISTS "EstadoCivil";
--DROP TABLE IF EXISTS "Postos";
--DROP TABLE IF EXISTS "Instituicao";
--DROP TABLE IF EXISTS "TabelaExames";
--DROP TABLE IF EXISTS "ERTemporario";
--DROP TABLE IF EXISTS "Controle";
--DROP TABLE IF EXISTS "Medicos";
--DROP TABLE IF EXISTS "Pacientes";
--DROP TABLE IF EXISTS "ClasseExames";
--DROP TABLE IF EXISTS "Assinaturas";
--DROP TABLE IF EXISTS "IntegracaoDadosArmazenamento";
--DROP TABLE IF EXISTS "IntegracaoDadosConfiguracao";
--DROP TABLE IF EXISTS "IntegracaoDadosLayout";
--DROP TABLE IF EXISTS "IntegracaoDadosExecucao";
--DROP TABLE IF EXISTS "IntegracaoDadosExecucaoArquivo";
--DROP TABLE IF EXISTS "IntegracaoDadosPeriodicidade";
--DROP TABLE IF EXISTS "LogArquivos";
--DROP TABLE IF EXISTS "ControleConcorrencia";
--DROP TABLE IF EXISTS "UsuariosWeb";
--DROP TABLE IF EXISTS "Senhas";
--DROP TABLE IF EXISTS "Configuracoes";
--DROP TABLE IF EXISTS "Empresa";
--DROP TABLE IF EXISTS "ReCaptchaMonitoramento";

-- ===============================
-- ClasseExames
-- ===============================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
          AND table_name = 'ClasseExames'
    ) THEN

        CREATE TABLE "ClasseExames" (
            "Id" SERIAL,
            "RefExame" VARCHAR(50),
            "Etiquetas" INT NOT NULL DEFAULT 0,
            "TipoMapa" VARCHAR(1),
            "Assinatura1" INT NOT NULL DEFAULT 0,
            "Assinatura2" INT NOT NULL DEFAULT 0,
            "Assinatura3" INT NOT NULL DEFAULT 0,
            "Assinatura4" INT NOT NULL DEFAULT 0,
            "ImgAss1" BYTEA, 
            "ImgAss2" BYTEA,
            "ImgAss3" BYTEA,
            "ImgAss4" BYTEA,
            "NomeAss1" VARCHAR(100),
            "NomeAss2" VARCHAR(100),
            "NomeAss3" VARCHAR(100),
            "NomeAss4" VARCHAR(100),
            "Marcado" INT NOT NULL DEFAULT 0,
            "Planilha" INT NOT NULL DEFAULT 0,
            "MHI" INT NOT NULL DEFAULT 0, -- índice de ordenação
            "LaboratorioExterno" VARCHAR(20),
            CONSTRAINT "iClasseExames1" PRIMARY KEY ("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
          AND table_name = 'Assinaturas'
    ) THEN

        CREATE TABLE "Assinaturas" (
            "Id" SERIAL,
            "Assinatura1" BYTEA,
            "Usar1" INT NOT NULL DEFAULT 0,
            "CRBio1" VARCHAR(12) NOT NULL DEFAULT '123456789',
            "Assinatura2" BYTEA,
            "Usar2" INT NOT NULL DEFAULT 0,
            "CRBio2" VARCHAR(12),
            "Assinatura3" BYTEA,
            "Usar3" INT NOT NULL DEFAULT 0,
            "CRBio3" VARCHAR(12),
            "Assinatura4" BYTEA,
            "Usar4" INT NOT NULL DEFAULT 0,
            "CRBio4" VARCHAR(12),
            CONSTRAINT "iAssinaturas1" PRIMARY KEY ("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
          AND table_name = 'Pacientes'
    ) THEN

        CREATE TABLE "Pacientes" (
            "Id" SERIAL,
            "IdPacienteExterno" VARCHAR(20),
            "NomePaciente" VARCHAR(100) NOT NULL,
            "NomeSocial" VARCHAR(100),
            "NomePai" VARCHAR(100),
            "NomeMae" VARCHAR(100),
            "Nascimento" DATE NOT NULL, -- Feito pelo Qoder em 22/08/2026 — datas de negócio em DATE (migração TIMESTAMPTZ→DATE)
            "CPF" VARCHAR(11),
            "TipoDocumento" INT NOT NULL,
            "Identidade" VARCHAR(20),
            "Emissor" INT NOT NULL DEFAULT 0,
            "CarteiraSUS" VARCHAR(15),
            "EstadoCivil" INT NOT NULL DEFAULT 0,
            "Sexo" VARCHAR(1),
            "Cor" VARCHAR(7),
            "EtniaIndigena" VARCHAR(60),
            "TipoSanguineo" VARCHAR(3),
            "DUM" DATE,
            "TempoGestacao" INT NOT NULL DEFAULT 0,
            "Profissao" VARCHAR(100),
            "Naturalidade" VARCHAR(30),
            "Nacionalidade" VARCHAR(30),
            "DataEntradaBrasil" DATE,
            "Logradouro" VARCHAR(8),
            "Endereco" VARCHAR(100),
            "Numero" VARCHAR(15),
            "Complemento" VARCHAR(25),
            "Bairro" VARCHAR(45),
            "Cidade" VARCHAR(45),
            "UF" VARCHAR(2),
            "CEP" VARCHAR(8),
            "Telefone" VARCHAR(15),
            "Email" VARCHAR(100),
            "Observacao" VARCHAR(2000),
            "DataEntrada" DATE NOT NULL,
            "DataBaixa" DATE,
            "StatusBaixa" INT NOT NULL DEFAULT 0,
            "DataRegistro" TIMESTAMPTZ NOT NULL,
            CONSTRAINT "iPacientes1" PRIMARY KEY ("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
          AND table_name = 'ERTemporario'
    ) THEN

        CREATE TABLE "ERTemporario" (
            "Id" SERIAL,
            "ExameId" INT NOT NULL DEFAULT 0,
            "PacienteId" INT NOT NULL DEFAULT 0,
            "InstituicaoId" INT NOT NULL DEFAULT 0,
            "TabelaExamesId" INT NOT NULL DEFAULT 0,
            "MedicoId" INT NOT NULL DEFAULT 0,
            "Sequencial" INT NOT NULL DEFAULT 0,
            "ClasseExamesId" INT NOT NULL DEFAULT 0,
            "HistoricoClinico" VARCHAR(2000),
            "DataIni" DATE, -- Feito pelo Qoder em 22/08/2026 — datas de negócio em DATE
            "DataFim" DATE,
            "Liberacao" INT NOT NULL DEFAULT 0,
            "DataExame" DATE,
            "DataEntrega" DATE,
            "Baixado" INT NOT NULL DEFAULT 0,
            CONSTRAINT "iERTemporario1" PRIMARY KEY ("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
          AND table_name = 'EstadoCivil'
    ) THEN

        CREATE TABLE "EstadoCivil" (
            "Id" SERIAL,
            "Descricao" VARCHAR(10) NOT NULL,
            CONSTRAINT "iEstadoCivil1" PRIMARY KEY ("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
          AND table_name = 'TabelaExames'
    ) THEN

        CREATE TABLE "TabelaExames" (
            "Id" SERIAL,
            "SiglaTabela" VARCHAR(20) NOT NULL,
            "NomeTabela" VARCHAR(50) NOT NULL,
            "Orcamento" INT NOT NULL DEFAULT 0,
            "Bloqueado" INT NOT NULL DEFAULT 0,
            CONSTRAINT "iTabelaExames1" PRIMARY KEY ("Id"),
            CONSTRAINT "iTabelaExames2" UNIQUE("SiglaTabela")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
          AND table_name = 'Instituicao'
    ) THEN

        CREATE TABLE "Instituicao" (
            "Id" SERIAL,
            "Sigla" VARCHAR(20) NOT NULL,
            "Nome" VARCHAR(100) NOT NULL,
            "CNPJ" VARCHAR(14) NOT NULL,
            "Sequencial" INT NOT NULL DEFAULT 0,
            "Email" VARCHAR(100) NOT NULL,
            "TituloTimbre" VARCHAR(60),
            "SubTituloTimbre" VARCHAR(80),
            "Timbre" BYTEA,
            "Logomarca" BYTEA,
            "NomeTimbre" VARCHAR(250),
            "NomeLogomarca" VARCHAR(250),
            "CarimboSN" INT NOT NULL DEFAULT 0,
            "TimbreSN" INT NOT NULL DEFAULT 0,
            "Logradouro" VARCHAR(8),
            "Endereco" VARCHAR(100),
            "Numero" VARCHAR(15),
            "Complemento" VARCHAR(25),
            "Bairro" VARCHAR(45),
            "Cidade" VARCHAR(45),
            "UF" VARCHAR(2),
            "CEP" VARCHAR(8),
            "Contato" VARCHAR(60) NOT NULL,
            "Telefone" VARCHAR(15) NOT NULL,
            "Celular" VARCHAR(15),
            "UsuarioCaminhoFTP" VARCHAR(250),
            "UsuarioEmailFTP" VARCHAR(150),
            "UsuarioPortaFTP" INT,
            "UsuarioSenhaFTP" VARCHAR(60),
            "ValorExameCitologia" NUMERIC(18,4),
            "Propaganda" INT DEFAULT 0,
            "AvisoRodape1" VARCHAR(140),
            "AvisoRodape2" VARCHAR(140),
            CONSTRAINT "iInstituicao1" PRIMARY KEY ("Id"),
            CONSTRAINT "iInstituicao2" UNIQUE("Sigla")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
          AND table_name = 'Postos'
    ) THEN

        CREATE TABLE "Postos" (
            "Id" SERIAL,
            "InstituicaoId" INT NOT NULL,
            "SiglaPosto" VARCHAR(20) NOT NULL,
            "NomePosto" VARCHAR(60) NOT NULL,
            "Responsavel" VARCHAR(60) NOT NULL,
            "Logradouro" VARCHAR(8),
            "Endereco" VARCHAR(100),
            "Numero" VARCHAR(15),
            "Complemento" VARCHAR(25),
            "Bairro" VARCHAR(45),
            "Cidade" VARCHAR(45),
            "UF" VARCHAR(2),
            "CEP" VARCHAR(8),
            "Telefone" VARCHAR(60),
            CONSTRAINT "iPostos1" PRIMARY KEY ("Id"),
            CONSTRAINT "iPostos_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id") ON DELETE RESTRICT
        );

        CREATE INDEX IF NOT EXISTS "iPostos_InstituicaoId" ON "Postos" ("InstituicaoId");

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
          AND table_name = 'Medicos'
    ) THEN

        CREATE TABLE "Medicos" (
            "Id" SERIAL,
            "NomeMedico" VARCHAR(100) NOT NULL,
            "Especialidade" VARCHAR(100),
            "CRM" VARCHAR(15) NOT NULL,
            "Telefone" VARCHAR(15),
            "Email" VARCHAR(100),
            CONSTRAINT "iMedicos1" PRIMARY KEY ("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
          AND table_name = 'ExamesRealizados'
    ) THEN

        CREATE TABLE "ExamesRealizados" (
            "Id" SERIAL,
            "PacienteId" INT NOT NULL DEFAULT 0,
            "TabelaExamesId" INT NOT NULL DEFAULT 0,
            "InstituicaoId" INT NOT NULL DEFAULT 0,
            "PostoId" INT,
            "MedicoId" INT NOT NULL DEFAULT 0,
            "Sequencial" INT NOT NULL DEFAULT 0,
            "LaboratorioApoio" VARCHAR(20),
            "ControleApoio" VARCHAR(20) NOT NULL,
            "HistoricoClinico" VARCHAR(2000),
            "ExameColado" VARCHAR(250),
            "ExameColadoImagens" VARCHAR(250),
            "TravaColado" INT NOT NULL DEFAULT 0,
            "DataIni" DATE NOT NULL, -- Feito pelo Qoder em 22/08/2026 — datas de negócio em DATE
            "DataFim" DATE,
            "Liberacao" INT NOT NULL DEFAULT 0,
            "DataExame" DATE,
            "DataColeta" VARCHAR(10),
            "DataEntrega" DATE,
            "Baixado" INT NOT NULL DEFAULT 0,
            "EnviarEmail" INT NOT NULL DEFAULT 0,
            "Situacao" INT NOT NULL DEFAULT 0,
            "TotalImpresso" INT NOT NULL DEFAULT 0,
            "Faturado" BOOLEAN DEFAULT FALSE,
            CONSTRAINT "iExamesRealizados1" PRIMARY KEY ("Id"),
            CONSTRAINT "iExamesRealizados_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
            CONSTRAINT "iExamesRealizados_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id"),
            CONSTRAINT "iExamesRealizados_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
            CONSTRAINT "iExamesRealizados_Medicos" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
          AND table_name = 'ExamesRealizadosAM'
    ) THEN

        CREATE TABLE "ExamesRealizadosAM" (
            "Id" SERIAL,
            "OrigemId" INT NOT NULL,
            "PacienteId" INT NOT NULL DEFAULT 0,
            "TabelaExamesId" INT NOT NULL DEFAULT 0,
            "InstituicaoId" INT NOT NULL DEFAULT 0,
            "PostoId" INT,
            "MedicoId" INT NOT NULL DEFAULT 0,
            "Sequencial" INT NOT NULL DEFAULT 0,
            "LaboratorioApoio" VARCHAR(20),
            "ControleApoio" VARCHAR(20) NOT NULL,
            "HistoricoClinico" VARCHAR(2000),
            "ExameColado" VARCHAR(250),
            "ExameColadoImagens" VARCHAR(250),
            "TravaColado" INT NOT NULL DEFAULT 0,
            "DataIni" DATE NOT NULL, -- Feito pelo Qoder em 22/08/2026 — datas de negócio em DATE
            "DataFim" DATE,
            "Liberacao" INT NOT NULL DEFAULT 0,
            "DataExame" DATE,
            "DataColeta" VARCHAR(10),
            "DataEntrega" DATE,
            "Baixado" INT NOT NULL DEFAULT 0,
            "EnviarEmail" INT NOT NULL DEFAULT 0,
            "Situacao" INT NOT NULL DEFAULT 0,
            "TotalImpresso" INT NOT NULL DEFAULT 0,
            "Faturado" BOOLEAN DEFAULT FALSE,
            CONSTRAINT "iExamesRealizadosAM1" PRIMARY KEY ("Id"),
            CONSTRAINT "iExamesRealizadosAM_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
            CONSTRAINT "iExamesRealizadosAM_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id"),
            CONSTRAINT "iExamesRealizadosAM_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
            CONSTRAINT "iExamesRealizadosAM_Medicos" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
          AND table_name = 'ExamesExportados'
    ) THEN

        CREATE TABLE "ExamesExportados" (
            "Id" SERIAL,
            "ExameId" INT NOT NULL DEFAULT 0,
            "PacienteId" INT NOT NULL DEFAULT 0,
            "InstituicaoId" INT NOT NULL DEFAULT 0,
            "TabelaExamesId" INT NOT NULL DEFAULT 0,
            "MedicoId" INT NOT NULL DEFAULT 0,
            "Sequencial" INT NOT NULL DEFAULT 0,
            "LaboratorioApoio" VARCHAR(20),
            "ControleApoio" VARCHAR(20) NOT NULL,
            "DataColeta" DATE, -- Feito pelo Qoder em 22/08/2026 — data de negócio em DATE (DataExportado/DataImportado seguem TIMESTAMPTZ)
            "DataExportado" TIMESTAMPTZ,
            "DataImportado" TIMESTAMPTZ,
            CONSTRAINT "iExamesExportados1" PRIMARY KEY ("Id"),
            CONSTRAINT "iExamesExportados_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
            CONSTRAINT "iExamesExportados_ExamesRealizados" FOREIGN KEY ("ExameId") REFERENCES "ExamesRealizados"("Id"),
            CONSTRAINT "iExamesExportados_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
            CONSTRAINT "iExamesExportados_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id"),
            CONSTRAINT "iExamesExportados_Medicos" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables 
        WHERE table_schema = 'public'
          AND table_name = 'ExamesImpressos'
    ) THEN

        CREATE TABLE "ExamesImpressos" (
            "Id" SERIAL,
            "PacienteId" INT NOT NULL DEFAULT 0,
            "InstituicaoId" INT NOT NULL DEFAULT 0,
            "TabelaExamesId" INT NOT NULL DEFAULT 0,
            "Sequencial" INT NOT NULL DEFAULT 0,
            "DataExame" DATE, -- Feito pelo Qoder em 22/08/2026 — data de negócio em DATE (DataImpresso segue TIMESTAMPTZ)
            "DataImpresso" TIMESTAMPTZ,
            "TotalImpresso" INT NOT NULL DEFAULT 0,
            CONSTRAINT "iExamesImpressos1" PRIMARY KEY ("Id"),
            CONSTRAINT "iExamesImpressos_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
            CONSTRAINT "iExamesImpressos_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
            CONSTRAINT "iExamesImpressos_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'ExamesPendentes'
    ) THEN

        CREATE TABLE "ExamesPendentes" (
            "Id" SERIAL,
            "PacienteId" INT NOT NULL DEFAULT 0,
            "InstituicaoId" INT NOT NULL DEFAULT 0,
            "TabelaExamesId" INT NOT NULL DEFAULT 0,
            "MedicoId" INT NOT NULL DEFAULT 0,
            "ClasseExamesId" INT NOT NULL DEFAULT 0,
            "Sequencial" INT NOT NULL DEFAULT 0,
            "LaboratorioApoio" VARCHAR(20),
            "ControleApoio" VARCHAR(15),
            "ContaExame" VARCHAR(11),
            "NomeFolha" VARCHAR(50),
            "NomeGrupo" VARCHAR(50),
            "NomeItem" VARCHAR(50),
            "DataIni" DATE, -- Feito pelo Qoder em 22/08/2026 — data de negócio em DATE
            CONSTRAINT "iExamesPendentes1" PRIMARY KEY ("Id"),
            CONSTRAINT "iExamesPendentes_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
            CONSTRAINT "iExamesPendentes_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
            CONSTRAINT "iExamesPendentes_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id"),
            CONSTRAINT "iExamesPendentes_Medicos" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id"),
            CONSTRAINT "iExamesPendentes_ClasseExames" FOREIGN KEY ("ClasseExamesId") REFERENCES "ClasseExames"("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'FichasInternas'
    ) THEN

        CREATE TABLE "FichasInternas" (
            "Id" SERIAL,
            "NomeFicha" VARCHAR(50),
            "ContaExame" VARCHAR(11),
            "Descricao" VARCHAR(50),
            "Resultado" VARCHAR(30),
            "MapaHorizontal" VARCHAR(6),
            "ExamesRealizadosId" INT NOT NULL DEFAULT 0,
            "PacienteId" INT NOT NULL DEFAULT 0,
            "MedicoId" INT NOT NULL DEFAULT 0,
            "InstituicaoId" INT NOT NULL DEFAULT 0,
            "DataExame" DATE NOT NULL, -- Feito pelo Qoder em 22/08/2026 — datas de negócio em DATE
            "ControleApoio" VARCHAR(20),
            "Sequencial" INT NOT NULL DEFAULT 0,
            "HistoricoClinico" VARCHAR(2000),
            "DataIni" DATE NOT NULL,
            "DataFim" DATE,
            "Pagina" INT NOT NULL DEFAULT 0,
            "Coluna1" VARCHAR(6),
            "Coluna2" VARCHAR(6),
            "Coluna3" VARCHAR(6),
            "Coluna4" VARCHAR(6),
            "Coluna5" VARCHAR(6),
            "Coluna6" VARCHAR(6),
            "Coluna7" VARCHAR(6),
            "Coluna8" VARCHAR(6),
            "Coluna9" VARCHAR(6),
            "Coluna10" VARCHAR(6),
            "Coluna11" VARCHAR(6),
            "Coluna12" VARCHAR(6),
            "Coluna13" VARCHAR(6),
            "Coluna14" VARCHAR(6),
            "Coluna15" VARCHAR(6),
            "Coluna16" VARCHAR(6),
            "Coluna17" VARCHAR(6),
            "Coluna18" VARCHAR(6),
            CONSTRAINT "iFichasInternas1" PRIMARY KEY ("Id"),
            CONSTRAINT "iFichasInternas_ExamesRealizados" FOREIGN KEY ("ExamesRealizadosId") REFERENCES "ExamesRealizados"("Id"),
            CONSTRAINT "iFichasInternas_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
            CONSTRAINT "iFichasInternas_Medicos" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id"),
            CONSTRAINT "iFichasInternas_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'FichasLotes'
    ) THEN

        CREATE TABLE "FichasLotes" (
            "Id" SERIAL,
            "NomeFicha" VARCHAR(50),
            "ContaExame" VARCHAR(11),
            "Descricao" VARCHAR(50),
            "Resultado" VARCHAR(30),
            "MapaHorizontal" VARCHAR(6),
            "ExamesRealizadosId" INT NOT NULL DEFAULT 0,
            "PacienteId" INT NOT NULL DEFAULT 0,
            "MedicoId" INT NOT NULL DEFAULT 0,
            "InstituicaoId" INT NOT NULL DEFAULT 0,
            "TabelaExamesId" INT NOT NULL DEFAULT 0,
            "DataExame" DATE, -- Feito pelo Qoder em 22/08/2026 — datas de negócio em DATE
            "ControleApoio" VARCHAR(20),
            "Sequencial" INT NOT NULL DEFAULT 0,
            "HistoricoClinico" VARCHAR(2000),
            "DataIni" DATE,
            "DataFim" DATE,
            "Lote" INT NOT NULL DEFAULT 0,
            "LiberadoExclusao" VARCHAR(1),
            CONSTRAINT "iFichasLotes1" PRIMARY KEY ("Id"),
            CONSTRAINT "iFichasLotes_ExamesRealizados" FOREIGN KEY ("ExamesRealizadosId") REFERENCES "ExamesRealizados"("Id"),
            CONSTRAINT "iFichasLotes_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
            CONSTRAINT "iFichasLotes_Medicos" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id"),
            CONSTRAINT "iFichasLotes_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
            CONSTRAINT "iFichasLotes_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'FichasPlanilhas'
    ) THEN

        CREATE TABLE "FichasPlanilhas" (
            "Id" SERIAL,
            "NomeFicha" VARCHAR(50),
            "ContaExame" VARCHAR(11),
            "Descricao" VARCHAR(50),
            "Resultado" VARCHAR(30),
            "MapaHorizontal" VARCHAR(6),
            "ExamesRealizadosId" INT NOT NULL DEFAULT 0,
            "PacienteId" INT NOT NULL DEFAULT 0,
            "MedicoId" INT NOT NULL DEFAULT 0,
            "InstituicaoId" INT NOT NULL DEFAULT 0,
            "TabelaExamesId" INT NOT NULL DEFAULT 0,
            "DataExame" DATE, -- Feito pelo Qoder em 22/08/2026 — datas de negócio em DATE
            "ControleApoio" VARCHAR(20),
            "Sequencial" INT NOT NULL DEFAULT 0,
            "HistoricoClinico" VARCHAR(2000),
            "DataIni" DATE NOT NULL,
            "DataFim" DATE,
            "Lote" INT NOT NULL DEFAULT 0,
            "LiberadoExclusao" VARCHAR(1),
            CONSTRAINT "iFichasPlanilhas1" PRIMARY KEY ("Id"),
            CONSTRAINT "iFichasPlanilhas_ExamesRealizados" FOREIGN KEY ("ExamesRealizadosId") REFERENCES "ExamesRealizados"("Id"),
            CONSTRAINT "iFichasPlanilhas_Pacientes" FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
            CONSTRAINT "iFichasPlanilhas_Medicos" FOREIGN KEY ("MedicoId") REFERENCES "Medicos"("Id"),
            CONSTRAINT "iFichasPlanilhas_Instituicao" FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
            CONSTRAINT "iFichasPlanilhas_TabelaExames" FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'ItensExamesRealizados'
    ) THEN

        CREATE TABLE "ItensExamesRealizados" (
            "Id" SERIAL,
            "PacienteId" INT NOT NULL DEFAULT 0,
            "ClasseExamesId" INT NOT NULL DEFAULT 0,
            "ClasseExamesNome" VARCHAR(50) NOT NULL,
            "ExameRealizadoId" INT NOT NULL DEFAULT 0,
            "TabelaExamesId" INT NOT NULL DEFAULT 0,
            "OrdemItem" INT NOT NULL DEFAULT 0,
            "RefExame" VARCHAR(50) NOT NULL,
            "RefItem" VARCHAR(50) NOT NULL,
            "ContaExame" VARCHAR(11) NOT NULL,
            "CitoTituloFolha" INT NOT NULL DEFAULT 0,
            "CitoTituloExame" INT NOT NULL DEFAULT 0,
            "CitoRefItem" INT NOT NULL DEFAULT 0,
            "InstituicaoId" INT NOT NULL DEFAULT 0,
            "Sequencial" INT NOT NULL DEFAULT 0,
            "LaboratorioApoio" VARCHAR(20),
            "ControleApoio" VARCHAR(20),
            "LaboratorioExterno" VARCHAR(20),
            "MaterialSaida" VARCHAR(16),
            "MaterialRetorno" VARCHAR(16),
            "Descricao" VARCHAR(50),
            "CitoDescricao" VARCHAR(2000),
            "Resultado" VARCHAR(30),
            "UnidadeMedida" VARCHAR(20),
            "Referencia" VARCHAR(60),
            "ValorItem" NUMERIC(18,4),
            "Laudo" BYTEA,
            "Etiquetas" INT NOT NULL DEFAULT 0,
            "DataEntregaParcial" DATE, -- Feito pelo Qoder em 22/08/2026 — data de negócio em DATE
            "Liberado" INT NOT NULL DEFAULT 0,
            "Baixado" INT NOT NULL DEFAULT 0,
            CONSTRAINT "iItensExamesRealizados1" PRIMARY KEY ("Id"),
            CONSTRAINT "iItensExamesRealizados_ExamesRealizados"
                FOREIGN KEY ("ExameRealizadoId") REFERENCES "ExamesRealizados"("Id")
                ON DELETE CASCADE ON UPDATE CASCADE,
            CONSTRAINT "iItensExamesRealizados_Pacientes"
                FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
            CONSTRAINT "iItensExamesRealizados_ClasseExames"
                FOREIGN KEY ("ClasseExamesId") REFERENCES "ClasseExames"("Id"),
            CONSTRAINT "iItensExamesRealizados_TabelaExames"
                FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id"),
            CONSTRAINT "iItensExamesRealizados_Instituicao"
                FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'ItensExamesRealizadosAM'
    ) THEN

        CREATE TABLE "ItensExamesRealizadosAM" (
            "Id" SERIAL,
            "OrigemAMId" INT NOT NULL,
            "PacienteId" INT NOT NULL DEFAULT 0,
            "ClasseExamesId" INT NOT NULL DEFAULT 0,
            "ClasseExamesNome" VARCHAR(50) NOT NULL,
            "ExameRealizadoAMId" INT NOT NULL DEFAULT 0,
            "TabelaExamesId" INT NOT NULL DEFAULT 0,
            "OrdemItem" INT NOT NULL DEFAULT 0,
            "RefExame" VARCHAR(50) NOT NULL,
            "RefItem" VARCHAR(50) NOT NULL,
            "ContaExame" VARCHAR(11) NOT NULL,
            "CitoTituloFolha" INT NOT NULL DEFAULT 0,
            "CitoTituloExame" INT NOT NULL DEFAULT 0,
            "CitoRefItem" INT NOT NULL DEFAULT 0,
            "InstituicaoId" INT NOT NULL DEFAULT 0,
            "Sequencial" INT NOT NULL DEFAULT 0,
            "LaboratorioApoio" VARCHAR(20),
            "ControleApoio" VARCHAR(20),
            "LaboratorioExterno" VARCHAR(20),
            "MaterialSaida" VARCHAR(16),
            "MaterialRetorno" VARCHAR(16),
            "Descricao" VARCHAR(50),
            "CitoDescricao" VARCHAR(2000),
            "Resultado" VARCHAR(30),
            "UnidadeMedida" VARCHAR(20),
            "Referencia" VARCHAR(60),
            "ValorItem" NUMERIC(18,4),
            "Laudo" BYTEA,
            "Etiquetas" INT NOT NULL DEFAULT 0,
            "DataEntregaParcial" DATE, -- Feito pelo Qoder em 22/08/2026 — data de negócio em DATE
            "Liberado" INT NOT NULL DEFAULT 0,
            "Baixado" INT NOT NULL DEFAULT 0,
            CONSTRAINT "iItensExamesRealizadosAM1" PRIMARY KEY ("Id"),
            CONSTRAINT "iItensExamesRealizadosAM1_ExamesRealizados"
                FOREIGN KEY ("ExameRealizadoAMId") REFERENCES "ExamesRealizadosAM"("Id")
                ON DELETE CASCADE ON UPDATE CASCADE,
            CONSTRAINT "iItensExamesRealizadosAM1_Pacientes"
                FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id"),
            CONSTRAINT "iItensExamesRealizadosAM1_ClasseExames"
                FOREIGN KEY ("ClasseExamesId") REFERENCES "ClasseExames"("Id"),
            CONSTRAINT "iItensExamesRealizadosAM1_TabelaExames"
                FOREIGN KEY ("TabelaExamesId") REFERENCES "TabelaExames"("Id"),
            CONSTRAINT "iItensExamesRealizadosAM1_Instituicao"
                FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'Logradouro'
    ) THEN

        CREATE TABLE "Logradouro" (
            "Id" SERIAL,
            "Descricao" VARCHAR(8) NOT NULL,
            CONSTRAINT "iLogradouro1" PRIMARY KEY ("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'MemoAuxiliar'
    ) THEN

        CREATE TABLE "MemoAuxiliar" (
            "Id" SERIAL,
            "NomeFolha" VARCHAR(50),
            "Linha1" VARCHAR(250),
            "Linha2" VARCHAR(250),
            "Linha3" VARCHAR(250),
            "Linha4" VARCHAR(250),
            "Linha5" VARCHAR(250),
            "Linha6" VARCHAR(250),
            "CampoMemo" BYTEA,
            CONSTRAINT "iMemoAuxiliar1" PRIMARY KEY ("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'PlanoExames'
    ) THEN

        CREATE TABLE "PlanoExames" (
            "Id" SERIAL,
            "ClasseExamesId" INT NOT NULL DEFAULT 0,
            "CitoInstituicao" INT NOT NULL DEFAULT 0,
            "CitoTituloFolha" VARCHAR(60),
            "CitoTituloExame" INT NOT NULL DEFAULT 0,
            "CitoParteDescricao" VARCHAR(100),
            "CitoDescricao" BYTEA,
            "RefExame" VARCHAR(50) NOT NULL,
            "RefItem" VARCHAR(50) NOT NULL,
            "TabelaExamesId" INT NOT NULL DEFAULT 0,
            "ContaExame" VARCHAR(11) NOT NULL,
            "Descricao" VARCHAR(50) NOT NULL,
            "ValorCusto" NUMERIC(18,4),
            "ValorItem" NUMERIC(18,4),
            "TABELACH" VARCHAR(10),
            "QCH" INT NOT NULL DEFAULT 0,
            "ICH" NUMERIC(18,2),
            "UnidadeMedida" VARCHAR(20),
            "Referencia" VARCHAR(60),
            "Etiqueta" INT NOT NULL DEFAULT 0,
            "Etiquetas" INT NOT NULL DEFAULT 0,
            "GraficoNoItem" INT NULL,
            "Laudo" BYTEA,
            "AlinhaLaudo" INT NOT NULL DEFAULT 0,
            "Seleciona" INT NOT NULL DEFAULT 0,
            "NaoMostrar" INT NOT NULL DEFAULT 0,
            "MapaHorizontal" VARCHAR(6),
            "ResultadoMinimo" NUMERIC(18,4),
            "ResultadoMaximo" NUMERIC(18,4),
            "LaboratorioExterno" VARCHAR(20),
            "PrazoResultadoDias" INT,
            CONSTRAINT "iPlanoExames1" PRIMARY KEY ("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'Rastreamentos'
    ) THEN

        CREATE TABLE "Rastreamentos" (
            "Id" SERIAL,
            "UsuarioId" INT NOT NULL DEFAULT 0,
            "DataOcorrencia" TIMESTAMPTZ NOT NULL,
            "SistemaUtilizado" VARCHAR(30),
            "VersaoSistema" VARCHAR(26),
            "OpcaoMenu" VARCHAR(26),
            "OperacaoRealizada" VARCHAR(500),
            "OperacaoComplementar" VARCHAR(500),
            "Falha" VARCHAR(1000),
            "Exception" VARCHAR(4000),
            "IPLocal" VARCHAR(15),
            "IPExterno" VARCHAR(15),
            "NomeComputador" VARCHAR(100),
            CONSTRAINT "iRastreamentos1" PRIMARY KEY ("Id")
        );

    END IF;
END
$$;

--Feito pelo Qoder em 23/08/2026 — tabela "Requisitar" removida definitivamente: a entidade foi
--eliminada do modelo e a tela "Compactar Requisições" foi excluída do sistema.

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'SituacaoExames'
    ) THEN

        CREATE TABLE "SituacaoExames" (
            "Id" SERIAL,
            "Descricao" VARCHAR(40),
            CONSTRAINT "iSituacaoExames1" PRIMARY KEY ("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'TextosProntos'
    ) THEN

        CREATE TABLE "TextosProntos" (
            "Id" SERIAL,
            "Texto" VARCHAR(100),
            CONSTRAINT "iTextosProntos1" PRIMARY KEY ("Id"),
            CONSTRAINT "iTextosProntos2" UNIQUE ("Texto")  -- coluna Texto jamais conterá valor duplicado
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'UF'
    ) THEN

        CREATE TABLE "UF" (
            "Id" SERIAL,
            "Sigla" VARCHAR(2),
            "Descricao" VARCHAR(20),
            CONSTRAINT "iUF1" PRIMARY KEY ("Id"),
            CONSTRAINT "iUF2" UNIQUE ("Sigla")  -- restrição exclusiva de coluna
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'Cor'
    ) THEN

        CREATE TABLE "Cor" (
            "Id" SERIAL,
            "Cor" VARCHAR(8) NOT NULL,
            CONSTRAINT "iCor1" PRIMARY KEY ("Id"),
            CONSTRAINT "iCor2" UNIQUE ("Cor")  -- restrição exclusiva de coluna
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'Sexo'
    ) THEN

        CREATE TABLE "Sexo" (
            "Id" SERIAL,
            "Sigla" VARCHAR(1),
            "Descricao" VARCHAR(15),
            CONSTRAINT "iSexo1" PRIMARY KEY ("Id"),
            CONSTRAINT "iSexo2" UNIQUE ("Sigla")  -- restrição exclusiva de coluna
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'TipoSanguineo'
    ) THEN

        CREATE TABLE "TipoSanguineo" (
            "Id" SERIAL,
            "Tipo" VARCHAR(2) NOT NULL,
            "RH" VARCHAR(1) NOT NULL,
            "DoaPara" VARCHAR(40),
            "RecebeDe" VARCHAR(40),
            CONSTRAINT "iTipoSanguineo1" PRIMARY KEY ("Id")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'TituloExames'
    ) THEN

        CREATE TABLE "TituloExames" (
            "Id" SERIAL,
            "TituloExame" VARCHAR(60) NOT NULL,
            CONSTRAINT "iTituloExames1" PRIMARY KEY ("Id")
        );

    END IF;
END
$$;

/* 
   Tabelas das Integrações de Dados (Exportação/Importação)
   Ordem do CASCADE
*/
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_schema = 'public' AND table_name = 'IntegracaoDadosArmazenamento'
    ) THEN
        CREATE TABLE "IntegracaoDadosArmazenamento" (
            "Id" SERIAL,
            "Senha" VARCHAR(100),
            "TipoArmazenamento" INT NOT NULL,
            "Host" VARCHAR(500),
            "Usuario" INT,
            "UsuarioLogin" VARCHAR(100),
            CONSTRAINT "iIntegracaoDadosArmazenamento1" PRIMARY KEY ("Id")
        );
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_schema = 'public' AND table_name = 'IntegracaoDadosConfiguracao'
    ) THEN
        CREATE TABLE "IntegracaoDadosConfiguracao" (
            "Id" SERIAL,
            "IntegracaoDadosArmazenamentoId" INT NOT NULL,
            "PastaSaida" VARCHAR(500),
            "PastaEntrada" VARCHAR(500),
            "NomeArquivo" VARCHAR(200) NOT NULL,
            "HoraExecucao" VARCHAR(5) NOT NULL,
            "HoraEncerramento" VARCHAR(5),
            "DiaExecucao" INT,
            "Periodicidade" INT NOT NULL DEFAULT 1,
            "IntegraUmaUnicaVezNoDia" BOOLEAN NOT NULL DEFAULT TRUE,
            "PausaDoEventoEmMinutos" INT NOT NULL DEFAULT 1,
            "PastaEntradaProcessado" VARCHAR(500),
            "PastaEntradaProcessadoErro" VARCHAR(500),
            "PastaEntradaProcessadoParcial" VARCHAR(500),
            "UsuarioPadrao" INT,
            CONSTRAINT "iIntegracaoDadosConfiguracao1" PRIMARY KEY ("Id"),
            CONSTRAINT "iIntegracaoDadosConfiguracao2"
                FOREIGN KEY ("IntegracaoDadosArmazenamentoId")
                REFERENCES "IntegracaoDadosArmazenamento"("Id")
                ON DELETE CASCADE ON UPDATE CASCADE
        );
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_schema = 'public' AND table_name = 'IntegracaoDadosLayout'
    ) THEN
        CREATE TABLE "IntegracaoDadosLayout" (
            "Id" SERIAL,
            "IntegracaoDadosConfiguracaoId" INT NOT NULL,
            "Descricao" VARCHAR(60) NOT NULL,
            "TipoServico" INT NOT NULL,
            "Exportacao" BOOLEAN NOT NULL,
            "Habilitado" BOOLEAN NOT NULL,
            "DataInicial" TIMESTAMPTZ,
            "DataFinal" TIMESTAMPTZ,
            CONSTRAINT "iIntegracaoDadosLayout1" PRIMARY KEY ("Id"),
            CONSTRAINT "iIntegracaoDadosLayout2"
                FOREIGN KEY ("IntegracaoDadosConfiguracaoId")
                REFERENCES "IntegracaoDadosConfiguracao"("Id")
                ON DELETE CASCADE ON UPDATE CASCADE
        );
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_schema = 'public' AND table_name = 'IntegracaoDadosExecucao'
    ) THEN
        CREATE TABLE "IntegracaoDadosExecucao" (
            "Id" SERIAL,
            "IntegracaoDadosLayoutId" INT NOT NULL,
            "Inicio" TIMESTAMPTZ NOT NULL,
            "Termino" TIMESTAMPTZ,
            "Sucesso" BOOLEAN NOT NULL,
            "Resumo" VARCHAR(4000),
            "NomeServico" VARCHAR(500),
            "NomeArquivo" VARCHAR(200),
            "Header" VARCHAR(1000),
            "Summary" VARCHAR(1000),
            CONSTRAINT "iIntegracaoDadosExecucao1" PRIMARY KEY ("Id"),
            CONSTRAINT "iIntegracaoDadosExecucao2"
                FOREIGN KEY ("IntegracaoDadosLayoutId")
                REFERENCES "IntegracaoDadosLayout"("Id")
                ON DELETE CASCADE ON UPDATE CASCADE
        );
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_schema = 'public' AND table_name = 'IntegracaoDadosExecucaoArquivo'
    ) THEN
        CREATE TABLE "IntegracaoDadosExecucaoArquivo" (
            "Id" SERIAL,
            "IntegracaoDadosExecucaoId" INT NOT NULL,
            "NomeArquivo" VARCHAR(200),
            "Status" INT NOT NULL,
            "Resumo" VARCHAR(4000),
            "NomeArquivoProcessado" VARCHAR(200),
            "NomeArquivoGerado" VARCHAR(200),
            CONSTRAINT "iIntegracaoDadosExecucaoArquivo1" PRIMARY KEY ("Id"),
            CONSTRAINT "iIntegracaoDadosExecucaoArquivo2"
                FOREIGN KEY ("IntegracaoDadosExecucaoId")
                REFERENCES "IntegracaoDadosExecucao"("Id")
                ON DELETE CASCADE ON UPDATE CASCADE
        );
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_schema = 'public' AND table_name = 'IntegracaoDadosPeriodicidade'
    ) THEN
        CREATE TABLE "IntegracaoDadosPeriodicidade" (
            "Id" SERIAL,
            "TipoPeriodoExtracao" VARCHAR(12) NOT NULL,
            CONSTRAINT "iIntegracaoDadosPeriodicidade1" PRIMARY KEY ("Id")
        );
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_schema = 'public' AND table_name = 'LogArquivos'
    ) THEN
        CREATE TABLE "LogArquivos" (
            "Id" SERIAL,
            "IntegracaoDadosLayoutId" INT,
            "StrRef" VARCHAR(250) NOT NULL,
            "NomeArquivo" VARCHAR(200),
            "Data" TIMESTAMPTZ NOT NULL,
            "DataPeriodoInicial" TIMESTAMPTZ,
            "DataPeriodoFinal" TIMESTAMPTZ,
            "TipoServico" INT NOT NULL,
            CONSTRAINT "iLogArquivos1" PRIMARY KEY ("Id"),
            CONSTRAINT "iLogArquivos2"
                FOREIGN KEY ("IntegracaoDadosLayoutId")
                REFERENCES "IntegracaoDadosLayout"("Id")
                ON DELETE CASCADE ON UPDATE CASCADE
        );
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'ControleConcorrencia'
    ) THEN

        CREATE TABLE "ControleConcorrencia" (
            "Processo" VARCHAR(200) NOT NULL,
            "DataHora" TIMESTAMPTZ NOT NULL,
            CONSTRAINT "iControleConcorrencia1" PRIMARY KEY ("Processo")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'Configuracoes'
    ) THEN

        CREATE TABLE "Configuracoes" (
            "Id" INT NOT NULL,
            "ImpressoraCupom1" VARCHAR(500),
            "ImpressoraCupom2" VARCHAR(500),
            "ImpressoraCupom3" VARCHAR(500),
            "UsarImpressoraCupom1" INT NOT NULL DEFAULT 0,
            "UsarImpressoraCupom2" INT NOT NULL DEFAULT 0,
            "UsarImpressoraCupom3" INT NOT NULL DEFAULT 0,
            "FonteNome" VARCHAR(100) DEFAULT 'Consolas',
            "FonteTamanho" INT DEFAULT 8,
            "LarguraPapel" INT DEFAULT 283,     -- em centésimos de polegada
            "AlturaPapel" INT DEFAULT 32767,    -- em centésimos de polegada
            "MargemEsquerda" INT DEFAULT 5,
            "MargemDireita" INT DEFAULT 5,
            "MargemSuperior" INT DEFAULT 5,
            "MargemInferior" INT DEFAULT 5,
            CONSTRAINT "iConfiguracoes1" PRIMARY KEY ("Id"),
            CONSTRAINT "CHK_IdUnico" CHECK ("Id" = 1)
        );

    END IF;
END
$$;


DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'Senhas'
    ) THEN

        CREATE TABLE "Senhas" (
            "Id" SERIAL,
            "LoginUsuario" VARCHAR(60) NOT NULL,   -- Email ou nick válido
            "NomeUsuario" VARCHAR(15) NOT NULL,
            "NomeCompleto" VARCHAR(100),
            "SenhaUsuario" VARCHAR(256) NOT NULL,
            "DataCadastro" TIMESTAMPTZ NOT NULL,
            "DataExpira" TIMESTAMPTZ,
            "Assinatura" BYTEA,
            "UsarAssinatura" INT NOT NULL DEFAULT 0,
            "NomeAssinatura" VARCHAR(250),
            "Bloqueado" INT NOT NULL DEFAULT 0,
            "Administrador" INT NOT NULL DEFAULT 0,
            "Email" VARCHAR(100) NOT NULL,
            "EmailConfirmado" INT NOT NULL DEFAULT 0,
            "CNPJEmpresa" VARCHAR(14) NOT NULL,
            CONSTRAINT "iSenhas1" PRIMARY KEY ("Id"),
            CONSTRAINT "iSenhas2" UNIQUE ("LoginUsuario")  -- restrição exclusiva
        );

    END IF;
END
$$;


DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'UsuariosWeb'
    ) THEN

        CREATE TABLE "UsuariosWeb" (
            "Id" SERIAL,
            "SenhaId" INT NOT NULL,
            "CPFUsuario" VARCHAR(11) NOT NULL,
            "DataNascimentoUsuario" DATE NOT NULL, -- Feito pelo Qoder em 22/08/2026 — data de negócio em DATE
            "CNPJEmpresa" VARCHAR(14) NOT NULL,
            "DataCadastro" TIMESTAMPTZ NOT NULL,
            CONSTRAINT "iUsuariosWeb1" PRIMARY KEY ("Id"),
            CONSTRAINT "iUsuariosWeb_Senhas"
                FOREIGN KEY ("SenhaId")
                REFERENCES "Senhas"("Id")
                ON DELETE CASCADE
                ON UPDATE CASCADE,
    		CONSTRAINT "iUsuariosWeb_SenhaId" UNIQUE ("SenhaId")
        );

    END IF;
END
$$;

-- Só insere o registro na tabela "Senhas" se o mesmo não existir
--Feito pelo Qoder em 23/08/2026 — credenciais reais substituídas por PLACEHOLDERS para versionamento.
--Na implantação, gere o valor criptografado da senha (FerramentaCripto) e ajuste os dados do administrador.
INSERT INTO "Senhas"
    ("LoginUsuario", "NomeUsuario", "NomeCompleto", "SenhaUsuario", "DataCadastro", "DataExpira", "Assinatura", "UsarAssinatura", "Bloqueado", "Administrador", "Email", "EmailConfirmado", "CNPJEmpresa")
VALUES
    ('admin@exemplo.com.br', 'Admin', 'Administrador do Sistema', '<VALOR_CRIPTOGRAFADO_DA_SENHA>', NOW(), NULL, NULL, 0, 0, 1, 'admin@exemplo.com.br', 1, '00000000000100')
ON CONFLICT ("LoginUsuario") DO NOTHING;

-- Só insere o registro na tabela "UsuariosWeb" se o mesmo não existir
INSERT INTO "UsuariosWeb"
    ("SenhaId", "CPFUsuario", "DataNascimentoUsuario", "CNPJEmpresa", "DataCadastro")
VALUES
    (1, '00000000000', '1990-01-01', '00000000000100', NOW())
ON CONFLICT DO NOTHING;



DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'Empresa'
    ) THEN

        CREATE TABLE "Empresa" (
            "Id" SERIAL,
            "Matriz" INT NOT NULL DEFAULT 1,
            "Filial" INT NOT NULL DEFAULT 0,  -- Se matriz=1 e filial=0 então trata-se de uma matriz!
            "Sigla" VARCHAR(20) NOT NULL,
            "NomeFantasia" VARCHAR(100) NOT NULL,
            "RazaoSocial" VARCHAR(100) NOT NULL,
            "CNPJ" CHAR(14) NOT NULL,
            "Logradouro" VARCHAR(20),
            "Endereco" VARCHAR(100),
            "Numero" VARCHAR(15),
            "Complemento" VARCHAR(25),
            "Bairro" VARCHAR(45),
            "Cidade" VARCHAR(45),
            "UF" VARCHAR(2),
            "CEP" CHAR(8),
            "Telefones" VARCHAR(15) NOT NULL,
            "Email" VARCHAR(500) NOT NULL,
            "SiteURL" VARCHAR(2000),
            "HostLogoMarca" VARCHAR(2000),
            "UnidadeLogoMarca" VARCHAR(2),
            "CaminhoLogoMarca" VARCHAR(2000),
            "NomeLogoMarca" VARCHAR(500),
            "TituloEmpresa" VARCHAR(100),
            "SubTituloEmpresa" VARCHAR(100),
            "Rodape" VARCHAR(140),
            "StringConexao" VARCHAR(2000),
            "SmtpServer" VARCHAR(2000),
            "SmtpPortSSL" INT NOT NULL DEFAULT 0,
            "SmtpRequerSSL" BOOLEAN NOT NULL DEFAULT FALSE,
            "SmtpPortTLS" INT NOT NULL DEFAULT 0,
            "SmtpRequerTLS" BOOLEAN NOT NULL DEFAULT FALSE,
            "SmtpUsername" VARCHAR(500),
            "SmtpPassword" VARCHAR(500),
            "SmtpName" VARCHAR(500),
            "SmtpSenhaApp" VARCHAR(500),
            "PopServer" VARCHAR(500),
            "PopPortSSL" INT NOT NULL DEFAULT 0,
            "PopRequerSSL" BOOLEAN NOT NULL DEFAULT FALSE,
            "PopUsername" VARCHAR(500),
            "PopPassword" VARCHAR(500),
            "PopName" VARCHAR(500),
            "DataExpira" TIMESTAMPTZ,
            "DataCadastro" TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT "iEmpresa1" PRIMARY KEY ("Id"),
            CONSTRAINT "iEmpresa2" UNIQUE ("Sigla", "Matriz", "Filial")
        );

    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'ReCaptchaMonitoramento'
    ) THEN

        CREATE TABLE "ReCaptchaMonitoramento" (
            "Id" SERIAL,
            "NomeProjeto" VARCHAR(100),
            "QuantidadeSolicitacoes" INT NOT NULL DEFAULT 0,
            "MesReferencia" INT NOT NULL,
            "AnoReferencia" INT NOT NULL,
            CONSTRAINT "iReCaptchaMonitoramento1" PRIMARY KEY ("Id")
        );

    END IF;
END
$$;

