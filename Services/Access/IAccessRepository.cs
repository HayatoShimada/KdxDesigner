using KdxDesigner.Models;
using KdxDesigner.Models.Define;

namespace KdxDesigner.Services.Access
{
    public interface IAccessRepository
    {
        string ConnectionString { get; }

        List<Company> GetCompanies();
        List<Model> GetModels();
        List<PLC> GetPLCs();
        List<Cycle> GetCycles();
        List<Models.Process> GetProcesses();
        List<Models.Machine> GetMachines();
        List<DriveMain> GetDriveMains();
        List<DriveSub> GetDriveSubs();
        List<CY> GetCYs();
        List<Models.Timer> GetTimersByCycleId(int cycleId);
        List<Operation> GetOperations();
        Operation? GetOperationById(int id);
        List<Length>? GetLengthByPlcId(int plcId);
        void UpdateOperation(Operation operation);
        List<ProcessDetail> GetProcessDetails();
        List<ProcessDetailDto> GetProcessDetailDtos();
        void SaveProcessDetailDtos(List<ProcessDetailDto> details);
        List<IO> GetIoList();
    }
}