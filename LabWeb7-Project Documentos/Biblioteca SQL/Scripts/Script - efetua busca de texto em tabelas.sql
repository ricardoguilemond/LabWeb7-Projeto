--
-- Script para efetuar a busca de um determinado TEXTO em TODAS AS TABELAS
-- ESTE SCRIPT GERA RESULTADO NO ATO DA EXECUÇÃO, MOSTRANDO AS RESPOSTAS.
-- A CONSULTA FEITA NO SGOP_BDCONNECT PODE SER BASTANTE DEMORADA.
-- Deve ser rodado em SGOP_MAPFRE, GERPF_MAPFRE e SGOP_BDCONNECT para averiguar se existe MAPFRE escrito em minúscula.
-- Pode ser rodado em todas as base sem problema.
--

-- Declaração do SQL
Declare @SQL        varchar(max) = ''
Declare @pularLinha varchar(max) = char(13) + char(10)

-- Declaração da Busca
-- Estamos usando COLLATE Latin1_General_BIN LIKE par buscar EXATAMENTE o texto CASE SENSITIVE que queremos...
Declare @filtro     varchar(max) = 'Alça Viária%'      -- Texto a ser procurado
Declare @filtro_www varchar(max) = ''

 
SELECT
       tabelas.name   AS Tabela, 
       colunas.name   AS Coluna,
       tipos.name     AS Tipo,
       colunas.length AS Tamanho
INTO
       temp_result
FROM 
       sysobjects tabelas
       inner join syscolumns colunas ON colunas.id = tabelas.id
       inner join systypes tipos     ON tipos.xtype = colunas.xtype
WHERE 
       tabelas.xtype = 'u'
       AND tipos.name IN ('text', 'ntext', 'varchar', 'nvarchar')   -- informar os tipos das colunas que serão rastreadas
 
-- Usaremos o SET CURSOR para fazer a varredura nas tabelas
DECLARE cTabelas cursor LOCAL fast_forward FOR
        SELECT DISTINCT Tabela FROM temp_result
 
DECLARE @nomeTabela varchar(255)
 
OPEN cTabelas
 
fetch NEXT FROM cTabelas INTO @nomeTabela
 
while @@fetch_status = 0
BEGIN
   -- cursor para varrer as colunas da tabela corrente/encontrada na sequência da busca
   DECLARE cColunas cursor LOCAL fast_forward FOR
           SELECT Coluna, Tipo, Tamanho FROM temp_result WHERE Tabela = @nomeTabela
   
   DECLARE @nomeColuna    varchar(255)
   DECLARE @tipoColuna    varchar(255)
   DECLARE @tamanhoColuna varchar(255)
   
   OPEN cColunas
   
   -- monta as colunas da cláusula select 
   fetch NEXT FROM cColunas INTO @nomeColuna, @tipoColuna, @tamanhoColuna
   
   while @@fetch_status = 0
   BEGIN
      -- cria a declaração da variável
      SET @SQL = 'declare @hasresults bit' + @pularLinha + @pularLinha
   	  
      -- cria o select
      SET @SQL = @SQL + 'select '      + @pularLinha
      SET @SQL = @SQL + char(9) + '''' + @nomeTabela + ''' AS NomeTabela'
      SET @SQL = @SQL + char(9) + ','  + @nomeColuna + @pularLinha
   	  
      -- adiciona uma coluna com o tipo e o tamanho do campo
      SET @SQL = @SQL  + char(9) + ',' + '''' + @tipoColuna + ''' AS ''' + @nomeColuna + '_Tipo''' + @pularLinha
      SET @SQL = @SQL  + char(9) + ',' + 'DATALENGTH(' + @nomeColuna + ') AS ''' + @nomeColuna + '_Tamanho_Ocupado''' + @pularLinha    
      SET @SQL = @SQL  + char(9) + ',' + '''' + @tamanhoColuna + ''' AS ''' + @nomeColuna + '_Tamanho_Maximo''' + @pularLinha
   	  
      -- define o chamado da tabela temporária (temp_result)
      SET @SQL = @SQL + 'INTO' + @pularLinha + char(9) + 'temp_result_' + @nomeTabela + @pularLinha
   	  
      -- adiciona a cláusula FROM para apontar para a tabela que iremos LER.
      SET @SQL = @SQL +  'FROM' + @pularLinha + char(9) + @nomeTabela + @pularLinha
   	  
      -- inicia a montagem do where com a cláusula LIKE 
      SET @SQL = @SQL + 'WHERE' + @pularLinha
      SET @SQL = @SQL + char(9) + @nomeColuna + ' COLLATE Latin1_General_BIN LIKE ''' + @filtro + '''' + @pularLinha
   	  
      SET @SQL = @SQL + @pularLinha + 'Declare @res int = isnull( (SELECT count(*) FROM temp_result_' + @nomeTabela + '), 0)' + @pularLinha
      SET @SQL = @SQL + @pularLinha + 'IF @res > 0'
      SET @SQL = @SQL + @pularLinha + 'BEGIN'
      SET @SQL = @SQL + @pularLinha + char(9) + 'SELECT * FROM temp_result_' + @nomeTabela
      SET @SQL = @SQL + @pularLinha + 'END' + @pularLinha
      SET @SQL = @SQL + @pularLinha + 'DROP TABLE temp_result_' + @nomeTabela
      SET @SQL = @SQL + @pularLinha

	  IF OBJECT_ID('temp_result_'+@nomeTabela, 'U') IS NOT NULL
      DROP TABLE #TempTableName; 
   	  
      fetch NEXT FROM cColunas INTO @nomeColuna, @tipoColuna, @tamanhoColuna
	  -- descomentar a linha abaixo para ver as mensagens de SQL
      --print @sql
   	  
      EXEC(@SQL)
      SET @SQL = ''
   END
   
   close cColunas
   deallocate cColunas
   
   fetch NEXT FROM cTabelas INTO @nomeTabela
END
 
close cTabelas
deallocate cTabelas
 
--select * from temp_Result 

-- depois de tudo listado, excluímos a tabela temporária temp_result
DROP TABLE temp_result



