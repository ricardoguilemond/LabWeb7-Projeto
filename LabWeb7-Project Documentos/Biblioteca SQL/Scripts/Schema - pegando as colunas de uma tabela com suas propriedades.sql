-- Pegando as colunas de uma tabela com suas propriedades
USE [SGOP_PREV]

declare @nomeTabela varchar(max) = 'TblPlano'

select TABLE_NAME Tabela, COLUMN_NAME as Campo, 
       IS_NULLABLE as Nulo, DATA_TYPE as Tipo, CHARACTER_MAXIMUM_LENGTH as Tamanho
  from INFORMATION_SCHEMA.COLUMNS 
 where table_name = @nomeTabela


