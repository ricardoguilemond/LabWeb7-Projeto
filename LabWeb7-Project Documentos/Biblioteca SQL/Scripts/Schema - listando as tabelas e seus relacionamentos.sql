-- Listando a(s) tabela(s) e seus relacionamentos
USE [SGOP_BDCONNECT]

declare @nomeTabela varchar(max) = 'TblOperadora'

select     Tabela = PKT.TABLE_NAME,
		   PK_Coluna = PKC.COLUMN_NAME,
           Referenciada = FKT.TABLE_NAME,
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
--ORDER BY 1,2,3,4
--WHERE FKT.TABLE_NAME='something'
--WHERE PKT.TABLE_NAME IN ('one_thing', 'another')
--WHERE FKT.TABLE_NAME IN ('one_thing', 'another')

-- Informações da Tabela
---------------------------------------------------------------------------------------------------------------
select    ic.TABLE_CATALOG,
          ic.TABLE_NAME,
          ic.COLUMN_NAME,
          ic.IS_NULLABLE,
          ic.DATA_TYPE,
          ic.CHARACTER_MAXIMUM_LENGTH,
          it.CONSTRAINT_NAME,
          rf.UPDATE_RULE,
          rf.DELETE_RULE
from      information_schema.columns ic
left join information_schema.KEY_COLUMN_USAGE it
left join INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rf on it.CONSTRAINT_NAME = rf.CONSTRAINT_NAME
                                                        on it.COLUMN_NAME = ic.COLUMN_NAME and it.TABLE_NAME = @nomeTabela
where     it.TABLE_NAME = ic.TABLE_NAME or it.TABLE_NAME is null
and       it.TABLE_NAME = @nomeTabela
order by  ic.TABLE_NAME, it.CONSTRAINT_NAME desc, ic.COLUMN_NAME

