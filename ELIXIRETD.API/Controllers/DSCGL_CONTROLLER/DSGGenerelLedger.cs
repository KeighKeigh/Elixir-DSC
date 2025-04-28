﻿using ELIXIRETD.API.Authentication;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.DTOs.REPORTS_DTO.ConsolidationDto;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.STORE_CONTEXT;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ELIXIRETD.API.Controllers.DSCGL_CONTROLLER
{
    [Route("api/dsgl-gl"), ApiController]
    [AllowAnonymous]
    public class DSGGL : ControllerBase
    {
        private readonly IMediator _mediator;
        public DSGGL(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ApiKeyAuth]
        public async Task<IActionResult> Get([FromQuery] DSGGLQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            else
            {
                return BadRequest(result);
            }

        }

        public class DSGGLQuery : IRequest<Result<List<DSGGLResult>>>
        {
            public string adjustment_month { get; set; }

        }

        public class DSGGLResult
        {
            public string SyncId { get; set; }
            public string Mark1 { get; set; }
            public string Mark2 { get; set; }
            public string AssetCIP { get; set; }
            public string AccountingTag { get; set; }
            public DateTime? TransactionDate { get; set; }
            public string ClientSupplier { get; set; }
            public string AccountTitleCode { get; set; }
            public string AccountTitle { get; set; }
            public string CompanyCode { get; set; }
            public string Company { get; set; }
            public string DivisionCode { get; set; }
            public string Division { get; set; }
            public string DepartmentCode { get; set; }
            public string Department { get; set; }
            public string UnitCode { get; set; }
            public string Unit { get; set; }
            public string SubUnitCode { get; set; }
            public string SubUnit { get; set; }
            public string LocationCode { get; set; }
            public string Location { get; set; }
            public string PONumber { get; set; }
            public string RRNumber { get; set; }
            public string ReferenceNo { get; set; }
            public string ItemCode { get; set; }
            public string ItemDescription { get; set; }
            public decimal? Quantity { get; set; }
            public string UOM { get; set; }
            public decimal? UnitPrice { get; set; }
            public decimal? LineAmount { get; set; }
            public string VoucherJournal { get; set; }
            public string AccountType { get; set; }
            public string DRCR { get; set; }
            public string AssetCode { get; set; }
            public string Asset { get; set; }
            public string ServiceProviderCode { get; set; }
            public string ServiceProvider { get; set; }
            public string BOA { get; set; }
            public string Allocation { get; set; }
            public string AccountGroup { get; set; }
            public string AccountSubGroup { get; set; }
            public string FinancialStatement { get; set; }
            public string UnitResponsible { get; set; }
            public string Batch { get; set; }
            public string Remarks { get; set; }
            public string PayrollPeriod { get; set; }
            public string Position { get; set; }
            public string PayrollType { get; set; }
            public string PayrollType2 { get; set; }
            public string DepreciationDescription { get; set; }
            public string RemainingDepreciationValue { get; set; }
            public string UsefulLife { get; set; }
            public string Month { get; set; }
            public string Year { get; set; }
            public string Particulars { get; set; }
            public string Month2 { get; set; }
            public string FarmType { get; set; }
            public string Adjustment { get; set; }
            public string From { get; set; }
            public string ChangeTo { get; set; }
            public string Reason { get; set; }
            public string CheckingRemarks { get; set; }
            public string BankName { get; set; }
            public string ChequeNumber { get; set; }
            public string ChequeVoucherNumber { get; set; }
            public string ChequeDate { get; set; }
            public string ReleasedDate { get; set; }
            public string BOA2 { get; set; }
            public string System { get; set; }
            public string Books { get; set; }
        }

        public class Handler : IRequestHandler<DSGGLQuery, Result<List<DSGGLResult>>>
        {
            private readonly StoreContext _context;

            public Handler(StoreContext context)
            {
                _context = context;

            }
            // Type desc


            public async Task<Result<List<DSGGLResult>>> Handle(DSGGLQuery request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(request.adjustment_month))
                {
                    return Result.Success(new List<DSGGLResult>());
                }

                if (!DateTime.TryParseExact(request.adjustment_month, "yyyy-MM",
                                            CultureInfo.InvariantCulture, DateTimeStyles.None,
                                            out DateTime adjustmentMonth))
                {
                    throw new ArgumentException("Adjustment_month must be in the format yyyy-MM");
                }

                var startDate = new DateTime(adjustmentMonth.Year, adjustmentMonth.Month, 1);
                var endDate = startDate.AddMonths(1);


                var moveOrderTask = await MoveOrderTransactions(startDate, endDate);
                var receiptTask = await ReceiptTransactions(startDate, endDate);
                var issueTask = await IssueTransactions(startDate, endDate);
                var borrowedTask = await BorrowedTransactions(startDate, endDate);
                var returnedTask = await ReturnedTransactions(startDate, endDate);
                var fuelTask = await FuelTransactions(startDate, endDate);

                //Task.WhenAll(moveOrderTask, receiptTask, issueTask, borrowedTask, returnedTask, fuelTask);


                var consolidateList = moveOrderTask.Concat(receiptTask).Concat(issueTask)
                    .Concat(borrowedTask)
                    .Concat(returnedTask)
                    .Concat(fuelTask);

                var result = consolidateList.SelectMany(x => new List<DSGGLResult>
                {
                    
                    //debit
                    new DSGGLResult
                    {
                        SyncId = "ETD-" + (x.SyncId.ToString() ?? string.Empty) + "-D",
                        Mark1 = string.Empty,
                        Mark2 = string.Empty,
                        AssetCIP = x.AssetCIP ?? string.Empty,
                        AccountingTag = string.Empty,
                        TransactionDate = x.TransactionDate,
                        ClientSupplier = x.ClientSupplier ?? string.Empty,
                        AccountTitleCode = x.AccountTitleCode,
                        AccountTitle = x.AccountTitle,
                        CompanyCode = "0001",
                        Company = "RDFFLFI",
                        DivisionCode = x.DivisionCode ?? string.Empty,
                        Division = x.Division ?? string.Empty,
                        DepartmentCode = x.DepartmentCode ?? string.Empty,
                        Department = x.Department ?? string.Empty,
                        UnitCode = x.UnitCode ?? string.Empty,
                        Unit = string.Empty,
                        SubUnitCode = x.SubUnitCode ?? string.Empty,
                        SubUnit = string.Empty,
                        LocationCode = x.LocationCode ?? string.Empty,
                        Location = x.Location ?? string.Empty,
                        PONumber = x.PONumber ?? string.Empty,
                        RRNumber = x.RRNumber,
                        ReferenceNo = x.ReferenceNo ?? string.Empty,
                        ItemCode = x.ItemCode ?? string.Empty,
                        ItemDescription = x.ItemDescription ?? string.Empty,
                        Quantity = x?.Quantity ?? 0,
                        UOM = x.UOM ?? string.Empty,
                        UnitPrice = x?.UnitPrice ?? 0,
                        LineAmount = x?.LineAmount ?? 0,
                        VoucherJournal = string.Empty,
                        AccountType = "Inventoriables",
                        DRCR = "Debit",
                        AssetCode = string.Empty,
                        Asset= string.Empty,
                        ServiceProviderCode = x.ServiceProviderCode ?? string.Empty,
                        ServiceProvider = x.ServiceProvider ?? string.Empty,
                        BOA = "Inventoriables",
                        Allocation = string.Empty,
                        AccountGroup = "Current Assets",
                        AccountSubGroup = "Inventories",
                        FinancialStatement = "Balance Sheet",
                        UnitResponsible = "MAU",
                        Batch = x.Reason ?? string.Empty,
                        Remarks = x.Remarks ?? string.Empty,
                        PayrollPeriod = string.Empty,
                        Position = x.Position ?? string.Empty,
                        PayrollType = string.Empty,
                        PayrollType2 = string.Empty,
                        DepreciationDescription = string.Empty,
                        RemainingDepreciationValue = string.Empty,
                        UsefulLife = string.Empty,
                        Month = x.TransactionDate.Value.ToString("MMM") ?? string.Empty,
                        Year = x.TransactionDate.Value.ToString("yyyy") ?? string.Empty,
                        Particulars = string.Empty,
                        Month2 = x.TransactionDate.Value.ToString("yyyyMM") ?? string.Empty,
                        FarmType = string.Empty,
                        Adjustment = string.Empty,
                        From = string.Empty,
                        ChangeTo = string.Empty,
                        Reason = string.Empty,
                        CheckingRemarks = x.CheckingRemarks ?? string.Empty,
                        BankName = string.Empty,
                        ChequeNumber = string.Empty,
                        ChequeVoucherNumber = string.Empty,
                        ReleasedDate = string.Empty,
                        ChequeDate = string.Empty,
                        BOA2 = "Inventoriables",
                        System = "Elixir - ETD",
                        Books = "Journal Book",
                    },
                    //credit
                    new DSGGLResult
                    {
                        SyncId = "ETD-" + (x.SyncId.ToString() ?? string.Empty) + "-C",
                        Mark1 = string.Empty,
                        Mark2 = string.Empty,
                        AssetCIP = x.AssetCIP ?? string.Empty,
                        AccountingTag = string.Empty,
                        TransactionDate = x.TransactionDate,
                        ClientSupplier = x.ClientSupplier ?? string.Empty,
                        AccountTitleCode = "115998",
                        AccountTitle = "Materials & Supplies Inventory",
                        CompanyCode = "0001",
                        Company = "RDFFLFI",
                        DivisionCode = x.DivisionCode ?? string.Empty,
                        Division = x.Division ?? string.Empty,
                        DepartmentCode = x.DepartmentCode ?? string.Empty,
                        Department = x.Department ?? string.Empty,
                        UnitCode = x.UnitCode ?? string.Empty,
                        Unit = x.Unit ?? string.Empty,
                        SubUnitCode = x.SubUnitCode ?? string.Empty,
                        SubUnit = x.SubUnit ?? string.Empty,
                        LocationCode = x.LocationCode ?? string.Empty,
                        Location = x.Location ?? string.Empty,
                        PONumber = x.PONumber ?? string.Empty,
                        RRNumber = x.RRNumber,
                        ReferenceNo = x.ReferenceNo ?? string.Empty,
                        ItemCode = x.ItemCode ?? string.Empty,
                        ItemDescription = x.ItemDescription ?? string.Empty,
                        Quantity = x?.Quantity ?? 0,
                        UOM = x.UOM ?? string.Empty,
                        UnitPrice = x?.UnitPrice ?? 0,
                        LineAmount = -(x?.LineAmount) ?? 0,
                        VoucherJournal = string.Empty,
                        AccountType = "Inventoriables",
                        DRCR = "Credit",
                        AssetCode = x.AssetCode ?? string.Empty,
                        Asset= string.Empty,
                        ServiceProviderCode = x.ServiceProviderCode ?? string.Empty,
                        ServiceProvider = x.ServiceProvider ?? string.Empty,
                        BOA = "Inventoriables",
                        Allocation = string.Empty,
                        AccountGroup = "Current Assets",
                        AccountSubGroup = "Inventories",
                        FinancialStatement = "Balance Sheet",
                        UnitResponsible = "MAU",
                        Batch = x.Reason ?? string.Empty,
                        Remarks = x.Remarks ?? string.Empty,
                        PayrollPeriod = string.Empty,
                        Position = string.Empty,
                        PayrollType = string.Empty,
                        PayrollType2 =  string.Empty,
                        DepreciationDescription =  string.Empty,
                        RemainingDepreciationValue =  string.Empty,
                        UsefulLife =  string.Empty,
                        Month = x.TransactionDate.Value.ToString("MMM") ?? string.Empty,
                        Year = x.TransactionDate.Value.ToString("yyyy") ?? string.Empty,
                        Particulars = string.Empty,
                        Month2 = x.TransactionDate.Value.ToString("yyyyMM") ?? string.Empty,
                        FarmType =  string.Empty,
                        Adjustment =  string.Empty,
                        From =  string.Empty,
                        ChangeTo =  string.Empty,
                        Reason = string.Empty,
                        CheckingRemarks = x.CheckingRemarks ?? string.Empty,
                        BankName =  string.Empty,
                        ChequeNumber =  string.Empty,
                        ChequeVoucherNumber =  string.Empty,
                        ReleasedDate = string.Empty,
                        ChequeDate = string.Empty,
                        BOA2 = "Inventoriables",
                        System = "Elixir - ETD",
                        Books = "Journal Book",
                    }
                }).ToList();

                return Result.Success(result);
            }

            private async Task<List<DSGGLResult>> MoveOrderTransactions(DateTime startDate, DateTime endDate)
            {
                var result = (from m in _context.MoveOrders
                              join t in _context.TransactOrder on m.OrderNo equals t.OrderNo
                              join w in _context.WarehouseReceived on m.WarehouseId equals w.Id
                              join u in _context.Users on t.PreparedBy equals u.FullName
                              where t.PreparedDate >= startDate && t.PreparedDate <= endDate && m.IsTransact == true
                              select new DSGGLResult
                              {
                                  SyncId = m.Id.ToString(),
                                  TransactionDate = t.PreparedDate,
                                  ClientSupplier = m.CustomerName,
                                  PONumber = m.Category,
                                  RRNumber = m.HelpdeskNo.ToString(),
                                  ItemCode = w.ItemCode,
                                  ItemDescription = w.ItemDescription,
                                  Quantity = m.QuantityOrdered,
                                  UnitPrice = w.UnitPrice,
                                  LineAmount = (w.UnitPrice * m.QuantityOrdered),
                                  UOM = w.Uom,
                                  CheckingRemarks = "Move Order",
                                  DivisionCode = m.CompanyCode,
                                  Division = m.CompanyName,
                                  LocationCode = m.LocationCode,
                                  Location = m.LocationName,
                                  AccountTitle = m.AccountTitles,
                                  AccountTitleCode = m.AccountCode,
                                  DepartmentCode = m.DepartmentCode,
                                  Department = m.DepartmentName,
                                  AssetCIP = m.Cip_No,
                                  Batch = m.ItemRemarks,
                                  ServiceProvider = t.PreparedBy,
                                  ServiceProviderCode = u.EmpId,
                                  ReferenceNo = (m.Id.ToString() ?? "") + (m.EmpId ?? ""),
                                  Remarks = m.ItemRemarks


                              });

                return await result.ToListAsync();
            }
            private async Task<List<DSGGLResult>> ReceiptTransactions(DateTime startDate, DateTime endDate)
            {
                var result = _context.MiscellaneousReceipts
                .AsNoTracking()
                .GroupJoin(_context.WarehouseReceived, receipt => receipt.Id, warehouse => warehouse.MiscellaneousReceiptId, (receipt, warehouse) => new { receipt, warehouse })
                .SelectMany(x => x.warehouse.DefaultIfEmpty(), (x, warehouse) => new { x.receipt, warehouse }).Join(_context.Users, x => x.receipt.PreparedBy, user => user.FullName,
            (x, user) => new { x.receipt, x.warehouse, user })
                .Where(x => x.warehouse.IsActive == true && x.warehouse.TransactionType == "MiscellaneousReceipt"
                && x.receipt.TransactionDate >= startDate && x.receipt.TransactionDate <= endDate)
                .Select(x => new DSGGLResult
                {
                    SyncId = x.warehouse.Id.ToString(),
                    TransactionDate = x.receipt.TransactionDate,
                    ItemCode = x.warehouse.ItemCode,
                    ItemDescription = x.warehouse.ItemDescription,
                    UOM = x.warehouse.Uom,
                    PONumber = "",
                    Quantity = x.warehouse.ActualGood,
                    UnitPrice = x.warehouse.UnitPrice,
                    LineAmount = Math.Round(x.warehouse.UnitPrice * x.warehouse.ActualGood, 2),
                    CheckingRemarks = "Miscellaneous Receipt",
                    Remarks = x.receipt.Details,
                    DivisionCode = x.receipt.CompanyCode,
                    Division = x.receipt.CompanyName,
                    DepartmentCode = x.receipt.DepartmentCode,
                    Department = x.receipt.DepartmentName,
                    LocationCode = x.receipt.LocationCode,
                    Location = x.receipt.LocationName,
                    AccountTitleCode = x.warehouse.AccountCode,
                    AccountTitle = x.warehouse.AccountTitles,
                    ServiceProvider = x.user.FullName,
                    ServiceProviderCode = x.user.EmpId,
                    RRNumber = 0.ToString(),
                    AssetCIP = "",
                });

                return await result.ToListAsync();
            }
            private async Task<List<DSGGLResult>> IssueTransactions(DateTime startDate, DateTime endDate)
            {
                var result = _context.MiscellaneousIssues
                .AsNoTracking()
                .Join(_context.MiscellaneousIssueDetail, miscDatail => miscDatail.Id, issue => issue.IssuePKey,
                (miscDetail, issue) => new { miscDetail, issue }).Join(_context.Users, x => x.miscDetail.PreparedBy, user => user.FullName,
            (x, user) => new { x.miscDetail, x.issue, user })
                .Where(x => x.issue.IsActive == true && x.miscDetail.TransactionDate >= startDate && x.miscDetail.TransactionDate <= endDate)
                .Select(x => new DSGGLResult
                {
                    SyncId = x.issue.Id.ToString(),
                    TransactionDate = x.miscDetail.TransactionDate.Date,
                    ItemCode = x.issue.ItemCode,
                    ItemDescription = x.issue.ItemDescription,
                    UOM = x.issue.Uom,
                    PONumber = "",
                    Quantity = Math.Round(x.issue.Quantity, 2),
                    UnitPrice = x.issue.UnitPrice,
                    LineAmount = Math.Round(x.issue.UnitPrice * x.issue.Quantity, 2),

                    CheckingRemarks = "Miscellaneous Issue",
                    Reason = x.issue.Remarks,

                    DivisionCode = x.miscDetail.CompanyCode,
                    Division = x.miscDetail.CompanyName,
                    DepartmentCode = x.miscDetail.DepartmentCode,
                    Department = x.miscDetail.DepartmentName,
                    LocationCode = x.miscDetail.LocationCode,
                    Location = x.miscDetail.LocationName,
                    AccountTitleCode = x.issue.AccountCode,
                    AccountTitle = x.issue.AccountTitles,
                    ServiceProvider = x.user.FullName,
                    ServiceProviderCode = x.user.EmpId,
                    AssetCIP = "",
                    RRNumber = 0.ToString(),


                });

                return await result.ToListAsync();
            }
            private async Task<List<DSGGLResult>> BorrowedTransactions(DateTime startDate, DateTime endDate)
            {
                var result = _context.BorrowedIssues
                    .AsNoTracking()
                    .Join(_context.BorrowedIssueDetails, borrow => borrow.Id, borrowDetail => borrowDetail.BorrowedPKey,
                    (borrow, borrowDetail) => new { borrow, borrowDetail }).Join(_context.Users, x => x.borrow.PreparedBy, user => user.FullName,
            (x, user) => new { x.borrow, x.borrowDetail, user })
                    .Where(x => x.borrowDetail.IsActive == true && x.borrowDetail.PreparedDate >= startDate && x.borrowDetail.PreparedDate <= endDate)
                    .Select(x => new DSGGLResult
                    {
                        SyncId = x.borrowDetail.Id.ToString(),
                        TransactionDate = x.borrowDetail.PreparedDate.Date,
                        ItemCode = x.borrowDetail.ItemCode,
                        ItemDescription = x.borrowDetail.ItemDescription,
                        UOM = x.borrowDetail.Uom,
                        PONumber = "",
                        Quantity = Math.Round(x.borrowDetail.Quantity, 2),
                        UnitPrice = x.borrowDetail.UnitPrice,
                        LineAmount = Math.Round(x.borrowDetail.UnitPrice * x.borrowDetail.Quantity, 2),

                        CheckingRemarks = "Borrow",
                        Reason = x.borrow.Remarks,
                        Remarks = x.borrow.Details,

                        DivisionCode = "",
                        Division = "",
                        DepartmentCode = "",
                        Department = "",
                        LocationCode = "",
                        Location = "",
                        AccountTitleCode = "",
                        AccountTitle = "",
                        ServiceProvider = x.user.FullName,
                        ServiceProviderCode = x.user.EmpId,
                        AssetCIP = "",
                        RRNumber = 0.ToString(),
                        //Remarks = x.borrow.Remarks,


                    });
                return await result.ToListAsync();
            }
            private async Task<List<DSGGLResult>> ReturnedTransactions(DateTime startDate, DateTime endDate)
            {
                var consumeList = _context.BorrowedConsumes
                    .Where(x => x.IsActive == true)
                    .Select(x => new BorrowedConsolidatedDto
                    {
                        Id = x.Id,
                        BorrowedId = x.BorrowedItemPkey,
                        ItemCode = x.ItemCode,
                        ItemDescription = x.ItemDescription,
                        Uom = x.Uom,
                        Consumed = x.Consume,
                        CompanyCode = x.CompanyCode,
                        CompanyName = x.CompanyName,
                        DepartmentCode = x.DepartmentCode,
                        DepartmentName = x.DepartmentName,
                        LocationCode = x.LocationCode,
                        LocationName = x.LocationName,
                        AccountCode = x.AccountCode,
                        AccountTitles = x.AccountTitles,
                        EmpId = x.EmpId,
                        FullName = x.FullName,
                        ReportNumber = x.ReportNumber,

                    });

                var returnList = _context.BorrowedIssueDetails
                    .Where(x => x.IsActive == true && x.IsApprovedReturned == true)
                    .GroupJoin(consumeList, borrowDetails => borrowDetails.Id, consume => consume.BorrowedId
                    , (borrowDetails, consume) => new { borrowDetails, consume })
                    .SelectMany(x => x.consume.DefaultIfEmpty(), (x, consume) => new { x.borrowDetails, consume })
                    .Select(x => new BorrowedConsolidatedDto
                    {
                        Id = x.borrowDetails.Id,
                        BorrowedId = x.borrowDetails.BorrowedPKey,
                        ItemCode = x.borrowDetails.ItemCode,
                        ItemDescription = x.borrowDetails.ItemDescription,
                        Uom = x.borrowDetails.Uom,
                        BorrowedQuantity = x.borrowDetails.Quantity != null ? x.borrowDetails.Quantity : 0,
                        Consumed = x.consume.Consumed != null ? x.consume.Consumed : 0,
                        CompanyCode = x.consume.CompanyCode,
                        CompanyName = x.consume.CompanyName,
                        DepartmentCode = x.consume.DepartmentCode,
                        DepartmentName = x.consume.DepartmentName,
                        LocationCode = x.consume.LocationCode,
                        LocationName = x.consume.LocationName,
                        AccountCode = x.consume.AccountCode,
                        AccountTitles = x.consume.AccountTitles,
                        EmpId = x.consume.EmpId,
                        FullName = x.consume.FullName,
                        ReportNumber = x.consume.ReportNumber,
                        UnitPrice = x.borrowDetails.UnitPrice

                    });

                var borrowedIssueList = _context.BorrowedIssues
                    .AsNoTracking()
                    .Where(x => x.IsActive == true);

                var result = returnList
                    .GroupJoin(borrowedIssueList, borrowDetail => borrowDetail.BorrowedId, borrow => borrow.Id,
                    (borrowDetail, borrow) => new { borrowDetail, borrow })
                    .SelectMany(x => x.borrow.DefaultIfEmpty(), (x, borrow) => new { x.borrowDetail, borrow }).Where(x => x.borrow.PreparedDate >= startDate && x.borrow.PreparedDate >= endDate)
                    .Select(x => new DSGGLResult
                    {

                        SyncId = x.borrowDetail.Id.ToString(),
                        TransactionDate = x.borrow.PreparedDate.Date,
                        ItemCode = x.borrowDetail.ItemCode,
                        ItemDescription = x.borrowDetail.ItemDescription,
                        UOM = x.borrowDetail.Uom,
                        PONumber = "",
                        Quantity = x.borrowDetail.BorrowedQuantity - x.borrowDetail.Consumed,
                        UnitPrice = x.borrowDetail.UnitPrice,
                        LineAmount = Math.Round(x.borrowDetail.UnitPrice.Value * x.borrowDetail.BorrowedQuantity - x.borrowDetail.Consumed, 2),

                        CheckingRemarks = "Returned",
                        Reason = "",
                        Remarks = "",

                        DivisionCode = x.borrowDetail.CompanyCode,
                        Division = x.borrowDetail.CompanyName,
                        DepartmentCode = x.borrowDetail.DepartmentCode,
                        Department = x.borrowDetail.DepartmentName,
                        LocationCode = x.borrowDetail.LocationCode,
                        Location = x.borrowDetail.LocationName,
                        AccountTitleCode = x.borrowDetail.AccountCode,
                        AccountTitle = x.borrowDetail.AccountTitles,
                        ServiceProvider = x.borrowDetail.FullName,
                        ServiceProviderCode = x.borrowDetail.EmpId,
                        AssetCIP = "",
                        RRNumber = 0.ToString(),

                    });
                return await result.ToListAsync();
            }
            private async Task<List<DSGGLResult>> FuelTransactions(DateTime startDate, DateTime endDate)
            {
                var result = _context.FuelRegisterDetails
                    .Include(m => m.Material)
                    .ThenInclude(id => id.ItemCategory)
                    .Include(w => w.Warehouse_Receiving)
                    .Where(r => r.FuelRegister.Is_Transact == true && r.FuelRegister.Transact_At >= startDate && r.FuelRegister.Transact_At <= endDate)

                    .Select(x => new DSGGLResult
                    {

                        SyncId = x.Id.ToString(),
                        TransactionDate = x.FuelRegister.Transact_At.Value.Date,
                        ItemCode = x.Material.ItemCode,
                        ItemDescription = x.Material.ItemDescription,
                        UOM = x.Material.Uom.UomCode,
                        PONumber = "",
                        Quantity = x.Liters != null ? x.Liters : 0,
                        UnitPrice = x.Warehouse_Receiving.UnitPrice,
                        LineAmount = Math.Round(x.Warehouse_Receiving.UnitPrice * x.Liters.Value, 2),

                        CheckingRemarks = "Returned",
                        Reason = x.FuelRegister.Remarks,
                        Remarks = "",

                        DivisionCode = x.FuelRegister.Company_Code,
                        Division = x.FuelRegister.Company_Name,
                        DepartmentCode = x.FuelRegister.Department_Code,
                        Department = x.FuelRegister.Department_Name,
                        LocationCode = x.FuelRegister.Location_Code,
                        Location = x.FuelRegister.Location_Name,
                        AccountTitleCode = x.FuelRegister.Account_Title_Code,
                        AccountTitle = x.FuelRegister.Account_Title_Code,

                        AssetCIP = "",
                        RRNumber = 0.ToString(),


                    });
                return await result.ToListAsync();
            }


        }
    }
}
