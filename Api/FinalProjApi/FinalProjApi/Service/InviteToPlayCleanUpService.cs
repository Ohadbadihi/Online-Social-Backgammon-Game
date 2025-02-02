using FinalProjApi.Repository.UserRpository;


namespace FinalProjApi.Service
{
    public class InviteToPlayCleanUpService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public InviteToPlayCleanUpService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

                    await userRepository.RemoveExpiredGameInvites();
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
         }
        }
    }
}