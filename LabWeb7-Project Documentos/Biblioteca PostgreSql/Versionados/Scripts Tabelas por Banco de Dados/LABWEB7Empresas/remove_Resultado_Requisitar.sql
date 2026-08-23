-- ============================================================================
-- Script de Migração: Remoção da coluna "Resultado" da tabela "Requisitar"
-- Data: 04/06/2026
-- Autor: Qoder
-- Motivo: O campo Resultado na tabela Requisitar não é utilizado pelo usuário.
--         O resultado relevante está na tabela ItensExamesRealizados.
-- ============================================================================

-- Verifica se a coluna existe antes de remover
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'Requisitar' 
        AND column_name = 'Resultado'
    ) THEN
        -- Remove a coluna Resultado da tabela Requisitar
        ALTER TABLE "Requisitar" DROP COLUMN "Resultado";
        
        RAISE NOTICE 'Coluna "Resultado" removida com sucesso da tabela "Requisitar".';
    ELSE
        RAISE NOTICE 'Coluna "Resultado" já não existe na tabela "Requisitar".';
    END IF;
END $$;

-- ============================================================================
-- Observações Importantes:
-- ============================================================================
-- 1. Este script é idempotente (pode ser executado múltiplas vezes com segurança)
-- 2. Não afeta dados existentes em outras colunas
-- 3. O campo Resultado da tabela ItensExamesRealizados NÃO é alterado
-- 4. Executar uma vez por banco de dados de cliente
-- 5. Não há rollback necessário pois o campo nunca foi utilizado
-- ============================================================================
