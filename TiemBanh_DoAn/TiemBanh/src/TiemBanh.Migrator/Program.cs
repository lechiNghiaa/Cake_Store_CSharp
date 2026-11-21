using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TiemBanh.Business;
// Triển khai phần mềm — áp dụng migration một lần trước khi mở API.
var cfg=new ConfigurationBuilder().AddEnvironmentVariables().Build();var services=new ServiceCollection().AddLogging().AddBakery(cfg).BuildServiceProvider();
await using var scope=services.CreateAsyncScope();await Bootstrap.MigrateAsync(scope.ServiceProvider,cfg);Console.WriteLine("Migration và seed hoàn tất.");
