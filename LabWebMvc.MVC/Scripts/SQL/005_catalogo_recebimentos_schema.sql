-- Migração: Criar schema do Catálogo de Recebimentos
-- Objetivo: Estruturar tabelas e flags para controle de recebimentos de exames
-- Data: 2026

-- ==============================================================================
-- PASSO 1: Adicionar flag EmCatalogoRecebimentos nas tabelas de exames
-- ==============================================================================

ALTER TABLE "ExamesRealizados"
    ADD COLUMN IF NOT EXISTS "EmCatalogoRecebimentos" BOOLEAN DEFAULT FALSE;

ALTER TABLE "ExamesRealizadosAM"
    ADD COLUMN IF NOT EXISTS "EmCatalogoRecebimentos" BOOLEAN DEFAULT FALSE;

-- ==============================================================================
-- PASSO 2: Tabela de Contas de Recebimento
-- ==============================================================================

CREATE TABLE IF NOT EXISTS "ContasRecebimento" (
    "Id" SERIAL PRIMARY KEY,
    "Nome" VARCHAR(100) NOT NULL,
    "Tipo" INTEGER NOT NULL DEFAULT 4, -- 1=Caixa, 2=Banco, 3=Cofre, 4=Outro
    "Identificacao" VARCHAR(100),
    "PadraoPortaria" BOOLEAN DEFAULT FALSE,
    "Ativo" BOOLEAN DEFAULT TRUE,
    "DataRegistro" TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "idx_ContasRecebimento_Ativo"
    ON "ContasRecebimento" ("Ativo");

CREATE INDEX IF NOT EXISTS "idx_ContasRecebimento_PadraoPortaria"
    ON "ContasRecebimento" ("PadraoPortaria")
    WHERE "PadraoPortaria" = TRUE;

-- ==============================================================================
-- PASSO 2.1: Seed de Conta de Recebimento padrão (Caixa da Portaria)
-- ==============================================================================

INSERT INTO "ContasRecebimento" ("Id", "Nome", "Tipo", "Identificacao", "PadraoPortaria", "Ativo")
VALUES (1, 'Caixa', 1, 'Recebimentos de Portaria', TRUE, TRUE)
ON CONFLICT ("Id") DO NOTHING;

-- Garante que o sequence do SERIAL esteja sincronizado após insert com Id explícito
SELECT setval(pg_get_serial_sequence('"ContasRecebimento"', 'Id'), COALESCE((SELECT MAX("Id") FROM "ContasRecebimento"), 1), true);

-- ==============================================================================
-- PASSO 3: Tabela de Formas de Recebimento
-- ==============================================================================

CREATE TABLE IF NOT EXISTS "FormasRecebimento" (
    "Id" SERIAL PRIMARY KEY,
    "Nome" VARCHAR(100) NOT NULL UNIQUE,
    "PermiteParticular" BOOLEAN DEFAULT TRUE,
    "PermiteInstituicao" BOOLEAN DEFAULT TRUE,
    "Ativo" BOOLEAN DEFAULT TRUE,
    "DataRegistro" TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "idx_FormasRecebimento_Ativo"
    ON "FormasRecebimento" ("Ativo");

-- ==============================================================================
-- PASSO 4: Tabela principal do Catálogo de Recebimentos
-- ==============================================================================

CREATE TABLE IF NOT EXISTS "CatalogoRecebimentos" (
    "Id" SERIAL PRIMARY KEY,
    "Origem" INTEGER NOT NULL DEFAULT 1, -- 1=Portaria, 2=Faturamento
    "InstituicaoId" INTEGER NOT NULL,
    "PacienteId" INTEGER NOT NULL,
    "PeriodoFaturamento" VARCHAR(10), -- Formato MM/AAAA, preenchido quando origem = Faturamento
    "ValorTotal" NUMERIC(18, 2) NOT NULL DEFAULT 0,
    "DataRecebimento" DATE NOT NULL,
    "Status" INTEGER NOT NULL DEFAULT 0, -- 0=Pendente, 1=Recebido
    "Observacao" TEXT,
    "UsuarioRegistro" VARCHAR(100),
    "DataRegistro" TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "fk_CatalogoRecebimentos_Instituicao"
        FOREIGN KEY ("InstituicaoId") REFERENCES "Instituicao"("Id"),
    CONSTRAINT "fk_CatalogoRecebimentos_Paciente"
        FOREIGN KEY ("PacienteId") REFERENCES "Pacientes"("Id")
);

CREATE INDEX IF NOT EXISTS "idx_CatalogoRecebimentos_InstituicaoId"
    ON "CatalogoRecebimentos" ("InstituicaoId");

CREATE INDEX IF NOT EXISTS "idx_CatalogoRecebimentos_PacienteId"
    ON "CatalogoRecebimentos" ("PacienteId");

CREATE INDEX IF NOT EXISTS "idx_CatalogoRecebimentos_PeriodoFaturamento"
    ON "CatalogoRecebimentos" ("PeriodoFaturamento");

CREATE INDEX IF NOT EXISTS "idx_CatalogoRecebimentos_DataRecebimento"
    ON "CatalogoRecebimentos" ("DataRecebimento");

-- ==============================================================================
-- PASSO 5: Tabela de vínculo entre Catálogo e Exames
-- ==============================================================================

CREATE TABLE IF NOT EXISTS "CatalogoRecebimentosExames" (
    "Id" SERIAL PRIMARY KEY,
    "CatalogoRecebimentoId" INTEGER NOT NULL,
    "ExameRealizadoId" INTEGER NOT NULL,
    "Valor" NUMERIC(18, 2) NOT NULL DEFAULT 0,
    CONSTRAINT "fk_CatalogoRecebimentosExames_Catalogo"
        FOREIGN KEY ("CatalogoRecebimentoId") REFERENCES "CatalogoRecebimentos"("Id") ON DELETE CASCADE,
    CONSTRAINT "fk_CatalogoRecebimentosExames_Exame"
        FOREIGN KEY ("ExameRealizadoId") REFERENCES "ExamesRealizados"("Id")
);

CREATE INDEX IF NOT EXISTS "idx_CatalogoRecebimentosExames_Catalogo"
    ON "CatalogoRecebimentosExames" ("CatalogoRecebimentoId");

CREATE INDEX IF NOT EXISTS "idx_CatalogoRecebimentosExames_Exame"
    ON "CatalogoRecebimentosExames" ("ExameRealizadoId");

-- ==============================================================================
-- PASSO 6: Tabela de formas de pagamento do Catálogo de Recebimentos
-- ==============================================================================

CREATE TABLE IF NOT EXISTS "CatalogoRecebimentosFormas" (
    "Id" SERIAL PRIMARY KEY,
    "CatalogoRecebimentoId" INTEGER NOT NULL,
    "FormaRecebimentoId" INTEGER NOT NULL,
    "ContaRecebimentoId" INTEGER NOT NULL,
    "Valor" NUMERIC(18, 2) NOT NULL DEFAULT 0,
    "DataRecebimento" DATE NOT NULL,
    "Observacao" TEXT,
    CONSTRAINT "fk_CatalogoRecebimentosFormas_Catalogo"
        FOREIGN KEY ("CatalogoRecebimentoId") REFERENCES "CatalogoRecebimentos"("Id") ON DELETE CASCADE,
    CONSTRAINT "fk_CatalogoRecebimentosFormas_Forma"
        FOREIGN KEY ("FormaRecebimentoId") REFERENCES "FormasRecebimento"("Id"),
    CONSTRAINT "fk_CatalogoRecebimentosFormas_Conta"
        FOREIGN KEY ("ContaRecebimentoId") REFERENCES "ContasRecebimento"("Id")
);

CREATE INDEX IF NOT EXISTS "idx_CatalogoRecebimentosFormas_Catalogo"
    ON "CatalogoRecebimentosFormas" ("CatalogoRecebimentoId");

CREATE INDEX IF NOT EXISTS "idx_CatalogoRecebimentosFormas_Forma"
    ON "CatalogoRecebimentosFormas" ("FormaRecebimentoId");

CREATE INDEX IF NOT EXISTS "idx_CatalogoRecebimentosFormas_Conta"
    ON "CatalogoRecebimentosFormas" ("ContaRecebimentoId");

-- ==============================================================================
-- PASSO 7: Seed de Formas de Recebimento padrão
-- ==============================================================================

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
