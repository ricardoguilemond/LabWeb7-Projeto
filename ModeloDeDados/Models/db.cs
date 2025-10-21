using Microsoft.EntityFrameworkCore;
using System.Collections;
using static LabWebMvc.MVC.ExtensionsMethods.EventViewerHelper.EventLogHelper;

namespace LabWebMvc.MVC.Models;

public class Db : DbContext
{
    #region Instruções Importantes e comentários diversos
    /*
     * Fontes do Help:
     * https://docs.microsoft.com/en-us/ef/core/miscellaneous/cli/powershell#scaffold-dbcontext
     * https://www.entityframeworktutorial.net/efcore/fluent-api-in-entity-framework-core.aspx
     * https://docs.microsoft.com/en-us/ef/core/modeling/included-types
     * 
     * Herdando do db_labwebContext que foi gerado AUTOMATICAMENTE, ORIGINALMENTE do Banco de Dados.
     * O original foi construído através da seguinte instrução, via Package Manager Console:
     * (TODAS as tabelas precisam ter uma Primary Key)
     * 
     * Instrução: Scaffold-DbContext
     * Para um help do comando acima, use: PM>      get-help scaffold-dbcontext –detailed
     * 
     * Parâmetros NA ORDEM:
     * 
     * -Connection <String> --> "Server=127.0.0.1;Database=db_labweb;Uid=root;Pwd=root;"
     * -Provider <String> --> MySql.Data.EntityFrameworkCore --> é o provedor de acesso MySQL
     * -OutputDir Models --> informa que o nome da pasta dos modelos é "Models"
     * -ContextDir <String> --> Cria o Context numa pasta separada (senão ele cria dentro da mesma pasta dos modelos)
     * -Schemas <String[]> --> os schemas das tabelas para as quais gerar os tipos de entidade. 
     *                         Se omitido gerará todos.
     * -Tables <String[]> --> gera as tabelas informadas, ou se omitida gerará todas.
     * -DataAnnotations --> uso de atributos para configurar os modelos. 
     *                      Se omitido apenas a API fluente será usada.
     * -UseDatabaseNames --> use para informar os nomes de Tabelas e Colunas exatamente como eles aparecem no Banco de Dados. 
     *                       Se omitido, o C# usará sua convenção de nomes.
     * -f --> sobrescreve arquivos existentes. Força a criação do DbContext/Dataset de acordo com as tabelas existentes.
     * 
     * Instrução completa:
     * Scaffold-DbContext [-Connection] [-Provider] [-OutputDir] [-Context] [-Schemas>] [-Tables>] 
     *                    [-DataAnnotations] [-Force] [-Project] [-StartupProject] [<CommonParameters>]
     * ===============================================================================================================================================================================================================
     * Quando for MySQL:
     * Scaffold-DbContext "Server=127.0.0.1;Database=db_labweb;Uid=root;Pwd=root;" MySql.Data.EntityFrameworkCore -OutputDir Models -f
     * 
     * Scaffold-DbContext "Server=127.0.0.1;Database=db_labweb;Uid=root;Pwd=root;" MySql.Data.EntityFrameworkCore -OutputDir Models -f -Tables Tabela1, Tabela2
     * ===============================================================================================================================================================================================================
     * Quando for SQL SERVER:
     * Irá atualizar o DBContext (db.cs) EXATAMENTE conforme as tabelas que se encontram na base de dados (estrutura, keys, indices, foreign key etc.)
     * Mas, para evitar que mude o nome do "Db" (pois o padrão seria DbContext), vamos ter que executar o comando completo na linha do bash do Nuget...
     * ------------------------------------------------------------------------------------------------------------------------
     * Faça antes uma cópia do \LabWebMvc.MVC\Models\db.cs, pois teremos que recolocar tudo que não for gerado automaticamente.
     * ------------------------------------------------------------------------------------------------------------------------
     * Comando completo: (o uso do "dotnet ef dbcontext scaffold" é a partir do .Net 6)
     * dotnet ef dbcontext scaffold "Server=GUILEMOND-ACER;Database=LABWEB7;User Id=sa;Password=Acer@105" Microsoft.EntityFrameworkCore.SqlServer -o Models --context Db --project LabWebMvc.MVC --force
     * 
     * Se for o caso de ignorar os "Warnings" usar "--no-build"  (atenção: "Errors" não são ignorados):
     * a opção --no-pluralize É importante, pois ela exige que o DbContext crie os nomes dos datasets exatemente como foram criados na Tabelas.
     * a opção --force Vai forçar a recriação de todas as tabelas, mas sem o "force" serão criadas apenas as tabelas novas ou alterada.
     * dotnet ef dbcontext scaffold "Server=GUILEMOND-ACER;Database=LABWEB7;User Id=sa;Password=Acer@105" Microsoft.EntityFrameworkCore.SqlServer -o Models --context Db --project LabWebMvc.MVC --no-build --no-pluralize
     * =============================================================================================================================================================================================================================
     * 
     * IMPORTANTE!!! ATUALIZAR O TOOLS PARA VERSãO FRAMEWORK MAIS RECENTE ----> dotnet tool update --global dotnet-ef
     * ---------------------------------------------------------------------------------------------------------------
     * PARA ADICIONAR O PRIMEIRO MIGRATION
     * Criar uma Migration NOVA:        Add-Migration InitialCreate -Context Db
     * Atualizar no Banco de Dados:     Update-Database -Context Db
     * 
     * Atualizações (exemplo):
     * dotnet ef migrations add MyFirstMigration --project LabWebMvc.MVC --context Db
     * 
     */
    #endregion

    #region Constructors
    public Db() : base(GetOptions())
    { } // Construtor sem parâmetros

    private static DbContextOptions GetOptions()
    {
        return new DbContextOptionsBuilder<Db>()
            .UseSqlServer(GlobalConnectionString.GetConnectionString())
            .Options;
    }

    public Db(DbContextOptions options) : base(options)
    {
    } // Continua funcionando sem especificar o tipo
    #endregion Constructors

    #region Configuração da Conexão
    protected override void OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            string? stringConnection = DatabaseContextFactory.RetornaStringDeConexaoPadrao();

            if (!string.IsNullOrEmpty(stringConnection))
            {
                optionsBuilder.UseSqlServer(stringConnection);
            }
            else
            {
                LogEventViewer("ERRO FATAL: Nenhuma string de conexão encontrada no appsettings.", "wError");
                throw new Exception("ERRO FATAL: Nenhuma string de conexão encontrada no appsettings.");
            }
        }
    }
    #endregion

    #region SaveChanges
    public override int SaveChanges()
    {
        try
        {
            if (this.ChangeTracker.Entries().Count() > 0)
            {
                DeleteOrphans();
                return base.SaveChanges();
            }
            else return 0;

        }
        catch (Exception ex)
        {
            // Retrieve the error messages as a list of strings.
            char[] errorMessages = ex.Message.ToArray();

            // Join the list to a single string.
            string fullErrorMessage = string.Join("; ", errorMessages);

            // Combine the original exception message with the new one.
            string exceptionMessage = string.Concat(ex.Message, " Os erros validados são: ", fullErrorMessage);

            LogEventViewer("db: " + errorMessages + " ::: " + fullErrorMessage + " ::: " + exceptionMessage, "wError");

            return 0;
        }
    }

    public async override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Verifica se há alterações no ChangeTracker antes de prosseguir
            if (this.ChangeTracker.HasChanges())
            {
                // Método que trata órfãos
                DeleteOrphans();

                // Salva as alterações e retorna o número de registros afetados
                return await base.SaveChangesAsync(cancellationToken);
            }

            // Caso não haja alterações, retorna 0
            return 0;
        }
        catch (Exception ex)
        {
            // Log detalhado da exceção
            string exceptionMessage = $"Erro ao salvar alterações: {ex.Message}";

            // Inclui os detalhes da pilha de chamadas (stack trace) para depuração
            LogEventViewer($"db: {ex.Message}; StackTrace: {ex.StackTrace}", "wError");

            // Opcional: Re-throw da exceção caso você queira lidar com ela em um nível superior
            throw new Exception(exceptionMessage, ex);
        }
    }

    /* 
     *  Deleta órfãos que foram perdidos de forma não registrada por suas Entidades principais
     *  USO:
     *  DeleteOrphans<Senhas, UsuariosWeb>(db.Senhas, db.UsuariosWeb, child => child.SenhaId);
     *  
     */
    public void DeleteOrphans()  //se o método abaixo tiver dando algum erro não solucionável, então voltar a usar este constructor vazio até uma solução!
    { }

    public void DeleteOrphans2()
    {
        //Obtém as entidades monitoradas pelo ChangeTracker e que estão 
        //no estado de Deleted ou Modified
        var trackedEntries = this.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Deleted || e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in trackedEntries)
        {
            //Obtém o tipo da entidade pai e cria uma coleção
            var entityType = entry.Entity.GetType();
            var navigationProperties = this.Model.FindEntityType(entityType).GetNavigations();

            foreach (var navigation in navigationProperties)
            {
                //Verifica se a propriedade é uma coleção (IENumerable) 
                //Mas exclui "string" que é um tipo especial que também implementa IENumerable.
                if (typeof(IEnumerable).IsAssignableFrom(navigation.ClrType) && navigation.ClrType != typeof(string))
                {
                    var relatedType = navigation.ClrType; //Identifica o tipo da entidade filha
                    var foreignKey = navigation.ForeignKey.Properties.First(); //Chave estrangeira na entidade filha

                    //Usa reflection para acessar o DbSet da entidade filha
                    var dbSet = typeof(DbContext).GetMethod("Set")
                        ?.MakeGenericMethod(relatedType)
                        .Invoke(this, null);

                    //Converte dbSet para dynamic para acessar o Local
                    var localSet = dbSet as dynamic;

                    if (localSet == null)
                    {
                        LogEventViewer("Não foi possível acessar o DbSet para o tipo relacionado ::: " + entry.ToString() ?? "", "wWarning");
                        //throw new InvalidOperationException("Não foi possível acessar o DbSet para o tipo relacionado.");
                        return;  //apenas sai fora
                    }
                    // Identifica registros órfãos rastreados no contexto
                    var orphans = ((IEnumerable<object>)localSet.Local)
                    .Where(child =>
                    {
                        var childProperty = child.GetType().GetProperty(foreignKey.Name);
                        if (childProperty == null)
                            return false; //Ignora se a propriedade não for encontrada

                        var propertyValue = childProperty.GetValue(child); //Valor na entidade filha
                        var parentValue = entry.CurrentValues[foreignKey.Name]; //Valor na entidade pai

                        //Retorna se o registro é órfão, montando uma lista "ToList()"
                        return propertyValue == null || !propertyValue.Equals(parentValue);
                    })
                    .ToList();

                    // Remove os registros órfãos
                    if (orphans.Any())
                    {
                        localSet.RemoveRange(orphans);
                    }
                }
            }
        }
    }


    /* Exemplo de uso do método abaixo (passando parâmetros obrigatórios):
     * DeleteOrphans(db.Senhas,                // DbSet da entidade pai
                     db.UsuariosWeb,           // DbSet da entidade filha
                     child => child.SenhaId    // Selecionador da chave estrangeira na entidade filha
                     );
     *     
     */
    public void DeleteOrphans<TParent, TChild>(DbSet<TParent> parentSet,
                                               DbSet<TChild> childSet,
                                               Func<TChild, object> parentKeySelector)
                                               where TParent : class
                                               where TChild : class
    {
        // Obter todas as chaves dos registros pais existentes
        var parentKeys = parentSet
            .Select(e => EF.Property<object>(e, "Id")) // Assume "Id" como chave primária
            .ToHashSet();

        // Identificar os registros órfãos nos filhos
        var orphans = childSet
            .Where(child => !parentKeys.Contains(parentKeySelector(child)))
            .ToList();

        // Excluir os registros órfãos
        if (orphans.Any())
        {
            childSet.RemoveRange(orphans);
        }
    }
    #endregion

    /* 
     * Do Modelo de Dados do Repositório
     */

    #region Modelo DbSet
    public virtual DbSet<Assinaturas> Assinaturas { get; set; }

    public virtual DbSet<ClasseExames> ClasseExames { get; set; }

    public virtual DbSet<ControleConcorrencia> ControleConcorrencia { get; set; }

    public virtual DbSet<ControleDeAcesso> ControleDeAcesso { get; set; }

    public virtual DbSet<ControleDePerfil> ControleDePerfil { get; set; }

    public virtual DbSet<ControleDePerfilMenu> ControleDePerfilMenu { get; set; }

    public virtual DbSet<ControleDePerfilModelo> ControleDePerfilModelo { get; set; }

    public virtual DbSet<ControleDePerfilTipo> ControleDePerfilTipo { get; set; }

    public virtual DbSet<Cor> Cor { get; set; }

    public virtual DbSet<Empresa> Empresa { get; set; }

    public virtual DbSet<ERTemporario> ERTemporario { get; set; }

    public virtual DbSet<EstadoCivil> EstadoCivil { get; set; }

    public virtual DbSet<ExamesExportados> ExamesExportados { get; set; }

    public virtual DbSet<ExamesImpressos> ExamesImpressos { get; set; }

    public virtual DbSet<ExamesPendentes> ExamesPendentes { get; set; }

    public virtual DbSet<ExamesRealizados> ExamesRealizados { get; set; }

    public virtual DbSet<ExamesRealizadosAM> ExamesRealizadosAM { get; set; }

    public virtual DbSet<FichasInternas> FichasInternas { get; set; }

    public virtual DbSet<FichasLotes> FichasLotes { get; set; }

    public virtual DbSet<FichasPlanilhas> FichasPlanilhas { get; set; }

    public virtual DbSet<Instituicao> Instituicao { get; set; }

    public virtual DbSet<IntegracaoDadosArmazenamento> IntegracaoDadosArmazenamento { get; set; }

    public virtual DbSet<IntegracaoDadosConfiguracao> IntegracaoDadosConfiguracao { get; set; }

    public virtual DbSet<IntegracaoDadosExecucao> IntegracaoDadosExecucao { get; set; }

    public virtual DbSet<IntegracaoDadosExecucaoArquivo> IntegracaoDadosExecucaoArquivo { get; set; }

    public virtual DbSet<IntegracaoDadosLayout> IntegracaoDadosLayout { get; set; }

    public virtual DbSet<IntegracaoDadosPeriodicidade> IntegracaoDadosPeriodicidade { get; set; }

    public virtual DbSet<ItensExamesRealizados> ItensExamesRealizados { get; set; }

    public virtual DbSet<ItensExamesRealizadosAM> ItensExamesRealizadosAM { get; set; }

    public virtual DbSet<LogArquivos> LogArquivos { get; set; }

    public virtual DbSet<Logradouro> Logradouro { get; set; }

    public virtual DbSet<Medicos> Medicos { get; set; }

    public virtual DbSet<MemoAuxiliar> MemoAuxiliar { get; set; }

    public virtual DbSet<Pacientes> Pacientes { get; set; }

    public virtual DbSet<PlanoExames> PlanoExames { get; set; }

    public virtual DbSet<Postos> Postos { get; set; }

    public virtual DbSet<Rastreamentos> Rastreamentos { get; set; }

    public virtual DbSet<Requisitar> Requisitar { get; set; }

    public virtual DbSet<Senhas> Senhas { get; set; }

    public virtual DbSet<Sexo> Sexo { get; set; }

    public virtual DbSet<SituacaoExames> SituacaoExames { get; set; }

    public virtual DbSet<TabelaExames> TabelaExames { get; set; }

    public virtual DbSet<TextosProntos> TextosProntos { get; set; }

    public virtual DbSet<TipoSanguineo> TipoSanguineo { get; set; }

    public virtual DbSet<TituloExames> TituloExames { get; set; }

    public virtual DbSet<UF> UF { get; set; }

    public virtual DbSet<UsuariosWeb> UsuariosWeb { get; set; }
    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assinaturas>(entity =>
        {
            entity.ToTable("Assinaturas");
            entity.HasKey(e => e.Id).HasName("iAssinaturas1");

            entity.Property(e => e.Crbio1)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasDefaultValue("123456789")
                .HasColumnName("CRBio1");
            entity.Property(e => e.Crbio2)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("CRBio2");
            entity.Property(e => e.Crbio3)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("CRBio3");
            entity.Property(e => e.Crbio4)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("CRBio4");
        });


        modelBuilder.Entity<ClasseExames>(entity =>
        {
            entity.ToTable("ClasseExames");
            entity.HasKey(e => e.Id).HasName("iClasseExames1");

            entity.Property(e => e.LaboratorioExterno)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MHI).HasColumnName("MHI");
            entity.Property(e => e.NomeAss1)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NomeAss2)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NomeAss3)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NomeAss4)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RefExame)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TipoMapa)
                .HasMaxLength(1)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ControleConcorrencia>(entity =>
        {
            entity.ToTable("ControleConcorrencia");
            entity.HasKey(e => e.Processo).HasName("iControleConcorrencia1");

            entity.Property(e => e.Processo)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.DataHora).HasColumnType("datetime");
        });

        modelBuilder.Entity<ControleDeAcesso>(entity =>
        {
            entity.ToTable("ControleDeAcesso");
            entity.HasKey(e => e.Id).HasName("iControleDeAcesso1");

            entity.HasIndex(e => e.SenhaId, "iControleDeAcesso2").IsUnique();
        });

        modelBuilder.Entity<ControleDePerfil>(entity =>
        {
            entity.ToTable("ControleDePerfil");
            entity.HasKey(e => e.Id).HasName("iControleDePerfil1");

            entity.HasIndex(e => new { e.MenuNivelMenu, e.MenuNivel1, e.MenuNivel2, e.MenuNivel3, e.MenuNivel4 }, "iControleDePerfil3");

            entity.Property(e => e.MenuNivel1)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.MenuNivel2)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.MenuNivel3)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.MenuNivel4)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.MenuNivelMenu)
                .HasMaxLength(3)
                .IsUnicode(false);

            entity.HasOne(d => d.ControleDeAcesso).WithMany(p => p.ControleDePerfil)
                .HasForeignKey(d => d.ControleDeAcessoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iControleDePerfil2");
        });

        modelBuilder.Entity<ControleDePerfilMenu>(entity =>
        {
            entity.ToTable("ControleDePerfilMenu");
            entity.HasKey(e => e.Id).HasName("iControleDePerfilMenu1");

            entity.HasIndex(e => new { e.Coluna, e.Nivel }, "iControleDePerfilMenu2");

            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Area)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Controller)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Menu)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Nivel)
                .HasMaxLength(3)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ControleDePerfilModelo>(entity =>
        {
            entity.ToTable("ControleDePerfilModelo");
            entity.HasKey(e => e.Id).HasName("iControleDePerfilModelo1");

            entity.HasIndex(e => new { e.MenuNivel1, e.MenuNivel2, e.MenuNivel3, e.MenuNivel4, e.MenuNivel5 }, "iControleDePerfilModelo2");

            entity.Property(e => e.MenuNivel1)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.MenuNivel2)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.MenuNivel3)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.MenuNivel4)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.MenuNivel5)
                .HasMaxLength(3)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ControleDePerfilTipo>(entity =>
        {
            entity.ToTable("ControleDePerfilTipo");
            entity.HasKey(e => e.Id).HasName("iControleDePerfilTipo1");

            entity.Property(e => e.Tipo)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Cor>(entity =>
        {
            entity.ToTable("Cor");
            entity.HasKey(e => e.Id).HasName("iCor1");

            entity.HasIndex(e => e.Cor1, "iCor2").IsUnique();

            entity.Property(e => e.Cor1)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("Cor");
        });

        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.ToTable("Empresa");
            entity.HasKey(e => e.Id).HasName("iEmpresa1");

            entity.HasIndex(e => new { e.Sigla, e.Matriz, e.Filial }, "iEmpresa2").IsUnique();

            entity.Property(e => e.Bairro)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.CaminhoLogoMarca)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.CEP)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("CEP");
            entity.Property(e => e.Cidade)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.CNPJ)
                .HasMaxLength(14)
                .IsUnicode(false)
                .HasColumnName("CNPJ");
            entity.Property(e => e.Complemento)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.DataCadastro).HasColumnType("datetime");
            entity.Property(e => e.DataExpira).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Endereco)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.HostLogoMarca)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.Logradouro)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.Matriz).HasDefaultValue(1);
            entity.Property(e => e.NomeFantasia)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NomeLogoMarca)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Numero)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.PopName)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PopPassword)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PopPortSsl).HasColumnName("PopPortSSL");
            entity.Property(e => e.PopRequerSsl).HasColumnName("PopRequerSSL");
            entity.Property(e => e.PopServer)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PopUsername)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RazaoSocial)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Rodape)
                .HasMaxLength(140)
                .IsUnicode(false);
            entity.Property(e => e.Sigla)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SiteUrl)
                .HasMaxLength(2000)
                .IsUnicode(false)
                .HasColumnName("SiteURL");
            entity.Property(e => e.SmtpName)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.SmtpPassword)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.SmtpPortSsl).HasColumnName("SmtpPortSSL");
            entity.Property(e => e.SmtpPortTls).HasColumnName("SmtpPortTLS");
            entity.Property(e => e.SmtpRequerSsl).HasColumnName("SmtpRequerSSL");
            entity.Property(e => e.SmtpRequerTls).HasColumnName("SmtpRequerTLS");
            entity.Property(e => e.SmtpSenhaApp)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.SmtpServer)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.SmtpUsername)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.StringConexao)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.SubTituloEmpresa)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Telefones)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TituloEmpresa)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UF)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("UF");
            entity.Property(e => e.UnidadeLogoMarca)
                .HasMaxLength(2)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ERTemporario>(entity =>
        {
            entity.ToTable("ERTemporario");
            entity.HasKey(e => e.Id).HasName("iERTemporario1");

            entity.Property(e => e.DataEntrega).HasColumnType("datetime");
            entity.Property(e => e.DataExame).HasColumnType("datetime");
            entity.Property(e => e.DataFim).HasColumnType("datetime");
            entity.Property(e => e.DataIni).HasColumnType("datetime");
            entity.Property(e => e.HistoricoClinico)
                .HasMaxLength(2000)
                .IsUnicode(false);
        });

        modelBuilder.Entity<EstadoCivil>(entity =>
        {
            entity.ToTable("EstadoCivil");
            entity.HasKey(e => e.Id).HasName("iEstadoCivil1");

            entity.Property(e => e.Descricao)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ExamesExportados>(entity =>
        {
            entity.ToTable("ExamesExportados");
            entity.HasKey(e => e.Id).HasName("iExamesExportados1");

            entity.Property(e => e.ControleApoio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DataColeta).HasColumnType("datetime");
            entity.Property(e => e.DataExportado).HasColumnType("datetime");
            entity.Property(e => e.DataImportado).HasColumnType("datetime");
            entity.Property(e => e.LaboratorioApoio)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.ExamesRealizados).WithMany(p => p.ExamesExportados)
                .HasForeignKey(d => d.ExameId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesExportados_ExamesRealizados");

            entity.HasOne(d => d.Instituicao).WithMany(p => p.ExamesExportados)
                .HasForeignKey(d => d.InstituicaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesExportados_Instituicao");

            entity.HasOne(d => d.Medicos).WithMany(p => p.ExamesExportados)
                .HasForeignKey(d => d.MedicoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesExportados_Medicos");

            entity.HasOne(d => d.Pacientes).WithMany(p => p.ExamesExportados)
                .HasForeignKey(d => d.PacienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesExportados_Pacientes");

            entity.HasOne(d => d.TabelaExames).WithMany(p => p.ExamesExportados)
                .HasForeignKey(d => d.TabelaExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesExportados_TabelaExames");
        });

        modelBuilder.Entity<ExamesImpressos>(entity =>
        {
            entity.ToTable("ExamesImpressos");
            entity.HasKey(e => e.Id).HasName("iExamesImpressos1");

            entity.Property(e => e.DataExame).HasColumnType("datetime");
            entity.Property(e => e.DataImpresso).HasColumnType("datetime");

            entity.HasOne(d => d.Instituicao).WithMany(p => p.ExamesImpressos)
                .HasForeignKey(d => d.InstituicaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesImpressos_Instituicao");

            entity.HasOne(d => d.Pacientes).WithMany(p => p.ExamesImpressos)
                .HasForeignKey(d => d.PacienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesImpressos_Pacientes");

            entity.HasOne(d => d.TabelaExames).WithMany(p => p.ExamesImpressos)
                .HasForeignKey(d => d.TabelaExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesImpressos_TabelaExames");
        });

        modelBuilder.Entity<ExamesPendentes>(entity =>
        {
            entity.ToTable("ExamesPendentes");
            entity.HasKey(e => e.Id).HasName("iExamesPendentes1");

            entity.Property(e => e.ContaExame)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.ControleApoio)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.DataIni).HasColumnType("datetime");
            entity.Property(e => e.LaboratorioApoio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NomeFolha)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NomeGrupo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NomeItem)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.ClasseExames).WithMany(p => p.ExamesPendentes)
                .HasForeignKey(d => d.ClasseExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesPendentes_ClasseExames");

            entity.HasOne(d => d.Instituicao).WithMany(p => p.ExamesPendentes)
                .HasForeignKey(d => d.InstituicaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesPendentes_Instituicao");

            entity.HasOne(d => d.Medicos).WithMany(p => p.ExamesPendentes)
                .HasForeignKey(d => d.MedicoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesPendentes_Medicos");

            entity.HasOne(d => d.Pacientes).WithMany(p => p.ExamesPendentes)
                .HasForeignKey(d => d.PacienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesPendentes_Pacientes");

            entity.HasOne(d => d.TabelaExames).WithMany(p => p.ExamesPendentes)
                .HasForeignKey(d => d.TabelaExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesPendentes_TabelaExames");
        });

        modelBuilder.Entity<ExamesRealizados>(entity =>
        {
            entity.ToTable("ExamesRealizados");
            entity.HasKey(e => e.Id).HasName("iExamesRealizados1");

            entity.Property(e => e.ControleApoio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DataColeta)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.DataEntrega).HasColumnType("datetime");
            entity.Property(e => e.DataExame).HasColumnType("datetime");
            entity.Property(e => e.DataFim).HasColumnType("datetime");
            entity.Property(e => e.DataIni).HasColumnType("datetime");
            entity.Property(e => e.ExameColado)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ExameColadoImagens)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.HistoricoClinico)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.LaboratorioApoio)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.ClasseExames).WithMany(p => p.ExamesRealizados)
                .HasForeignKey(d => d.ClasseExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesRealizados_ClasseExames");

            entity.HasOne(d => d.Instituicao).WithMany(p => p.ExamesRealizados)
                .HasForeignKey(d => d.InstituicaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesRealizados_Instituicao");

            entity.HasOne(d => d.Medicos).WithMany(p => p.ExamesRealizados)
                .HasForeignKey(d => d.MedicoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesRealizados_Medicos");

            entity.HasOne(d => d.Pacientes).WithMany(p => p.ExamesRealizados)
                .HasForeignKey(d => d.PacienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesRealizados_Pacientes");

            entity.HasOne(d => d.Postos).WithMany(p => p.ExamesRealizados)
                .HasForeignKey(d => d.PostoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesRealizados_Postos");

            entity.HasOne(d => d.TabelaExames).WithMany(p => p.ExamesRealizados)
                .HasForeignKey(d => d.TabelaExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesRealizados_TabelaExames");
        });

        modelBuilder.Entity<ExamesRealizadosAM>(entity =>
        {
            entity.ToTable("ExamesRealizadosAM");
            entity.HasKey(e => e.Id).HasName("iExamesRealizadosAM1");

            entity.Property(e => e.ControleApoio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DataColeta)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.DataEntrega).HasColumnType("datetime");
            entity.Property(e => e.DataExame).HasColumnType("datetime");
            entity.Property(e => e.DataFim).HasColumnType("datetime");
            entity.Property(e => e.DataIni).HasColumnType("datetime");
            entity.Property(e => e.ExameColado)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.ExameColadoImagens)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.HistoricoClinico)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.LaboratorioApoio)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.ClasseExames).WithMany(p => p.ExamesRealizadosAM)
                .HasForeignKey(d => d.ClasseExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesRealizadosAM_ClasseExames");

            entity.HasOne(d => d.Instituicao).WithMany(p => p.ExamesRealizadosAM)
                .HasForeignKey(d => d.InstituicaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesRealizadosAM_Instituicao");

            entity.HasOne(d => d.Medicos).WithMany(p => p.ExamesRealizadosAM)
                .HasForeignKey(d => d.MedicoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesRealizadosAM_Medicos");

            entity.HasOne(d => d.Pacientes).WithMany(p => p.ExamesRealizadosAM)
                .HasForeignKey(d => d.PacienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesRealizadosAM_Pacientes");

            entity.HasOne(d => d.Postos).WithMany(p => p.ExamesRealizadosAM)
                .HasForeignKey(d => d.PostoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesRealizadosAM_Postos");

            entity.HasOne(d => d.TabelaExames).WithMany(p => p.ExamesRealizadosAM)
                .HasForeignKey(d => d.TabelaExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iExamesRealizadosAM_TabelaExames");
        });

        modelBuilder.Entity<FichasInternas>(entity =>
        {
            entity.ToTable("FichasInternas");
            entity.HasKey(e => e.Id).HasName("iFichasInternas1");

            entity.Property(e => e.Coluna1)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna10)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna11)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna12)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna13)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna14)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna15)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna16)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna17)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna18)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna2)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna3)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna4)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna5)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna6)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna7)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna8)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.Coluna9)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.ContaExame)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.ControleApoio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DataExame).HasColumnType("datetime");
            entity.Property(e => e.DataFim).HasColumnType("datetime");
            entity.Property(e => e.DataIni).HasColumnType("datetime");
            entity.Property(e => e.Descricao)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.HistoricoClinico)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.MapaHorizontal)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.NomeFicha)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Resultado)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.ExamesRealizados).WithMany(p => p.FichasInternas)
                .HasForeignKey(d => d.ExamesRealizadosId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iFichasInternas_ExamesRealizados");

            entity.HasOne(d => d.Instituicao).WithMany(p => p.FichasInternas)
                .HasForeignKey(d => d.InstituicaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iFichasInternas_Instituicao");

            entity.HasOne(d => d.Medicos).WithMany(p => p.FichasInternas)
                .HasForeignKey(d => d.MedicoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iFichasInternas_Medicos");

            entity.HasOne(d => d.Pacientes).WithMany(p => p.FichasInternas)
                .HasForeignKey(d => d.PacienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iFichasInternas_Pacientes");
        });

        modelBuilder.Entity<FichasLotes>(entity =>
        {
            entity.ToTable("FichasLotes");
            entity.HasKey(e => e.Id).HasName("iFichasLotes1");

            entity.Property(e => e.ContaExame)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.ControleApoio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DataExame).HasColumnType("datetime");
            entity.Property(e => e.DataFim).HasColumnType("datetime");
            entity.Property(e => e.DataIni).HasColumnType("datetime");
            entity.Property(e => e.Descricao)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.HistoricoClinico)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.LiberadoExclusao)
                .HasMaxLength(1)
                .IsUnicode(false);
            entity.Property(e => e.MapaHorizontal)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.NomeFicha)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Resultado)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.ExamesRealizados).WithMany(p => p.FichasLotes)
                .HasForeignKey(d => d.ExamesRealizadosId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iFichasLotes_ExamesRealizados");

            entity.HasOne(d => d.Instituicao).WithMany(p => p.FichasLotes)
                .HasForeignKey(d => d.InstituicaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iFichasLotes_Instituicao");

            entity.HasOne(d => d.Medicos).WithMany(p => p.FichasLotes)
                .HasForeignKey(d => d.MedicoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iFichasLotes_Medicos");

            entity.HasOne(d => d.Pacientes).WithMany(p => p.FichasLotes)
                .HasForeignKey(d => d.PacienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iFichasLotes_Pacientes");

            entity.HasOne(d => d.TabelaExames).WithMany(p => p.FichasLotes)
                .HasForeignKey(d => d.TabelaExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iFichasLotes_TabelaExames");
        });

        modelBuilder.Entity<FichasPlanilhas>(entity =>
        {
            entity.ToTable("FichasPlanilhas");
            entity.HasKey(e => e.Id).HasName("iFichasPlanilhas1");

            entity.Property(e => e.ContaExame)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.ControleApoio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DataExame).HasColumnType("datetime");
            entity.Property(e => e.DataFim).HasColumnType("datetime");
            entity.Property(e => e.DataIni).HasColumnType("datetime");
            entity.Property(e => e.Descricao)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.HistoricoClinico)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.LiberadoExclusao)
                .HasMaxLength(1)
                .IsUnicode(false);
            entity.Property(e => e.MapaHorizontal)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.NomeFicha)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Resultado)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.ExamesRealizados).WithMany(p => p.FichasPlanilhas)
                .HasForeignKey(d => d.ExamesRealizadosId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iFichasPlanilhas_ExamesRealizados");

            entity.HasOne(d => d.Instituicao).WithMany(p => p.FichasPlanilhas)
                .HasForeignKey(d => d.InstituicaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iFichasPlanilhas_Instituicao");

            entity.HasOne(d => d.Medicos).WithMany(p => p.FichasPlanilhas)
                .HasForeignKey(d => d.MedicoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iFichasPlanilhas_Medicos");

            entity.HasOne(d => d.Pacientes).WithMany(p => p.FichasPlanilhas)
                .HasForeignKey(d => d.PacienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iFichasPlanilhas_Pacientes");

            entity.HasOne(d => d.TabelaExames).WithMany(p => p.FichasPlanilhas)
                .HasForeignKey(d => d.TabelaExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iFichasPlanilhas_TabelaExames");
        });

        modelBuilder.Entity<Instituicao>(entity =>
        {
            entity.ToTable("Instituicao");
            entity.HasKey(e => e.Id).HasName("iInstituicao1");

            entity.HasIndex(e => e.Sigla, "iInstituicao2").IsUnique();

            entity.Property(e => e.AvisoRodape1)
                .HasMaxLength(140)
                .IsUnicode(false);
            entity.Property(e => e.AvisoRodape2)
                .HasMaxLength(140)
                .IsUnicode(false);
            entity.Property(e => e.Bairro)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.CarimboSN).HasColumnName("CarimboSN");
            entity.Property(e => e.Celular)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.CEP)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("CEP");
            entity.Property(e => e.Cidade)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.CNPJ)
                .HasMaxLength(14)
                .IsUnicode(false)
                .HasColumnName("CNPJ");
            entity.Property(e => e.Complemento)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.Contato)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Endereco)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Logradouro)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.Nome)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NomeLogomarca)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.NomeTimbre)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Numero)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Propaganda).HasDefaultValueSql("('')");
            entity.Property(e => e.Sigla)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SubTituloTimbre)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.Telefone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TimbreSN).HasColumnName("TimbreSN");
            entity.Property(e => e.TituloTimbre)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.UF)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("UF");
            entity.Property(e => e.UsuarioCaminhoFTP)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("UsuarioCaminhoFTP");
            entity.Property(e => e.UsuarioEmailFTP)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("UsuarioEmailFTP");
            entity.Property(e => e.UsuarioPortaFTP).HasColumnName("UsuarioPortaFTP");
            entity.Property(e => e.UsuarioSenhaFTP)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("UsuarioSenhaFTP");
            entity.Property(e => e.ValorExameCitologia).HasColumnType("decimal(18, 4)");
        });

        modelBuilder.Entity<IntegracaoDadosArmazenamento>(entity =>
        {
            entity.ToTable("IntegracaoDadosArmazenamento");
            entity.HasKey(e => e.Id).HasName("iIntegracaoDadosArmazenamento1");

            entity.Property(e => e.Host)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Senha)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UsuarioLogin)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<IntegracaoDadosConfiguracao>(entity =>
        {
            entity.ToTable("IntegracaoDadosConfiguracao");
            entity.HasKey(e => e.Id).HasName("iIntegracaoDadosConfiguracao1");

            entity.Property(e => e.HoraEncerramento)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.HoraExecucao)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.IntegraUmaUnicaVezNoDia).HasDefaultValue(true);
            entity.Property(e => e.NomeArquivo)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.PastaEntrada)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PastaEntradaProcessado)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PastaEntradaProcessadoErro)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PastaEntradaProcessadoParcial)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PastaSaida)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.PausaDoEventoEmMinutos).HasDefaultValue(1);
            entity.Property(e => e.Periodicidade).HasDefaultValue(1);

            entity.HasOne(d => d.IntegracaoDadosArmazenamento).WithMany(p => p.IntegracaoDadosConfiguracao)
                .HasForeignKey(d => d.IntegracaoDadosArmazenamentoId)
                .HasConstraintName("iIntegracaoDadosConfiguracao2");
        });

        modelBuilder.Entity<IntegracaoDadosExecucao>(entity =>
        {
            entity.ToTable("IntegracaoDadosExecucao");
            entity.HasKey(e => e.Id).HasName("iIntegracaoDadosExecucao1");

            entity.Property(e => e.Header)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.Inicio).HasColumnType("datetime");
            entity.Property(e => e.NomeArquivo)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.NomeServico)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Resumo)
                .HasMaxLength(4000)
                .IsUnicode(false);
            entity.Property(e => e.Summary)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.Termino).HasColumnType("datetime");

            entity.HasOne(d => d.IntegracaoDadosLayout).WithMany(p => p.IntegracaoDadosExecucao)
                .HasForeignKey(d => d.IntegracaoDadosLayoutId)
                .HasConstraintName("iIntegracaoDadosExecucao2");
        });

        modelBuilder.Entity<IntegracaoDadosExecucaoArquivo>(entity =>
        {
            entity.ToTable("IntegracaoDadosExecucaoArquivo");
            entity.HasKey(e => e.Id).HasName("iIntegracaoDadosExecucaoArquivo1");

            entity.Property(e => e.NomeArquivo)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.NomeArquivoGerado)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.NomeArquivoProcessado)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Resumo)
                .HasMaxLength(4000)
                .IsUnicode(false);

            entity.HasOne(d => d.IntegracaoDadosExecucao).WithMany(p => p.IntegracaoDadosExecucaoArquivo)
                .HasForeignKey(d => d.IntegracaoDadosExecucaoId)
                .HasConstraintName("iIntegracaoDadosExecucaoArquivo2");
        });

        modelBuilder.Entity<IntegracaoDadosLayout>(entity =>
        {
            entity.ToTable("IntegracaoDadosLayout");
            entity.HasKey(e => e.Id).HasName("iIntegracaoDadosLayout1");

            entity.Property(e => e.DataFinal).HasColumnType("datetime");
            entity.Property(e => e.DataInicial).HasColumnType("datetime");
            entity.Property(e => e.Descricao)
                .HasMaxLength(60)
                .IsUnicode(false);

            entity.HasOne(d => d.IntegracaoDadosConfiguracao).WithMany(p => p.IntegracaoDadosLayout)
                .HasForeignKey(d => d.IntegracaoDadosConfiguracaoId)
                .HasConstraintName("iIntegracaoDadosLayout2");
        });

        modelBuilder.Entity<IntegracaoDadosPeriodicidade>(entity =>
        {
            entity.ToTable("IntegracaoDadosPeriodicidade");
            entity.HasKey(e => e.Id).HasName("iIntegracaoDadosPeriodicidade1");

            entity.Property(e => e.TipoPeriodoExtracao)
                .HasMaxLength(12)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ItensExamesRealizados>(entity =>
        {
            entity.ToTable("ItensExamesRealizados");
            entity.HasKey(e => e.Id).HasName("iItensExamesRealizados1");

            entity.Property(e => e.CitoDescricao)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.ClasseExamesNome)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ContaExame)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.ControleApoio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DataEntregaParcial).HasColumnType("datetime");
            entity.Property(e => e.Descricao)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LaboratorioApoio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LaboratorioExterno)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MaterialRetorno)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.MaterialSaida)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.RefExame)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RefItem)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Referencia)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.Resultado)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.UnidadeMedida)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ValorItem).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.ClasseExames).WithMany(p => p.ItensExamesRealizados)
                .HasForeignKey(d => d.ClasseExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iItensExamesRealizados_ClasseExames");

            entity.HasOne(d => d.ExamesRealizados).WithMany(p => p.ItensExamesRealizados)
                .HasForeignKey(d => d.ExameRealizadoId)
                .HasConstraintName("iItensExamesRealizados_ExamesRealizados");

            entity.HasOne(d => d.Instituicao).WithMany(p => p.ItensExamesRealizados)
                .HasForeignKey(d => d.InstituicaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iItensExamesRealizados_Instituicao");

            entity.HasOne(d => d.Pacientes).WithMany(p => p.ItensExamesRealizados)
                .HasForeignKey(d => d.PacienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iItensExamesRealizados_Pacientes");

            entity.HasOne(d => d.TabelaExames).WithMany(p => p.ItensExamesRealizados)
                .HasForeignKey(d => d.TabelaExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iItensExamesRealizados_TabelaExames");
        });

        modelBuilder.Entity<ItensExamesRealizadosAM>(entity =>
        {
            entity.ToTable("ItensExamesRealizadosAM");
            entity.HasKey(e => e.Id).HasName("iItensExamesRealizadosAM1");

            entity.Property(e => e.CitoDescricao)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.ClasseExamesNome)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ContaExame)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.ControleApoio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DataEntregaParcial).HasColumnType("datetime");
            entity.Property(e => e.Descricao)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ExameRealizadoAMId).HasColumnName("ExameRealizadoAMId");
            entity.Property(e => e.LaboratorioApoio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LaboratorioExterno)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MaterialRetorno)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.MaterialSaida)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.OrigemAmid).HasColumnName("OrigemAMId");
            entity.Property(e => e.RefExame)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RefItem)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Referencia)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.Resultado)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.UnidadeMedida)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ValorItem).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.ClasseExames).WithMany(p => p.ItensExamesRealizadosAM)
                .HasForeignKey(d => d.ClasseExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iItensExamesRealizadosAM1_ClasseExames");

            entity.HasOne(d => d.ExamesRealizadosAM).WithMany(p => p.ItensExamesRealizadosAM)
                .HasForeignKey(d => d.ExameRealizadoAMId)
                .HasConstraintName("iItensExamesRealizadosAM1_ExamesRealizados");

            entity.HasOne(d => d.Instituicao).WithMany(p => p.ItensExamesRealizadosAM)
                .HasForeignKey(d => d.InstituicaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iItensExamesRealizadosAM1_Instituicao");

            entity.HasOne(d => d.Pacientes).WithMany(p => p.ItensExamesRealizadosAM)
                .HasForeignKey(d => d.PacienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iItensExamesRealizadosAM1_Pacientes");

            entity.HasOne(d => d.TabelaExames).WithMany(p => p.ItensExamesRealizadosAM)
                .HasForeignKey(d => d.TabelaExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iItensExamesRealizadosAM1_TabelaExames");
        });

        modelBuilder.Entity<LogArquivos>(entity =>
        {
            entity.ToTable("LogArquivos");
            entity.HasKey(e => e.Id).HasName("iLogArquivos1");

            entity.Property(e => e.Data).HasColumnType("datetime");
            entity.Property(e => e.DataPeriodoFinal).HasColumnType("datetime");
            entity.Property(e => e.DataPeriodoInicial).HasColumnType("datetime");
            entity.Property(e => e.NomeArquivo)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.StrRef)
                .HasMaxLength(250)
                .IsUnicode(false);

            entity.HasOne(d => d.IntegracaoDadosLayout).WithMany(p => p.LogArquivos)
                .HasForeignKey(d => d.IntegracaoDadosLayoutId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("iLogArquivos2");
        });

        modelBuilder.Entity<Logradouro>(entity =>
        {
            entity.ToTable("Logradouro");
            entity.HasKey(e => e.Id).HasName("iLogradouro1");

            entity.Property(e => e.Descricao)
                .HasMaxLength(8)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Medicos>(entity =>
        {
            entity.ToTable("Medicos");
            entity.HasKey(e => e.Id).HasName("iMedicos1");

            entity.Property(e => e.CRM)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("CRM");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Especialidade)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NomeMedico)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Telefone)
                .HasMaxLength(15)
                .IsUnicode(false);
        });

        modelBuilder.Entity<MemoAuxiliar>(entity =>
        {
            entity.ToTable("MemoAuxiliar");
            entity.HasKey(e => e.Id).HasName("iMemoAuxiliar1");

            entity.Property(e => e.Linha1)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Linha2)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Linha3)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Linha4)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Linha5)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Linha6)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.NomeFolha)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Pacientes>(entity =>
        {
            entity.ToTable("Pacientes");
            entity.HasKey(e => e.Id).HasName("iPacientes1");

            entity.Property(e => e.Bairro)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.CarteiraSUS)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("CarteiraSUS");
            entity.Property(e => e.CEP)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("CEP");
            entity.Property(e => e.Cidade)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.Complemento)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.Cor)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.CPF)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("CPF");
            entity.Property(e => e.DataBaixa).HasColumnType("datetime");
            entity.Property(e => e.DataEntrada).HasColumnType("datetime");
            entity.Property(e => e.DataEntradaBrasil).HasColumnType("datetime");
            entity.Property(e => e.DataRegistro).HasColumnType("datetime");
            entity.Property(e => e.DUM)
                .HasColumnType("datetime")
                .HasColumnName("DUM");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Emissor).HasDefaultValue(1);
            entity.Property(e => e.Endereco)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EtniaIndigena)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.IdPacienteExterno)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Identidade)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Logradouro)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.Nacionalidade)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Nascimento).HasColumnType("datetime");
            entity.Property(e => e.Naturalidade)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.NomeMae)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NomePaciente)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NomePai)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NomeSocial)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Numero)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Observacao)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.Profissao)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Sexo)
                .HasMaxLength(1)
                .IsUnicode(false);
            entity.Property(e => e.Telefone)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TipoSanguineo)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.UF)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("UF");
        });

        modelBuilder.Entity<PlanoExames>(entity =>
        {
            entity.ToTable("PlanoExames");
            entity.HasKey(e => e.Id).HasName("iPlanoExames1");

            entity.Property(e => e.CitoParteDescricao)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CitoTituloFolha)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.ContaExame)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.Descricao)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ICH)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ICH");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.LaboratorioExterno)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MapaHorizontal)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.QCH).HasColumnName("QCH");
            entity.Property(e => e.RefExame)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RefItem)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Referencia)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.ResultadoMaximo).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ResultadoMinimo).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.TABELACH)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("TABELACH");
            entity.Property(e => e.UnidadeMedida)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ValorCusto).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ValorItem).HasColumnType("decimal(18, 4)");
        });

        modelBuilder.Entity<Postos>(entity =>
        {
            entity.ToTable("Postos");
            entity.HasKey(e => e.Id).HasName("iPostos1");

            entity.Property(e => e.Bairro)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.CEP)
                .HasMaxLength(8)
                .IsUnicode(false)
                .HasColumnName("CEP");
            entity.Property(e => e.Cidade)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.Complemento)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.Endereco)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Logradouro)
                .HasMaxLength(8)
                .IsUnicode(false);
            entity.Property(e => e.NomePosto)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.Numero)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Responsavel)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.Telefone)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.UF)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasColumnName("UF");
        });

        modelBuilder.Entity<Rastreamentos>(entity =>
        {
            entity.ToTable("Rastreamentos");
            entity.HasKey(e => e.Id).HasName("iRastreamentos1");

            entity.Property(e => e.DataOcorrencia).HasColumnType("datetime");
            entity.Property(e => e.Exception)
                .HasMaxLength(4000)
                .IsUnicode(false);
            entity.Property(e => e.Falha)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.Ipexterno)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("IPExterno");
            entity.Property(e => e.Iplocal)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("IPLocal");
            entity.Property(e => e.NomeComputador)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.OpcaoMenu)
                .HasMaxLength(26)
                .IsUnicode(false);
            entity.Property(e => e.OperacaoComplementar)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.OperacaoRealizada)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.SistemaUtilizado)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.VersaoSistema)
                .HasMaxLength(26)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Requisitar>(entity =>
        {
            entity.ToTable("Requisitar");
            entity.HasKey(e => e.Id).HasName("iRequisitar1");

            entity.Property(e => e.ClasseExamesNome)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ContaExame)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.ControleApoio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DataEntregaParcial).HasColumnType("datetime");
            entity.Property(e => e.DataIni).HasColumnType("datetime");
            entity.Property(e => e.Descricao)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LaboratorioApoio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LaboratorioExterno)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MaterialRetorno)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.MaterialSaida)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.RefExame)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RefItem)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Referencia)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.Resultado)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.UnidadeMedida)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ValorItem).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.ClasseExames).WithMany(p => p.Requisitar)
                .HasForeignKey(d => d.ClasseExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iRequisitar_ClasseExames");

            entity.HasOne(d => d.Instituicao).WithMany(p => p.Requisitar)
                .HasForeignKey(d => d.InstituicaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iRequisitar_Instituicao");

            entity.HasOne(d => d.Medicos).WithMany(p => p.Requisitar)
                .HasForeignKey(d => d.MedicoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iRequisitar_Medicos");

            entity.HasOne(d => d.Pacientes).WithMany(p => p.Requisitar)
                .HasForeignKey(d => d.PacienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iRequisitar_Pacientes");

            entity.HasOne(d => d.TabelaExames).WithMany(p => p.Requisitar)
                .HasForeignKey(d => d.TabelaExamesId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("iRequisitar_TabelaExames");
        });

        modelBuilder.Entity<Senhas>(entity =>
        {
            entity.ToTable("Senhas");
            entity.HasKey(e => e.Id).HasName("iSenhas1");

            entity.HasIndex(e => e.LoginUsuario, "iSenhas2").IsUnique();

            entity.Property(e => e.CNPJEmpresa)
                .HasMaxLength(14)
                .IsUnicode(false)
                .HasColumnName("CNPJEmpresa");
            entity.Property(e => e.DataCadastro).HasColumnType("datetime");
            entity.Property(e => e.DataExpira).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.LoginUsuario)
                .HasMaxLength(60)
                .IsUnicode(false);
            entity.Property(e => e.NomeCompleto)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NomeUsuario)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.SenhaUsuario)
                .HasMaxLength(256)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Sexo>(entity =>
        {
            entity.ToTable("Sexo");
            entity.HasKey(e => e.Id).HasName("iSexo1");

            entity.HasIndex(e => e.Sigla, "iSexo2").IsUnique();

            entity.Property(e => e.Descricao)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Sigla)
                .HasMaxLength(1)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SituacaoExames>(entity =>
        {
            entity.ToTable("SituacaoExames");
            entity.HasKey(e => e.Id).HasName("iSituacaoExames1");

            entity.Property(e => e.Descricao)
                .HasMaxLength(40)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TabelaExames>(entity =>
        {
            entity.ToTable("TabelaExames");
            entity.HasKey(e => e.Id).HasName("iTabelaExames1");

            entity.HasIndex(e => e.SiglaTabela, "iTabelaExames2").IsUnique();

            entity.Property(e => e.NomeTabela)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SiglaTabela)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TextosProntos>(entity =>
        {
            entity.ToTable("TextosProntos");
            entity.HasKey(e => e.Id).HasName("iTextosProntos1");

            entity.HasIndex(e => e.Texto, "iTextosProntos2").IsUnique();

            entity.Property(e => e.Texto)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TipoSanguineo>(entity =>
        {
            entity.ToTable("TipoSanguineo");
            entity.HasKey(e => e.Id).HasName("iTipoSanguineo1");

            entity.Property(e => e.DoaPara)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.RecebeDe)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.Rh)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasColumnName("RH");
            entity.Property(e => e.Tipo)
                .HasMaxLength(2)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TituloExames>(entity =>
        {
            entity.ToTable("TituloExames");
            entity.HasKey(e => e.Id).HasName("iTituloExames1");

            entity.Property(e => e.TituloExame)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("TituloExame");
        });

        modelBuilder.Entity<UF>(entity =>
        {
            entity.ToTable("UF");
            entity.HasKey(e => e.Id).HasName("iUF1");

            entity.HasIndex(e => e.Sigla, "iUF2").IsUnique();

            entity.Property(e => e.Descricao)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Sigla)
                .HasMaxLength(2)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UsuariosWeb>(entity =>
        {
            entity.ToTable("UsuariosWeb");
            entity.HasKey(e => e.Id).HasName("iUsuariosWeb1");

            entity.Property(e => e.CNPJEmpresa)
                .HasMaxLength(14)
                .IsUnicode(false)
                .HasColumnName("CNPJEmpresa");
            entity.Property(e => e.CPFUsuario)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("CPFUsuario");
            entity.Property(e => e.DataCadastro).HasColumnType("datetime");
            entity.Property(e => e.DataNascimentoUsuario).HasColumnType("datetime");

            entity.HasOne(d => d.Senhas).WithMany(p => p.UsuariosWeb)
                .HasForeignKey(d => d.SenhaId)
                .HasConstraintName("iUsuariosWeb_Senhas")
                .OnDelete(DeleteBehavior.Cascade);
        });

    }
}
