using Microsoft.AspNetCore.Http;
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
// 1. ПРОСМОТР ЗАКАЗА — с кнопками Принять / Предложить / Отказать
//    GET /view/{token}/{counterpartyId}
// =========================================================
app.MapGet("/view/{token}/{counterpartyId}", (Guid token, long counterpartyId) =>
{
    using var db = DBContextClass.CreateContext();

    var order = db.Orders
        .Include(x => x.Warehouse)
        .Include(x => x.OrderItems).ThenInclude(oi => oi.Nomenclature)
        .FirstOrDefault(x => x.PublicToken == token);

    if (order == null)
        return Results.Content(WrapHtml("<h1>Заказ не найден</h1>", token, counterpartyId),
            "text/html; charset=utf-8");

    var counterparty = db.Counterparties
        .FirstOrDefault(x => x.CounterpartyId == counterpartyId);

    if (counterparty == null)
        return Results.Content(WrapHtml("<h1>Контрагент не найден</h1>", token, counterpartyId),
            "text/html; charset=utf-8");

    if (order.AcceptedByCounterpartyId != null)
    {
        if (order.AcceptedByCounterpartyId == counterpartyId)
            return Results.Content(WrapAccepted(token, counterpartyId, order),
                "text/html; charset=utf-8");

        return Results.Content(WrapHtml(
            "<h1>Заказ уже взят</h1>" +
            "<p>К сожалению, заказ принят другим контрагентом.</p>",
            token, counterpartyId),
            "text/html; charset=utf-8");
    }

    return Results.Content(WrapView(token, counterpartyId, order, counterparty.FullName),
        "text/html; charset=utf-8");
});

// =========================================================
// 2. ПРИНЯТЬ ЗАКАЗ ПО НАШЕЙ ЦЕНЕ
//    POST /accept/{token}/{counterpartyId}
// =========================================================
app.MapPost("/accept/{token}/{counterpartyId}", (Guid token, long counterpartyId) =>
{
    using var db = DBContextClass.CreateContext();

    var order = db.Orders
        .FirstOrDefault(x => x.PublicToken == token);

    if (order == null)
        return Results.Content(WrapHtml("<h1>Заказ не найден</h1>", token, counterpartyId),
            "text/html; charset=utf-8");

    var counterparty = db.Counterparties
        .FirstOrDefault(x => x.CounterpartyId == counterpartyId);

    if (counterparty == null)
        return Results.Content(WrapHtml("<h1>Контрагент не найден</h1>", token, counterpartyId),
            "text/html; charset=utf-8");

    using var transaction = db.Database.BeginTransaction();

    var freshOrder = db.Orders
        .FromSqlRaw("SELECT * FROM orders WHERE order_id = {0} FOR UPDATE", order.OrderId)
        .FirstOrDefault();

    if (freshOrder == null)
        return Results.Content(WrapHtml("<h1>Заказ не найден</h1>", token, counterpartyId),
            "text/html; charset=utf-8");

    if (freshOrder.AcceptedByCounterpartyId != null)
    {
        transaction.Rollback();

        if (freshOrder.AcceptedByCounterpartyId == counterpartyId)
            return Results.Content(WrapAccepted(token, counterpartyId, order),
                "text/html; charset=utf-8");

        return Results.Content(WrapHtml(
            "<h1>Заказ уже взят</h1>" +
            "<p>К сожалению, заказ принят другим контрагентом.</p>",
            token, counterpartyId),
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

    return Results.Content(WrapAccepted(token, counterpartyId, order),
        "text/html; charset=utf-8");
});

// =========================================================
// 3. ФОРМА ПРЕДЛОЖЕНИЯ СВОЕЙ ЦЕНЫ
//    GET /offer/{token}/{counterpartyId}
// =========================================================
app.MapGet("/offer/{token}/{counterpartyId}", (Guid token, long counterpartyId) =>
{
    using var db = DBContextClass.CreateContext();

    var order = db.Orders
        .Include(x => x.Warehouse)
        .Include(x => x.OrderItems).ThenInclude(oi => oi.Nomenclature)
        .FirstOrDefault(x => x.PublicToken == token);

    if (order == null)
        return Results.Content(WrapHtml("<h1>Заказ не найден</h1>", token, counterpartyId),
            "text/html; charset=utf-8");

    var counterparty = db.Counterparties
        .FirstOrDefault(x => x.CounterpartyId == counterpartyId);

    if (counterparty == null)
        return Results.Content(WrapHtml("<h1>Контрагент не найден</h1>", token, counterpartyId),
            "text/html; charset=utf-8");

    if (order.AcceptedByCounterpartyId != null)
        return Results.Content(WrapHtml("<h1>Заказ уже взят</h1>", token, counterpartyId),
            "text/html; charset=utf-8");

    return Results.Content(WrapOfferForm(token, counterpartyId, order, counterparty.FullName),
        "text/html; charset=utf-8");
});

// =========================================================
// 4. ОБРАБОТКА ПРЕДЛОЖЕНИЯ
//    POST /offer/{token}/{counterpartyId}
// =========================================================
app.MapPost("/offer/{token}/{counterpartyId}", async (Guid token, long counterpartyId, HttpRequest request) =>
{
    using var db = DBContextClass.CreateContext();

    var order = db.Orders
        .Include(x => x.Warehouse)
        .Include(x => x.OrderItems).ThenInclude(oi => oi.Nomenclature)
        .FirstOrDefault(x => x.PublicToken == token);

    if (order == null)
        return Results.Content(WrapHtml("<h1>Заказ не найден</h1>", token, counterpartyId),
            "text/html; charset=utf-8");

    var counterparty = db.Counterparties
        .FirstOrDefault(x => x.CounterpartyId == counterpartyId);

    if (counterparty == null)
        return Results.Content(WrapHtml("<h1>Контрагент не найден</h1>", token, counterpartyId),
            "text/html; charset=utf-8");

    if (order.AcceptedByCounterpartyId != null)
        return Results.Content(WrapHtml("<h1>Заказ уже взят</h1>", token, counterpartyId),
            "text/html; charset=utf-8");

    var form = await request.ReadFormAsync();
    var offered = new Dictionary<long, decimal>();

    foreach (var item in order.OrderItems)
    {
        var key = $"price_{item.OrderItemId}";
        if (form.ContainsKey(key) &&
            decimal.TryParse(form[key],
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal val) && val >= 0)
        {
            offered[item.OrderItemId] = val;
        }
        else
        {
            return Results.Content(WrapOfferError(token, counterpartyId, order,
                "Заполните все цены корректно"),
                "text/html; charset=utf-8");
        }
    }

    decimal ourTotal = order.OrderItems.Sum(oi => oi.Quantity * (oi.Price ?? 0));
    decimal offeredTotal = order.OrderItems.Sum(oi => oi.Quantity * offered[oi.OrderItemId]);

    if (offeredTotal > ourTotal)
    {
        return Results.Content(WrapOfferError(token, counterpartyId, order,
            $"Ваша цена {offeredTotal:N2} ₽ выше нашей {ourTotal:N2} ₽. Укажите цену не выше нашей."),
            "text/html; charset=utf-8");
    }

    using var tx = db.Database.BeginTransaction();

    var freshOrder = db.Orders
        .FromSqlRaw("SELECT * FROM orders WHERE order_id = {0} FOR UPDATE", order.OrderId)
        .FirstOrDefault();

    if (freshOrder == null || freshOrder.AcceptedByCounterpartyId != null)
    {
        tx.Rollback();
        return Results.Content(WrapHtml("<h1>Заказ уже взят</h1>", token, counterpartyId),
            "text/html; charset=utf-8");
    }

    var orderItems = db.OrderItems.Where(x => x.OrderId == order.OrderId).ToList();

    foreach (var oi in orderItems)
    {
        if (offered.ContainsKey(oi.OrderItemId))
            oi.Price = offered[oi.OrderItemId];
    }

    freshOrder.CounterpartyId = counterpartyId;
    freshOrder.AcceptedByCounterpartyId = counterpartyId;
    freshOrder.AcceptedAt = DateTime.Now;

    var takenStatus = db.OrderStatuses.FirstOrDefault(x => x.Name == "Взят контрагентом");
    if (takenStatus != null)
        freshOrder.OrderStatusId = takenStatus.OrderStatusId;

    db.SaveChanges();
    tx.Commit();

    return Results.Content(WrapAccepted(token, counterpartyId, order),
        "text/html; charset=utf-8");
});

// =========================================================
// 5. ОТКАЗАТЬСЯ
//    POST /decline/{token}/{counterpartyId}
// =========================================================
app.MapPost("/decline/{token}/{counterpartyId}", (Guid token, long counterpartyId) =>
{
    using var db = DBContextClass.CreateContext();

    var order = db.Orders
        .FirstOrDefault(x => x.PublicToken == token);

    if (order == null)
        return Results.Content(WrapHtml("<h1>Заказ не найден</h1>", token, counterpartyId),
            "text/html; charset=utf-8");

    var counterparty = db.Counterparties
        .FirstOrDefault(x => x.CounterpartyId == counterpartyId);

    if (counterparty == null)
        return Results.Content(WrapHtml("<h1>Контрагент не найден</h1>", token, counterpartyId),
            "text/html; charset=utf-8");

    if (order.AcceptedByCounterpartyId != null)
        return Results.Content(WrapHtml(
            "<h1>Отказ невозможен</h1>" +
            "<p>Этот заказ уже взят другим контрагентом.</p>",
            token, counterpartyId),
            "text/html; charset=utf-8");

    try
    {
        db.Database.ExecuteSqlRaw(
            "INSERT INTO order_declined (order_id, counterparty_id, declined_at) " +
            "VALUES ({0}, {1}, NOW())",
            order.OrderId, counterpartyId);
    }
    catch { }

    return Results.Content(WrapDeclined(token, counterpartyId, order.OrderId),
        "text/html; charset=utf-8");
});

// =========================================================
// 6. ВЫГРУЗКА EXCEL
//    GET /template/{token}
// =========================================================
app.MapGet("/template/{token}", (Guid token) =>
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

    var headerRange = ws.Range(1, 1, 1, 6);
    headerRange.Style.Font.Bold = true;
    headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#7357B8");
    headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

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
// 7. ЗАГРУЗКА ЗАПОЛНЕННОГО ФАЙЛА
//    POST /upload/{token}
// =========================================================
app.MapPost("/upload/{token}", async (Guid token, HttpRequest request) =>
{
    using var db = DBContextClass.CreateContext();

    var order = db.Orders
        .Include(x => x.OrderItems)
        .FirstOrDefault(x => x.PublicToken == token);

    if (order == null)
        return Results.Content(WrapHtml("<h1>Заказ не найден</h1>", token, 0),
            "text/html; charset=utf-8");

    if (order.AcceptedByCounterpartyId == null)
        return Results.Content(WrapHtml("<h1>Заказ ещё не принят</h1>", token, 0),
            "text/html; charset=utf-8");

    var existingDoc = db.Documents.FirstOrDefault(x => x.OrderId == order.OrderId);
    if (existingDoc != null)
        return Results.Content(WrapThanks(order), "text/html; charset=utf-8");

    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();

    if (file == null || file.Length == 0)
        return Results.Content(WrapUploadError(token, order.OrderId, "Файл не загружен"),
            "text/html; charset=utf-8");

    try
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        using var wb = new ClosedXML.Excel.XLWorkbook(stream);
        var ws = wb.Worksheet(1);

        var rows = new List<(long oiId, decimal qty, string? sn, string? bn)>();
        int row = 2;

        while (!ws.Cell(row, 1).IsEmpty())
        {
            long oiId = (long)ws.Cell(row, 1).GetDouble();
            decimal qty = (decimal)ws.Cell(row, 3).GetDouble();
            string? sn = ws.Cell(row, 5).GetString();
            string? bn = ws.Cell(row, 6).GetString();

            rows.Add((oiId, qty,
                string.IsNullOrWhiteSpace(sn) ? null : sn,
                string.IsNullOrWhiteSpace(bn) ? null : bn));
            row++;
        }

        if (rows.Count == 0)
            return Results.Content(WrapUploadError(token, order.OrderId, "Файл пуст"),
                "text/html; charset=utf-8");

        using var tx = db.Database.BeginTransaction();

        var docStatus = db.DocumentStatuses
            .FirstOrDefault(x => x.Name == "Проведён");

        var document = new Document
        {
            DocumentNumber = $"IN-{DateTime.Now:yyyyMMdd}-{order.OrderId}",
            DocumentDate = DateTime.Now,
            DocumentType = "Приход",
            DocumentStatusId = docStatus?.DocumentStatusId ?? 1,
            OrderId = order.OrderId,
            WarehouseId = order.WarehouseId,
            EmployeeId = order.AcceptedByCounterpartyId ?? 1
        };
        db.Documents.Add(document);
        db.SaveChanges();

        foreach (var (oiId, qty, sn, bn) in rows)
        {
            var oi = db.OrderItems.FirstOrDefault(x => x.OrderItemId == oiId);
            if (oi == null) continue;

            db.DocumentItems.Add(new DocumentItem
            {
                DocumentId = document.DocumentId,
                NomenclatureId = oi.NomenclatureId,
                SerialNumber = sn,
                BatchNumber = bn,
                Quantity = qty
            });
        }

        var fileStatus = db.OrderStatuses
            .FirstOrDefault(x => x.Name == "Файл получен");
        if (fileStatus != null)
            order.OrderStatusId = fileStatus.OrderStatusId;

        db.SaveChanges();
        tx.Commit();

        return Results.Content(WrapThanks(order), "text/html; charset=utf-8");
    }
    catch (Exception ex)
    {
        return Results.Content(WrapUploadError(token, order.OrderId, ex.Message),
            "text/html; charset=utf-8");
    }
});

// =========================================================
// 8. ГЛАВНАЯ
// =========================================================
app.MapGet("/", () => Results.Content(WrapMainPage(), "text/html; charset=utf-8"));

app.Run();


// =========================================================
// HTML-ШАБЛОНЫ
// =========================================================

// ---------- Общая обёртка ----------
static string WrapHtml(string content, Guid token, long cpId) => $@"
<!DOCTYPE html>
<html lang='ru'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<title>Поток — Заказы</title>
<style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
            background: #111318; color: #F1F3F5;
            min-height: 100vh; display: flex; align-items: center; justify-content: center;
            padding: 24px; line-height: 1.5; }}
    .container {{ max-width: 640px; width: 100%; }}
    .card {{ background: #1D2128; border: 1px solid #2A303A; border-radius: 16px;
             padding: 32px; }}
    h1 {{ font-size: 24px; font-weight: 600; color: #A78BFA; margin-bottom: 16px; }}
    p {{ color: #A9B0BB; margin-bottom: 12px; }}
    b {{ color: #F1F3F5; }}
    .btn {{ display: inline-block; padding: 14px 32px;
            background: #A78BFA; color: white;
            text-decoration: none; border-radius: 8px;
            font-weight: 600; margin-top: 24px; }}
    .btn:hover {{ background: #B69CFF; }}
</style>
</head>
<body>
<div class='container'><div class='card'>{content}</div></div>
</body>
</html>";


// ---------- Главная ----------
static string WrapMainPage() => @"
<!DOCTYPE html>
<html lang='ru'>
<head>
<meta charset='utf-8'>
<title>Поток — Материальные ресурсы</title>
<style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
           background: #111318; color: #F1F3F5;
           min-height: 100vh; display: flex; align-items: center; justify-content: center;
           padding: 24px; }
    .container { max-width: 520px; text-align: center; }
    .logo { font-size: 56px; margin-bottom: 16px; }
    h1 { font-size: 32px; font-weight: 700;
         background: linear-gradient(135deg, #A78BFA, #7357B8);
         -webkit-background-clip: text; -webkit-text-fill-color: transparent;
         margin-bottom: 12px; }
    p { color: #A9B0BB; font-size: 15px; }
    .hint { margin-top: 32px; padding: 20px; background: #1D2128;
            border: 1px solid #2A303A; border-radius: 12px; text-align: left; }
    .hint h3 { color: #A78BFA; font-size: 14px; margin-bottom: 8px; text-transform: uppercase;
               letter-spacing: 1px; }
    .hint ul { list-style: none; color: #A9B0BB; font-size: 14px; }
    .hint li { padding: 6px 0; }
    .hint li::before { content: '→ '; color: #7357B8; margin-right: 6px; }
</style>
</head>
<body>
<div class='container'>
    <div class='logo'>📦</div>
    <h1>Поток</h1>
    <p>Система приёма заказов</p>
    <div class='hint'>
        <h3>Что здесь можно</h3>
        <ul>
            <li>Просмотреть поступивший заказ</li>
            <li>Принять по нашей цене или предложить свою</li>
            <li>Скачать Excel-шаблон для заполнения</li>
            <li>Загрузить заполненный файл обратно</li>
        </ul>
        <p style='margin-top:16px;font-size:13px;color:#68707C;'>
            Откройте ссылку из письма, которое вам прислал администратор.
        </p>
    </div>
</div>
</body>
</html>";


// ---------- Просмотр заказа с ценами ----------
static string WrapView(Guid token, long cpId, Order order, string counterpartyName)
{
    var itemsHtml = new System.Text.StringBuilder();
    itemsHtml.Append("<table style='width:100%;border-collapse:collapse;margin:16px 0;'>");
    itemsHtml.Append("<tr style='border-bottom:1px solid #2A303A;'>" +
                     "<th style='text-align:left;padding:8px 4px;font-size:13px;color:#68707C;'>№</th>" +
                     "<th style='text-align:left;padding:8px 4px;font-size:13px;color:#68707C;'>Номенклатура</th>" +
                     "<th style='text-align:right;padding:8px 4px;font-size:13px;color:#68707C;'>Кол-во</th>" +
                     "<th style='text-align:right;padding:8px 4px;font-size:13px;color:#68707C;'>Цена за ед.</th>" +
                     "<th style='text-align:right;padding:8px 4px;font-size:13px;color:#68707C;'>Сумма</th></tr>");

    int i = 1;
    decimal total = 0;
    foreach (var item in order.OrderItems)
    {
        decimal price = item.Price ?? 0;
        decimal sum = item.Quantity * price;
        total += sum;

        itemsHtml.Append("<tr style='border-bottom:1px solid #2A303A;'>");
        itemsHtml.Append($"<td style='padding:10px 4px;font-size:14px;'>{i}</td>");
        itemsHtml.Append($"<td style='padding:10px 4px;font-size:14px;'>{item.Nomenclature?.Name}</td>");
        itemsHtml.Append($"<td style='padding:10px 4px;font-size:14px;text-align:right;'>{item.Quantity}</td>");
        itemsHtml.Append($"<td style='padding:10px 4px;font-size:14px;text-align:right;'>{price:N2}</td>");
        itemsHtml.Append($"<td style='padding:10px 4px;font-size:14px;text-align:right;'>{sum:N2}</td>");
        itemsHtml.Append("</tr>");
        i++;
    }
    itemsHtml.Append("</table>");

    return $@"
<!DOCTYPE html>
<html lang='ru'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<title>Заказ №{order.OrderId} — Поток</title>
<style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
            background: #111318; color: #F1F3F5;
            min-height: 100vh; padding: 24px; line-height: 1.5; }}
    .container {{ max-width: 760px; margin: 0 auto; }}
    .header {{ display: flex; align-items: center; gap: 12px; margin-bottom: 24px; }}
    .logo {{ font-size: 32px; }}
    .brand {{ font-size: 20px; font-weight: 700;
              background: linear-gradient(135deg, #A78BFA, #7357B8);
              -webkit-background-clip: text; -webkit-text-fill-color: transparent; }}
    .card {{ background: #1D2128; border: 1px solid #2A303A;
             border-radius: 16px; padding: 32px; margin-bottom: 16px; }}
    h1 {{ font-size: 24px; font-weight: 600; margin-bottom: 8px; }}
    h1 span {{ color: #A78BFA; }}
    .greeting {{ color: #A9B0BB; margin-bottom: 24px; }}
    .info-grid {{ display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin: 20px 0; }}
    .info-item {{ padding: 12px 16px; background: #181B21; border: 1px solid #2A303A; border-radius: 10px; }}
    .info-label {{ font-size: 12px; color: #68707C; text-transform: uppercase;
                   letter-spacing: 0.5px; margin-bottom: 4px; }}
    .info-value {{ font-size: 14px; color: #F1F3F5; }}
    .total {{ text-align: right; font-size: 22px; font-weight: 700;
              color: #63C7A0; margin: 20px 0; }}
    .actions {{ display: flex; gap: 12px; margin-top: 24px; flex-wrap: wrap; }}
    .btn {{ flex: 1; min-width: 180px; padding: 16px 24px; border: none; border-radius: 10px;
            font-size: 15px; font-weight: 600; cursor: pointer;
            text-align: center; text-decoration: none;
            display: inline-flex; align-items: center; justify-content: center; gap: 8px;
            font-family: inherit; }}
    .btn-accept {{ background: #A78BFA; color: white; }}
    .btn-accept:hover {{ background: #B69CFF; }}
    .btn-offer {{ background: #63C7A0; color: white; }}
    .btn-offer:hover {{ background: #7BD5B2; }}
    .btn-decline {{ background: transparent; color: #A9B0BB; border: 1px solid #2A303A; }}
    .btn-decline:hover {{ background: #181B21; color: #E57979; border-color: #E57979; }}
    @media (max-width: 600px) {{
        .info-grid {{ grid-template-columns: 1fr; }}
        .actions {{ flex-direction: column; }}
    }}
</style>
</head>
<body>
<div class='container'>
    <div class='header'>
        <div class='logo'>📦</div>
        <div class='brand'>Поток</div>
    </div>

    <div class='card'>
        <h1>Заказ <span>№{order.OrderId}</span></h1>
        <p class='greeting'>Здравствуйте, <b style='color:#F1F3F5;'>{counterpartyName}</b>!</p>
        <p style='color:#A9B0BB;font-size:14px;'>Вам предложен заказ на поставку. Ознакомьтесь с составом и ценами.</p>

        <div class='info-grid'>
            <div class='info-item'>
                <div class='info-label'>Склад доставки</div>
                <div class='info-value'>{order.Warehouse?.Name}</div>
            </div>
            <div class='info-item'>
                <div class='info-label'>Адрес</div>
                <div class='info-value'>{order.Warehouse?.Address}</div>
            </div>
        </div>

        {itemsHtml}

        <div class='total'>Наш итог: {total:N2} ₽</div>

        <div class='actions'>
            <form method='post' action='/accept/{token}/{cpId}' style='display:contents;'>
                <button type='submit' class='btn btn-accept'>
                    ✓ Принять по нашей цене
                </button>
            </form>
            <a href='/offer/{token}/{cpId}' class='btn btn-offer'>
                ₽ Предложить свою цену
            </a>
            <form method='post' action='/decline/{token}/{cpId}' style='display:contents;'>
                <button type='submit' class='btn btn-decline'>
                    ✕ Отказаться
                </button>
            </form>
        </div>
    </div>
</div>
</body>
</html>";
}


// ---------- Форма ввода своих цен ----------
static string WrapOfferForm(Guid token, long cpId, Order order, string counterpartyName)
{
    var itemsHtml = new System.Text.StringBuilder();

    int i = 1;
    foreach (var item in order.OrderItems)
    {
        decimal ourPrice = item.Price ?? 0;

        itemsHtml.Append($@"
<tr style='border-bottom:1px solid #2A303A;'>
    <td style='padding:10px 4px;font-size:14px;'>{i}</td>
    <td style='padding:10px 4px;font-size:14px;'>{item.Nomenclature?.Name}</td>
    <td style='padding:10px 4px;font-size:14px;text-align:right;'>{item.Quantity}</td>
    <td style='padding:10px 4px;font-size:14px;text-align:right;color:#63C7A0;'>{ourPrice:N2}</td>
    <td style='padding:10px 4px;'>
        <input type='number' step='0.01' min='0'
               name='price_{item.OrderItemId}'
               value='{ourPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}'
               required
               style='width:100%;padding:8px 10px;background:#181B21;
                      border:1px solid #2A303A;border-radius:6px;
                      color:#F1F3F5;font-size:14px;font-family:inherit;'/>
    </td>
</tr>");
        i++;
    }

    return $@"
<!DOCTYPE html>
<html lang='ru'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<title>Предложить цену — Поток</title>
<style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
            background: #111318; color: #F1F3F5;
            min-height: 100vh; padding: 24px; }}
    .container {{ max-width: 820px; margin: 0 auto; }}
    .card {{ background: #1D2128; border: 1px solid #2A303A;
             border-radius: 16px; padding: 32px; }}
    h1 {{ font-size: 24px; font-weight: 600; margin-bottom: 8px; }}
    h1 span {{ color: #A78BFA; }}
    p {{ color: #A9B0BB; margin-bottom: 16px; }}
    table {{ width: 100%; border-collapse: collapse; margin: 16px 0; }}
    th {{ text-align: left; padding: 8px 4px; font-size: 13px; color: #68707C; }}
    .btn {{ padding: 16px 32px; border: none; border-radius: 10px;
            font-size: 15px; font-weight: 600; cursor: pointer;
            font-family: inherit; margin-top: 16px; }}
    .btn-submit {{ background: #63C7A0; color: white; width: 100%; }}
    .btn-submit:hover {{ background: #7BD5B2; }}
    .btn-back {{ background: transparent; color: #A9B0BB;
                 border: 1px solid #2A303A; display: inline-block;
                 margin-right: 12px; text-decoration: none; }}
    .hint {{ background: #181B21; border-left: 3px solid #63C7A0;
             padding: 12px 16px; border-radius: 6px; margin-bottom: 16px;
             font-size: 13px; color: #A9B0BB; }}
</style>
</head>
<body>
<div class='container'>
    <div class='card'>
        <h1>Заказ <span>№{order.OrderId}</span></h1>
        <p>Уважаемый(ая) {counterpartyName}!</p>

        <div class='hint'>
            💡 Укажите <b>свою цену</b> за единицу по каждой позиции.<br>
            Если ваша <b>итоговая сумма</b> будет <b>не выше нашей</b>, заказ будет принят автоматически.
        </div>

        <form method='post' action='/offer/{token}/{cpId}'>
            <table>
                <thead>
                    <tr style='border-bottom:2px solid #2A303A;'>
                        <th style='width:40px;'>№</th>
                        <th>Номенклатура</th>
                        <th style='text-align:right;'>Кол-во</th>
                        <th style='text-align:right;'>Наша цена</th>
                        <th style='width:180px;'>Ваша цена</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>

            <a href='/view/{token}/{cpId}' class='btn btn-back'>← Назад</a>
            <button type='submit' class='btn btn-submit'>
                ✓ Отправить предложение
            </button>
        </form>
    </div>
</div>
</body>
</html>";
}


// ---------- Ошибка: цена выше нашей ----------
static string WrapOfferError(Guid token, long cpId, Order order, string message)
{
    return $@"
<!DOCTYPE html>
<html lang='ru'>
<head>
<meta charset='utf-8'>
<title>Ошибка — Поток</title>
<style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
            background: #111318; color: #F1F3F5;
            min-height: 100vh; display: flex; align-items: center; justify-content: center;
            padding: 24px; text-align: center; }}
    .container {{ max-width: 520px; }}
    .icon {{ width: 80px; height: 80px; margin: 0 auto 24px;
             background: linear-gradient(135deg, #E57979, #C64D4D);
             border-radius: 50%; display: flex; align-items: center; justify-content: center;
             font-size: 40px; color: white; }}
    h1 {{ font-size: 26px; margin-bottom: 12px; }}
    p {{ color: #A9B0BB; margin-bottom: 12px; }}
    .error {{ background: #2A1818; border: 1px solid #E57979;
              border-radius: 12px; padding: 16px; margin-top: 16px;
              text-align: left; font-size: 13px; color: #E57979; }}
    .btn {{ display: inline-block; padding: 14px 28px;
            background: #A78BFA; color: white;
            text-decoration: none; border-radius: 10px;
            font-weight: 600; margin-top: 20px; }}
</style>
</head>
<body>
<div class='container'>
    <div class='icon'>!</div>
    <h1>Цена выше нашей</h1>
    <p>Заказ №{order.OrderId}</p>
    <div class='error'>{message}</div>
    <a href='/offer/{token}/{cpId}' class='btn'>Попробовать снова</a>
</div>
</body>
</html>";
}


// ---------- Принято — форма загрузки Excel ----------
static string WrapAccepted(Guid token, long cpId, Order order)
{
    return $@"
<!DOCTYPE html>
<html lang='ru'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<title>Заказ принят — Поток</title>
<style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
            background: #111318; color: #F1F3F5;
            min-height: 100vh; display: flex; align-items: center; justify-content: center;
            padding: 24px; line-height: 1.5; }}
    .container {{ max-width: 600px; width: 100%; }}
    .check {{ width: 80px; height: 80px; margin: 0 auto 24px;
              background: linear-gradient(135deg, #63C7A0, #3FA37F);
              border-radius: 50%; display: flex; align-items: center; justify-content: center;
              font-size: 40px; }}
    h1 {{ font-size: 28px; margin-bottom: 12px; text-align: center; }}
    .subtitle {{ color: #A9B0BB; margin-bottom: 24px; text-align: center; }}
    .card {{ background: #1D2128; border: 1px solid #2A303A;
             border-radius: 16px; padding: 28px; text-align: left; margin-bottom: 16px; }}
    .step-title {{ color: #A78BFA; font-size: 13px; font-weight: 600;
                   text-transform: uppercase; letter-spacing: 1px; margin-bottom: 12px;
                   display: flex; align-items: center; }}
    .step-num {{ display: inline-flex; width: 24px; height: 24px;
                 align-items: center; justify-content: center;
                 background: #A78BFA; color: white;
                 border-radius: 50%; font-size: 13px; font-weight: 700; margin-right: 8px; }}
    .btn {{ display: inline-block; padding: 14px 28px;
            background: #A78BFA; color: white;
            text-decoration: none; border-radius: 10px;
            font-weight: 600; font-size: 15px;
            border: none; cursor: pointer; width: 100%; text-align: center;
            font-family: inherit; }}
    .btn:hover {{ background: #B69CFF; }}
    .divider {{ height: 1px; background: #2A303A; margin: 20px 0; }}
    .upload-form {{ margin-top: 16px; }}
    .file-input-wrap {{ border: 2px dashed #2A303A; border-radius: 10px;
                        padding: 24px; text-align: center; margin-bottom: 12px;
                        cursor: pointer; display: block; }}
    .file-input-wrap:hover {{ border-color: #A78BFA; }}
    .file-input-wrap input {{ display: none; }}
    .file-input-label {{ color: #A9B0BB; font-size: 14px; cursor: pointer; }}
    .file-input-label b {{ color: #A78BFA; }}
    .file-name {{ color: #63C7A0; font-size: 13px; margin-top: 8px; display: none; }}
    .info-note {{ color: #68707C; font-size: 13px; margin-bottom: 16px; }}
</style>
</head>
<body>
<div class='container'>
    <div class='check'>✓</div>
    <h1>Заказ №{order.OrderId} принят!</h1>
    <p class='subtitle'>Теперь скачайте шаблон, заполните и загрузите файл обратно.</p>

    <div class='card'>
        <div class='step-title'><span class='step-num'>1</span>Скачайте шаблон</div>
        <p class='info-note'>Excel-файл с составом заказа</p>
        <a href='/template/{token}' class='btn'>📄 Скачать шаблон Excel</a>

        <div class='divider'></div>

        <div class='step-title'><span class='step-num'>2</span>Заполните и загрузите</div>
        <p class='info-note'>Заполните колонки «Серийный номер» и «Номер партии», затем загрузите файл</p>

        <form method='post' action='/upload/{token}' enctype='multipart/form-data' class='upload-form'>
            <label class='file-input-wrap' for='fileInput'>
                <input type='file' id='fileInput' name='file' accept='.xlsx,.xls'
                       onchange='showFileName(this)'>
                <div class='file-input-label'>
                    <b>Выберите файл</b><br>или перетащите сюда
                </div>
                <div class='file-name' id='fileName'></div>
            </label>
            <button type='submit' class='btn'>📤 Отправить заполненный файл</button>
        </form>
    </div>
</div>
<script>
function showFileName(input) {{
    var fileNameDiv = document.getElementById('fileName');
    if (input.files && input.files[0]) {{
        fileNameDiv.textContent = '✓ ' + input.files[0].name;
        fileNameDiv.style.display = 'block';
    }}
}}
</script>
</body>
</html>";
}


// ---------- Отказ ----------
static string WrapDeclined(Guid token, long cpId, long orderId)
{
    return $@"
<!DOCTYPE html>
<html lang='ru'>
<head>
<meta charset='utf-8'>
<title>Отказ — Поток</title>
<style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
            background: #111318; color: #F1F3F5;
            min-height: 100vh; display: flex; align-items: center; justify-content: center;
            padding: 24px; text-align: center; }}
    .container {{ max-width: 520px; }}
    .icon {{ width: 80px; height: 80px; margin: 0 auto 24px;
             background: linear-gradient(135deg, #6B7280, #4B5563);
             border-radius: 50%; display: flex; align-items: center; justify-content: center;
             font-size: 40px; }}
    h1 {{ font-size: 28px; margin-bottom: 12px; }}
    p {{ color: #A9B0BB; margin-bottom: 12px; }}
    .info {{ background: #1D2128; border: 1px solid #2A303A;
             border-radius: 12px; padding: 20px; margin-top: 24px;
             text-align: left; font-size: 14px; color: #A9B0BB; }}
</style>
</head>
<body>
<div class='container'>
    <div class='icon'>✕</div>
    <h1>Вы отказались</h1>
    <p>Отказ от заказа №{orderId} зафиксирован.</p>
    <div class='info'>
        <p>Спасибо за ответ! Если у вас есть вопросы — свяжитесь с администратором.</p>
    </div>
</div>
</body>
</html>";
}


// ---------- Данные приняты ----------
static string WrapThanks(Order order)
{
    return $@"
<!DOCTYPE html>
<html lang='ru'>
<head>
<meta charset='utf-8'>
<title>Данные приняты — Поток</title>
<style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
            background: #111318; color: #F1F3F5;
            min-height: 100vh; display: flex; align-items: center; justify-content: center;
            padding: 24px; text-align: center; }}
    .container {{ max-width: 520px; }}
    .check {{ width: 80px; height: 80px; margin: 0 auto 24px;
              background: linear-gradient(135deg, #63C7A0, #3FA37F);
              border-radius: 50%; display: flex; align-items: center; justify-content: center;
              font-size: 40px; }}
    h1 {{ font-size: 28px; margin-bottom: 12px; }}
    p {{ color: #A9B0BB; margin-bottom: 12px; }}
    .info {{ background: #1D2128; border: 1px solid #2A303A;
             border-radius: 12px; padding: 20px; margin-top: 24px;
             text-align: left; font-size: 14px; color: #A9B0BB; }}
    .info b {{ color: #A78BFA; }}
</style>
</head>
<body>
<div class='container'>
    <div class='check'>✓</div>
    <h1>Данные приняты!</h1>
    <p>Заказ №{order.OrderId} успешно обработан.</p>
    <div class='info'>
        <p><b>Что дальше?</b></p>
        <p style='margin-top:8px;'>
            Серийные номера и номера партий сохранены в системе.
            Документ прихода сформирован автоматически.
        </p>
        <p style='margin-top:12px;color:#63C7A0;'>Можете закрыть эту страницу.</p>
    </div>
</div>
</body>
</html>";
}


// ---------- Ошибка загрузки ----------
static string WrapUploadError(Guid token, long orderId, string message)
{
    return $@"
<!DOCTYPE html>
<html lang='ru'>
<head>
<meta charset='utf-8'>
<title>Ошибка загрузки — Поток</title>
<style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
            background: #111318; color: #F1F3F5;
            min-height: 100vh; display: flex; align-items: center; justify-content: center;
            padding: 24px; text-align: center; }}
    .container {{ max-width: 520px; }}
    .icon {{ width: 80px; height: 80px; margin: 0 auto 24px;
             background: linear-gradient(135deg, #E57979, #C64D4D);
             border-radius: 50%; display: flex; align-items: center; justify-content: center;
             font-size: 40px; color: white; }}
    h1 {{ font-size: 28px; margin-bottom: 12px; }}
    p {{ color: #A9B0BB; margin-bottom: 12px; }}
    .error {{ background: #2A1818; border: 1px solid #E57979;
              border-radius: 12px; padding: 16px; margin-top: 24px;
              text-align: left; font-size: 13px; color: #E57979;
              word-break: break-word; }}
    .btn {{ display: inline-block; padding: 14px 28px;
            background: #A78BFA; color: white;
            text-decoration: none; border-radius: 10px;
            font-weight: 600; margin-top: 24px; }}
</style>
</head>
<body>
<div class='container'>
    <div class='icon'>!</div>
    <h1>Ошибка загрузки</h1>
    <p>Не удалось обработать файл для заказа №{orderId}.</p>
    <div class='error'>{message}</div>
    <a href='/view/{token}/0' class='btn'>← Вернуться к заказу</a>
</div>
</body>
</html>";
}
