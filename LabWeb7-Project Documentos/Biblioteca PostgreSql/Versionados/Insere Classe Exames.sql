--Criar as Folhas de Exames por Script
select * from ClasseExames
order by Id;
--RefExame


select * from TabelaExames;

select * from PlanoExames
where    ContaExame = '11020030001'
order by ExameId, TabelaExamesId;

select Id, ExameId, TabelaExamesId, ContaExame, RefExame, RefItem, Descricao, ValorCusto, ValorItem
from   PlanoExames
where  ExameId = 2
and    TabelaExamesId = 1     ----SUS = 1
order  by ContaExame, TabelaExamesId;

select Id, ExameId, TabelaExamesId, ContaExame, RefExame, RefItem, Descricao, ValorCusto, ValorItem
from   PlanoExames
--where  Id = 41
where ValorCusto is not null;





select * from TabelaExames
order by NomeTabela;

-- RETURN (removido - não existe no PostgreSQL)

delete from PlanoExames
where Descricao = 'CULTURA';
--where ContaExame = '11020020000'


select * from PlanoExames
where ExameId = 42
order by ExameId;



select * from ControleDePerfilMenu
order by Coluna, Nivel;
