﻿using ELIXIRETD.DATA.DATA_ACCESS_LAYER.MODELS.SETUP_MODEL;
using ELIXIRETD.DATA.DATA_ACCESS_LAYER.MODELS.WAREHOUSE_MODEL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ELIXIRETD.DATA.DATA_ACCESS_LAYER.DTOs.FUEL_REGISTER_DTO
{
    public class CreateFuelRegisterDto
    {
        public int ? Id { get; set; }

        public string RequestorId { get; set; }
        public string RequestorName { get; set; }

        public int? UserId { get; set; }

        public string Item_Code { get; set; }

        public int? Warehouse_ReceivingId { get; set; }

        public decimal Liters { get; set; }
        public int? AssetId { get; set; }

        public decimal? Odometer { get; set; }


        public string Added_By { get; set; }
        public string Modified_By { get; set; }

        public string Remarks { get; set; }
        public string Approve_By { get; set; }
        public string Company_Code { get; set; }
        public string Company_Name { get; set; }

        public string Department_Code { get; set; }
        public string Department_Name { get; set; }

        public string Location_Code { get; set; }
        public string Location_Name { get; set; }

        public string Account_Title_Code { get; set; }
        public string Account_Title_Name { get; set; }
        public string EmpId { get; set; }

        public string Fullname { get; set; }

        public string Transact_By { get; set; }

    }
}
