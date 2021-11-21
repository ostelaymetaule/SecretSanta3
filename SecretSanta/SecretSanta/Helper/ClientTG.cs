using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SecretSanta.Data;

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Extensions.Polling;
using Microsoft.Extensions.Logging;

namespace SecretSanta.Helper
{
    public class ClientTG
    {
        private readonly Assigner _assigner;
        private readonly Repository _repository;
        private readonly TelegramBotClient _botClient;
        private ILogger<ClientTG> _logger;
        public ClientTG(ILogger<ClientTG> logger, Assigner assigner, Repository repository, TelegramBotClient botClient)
        {
            this._assigner = assigner ?? throw new NullReferenceException(nameof(assigner));
            this._repository = repository ?? throw new NullReferenceException(nameof(repository));
            this._botClient = botClient ?? throw new NullReferenceException(nameof(botClient));
            this._logger = logger ?? throw new NullReferenceException(nameof(logger));

        }


        public async Task StartReceiving(CancellationTokenSource cts)
        {
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // receive all update types
            };
            var updateReceiver = new QueuedUpdateReceiver(_botClient, receiverOptions);
            try
            {
                await foreach (Update update in updateReceiver.WithCancellation(cts.Token))
                {
                    if (update.Message is Message message)
                    {
                        await _botClient.SendTextMessageAsync(
                            message.Chat,
                            $"Still have to process {updateReceiver.PendingUpdates} updates"
                        );
                    }
                }
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogError("error while receiving tg message @{exception}", exception);
            }
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is Message message)
            {
                await botClient.SendTextMessageAsync(message.Chat, "Hello");
            }
        }

        async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is ApiRequestException apiRequestException)
            {
                await botClient.SendTextMessageAsync(123, apiRequestException.ToString());
            }
        }

        //TODO: admin create a room and get an id  (random set of nouns?)
        //TODO: admin seeing if someone not filled the information
        //TODO: admin triggering the assignment

        //TODO: participant: join an existing group by id
        //TODO: greetings and 
        //TODO: anonym communication via bot 


    }
}
