using SecretSanta.Bot.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SecretSanta.Bot.Helpers
{
    public class BotCaller
    {
        private const string ADDRESS = "/Адрес";
        private const string CAN_SEND_TO = "/Регион";
        private const string LOVE_TO_RECEIVE = "/Пожелания";
        private const string DO_NOT_LOVE_TO_RECEIVE = "/Фуфуфу";
        private const string SHOW_INFO = "Показать инфу";
        private const string SEND_FEEDBACK = "/Передать Внучку";
        private const string SEND_FEEDBACK_TO_SANTA = "/Написать Санте";

        private Telegram.Bot.TelegramBotClient _client;

        public BotCaller(IFileRepository rep, Telegram.Bot.TelegramBotClient client)
        {

            _rep = rep;
            _rep.GetUserInfos().ForEach(x => System.Console.WriteLine($"{x.UserName}:\n my secret santa is {x.MySecretSanta},\n and i am secret santa for: {x.IAmSecretSantaForUser}\n\n"));
            _client = client;

            _client.OnMessage += Client_OnMessage;
            _client.OnInlineResultChosen += Client_OnInlineResultChosen;
            _client.OnInlineQuery += Client_OnInlineQuery;
            _client.OnCallbackQuery += Client_OnCallbackQuery;

            _client.StartReceiving();
        }

        private async void Client_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            var action = e.CallbackQuery.Data;
            var user = e.CallbackQuery.Message.Chat.Username;
            var chatId = e.CallbackQuery.Message.Chat.Id;
            var userInfo = _rep.GetUserInfos().FirstOrDefault(x => x.UserName == user) ?? new UserSantaInfos() { ChatId = chatId, UserName = user };
            switch (action)
            {
                case "только Москва":
                    userInfo.CanSendPresentTo = action;
                    break;

                case "только Россия":
                    userInfo.CanSendPresentTo = action;

                    break;

                case "только Земля":
                    userInfo.CanSendPresentTo = action;

                    break;

                default:
                    break;
            }

            _rep.SaveUserInfosToFile(new List<UserSantaInfos>()
                {
                    userInfo
                });

            await _client.SendTextMessageAsync(chatId, "Записал куда можешь выслать подарок.");
        }

        /// <summary>
        /// Найти всех пользователей с заполненными адресами - это санты без внучков 
        /// Создать пустой список для сант с их внучками(матчингами)
        /// посчитать количество всех потенциальных сант
        /// найти первого случайного санту, уменьшить количество потенциальных сант на одного и убрать найденного из этого списка
        /// продолжать пока список сант не опустеет:
        ///     Если случайный санта может слать только по россии:
        ///         выбрать случайный индекс из списка сант без внучков, которые находятся в россии
        ///         и выбрать случайного внучка по этому индексу
        ///     иначе
        ///         выбрать случайный индекс из списка сант без внучков, вне зависимости от места положения
        ///         и выбрать случайного внучка по этому индексу
        ///     Установить случайному санте выбранного случайного внучка и наоборот, внучку санту(залинковать по userID)
        ///     добавить санту в список для сант с внучками (матчингами)
        ///     
        ///     Теперь выбраный внучок сам становится сантой для следующего цикла
        ///     уменьшаем количество всех незаматченных сант на 1
        ///     удаляем нового санту из списка сант без внучков
        /// конец цикла
        /// Установить случайному санте выбранного случайного внучка и наоборот, внучку санту(залинковать по userID)
        /// добавить санту в список для сант с внучками (матчингами)
        /// сохраняем список сант с внучками в базу
        /// </summary>
        /// <returns></returns>
        public List<Tuple<string, string>> TriggerMatching()
        {
            var ret = new List<Tuple<string, string>>();

            var notMatchedSantas = _rep.GetUserInfos().Where(x => !String.IsNullOrEmpty(x.PostAddress)).ToList();
            var santasWithMatch = new List<UserSantaInfos>();
            Random r = new Random((int)DateTime.Now.Ticks);
            var restTargetCount = notMatchedSantas.Count;
            var firstSanta = notMatchedSantas[r.Next(restTargetCount)];
            var santa = firstSanta;
            restTargetCount--;
            notMatchedSantas.Remove(santa);

            while (restTargetCount > 0)
            {
                UserSantaInfos target;

                if (santa.CanSendPresentTo == "только Россия")
                {
                    var nextRandomIndexOfRussianSantas = r.Next(notMatchedSantas.Where(x => x.IAmInMoscow || x.IAmInRussia).Count());
                    target = notMatchedSantas.Where(x => x.IAmInMoscow || x.IAmInRussia).ToList()[nextRandomIndexOfRussianSantas];
                }
                else
                {
                    var nextRandomIndexOfGlobalSantas = r.Next(notMatchedSantas.Count);
                    target = notMatchedSantas[nextRandomIndexOfGlobalSantas];
                }

                santa.IAmSecretSantaForUser = target.UserName;
                target.MySecretSanta = santa.UserName;
                santasWithMatch.Add(santa);
                santa = target;
                restTargetCount--;
                notMatchedSantas.Remove(santa);
            }

            santa.IAmSecretSantaForUser = firstSanta.UserName;
            firstSanta.MySecretSanta = santa.UserName;
            santasWithMatch.Add(santa);
            _rep.SaveUserInfosToFile(santasWithMatch);
            ret = santasWithMatch
                .Select(x => new Tuple<string, string>(x.UserName, x.IAmSecretSantaForUser))
                .ToList();
            return ret;
        }

        private void Client_OnInlineQuery(object sender, InlineQueryEventArgs e)
        {
        }

        private void Client_OnInlineResultChosen(object sender, ChosenInlineResultEventArgs e)
        {
            var idreceived = e.ChosenInlineResult.ResultId;
        }

        private async void AskButtonWithCallBack(long chatId, string qustionText, List<string> options)
        {
            var keyboard = new InlineKeyboardMarkup(options.Select(x => new[] { new InlineKeyboardButton() { Text = x, CallbackData = x } }).ToArray());

            await _client.SendTextMessageAsync(chatId, qustionText,
                replyMarkup: keyboard);
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
            await _client.SendTextMessageAsync(chatId, qustionText,
                replyMarkup: keyboard);
        }

        private async void Client_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(e.Message.Chat.Username))
            {
                var errorNoUserName = "Сорямба, мне нужно чтобы у тебя был userName. Пожалуйста в настройках телеграма укажи свой userName";
                await _client.SendTextMessageAsync(e.Message.Chat.Id, errorNoUserName);
                return;
            }
            var helloMessage = "Привет!\n\nЯ очень рад приветствовать тебя тут. Если ты зашел сюда, то ты хочешь стать Секретным Сантой кого-то из чатика addmeto & techsparks.Это очень приятно!";
            var rulez1 = "Чтобы всё у нас получилось, ты должен ответить на несколько моих вопросов. Они личные, но знай, я забочусь о безопасности твоих данных.";
            var rulez2 = "Ты расскажешь мне, как тебя зовут, откуда ты и что ты хочешь или не хочешь получить в подарок, а я позабочусь о том, чтобы кто-то получил твой адрес и выдал его твоему Санте. А ты получишь адрес того, кого будешь одаривать ты.";
            var rulez3 = "Так как это секрет, то только ты и будешь знать, кто твой подопечный. Пожалуйста, сохрани эту информацию для себя! Иначе никакой магии не получится.";
            if (IsFirstMessage(e.Message))
            {
                await _client.SendTextMessageAsync(e.Message.Chat.Id, helloMessage);
                await _client.SendTextMessageAsync(e.Message.Chat.Id, rulez1);
                await _client.SendTextMessageAsync(e.Message.Chat.Id, rulez2);
                await _client.SendTextMessageAsync(e.Message.Chat.Id, rulez3);

            }

            _rep.SaveMessagesToFile(new List<SecretSantaEntry>() {new SecretSantaEntry()
            {
                Message = e.Message.Text,
                UserName = e.Message.Chat.Username,
                MessageId = e.Message.MessageId,
                MessageTime = e.Message.Date,
                UserFirstName = e.Message.Chat.FirstName,
                UserLastName = e.Message.Chat.LastName,
                ChatId = e.Message.Chat.Id
            } });

            var userInfo = _rep.GetUserInfos().FirstOrDefault(x => x.UserName == e.Message.Chat.Username) ?? new UserSantaInfos() { ChatId = e.Message.Chat.Id, UserName = e.Message.Chat.Username };

            AskButton(e.Message.Chat.Id, "Заполни анкету, %юзернейм%", new List<string>() {
                ADDRESS,
                //SEND_FEEDBACK,
                //SEND_FEEDBACK_TO_SANTA,
                CAN_SEND_TO,
                LOVE_TO_RECEIVE,
                DO_NOT_LOVE_TO_RECEIVE,
                SHOW_INFO

            });

            switch (e.Message.Text)
            {
                case ADDRESS:
                    var tellMeWhereYouLive = "Введи свой почтовый адрес включая ФИО. Пожалуйста, введи его в такой форме, как он должен быть написан на посылке/конверте. К сожалению не возможно доставить посылку без ФИО и правильного адреса, поэтому не огорчай своего Санту и сделай всё правильно.";
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, tellMeWhereYouLive);
                    return;

                case CAN_SEND_TO:
                    AskButtonWithCallBack(e.Message.Chat.Id, "Я понимаю, что ты может быть не хочешь отсылать посылку в США или в Антарктиду. Расскажи, куда ты можешь и хочешь отправить подарок, а я учту это при распределении.", new List<string>() { "только Россия", "только Москва", "только Земля" });
                    return;

                case LOVE_TO_RECEIVE:
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, "А теперь давай о хорошем. Расскажи тут своему Санте о своих предпочтениях, можешь рассказать немного о себе и о том, что тебе по душе.");
                    return;

                case DO_NOT_LOVE_TO_RECEIVE:
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, "Чтобы не было неприятных сюрпризов, поделись, что бы ты не хотел ни в коем случае получить в подарок от своего Санты. Если ты не любишь розовых единорогов, то это самое время об этом написать.");
                    return;

                case SHOW_INFO:
                    var anythingElse = "Вот то, что ты мне о себе рассказал. Если ты нашел ошибку, то можешь исправить её, повторно введя ту или иную информацию.";
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, anythingElse);
                    var allInfo = $"username @{userInfo.UserName}: {userInfo.PostAddress};\n куда могу послать подарок: {userInfo.CanSendPresentTo}\n Хочешь получить: {userInfo.LoveToReceive} \n Не хочешь: {userInfo.DontWantToReceive} \n";
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, allInfo);
                    await SingleSantaMatchSend(_rep.GetUserInfos(), userInfo);
                    return;
                case SEND_FEEDBACK:
                    var tellMeWhatToSendBack = "Напиши своему внучку анонимно, например треккинг номер посылки, чтобы он её вовремя забрал с почты:";
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, tellMeWhatToSendBack);
                    return;
                case SEND_FEEDBACK_TO_SANTA:
                    var tellMeWhatToSendToSanta = "Напиши сикрет санте анонимно, например что он забыл дать тебе треккинг номер:";
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, tellMeWhatToSendToSanta);
                    return;
                default:
                    break;
            }
            var userMessages = _rep.GetMessages()
                .Where(x => x.UserName == userInfo.UserName)
                .OrderByDescending(x => x.MessageId);

            if (userMessages.Count() > 1)
            {
                var lastMessage = userMessages.ToArray()[1];

                switch (lastMessage.Message)
                {
                    case ADDRESS:
                        userInfo.PostAddress = e.Message.Text;
                        await _client.SendTextMessageAsync(e.Message.Chat.Id, "Записал адрес.");
                        break;

                    case LOVE_TO_RECEIVE:
                        userInfo.LoveToReceive = e.Message.Text;
                        await _client.SendTextMessageAsync(e.Message.Chat.Id, "Записал предпочтения.");
                        break;

                    case DO_NOT_LOVE_TO_RECEIVE:
                        userInfo.DontWantToReceive = e.Message.Text;
                        await _client.SendTextMessageAsync(e.Message.Chat.Id, "Записал что не нравится.");
                        break;
                    case SEND_FEEDBACK:

                        var grandChild = _rep.GetUserInfos().FirstOrDefault(x => x.UserName == userInfo.IAmSecretSantaForUser);
                        if (grandChild == null)
                        {
                            grandChild = _rep.GetUserInfos().FirstOrDefault(x => x.MySecretSanta == userInfo.UserName);
                            if (grandChild == null)
                            {
                                await SendLogToLorinAsync(userInfo, "Не нашел внучка в базе");
                                break;
                            }
                        }
                        await _client.SendTextMessageAsync(grandChild.ChatId, "Твой дед-анон пишет тебе:");
                        await _client.SendTextMessageAsync(grandChild.ChatId, "```" + e.Message.Text + "```");
                        //await SendLogToLorinAsync(userInfo, "отправил своему внучку сообщение");

                        break;
                    case SEND_FEEDBACK_TO_SANTA:

                        var mySanta = _rep.GetUserInfos().FirstOrDefault(x => x.UserName == userInfo.MySecretSanta);

                        if (mySanta == null)
                        {
                            mySanta = _rep.GetUserInfos().FirstOrDefault(x => x.IAmSecretSantaForUser == userInfo.UserName);
                            if (mySanta == null)
                            {
                                await SendLogToLorinAsync(userInfo, "Не нашел санту в базе");
                                break;
                            }

                        }
                        await _client.SendTextMessageAsync(mySanta.ChatId, "Твой внук пишет тебе:");
                        await _client.SendTextMessageAsync(mySanta.ChatId, "```" + e.Message.Text + "```");
                        //await SendLogToLorinAsync(userInfo, "отправил своему санте сообщение");

                        break;
                    default:

                        break;
                }

                _rep.SaveUserInfosToFile(new List<UserSantaInfos>()
                {
                    userInfo
                });

                if (String.IsNullOrEmpty(userInfo.CanSendPresentTo))
                {
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, "Укажи куда можешь выслать подарок");
                }
                if (String.IsNullOrEmpty(userInfo.DontWantToReceive))
                {
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, "Не забудь написать что тебе не нравится");
                }
                if (String.IsNullOrEmpty(userInfo.LoveToReceive))
                {
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, "Не забуть написать что тебе нравится");
                }
                if (String.IsNullOrEmpty(userInfo.PostAddress))
                {
                    await _client.SendTextMessageAsync(e.Message.Chat.Id, "И не забудь свой адрес");
                }
            }
        }

        private async Task SendLogToLorinAsync(UserSantaInfos userInfo, string v)
        {

            var lorin = _rep.GetUserInfos().FirstOrDefault(x => x.UserName == "ostelaymetaule");
            await _client.SendTextMessageAsync(lorin?.ChatId, "баг репорт от " + userInfo.UserName + ": " + v);

        }

        private bool IsFirstMessage(Message message)
        {
            var messages = _rep.GetMessages();
            var firstMessageExists = messages.Exists(x => x.UserName == message.Chat.Username);
            return !firstMessageExists;
        }

        private IFileRepository _rep;


        public async Task<bool> SendNotificationAsync()
        {
            var userInfos = _rep.GetUserInfos();
            foreach (var infoUser in userInfos)
            {
                if (!infoUser.NotificationSent)
                {
                    if (String.IsNullOrEmpty(infoUser.PostAddress))
                    {
                        await _client.SendTextMessageAsync(infoUser.ChatId, "Привет! Мы заканчиваем прием заявок. Ты не указал адрес и ФИО куда подарок слать. До 16.12.19 можешь передумать и заполнить анкету. Иначе при распределении дедов морозов твоя заявка учитываться не будет.");
                    }
                    else
                    {
                        var text = @"Привет!

Вот и настало время получения внучков. Очень важно для вас:
ЧЗВ - (он же ФАКью)

1. Цена подарка
- 7-10$. Можно, конечно, и дороже, но это уже на ваше усмотрение.

2. Когда отправлять?
- Как можно быстрее, потому что цель всё же успеть одарить вашего 'внучка' до нового года

3.Мне нужно узнать у 'внучка' что - то важное
 - Воспользуйся кнопочкой 'написать внучку'.Нет, он не узнает, кто ты, всё хорошо.

4.У меня всё поменялось, надо сообщить об этом Санте
- Воспользуйся кнопочкой 'написать дедушке', или как мы её там обозвали

5.Я жду и жду, а подарка так и нет, пните моего Санту.Он на мои сообщения не отвечает.
-Пинать начнем официально в середине января, иначе списываем всё на погоду на Марсе и Почту Уганды

6.Надо ли сообщать 'внучку', кто я?
-Это решение за вами.Аутить мы никого не будем, разве что очень плохих Сант, которые не удосужатся сходить на почту.

7.Я получил подарок, что теперь
- Расскажи об этом в чатике и не забудь использовать хэштег #СекретныйСанта

Ну вроде и всё! Всем счастливых праздников и приятной игры!";
                        await _client.SendTextMessageAsync(infoUser.ChatId, text);

                    }
                    infoUser.NotificationSent = true;
                }
            }
            _rep.SaveUserInfosToFile(userInfos);


            return true;
        }


        public async Task<bool> SendNotification2Async()
        {

            var userInfos = _rep.GetUserInfos();
            foreach (var infoUser in userInfos)
            {

                await _client.SendTextMessageAsync(infoUser.ChatId, "Да, вот именно это ты указал, проверь пожалуйста");
                var allInfo = $"username @{infoUser.UserName}: Адрес получателя: {infoUser.PostAddress};\n куда могу послать подарок: {infoUser.CanSendPresentTo}\n Хочешь получить: {infoUser.LoveToReceive} \n Не хочешь: {infoUser.DontWantToReceive} \n";
                await _client.SendTextMessageAsync(infoUser.ChatId, allInfo);
                //infoUser.NotificationSent = true;

            }
            //_rep.SaveUserInfosToFile(userInfos);


            return true;
        }


        public async Task<bool> SendSecretSantaMatchAsync()
        {

            var userInfos = _rep.GetUserInfos().Where(x => !String.IsNullOrEmpty(x.IAmSecretSantaForUser));
            foreach (var infoUser in userInfos)
            {
                await SingleSantaMatchSend(userInfos, infoUser);
                //infoUser.NotificationSent = true;

            }
            //_rep.SaveUserInfosToFile(userInfos);


            return true;
        }

        private async Task SingleSantaMatchSend(IEnumerable<UserSantaInfos> userInfos, UserSantaInfos infoUser)
        {


            if (!String.IsNullOrEmpty(infoUser.IAmSecretSantaForUser))
            {
                await _client.SendTextMessageAsync(infoUser.ChatId, "Привет Санта, искусственный интеллект посовещался и решил что вот он твой внучок: ");
                var target = userInfos.FirstOrDefault(x => x.UserName == infoUser.IAmSecretSantaForUser);
                var allInfo = $"Ник внучка @{target.UserName}: Адрес внучка: {target.PostAddress};\n  Пожелания внучка: {target.LoveToReceive} \n Внучок не хочет: {target.DontWantToReceive} \n";
                await _client.SendTextMessageAsync(infoUser.ChatId, allInfo);
            }



        }

        public async Task CallForMessages()
        {
            var me = await _client.GetMeAsync();
            var updates = await _client.GetUpdatesAsync();
            var messages = updates.Select(x => new SecretSantaEntry()
            {
                Message = x.Message.Text,
                UserName = x.Message.Chat.Username,
                MessageId = x.Message.MessageId,
                MessageTime = x.Message.Date,
                UserFirstName = x.Message.Chat.FirstName,
                UserLastName = x.Message.Chat.LastName,
                ChatId = x.Message.Chat.Id
            }).ToList();
        }
    }
}