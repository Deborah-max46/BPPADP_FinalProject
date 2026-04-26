using System.Text.Json;
using ClosedXML.Excel;
using ConsumersVoiceSystemPrototype.Data;
using ConsumersVoiceSystemPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace ConsumersVoiceSystemPrototype.Pages.Reports;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    [BindProperty(SupportsGet = true)]
    public ComplaintStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    public SelectList? CategoryList { get; set; }

    public IReadOnlyList<RowVm> Rows { get; set; } = Array.Empty<RowVm>();

    public string ChartJson { get; set; } = "{}";

    public record RowVm(string Title, string CategoryName, string? BusinessName, string ConsumerName, ComplaintStatus Status, DateTime CreatedAt);

    public async Task OnGetAsync()
    {
        await LoadLookupsAsync();
        await LoadRowsAsync();
    }

    public async Task<IActionResult> OnPostExportExcelAsync()
    {
        await LoadLookupsAsync();
        await LoadRowsAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Complaints");
        ws.Cell(1, 1).Value = "Title";
        ws.Cell(1, 2).Value = "Category";
        ws.Cell(1, 3).Value = "Business";
        ws.Cell(1, 4).Value = "Consumer";
        ws.Cell(1, 5).Value = "Status";
        ws.Cell(1, 6).Value = "Created";

        var r = 2;
        foreach (var row in Rows)
        {
            ws.Cell(r, 1).Value = row.Title;
            ws.Cell(r, 2).Value = row.CategoryName;
            ws.Cell(r, 3).Value = row.BusinessName ?? "";
            ws.Cell(r, 4).Value = row.ConsumerName;
            ws.Cell(r, 5).Value = row.Status.ToString();
            ws.Cell(r, 6).Value = row.CreatedAt;
            r++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"complaints-{DateTime.UtcNow:yyyyMMddHHmm}.xlsx");
    }

    public async Task<IActionResult> OnPostExportPdfAsync()
    {
        await LoadLookupsAsync();
        await LoadRowsAsync();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(28);
                page.Header().Text("Complaints report").SemiBold().FontSize(18);
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });

                    table.Header(h =>
                    {
                        h.Cell().PaddingVertical(4).Text("Title").SemiBold();
                        h.Cell().PaddingVertical(4).Text("Category").SemiBold();
                        h.Cell().PaddingVertical(4).Text("Business").SemiBold();
                        h.Cell().PaddingVertical(4).Text("Consumer").SemiBold();
                        h.Cell().PaddingVertical(4).Text("Status").SemiBold();
                    });

                    foreach (var row in Rows)
                    {
                        table.Cell().PaddingVertical(2).Text(row.Title);
                        table.Cell().Text(row.CategoryName);
                        table.Cell().Text(row.BusinessName ?? "—");
                        table.Cell().Text(row.ConsumerName);
                        table.Cell().Text(row.Status.ToString());
                    }
                });
            });
        });

        var pdf = doc.GeneratePdf();
        return File(pdf, "application/pdf", $"complaints-{DateTime.UtcNow:yyyyMMddHHmm}.pdf");
    }

    private async Task LoadLookupsAsync()
    {
        var cats = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        var items = new List<SelectListItem>
        {
            new() { Text = "Any", Value = "" }
        };
        items.AddRange(cats.Select(c =>
            new SelectListItem(c.Name, c.Id.ToString(), CategoryId == c.Id)));
        CategoryList = new SelectList(items, "Value", "Text", CategoryId?.ToString() ?? "");
    }

    private async Task LoadRowsAsync()
    {
        var q = _db.Complaints.AsNoTracking().AsQueryable();
        var uid = _users.GetUserId(User);

        if (User.IsInRole(RoleNames.Consumer) && uid != null)
            q = q.Where(c => c.ConsumerId == uid);
        else if (User.IsInRole(RoleNames.Business) && uid != null)
        {
            var bizId = await _db.Businesses.AsNoTracking()
                .Where(b => b.OwnerUserId == uid)
                .Select(b => b.Id)
                .FirstOrDefaultAsync();
            if (bizId != 0)
                q = q.Where(c => c.BusinessId == bizId);
            else
                q = q.Where(c => false);
        }

        if (From is { } f)
            q = q.Where(c => c.CreatedAt >= DateTime.SpecifyKind(f, DateTimeKind.Utc).Date);
        if (To is { } t)
        {
            var end = DateTime.SpecifyKind(t, DateTimeKind.Utc).Date.AddDays(1);
            q = q.Where(c => c.CreatedAt < end);
        }

        if (Status is { } st)
            q = q.Where(c => c.Status == st);

        if (CategoryId is { } cid)
            q = q.Where(c => c.CategoryId == cid);

        Rows = await q
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new RowVm(
                c.Title,
                c.Category.Name,
                c.Business != null ? c.Business.Name : null,
                c.Consumer.FullName,
                c.Status,
                c.CreatedAt))
            .ToListAsync();

        var grouped = Rows.GroupBy(r => r.Status.ToString()).ToDictionary(g => g.Key, g => g.Count());
        var payload = new
        {
            labels = grouped.Keys.ToArray(),
            data = grouped.Values.ToArray()
        };
        ChartJson = JsonSerializer.Serialize(payload);
    }
}
