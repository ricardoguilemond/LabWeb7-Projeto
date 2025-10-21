
-----  EM DESENVOLVIMENTO, FALTA A PARTE DA BASE EXTERNA


-- listando a tabela principal com suas referenciadas em outras bases de dados
-- Informe no USE a base da operadora SGOP_XXX:
USE [SGOP_PREV]
-- Informe a base externa (outra base de dados para investigar possíveis relacionamentos):
declare @baseExterna varchar(max) = 'GERPF_PREV' 
-- Informe a tabela a ser pesquisada:
declare @nomeTabela varchar(max) = 'TblCredenciado'
-- 
-- LISTANDO de acordo com os dados pedidos acima.
--
set @baseExterna = concat('[',@baseExterna,']')
declare @chavePrimaria varchar(max) = isnull(  (select   TOP 1 replace(CONSTRAINT_NAME,concat(@nomeTabela,'_'),'') 
                                                  from   INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                                                 where   TABLE_NAME = @nomeTabela
                                                   and   CONSTRAINT_TYPE = 'PRIMARY KEY'), '')

declare @base varchar(max) = isnull( (select top 1 TABLE_CATALOG from information_schema.COLUMNS where TABLE_NAME = @nomeTabela), '')
select @base 'Base Atual',
       @nomeTabela 'Tabela da Pesquisa', 
	   @chavePrimaria 'Chave Primária', 
	   @baseExterna 'BASE EXTERNA a ser pesquisada'

select     Base_Atual = PKT.TABLE_CATALOG,
           Tabela = PKT.TABLE_NAME,
		   PK_Coluna = PKC.COLUMN_NAME,
           TABELAS_REFERENCIADAS_NA_BASE_DA_OPERADORA = FKT.TABLE_NAME,
		   FK_Coluna_Referenciada = COL.COLUMN_NAME,
	       Constraint_Name = C.CONSTRAINT_NAME
from       INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS C
INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS       FKT  on C.CONSTRAINT_NAME = FKT.CONSTRAINT_NAME
INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS       PKT  on C.UNIQUE_CONSTRAINT_NAME = PKT.CONSTRAINT_NAME
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE        COL  on C.CONSTRAINT_NAME = COL.CONSTRAINT_NAME
INNER JOIN (
			   select     i1.TABLE_NAME, i2.COLUMN_NAME
			   from       INFORMATION_SCHEMA.TABLE_CONSTRAINTS i1
			   INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE  i2  on i1.CONSTRAINT_NAME = i2.CONSTRAINT_NAME
			   where      i1.CONSTRAINT_TYPE = 'PRIMARY KEY'
		   ) PKC on PKC.TABLE_NAME = PKT.TABLE_NAME
---- optional:
where PKT.TABLE_NAME = @nomeTabela
order by FKT.TABLE_NAME

--

