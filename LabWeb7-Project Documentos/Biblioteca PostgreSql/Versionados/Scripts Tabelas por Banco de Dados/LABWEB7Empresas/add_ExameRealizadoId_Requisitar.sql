-- Adiciona coluna ExameRealizadoId na tabela Requisitar
-- Criado pelo Kiro em 03/05/2026
--
-- Vínculo LÓGICO com ExamesRealizados.Id — SEM FK física.
-- Nullable para compatibilidade com dados existentes.
-- Executar uma única vez em cada banco de dados do cliente.

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'Requisitar'
          AND column_name = 'ExameRealizadoId'
    ) THEN
        ALTER TABLE "Requisitar"
            ADD COLUMN "ExameRealizadoId" INT;

        RAISE NOTICE 'Coluna ExameRealizadoId adicionada à tabela Requisitar.';
    ELSE
        RAISE NOTICE 'Coluna ExameRealizadoId já existe na tabela Requisitar.';
    END IF;
END $$;
