-- Migração: Recebimento Consolidado por Instituição/Período
-- Objetivo: permitir o recebimento de todos os exames de uma instituição em um
--           período (data inicial/final) em uma única operação, com:
--             * Valor Total Devido imutável (soma dos valores dos exames);
--             * desconto/acréscimo informado pelo usuário (não altera o valor
--               individual de cada exame);
--             * Valor Total a ser Pago (devido + ajuste), quitado pelas formas
--               de recebimento informadas.
--           O recebimento consolidado abrange exames de VÁRIOS pacientes, por
--           isso PacienteId passa a aceitar NULL.
-- Data: 16/08/2026 (Qoder)

-- PASSO 1: PacienteId opcional (recebimento consolidado não tem paciente único)
ALTER TABLE "CatalogoRecebimentos"
    ALTER COLUMN "PacienteId" DROP NOT NULL;

-- PASSO 2: valor total devido (soma imutável dos exames do período)
ALTER TABLE "CatalogoRecebimentos"
    ADD COLUMN IF NOT EXISTS "ValorTotalDevido" NUMERIC(18,2) NULL;

-- PASSO 3: backfill — nos registros existentes a soma dos exames equivale ao
--          valor recebido (ValorTotal) mais o desconto concedido (ValorDesconto)
UPDATE "CatalogoRecebimentos"
   SET "ValorTotalDevido" = "ValorTotal" + "ValorDesconto"
 WHERE "ValorTotalDevido" IS NULL;
