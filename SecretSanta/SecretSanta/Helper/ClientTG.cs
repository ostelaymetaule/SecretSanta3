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
using Telegram.Bot.Types.Enums;
using SecretSanta.Model;
using Telegram.Bot.Types.ReplyMarkups;

namespace SecretSanta.Helper
{
    public class ClientTG
    {
        private const string CONFIRM_ADDRESS = "Йо, всё отлично так и отсылай";
        private const string CLEAR_INFO = "Не, давай заново";
        private const string SHOW_INFO = "Просто напомни мне еще раз";
        private readonly Assigner _assigner;
        private readonly Repository _rep;
        private readonly TelegramBotClient _botClient;
        private ILogger<ClientTG> _logger;
        private ChatGroup _group;
        public ClientTG(ILogger<ClientTG> logger, Assigner assigner, Repository repository, TelegramBotClient botClient)
        {
            this._assigner = assigner ?? throw new NullReferenceException(nameof(assigner));
            this._rep = repository ?? throw new NullReferenceException(nameof(repository));
            this._botClient = botClient ?? throw new NullReferenceException(nameof(botClient));
            this._logger = logger ?? throw new NullReferenceException(nameof(logger));
            var allGroups = _rep.ReadAll();
            _group = _rep.ReadAll().FirstOrDefault(x => x.GroupName == "it-chat-nrw-test");
            if (_group == null)
            {
                _group = new ChatGroup()
                {
                    Id = Guid.NewGuid(),
                    GroupName = "it-chat-nrw-test",
                    Status = Status.init,
                    Participants = new List<Participant>()
                };
                _rep.Save(_group);
            }


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
                    await HandleUpdate(update);

                }
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogError("error while receiving tg message @{exception}", exception);
            }
        }

        private async Task HandleUpdate(Update update)
        {
            if (update.Message is Message message)
            {

                var me = _group.Participants.FirstOrDefault(k => k.UserId == message.Chat.Id);
                if (me == null)
                {
                    me = new Participant()
                    {
                        UserId = message.Chat.Id,
                        AccountName = message.Chat.Username,
                        LastMessage = message.Text,
                        Id = Guid.NewGuid(),
                        UnformattedText = message.Text,
                        CanSendTo = LocationMarker.World,
                        ParticipantStatus = ParticipantStatus.init
                    };
                    _group.Participants.Add(me);
                }
                else
                {
                    switch (message.Text)
                    {
                        case CONFIRM_ADDRESS:
                            me.ParticipantStatus = ParticipantStatus.confirmed;
                            break;
                        case CLEAR_INFO:
                            me.UnformattedText = "";
                            me.ParticipantStatus = ParticipantStatus.cleared;
                            break;
                        case SHOW_INFO:
                            await _botClient.SendTextMessageAsync(
                    message.Chat,
                    $"Вот что я отошлю твоему тайному санте: >> {me.UnformattedText} <<" ); 
                            me.ParticipantStatus = ParticipantStatus.progress;
                            break;
                        default:
                            me.UnformattedText = me.UnformattedText + " \n " + message.Text;
                            me.ParticipantStatus = ParticipantStatus.progress;
                            break;
                    }

                    me.LastMessage = message.Text;
                }
                _rep.Save(_group);

                var helloMessage = @"sup, %юзернейм%,";
                if (me.ParticipantStatus == ParticipantStatus.init)
                {
                    helloMessage = 
@"напиши мне свой адрес, 
что бы хотел получить и что бы не очень хотел, 
например там вдруг ты терпеть ненавидишь кулинарные книги, но любишь фигурки октокота, 
чтобы санте было проще подобрать для тебя что-то что не отправится в мусорку в первый же день";
                }
                else if(me.ParticipantStatus == ParticipantStatus.progress)
                {
                    helloMessage = "Можешь дополнять написаный текст отсылая новые строчки или же стереть все и начать заново (для этого есть кнопки снизу экрана)";
                }
                else if (me.ParticipantStatus == ParticipantStatus.cleared)
                {
                    helloMessage = "Не забудь написать свой почтовый адрес с именем =)";
                }
                else if (me.ParticipantStatus == ParticipantStatus.confirmed)
                {
                    helloMessage = $"Отлично, я все записал и передам твоему санте когда он заматчится: >> {me.UnformattedText} <<";
                }


                    AskButton(message.Chat.Id, helloMessage, new List<string>() {
                //ADDRESS,
                CONFIRM_ADDRESS,
                CLEAR_INFO,
                //CAN_SEND_TO,
                //LOVE_TO_RECEIVE,
                //DO_NOT_LOVE_TO_RECEIVE,
                SHOW_INFO

            });
            }
        }


        private async void AskButton(long chatId, string qustionText, List<string> options)
        {
            var keyboard = new ReplyKeyboardMarkup(options.Select(x => new[] { new KeyboardButton(x) }).ToArray());
            var k = new KeyboardButton[options.Count / 2 + 1][];
            for (int i = 0; i < options.Count; i = i + 2)
            {
                if (options.Count - 1 > i + 1)
                {
                    var l = new[] { new KeyboardButton(options[i]), new KeyboardButton(options[i + 1]) };
                    k[i / 2] = l;
                }
                else
                {
                    var l = new[] { new KeyboardButton(options[i]) };
                    k[i / 2] = l;
                }
            }
            await _botClient.SendTextMessageAsync(chatId, qustionText,
                replyMarkup: keyboard);
        }
        //async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        //{
        //    if (update.Message is Message message)
        //    {
        //        await botClient.SendTextMessageAsync(message.Chat, "Hello");
        //    }
        //}

        //async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        //{
        //    if (exception is ApiRequestException apiRequestException)
        //    {
        //        await botClient.SendTextMessageAsync(123, apiRequestException.ToString());
        //    }
        //}

        //TODO: admin create a room and get an id  (random set of nouns?)
        //TODO: admin seeing if someone not filled the information
        //TODO: admin triggering the assignment

        //TODO: participant: join an existing group by id
        //TODO: greetings and 
        //TODO: anonym communication via bot 


    }
}
