using LabWebMvc.MVC.Models;

namespace LabWebMvc.MVC.ViewModel
{
    public class vmRequisitar
    {
        // Restante das propriedades do modelo
        public int Id { get; set; }

        public int PacienteId { get; set; }
        public int ExameId { get; set; }
        public int ClasseExamesId { get; set; } 
        public int OrdemItem { get; set; }
        public string? RefExame { get; set; }
        public string? RefItem { get; set; }
        public string ContaExame { get; set; } = null!;
        public int InstituicaoId { get; set; }
        public int PostoId { get; set; }
        public bool ValidarPostoId(Db db)
        {
            return PostoId <= 0 || db.Postos.Any(p => p.Id == PostoId);
        }

        public int TabelaExamesId { get; set; }
        public int MedicoId { get; set; }
        public string? LaboratorioApoio { get; set; }
        public string? ControleApoio { get; set; }
        public string? LaboratorioExterno { get; set; }
        public string? MaterialSaida { get; set; }
        public string? MaterialRetorno { get; set; }
        public string? Descricao { get; set; }
        public string? Resultado { get; set; }
        public string? UnidadeMedida { get; set; }
        public string? Referencia { get; set; }
        public decimal? ValorItem { get; set; }
        public byte[]? Laudo { get; set; }
        public int Etiquetas { get; set; }
        public DateTime DataIni { get; set; }
        public DateTime? DataEntregaParcial { get; set; }
        public int Liberado { get; set; }
        public int Baixado { get; set; }

        public virtual Instituicao InstituicaoIdNavigation { get; set; } = null!;
        public virtual Postos PostoIdNavigation { get; set; } = null!;
        public virtual Pacientes PacienteIdNavigation { get; set; } = null!;
        public virtual TabelaExames TabelaExamesIdNavigation { get; set; } = null!;

        /* Variáveis auxiliares */
        public virtual vmGeral vmGeral { get; set; } = null!;

        /* Das VMs auxiliares */
        public virtual vmPacientes VmPacientes { get; set; } = null!;
        public virtual vmInstituicao VmInstituicao { get; set; } = null!;
        public virtual vmPostos VmPostos { get; set; } = null!;
        public virtual vmTabelaExames VmTabelaExames { get; set; } = null!;
        public virtual vmMedicos VmMedicos { get; set; } = null!;
        public virtual vmPlanoExames VmPlanoExames { get; set; } = null!;

        /*
         * Campos auxiliares que não fazem parte da persistência, são apenas informativos/leituras de outras bases (usados em filtros)
         */

        //Da Instituição e do Posto de Coleta
        public virtual string SiglaInstituicao { get; set; } = null!;
        public virtual string NomeInstituicao { get; set; } = null!;
        public virtual string NomePosto { get; set; } = null!;

        //Da Tabela de Exames
        public virtual string SiglaTabela { get; set; } = null!;
        public virtual string NomeTabela { get; set; } = null!;

        //Do Médico
        public virtual string CRM { get; set; } = null!;
        public virtual string NomeMedico { get; set; } = null!;

        //Do Paciente
        public virtual string? CPFPaciente { get; set; }
        public virtual string? IdPacienteExterno { get; set; }
        public virtual string NomePaciente { get; set; } = null!;
        public virtual string? NomeSocial { get; set; }
        public virtual string? NomePai { get; set; }
        public virtual string? NomeMae { get; set; }
        public virtual string Nascimento { get; set; } = null!;
        public virtual int TipoDocumento { get; set; }
        public virtual string? Identidade { get; set; }
        public virtual int Emissor { get; set; }
        public virtual string? CarteiraSUS { get; set; }
        public virtual int EstadoCivil { get; set; }
        public virtual string? Sexo { get; set; }
        public virtual string? Cor { get; set; }
        public virtual string? EtniaIndigena { get; set; }
        public virtual string? TipoSanguineo { get; set; }
        public virtual string? DUM { get; set; }
        public virtual int TempoGestacao { get; set; }
        public virtual string? Profissao { get; set; }
        public virtual string? Naturalidade { get; set; }
        public virtual string? Nacionalidade { get; set; }
        public virtual DateTime? DataEntradaBrasil { get; set; }
        public virtual string? Logradouro { get; set; }
        public virtual string? Endereco { get; set; }
        public virtual string? Numero { get; set; }
        public virtual string? Complemento { get; set; }
        public virtual string? Bairro { get; set; }
        public virtual string? Cidade { get; set; }
        public virtual string? UF { get; set; }
        public virtual string? CEP { get; set; }
        public virtual string? Telefone { get; set; }
        public virtual string? Email { get; set; }
        public virtual string? Observacao { get; set; }
        public int RequisitarId { get; internal set; }

        /*
         * Das Listas de Consultas EXIGIDAS na "_PartialLancarExames" (Requisições dos Exames)
         */
        public ICollection<Pacientes>? ListaPacientes { get; set; }
        public ICollection<Instituicao>? ListaInstituicoes { get; set; }
        public ICollection<Postos>? ListaPostos { get; set; }
        public ICollection<TabelaExames>? ListaTabelas { get; set; }
        public ICollection<Medicos>? ListaMedicos { get; set; }
        public ICollection<PlanoExames>? ListaCupom { get; set; }

    }//Fim
}