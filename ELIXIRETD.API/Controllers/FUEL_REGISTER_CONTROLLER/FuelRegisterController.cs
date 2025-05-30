﻿using ELIXIRETD.DATA.CORE.ICONFIGURATION;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.DTOs.FUEL_REGISTER_DTO;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.DTOs.INVENTORY_DTO.MRP;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.EXTENSIONS;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.HELPERS;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.STORE_CONTEXT;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ELIXIRETD.API.Controllers.FUEL_REGISTER_CONTROLLER
{
    [Route("api/[controller]")]
    [ApiController]
    public class FuelRegisterController : ControllerBase
    {
        private readonly IUnitOfWork _unitofwork;
        private readonly StoreContext _context;

        public FuelRegisterController(IUnitOfWork unitofwork,StoreContext context)
        {
            _unitofwork = unitofwork;
            _context = context;
        }

        [AllowAnonymous]
        [HttpPost("create-fuel")]
        public async Task<IActionResult> CreateFuelRegister(CreateFuelRegisterDto fuel)
        {

            fuel.Added_By = User.Identity.Name;
            fuel.Modified_By = User.Identity.Name;

             var addFuel =  await _unitofwork.FuelRegister.CreateFuelRegister(fuel);

            var fuelDetailsList = await _context.FuelRegisterDetails
           .Where(x => x.Is_Active == true && x.FuelRegisterId == null)
           .ToListAsync();

            foreach (var fuelDetail in fuelDetailsList)
            {
                var fuelDetailExist = await _context.FuelRegisterDetails
                    .FirstOrDefaultAsync(x => x.Id == fuelDetail.Id);

                fuelDetailExist.FuelRegisterId = addFuel.Id;
            }

            await _unitofwork.CompleteAsync();

            return Ok("Successfully created");
        }

        [AllowAnonymous] // not ok, can't solve it by me
        [HttpPost("create-fuel-details")]
        public async Task<IActionResult> CreateFuelRegisterDetails(CreateFuelRegisterDetailsDto fuel)
        {
            var barCodeDic = new Dictionary<int, decimal?>();
            var fuelLiter = new List<decimal?>();

            var fuelLiterRegister = new decimal();

            fuelLiterRegister = fuel.Liters;
            decimal fuelLiters = 0;


            fuel.Added_By = User.Identity.Name;
            fuel.Modified_By = User.Identity.Name;
       
            var barcodeList = await _unitofwork.FuelRegister.GetMaterialStockByWarehouse();

            foreach (var item in barcodeList)
            {
                barCodeDic.Add(item.WarehouseId, item.Remaining_Stocks);
            }

            if (fuel.Liters > barCodeDic.Values.Sum())
                return BadRequest("Not enough stocks!");

            foreach (var liter in barCodeDic)
            {
                decimal fuelDif = Math.Abs(fuelLiterRegister - fuelLiter.Sum().Value);


                if (liter.Value >= (fuelLiterRegister - fuelLiter.Sum()))
                {

                    fuel.Liters = fuelDif;
                    fuel.Warehouse_ReceivingId = liter.Key;

                    await _unitofwork.FuelRegister.CreateFuelRegisterDetails(fuel);
                    break;
                }

                fuel.Liters = liter.Value.Value;
                fuel.Warehouse_ReceivingId = liter.Key;

                var createFuel = await _unitofwork.FuelRegister.CreateFuelRegisterDetails(fuel);

                fuelLiter.Add(liter.Value.Value);

            }


            await _unitofwork.CompleteAsync();

            return Ok("Successfully created");
        }

        [HttpGet("material-available")]
        public async Task<IActionResult> GetMaterialByStocks()
        {
            var results = await _unitofwork.FuelRegister.GetMaterialByStocks();

            return Ok(results); 
        }

        [HttpGet("user-role")]
        public async Task<IActionResult> GetDriverUser()
        {
            var results = await _unitofwork.FuelRegister.GetDriverUser();

            return Ok(results);
        }


        [HttpGet("material-available-item")]
        public async Task<IActionResult> GetMaterialStockByWarehouse()
        {
            var results = await _unitofwork.FuelRegister.GetMaterialStockByWarehouse();

            return Ok(results);
        }

        [HttpGet("page")]
        public async Task<ActionResult<IEnumerable<GetFuelRegisterDto>>> GetFuelRegister([FromQuery] UserParams userParams, [FromQuery] string Search, [FromQuery]string Status, [FromQuery]string UserId)
        {

            var fuel = await _unitofwork.FuelRegister.GetFuelRegister(userParams, Search, Status,UserId);

            Response.AddPaginationHeader(fuel.CurrentPage, fuel.PageSize, fuel.TotalCount, fuel.TotalPages, fuel.HasNextPage, fuel.HasPreviousPage);

            var fuelResult = new
            {
                fuel,
                fuel.CurrentPage,
                fuel.PageSize,
                fuel.TotalCount,
                fuel.TotalPages,
                fuel.HasNextPage,
                fuel.HasPreviousPage
            };

            return Ok(fuelResult);
        }

        [HttpPut("approve")]
        public async Task<IActionResult> ApproveFuel([FromBody]ApproveFuelDto[] fuel)
        {

            foreach(var item in fuel)
            {
                var fuelNotExist = await _unitofwork.FuelRegister.FuelRegisterNotExist(item.Id);
                if (fuelNotExist is false)
                    return BadRequest("fuel not exist!");

                item.Approve_By = User.Identity.Name;
                item.Transact_By = User.Identity.Name;

                await _unitofwork.FuelRegister.ApproveFuel(item);

            }

            await _unitofwork.CompleteAsync();

            return Ok("Successfully approve");
        }

        [HttpPut("reject")]
        public async Task<IActionResult> RejectFuel([FromBody] RejectFuelDto[] fuel)
        {

            foreach (var item in fuel)
            {
                var fuelNotExist = await _unitofwork.FuelRegister.FuelRegisterNotExist(item.Id);
                if (fuelNotExist is false)
                    return BadRequest("fuel not exist!");

                item.Reject_By = User.Identity.Name;

                await _unitofwork.FuelRegister.RejectFuel(item);

            }

            await _unitofwork.CompleteAsync();

            return Ok("Successfully reject");
        }

        [HttpPut("transact")]
        public async Task<IActionResult> TransactFuel([FromBody] TransactedFuelDto[] fuel)
        {


            foreach (var item in fuel)
            {
                var fuelNotExist = await _unitofwork.FuelRegister.FuelRegisterNotExist(item.Id);
                if (fuelNotExist is false)
                    return BadRequest("fuel not exist!");

                item.Transacted_By = User.Identity.Name;

                await _unitofwork.FuelRegister.TransactFuel(item);

            }

            await _unitofwork.CompleteAsync();

            return Ok("Successfully transacted");
        }

        [HttpPut("cancel")]
        public async Task<IActionResult> CancelFuel([FromBody] int[] item)
        {

            foreach(var id in item)
            {
                var fuelNotExist = await _unitofwork.FuelRegister.FuelRegisterNotExist(id);
                if (fuelNotExist is false)
                    return BadRequest("fuel not exist!");


                await _unitofwork.FuelRegister.CancelFuel(id);

            }

            await _unitofwork.CompleteAsync();

            return Ok("Successfully cancel");
        }

        [HttpGet("view")]
        public async Task<IActionResult> GetForApprovalFuel()
        {
            var fuel = await _unitofwork.FuelRegister.GetForApprovalFuel();

            return Ok(fuel);
        }
    }
}