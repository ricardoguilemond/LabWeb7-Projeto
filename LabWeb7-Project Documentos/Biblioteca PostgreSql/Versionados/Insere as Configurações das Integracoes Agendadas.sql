--select 'IntegracaoDadosArmazenamento', * from IntegracaoDadosArmazenamento;
--select 'IntegracaoDadosConfiguracao',  * from IntegracaoDadosConfiguracao;
--select 'IntegracaoDadosLayout',        * from IntegracaoDadosLayout;
select * from IntegracaoDadosExecucao;
select * from IntegracaoDadosExecucaoArquivo;
select * from LogArquivos;

-- RETURN (removido - não existe no PostgreSQL)

select 'IntegracaoDadosArmazenamento', * from IntegracaoDadosArmazenamento;
select     * 
from       IntegracaoDadosConfiguracao conf
inner join IntegracaoDadosLayout layout ON conf.Id = layout.IntegracaoDadosConfiguracaoId;

-- RETURN (removido - não existe no PostgreSQL)

UPDATE IntegracaoDadosLayout
SET Habilitado = 1
WHERE Id = 1;

-- RETURN (removido - não existe no PostgreSQL)

--IntegracaoDadosArmazenamento
INSERT INTO IntegracaoDadosArmazenamento
(Senha, TipoArmazenamento, Host, Usuario, UsuarioLogin) VALUES (12345, 1, 'localhost', 1, 'sistema');

--IntegracaoDadosConfiguracao
INSERT INTO IntegracaoDadosConfiguracao
(IntegracaoDadosArmazenamentoId, PastaSaida, PastaEntrada, NomeArquivo, HoraExecucao, DiaExecucao, Periodicidade, PastaEntradaProcessado, PastaEntradaProcessadoErro, PastaEntradaProcessadoParcial, UsuarioPadrao)
VALUES 
(1,'C:\Temp', 'C:\Temp', 'ServicoExportacaoPacientes', '15:00', 1, 1, 'C:\Temp', 'C:\Temp', 'C:\Temp', 1);

--IntegracaoDadosLayout
INSERT INTO IntegracaoDadosLayout
(IntegracaoDadosConfiguracaoId, Descricao, TipoServico, Exportacao, Habilitado) 
VALUES 
(1, 'Serviço de Exportação de Cadastro de Pacientes', 1, 1, 1);

--Periodicidade
INSERT INTO IntegracaoDadosPeriodicidade
(TipoPeriodoExtracao) VALUES ('diario');
INSERT INTO IntegracaoDadosPeriodicidade
(TipoPeriodoExtracao) VALUES ('semanal');
INSERT INTO IntegracaoDadosPeriodicidade
(TipoPeriodoExtracao) VALUES ('mensal');
INSERT INTO IntegracaoDadosPeriodicidade
(TipoPeriodoExtracao) VALUES ('retroativo');
