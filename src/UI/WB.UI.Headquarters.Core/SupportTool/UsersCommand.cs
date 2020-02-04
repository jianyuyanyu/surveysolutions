using System.CommandLine;
using System.CommandLine.Invocation;
using Main.Core.Entities.SubEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WB.Core.BoundedContexts.Headquarters.Implementation;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.Infrastructure.Domain;
using Option = System.CommandLine.Option;

namespace WB.UI.Headquarters.SupportTool
{
    public class UsersCommand : Command
    {
        private readonly IHost host;

        public UsersCommand(IHost host) : base("users", "Manage users of Headquarters")
        {
            this.host = host;

            this.Add(UsersCreateCommand());
            this.Add(ResetPasswordCommand());
        }

        private Command UsersCreateCommand()
        {
            var cmd = new Command("create")
            {
                new Option("--role")
                {
                    Required = true,
                    Argument = new Argument<UserRoles>()
                },
                new Option("--password")
                {
                    Required = true,
                    Argument = new Argument<string>()
                },
                new Option("--login")
                {
                    Required = true,
                    Argument = new Argument<string>()
                }
            };

            cmd.Handler = CommandHandler.Create<UserRoles, string, string>(async (role, password, login) =>
            {
                var inScopeExecutor = this.host.Services.GetRequiredService<IInScopeExecutor>();
                await inScopeExecutor.ExecuteAsync(async (locator, unitOfWork) =>
                {
                    var loggerProvider = locator.GetInstance<ILoggerProvider>();
                    var logger = loggerProvider.CreateLogger(nameof(UsersCreateCommand));
                    var userManager = locator.GetInstance<UserManager<HqUser>>();
                    var user = new HqUser
                    {
                        UserName = login
                    };
                    var creationResult = await userManager.CreateAsync(user, password);
                    if (creationResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, role.ToString());
                        logger.LogInformation("Created user {user} as {role}", login, role);
                    }
                    else
                    {
                        logger.LogError("Failed to create user {user}", login);
                        foreach (var error in creationResult.Errors)
                        {
                            logger.LogError(error.Description);
                        }

                        unitOfWork.DiscardChanges();
                    }
                });

            });
            return cmd;
        }

        private Command ResetPasswordCommand()
        {
            var cmd = new Command("reset-password")
            {
                new Option("--username")
                {
                    Required = true,
                    Argument = new Argument<string>()
                },
                new Option("--password")
                {
                    Required = true,
                    Argument = new Argument<string>()
                },
            };

            cmd.Handler = CommandHandler.Create<string, string>(async (username, password) =>
            {
                var inScopeExecutor = this.host.Services.GetRequiredService<IInScopeExecutor>();
                await inScopeExecutor.ExecuteAsync(async (locator, unitOfWork) =>
                {
                    var loggerProvider = locator.GetInstance<ILoggerProvider>();
                    var logger = loggerProvider.CreateLogger(nameof(UsersCreateCommand));
                    var userManager = locator.GetInstance<UserManager<HqUser>>();
                    var user = await userManager.FindByNameAsync(username);
                    if (user == null)
                    {
                        logger.LogError($"User {username} not found");
                        unitOfWork.DiscardChanges();
                        return;
                    }

                    var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                    var result = await userManager.ResetPasswordAsync(user, resetToken, password);
                    if (result.Succeeded)
                    {
                        logger.LogInformation($"Reset password for user {username} succeeded");
                    }
                    else
                    {
                        logger.LogError($"Failed to reset password for user {username}");
                        foreach (var error in result.Errors)
                        {
                            logger.LogError(error.Description);
                        }

                        unitOfWork.DiscardChanges();
                    }
                });

            });
            return cmd;
        }
    }
}