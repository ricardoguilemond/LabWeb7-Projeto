-- Conectar ao banco: \c LABWEB7Empresas
-- Convertido de MSSQL para PostgreSQL

DROP TABLE IF EXISTS EmpresaCliente;

----Controle das empresas/clientes do Sistema
----"StringConexao": determina para qual base de dados o cliente irá se conectar!
CREATE TABLE IF NOT EXISTS EmpresaCliente
(
  Id                  SERIAL         NOT NULL,
  CNPJ                varchar(14)    NOT NULL,
  Email               varchar(500)   NOT NULL,
  StringConexao       varchar(2000),
  LimiteUsuarios      int            NOT NULL DEFAULT 2,
  DataExpira          TIMESTAMP,
  DataCadastro        TIMESTAMP      NOT NULL,
  CONSTRAINT iEmpresaCliente1 PRIMARY KEY (Id),
  CONSTRAINT iEmpresaCliente2 UNIQUE (CNPJ),
  CONSTRAINT iEmpresaCliente3 UNIQUE (Email)
);

select * from EmpresaCliente;

/*
  LÓGICA:
  Na tabela acima, estão somente os ADMINISTRADORES dos sistemas dos clientes. 
  Apenas um único ADM por cliente, com CNPJ e e-mail único!
*/
