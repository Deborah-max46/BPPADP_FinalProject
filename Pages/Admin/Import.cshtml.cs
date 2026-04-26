using ClosedXML.Excel;
using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConsumersVoiceSystemPrototype.Pages.Admin;

[Authorize(Roles = RoleNames.Admin)]
public class ImportModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ImportModel(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public int Imported { get; set; }
    public List<string> Skipped { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Upload == null || Upload.Length == 0)
        {
            TempData["ErrorMessage"] = "Choose an Excel file.";
            return Page();
        }

        await using var readStream = Upload.OpenReadStream();
        using var memory = new MemoryStream();
        await readStream.CopyToAsync(memory);
        memory.Position = 0;

        using var workbook = new XLWorkbook(memory);
        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet == null)
        {
            TempData["ErrorMessage"] = "Workbook has no worksheets.";
            return Page();
        }

        var first = sheet.FirstRowUsed();
        if (first == null)
        {
            TempData["ErrorMessage"] = "Sheet is empty.";
            return Page();
        }

        var header = first.RowNumber();
        var colTitle = FindColumn(sheet, header, "Title");
        var colDescription = FindColumn(sheet, header, "Description");
        var colCategory = FindColumn(sheet, header, "CategoryName");
        var colConsumer = FindColumn(sheet, header, "ConsumerEmail");

        if (colTitle == 0 || colDescription == 0 || colCategory == 0 || colConsumer == 0)
        {
            TempData["ErrorMessage"] = "Required columns: Title, Description, CategoryName, ConsumerEmail.";
            return Page();
        }

        var categories = await _db.Categories.AsNoTracking().ToListAsync();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? header;

        for (var row = header + 1; row <= lastRow; row++)
        {
            var title = sheet.Cell(row, colTitle).GetString().Trim();
            var desc = sheet.Cell(row, colDescription).GetString().Trim();
            var rawCat = sheet.Cell(row, colCategory).GetString().Trim();
            var email = sheet.Cell(row, colConsumer).GetString().Trim();

            if (string.IsNullOrWhiteSpace(title))
                continue;

            var cat = categories.FirstOrDefault(c =>
                c.Name.Equals(rawCat, StringComparison.OrdinalIgnoreCase));
            if (cat == null)
            {
                Skipped.Add($"Row {row}: unknown category '{rawCat}'");
                continue;
            }

            var user = await _users.FindByEmailAsync(email);
            if (user == null || !await _users.IsInRoleAsync(user, RoleNames.Consumer))
            {
                Skipped.Add($"Row {row}: invalid consumer '{email}'");
                continue;
            }

            _db.Complaints.Add(new Complaint
            {
                Title = title,
                Description = string.IsNullOrWhiteSpace(desc) ? "(Imported)" : desc,
                CategoryId = cat.Id,
                ConsumerId = user.Id,
                Status = ComplaintStatus.Submitted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            Imported++;
        }

        await _db.SaveChangesAsync();
        TempData["StatusMessage"] = $"Imported {Imported} complaints.";
        return Page();
    }

    private static int FindColumn(IXLWorksheet sheet, int headerRow, string name)
    {
        var row = sheet.Row(headerRow);
        foreach (var cell in row.CellsUsed())
        {
            if (string.Equals(cell.GetString().Trim(), name, StringComparison.OrdinalIgnoreCase))
                return cell.Address.ColumnNumber;
        }

        return 0;
    }
}
