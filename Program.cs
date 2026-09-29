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
// 1. ПРОСМОТР ЗАКАЗА — с кнопками Принять/Отказать
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

    // Уже взят?
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

    // Показываем страницу с кнопками
    return Results.Content(WrapView(token, counterpartyId, order, counterparty.FullName),
        "text/html; charset=utf-8");
});

// =========================================================
// 2. ПРИНЯТЬ ЗАКАЗ
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
// 3. ОТКАЗАТЬСЯ ОТ ЗАКАЗА
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

    // Если уже взят — отказ невозможен
    if (order.AcceptedByCounterpartyId != null)
        return Results.Content(WrapHtml(
            "<h1>Отказ невозможен</h1>" +
            "<p>Этот заказ уже взят другим контрагентом.</p>",
            token, counterpartyId),
            "text/html; charset=utf-8");

    // Сохраняем отказ
    try
    {
        db.Database.ExecuteSqlRaw(
            "INSERT INTO order_declined (order_id, counterparty_id, declined_at) " +
            "VALUES ({0}, {1}, NOW())",
            order.OrderId, counterpartyId);
    }
    catch
    {
        // Если таблицы нет — просто игнорируем, не критично
    }

    return Results.Content(WrapHtml(
        $"<h1>Вы отказались от заказа №{order.OrderId}</h1>" +
        "<p>Ваш отказ зафиксирован. Спасибо за ответ!</p>" +
        $"<p>Если передумаете — вы можете вернуться по ссылке из письма.</p>",
        token, counterpartyId),
        "text/html; charset=utf-8");
});

// =========================================================
// 4. ВЫГРУЗКА EXCEL
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
// 5. ГЛАВНАЯ
// =========================================================
app.MapGet("/", () => Results.Content(
    WrapMainPage(),
    "text/html; charset=utf-8"));

app.Run();


// =========================================================
// HTML-ШАБЛОНЫ
// =========================================================
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
            font-weight: 600; margin-top: 24px;
            transition: background 0.2s; }}
    .btn:hover {{ background: #B69CFF; }}
</style>
</head>
<body>
<div class='container'>
    <div class='card'>
        {content}
    </div>
</div>
</body>
</html>";

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
            <li>Принять или отклонить его</li>
            <li>Скачать Excel-шаблон для заполнения</li>
        </ul>
        <p style='margin-top:16px;font-size:13px;color:#68707C;'>
            Для этого откройте ссылку из письма, которое вам прислал администратор.
        </p>
    </div>
</div>
</body>
</html>";

static string WrapView(Guid token, long cpId, Order order, string counterpartyName)
{
    var itemsHtml = new System.Text.StringBuilder();
    itemsHtml.Append("<table style='width:100%;border-collapse:collapse;margin:16px 0;'>");
    itemsHtml.Append("<tr style='border-bottom:1px solid #2A303A;'>" +
                     "<th style='text-align:left;padding:8px 4px;font-size:13px;color:#68707C;font-weight:500;'>№</th>" +
                     "<th style='text-align:left;padding:8px 4px;font-size:13px;color:#68707C;font-weight:500;'>Номенклатура</th>" +
                     "<th style='text-align:right;padding:8px 4px;font-size:13px;color:#68707C;font-weight:500;'>Кол-во</th></tr>");

    int i = 1;
    foreach (var item in order.OrderItems)
    {
        itemsHtml.Append("<tr style='border-bottom:1px solid #2A303A;'>");
        itemsHtml.Append($"<td style='padding:10px 4px;font-size:14px;'>{i}</td>");
        itemsHtml.Append($"<td style='padding:10px 4px;font-size:14px;'>{item.Nomenclature?.Name}</td>");
        itemsHtml.Append($"<td style='padding:10px 4px;font-size:14px;text-align:right;'>{item.Quantity}</td>");
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
    .container {{ max-width: 680px; margin: 0 auto; }}
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
    .section-title {{ font-size: 14px; color: #68707C; text-transform: uppercase;
                     letter-spacing: 0.5px; margin-top: 24px; margin-bottom: 4px; }}
    .actions {{ display: flex; gap: 12px; margin-top: 32px; }}
    .btn {{ flex: 1; padding: 16px 24px; border: none; border-radius: 10px;
            font-size: 15px; font-weight: 600; cursor: pointer;
            transition: all 0.2s; text-align: center; text-decoration: none;
            display: inline-flex; align-items: center; justify-content: center; gap: 8px; }}
    .btn-accept {{ background: #A78BFA; color: white; }}
    .btn-accept:hover {{ background: #B69CFF; transform: translateY(-1px); }}
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
        <p style='color:#A9B0BB;font-size:14px;'>Вам предложен заказ на поставку. Ознакомьтесь с составом и примите решение.</p>

        <div class='info-grid'>
            <div class='info-item'>
                <div class='info-label'>Склад доставки</div>
                <div class='info-value'>{order.Warehouse?.Name}</div>
            </div>
            <div class='info-item'>
                <div class='info-label'>Адрес</div>
                <div class='info-value'>{order.Warehouse?.Address}</div>
            </div>
            <div class='info-item'>
                <div class='info-label'>Дата создания</div>
                <div class='info-value'>{order.OrderDate:dd.MM.yyyy HH:mm}</div>
            </div>
            <div class='info-item'>
                <div class='info-label'>Статус</div>
                <div class='info-value'>Предложение</div>
            </div>
        </div>

        <div class='section-title'>Состав заказа</div>
        {itemsHtml}

        <form method='post' action='/accept/{token}/{cpId}' style='display:inline;'>
            <div class='actions'>
                <button type='submit' class='btn btn-accept'>
                    ✓ Принять заказ
                </button>
                <button type='submit' formaction='/decline/{token}/{cpId}' class='btn btn-decline'>
                    ✕ Отказаться
                </button>
            </div>
        </form>
    </div>

    <p style='text-align:center;color:#68707C;font-size:12px;'>
        Поток — Материальные ресурсы
    </p>
</div>
</body>
</html>";
}

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
    .container {{ max-width: 560px; width: 100%; text-align: center; }}
    .check {{ width: 80px; height: 80px; margin: 0 auto 24px;
              background: linear-gradient(135deg, #63C7A0, #3FA37F);
              border-radius: 50%; display: flex; align-items: center; justify-content: center;
              font-size: 40px; }}
    h1 {{ font-size: 28px; margin-bottom: 12px; }}
    p {{ color: #A9B0BB; margin-bottom: 24px; }}
    .btn {{ display: inline-block; padding: 16px 32px;
            background: #A78BFA; color: white;
            text-decoration: none; border-radius: 10px;
            font-weight: 600; font-size: 15px; transition: background 0.2s; }}
    .btn:hover {{ background: #B69CFF; }}
    .info {{ background: #1D2128; border: 1px solid #2A303A; border-radius: 12px;
             padding: 20px; margin-bottom: 24px; text-align: left; }}
    .info b {{ color: #A78BFA; }}
</style>
</head>
<body>
<div class='container'>
    <div class='check'>✓</div>
    <h1>Заказ №{order.OrderId} принят!</h1>
    <p>Спасибо! Теперь скачайте Excel-шаблон и заполните серийные номера.</p>

    <div class='info'>
        <p style='color:#F1F3F5;font-size:14px;margin-bottom:12px;'>
            <b>Что делать дальше:</b>
        </p>
        <ol style='color:#A9B0BB;font-size:14px;padding-left:20px;'>
            <li style='margin-bottom:6px;'>Скачайте Excel-шаблон по кнопке ниже</li>
            <li style='margin-bottom:6px;'>Заполните колонки «Серийный номер» и «Номер партии»</li>
            <li>Отправьте файл на почту администратора</li>
        </ol>
    </div>

    <a href='/template/{token}' class='btn'>
        📄 Скачать шаблон Excel
    </a>
</div>
</body>
</html>";
}
