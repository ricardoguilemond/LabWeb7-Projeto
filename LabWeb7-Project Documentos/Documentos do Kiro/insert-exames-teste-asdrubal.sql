-- =============================================================================
-- Script de Inserção de Dados de Teste
-- Paciente: ASDRUBAL TROUXE O TROMBONE (Id = 98)
-- 20 exames com itens de glicose, 3 por mês retroativos a partir de Dez/2025
-- Tabelas: ExamesRealizados, ItensExamesRealizados, Requisitar
-- =============================================================================
-- IMPORTANTE: Execute este script no banco LABWEB7 (PostgreSQL)
-- Antes de executar, confirme os IDs abaixo consultando seu banco:
--   SELECT "Id" FROM "Pacientes" WHERE "NomePaciente" ILIKE '%ASDRUBAL%';
--   SELECT "Id", "Sigla" FROM "Instituicao" LIMIT 5;
--   SELECT "Id", "NomePosto" FROM "Postos" LIMIT 5;
--   SELECT "Id" FROM "Medicos" LIMIT 5;
--   SELECT "Id", "NomeTabela" FROM "TabelaExames" LIMIT 5;
--   SELECT "Id", "RefExame" FROM "ClasseExames" LIMIT 5;
-- =============================================================================

DO $$
DECLARE
    v_paciente_id INT := 98;  -- ASDRUBAL TROUXE O TROMBONE
    v_instituicao_id INT;
    v_posto_id INT;
    v_medico_id INT;
    v_tabela_exames_id INT;
    v_classe_exames_id INT;
    v_ref_exame VARCHAR(50);
    v_exame_id INT;
    v_sequencial INT := 0;
    v_data TIMESTAMPTZ;
    v_mes INT;
    v_dia INT;
    v_dias_array INT[] := ARRAY[5, 15, 25];
    v_ordem INT;
BEGIN
    -- Buscar IDs existentes no banco (primeiro registro de cada tabela)
    SELECT "Id" INTO v_instituicao_id FROM "Instituicao" ORDER BY "Id" LIMIT 1;
    SELECT "Id" INTO v_posto_id FROM "Postos" ORDER BY "Id" LIMIT 1;
    SELECT "Id" INTO v_medico_id FROM "Medicos" ORDER BY "Id" LIMIT 1;
    SELECT "Id" INTO v_tabela_exames_id FROM "TabelaExames" ORDER BY "Id" LIMIT 1;
    SELECT "Id", "RefExame" INTO v_classe_exames_id, v_ref_exame
        FROM "ClasseExames" ORDER BY "Id" LIMIT 1;

    -- Validar que todos os IDs foram encontrados
    IF v_instituicao_id IS NULL OR v_posto_id IS NULL OR v_medico_id IS NULL
       OR v_tabela_exames_id IS NULL OR v_classe_exames_id IS NULL THEN
        RAISE EXCEPTION 'IDs de referência não encontrados. Verifique as tabelas auxiliares.';
    END IF;

    -- Verificar que o paciente existe
    IF NOT EXISTS (SELECT 1 FROM "Pacientes" WHERE "Id" = v_paciente_id) THEN
        RAISE EXCEPTION 'Paciente Id=% não encontrado.', v_paciente_id;
    END IF;

    RAISE NOTICE 'Inserindo exames para PacienteId=%, InstituicaoId=%, PostoId=%, MedicoId=%, TabelaExamesId=%, ClasseExamesId=%',
        v_paciente_id, v_instituicao_id, v_posto_id, v_medico_id, v_tabela_exames_id, v_classe_exames_id;

    -- Gerar 20 exames: Dez/2025 (3), Nov/2025 (3), Out/2025 (3), Set/2025 (3),
    --                   Ago/2025 (3), Jul/2025 (2), Jun/2025 (0) = 17... 
    -- Ajuste: 7 meses x 3 = 21, pegar 20
    -- Meses: Dez, Nov, Out, Set, Ago, Jul, Jun de 2025 (dias 5, 15, 25 de cada)
    v_ordem := 0;

    FOR v_mes IN REVERSE 12..6 LOOP
        FOR i IN 1..3 LOOP
            v_ordem := v_ordem + 1;
            EXIT WHEN v_ordem > 20;

            v_dia := v_dias_array[i];
            v_data := make_timestamptz(2025, v_mes, v_dia, 8, 0, 0, 'America/Sao_Paulo');
            v_sequencial := v_sequencial + 1;

            -- 1. Inserir ExamesRealizados
            INSERT INTO "ExamesRealizados" (
                "PacienteId", "TabelaExamesId", "InstituicaoId", "PostoId",
                "MedicoId", "Sequencial", "ControleApoio", "DataIni",
                "Liberacao", "TravaColado", "Baixado", "EnviarEmail", "Situacao", "TotalImpresso"
            ) VALUES (
                v_paciente_id, v_tabela_exames_id, v_instituicao_id, v_posto_id,
                v_medico_id, v_sequencial, '', v_data,
                0, 0, 0, 0, 0, 0
            ) RETURNING "Id" INTO v_exame_id;

            -- 2. Inserir ItensExamesRealizados (1 item de Glicose por exame)
            INSERT INTO "ItensExamesRealizados" (
                "PacienteId", "ClasseExamesId", "ClasseExamesNome",
                "ExameRealizadoId", "TabelaExamesId", "OrdemItem",
                "RefExame", "RefItem", "ContaExame",
                "InstituicaoId", "Sequencial",
                "Descricao", "Resultado", "UnidadeMedida", "Referencia",
                "ValorItem", "Etiquetas", "Liberado", "Baixado",
                "CitoTituloFolha", "CitoTituloExame", "CitoRefItem"
            ) VALUES (
                v_paciente_id, v_classe_exames_id, COALESCE(v_ref_exame, 'BIOQUIMICA'),
                v_exame_id, v_tabela_exames_id, 1,
                COALESCE(v_ref_exame, 'BIOQUIMICA'), 'GLICOSE',
                LPAD(v_classe_exames_id::TEXT, 2, '0') || '.001.0001',
                v_instituicao_id, v_sequencial,
                'Glicose', '95', 'mg/dL', '70 a 99',
                15.00, 0, 0, 0,
                0, 0, 0
            );

            -- 3. Inserir Requisitar (cópia operacional)
            INSERT INTO "Requisitar" (
                "PacienteId", "ClasseExamesId", "ClasseExamesNome",
                "ExameId", "OrdemItem", "RefExame", "RefItem", "ContaExame",
                "InstituicaoId", "PostoId", "TabelaExamesId", "MedicoId",
                "Descricao", "Resultado", "UnidadeMedida", "Referencia",
                "ValorItem", "Etiquetas", "DataIni", "Liberado", "Baixado",
                "ExameRealizadoId"
            ) VALUES (
                v_paciente_id, v_classe_exames_id, COALESCE(v_ref_exame, 'BIOQUIMICA'),
                v_classe_exames_id, 1, COALESCE(v_ref_exame, 'BIOQUIMICA'), 'GLICOSE',
                LPAD(v_classe_exames_id::TEXT, 2, '0') || '.001.0001',
                v_instituicao_id, v_posto_id, v_tabela_exames_id, v_medico_id,
                'Glicose', '95', 'mg/dL', '70 a 99',
                15.00, 0, v_data, 0, 0,
                v_exame_id
            );

            RAISE NOTICE 'Exame % inserido: Id=%, Data=%', v_ordem, v_exame_id, v_data;
        END LOOP;
        EXIT WHEN v_ordem >= 20;
    END LOOP;

    RAISE NOTICE 'Concluído: % exames inseridos para ASDRUBAL TROUXE O TROMBONE.', v_ordem;
END $$;
