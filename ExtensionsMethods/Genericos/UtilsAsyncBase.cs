namespace ExtensionsMethods.Genericos
{
    public class UtilsAsyncBase
    {
        public static async Task<string> LerURL(string url)
        {
            using (HttpClient client = new())
            {
                string response = await client.GetStringAsync(url);
                //Existem outras opções aliém do GetStringAsync, aí você precisa explorar a classe

                return response;
            }
        }
    }
}