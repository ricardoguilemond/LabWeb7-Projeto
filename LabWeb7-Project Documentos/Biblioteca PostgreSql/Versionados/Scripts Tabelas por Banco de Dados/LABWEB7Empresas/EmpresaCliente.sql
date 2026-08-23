-- Selecionar o banco de dados (pgAdmin não precisa do USE, você conecta no banco diretamente)
-- DROP TABLE se existir
DROP TABLE IF EXISTS "EmpresaCliente";

-- Criar a tabela
CREATE TABLE "EmpresaCliente"
(
    "Id" SERIAL PRIMARY KEY,                             -- IDENTITY(1,1) → SERIAL
    "CNPJ" VARCHAR(14) NOT NULL UNIQUE,                  -- UNIQUE direto na coluna
    "Email" VARCHAR(500) NOT NULL,
    "StringConexao" VARCHAR(4000) NOT NULL,
    "LimiteUsuarios" INT NOT NULL DEFAULT 2,
    "DataExpira" TIMESTAMP NOT NULL DEFAULT (CURRENT_DATE + INTERVAL '365 days'),
    "DataCadastro" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
