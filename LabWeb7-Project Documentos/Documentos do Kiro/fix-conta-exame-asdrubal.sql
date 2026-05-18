-- =============================================================================
-- Correção da ContaExame nos registros de teste do ASDRUBAL TROUXE O TROMBONE
-- Paciente Id = 98
-- Valor incorreto no banco: "01.001.0001" (com pontos, formato errado)
-- Valor correto no banco:   "11030010001" (11 dígitos numéricos, sem pontos)
-- Formatação em tela (FormatarContaExameSem11): "03.001.0001"
-- =============================================================================

-- Corrigir ItensExamesRealizados
UPDATE "ItensExamesRealizados"
SET "ContaExame" = '11030010001'
WHERE "PacienteId" = 98
  AND "Descricao" = 'Glicose'
  AND "ContaExame" LIKE '%01.001.0001%';

-- Corrigir Requisitar
UPDATE "Requisitar"
SET "ContaExame" = '11030010001'
WHERE "PacienteId" = 98
  AND "Descricao" = 'Glicose'
  AND "ContaExame" LIKE '%01.001.0001%';

-- Verificar resultado
SELECT 'ItensExamesRealizados' AS tabela, "Id", "ContaExame"
FROM "ItensExamesRealizados"
WHERE "PacienteId" = 98 AND "Descricao" = 'Glicose'
ORDER BY "Id"
LIMIT 5;

SELECT 'Requisitar' AS tabela, "Id", "ContaExame"
FROM "Requisitar"
WHERE "PacienteId" = 98 AND "Descricao" = 'Glicose'
ORDER BY "Id"
LIMIT 5;
