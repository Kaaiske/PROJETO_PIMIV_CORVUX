using System.IO;
using System.Threading.Tasks;

namespace CorvusIAM.Interfaces
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream stream, string fileName, string contentType);
        Task<Stream?> DownloadFileAsync(string fileKey);
        string GetFileUrl(string fileKey);
    }
}
