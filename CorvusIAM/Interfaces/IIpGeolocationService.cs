using System.Threading.Tasks;

namespace CorvusIAM.Interfaces
{
    public interface IIpGeolocationService
    {
        Task<string> GetLocationFromIpAsync(string ipAddress);
    }
}
