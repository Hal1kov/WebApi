using Microsoft.EntityFrameworkCore;
using Potok.Data;
using Potok.DataFolder;
using Potok.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

// =========================================================
// 1. ПРИНЯТИЕ ЗАКАЗА
// =========================================================
app.MapGet("/accept", async (Guid token, long counterpartyId) =>
{
    using var db = DBContextClass.CreateContext();

    var order = db.Orders
        .FirstOrDefault(x => x.PublicToken == token);

    if (order == null)
        return Results.Content(WrapHtml("<h1>Заказ не найден</h1>"),
            "text/html; charset=utf-8");

    var counterparty = db.Counterparties
        .FirstOrDefault(x => x.CounterpartyId == counterpartyId);

    if (counterparty == null)
        return Results.Content(WrapHtml("<h1>Контрагент не найден</h1>"),
            "text/html; charset=utf-8");

    using var transaction = db.Database.BeginTransaction();

    var freshOrder = db.Orders
        .FromSqlRaw("SELECT * FROM orders WHERE order_id = {0} FOR UPDATE", order.OrderId)
        .FirstOrDefault();

    if (freshOrder == null)
        return Results.Content(WrapHtml("<h1>Заказ не найден</h1>"),
            "text/html; charset=utf-8");

    if (freshOrder.AcceptedByCounterpartyId != null)
    {
        transaction.Rollback();

        if (freshOrder.AcceptedByCounterpartyId == counterpartyId)
            return Results.Content(WrapHtml(
                $"<h1>Вы уже взяли этот заказ</h1>" +
                $"<p><a class='btn' href='/template?token={token}'>Скачать шаблон Excel</a></p>"),
                "text/html; charset=utf-8");

        return Results.Content(WrapHtml(
            "<h1>Заказ уже взят</h1>" +
            "<p>К сожалению, заказ принят другим контрагентом.</p>"),
            "text/html; charset=utf-8");
    }

    freshOrder.CounterpartyId = counterpartyId;
    freshOrder.AcceptedByCounterpartyId = counterpartyId;
    freshOrder.AcceptedAt = DateTime.Now;

    var takenStatus = db.OrderStatuses
        .FirstOrDefault(x => x.Name == "Взят контрагентом");
    if (takenStatus != null)
        freshOrder.OrderStatusId = takenStatus.OrderStatusId;

    db.SaveChanges();
    transaction.Commit();

    return Results.Content(WrapHtml(
        $"<h1>Заказ №{freshOrder.OrderId} успешно взят!</h1>" +
        $"<p>Спасибо, <b>{counterparty.FullName}</b>!</p>" +
        $"<p><a class='btn' href='/template?token={token}'>Скачать шаблон Excel</a></p>"),
        "text/html; charset=utf-8");
});

// =========================================================
// 2. ВЫГРУЗКА EXCEL
// =========================================================
app.MapGet("/template", (Guid token) =>
{
    using var db = DBContextClass.CreateContext();

    var order = db.Orders
        .Include(x => x.OrderItems).ThenInclude(oi => oi.Nomenclature)
        .FirstOrDefault(x => x.PublicToken == token);

    if (order == null) return Results.NotFound();

    using var wb = new ClosedXML.Excel.XLWorkbook();
    var ws = wb.Worksheets.Add("Заказ");

    ws.Cell(1, 1).Value = "ID позиции";
    ws.Cell(1, 2).Value = "Номенклатура";
    ws.Cell(1, 3).Value = "Количество";
    ws.Cell(1, 4).Value = "Ед. изм.";
    ws.Cell(1, 5).Value = "Серийный номер";
    ws.Cell(1, 6).Value = "Номер партии";

    ws.Range(1, 1, 1, 6).Style.Font.Bold = true;

    int row = 2;
    foreach (var item in order.OrderItems)
    {
        ws.Cell(row, 1).Value = item.OrderItemId;
        ws.Cell(row, 2).Value = item.Nomenclature?.Name ?? "";
        ws.Cell(row, 3).Value = item.Quantity;
        row++;
    }

    ws.Columns().AdjustToContents();

    using var stream = new MemoryStream();
    wb.SaveAs(stream);

    return Results.File(
        stream.ToArray(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"Заказ_{order.OrderId}.xlsx");
});

// =========================================================
// 3. ГЛАВНАЯ
// =========================================================
app.MapGet("/", () => Results.Content(
    WrapHtml("<h1>Система приёма заказов «Поток»</h1><p>Откройте ссылку из письма.</p>"),
    "text/html; charset=utf-8"));

app.Run();

static string WrapHtml(string content) => $@"
<!DOCTYPE html>
<html lang='ru'>
<head>
<meta charset='utf-8'>
<title>Поток — Заказы</title>
<style>
    body {{ font-family: Arial, sans-serif; max-width: 800px;
            margin: 40px auto; padding: 20px;
            background: #f5f6f8; color: #20242a; }}
    h1 {{ color: #7357b8; }}
    .btn {{ display: inline-block; padding: 12px 24px;
            background: #7357b8; color: white;
            text-decoration: none; border-radius: 6px;
            font-weight: bold; margin-top: 16px; }}
</style>
</head>
<body>{content}</body>
</html>";