namespace ExtensionsMethods.Enumerations
{
    public enum HttpCompletionOption : int
    {
        LerTrazerQuandoCompleto = 0,  //A operação deve ser concluída após a leitura de toda a resposta, incluindo o conteúdo.
        LerTrazerEnquantoParcial = 1  //A operação deve ser concluída assim que uma resposta estiver disponível e os cabeçalhos forem lidos. O conteúdo ainda não foi lido.
    }
}