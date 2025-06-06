﻿using ELIXIRETD.DATA.DATA_ACCESS_LAYER.DTOs.IMPORT_DTO;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.MODELS.IMPORT_MODEL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ELIXIRETD.DATA.CORE.INTERFACES.IMPORT_INTERFACE
{
    public interface IPoSummaryRepository
    {

        Task<bool> AddNewPORequest(PoSummary posummary);

        Task<bool> CheckItemCode(string rawmaterial);
        Task<bool> CheckUomCode(string uom);
        Task<bool> CheckSupplier(string supplier);
        Task<bool> ValidateRRAndItemcodeManual(string ponumber ,string rrNo, string itemcode, decimal quantity);

        Task<bool> ValidatePOAndItemcodeManual(string ponumber, string itemcode, decimal quantity);

        Task<bool> ValidateQuantityOrder(decimal quantity);
        Task<bool> ValidationItemcodeandUom(string itemcode /*,string itemdescription */, string uom);

        //Task<IReadOnlyList>


        Task<bool> ImportBufferLevel(ImportBufferLevelDto itemCode);

        Task<IReadOnlyList<WarehouseOverAllStocksDto>> WarehouseOverAllStocks(string Search);

        Task<bool> AddImportReceipt(ImportMiscReceiptDto items);

        Task<bool> AddImportReceiptToWarehouse(ImportMiscReceiptDto.WarehouseReceiptDto item);




        





    }      
             
}
    
