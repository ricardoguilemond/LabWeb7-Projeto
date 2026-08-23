-- Criação da tabela ExameReferencia para armazenar referências de exames migradas do Delphi
-- Feito pelo Kiro em 11/07/2025

CREATE TABLE IF NOT EXISTS "ExameReferencia" (
    "Id"                SERIAL          NOT NULL,
    "ContaExame"        VARCHAR(11)     NOT NULL,
    "TabelaExamesId"    INT             NOT NULL,
    "ConteudoBinario"   BYTEA           NOT NULL,
    "FormatoOrigem"     VARCHAR(10)     NOT NULL DEFAULT 'RTF',
    "AlinhaLaudo"       INT             NOT NULL DEFAULT 0,
    "DataCriacao"       TIMESTAMPTZ     NOT NULL,
    "DataAlteracao"     TIMESTAMPTZ     NOT NULL,
    "UsuarioAlteracao"  VARCHAR(100)    NOT NULL,
    "Versao"            INT             NOT NULL DEFAULT 1,
    CONSTRAINT "iExameReferencia1" PRIMARY KEY ("Id"),
    CONSTRAINT "iExameReferencia_TabelaExames" FOREIGN KEY ("TabelaExamesId")
        REFERENCES "TabelaExames"("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "iExameReferencia2" ON "ExameReferencia"("ContaExame", "TabelaExamesId");
CREATE INDEX IF NOT EXISTS "iExameReferencia3" ON "ExameReferencia"("ContaExame");

-- ..Kiro
