
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTaurusMvc();
builder.Services.AddTaurusDtc();

var app = builder.Build();
app.UseTaurusMvc();

app.UseTaurusDtc(StartType.Client);//this example: client and server in the same project.

app.Run();
