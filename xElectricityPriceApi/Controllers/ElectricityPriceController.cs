using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using xElectricityPriceApi.Models;
using xElectricityPriceApi.Services;
using xElectricityPriceApiShared;
using xElectricityPriceApiShared.ElectricityPrice;

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
    public IEnumerable<PriceInformation> Get()
    {
        return _priceService.GetAllTodayAndTomorrow();
    }

    [HttpGet, Route("api/GetCurrentPrice/")]
    public PriceInformation GetCurrentPrice()
    {
        var price = _priceService.GetCurrentPrice() ?? new PriceInformation();
        return price;
    }

    [HttpGet, Route("api/price/sortByPrice")]
    public IEnumerable<PriceInformation> SortByPrice()
    {
        var priceList = _priceService.GetAllTodayAndTomorrow();
        priceList.Sort(new SortByLowestPrice());
        return priceList;
    }

    [HttpGet, Route("api/price/sortByPrice/{takeLimit}")]
    public IEnumerable<PriceInformation> SortByPrice(int takeLimit = 4)
    {
        var priceList = _priceService.GetAllTodayAndTomorrow();
        priceList.Sort(new SortByLowestPrice());
        return priceList.Take(takeLimit);
    }
}