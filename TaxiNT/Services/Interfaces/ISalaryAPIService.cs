﻿using TaxiNT.Libraries.Models.GGSheets;

namespace TaxiNT.Services.Interfaces;
public interface ISalaryAPIService
{
    Task<Salary> GetSalary(string userId);
}
