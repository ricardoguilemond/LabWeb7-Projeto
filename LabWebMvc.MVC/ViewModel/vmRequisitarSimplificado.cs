namespace LabWebMvc.MVC.ViewModel
{
    public class vmRequisitarSimplificado
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public string? NomePaciente { get; set; }
        public string? Nascimento { get; set; }
        public string? NomeInstituicao { get; set; }
        public string? NomePosto { get; set; }
        public string? NomeTabela { get; set; }
        public string? LaboratorioApoio { get; set; }
        public string? DataIni { get; set; }
        public string? DataEntregaParcial { get; set; }
        //Feito pelo Kiro em 02/05/2026
        // TabelaExamesId necessário para identificar qual requisição editar
        // quando o paciente tem múltiplas requisições no mesmo dia.
        public int TabelaExamesId { get; set; }
        //..Kiro

        //Feito pelo Kiro em 03/05/2026
        // ExameRealizadoId: código do exame (vínculo lógico com ExamesRealizados.Id)
        public int? ExameRealizadoId { get; set; }
        //..Kiro
    }

    public class CupomRequisicaoViewModel
    {
        public int IdPaciente { get; set; }
        public DateTime? Data { get; set; }
        //Feito pelo Kiro em 02/05/2026
        // TabelaExamesId para filtrar o cupom quando o paciente tem
        // múltiplas requisições no mesmo dia com tabelas diferentes.
        public int TabelaExamesId { get; set; }
        //..Kiro

        //Feito pelo Qoder em 31/05/2026
        // ExameRealizadoId: identifica de forma única a sessão (header) de exame.
        // Necessário para excluir/filtrar somente os itens de Requisitar vinculados
        // àquela sessão, evitando apagar outras sessões do mesmo paciente no dia.
        public int? ExameRealizadoId { get; set; }
        //..Qoder
    }

}
