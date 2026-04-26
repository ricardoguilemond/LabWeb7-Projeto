namespace LabWebMvc.MVC.Integracoes.Exportacao
{
    public class ExportacaoFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ExportacaoFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ServicoIntegracao CriarServico<T>() where T : ServicoIntegracao
        {
            return _serviceProvider.GetRequiredService<T>();
        }
    }

    //    Estava correto antes, mas não resolve via DI????
    //    public ServicoIntegracao CriarServico(LayoutExecucao layout)
    //    {
    //        var type = layout.GetType()
    //                         .GetField(layout.ToString())
    //                         ?.GetCustomAttributes(typeof(AttributeEnumType), false)
    //                         .FirstOrDefault() as AttributeEnumType;

    //        if (type == null)
    //            throw new Exception("Serviço não encontrado.");

    //        // Resolve via DI
    //        return (ServicoIntegracao)_serviceProvider.GetRequiredService(type.ServiceType);
    //    }
}