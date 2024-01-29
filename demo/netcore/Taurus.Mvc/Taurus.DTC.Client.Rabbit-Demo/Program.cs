
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTaurusMvc();
builder.Services.AddTaurusDtc();

var app = builder.Build();
app.UseTaurusMvc();

app.UseTaurusDtc(DTCStartType.Client);//this example: client and server in the same project.

app.MapGet("/", () => "Hello World!"); // 3 添加路由处理
app.Run();
