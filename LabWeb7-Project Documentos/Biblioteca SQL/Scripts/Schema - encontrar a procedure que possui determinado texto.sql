USE [GERPF_PREV]
 
-- Iniciando a pesquisa nas tabelas de sistemas
 
SELECT     A.NAME, A.TYPE, B.TEXT
  FROM     SYSOBJECTS  A (nolock)
INNER JOIN SYSCOMMENTS B (nolock)  ON  A.ID = B.ID
WHERE      B.TEXT LIKE '%tbl_Log_Sinc_Gerpf_Sgop%'     --- Informação a ser procurada no corpo da procedure, funcao ou view
  AND      A.TYPE = 'P'                            --- Tipo de objeto a ser localizado no caso procedure
 ORDER BY  A.NAME
 
