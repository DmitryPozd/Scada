using System.Threading.Tasks;
using Scada.Client.Models;

namespace Scada.Client.Services;

public interface ISettingsService
{
    Task<ModbusConnectionConfig?> LoadAsync();
    Task SaveAsync(ModbusConnectionConfig config);
}
