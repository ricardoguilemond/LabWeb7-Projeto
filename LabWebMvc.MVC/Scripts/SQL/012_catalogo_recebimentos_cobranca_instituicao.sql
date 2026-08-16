-- Migração: Flag de cobrança à instituição no Catálogo de Recebimentos
-- Objetivo: registrar recebimentos na Portaria em que o valor NÃO é pago pelo paciente,
--           mas será cobrado da Instituição (título Pendente até a baixa).
-- Data: 16/08/2026 (Qoder)

ALTER TABLE "CatalogoRecebimentos"
    ADD COLUMN IF NOT EXISTS "CobrancaInstituicao" BOOLEAN NOT NULL DEFAULT FALSE;
