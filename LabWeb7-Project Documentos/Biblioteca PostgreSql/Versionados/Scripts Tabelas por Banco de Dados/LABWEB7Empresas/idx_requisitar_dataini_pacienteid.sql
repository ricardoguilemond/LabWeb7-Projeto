-- Índice composto para otimizar a query GetLancamentosHoje
-- Criado pelo Kiro em 01/05/2026
--
-- Problema: a query WHERE DataIni BETWEEN ... GROUP BY PacienteId
-- fazia full table scan (Seq Scan) na tabela Requisitar.
-- Com este índice, o PostgreSQL faz Index Scan + agrupamento eficiente.
--
-- Executar uma única vez em cada banco de dados do cliente.
-- Não causa downtime — CREATE INDEX é não-bloqueante em PostgreSQL.

CREATE INDEX IF NOT EXISTS idx_requisitar_dataini_pacienteid
ON "Requisitar" ("DataIni", "PacienteId");
