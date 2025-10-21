SELECT TABLE_NAME Tabela, COLUMN_NAME as Campo, 
       IS_NULLABLE as Nulo, DATA_TYPE as Tipo, CHARACTER_MAXIMUM_LENGTH as Tamanho
  FROM INFORMATION_SCHEMA.COLUMNS 
 WHERE table_name = 'assinaturas'
   --AND table_schema in (SELECT DATABASE())



