--
-- Encontra qual(ou quais) tabela(s) possuem o campo mencionado
--
Declare @nome_do_campo varchar(max) = 'Bancaria'
SELECT 
       OBJECT_NAME(id) AS Tabela,
       Name            AS Coluna 
FROM 
       sys.syscolumns 
WHERE 
       name LIKE '%'+@nome_do_campo+'%'
