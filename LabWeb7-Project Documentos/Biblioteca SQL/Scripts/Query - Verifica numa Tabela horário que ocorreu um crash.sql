--
-- Verifica numa tabela, em que hor�rio ocorreu um crash
--
SELECT [filename], creation_time FROM sys.dm_server_memory_dumps WITH (NOLOCK) 
ORDER BY creation_time DESC  

