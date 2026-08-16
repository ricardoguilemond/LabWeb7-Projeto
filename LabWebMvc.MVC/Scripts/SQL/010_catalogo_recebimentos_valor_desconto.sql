-- Migração: Adicionar coluna de desconto no Catálogo de Recebimentos
-- Objetivo: permitir registrar desconto concedido no recebimento (Portaria/Faturamento).
--           "ValorTotal" continua guardando o valor efetivamente recebido; "ValorDesconto"
--           registra o desconto concedido para auditoria/relatórios.
-- Data: 16/08/2026 (Qoder)

ALTER TABLE "CatalogoRecebimentos"
    ADD COLUMN IF NOT EXISTS "ValorDesconto" NUMERIC(18, 2) NOT NULL DEFAULT 0;
