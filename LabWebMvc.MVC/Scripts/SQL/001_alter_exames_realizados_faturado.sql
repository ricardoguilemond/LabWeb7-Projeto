-- Migração: Adicionar coluna "Faturado" nas tabelas ExamesRealizados e ExamesRealizadosAM
-- Objetivo: Flag de segurança que impede edição/exclusão de exames já faturados (incluídos no Relatório de Faturamento)
-- Data: 2026

ALTER TABLE "ExamesRealizados" ADD COLUMN "Faturado" BOOLEAN DEFAULT FALSE;
ALTER TABLE "ExamesRealizadosAM" ADD COLUMN "Faturado" BOOLEAN DEFAULT FALSE;
