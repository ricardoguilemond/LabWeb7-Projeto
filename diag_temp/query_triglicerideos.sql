-- 1. Buscar Triglicerídeos na PlanoExames para identificar a folha correta
SELECT pe."Id", pe."ExameId", pe."ContaExame", pe."RefExame", pe."RefItem", pe."Descricao",
       ce."Id" AS "ClasseExamesId", ce."RefExame" AS "NomeFolha"
FROM "PlanoExames" pe
JOIN "ClasseExames" ce ON ce."Id" = pe."ExameId"
WHERE pe."Descricao" ILIKE '%triglic%'
ORDER BY ce."RefExame", pe."ContaExame";

-- 2. Verificar registros do ASDRUBAL com Descricao Triglicerídeos em ItensExamesRealizados
SELECT i."Id", i."ExameRealizadoId", i."ClasseExamesId", i."ClasseExamesNome", i."RefExame", i."RefItem", i."ContaExame", i."Descricao"
FROM "ItensExamesRealizados" i
WHERE i."PacienteId" = 98
  AND i."Descricao" ILIKE '%triglic%'
ORDER BY i."Id";
