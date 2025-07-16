using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Metadata;
using TaxiNT.Data;
using TaxiNT.Extensions;
using TaxiNT.Libraries.Entities;
using TaxiNT.Libraries.Models;
using TaxiNT.Libraries.Models.GGSheets;
using TaxiNT.Services.Interfaces;

namespace TaxiNT.Services;
//WithSQL
public class BankService : IBankService
{
    #region SQL Controctor
    private readonly taxiNTDBContext context;
    public BankService(taxiNTDBContext _context)
    {
        this.context = _context;
    }
    #endregion
    #region CURD
    //Check Exists
    async Task<bool> isExists(string bankId) => await context.Banks.AnyAsync(b => b.bank_Id == bankId);

    //Get bank by Id
    public async Task<ModelBank> Get(string bankId)
    {
        var result = await context.Banks.SingleOrDefaultAsync(c => c.bank_Id == bankId);
        if (result == null)
            throw new Exception($"Not found ID to get: {bankId}");

        return result;
    }

    //Get all
    public async Task<List<ModelBank>> Gets()
    {
        try
        {
            var result = await context.Banks.ToListAsync();

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }

    }

    //Post
    public async Task<ModelBank> Post(BankPostDto model)
    {
        if (model.bank_Id != string.Empty && (await isExists(model.bank_Id)))
        {
            throw new Exception($"Exists ID to post: {model.bank_Id}");
        }

        //Nhập thông tin category
        var result = new ModelBank
        {
            bank_Id = !string.IsNullOrEmpty(model.bank_Id) ? model.bank_Id : Guid.NewGuid().ToString(),
            bank_NumberId = model.bank_NumberId,
            bank_Name = model.bank_Name,
            bank_NumberCard = model.bank_NumberCard,
            bank_Type = model.bank_Type,
            bank_AccountName = model.bank_AccountName,
            bank_Url = model.bank_Url,
            bank_Status = true,
            createdAt = DateTime.Now
        };

        var rs = await this.context.Banks.AddAsync(result);
        await context.SaveChangesAsync();

        return result;
    }

    //Patch
    public async Task<ModelBank> Patch(string bankId, BankPatchDto model)
    {
        var result = await context.Banks.FindAsync(bankId);
        if (result == null)
        {
            throw new Exception($"Exists ID to patch: {bankId}");
        }

        if (model.bank_NumberId != null)
            result.bank_NumberId = model.bank_NumberId;

        if (model.bank_Name != null)
            result.bank_Name = model.bank_Name;

        if (model.bank_NumberCard != null)
            result.bank_NumberCard = model.bank_NumberCard;

        if (model.bank_Type != null)
            result.bank_Type = model.bank_Type;

        if (model.bank_AccountName != null)
            result.bank_AccountName = model.bank_AccountName;

        if (model.bank_Url != null)
            result.bank_Url = model.bank_Url;

        if (model.bank_Status.HasValue)
            result.bank_Status = model.bank_Status.Value;

        result.updatedAt = DateTime.Now;

        await context.SaveChangesAsync();
        return result;
    }

    //Delete
    public async Task<bool> Delete(string bankId)
    {
        var result = await this.context.Banks.SingleOrDefaultAsync(c => c.bank_Id == bankId);

        if (result == null)
            return false;

        context.Banks.Remove(result);

        await context.SaveChangesAsync();
        return true;
    }

    //Deletes List
    public async Task<DeleteBanksResult> Deletes(List<string> Ids)
    {
        var result = new DeleteBanksResult();

        if (Ids == null || Ids.Count == 0)
        {
            result.Errors.Add("Models is null");
            return result;
        }

        // Lấy các bản ghi tồn tại trong DB
        var banksToDelete = await context.Banks
            .Where(b => Ids.Contains(b.bank_Id))
            .ToListAsync();

        // Ghi nhận những ID không tìm thấy
        var notFoundIds = Ids.Except(banksToDelete.Select(b => b.bank_Id)).ToList();
        result.Errors.AddRange(notFoundIds);

        try
        {
            context.Banks.RemoveRange(banksToDelete);
            await context.SaveChangesAsync();

            result.Results.AddRange(banksToDelete.Select(b => b.bank_Id));
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Lỗi khi xóa: {ex.Message}");
        }

        return result;
    }

    //Upsert List
    public async Task<UpsertBanksResult> Upserts(List<BankUpsertDto> models)
    {
        var result = new UpsertBanksResult();

        if (models == null || models.Count == 0)
        {
            result.Errors.Add("Danh sách rỗng.");
            return result;
        }

        // Khởi tạo thời giang hiện tại
        var now = DateTime.Now;

        // Lấy các Id hợp lệ (khác rỗng)
        var idsProvided = models
            .Where(x => !string.IsNullOrEmpty(x.bank_Id))
            .Select(x => x.bank_Id)
            .ToList();

        // Truy vấn các bản ghi đã tồn tại
        var existingBanks = await context.Banks
            .Where(b => idsProvided.Contains(b.bank_Id))
            .ToListAsync();

        foreach (var input in models)
        {
            try
            {
                // Cập nhật nếu đã có
                var existing = existingBanks.FirstOrDefault(b => b.bank_Id == input.bank_Id);
                if (existing != null)
                {
                    if (input.bank_NumberId != null)
                        existing.bank_NumberId = input.bank_NumberId;

                    if (input.bank_Name != null)
                        existing.bank_Name = input.bank_Name;

                    if (input.bank_NumberCard != null)
                        existing.bank_NumberCard = input.bank_NumberCard;

                    if (input.bank_Type != null)
                        existing.bank_Type = input.bank_Type;

                    if (input.bank_AccountName != null)
                        existing.bank_AccountName = input.bank_AccountName;

                    if (input.bank_Url != null)
                        existing.bank_Url = input.bank_Url;

                    if (input.bank_Status.HasValue)
                        existing.bank_Status = input.bank_Status.Value;

                    existing.updatedAt = now;

                    result.UpdateResults.Add(existing);
                }
                else
                {
                    // Thêm mới nếu chưa tồn tại hoặc bank_Id trống
                    var newBank = new ModelBank
                    {
                        bank_Id = string.IsNullOrWhiteSpace(input.bank_Id) ? Guid.NewGuid().ToString() : input.bank_Id,
                        bank_NumberId = input.bank_NumberId ?? string.Empty,
                        bank_Name = input.bank_Name ?? string.Empty,
                        bank_NumberCard = input.bank_NumberCard ?? string.Empty,
                        bank_Type = input.bank_Type ?? string.Empty,
                        bank_AccountName = input.bank_AccountName ?? string.Empty,
                        bank_Url = input.bank_Url ?? string.Empty,

                        bank_Status = true,
                        createdAt = now,
                        updatedAt = null
                    };

                    await context.Banks.AddAsync(newBank);
                    result.InsertResults.Add(newBank);
                }
            }
            catch (Exception ex)
            {
                //Thêm vào ID lỗi
                result.Errors.Add(input.bank_Id);
            }
        }

        await context.SaveChangesAsync();

        return result;
    }
    #endregion
}
