using KdxDesigner.Models;

using System.Collections.Generic;

namespace KdxDesigner.Services
{
    public interface IIOAddressService
    {
        FindIOResult FindByIOText(List<IO> ioList, string ioText, int plcId);
        List<IO> FindByIORange(List<IO> ioList, string ioText);
    }
}