-- Adiciona a coluna TipoCorteCupom na tabela Configuracoes
-- 0 = Nenhum, 1 = Parcial, 2 = Total (padrao)
ALTER TABLE "Configuracoes"
    ADD COLUMN IF NOT EXISTS "TipoCorteCupom" INTEGER NOT NULL DEFAULT 2;
