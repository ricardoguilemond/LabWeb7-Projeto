namespace LabWebMvc.MVC.Areas.Strategy
{
    public interface IDeleteStrategy<T> where T : class
    {
        Task<bool> DeleteAsync(int id);

        Task<bool> DeleteAsync(string valor, string campo);

        Task<bool> DeleteManyAsync(List<int> ids);
    }
}