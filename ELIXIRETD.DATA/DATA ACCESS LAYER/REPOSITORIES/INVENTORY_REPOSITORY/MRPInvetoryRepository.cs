﻿using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using ELIXIRETD.DATA.CORE.INTERFACES.INVENTORY_INTERFACE;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.DTOs.IMPORT_DTO;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.DTOs.INVENTORY_DTO.MRP;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.DTOs.INVENTORYDTO;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.DTOs.MISCELLANEOUS_DTO;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.DTOs.ORDER_DTO;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.DTOs.REPORTS_DTO;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.HELPERS;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.MODELS;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.STORE_CONTEXT;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.Linq;

namespace ELIXIRETD.DATA.DATA_ACCESS_LAYER.REPOSITORIES.INVENTORY_REPOSITORY
{
    public class MRPInvetoryRepository : IMRPInventory
    {
        private readonly StoreContext _context;

        public MRPInvetoryRepository(StoreContext context)
        {
            _context= context;
        }

        public async Task<IReadOnlyList<DtoGetAllAvailableInRawmaterialInventory>> GetAllAvailableInRawmaterialInventory()
        {
            return await _context.WarehouseReceived
              .GroupBy(x => new
            {
                  x.ItemCode,
                  x.ItemDescription,
                  x.LotSection,
                  x.Uom,
                  x.IsWarehouseReceived,

            }).Select(inventory => new DtoGetAllAvailableInRawmaterialInventory
            {
                ItemCode = inventory.Key.ItemCode,
                ItemDescription = inventory.Key.ItemDescription,
                LotCategory = inventory.Key.LotSection,
                Uom = inventory.Key.Uom,
                SOH = inventory.Sum(x => x.ActualGood),
                ReceiveIn = inventory.Sum(x => x.ActualGood),
                RejectOrder= inventory.Sum(x => x.TotalReject),
                IsWarehouseReceived = inventory.Key.IsWarehouseReceived, 
                

            }).OrderBy(x => x.ItemCode)
            .Where(x => x.IsWarehouseReceived == true)
            .ToListAsync();

        }

        public async Task<PagedList<DtoMRP>> GetallItemForInventoryPaginationOrig(UserParams userParams, string search)
        {

            var EndDate = DateTime.Now;
            var StartDate = EndDate.AddDays(-30);

            var getPoSummary = _context.PoSummaries.AsNoTracking().Where(x => x.IsActive == true)
                                                            .GroupBy(x => new
                                                            {

                                                                x.ItemCode,

                                                            }).Select(x => new PoSummaryDto
                                                            {
                                                                ItemCode = x.Key.ItemCode,
                                                                UnitPrice = x.Sum(x => x.UnitPrice),
                                                                Ordered = x.Sum(x => x.Ordered),
                                                                //TotalPrice = x.Average(x => x.UnitPrice)

                                                            });


            var getWarehouseIn = _context.WarehouseReceived.AsNoTracking().Where(x => x.IsActive == true)
                                                           .Where(x => x.TransactionType == "Receiving")
                                                           .OrderBy(x => x.ActualReceivingDate)
                                                           .GroupBy(x => new
                                                           {

                                                               x.ItemCode,
                                                           }).Select(x => new WarehouseInventory
                                                           {

                                                               ItemCode = x.Key.ItemCode,
                                                               ActualGood = x.Sum(x => x.ActualGood),
                                                           });

            var getMoveOrderOut = _context.MoveOrders.AsNoTracking().Where(x => x.IsActive == true)
                                                    .Where(x => x.IsPrepared == true)
                                                    .GroupBy(x => new
                                                    {
                                                        x.ItemCode,

                                                    }).Select(x => new MoveOrderInventory
                                                    {

                                                        ItemCode = x.Key.ItemCode,
                                                        QuantityOrdered = x.Sum(x => x.QuantityOrdered),

                                                    });

            var getNotTransacted = _context.MoveOrders.AsNoTracking().Where(x => x.IsActive == true && x.IsPrepared == true)
                                        .Where(x => x.IsTransact != true)
                                        .GroupBy(x => new
                                        {
                                            x.ItemCode,

                                        }).Select(x => new MoveOrderInventory
                                        {

                                            ItemCode = x.Key.ItemCode,
                                            QuantityOrdered = x.Sum(x => x.QuantityOrdered),

                                        });

            var getReceiptIn = _context.WarehouseReceived.AsNoTracking().Where(x => x.IsActive == true)
                                                         .Where(x => x.TransactionType == "MiscellaneousReceipt")
                                                         .GroupBy(x => new
                                                         {

                                                             x.ItemCode,

                                                         }).Select(x => new DtoRecieptIn
                                                         {

                                                             ItemCode = x.Key.ItemCode,
                                                             Quantity = x.Sum(x => x.ActualGood),

                                                         });


            var getIssueOut = _context.MiscellaneousIssueDetail.AsNoTracking().Where(x => x.IsActive == true)
                                                                //.Where(x => x.IsTransact == true)
                                                                .GroupBy(x => new
                                                                {

                                                                    x.ItemCode,

                                                                }).Select(x => new DtoIssueInventory
                                                                {
                                                                    ItemCode = x.Key.ItemCode,
                                                                    Quantity = x.Sum(x => x.Quantity),

                                                                });

            var getBorrowedIssue = _context.BorrowedIssueDetails
                .AsNoTracking()
                                                       .Where(x => x.IsActive == true)
                                                       .GroupBy(x => new
                                                       {

                                                           x.ItemCode,

                                                       }).Select(x => new DtoBorrowedIssue
                                                       {

                                                           ItemCode = x.Key.ItemCode,
                                                           Quantity = x.Sum(x => x.Quantity),

                                                       });


            var getBorrowedForMrp = _context.BorrowedIssueDetails.AsNoTracking().Where(x => x.IsActive == true)
                                                                 .GroupBy(x => new
                                                                 {
                                                                     x.ItemCode

                                                                 }).Select(x => new DtoBorrowedIssue
                                                                 {
                                                                     ItemCode = x.Key.ItemCode,
                                                                     Quantity = x.Sum(x => x.Quantity)
                                                                 });

            var consumed = _context.BorrowedConsumes.AsNoTracking().Where(x => x.IsActive)
                                                       .GroupBy(x => new
                                                       {
                                                           x.ItemCode,
                                                           x.BorrowedItemPkey

                                                       }).Select(x => new ItemStocksDto
                                                       {
                                                           ItemCode = x.Key.ItemCode,
                                                           BorrowedItemPkey = x.Key.BorrowedItemPkey,
                                                           Consume = x.Sum(x => x.Consume != null ? x.Consume : 0)
                                                       });


            var getReturnedBorrow = _context.BorrowedIssueDetails.AsNoTrackingWithIdentityResolution().Where(x => x.IsActive == true)
                                                             .Where(x => x.IsReturned == true)
                                                             .Where(x => x.IsApprovedReturned == true)
                                                             .GroupJoin(consumed, returned => returned.Id, itemconsume => itemconsume.BorrowedItemPkey, (returned, itemconsume) => new { returned, itemconsume })
                                                             .SelectMany(x => x.itemconsume.DefaultIfEmpty(), (x, itemconsume) => new { x.returned, itemconsume })
                                                             .GroupBy(x => new
                                                             {
                                                                 x.returned.ItemCode,

                                                             }).Select(x => new DtoBorrowedIssue
                                                             {

                                                                 ItemCode = x.Key.ItemCode,
                                                                 ReturnQuantity = x.Sum(x => x.returned.Quantity) - x.Sum(x => x.itemconsume.Consume),
                                                                 ConsumeQuantity = x.Sum(x => x.itemconsume.Consume),
                                                                 Quantity = x.Sum(x => x.returned.Quantity)

                                                             });



            var getConsumeBorrow = getBorrowedIssue
                .AsNoTrackingWithIdentityResolution()
                .GroupJoin(getReturnedBorrow, borrow => borrow.ItemCode, returned => returned.ItemCode, (borrow, returned) => new { borrow, returned })
                .SelectMany(x => x.returned.DefaultIfEmpty(), (x, returned) => new { x.borrow, returned })
                .GroupBy(x => new
                {

                    x.borrow.ItemCode,

                }).Select(x => new DtoBorrowedIssue
                {
                    ItemCode = x.Key.ItemCode,
                    ConsumeQuantity = x.Sum(x => x.borrow.Quantity) - x.Sum(x => x.returned.ReturnQuantity != null ? x.returned.ReturnQuantity : 0)

                });

         var fuelRegister = _context.FuelRegisterDetails
        .AsNoTrackingWithIdentityResolution()
        .Include(m => m.Material)
        .Where(fr => fr.Is_Active == true)
        .GroupBy(fr => new
        {
            fr.Material.ItemCode,

        }).Select(fr => new
        {
            itemCode = fr.Key.ItemCode,
            Quantity = fr.Sum(fr => fr.Liters != null ? fr.Liters : 0)

        });


            var getWarehouseStock = _context.WarehouseReceived.Where(x => x.IsActive == true)
                                                              .GroupBy(x => new
                                                              {
                                                                  x.ItemCode,

                                                              }).Select(x => new WarehouseInventory
                                                              {

                                                                  ItemCode = x.Key.ItemCode,
                                                                  ActualGood = x.Sum(x => x.ActualGood)

                                                              });


            var getOrderingReserve = _context.Orders.Where(x => x.IsActive == true)
                                                      .Where(x => x.PreparedDate != null)
                                                    .GroupBy(x => new
                                                    {
                                                        x.ItemCode,

                                                    }).Select(x => new OrderingInventory
                                                    {
                                                        ItemCode = x.Key.ItemCode,
                                                        QuantityOrdered = x.Sum(x => x.QuantityOrdered),

                                                    });


            var getSOH = (from warehouse in getWarehouseStock
                          join issue in getIssueOut
                          on warehouse.ItemCode equals issue.ItemCode
                          into leftJ1
                          from issue in leftJ1.DefaultIfEmpty()

                          join moveorder in getMoveOrderOut
                          on warehouse.ItemCode equals moveorder.ItemCode
                          into leftJ2
                          from moveorder in leftJ2.DefaultIfEmpty()

                          join borrowed in getBorrowedIssue
                          on warehouse.ItemCode equals borrowed.ItemCode
                          into leftJ3
                          from borrowed in leftJ3.DefaultIfEmpty()

                          join returned in getReturnedBorrow
                          on warehouse.ItemCode equals returned.ItemCode
                          into leftJ4
                          from returned in leftJ4.DefaultIfEmpty()

                          join fuel in fuelRegister
                          on warehouse.ItemCode equals fuel.itemCode
                          into leftJ5
                          from fuel in leftJ5.DefaultIfEmpty()

                          group new
                          {

                              warehouse,
                              moveorder,
                              issue,
                              borrowed,
                              returned,
                              fuel

                          }
                          by new
                          {
                              warehouse.ItemCode

                          } into total

                          select new DtoSOH
                          {

                              ItemCode = total.Key.ItemCode,
                              SOH = total.Sum(x => x.warehouse.ActualGood != null ? x.warehouse.ActualGood : 0) +
                             total.Sum(x => x.returned.ReturnQuantity != null ? x.returned.ReturnQuantity : 0) -
                             total.Sum(x => x.issue.Quantity != null ? x.issue.Quantity : 0) -
                             total.Sum(x => x.borrowed.Quantity != null ? x.borrowed.Quantity : 0) -
                             total.Sum(x => x.moveorder.QuantityOrdered != null ? x.moveorder.QuantityOrdered : 0) -
                             total.Sum(x => x.fuel.Quantity.Value)

                          });

            var getReserve = (from warehouse in getWarehouseStock
                              join ordering in getOrderingReserve
                              on warehouse.ItemCode equals ordering.ItemCode
                              into leftJ1
                              from ordering in leftJ1.DefaultIfEmpty()

                              join issue in getIssueOut
                              on warehouse.ItemCode equals issue.ItemCode
                              into leftJ2
                              from issue in leftJ2.DefaultIfEmpty()

                              join borrowed in getBorrowedIssue
                              on warehouse.ItemCode equals borrowed.ItemCode
                              into leftJ3
                              from borrowed in leftJ3.DefaultIfEmpty()

                              join returned in getReturnedBorrow
                              on warehouse.ItemCode equals returned.ItemCode
                              into leftJ4
                              from returned in leftJ4.DefaultIfEmpty()

                              join fuel in fuelRegister
                              on warehouse.ItemCode equals fuel.itemCode
                              into leftJ5
                              from fuel in leftJ5.DefaultIfEmpty()

                              group new
                              {

                                  warehouse,
                                  ordering,
                                  issue,
                                  borrowed,
                                  returned,
                                  fuel


                              } by new
                              {

                                  warehouse.ItemCode

                              } into total

                              select new ReserveInventory
                              {

                                  ItemCode = total.Key.ItemCode,
                                  Reserve = total.Sum(x => x.warehouse.ActualGood != null ? x.warehouse.ActualGood : 0)
                                  + total.Sum(x => x.returned.ReturnQuantity != null ? x.returned.ReturnQuantity : 0) 
                                  - total.Sum(x => x.ordering.QuantityOrdered != null ? x.ordering.QuantityOrdered : 0)
                                  - total.Sum(x => x.issue.Quantity != null ? x.issue.Quantity : 0)
                                  - total.Sum(x => x.borrowed.Quantity != null ? x.borrowed.Quantity : 0) -
                                    total.Sum(x => x.fuel.Quantity.Value)

                              });


            var getSuggestedPo = (from posummary in getPoSummary
                                  join receive in getWarehouseIn
                                  on posummary.ItemCode equals receive.ItemCode
                                  into leftJ1
                                  from receive in leftJ1.DefaultIfEmpty()

                                  group new
                                  {
                                      posummary,
                                      receive,
                                  }
                                  by new
                                  {
                                      posummary.ItemCode,

                                  } into total
                                  select new DtoPoSummaryInventory
                                  {

                                      ItemCode = total.Key.ItemCode,
                                      Ordered = total.Sum(x => x.posummary.Ordered != null ? x.posummary.Ordered : 0) -
                                                total.Sum(x => x.receive.ActualGood != null ? x.receive.ActualGood : 0)

                                  });

            var getMiscellaneousIssuePerMonth = _context.MiscellaneousIssueDetail.Where(x => x.PreparedDate >= StartDate && x.PreparedDate <= EndDate)
                                                                                 .Where(x => x.IsActive == true)
                                                                                 .GroupBy(x => new
                                                                                 {

                                                                                     x.ItemCode,

                                                                                 }).Select(x => new DtoIssueInventory
                                                                                 {
                                                                                     ItemCode = x.Key.ItemCode,
                                                                                     Quantity = x.Sum(x => x.Quantity),
                                                                                 });


            var getMoveOrderoutPerMonth = _context.MoveOrders.Where(x => x.PreparedDate >= StartDate && x.PreparedDate <= EndDate)
                                                              .Where(x => x.IsActive == true)
                                                              .Where(x => x.IsPrepared == true)
                                                              .GroupBy(x => new
                                                              {
                                                                  x.ItemCode,

                                                              }).Select(x => new MoveOrderInventory
                                                              {
                                                                  ItemCode = x.Key.ItemCode,
                                                                  QuantityOrdered = x.Sum(x => x.QuantityOrdered)

                                                              });


            var getConsumedPerMonth = _context.BorrowedIssueDetails.Where(x => x.IsActive == true && x.IsApprovedReturned == true && x.IsReturned == true)
                                                                     .GroupJoin(consumed, returned => returned.Id, itemconsume => itemconsume.BorrowedItemPkey, (returned, itemconsume) => new { returned, itemconsume })
                                                                     .SelectMany(x => x.itemconsume.DefaultIfEmpty(), (x, itemconsume) => new { x.returned, itemconsume })
                                                                      .GroupBy(x => new
                                                                      {

                                                                          x.returned.ItemCode,

                                                                      }).Select(x => new DtoBorrowedIssue
                                                                      {

                                                                          ItemCode = x.Key.ItemCode,
                                                                          Quantity = x.Sum(x => x.returned.Quantity),
                                                                          ReturnQuantity = x.Sum(x => x.returned.Quantity)
                                                                          - x.Sum(x => x.itemconsume.Consume),
                                                                      });

            var getBorrowedOutPerMonth = _context.BorrowedIssueDetails
                .AsNoTrackingWithIdentityResolution()
                .Where(x => x.PreparedDate >= StartDate && x.PreparedDate <= EndDate)
                .Where(x => x.IsActive == true)
                .GroupJoin(getConsumedPerMonth, returned => returned.ItemCode, itemconsume => itemconsume.ItemCode, 
                (returned, itemconsume) => new { returned, itemconsume })
                .SelectMany(x => x.itemconsume.DefaultIfEmpty(), (x, itemconsume) => new { x.returned, itemconsume })
                .GroupBy(x => new
                {
                    x.returned.ItemCode,

                }).Select(x => new DtoBorrowedIssue
                {
                    ItemCode = x.Key.ItemCode,
                    Quantity = x.Sum(x => x.returned.Quantity) - x.Sum(x => x.itemconsume.ReturnQuantity)

                });



            var getAvarageIssuance = (from warehouse in getWarehouseStock
                                      join moveorder in getMoveOrderoutPerMonth
                                      on warehouse.ItemCode equals moveorder.ItemCode
                                      into leftJ1
                                      from moveorder in leftJ1.DefaultIfEmpty()

                                      join borrowed in getBorrowedOutPerMonth
                                      on warehouse.ItemCode equals borrowed.ItemCode
                                      into leftJ2
                                      from borrowed in leftJ2.DefaultIfEmpty()

                                      join issue in getMiscellaneousIssuePerMonth
                                      on warehouse.ItemCode equals issue.ItemCode
                                      into leftJ3
                                      from issue in leftJ3.DefaultIfEmpty()

                                      group new
                                      {
                                          warehouse,
                                          borrowed,
                                          moveorder,
                                          issue
                                      }
                                      by new
                                      {
                                          warehouse.ItemCode

                                      } into total
                                      select new WarehouseInventory
                                      {

                                          ItemCode = total.Key.ItemCode,
                                          ActualGood = (total.Sum(x => x.borrowed.Quantity != null ? x.borrowed.Quantity : 0) + total.Sum(x => x.issue.Quantity != null ? x.issue.Quantity : 0) + total.Sum(x => x.moveorder.QuantityOrdered != null ? x.moveorder.QuantityOrdered : 0)) / 30

                                      });

            var getReseverUsage = (from warehouse in getWarehouseStock
                                   join ordering in getOrderingReserve
                                   on warehouse.ItemCode equals ordering.ItemCode
                                   into leftJ1
                                   from ordering in leftJ1.DefaultIfEmpty()

                                   group new
                                   {
                                       warehouse,
                                       ordering

                                   }
                                   by new
                                   {
                                       warehouse.ItemCode,

                                   } into total
                                   select new ReserveInventory
                                   {
                                       ItemCode = total.Key.ItemCode,
                                       Reserve = total.Sum(x => x.ordering.QuantityOrdered == null ? 0 : x.ordering.QuantityOrdered)

                                   });


            var getWarehouseStockById = _context.WarehouseReceived
                .AsNoTracking()
                .Select(x => new WarehouseInventory
                {

                    WarehouseId = x.Id,
                    ItemCode = x.ItemCode,
                    UnitPrice = x.UnitPrice,
                    ActualGood = x.ActualGood

                });





            var getMoveOrderOutid = _context.MoveOrders
                .AsNoTracking()
                .Where(x => x.IsActive == true)
                                                  .Where(x => x.IsPrepared == true)
                                                  .GroupBy(x => new
                                                  {
                                                      x.WarehouseId,
                                                      x.ItemCode,

                                                  }).Select(x => new MoveOrderInventory
                                                  {
                                                      WarehouseId = x.Key.WarehouseId,
                                                      ItemCode = x.Key.ItemCode,
                                                      QuantityOrdered = x.Sum(x => x.QuantityOrdered),

                                                  });


            var getIssueOutId = _context.MiscellaneousIssueDetail
               .AsNoTracking()
                                                              .Where(x => x.IsActive == true)
                                                              .GroupBy(x => new
                                                              {
                                                                  x.WarehouseId,
                                                                  x.ItemCode,

                                                              }).Select(x => new DtoIssueInventory
                                                              {
                                                                  WarehouseId = x.Key.WarehouseId,
                                                                  ItemCode = x.Key.ItemCode,
                                                                  Quantity = x.Sum(x => x.Quantity),

                                                              });


            var getBorrowedIssueId = _context.BorrowedIssueDetails.AsNoTracking()
                                                       .Where(x => x.IsActive == true)
                                                       .GroupBy(x => new
                                                       {
                                                           x.WarehouseId,
                                                           x.ItemCode,

                                                       }).Select(x => new DtoBorrowedIssue
                                                       {
                                                           WarehouseId = x.Key.WarehouseId,
                                                           ItemCode = x.Key.ItemCode,
                                                           Quantity = x.Sum(x => x.Quantity),

                                                       });



            var getReturnedBorrowId = _context.BorrowedIssueDetails
                .AsNoTrackingWithIdentityResolution().Where(x => x.IsActive == true)
                                                                 .Where(x => x.IsReturned == true)
                                                                 .Where(x => x.IsApprovedReturned == true)
                                                                 .GroupJoin(consumed, returned => returned.Id, itemconsume => itemconsume.BorrowedItemPkey, (returned, itemconsume) => new { returned, itemconsume })
                                                             .SelectMany(x => x.itemconsume.DefaultIfEmpty(), (x, itemconsume) => new { x.returned, itemconsume })
                                                                 .GroupBy(x => new
                                                                 {
                                                                     x.returned.WarehouseId,
                                                                     x.returned.ItemCode,

                                                                 }).Select(x => new DtoBorrowedIssue
                                                                 {
                                                                     WarehouseId = x.Key.WarehouseId,
                                                                     ItemCode = x.Key.ItemCode,
                                                                     ReturnQuantity = x.Sum(x => x.returned.Quantity) - x.Sum(x => x.itemconsume.Consume)

                                                                 });

            var getfuelRegister = _context.FuelRegisterDetails
           .AsNoTrackingWithIdentityResolution()
           .Include(x => x.Material)
           .Where(fr => fr.Is_Active == true)
           .GroupBy(fr => new
           {
               fr.Material.ItemCode,
               fr.Warehouse_ReceivingId,

           }).Select(fr => new
           {
               itemCode = fr.Key.ItemCode,
               WarehouseId = fr.Key.Warehouse_ReceivingId,
               Quantity = fr.Sum(fr => fr.Liters != null ? fr.Liters : 0)

           });

            var getUnitPrice = (from warehouse in getWarehouseStockById
                                join moveorder in getMoveOrderOutid
                                on warehouse.WarehouseId equals moveorder.WarehouseId
                                into leftJ1
                                from moveorder in leftJ1.DefaultIfEmpty()

                                join issue in getIssueOutId
                                on warehouse.WarehouseId equals issue.WarehouseId
                                into leftJ2
                                from issue in leftJ2.DefaultIfEmpty()

                                join borrow in getBorrowedIssueId
                                on warehouse.WarehouseId equals borrow.WarehouseId
                                into leftJ3
                                from borrow in leftJ3.DefaultIfEmpty()

                                join returned in getReturnedBorrowId
                                on warehouse.WarehouseId equals returned.WarehouseId
                                into leftJ4
                                from returned in leftJ4.DefaultIfEmpty()

                                join fuel in getfuelRegister
                                on warehouse.WarehouseId equals fuel.WarehouseId
                                into leftJ5
                                from fuel in leftJ5.DefaultIfEmpty()

                                group new
                                {
                                    warehouse,
                                    moveorder,
                                    issue,
                                    borrow,
                                    returned,
                                    fuel

                                }

                              by new
                              {
                                  warehouse.WarehouseId,
                                  warehouse.ItemCode,

                              }

                                into x
                                select new WarehouseInventory
                                {

                                    WarehouseId = x.Key.WarehouseId,
                                    ItemCode = x.Key.ItemCode,
                                    UnitPrice = x.First().warehouse.UnitPrice * (x.First().warehouse.ActualGood  + x.Sum(x => x.returned.ReturnQuantity) 
                                    - x.Sum(x => x.moveorder.QuantityOrdered) - x.Sum(x => x.issue.Quantity) - x.Sum(x => x.borrow.Quantity)),
                                    ActualGood = x.First().warehouse.ActualGood + (x.Sum(x => x.returned.ReturnQuantity)
                                    - x.Sum(x => x.moveorder.QuantityOrdered) - x.Sum(x => x.issue.Quantity)
                                    - x.Sum(x => x.borrow.Quantity) - x.Sum(x => x.fuel.Quantity.Value))

                                }).Where(x => x.UnitPrice > 0 && x.ActualGood > 0);


            var getUnitpriceTotal = getUnitPrice  
                .GroupBy(x => new
            {
                x.ItemCode,

            }).Select(x => new WarehouseInventory
            {
                ItemCode = x.Key.ItemCode,
                UnitPrice = x.Sum(x => x.UnitPrice != null ? x.UnitPrice : 0) / x.Sum(x => x.ActualGood != null ? x.ActualGood : 0),
                ActualGood = x.Sum(x => x.ActualGood),
                TotalUnitPrice = x.Sum(x => x.UnitPrice)


            });

            var inventory = (from material in _context.Materials
                             where material.IsActive == true

                             join posummary in getPoSummary
                              on material.ItemCode equals posummary.ItemCode
                              into leftJ1
                             from posummary in leftJ1.DefaultIfEmpty()

                             join warehouse in getWarehouseIn
                             on material.ItemCode equals warehouse.ItemCode
                             into leftJ2
                             from warehouse in leftJ2.DefaultIfEmpty()

                             join moveorders in getMoveOrderOut
                             on material.ItemCode equals moveorders.ItemCode
                             into leftJ3
                             from moveorders in leftJ3.DefaultIfEmpty()

                             join receiptIn in getReceiptIn
                             on material.ItemCode equals receiptIn.ItemCode
                             into leftJ4
                             from receiptin in leftJ4.DefaultIfEmpty()

                             join issueout in getIssueOut
                             on material.ItemCode equals issueout.ItemCode
                             into leftJ6
                             from issueOut in leftJ6.DefaultIfEmpty()

                             join SOH in getSOH
                             on material.ItemCode equals SOH.ItemCode
                             into leftJ7
                             from SOH in leftJ7.DefaultIfEmpty()

                             join consume in getReturnedBorrow
                             on material.ItemCode equals consume.ItemCode
                             into leftJ8
                             from consume in leftJ8.DefaultIfEmpty()

                             join returned in getReturnedBorrow
                             on material.ItemCode equals returned.ItemCode
                             into leftJ9
                             from returned in leftJ9.DefaultIfEmpty()

                             join reserve in getReserve
                             on material.ItemCode equals reserve.ItemCode
                             into leftJ10
                             from reserve in leftJ10.DefaultIfEmpty()

                             join sudggest in getSuggestedPo
                             on material.ItemCode equals sudggest.ItemCode
                             into leftJ11
                             from sudggest in leftJ11.DefaultIfEmpty()

                             join averageissuance in getAvarageIssuance
                             on material.ItemCode equals averageissuance.ItemCode
                             into leftJ12
                             from averageissuance in leftJ12.DefaultIfEmpty()

                             join usage in getReseverUsage
                             on material.ItemCode equals usage.ItemCode
                             into leftJ13
                             from usage in leftJ13.DefaultIfEmpty()

                             join unitprice in getUnitpriceTotal
                             on material.ItemCode equals unitprice.ItemCode
                             into leftJ14
                             from unitprice in leftJ14.DefaultIfEmpty()

                             join borrow in getBorrowedForMrp
                             on material.ItemCode equals borrow.ItemCode
                             into leftJ15
                             from borrow in leftJ15.DefaultIfEmpty()

                             join notTransact in getNotTransacted
                             on material.ItemCode equals notTransact.ItemCode
                             into leftJ16
                             from notTransact in leftJ16.DefaultIfEmpty()

                             join fuel in fuelRegister
                             on material.ItemCode equals fuel.itemCode
                             into leftJ17
                             from fuel in leftJ17.DefaultIfEmpty()

                             orderby material.ItemCode ascending

                             group new
                             {

                                 posummary,
                                 warehouse,
                                 moveorders,
                                 receiptin,
                                 issueOut,
                                 SOH,
                                 borrow,
                                 returned,
                                 consume,
                                 reserve,
                                 sudggest,
                                 averageissuance,
                                 usage,
                                 unitprice,
                                 notTransact,
                                 fuel,


                             }
                             by new
                             {
                                 material.Id,
                                 material.ItemCode,
                                 material.ItemDescription,
                                 material.Uom.UomCode,
                                 material.ItemCategory.ItemCategoryName,
                                 material.BufferLevel,
                                 UnitPrice = unitprice.UnitPrice != null ? unitprice.UnitPrice : 0,
                                 sudggest = sudggest.Ordered != null ? sudggest.Ordered : 0,
                                 warehouseActualGood = warehouse.ActualGood != null ? warehouse.ActualGood : 0,
                                 receiptin = receiptin.Quantity != null ? receiptin.Quantity : 0,
                                 moveorders = moveorders.QuantityOrdered != null ? moveorders.QuantityOrdered : 0,
                                 issueOut = issueOut.Quantity != null ? issueOut.Quantity : 0,
                                 borrow = borrow.Quantity != null ? borrow.Quantity : 0,
                                 returned = returned.ReturnQuantity != null ? returned.ReturnQuantity : 0,
                                 TotalPrice = unitprice.TotalUnitPrice != null ? unitprice.TotalUnitPrice : 0,
                                 SOH = SOH.SOH != null ? SOH.SOH : 0,
                                 reserve = reserve.Reserve != null ? reserve.Reserve : 0,
                                 averageissuance = averageissuance.ActualGood != null ? averageissuance.ActualGood : 0,
                                 usage = usage.Reserve != null ? usage.Reserve : 0,
                                 ConsumeQuantity = consume.ConsumeQuantity != null ? consume.ConsumeQuantity : 0,
                                 Fuel = fuel.Quantity != null ? fuel.Quantity : 0,

                             }

                              into total
                             select new DtoMRP

                             {
                                 Id = total.Key.Id,
                                 ItemCode = total.Key.ItemCode,
                                 ItemDescription = total.Key.ItemDescription,
                                 Uom = total.Key.UomCode,
                                 ItemCategory = total.Key.ItemCategoryName,
                                 BufferLevel = total.Key.BufferLevel,
                                 UnitCost = Math.Round(total.Key.UnitPrice, 2),
                                 ReceiveIn = total.Key.warehouseActualGood,
                                 MoveOrderOut = total.Key.moveorders,
                                 ReceiptIn = total.Key.receiptin,
                                 IssueOut = total.Key.issueOut,
                                 BorrowedOut = total.Key.borrow,
                                 ReturnedBorrowed = total.Key.returned,
                                 BorrowConsume = total.Key.ConsumeQuantity,
                                 FuelRegistration = total.Key.Fuel.Value,
                                 TotalCost = Math.Round(total.Key.TotalPrice, 2),
                                 SOH = total.Key.SOH,
                                 PreparedQuantity = total.First().notTransact.QuantityOrdered != null ? total.First().notTransact.QuantityOrdered : 0,
                                 Reserve = total.Key.reserve,
                                 SuggestedPo = total.Key.sudggest >= 0 ? total.Key.sudggest : 0,
                                 AverageIssuance = Math.Round(total.Key.averageissuance, 2),
                                 ReserveUsage = total.Key.usage,
                                 DaysLevel = total.Key.averageissuance != 0 ? (int)((total.Key.reserve) / Math.Round(total.Key.averageissuance, 2)) : (int)total.Key.reserve,

                             });


            if(!string.IsNullOrEmpty(search)) 
            {
                inventory = inventory.Where(x => x.ItemCode.ToLower().Contains(search.Trim().ToLower())
                || x.ItemDescription.ToLower().Contains(search.Trim().ToLower()));
            }

            inventory = inventory.OrderBy(x => x.ItemCode);

            return await PagedList<DtoMRP>.CreateAsync(inventory, userParams.PageNumber, userParams.PageSize);
        }



        public async Task<IReadOnlyList<DtoYmirSOHList>> YmirSOHList(string itemCode)
        {
            var EndDate = DateTime.Now;
            var StartDate = EndDate.AddDays(-30);

            var getWarehouseStock = _context.WarehouseReceived
                .AsNoTrackingWithIdentityResolution()
                .Where(x => x.IsActive == true)
                                                  .GroupBy(x => new
                                                  {
                                                      x.ItemCode,

                                                  }).Select(x => new WarehouseInventory
                                                  {
                                                      ItemCode = x.Key.ItemCode,
                                                      ActualGood = x.Sum(x => x.ActualGood)

                                                  });

            var getReserve = _context.Orders
                .AsNoTrackingWithIdentityResolution()
                .Where(x => x.IsActive == true)
                                         .Where(x => x.PreparedDate != null)
                                         .GroupBy(x => new
                                         {

                                             x.ItemCode,

                                         }).Select(x => new MoveOrderInventory
                                         {
                                             ItemCode = x.Key.ItemCode,
                                             QuantityOrdered = x.Sum(x => x.QuantityOrdered)

                                         });


            var getIssueOut = _context.MiscellaneousIssueDetail
                .AsNoTrackingWithIdentityResolution()
                .Where(x => x.IsActive == true)
                                                 .GroupBy(x => new
                                                 {
                                                     x.ItemCode

                                                 }).Select(x => new DtoMiscIssue
                                                 {

                                                     ItemCode = x.Key.ItemCode,
                                                     Quantity = x.Sum(x => x.Quantity)

                                                 });


            var getBorrowedOut = _context.BorrowedIssueDetails.AsNoTrackingWithIdentityResolution().Where(x => x.IsActive == true)
                                                              .GroupBy(x => new
                                                              {
                                                                  x.ItemCode,

                                                              }).Select(x => new DtoBorrowedIssue
                                                              {

                                                                  ItemCode = x.Key.ItemCode,
                                                                  Quantity = x.Sum(x => x.Quantity)

                                                              });

            var consumed = _context.BorrowedConsumes
                .AsNoTrackingWithIdentityResolution()
                .Where(x => x.IsActive)
                               .GroupBy(x => new
                               {
                                   x.ItemCode,
                                   x.BorrowedItemPkey

                               }).Select(x => new ItemStocksDto
                               {
                                   ItemCode = x.Key.ItemCode,
                                   BorrowedItemPkey = x.Key.BorrowedItemPkey,
                                   Consume = x.Sum(x => x.Consume != null ? x.Consume : 0)

                               });

            var getReturned = _context.BorrowedIssueDetails
                .AsNoTrackingWithIdentityResolution()
                .Where(x => x.IsActive == true)
                                                            .Where(x => x.IsReturned == true)
                                                            .Where(x => x.IsApprovedReturned == true)
                                                            .GroupJoin(consumed, returned => returned.Id, consume => consume.BorrowedItemPkey, (returned, consume) => new { returned, consume })
                                                            .SelectMany(x => x.consume.DefaultIfEmpty(), (x, consume) => new { x.returned, consume })
                                                             .GroupBy(x => new
                                                             {

                                                                 x.returned.ItemCode,

                                                             }).Select(x => new DtoBorrowedIssue
                                                             {

                                                                 ItemCode = x.Key.ItemCode,
                                                                 ReturnQuantity = x.Sum(x => x.returned.Quantity) - x.Sum(x => x.consume.Consume)

                                                             });

          var fuelRegister = _context.FuelRegisterDetails
        .Include(m => m.Material)
        .Where(fr => fr.Is_Active == true)
        .GroupBy(fr => new
        {
            fr.Material.ItemCode,

        }).Select(fr => new
        {
            itemCode = fr.Key.ItemCode,
            Quantity = fr.Sum(fr => fr.Liters.Value)

        });



            var getSOH = (from warehouse in getWarehouseStock
                          join reserve in getReserve
                          on warehouse.ItemCode equals reserve.ItemCode
                          into leftJ1
                          from reserve in leftJ1.DefaultIfEmpty()

                          join issue in getIssueOut
                          on warehouse.ItemCode equals issue.ItemCode
                          into leftJ2
                          from issue in leftJ2.DefaultIfEmpty()

                          join borrowed in getBorrowedOut
                          on warehouse.ItemCode equals borrowed.ItemCode
                          into leftJ3
                          from borrowed in leftJ3.DefaultIfEmpty()

                          join returned in getReturned
                          on warehouse.ItemCode equals returned.ItemCode
                          into leftJ4
                          from returned in leftJ4.DefaultIfEmpty()


                          join fuel in fuelRegister
                          on warehouse.ItemCode equals fuel.itemCode
                          into leftJ5
                          from fuel in leftJ5.DefaultIfEmpty()

                          group new
                          {

                              warehouse,
                              reserve,
                              issue,
                              borrowed,
                              returned,
                              fuel

                          }

                          by new
                          {
                              warehouse.ItemCode
                          }

                          into total

                          select new DtoSOH
                          {

                              ItemCode = total.Key.ItemCode,
                              SOH = total.Sum(x => x.warehouse.ActualGood != null ? x.warehouse.ActualGood : 0) +
                             total.Sum(x => x.returned.ReturnQuantity != null ? x.returned.ReturnQuantity : 0) -
                             total.Sum(x => x.issue.Quantity != null ? x.issue.Quantity : 0) -
                             total.Sum(x => x.borrowed.Quantity != null ? x.borrowed.Quantity : 0) -
                             total.Sum(x => x.reserve.QuantityOrdered != null ? x.reserve.QuantityOrdered : 0) -
                             total.Sum(x => x.fuel.Quantity != null ? x.fuel.Quantity : 0)
                          });


            var getMiscellaneousIssuePerMonth = _context.MiscellaneousIssueDetail
                .AsNoTrackingWithIdentityResolution()
                .Where(x => x.PreparedDate >= StartDate && x.PreparedDate <= EndDate)
                                                                                 .Where(x => x.IsActive == true)
                                                                                 .GroupBy(x => new
                                                                                 {
                                                                                     x.ItemCode,

                                                                                 }).Select(x => new DtoIssueInventory
                                                                                 {
                                                                                     ItemCode = x.Key.ItemCode,
                                                                                     Quantity = x.Sum(x => x.Quantity),
                                                                                 });


            var getMoveOrderoutPerMonth = _context.MoveOrders.AsNoTrackingWithIdentityResolution().Where(x => x.PreparedDate >= StartDate && x.PreparedDate <= EndDate)
                                                              .Where(x => x.IsActive == true)
                                                              .Where(x => x.IsPrepared == true)
                                                              .GroupBy(x => new
                                                              {
                                                                  x.ItemCode,

                                                              }).Select(x => new MoveOrderInventory
                                                              {
                                                                  ItemCode = x.Key.ItemCode,
                                                                  QuantityOrdered = x.Sum(x => x.QuantityOrdered)

                                                              });


            var getConsumedPerMonth = _context.BorrowedIssueDetails.AsNoTrackingWithIdentityResolution().Where(x => x.IsActive == true && x.IsApprovedReturned == true && x.IsReturned == true)
                                                                     .GroupJoin(consumed, returned => returned.Id, itemconsume => itemconsume.BorrowedItemPkey, (returned, itemconsume) => new { returned, itemconsume })
                                                                     .SelectMany(x => x.itemconsume.DefaultIfEmpty(), (x, itemconsume) => new { x.returned, itemconsume })
                                                                      //.Where(x => x.IsApproved == false)
                                                                      .GroupBy(x => new
                                                                      {

                                                                          x.returned.ItemCode,
                                                                          //x.returned.PreparedDate

                                                                      }).Select(x => new DtoBorrowedIssue
                                                                      {

                                                                          ItemCode = x.Key.ItemCode,
                                                                          Quantity = x.Sum(x => x.returned.Quantity),
                                                                          ReturnQuantity = x.Sum(x => x.returned.Quantity)
                                                                          - x.Sum(x => x.itemconsume.Consume),
                                                                          //PreparedDate = x.Key.PreparedDate.ToString()


                                                                      });

            var getBorrowedOutPerMonth = _context.BorrowedIssueDetails
               .AsNoTrackingWithIdentityResolution()
                .Where(x => x.PreparedDate >= StartDate && x.PreparedDate <= EndDate)
                .Where(x => x.IsActive == true)
                .GroupJoin(getConsumedPerMonth, returned => returned.ItemCode, itemconsume => itemconsume.ItemCode, (returned, itemconsume) => new { returned, itemconsume })
                .SelectMany(x => x.itemconsume.DefaultIfEmpty(), (x, itemconsume) => new { x.returned, itemconsume })
                .GroupBy(x => new
                {

                    x.returned.ItemCode,

                }).Select(x => new DtoBorrowedIssue
                {
                    ItemCode = x.Key.ItemCode,
                    Quantity = x.Sum(x => x.returned.Quantity) - x.Sum(x => x.itemconsume.ReturnQuantity)

                });








            var getAvarageIssuance = (from warehouse in getWarehouseStock
                                      join moveorder in getMoveOrderoutPerMonth
                                      on warehouse.ItemCode equals moveorder.ItemCode
                                      into leftJ1
                                      from moveorder in leftJ1.DefaultIfEmpty()

                                      join borrowed in getBorrowedOutPerMonth
                                      on warehouse.ItemCode equals borrowed.ItemCode
                                      into leftJ2
                                      from borrowed in leftJ2.DefaultIfEmpty()

                                      join issue in getMiscellaneousIssuePerMonth
                                      on warehouse.ItemCode equals issue.ItemCode
                                      into leftJ3
                                      from issue in leftJ3.DefaultIfEmpty()

                                      join fuel in fuelRegister
                                      on warehouse.ItemCode equals fuel.itemCode
                                      into leftJ4
                                      from fuel in leftJ4.DefaultIfEmpty()


                                      group new
                                      {
                                          warehouse,
                                          borrowed,
                                          moveorder,
                                          issue,
                                          fuel,
                                      }
                                      by new
                                      {
                                          warehouse.ItemCode

                                      } 
                                      into total
                                      select new WarehouseInventory
                                      {

                                          ItemCode = total.Key.ItemCode,
                                          ActualGood = (total.Sum(x => x.borrowed.Quantity != null ? x.borrowed.Quantity : 0)
                                          + total.Sum(x => x.issue.Quantity != null ? x.issue.Quantity : 0)
                                          + total.Sum(x => x.moveorder.QuantityOrdered != null ? x.moveorder.QuantityOrdered : 0))
                                          + total.Sum(x => x.fuel.Quantity != null ? x.fuel.Quantity : 0)/ 30

                                      });


            var report = _context.Materials
                .AsNoTracking()
                .Where(x => x.IsActive == true)
                .Where(x => x.ItemCode == itemCode)
                .GroupJoin(getSOH, material => material.ItemCode, soh => soh.ItemCode, (material, soh) => new { material, soh })
                .SelectMany(x => x.soh.DefaultIfEmpty(), (x, soh) => new { x.material, soh })
                .GroupJoin(getAvarageIssuance, material => material.material.ItemCode, issuance => issuance.ItemCode, (material, issuance) => new { material, issuance })
                .SelectMany(x => x.issuance.DefaultIfEmpty(), (x, issuance) => new { x.material, issuance })
                .GroupBy(x => x.material.material.ItemCode)
               .Select(x => new DtoYmirSOHList
               {
                    id = x.Key,
                    ItemDescription = x.First().material.material.ItemDescription,
                    BufferLevel = x.First().material.material.BufferLevel,
                    Reserve = x.First().material.soh.SOH != null ? x.First().material.soh.SOH : 0 ,
                    AverageIssuance = decimal.Round( x.First().issuance.ActualGood != null ? x.First().issuance.ActualGood : 0,2)

                });

            report.OrderBy(x => x.id);

            return await report.ToListAsync();
        }

        public async Task<PagedList<DtoMRP>> GetMRP(UserParams userParams, string search)
        {
            //throw new NotImplementedException();

            var mrp = _context.Materials
                .Where(x => x.IsActive == true)
                .OrderBy(x => x.ItemCode)
                .Select(x => new DtoMRP
                {
                    Id = x.Id,
                    ItemCode = x.ItemCode,
                    ItemDescription = x.ItemDescription,
                    Uom = x.Uom.UomCode,


                    UnitCost = (_context.WarehouseReceived
                                .Where(r => r.IsActive == true && r.TransactionType == "Receiving" && r.ItemCode == x.ItemCode)
                                .Sum(x => x.ActualGood) +
                                _context.WarehouseReceived
                                .AsNoTracking()
                                .Where(mr => mr.IsActive == true && mr.TransactionType == "MiscellaneousReceipt" && mr.ItemCode == x.ItemCode)
                                .Sum(x => x.ActualGood) +
                                _context.BorrowedIssueDetails
                                .AsNoTracking()
                                .Where(rb => rb.IsActive == true && rb.IsReturned == true && rb.IsApprovedReturned == true && rb.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity)) -
                                (_context.MoveOrders
                                .AsNoTracking()
                                .Where(m => m.IsActive == true && m.IsPrepared == true && m.ItemCode == x.ItemCode)
                                .Sum(x => x.QuantityOrdered) +
                                _context.MiscellaneousIssueDetail
                                .AsNoTracking()
                                .Where(mi => mi.IsActive == true && mi.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity) +
                                _context.BorrowedIssueDetails
                                .AsNoTracking()
                                .Where(b => b.IsActive == true && b.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity) +
                                _context.FuelRegisterDetails
                                .Where(fr => fr.Is_Active == true && fr.Material.ItemCode == x.ItemCode)
                                .Sum(x => x.Liters) ?? 0m) == 0 ? 0 :
                                _context.WarehouseReceived
                                .AsNoTracking()
                                .Where(p => p.ItemCode == x.ItemCode)
                                .Select(p => p.UnitPrice)
                                .FirstOrDefault() 

                                ,

                    TotalCost = ((_context.WarehouseReceived
                                .Where(r => r.IsActive == true && r.TransactionType == "Receiving" && r.ItemCode == x.ItemCode)
                                .Sum(x => x.ActualGood)
                                +
                                _context.WarehouseReceived
                                .AsNoTracking()
                                .Where(mr => mr.IsActive == true && mr.TransactionType == "MiscellaneousReceipt" && mr.ItemCode == x.ItemCode)
                                .Sum(x => x.ActualGood)
                                +
                                _context.BorrowedIssueDetails
                                .AsNoTracking()
                                .Where(rb => rb.IsActive == true && rb.IsReturned == true && rb.IsApprovedReturned == true && rb.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity))
                                -
                                (_context.MoveOrders
                                .AsNoTracking()
                                .Where(m => m.IsActive == true && m.IsPrepared == true && m.ItemCode == x.ItemCode)
                                .Sum(x => x.QuantityOrdered)
                                +
                                _context.MiscellaneousIssueDetail
                                .AsNoTracking()
                                .Where(mi => mi.IsActive == true && mi.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity)
                                +
                                _context.BorrowedIssueDetails
                                .AsNoTracking()
                                .Where(b => b.IsActive == true && b.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity)
                                +
                                _context.FuelRegisterDetails
                                .Where(fr => fr.Is_Active == true && fr.Material.ItemCode == x.ItemCode)
                                .Sum(x => x.Liters) ?? 0m))
                                *
                                (_context.Materials
                                .AsNoTracking()
                                .Where(p => p.ItemCode == x.ItemCode)
                                .Select(p => p.ItemCode)
                                .FirstOrDefault() ==
                                _context.WarehouseReceived
                                .AsNoTracking()
                                .Where(p => p.ItemCode == x.ItemCode)
                                .Select(p => p.ItemCode)
                                .FirstOrDefault() ?
                                _context.WarehouseReceived
                                .AsNoTracking()
                                .Where(p => p.ItemCode == x.ItemCode)
                                .Select(p => p.UnitPrice)
                                .FirstOrDefault() : 0),







                    ItemCategory = x.ItemCategory.ItemCategoryName,
                    BufferLevel = x.BufferLevel,
                    SuggestedPo = _context.PoSummaries.Where(s => s.IsActive == true && s.ItemCode == x.ItemCode)
                                .Sum(x => x.Ordered)
                                -
                                _context.WarehouseReceived.Where(r => r.IsActive == true && r.TransactionType == "Receiving" && r.ItemCode == x.ItemCode)
                                .Sum(x => x.ActualGood),
                    ReceiveIn = _context.WarehouseReceived.Where(r => r.IsActive == true && r.TransactionType == "Receiving" && r.ItemCode == x.ItemCode)
                                .Sum(x => x.ActualGood),
                    MoveOrderOut = _context.MoveOrders.AsNoTracking().Where(m => m.IsActive == true && m.IsPrepared == true && m.ItemCode == x.ItemCode)
                                .Sum(x => x.QuantityOrdered),

                    ReceiptIn = _context.WarehouseReceived.AsNoTracking().Where(mr => mr.IsActive == true && mr.TransactionType == "MiscellaneousReceipt" && mr.ItemCode == x.ItemCode)
                                .Sum(x => x.ActualGood),
                    IssueOut = _context.MiscellaneousIssueDetail.AsNoTracking().Where(mi => mi.IsActive == true && mi.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity),
                    BorrowedOut = _context.BorrowedIssueDetails.AsNoTracking().Where(b => b.IsActive == true && b.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity),
                    ReturnedBorrowed = _context.BorrowedIssueDetails.AsNoTracking().Where(rb => rb.IsActive == true && rb.IsReturned == true && rb.IsApprovedReturned == true && rb.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity),
                    BorrowConsume = _context.BorrowedConsumes.AsNoTracking().Where(bc => bc.IsActive && bc.ItemCode == x.ItemCode)
                                .Sum(x => x.Consume),
                    PreparedQuantity = _context.MoveOrders.AsNoTracking().Where(pq => pq.IsActive == true && pq.IsPrepared == true && pq.IsTransact != true && pq.ItemCode == x.ItemCode)
                                 .Sum(x => x.QuantityOrdered),
                    FuelRegistration = _context.FuelRegisterDetails
                        .Where(fr => fr.Is_Active == true && fr.Material.ItemCode == x.ItemCode)
                        .Sum(x => x.Liters) ?? 0m,
                    ReserveUsage = _context.Orders.Where(ru => ru.IsActive == true && ru.PreparedDate != null && ru.ItemCode == x.ItemCode)
                        .Sum(x => x.QuantityOrdered),
                    SOH = (_context.WarehouseReceived.Where(r => r.IsActive == true && r.TransactionType == "Receiving" && r.ItemCode == x.ItemCode)
                                .Sum(x => x.ActualGood) +
                        _context.WarehouseReceived.AsNoTracking().Where(mr => mr.IsActive == true && mr.TransactionType == "MiscellaneousReceipt" && mr.ItemCode == x.ItemCode)
                                .Sum(x => x.ActualGood) +
                        _context.BorrowedIssueDetails.AsNoTracking().Where(rb => rb.IsActive == true && rb.IsReturned == true && rb.IsApprovedReturned == true && rb.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity)) -
                        (_context.MoveOrders.AsNoTracking().Where(m => m.IsActive == true && m.IsPrepared == true && m.ItemCode == x.ItemCode)
                                .Sum(x => x.QuantityOrdered) +
                        _context.MiscellaneousIssueDetail.AsNoTracking().Where(mi => mi.IsActive == true && mi.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity) +
                        _context.BorrowedIssueDetails.AsNoTracking().Where(b => b.IsActive == true && b.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity) +
                        _context.FuelRegisterDetails
                        .Where(fr => fr.Is_Active == true && fr.Material.ItemCode == x.ItemCode)
                        .Sum(x => x.Liters) ?? 0m),
                    Reserve = (_context.WarehouseReceived.Where(r => r.IsActive == true && r.TransactionType == "Receiving" && r.ItemCode == x.ItemCode)
                                .Sum(x => x.ActualGood) +
                        _context.WarehouseReceived.AsNoTracking().Where(mr => mr.IsActive == true && mr.TransactionType == "MiscellaneousReceipt" && mr.ItemCode == x.ItemCode)
                                .Sum(x => x.ActualGood) +
                        _context.BorrowedIssueDetails.AsNoTracking().Where(rb => rb.IsActive == true && rb.IsReturned == true && rb.IsApprovedReturned == true && rb.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity)) -
                        (_context.MoveOrders.AsNoTracking().Where(m => m.IsActive == true && m.IsPrepared == true && m.ItemCode == x.ItemCode)
                                .Sum(x => x.QuantityOrdered) +
                        _context.MiscellaneousIssueDetail.AsNoTracking().Where(mi => mi.IsActive == true && mi.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity) +
                        _context.BorrowedIssueDetails.AsNoTracking().Where(b => b.IsActive == true && b.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity) +
                        _context.FuelRegisterDetails
                        .Where(fr => fr.Is_Active == true && fr.Material.ItemCode == x.ItemCode)
                        .Sum(x => x.Liters) ?? 0m),
                    AverageIssuance = (_context.MoveOrders.AsNoTracking().Where(m => m.IsActive == true && m.IsPrepared == true && m.ItemCode == x.ItemCode)
                                .Sum(x => x.QuantityOrdered) +
                        _context.MiscellaneousIssueDetail.AsNoTracking().Where(mi => mi.IsActive == true && mi.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity) +
                        _context.BorrowedIssueDetails.AsNoTracking().Where(b => b.IsActive == true && b.ItemCode == x.ItemCode)
                                .Sum(x => x.Quantity) +
                        _context.FuelRegisterDetails
                        .Where(fr => fr.Is_Active == true && fr.Material.ItemCode == x.ItemCode)
                        .Sum(x => x.Liters) ?? 0m) / 30,
                    




                }) ;
            //return await mrp.ToListAsync();

            if(!string.IsNullOrEmpty(search))
            {
                mrp = mrp.Where(m => m.ItemCode.Contains(search));
            }

            return await PagedList<DtoMRP>.CreateAsync(mrp, userParams.PageNumber, userParams.PageSize);

        }


        //public async Task<PagedList<DtoMRP>> GetMRPOrig(UserParams userParams, string search)
        //{

        //    var availableMaterials = _context.Materials
        //        .Where(x => x.IsActive == true)
        //        .OrderBy(x => x.ItemCode)
        //        .AsQueryable();

        //    var mrp =  availableMaterials.Select(x => new DtoMRP
        //    {
        //        Id = x.Id,
        //        ItemCode = x.ItemCode,
        //        ItemDescription = x.ItemDescription,
        //        Uom = x.Uom.UomCode,
        //        UnitCost =  CalculationUnitCost(x.ItemCode),
        //    });


        //     async Task<decimal> CalculationUnitCost(string itemCode)
        //    {
        //        var reveivingAG = _context.WarehouseReceived

        //            .Where(r => r.IsActive == true && r.TransactionType == "Receiving" && r.ItemCode == itemCode)
        //            .Sum(r => r.ActualGood);

        //        var miscAG = _context.WarehouseReceived
        //            .AsNoTracking()
        //            .Where(mr => mr.IsActive == true && mr.TransactionType == "MiscellaneousReceipt" && mr.ItemCode == itemCode)
        //            .Sum(x => x.ActualGood);

        //        var returnedQuantity = _context.BorrowedIssueDetails
        //            .AsNoTracking()
        //            .Where(rb => rb.IsActive == true && rb.IsReturned == true && rb.IsApprovedReturned == true && rb.ItemCode == itemCode)
        //            .Sum(x => x.Quantity);

        //        var quantityOrdered = _context.MoveOrders
        //            .AsNoTracking()
        //            .Where(m => m.IsActive == true && m.IsPrepared == true && m.ItemCode == itemCode)
        //            .Sum(x => x.QuantityOrdered);

        //        var miscQuantity = _context.MiscellaneousIssueDetail
        //            .AsNoTracking()
        //            .Where(mi => mi.IsActive == true && mi.ItemCode == itemCode)
        //            .Sum(x => x.Quantity);

        //        var borrowedQuantity = _context.BorrowedIssueDetails
        //            .AsNoTracking()
        //            .Where(b => b.IsActive == true && b.ItemCode == itemCode)
        //            .Sum(x => x.Quantity);

        //        var fuelQuantity = _context.FuelRegisterDetails
        //                .Where(fr => fr.Is_Active == true && fr.Material.ItemCode == itemCode)
        //                .Sum(x => x.Liters) ?? 0m;

        //        var available = reveivingAG + miscAG + returnedQuantity;

        //        var deduction = quantityOrdered + miscQuantity + borrowedQuantity + fuelQuantity;

        //        var total =  available - deduction;

        //        return total == 0 ? 0 : 
        //            await _context.WarehouseReceived
        //            .AsNoTracking()
        //            .Where(p => p.ItemCode == itemCode)
        //            .Select(p => p.UnitPrice)
        //            .FirstOrDefault();
        //    }

        //    if (!string.IsNullOrEmpty(search))
        //    {
        //        mrp = mrp.Where(m => m.ItemCode.Contains(search));
        //    }

        //    return await PagedList<DtoMRP>.CreateAsync(mrp, userParams.PageNumber, userParams.PageSize);
        //}
    }
}
