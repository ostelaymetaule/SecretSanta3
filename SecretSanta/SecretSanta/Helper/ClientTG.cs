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
        private const string MESSAGE_MY_RECEIPIENT = "Написать внучку анонимно";
        private const string MESSAGE_MY_SANTA = "Написать своему санте анонимно";
        private const string TELL_ME_WHAT_TO_SEND_TO_RECEIPIENT = "В следующем сообщении напиши что передать твоему внучку анонимно, например треккинг номер посылки, чтобы он её вовремя забрал с почты:";
        private const string TELL_ME_WHAT_TO_SEND_TO_SANTA = "В следующем сообщении напиши что передать твоему сикрет санте анонимно, например что он забыл дать тебе треккинг номер:";

        private const string HELLO_INIT = @"напиши мне свой адрес, 
что бы хотел получить и что бы не очень хотел, 
например там вдруг ты терпеть ненавидишь кулинарные книги, но любишь фигурки октокота, 
чтобы санте было проще подобрать для тебя что-то что не отправится в мусорку в первый же день";
        private const string HELLO_PROGRESS = "Можешь дополнять написаный текст отсылая новые строчки или же стереть все и начать заново (для этого есть кнопки снизу экрана)";
        private const string HELLO_CLEARED = "Не забудь написать свой почтовый адрес с именем =)";
        private const string HELLO_CONFIRMED = "Отлично, я все записал и передам твоему санте когда он заматчится:";
        public const string HELLO_CANCELLED = "Пони, удолил твой адрес и не буду учитывать тебя в сикретсанте";

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
            var helloMessage = @"sup, %юзернейм%,";

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
                    try
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
                            case MESSAGE_MY_RECEIPIENT:

                                helloMessage = TELL_ME_WHAT_TO_SEND_TO_RECEIPIENT;
                                //todo
                                break;
                            case MESSAGE_MY_SANTA:

                                helloMessage = TELL_ME_WHAT_TO_SEND_TO_SANTA;
                                //todo
                                break;


                            case ADMIN_SHOW_NOT_FILLED_ADDRESS:

                                foreach (var participant in _group.Participants)
                                {
                                    participant.UnformattedText = participant.UnformattedText?.Replace("/start", "") ?? "";
                                    participant.UnformattedText = participant.UnformattedText?.Replace(SINGN_OUT, "") ?? "";
                                    participant.UnformattedText = participant.UnformattedText?.Replace(SHOW_INFO, "") ?? "";
                                }

                                int numberOfUsers = _group.Participants.Count;
                                int numberOfUsersInactive = _group.Participants.Count(x => x.ParticipantStatus == ParticipantStatus.cancelled);
                                int numberOfUsersWithoutAddress = _group.Participants.Count(x => String.IsNullOrWhiteSpace(x.UnformattedText) || x.UnformattedText.Length < 7);

                                await _botClient.SendTextMessageAsync(
                                   message.Chat,
                                   $"UsersWithoutAddress: {numberOfUsersWithoutAddress}/{numberOfUsers}, InactiveUsers: {numberOfUsersInactive}/{numberOfUsers}");

                                break;
                            case ADMIN_TRIGGERMATCHING:
                                MatchSantasAndFindPairs();
                                //TODO: trigger the match
                                break;
                            case ADMIN_SEND_MATCHED_NOTIFICATION:
                                SendMatchingNotificationsAsync();
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

                                if (me.LastMessage == MESSAGE_MY_RECEIPIENT)
                                {
                                    //long myChatId = me.UserId;
                                    var receivingUser = _group.Participants.FirstOrDefault(x => x.Id == me.SantaMatching.SendingToId);
                                    var receivingUserChat = await _botClient.GetChatAsync(receivingUser.UserId);

                                    helloMessage = @"Передал внучку";

                                    await _botClient.SendTextMessageAsync(
                                    receivingUserChat.Id,
                                    $"Твой санта просил передать: {message.Text}");
                                }
                                else if (me.LastMessage == MESSAGE_MY_SANTA)
                                {
                                    var mySantaUser = _group.Participants.FirstOrDefault(x => x.Id == me.SantaMatching.ReceivingFromId);
                                    var mySantaUserChat = await _botClient.GetChatAsync(mySantaUser.UserId);
                                    helloMessage = @"Передал санте";


                                    await _botClient.SendTextMessageAsync(
                                    mySantaUserChat.Id,
                                    $"Твой внучок просил передать: {message.Text}");
                                }
                                else if (me.ParticipantStatus < ParticipantStatus.confirmed)
                                {

                                    me.UnformattedText = me.UnformattedText + " \n " + message.Text;


                                    me.ParticipantStatus = ParticipantStatus.progress;
                                }

                                break;
                        }
                    }
                    catch (Exception ex)
                    {

                        _logger.LogError("Exception while sending infos to agents {ex}", ex);
                        var admin = _group.Admin;
                        await _botClient.SendTextMessageAsync(
                           admin.UserId,
                           $"Хей, тут сломалось у пользователя {me.AccountName} при приеме сообщения >> {ex.Message}, {ex.StackTrace} <<");
                    }
                    me.LastMessage = message.Text;
                    me.UnformattedText = me.UnformattedText.Replace("/start", "");
                    me.UnformattedText = me.UnformattedText.Replace(SINGN_OUT, "");
                    me.UnformattedText = me.UnformattedText.Replace(SHOW_INFO, "");
                }
                _rep.Save(_group);

                if (_group.Status == Status.drawn || _group.Status == Status.instructionsSent)
                {
                    //helloMessage = "Ждем подарков :3";
                }
                else
                {
                    switch (me.ParticipantStatus)
                    {
                        case ParticipantStatus.init:
                            helloMessage = HELLO_INIT;
                            break;
                        case ParticipantStatus.progress:
                            helloMessage = HELLO_PROGRESS;
                            break;
                        case ParticipantStatus.cleared:
                            helloMessage = HELLO_CLEARED;
                            break;
                        case ParticipantStatus.confirmed:
                            helloMessage = $"{HELLO_CONFIRMED} >> {me.UnformattedText} <<";
                            break;
                        case ParticipantStatus.cancelled:
                            helloMessage = HELLO_CANCELLED;
                            break;
                    }

                }



                var buttonOptions = new List<string>() {
                    CONFIRM_ADDRESS,
                    CLEAR_INFO,
                    SINGN_OUT,
                    SHOW_INFO
                };

                if (_group.Status >= Status.registrationClosed)
                {
                    buttonOptions = new List<string>() {
                    SHOW_INFO
                };
                }
                if (_group.Status == Status.instructionsSent)
                {
                    buttonOptions = new List<string>() {
                    SHOW_INFO,
                    MESSAGE_MY_RECEIPIENT,
                    MESSAGE_MY_SANTA
                };
                }


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
                else if (_group.Admin?.UserId == me.UserId)
                {
                    buttonOptions = new List<string>() {
                        CONFIRM_ADDRESS,
                        CLEAR_INFO,
                        SINGN_OUT,
                        SHOW_INFO,
                         MESSAGE_MY_RECEIPIENT,
                    MESSAGE_MY_SANTA,
                        ADMIN_SHOW_NOT_FILLED_ADDRESS,
                        ADMIN_TRIGGERMATCHING,
                        ADMIN_SEND_MATCHED_NOTIFICATION,
                        ADMIN_RESIGN_ADMIN
                    };
                }
                AskButton(message.Chat.Id, helloMessage, buttonOptions);
            }
        }

        private void MatchSantasAndFindPairs()
        {
            _group.Status = Status.registrationClosed;
            _rep.Save(_group);
            var matchedPairs = _assigner.TriggerMatching(_group.Id);
            _logger.LogDebug("matched pairs for debug: @{matchedPairs}", matchedPairs);
            _group = _rep.ReadAll().FirstOrDefault(x => x.GroupName == "it-chat-nrw-test");
            _group.Status = Status.drawn;
            _rep.Save(_group);
            var buttonOptions = new List<string>() {
                    SHOW_INFO };

            foreach (var participant in _group.Participants.Where(x => x.ParticipantStatus != ParticipantStatus.cancelled && !String.IsNullOrWhiteSpace(x.UnformattedText) && x.UnformattedText.Length > 7))
            {
                AskButton(participant.UserId, "Секретный санта перемешивает шляпу", buttonOptions);
            }


        }

        private async Task SendMatchingNotificationsAsync()
        {

            foreach (var participant in _group.Participants.Where(x => x.ParticipantStatus != ParticipantStatus.cancelled && !String.IsNullOrWhiteSpace(x.UnformattedText) && x.UnformattedText.Length > 7))
            {
                try
                {
                    long myChatId = participant.UserId;
                    var receivingUser = _group.Participants.FirstOrDefault(x => x.SantaMatching.ReceivingFromId == participant.Id);
                    var receivingUserChat = await _botClient.GetChatAsync(receivingUser.UserId);

                    var name = $"{receivingUser.AccountName} ({receivingUserChat.FirstName} {receivingUserChat.LastName})";
                    await _botClient.SendTextMessageAsync(
                       myChatId,
                       $"Глубокий ИскусственныйИннокентий посовещался и решил что твой внучок на этот год с никнемом {name}, не пиши ему сам, тут появится кнопка чтобы связаться с ним анонимно чтобы например уточнить адрес или договориться о передаче лично. Нюдсы правда слать можно только текстом");
                    await _botClient.SendTextMessageAsync(
                        myChatId,
                        $"Хей, секретный санта! Вот что написал тебе твой внучок: \n\n>> {receivingUser.UnformattedText} <<");

                    participant.InternalStatusUpdates = 1;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception while sending infos to agents {ex}", ex);
                    var admin = _group.Admin;
                    await _botClient.SendTextMessageAsync(
                       admin.UserId,
                       $"Хей, тут сломалось у пользователя {participant.AccountName} при отсылке инфы о внучке >> {ex.Message} <<");
                }
            }
            _group.Status = Status.instructionsSent;
            _rep.Save(_group);
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
