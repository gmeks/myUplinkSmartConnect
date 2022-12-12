using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using xElectricityPriceApi.Models;
using xElectricityPriceApi.Services;
using xElectricityPriceApiShared;

namespace xElectricityPriceApi.Controllers;

[ApiController]
public class ElectricityPriceController : ControllerBase
{
    private readonly ILogger<ElectricityPriceController> _logger;
    private readonly PriceService _priceService;

    public ElectricityPriceController(PriceService priceService, ILogger<ElectricityPriceController> logger)
    {
        _logger = logger;
        _priceService = priceService;
    }

    [HttpGet, Route("api/price/")]
    public async Task<IEnumerable<PriceInformation>> Get()
    {
        return _priceService.GetAllTodayAndTomorrow();
    }
}