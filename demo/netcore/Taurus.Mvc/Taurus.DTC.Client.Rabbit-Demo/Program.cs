
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTaurusMvc();
builder.Services.AddTaurusDtc();

var app = builder.Build();
app.UseTaurusMvc();

app.UseTaurusDtc(DTCStartType.Client);//this example: client and server in the same project.

app.MapGet("/", () => "Hello World!"); // 3 ���·�ɴ���
app.Run();
