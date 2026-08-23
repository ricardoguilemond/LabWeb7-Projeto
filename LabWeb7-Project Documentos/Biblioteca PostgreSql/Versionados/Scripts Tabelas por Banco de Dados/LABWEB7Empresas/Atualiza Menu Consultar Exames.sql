-- Script para atualizar o item de menu "Consultar Exames" no banco existente
-- Feito pelo Kiro em 17/05/2026
-- Executar uma única vez no banco de desenvolvimento

UPDATE "ControleDePerfilMenu"
SET "Controller" = 'ConsultarExames',
    "Action" = 'Index'
WHERE "Coluna" = 2
  AND "Nivel" = '002'
  AND "Menu" = 'Consultar Exames';

-- Verificação
SELECT * FROM "ControleDePerfilMenu"
WHERE "Coluna" = 2
ORDER BY "Nivel";
