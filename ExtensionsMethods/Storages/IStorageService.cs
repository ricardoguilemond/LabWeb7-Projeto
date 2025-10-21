namespace ExtensionsMethods.Storages
{
    public interface IStorageService
    {
        Task<bool> SaveFileAsync(string fileName, byte[] data);

        Task<byte[]?> GetFileAsync(string fileName);

        Task<bool> DeleteFileAsync(string fileName);
    }
}