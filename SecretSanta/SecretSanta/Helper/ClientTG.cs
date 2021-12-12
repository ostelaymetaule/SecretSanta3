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
        private const string SHOW_INFO = "Покажи всю информацию еще раз";
        private const string SINGN_OUT = "Не хочу участвовать";

        private const string HELLO_INIT = @"напиши мне свой адрес, 
что бы хотел получить и что бы не очень хотел, 
например там вдруг ты терпеть ненавидишь кулинарные книги, но любишь фигурки октокота, 
чтобы санте было проще подобрать для тебя что-то что не отправится в мусорку в первый же день";
        private const string HELLO_PROGRESS = "Можешь дополнять написаный текст отсылая новые строчки или же стереть все и начать заново (для этого есть кнопки снизу экрана)";
        private const string HELLO_CLEARED = "Не забудь написать свой почтовый адрес с именем =)";
        private const string HELLO_CONFIRMED = "Отлично, я все записал и передам твоему санте когда он заматчится:";
        public const string HELLO_CANCELLED = "Пони, удолил твой адресс и не буду учитывать тебя в сикретсанте";

        private readonly Assigner _assigner;
        private readonly Repository _rep;
        private readonly TelegramBotClient _botClient;
        private ILogger<ClientTG> _logger;
        private ChatGroup _group;
        public const string ADMIN_SHOW_NOT_FILLED_ADDRESS = "ADMIN SHOW NOT FILLED ADRESSES USERS";
        public const string ADMIN_TRIGGERMATCHING = "ADMIN GO GO MATCHING";
        public const string ADMIN_SEND_MATCHED_NOTIFICATION = "ADMIN SEND MATCHINGS TO SANTAS";
        public const string BECOME_GROUP_ADMIN = "ADMIN BECOME GROUP ADMIN";
        public const string ADMIN_RESIGN_ADMIN = "ADMIN RESIGN";
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
                    $"Вот что я отошлю твоему тайному санте: >> {me.UnformattedText} <<");
                            me.ParticipantStatus = ParticipantStatus.progress;
                            break;
                        case SINGN_OUT:
                            me.UnformattedText = "";
                            me.ParticipantStatus = ParticipantStatus.cancelled;
                            break;
                        case ADMIN_SHOW_NOT_FILLED_ADDRESS:
                            int numberOfUsers = _group.Participants.Count;
                            int numberOfUsersInactive = _group.Participants.Where(x => x.ParticipantStatus == ParticipantStatus.cancelled).Count();
                            int numberOfUsersWithoutAddress = _group.Participants.Where(x => String.IsNullOrWhiteSpace(x.UnformattedText) || x.UnformattedText.Length < 5).Count();

                            await _botClient.SendTextMessageAsync(
                               message.Chat,
                               $"UsersWithoutAddress: {numberOfUsersWithoutAddress}/{numberOfUsers}, InactiveUsers: {numberOfUsersInactive}/{numberOfUsers}");

                            break;
                        case ADMIN_TRIGGERMATCHING:
                            //TODO: trigger the match
                            break;
                        case ADMIN_SEND_MATCHED_NOTIFICATION:
                            //TODO: tell users about the santas
                            break;
                        case BECOME_GROUP_ADMIN:
                            _group.Admin = me;
                            await _botClient.SendTextMessageAsync(
                    message.Chat,
                    $"NOW YOU ARE THE ADMIN");
                            break;
                        case ADMIN_RESIGN_ADMIN:
                            _group.Admin = null;
                            await _botClient.SendTextMessageAsync(
                    message.Chat,
                    $"NOW YOU ARE NOW NO LONGER THE ADMIN OF THE GROUP " + _group.GroupName);
                            break;
                        //TODO: anonymous conversation

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
                    helloMessage = HELLO_INIT;
                }
                else if (me.ParticipantStatus == ParticipantStatus.progress)
                {
                    helloMessage = HELLO_PROGRESS;
                }
                else if (me.ParticipantStatus == ParticipantStatus.cleared)
                {
                    helloMessage = HELLO_CLEARED;
                }
                else if (me.ParticipantStatus == ParticipantStatus.confirmed)
                {
                    helloMessage = $"{HELLO_CONFIRMED} >> {me.UnformattedText} <<";
                }
                else if (me.ParticipantStatus == ParticipantStatus.cancelled)
                {
                    helloMessage = HELLO_CANCELLED;
                }


                var buttonOptions = new List<string>() {
                    CONFIRM_ADDRESS,
                    CLEAR_INFO,
                    SINGN_OUT,
                    SHOW_INFO
                };

                if (_group.Admin == null || _group.Admin.UserId == default(long))
                {
                    buttonOptions = new List<string>() {
                        CONFIRM_ADDRESS,
                        CLEAR_INFO,
                        SINGN_OUT,
                        SHOW_INFO,
                        BECOME_GROUP_ADMIN
                    };
                }

                if (_group.Admin.UserId == me.UserId)
                {
                    buttonOptions = new List<string>() {
                        CONFIRM_ADDRESS,
                        CLEAR_INFO,
                        SINGN_OUT,
                        SHOW_INFO,
                        ADMIN_SHOW_NOT_FILLED_ADDRESS,
                        ADMIN_TRIGGERMATCHING,
                        ADMIN_SEND_MATCHED_NOTIFICATION,
                        ADMIN_RESIGN_ADMIN
                    };
                }
                AskButton(message.Chat.Id, helloMessage, buttonOptions);
            }
        }

        /// <summary>
        /// Sends buttons with given text opions
        /// </summary>
        /// <param name="chatId">Chat or user to send the mesages to</param>
        /// <param name="qustionText">Text being send as a message before buttons </param>
        /// <param name="options">list of texts to be send as buttons</param>
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

        //TODO: admin create a room and get an id  (random set of nouns?)
         
        //TODO: admin triggering the assignment

        //TODO: participant: join an existing group by id
        //TODO: greetings and 
        //TODO: anonym communication via bot 


    }
}
